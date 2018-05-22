#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using BehaviourMachine;
using EventSystem;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

#endregion

#if UNITY_ANDROID && !UNITY_EDITOR
using ICSharpCode.SharpZipLib.Zip;
#endif

public class ResourceHolder<T>
{
    private bool mCompleted;
    public T Resource { get; private set; }

    public void LoadCompleteCallback(T t)
    {
        Resource = t;
        mCompleted = true;
    }

    public Coroutine Wait()
    {
        return ResourceManager.Instance.StartCoroutine(WaitImpl());
    }

    private IEnumerator WaitImpl()
    {
        while (!mCompleted)
        {
            yield return null;
        }
    }
}

public class BundleLoader : Singleton<BundleLoader>
{
    private readonly LinkedList<Action> mQueue = new LinkedList<Action>();

    public Dictionary<string, string> mWaitingDownloadBundles = new Dictionary<string, string>();
    public Queue<string> DownloadBundleKeyQueue = new Queue<string>();

    //用来在 debugwindow 显示用
    public string DownLoadingFileName = string.Empty;
    public string ErrorMessage = "Nothing";
    public bool FirstPriorityDownLoading = false;
    private static int WritingBundle = 0;
    private readonly Thread saveThread;
    private readonly AutoResetEvent evt = new AutoResetEvent(false);
    private FileStream saveFileStream;
    private string bundleFilePath;
    private string bundlePathKey;
    private MemoryStream writeBundleStream = null;
    public bool DownLoadCanStart = false;

    //用来确保每次只有一个www实例
    public float mQueueLockerTimes;
    public bool mDownloadLocker;

    public static string dataPath = Application.dataPath;
    public static RuntimePlatform platfrom = Application.platform;

    private Stopwatch LoadStopWatch = new Stopwatch();
#if UNITY_ANDROID && !UNITY_EDITOR
    private ZipFile mZipFile = null;

    public ZipFile ZipFile
    {
        get
        {
            return mZipFile;
        }
        set { mZipFile = value; }
    }
#endif


    enum eLoadType
    {
        Scene = 0,
        SyncLoad = 1,
        AsyncLoad = 2,
    }

    private HashSet<string> syncBundle = new HashSet<string>();
    private HashSet<string> asyncBundle = new HashSet<string>();
    private HashSet<string> sceneBundle = new HashSet<string>();
    private HashSet<string> allBundle = new HashSet<string>();
    private readonly string[] noSyncPath = 
    {
        "Animation",
        "Effect",
        "Model",
        "Scene",
        "Terrain",
        "Sound",
        "TerrainMeshTree"
    };

    public BundleLoader()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var apkPath = Application.dataPath;
        var filestream = new FileStream(apkPath, FileMode.Open, FileAccess.Read);
        mZipFile = new ZipFile(filestream);
#endif
        UpdateHelper.CheckTargetPath(downloadDictionaryPath);
        LoadBundleDictionary();
        saveThread = new Thread(SaveBundleThread);
        saveThread.Start();
    }

    readonly string replace = "file://" + Application.dataPath.Replace("\\", "/") + "/BundleAsset/";

    private void BundleDebugLog(eLoadType type, string path, int size)
    {
#if BUNDLE_DEBUG_ENABLE
        var str = path.Replace(replace, "");
        str = str.Replace("\\", "/");
        str = str + Environment.NewLine;
//         switch (type)
//         {
//             case eLoadType.Scene:
//                 sceneBundle.Add(str);
//                 break;
//             case eLoadType.AsyncLoad:
//                 asyncBundle.Add(str);
//                 break;
//             case eLoadType.SyncLoad:
//                 syncBundle.Add(str);
//                 break;
//         }

        allBundle.Add(str);
#endif
    }

    public void PrintDebugLogToFile()
    {
#if BUNDLE_DEBUG_ENABLE
      //  WriteToFile(sceneBundle, Application.dataPath+"/../BundleLog/sceneBundle.txt");
      //  WriteToFile(asyncBundle, Application.dataPath+ "/../BundleLog/asyncBunle.txt");
      //  WriteToFile(syncBundle, Application.dataPath+ "/../BundleLog/syncBundle.txt");
        WriteToFile(allBundle, Application.dataPath + "/Res/Table/BundleWhiteList/BundleFiles.txt");
#endif
    }

    public void WriteToFile(HashSet<string> list, string path)
    {
#if BUNDLE_DEBUG_ENABLE
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var dirs = File.ReadAllLines(Application.dataPath + "/Res/Table/BundleWhiteList/Directorys.txt");
        var writeList = new HashSet<string>();
        //把运行时整理好的加进去
        foreach (var s in dirs)
        {
            var ss = s + "/";
            foreach (var oneLine in list)
            {
                if (oneLine.StartsWith(ss))
                {
                    writeList.Add(oneLine);
                }
            }
        }
        //把主角相关的都加进去,因为主角的动作和模型加载用的都是同步的
        AddMyPlayerBundle(writeList);

        FileStream fs = new FileStream(path, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);

        foreach (var oneLine in writeList)
        {
            sw.Write(oneLine);
        }
        sw.Flush();
        sw.Close();
        fs.Close();
#endif
    }

    private void AddMyPlayerBundle(ICollection<string> container)
    {

    }

    private const int MaxLoadcount = 40;
    public void Tick(float delta)
    {
        mQueueLockerTimes = 4;
        for (var i = 0; i < MaxLoadcount; i++)
        {
            if (mQueue.Count <= 0)
            {
                DownloadBundle();
            }
            else
            {
                var action = mQueue.First.Value;
                mQueue.RemoveFirst();
                action();
            }
        }
    }

    // 通过ResourceHolder.Resource获取资源
    public ResourceHolder<T> PrepareResource<T>(string bundlePath,
                                                string assetName,
                                                bool clearBundle,
                                                bool cacheResource,
                                                bool firstPriority,
                                                bool fromCache,
                                                bool forceFromUnityCache) where T : Object
    {
        var resourceHolder = new ResourceHolder<T>();

        if (mQueue.Count == 0 || firstPriority || fromCache)
        {
            Object res = null;
            if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
            {
                resourceHolder.LoadCompleteCallback(res as T);
                return resourceHolder;
            }
        }

        Action func = () =>
        {
            Object res = null;
            if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
            {
                resourceHolder.LoadCompleteCallback(res as T);
                return;
            }
            //mQueueLockerTimes = true;
            ResourceManager.Instance.StartCoroutine(Instance.GetResourceWithHolder(bundlePath, assetName, resourceHolder,
                clearBundle, cacheResource, forceFromUnityCache));
        };

        if (firstPriority)
        {
            mQueue.AddFirst(func);
        }
        else
        {
            mQueue.AddLast(func);
        }

        return resourceHolder;
    }

    // 通过action获取资源
    public void GetBundleResource<T>(string path,
                                     string name,
                                     Action<T> callBack,
                                     bool clearBundle,
                                     bool cacheResource,
                                     bool firstPriority,
                                     bool fromCache)
        where T : Object
    {
        if (mQueue.Count == 0 || firstPriority || fromCache)
        {
            Object res = null;
            if (ResourceManager.Instance.TryGetResourceFromCache(path, out res))
            {
                if (callBack != null)
                {
                    try
                    {
                        callBack(res as T);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("GetBundleResource {0}, {1}, {2}", path, name, ex.ToString());
                    }
                }
                return;
            }
        }

        Action func = () =>
        {
            Object res = null;
            if (ResourceManager.Instance.TryGetResourceFromCache(path, out res))
            {
                if (callBack != null)
                {
                    try
                    {
                        callBack(res as T);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("GetBundleResource {0}, {1}, {2}", path, name, ex.ToString());
                    }
                }
                return;
            }
          //  mQueueLockerTimes = true;
            ResourceManager.Instance.StartCoroutine(Instance.GetBundleResourceWithCallBack(path, name, callBack,
                clearBundle,
                cacheResource));
        };

        if (firstPriority)
        {
            mQueue.AddFirst(func);
        }
        else
        {
            mQueue.AddLast(func);
        }
    }



    //同步获取bundle资源方法
    public T GetResourceSync<T>(string bundlePath, string assetName, bool clearBundle, bool cacheResource, bool compressed)
        where T : Object
    {

#if BUNDLE_DEBUG_ENABLE
        foreach (var t in noSyncPath.Where(bundlePath.StartsWith))
        {
            Logger.Error("!!!!Load bundle Sync error!!!!! path :{0}, name :{1}", bundlePath, assetName);
        }
#endif
        var size = 0;
        Object res = null;
        if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
        {
            return res as T;
        }

        T resource;

        if (ResourceManager.Instance.UseAssetBundle)
        {
            resource = LoadResourceFromBundleSync<T>(bundlePath, assetName, clearBundle,compressed, out size);
        }
        else
        {
            resource = LoadResourceFromAsset<T>(bundlePath, assetName);
        }

        if (cacheResource)
        {
            ResourceManager.Instance.AddResourcesToCache(bundlePath, resource, size);
        }

        return resource;
    }

    private IEnumerator GetResourceWithHolder<T>(string bundlePath,
        string assetName,
        ResourceHolder<T> resourceHolder,
        bool clearBundle,
        bool cacheResource,
        bool forceFromUnityCache) where T : Object
    {
        T resource = default(T);
        var size = 0;
        if (ResourceManager.Instance.UseAssetBundle)
        {
            string path;
            string bundlefullname;
            var needDown = GetBundleRealPath(bundlePath, out path, out bundlefullname);
            WWW www;
            if (forceFromUnityCache && !needDown)
                www = WWW.LoadFromCacheOrDownload(path, UpdateHelper.Version);
            else
                www = clearBundle ? new WWW(path) : WWW.LoadFromCacheOrDownload(path, UpdateHelper.Version);

            yield return www;

            if (www.error != null)
            {
                Logger.Error("{0}, {1}", www.error, bundlePath);
                resourceHolder.LoadCompleteCallback(null);
                www.Dispose();
             //   mQueueLockerTimes = false;
                yield break;
            }

            if (clearBundle)
            {
                BundleDebugLog(eLoadType.AsyncLoad, path, 0);
            }

            size = 1;

            Object res = null;
            if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
            {
                resource = res as T;
            }
            else
            {
//                 var request = www.assetBundle.LoadAsync(Path.GetFileNameWithoutExtension(assetName), typeof(T));
//                 yield return request;
//                 resource = request.asset as T;


                if (mQueueLockerTimes < 0)
                {
                    yield return new WaitForEndOfFrame();
                }

                LoadStopWatch.Reset();
                LoadStopWatch.Start();
                resource = www.assetBundle.Load(Path.GetFileNameWithoutExtension(assetName), typeof (T)) as T;
                mQueueLockerTimes -= LoadStopWatch.ElapsedMilliseconds;
            }

#if UNITY_EDITOR
            var obj = resource as GameObject;
            if (obj)
            {
                ResourceManager.ChangeShader(obj.transform);
            }
#endif

            //缓存到硬盘
            if (needDown)
            {
                while (Interlocked.CompareExchange(ref WritingBundle, 1, 1) == 1)
                {
                    yield return new WaitForEndOfFrame();
                }
                SaveBundleToDisk(www, bundlefullname);
            }


            //清理
            if (clearBundle || forceFromUnityCache)
            {
                www.assetBundle.Unload(false);
            }
            else
            {
                ResourceManager.Instance.mCommonBundle.Add(www.assetBundle);
            }
            www.Dispose();
        }
        else
        {
            resource = LoadResourceFromAsset<T>(bundlePath, assetName);
            yield return new WaitForEndOfFrame();
        }

        if (cacheResource)
        {
            ResourceManager.Instance.AddResourcesToCache(bundlePath, resource, size);
        }

        //mQueueLockerTimes = false;
        resourceHolder.LoadCompleteCallback(resource);
    }

    private void SaveBundleThread()
    {

        while (evt.WaitOne())
        {
            try
            {
                saveFileStream = new FileStream(bundleFilePath, FileMode.Create);
                saveFileStream.Write(writeBundleStream.GetBuffer(), 0, (int) writeBundleStream.Length);
                saveFileStream.Close();

                Dictionary<string, string> dict = null;
                lock (mWaitingDownloadBundles)
                {
                    mWaitingDownloadBundles.Remove(bundlePathKey);
                    if (++saveCount % 10 == 0 || mWaitingDownloadBundles.Count < 2)
                    {
                        dict = new Dictionary<string, string>(mWaitingDownloadBundles);
                    }

                    if (mWaitingDownloadBundles.Count == 0)
                    {
                        writeBundleStream.Dispose();
                    }
                }

                if (dict != null)
                {
                    SaveBundleDictionary(dict);
                }
            }
            catch
            {

            }
            finally
            {
                Interlocked.Exchange(ref WritingBundle, 0);
            }
        }
    }

    private void SaveBundleToDisk(WWW www,string path)
    {
        try
        {
            if (null == writeBundleStream)
            {
                writeBundleStream = new MemoryStream();
            }
            Interlocked.Exchange(ref WritingBundle, 1);
            var localUrl = Path.Combine(UpdateHelper.DownloadRoot, path);
            UpdateHelper.CheckTargetPath(localUrl);
            writeBundleStream.SetLength(0);
            writeBundleStream.Seek(0, SeekOrigin.Begin);
            writeBundleStream.Write(www.bytes, 0, www.bytesDownloaded);
            bundleFilePath = localUrl;
            bundlePathKey = path;
            //Profiler.BeginSample("FileStream Write ");
            evt.Set();
            // DownloadBundleKeyQueue.
            //Profiler.EndSample();
        }
        catch (Exception e)
        {

            Interlocked.Exchange(ref WritingBundle, 0);
            Logger.Error("SaveBundleToDisk fail. path {0}, error {1}", path, e);
        }
    }

    private static int saveCount = 0;

    private static void SaveBundleToCache()
    {
       
    }

    private IEnumerator GetBundleResourceWithCallBack<T>(string bundlePath,
        string assetName,
        Action<T> callBack,
        bool clearBundle,
        bool cacheResource) where T : Object
    {
        var resource = default(T);
        var size = 0;
        if (ResourceManager.Instance.UseAssetBundle)
        {
            string path;
            string bundlefullname;
            var needDown = GetBundleRealPath(bundlePath, out path, out bundlefullname);
            WWW www;
            if (clearBundle)
            {
                www = new WWW(path);
            }
            else
            {
                www = WWW.LoadFromCacheOrDownload(path, UpdateHelper.Version);
            }
            yield return www;

            if (www.error != null)
            {
                Logger.Error("{0}, {1}", www.error, bundlePath);
                www.Dispose();
                //mQueueLockerTimes = false;
                yield break;
            }

            if (clearBundle)
            {
                BundleDebugLog(eLoadType.AsyncLoad, path, 0);
            }

            size = 1;
            Object res = null;
            if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
            {
                resource = res as T;
            }
            else
            {
//                 var request = www.assetBundle.LoadAsync(Path.GetFileNameWithoutExtension(assetName), typeof(T));
//                 yield return request;
//                 resource = request.asset as T;
                if (mQueueLockerTimes < 0)
                {
                    yield return new WaitForEndOfFrame();
                }

                LoadStopWatch.Reset();
                LoadStopWatch.Start();
                resource = www.assetBundle.Load(Path.GetFileNameWithoutExtension(assetName), typeof(T)) as T;
                mQueueLockerTimes -= LoadStopWatch.ElapsedMilliseconds;
            }

#if UNITY_EDITOR
            var obj = resource as GameObject;
            if (obj)
            {
                ResourceManager.ChangeShader(obj.transform);
            }
#endif

            if (needDown)
            {
                while (Interlocked.CompareExchange(ref WritingBundle, 1, 1) == 1)
                {
                    yield return new WaitForEndOfFrame();
                }
                SaveBundleToDisk(www, bundlefullname);
            }

            if (clearBundle)
            {
                www.assetBundle.Unload(false);
            }
            else
            {
                ResourceManager.Instance.mCommonBundle.Add(www.assetBundle);
            }
            www.Dispose();
        }
        else
        {
            resource = LoadResourceFromAsset<T>(bundlePath, assetName);
            yield return new WaitForEndOfFrame();
        }

        if (cacheResource)
        {
            ResourceManager.Instance.AddResourcesToCache(bundlePath, resource, size);
        }

        //mQueueLockerTimes = false;

        try
        {
            if (resource == null)
            {
                callBack(null);
            }
            else
            {
                callBack(resource);
            }
        }
        catch (Exception e)
        {
            Logger.Error("---------CallBackError!!------ Name = {0} , Exception = {1}", bundlePath, e);
        }
    }

    private void DownloadBundle()
    {
        if (!DownLoadCanStart || mDownloadLocker)
        {
            return;
        }

        lock (mWaitingDownloadBundles)
        {
            if (mWaitingDownloadBundles.Count == 0)
            {
                return;
            }
        }

        mDownloadLocker = true;
        try
        {
            ResourceManager.Instance.StartCoroutine(DownloadBundleOne());
        }
        catch (Exception e)
        {
            mDownloadLocker = false;
        }
    }

    private IEnumerator DownloadBundleOne()
    {

        //var enumerator = mWaitingDownloadBundles.GetEnumerator();
        if (DownloadBundleKeyQueue.Count == 0)
        {
            mDownloadLocker = false;
            yield break;
        }
        var key = DownloadBundleKeyQueue.Dequeue();

        string value = string.Empty;
        lock (mWaitingDownloadBundles)
        {
            if (!mWaitingDownloadBundles.ContainsKey(key))
            {
                mDownloadLocker = false;
                yield break;
            }

            value = mWaitingDownloadBundles[key];
        }

        var path = UpdateHelper.CheckUrl(value + "/Resources/" + key);
        DownLoadingFileName = key;
        FirstPriorityDownLoading = false;
        var www = new WWW(path);
        yield return www;
        if (!string.IsNullOrEmpty(www.error))
        {
            DownloadBundleKeyQueue.Enqueue(key);
            ErrorMessage = string.Format("download www bundle:{0} error:{1}", path, www.error);
            Logger.Error(ErrorMessage);
            mDownloadLocker = false;
            www.Dispose();
            yield break;
        }

        SaveBundleToCache();
        while (Interlocked.CompareExchange(ref WritingBundle, 1, 1) == 1)
        {
            yield return new WaitForEndOfFrame();
        }
        SaveBundleToDisk(www, key);
        www.Dispose();
        mDownloadLocker = false;
    }

    internal IEnumerator GetSceneResource(string bundlePath, string assetName, Action<WWW> callBack)
    {
        //mQueueLockerTimes = true;
      
        string path;
        string bundlefullname;
        var needDown = GetBundleRealPath(bundlePath, out path, out bundlefullname);
        if (needDown)
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ShowDownloadingSceneTipEvent());
        }

        WWW www = new WWW(path);
        yield return www;

        if (www.error != null)
        {
            if (null != callBack)
            {
                try
                {
                    callBack(www);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
            }

            //mQueueLockerTimes = false;
            www.Dispose();

            yield break;
        }

        if (needDown)
        {
            while (Interlocked.CompareExchange(ref WritingBundle, 1, 1) == 1)
            {
                yield return new WaitForEndOfFrame();
            }
            SaveBundleToDisk(www, bundlefullname);
        }

        BundleDebugLog(eLoadType.Scene, path, 0);

        if (!www.assetBundle)
        {
            yield break;
        }

        www.assetBundle.LoadAll();

        if (null != callBack)
        {
            try
            {
                callBack(www);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                //mQueueLockerTimes = false;
            }
        }
    }

    public static string GetStreamingAssetsUrl(string path)
    {
#if UNITY_EDITOR
        return String.Format("file://{0}/{1}", dataPath + "/BundleAsset", path);
#endif
        if (platfrom == RuntimePlatform.Android)
        {
            return string.Format("{0}/{1}", Application.streamingAssetsPath, path);
        }
        return string.Format("file://{0}/{1}", Application.streamingAssetsPath, path);
    }

    public static bool BundleExistInUpdatePath(string bundlePath, out string bundleUrl)
    {
#if UNITY_EDITOR
       // if (!ResourceManager.Instance.UseAssetBundle)
        {
            bundleUrl = string.Empty;
            return false;
        }
#endif
        bundlePath = bundlePath.Replace("\\", "/");
        var fileName = bundlePath.Replace("/", "_");
        var tempPath = Path.Combine(
            Path.Combine(UpdateHelper.DownloadRoot, Path.GetDirectoryName(bundlePath)),
            Path.GetFileNameWithoutExtension(fileName)) + ".unity3d";
        bundleUrl = string.Format("file://{0}", tempPath);
#if UNITY_EDITOR_WIN
        bundleUrl = bundleUrl.Replace("\\", "/");
#endif
        return File.Exists(tempPath);
    }

    public bool GetBundleRealPath(string bundlePath, out string path, out string bundleFullName)
    {
        bundleFullName = string.Empty;
        bundlePath = bundlePath.Replace("\\", "/");
        var fileName = bundlePath.Replace("/", "_");
        fileName = Path.GetFileNameWithoutExtension(fileName) + ".unity3d";
        var nameKey = Path.Combine(Path.GetDirectoryName(bundlePath), fileName);
#if UNITY_EDITOR_WIN
        nameKey = nameKey.Replace("\\", "/");
#endif

        lock (mWaitingDownloadBundles)
        {
            if (mWaitingDownloadBundles.ContainsKey(nameKey))
            {
                bundleFullName = nameKey;
                path = UpdateHelper.CheckUrl(mWaitingDownloadBundles[nameKey] + "/Resources/" + nameKey);
                //  path = GameUtils.GetNoCacheUrl(path);
                DownLoadingFileName = nameKey;
                FirstPriorityDownLoading = true;

                return true;
            }
        }

        if (BundleExistInUpdatePath(bundlePath, out path))
        {
            return false;
        }

        var url = GetStreamingAssetsUrl(bundlePath);
        var p1 = url.Substring(0, url.LastIndexOf('/'));
        var p2 = url.Substring(GetStreamingAssetsUrl("").Length).Replace('/', '_');
        path = Path.Combine(p1, p2);
        return false;
    }

    //调试环境直接从源文件读取资源
    public static T LoadResourceFromAsset<T>(string bundlePath, string assetName) where T : Object
    {
        if (string.IsNullOrEmpty(Path.GetExtension(assetName)))
        {
            assetName = assetName + ".prefab";
        }
        var realPath = Path.Combine("Assets/Res", Path.GetDirectoryName(bundlePath));
        realPath = Path.Combine(realPath, Path.GetFileName(assetName));
        var asset = Resources.LoadAssetAtPath<T>(realPath);
        if (!asset)
        {
            Logger.Error("can not load resource :" + assetName);
        }
        return asset;
    }

    //不可用，因为我们的资源放在Res目录下没有放在Resources目录下，读取不到.所以使用上边的Resources.LoadAssetAtPath
    public static ResourceRequest LoadResourceFromAssetAsync<T>(string bundlePath, string assetName) where T : Object
    {
        if (string.IsNullOrEmpty(Path.GetExtension(assetName)))
        {
            assetName = assetName + ".prefab";
        }
        var realPath = Path.Combine("Assets/Res", Path.GetDirectoryName(bundlePath));
        realPath = Path.Combine(realPath, Path.GetFileName(assetName));
        var resuest = Resources.LoadAsync<T>(realPath);
        return resuest;
    }



    public T LoadResourceFromBundleSync<T>(string bundlePath, string assetName, bool clearBundle,bool compressed, out int size)
        where T : Object
    {
        var resource = default(T);
        size = 1;
        string realpath;
        string unuse;

        var needDown = GetBundleRealPath(bundlePath, out realpath, out unuse);

        if (needDown)
        {
            size = 0;
            Logger.Error("get file error !:" + bundlePath);
            return null;
        }

        string noUse;
        if (!BundleExistInUpdatePath(bundlePath, out noUse))
        {
            //从apk包中读取文件
#if UNITY_ANDROID && !UNITY_EDITOR
    //GetNextEntry    assets/Controller/MainPlayer.unity3d
    //url             jar:file:///data/app/com.base.maya.test-1.apk!/assets/Controller/MainPlayer.unity3d

        var filepath = realpath.Substring(realpath.IndexOf("assets/"));
        var item = Instance.ZipFile.GetEntry(filepath);
        if (null != item)
        {
            var stream = Instance.ZipFile.GetInputStream(item);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            var assetBundle = AssetBundle.CreateFromMemoryImmediate(buffer);
            resource = assetBundle.Load(Path.GetFileNameWithoutExtension(assetName), typeof(T)) as T;
            stream.Close();
            if(clearBundle)
            {
                assetBundle.Unload(false);
            }
            else
            {
                ResourceManager.Instance.mCommonBundle.Add(assetBundle);
            }
        }
        else
        {
            Logger.Error("--LoadResourceFromBundleSync--,get file from apk error !:" + realpath);
        }

        return resource;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var path = realpath.Substring(realpath.IndexOf("file://") + 7);
#else
		var path = realpath.Substring(realpath.IndexOf("file:") +  5);
#endif
        if (File.Exists(path))
        {
            AssetBundle assetBundle;
            FileStream stream = null;
            if (compressed)
            {
                stream = File.OpenRead(path);
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                assetBundle = AssetBundle.CreateFromMemoryImmediate(buffer);
            }
            else
            {
                assetBundle = AssetBundle.CreateFromFile(path);
            }

            if (null == assetBundle )
            {
                Logger.Error("get file sync error !:" + path);
                return null; 
            }
            resource = assetBundle.Load(Path.GetFileNameWithoutExtension(assetName), typeof(T)) as T;
            if (clearBundle)
            {
                assetBundle.Unload(false);
            }
            else
            {
                ResourceManager.Instance.mCommonBundle.Add(assetBundle);
            }

            Instance.BundleDebugLog(eLoadType.SyncLoad, path, 0);
            if (stream != null) stream.Close();
        }
        else
        {
            Logger.Error("get file error !:" + path);
        }

        return resource;
    }

    private readonly string downloadDictionaryPath = Path.Combine(Application.persistentDataPath, "download.txt");

    public void SaveBundleDictionary(Dictionary<string, string> dict)
    {
        using (var fs = new FileStream(downloadDictionaryPath, FileMode.Create))
        {
            var bf = new BinaryFormatter();
            bf.Serialize(fs, dict);
            fs.Close();
        }
    }

    private void LoadBundleDictionary()
    {
        using (var fs = new FileStream(downloadDictionaryPath, FileMode.OpenOrCreate))
        {
            if (fs.Length != 0)
            {
                var bf = new BinaryFormatter();
                mWaitingDownloadBundles = (Dictionary<string, string>)bf.Deserialize(fs);
                var enumerator = mWaitingDownloadBundles.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    DownloadBundleKeyQueue.Enqueue(enumerator.Current.Key);
                }
            }
            fs.Close();
        }
    }

    public float GetWwwProgress()
    {

//         if (mWaitingWww != null)
//         {
//             return mWaitingWww.progress;
//         }
        return 0;
    }


    internal IEnumerator LoadResourceIntoCacheAsync(string bundlePath, string assetName)
    {


        Object res = null;
        if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
        {
            yield break;
        }

        if (!ResourceManager.Instance.UseAssetBundle)
        {
            res = LoadResourceFromAsset<Object>(bundlePath, assetName);
            ResourceManager.Instance.AddResourcesToCache(bundlePath, res, 1);
            yield break;
        }

        string path;
        string bundlefullname;
        GetBundleRealPath(bundlePath, out path, out bundlefullname);

        var www = new WWW(path);
        yield return www;

        if (www.error != null)
        {
            Logger.Error("{0}, {1}", www.error, bundlePath);
            www.Dispose();
            //   mQueueLockerTimes = false;
            yield break;
        }

        if (ResourceManager.Instance.TryGetResourceFromCache(bundlePath, out res))
        {
            www.Dispose();
            yield break;
        }
        else
        {
            var request = www.assetBundle.LoadAsync(Path.GetFileNameWithoutExtension(assetName), typeof(GameObject));
            yield return request;

            ResourceManager.Instance.AddResourcesToCache(bundlePath, request.asset, 1);
            www.assetBundle.Unload(false);
            www.Dispose();
        }
    }
}