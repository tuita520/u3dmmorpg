#region using

using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using Microsoft.Win32;
#endif

#endregion

public static class BundleTool
{
    //动画文件缀名
    public const string ANIMATION_EXT = ".anim";
    //prefab缀名
    public const string PREFAB_EXT = ".prefab";
    //斜杠
    public const char SLASH = '/';
    //获得最后文件夹名字  比方:传入path[abcd/myfolder] 返回值:myfolder
    public static string GetLastFolderName(string directory)
    {
        var i = directory.LastIndexOf(SLASH);
        if (-1 == i)
        {
            return directory;
        }
        return directory.Substring(i + 1, directory.Length - i - 1);
    }

    //转成prefab文件名字 比方:传入path[abcd/a] 返回值:a.prefab
    public static string GetPrefabName(string directory)
    {
        return GetLastFolderName(directory) + PREFAB_EXT;
    }
}

public class ResourceInfo
{
    private ResourceInfo()
    {
    }

    public ResourceInfo(int size, Object resource)
    {
        Size = size;
        Resource = resource;
    }

    public Object Resource { get; private set; }
    public int Size { get; private set; }
}

public class ResourceManager : MonoBehaviour, IManager
{
    //默认的bundle文件后缀
    private const string BUNDLEEXT = ".unity3d";
    public static GameObject sGameObject;
    private static ResourceManager sInstance;
    private float mClearCacheTimer;
    public List<AssetBundle> mCommonBundle = new List<AssetBundle>();
    public float mfCacheMB;
    
    private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, ResourceInfo>>> mLinkedListNodes =
        new Dictionary<string, LinkedListNode<KeyValuePair<string, ResourceInfo>>>();

    //资源cache
    private readonly LinkedList<KeyValuePair<string, ResourceInfo>> mResourcesCache =
        new LinkedList<KeyValuePair<string, ResourceInfo>>();

    private bool mUseAssetBundle = true;
    private RenderTexture mScreenRT = null;
    private RenderTexture mDistortionRT = null;

    public RenderTexture getScreenRenderTexture()
    {
        if (null != mScreenRT) return mScreenRT;

        var radio = GameUtils.GetResolutionRadio();
        var width = Mathf.CeilToInt(Screen.width * radio);
        var height = Mathf.CeilToInt(Screen.height * radio);
        mScreenRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        return mScreenRT;
    }

    public RenderTexture getDistortionRenderTexture()
    {
        if (null != mDistortionRT) return mDistortionRT;

        var radio = GameUtils.GetResolutionRadio();
        var width = Mathf.CeilToInt(Screen.width * radio);
        var height = Mathf.CeilToInt(Screen.height * radio);
        mDistortionRT = new RenderTexture(width, height, 0, GetSmallestRenderTextureFormat());
        return mDistortionRT;
    }

    public static RenderTextureFormat GetSmallestRenderTextureFormat()
    {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8))
        {
            return RenderTextureFormat.R8;
        }
        else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf))
        {
            return RenderTextureFormat.RHalf;
        }
        else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat))
        {
            return RenderTextureFormat.RFloat;
        }
        else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RInt))
        {
            return RenderTextureFormat.RInt;
        }
        /*
        else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
        {
            return RenderTextureFormat.ARGBHalf;
        }
         * */
        else
        {
            return RenderTextureFormat.Default;
        }
    }

    public void ResizeRenderTexture()
    {
        if (!GameSetting.Instance.RenderTextureEnable) return;

        var radio = GameUtils.GetResolutionRadio();
        var width = Mathf.CeilToInt(Screen.width * radio);
        var height = Mathf.CeilToInt(Screen.height * radio);
        if (null != mScreenRT)
        {
            Destroy(mScreenRT);
        }
        if(null != mDistortionRT)
        {
            Destroy(mDistortionRT);
        }
        if (GameLogic.Instance && GameLogic.Instance.MainCamera && GameSetting.Instance.RenderTextureEnable)
        {
            mScreenRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            mDistortionRT = new RenderTexture(width, height, 0, GetSmallestRenderTextureFormat());
            GameLogic.Instance.MainCamera.targetTexture = mScreenRT;
        }
    }

    public static ResourceManager Instance
    {
        get
        {
            if (null == sInstance)
            {
                Application.backgroundLoadingPriority = ThreadPriority.Normal;

                sGameObject = GameObject.Find("ResourceManager");
                if (null == sGameObject)
                {
                    sGameObject = new GameObject("ResourceManager");
                    DontDestroyOnLoad(sGameObject);
                }

                sInstance = sGameObject.GetComponent<ResourceManager>();
                if (null == sInstance)
                {
                    sInstance = sGameObject.AddComponent<ResourceManager>();
                }
#if UNITY_EDITOR
                var key = Registry.CurrentUser;
                RegistryKey software;
                software = key.OpenSubKey("software\\Spartacus", true);
                if (software != null)
                {
                    var path = software.GetValue("NotUseBundleOnEditor");
                    if (path != null)
                    {
                        //注册表里存储的是NotUse，这里用的是use
                        sInstance.mUseAssetBundle = !bool.Parse(path.ToString().ToLower());
                    }
                }
                key.Close();
#endif
            }
            return sInstance;
        }
    }

    public bool UseAssetBundle
    {
        get { return mUseAssetBundle; }
    }

    //ios和android在editor下shader需要从新赋值一次
    public static void ChangeShader(Transform obj)
    {
#if UNITY_EDITOR
        if (obj.renderer != null && obj.renderer.sharedMaterial != null)
        {
            var sm = obj.renderer.sharedMaterial;
            var shaderName = sm.shader.name;
            if (!String.IsNullOrEmpty(shaderName))
            {
                var newShader = Shader.Find(shaderName);
                if (newShader != null)
                {
                    sm.shader = newShader;
                }
                else
                {
                    Logger.Warn("unable to refresh shader: " + shaderName + " in material " + sm.name);
                }
            }
        }

        var objchildCount0 = obj.childCount;
        for (var i = 0; i < objchildCount0; i++)
        {
            ChangeShader(obj.transform.GetChild(i));
        }
#endif
    }

    internal static bool Exist(string aniFile)
    {
        //先判断自动更新目录
        string noUse;
        if (BundleLoader.BundleExistInUpdatePath(aniFile, out noUse))
        {
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        var bundlePath = BundleNameSplit(aniFile);
        string realpath, nouse;
        var needUpdate = BundleLoader.Instance.GetBundleRealPath(AddBundleExt(bundlePath[0]), out realpath, out nouse);
        if(needUpdate)
        {
            return true;
        }
        var filepath = realpath.Substring(realpath.IndexOf("assets/"));
        var item = BundleLoader.Instance.ZipFile.GetEntry(filepath);
        return (item != null);
#else
        if (sInstance.UseAssetBundle)
        {
            aniFile = aniFile.Replace("\\", "/");
            var fileName = aniFile.Replace("/", "_");
#if UNITY_EDITOR
            var path = Path.Combine(
                Path.Combine(Path.Combine(Application.dataPath, "BundleAsset"), Path.GetDirectoryName(aniFile)),
                Path.GetFileNameWithoutExtension(fileName)) + ".unity3d";
#else
            var path = Path.Combine(
                Path.Combine(Application.streamingAssetsPath, Path.GetDirectoryName(aniFile)),
                Path.GetFileNameWithoutExtension(fileName)) + ".unity3d";
#endif
            return File.Exists(path);
        }
        return File.Exists(Path.Combine(Path.Combine(Application.dataPath, "Res"), aniFile));
#endif
    }

    public void UnloadCommonBundle()
    {
        var c = mCommonBundle.Count;
        for (var i = 0; i < c; i++)
        {
            mCommonBundle[i].Unload(false);
        }
        mCommonBundle.Clear();
    }

    public IEnumerator Init()
    {
        yield return null;
    }

    public void Reset()
    {
        //  ClearCache(true);
    }

    public void Tick(float delta)
    {
        //Profiler.BeginSample("t1");
        BundleLoader.Instance.Tick(delta);
        //Profiler.EndSample();

        // 在lowmemeory 的时候再清理吧
        //mClearCacheTimer += delta;

        //const float interval = 5.0f;

        ////Profiler.BeginSample("t2");
        //if (mClearCacheTimer > interval)
        //{
        //    ClearCache();
        //    mClearCacheTimer -= interval;
        //}
        ////Profiler.EndSample();

        if (LoadingCompleted && AsyncLoadingQueue.Count > 0)
        {
            StartCoroutine(LoadResourceIntoCacheCoroutine());
        }
    }

    public void Destroy()
    {
        ClearCache(true);
        UnloadCommonBundle();
        if (null != sGameObject)
        {
            DestroyObject(sGameObject);
        }
    }

    #region 读取资源静态方法

    // 异步读取bundle，通过action返回
    public static void PrepareResource<T>(string bundleName,
                                          string resourceName,
                                          Action<T> callBack,
                                          bool clearBundle = true,
                                          bool cacheResource = true,
                                          bool firstPriority = false,
                                          bool fromCache = false)
        where T : Object
    {
        //Logger.Debug ("PrepareResourceWithHolder {0}, {1}", bundleName, resourceName);
        BundleLoader.Instance.GetBundleResource(AddBundleExt(bundleName), resourceName, callBack, clearBundle,
            cacheResource, firstPriority, fromCache);
    }


    //异步读取bundle，resourceHoder.wait()之后返回资源
    public static ResourceHolder<T> PrepareResourceWithHolder<T>(string bundleName,
                                                                 string resourceName,
                                                                 bool clearBundle = true,
                                                                 bool cacheResource = true,
                                                                 bool firstPriority = false,
                                                                 bool fromCache = false,
                                                                 bool forceFromUnityCache = false)
        where T : Object
    {
        //Logger.Debug ("PrepareResourceWithHolder {0}, {1}", bundleName, resourceName);
        return BundleLoader.Instance.PrepareResource<T>(AddBundleExt(bundleName), resourceName, clearBundle,
            cacheResource, firstPriority, fromCache, forceFromUnityCache);
    }

    //加载资源简化版
    public static T PrepareResourceSync<T>(string bundleName, bool clearBundle = true, bool cacheResource = true, bool compressed = true)
        where T : Object
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            return default(T);
        }

        //Logger.Debug ("PrepareResourceWithHolder {0}", bundleName);
        var strs = BundleNameSplit(bundleName);
        return BundleLoader.Instance.GetResourceSync<T>(AddBundleExt(strs[0]), strs[1], clearBundle, cacheResource, compressed);
    }

    public static void PrepareResource<T>(string bundleName,
                                          Action<T> callback,
                                          bool clearBundle = true,
                                          bool cacheResource = true,
                                          bool sync = false,
                                          bool firstPriority = false,
                                          bool fromCache = false,
                                          bool compressed = true)
        where T : Object
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            return;
        }

        //Logger.Debug ("PrepareResourceWithHolder {0}", bundleName);
        var strs = BundleNameSplit(bundleName);

        if (sync)
        {
            var res = BundleLoader.Instance.GetResourceSync<T>(AddBundleExt(strs[0]), strs[1], clearBundle,
                cacheResource,compressed);
            if (callback != null)
            {
                try
                {
                    callback(res);
                }
                catch (Exception ex)
                {
                    Logger.Error("Prepare resource sync error, name:" + bundleName + "\n" + ex);
                }
            }
        }
        else
        {
            BundleLoader.Instance.GetBundleResource(AddBundleExt(strs[0]), strs[1], callback, clearBundle, cacheResource,
                firstPriority, fromCache);
        }
    }

    public static void PrepareResource<T0, T1>(string bundleName0,
                                               string bundleName1,
                                               Action<T0, T1> callBack,
                                               bool clearBundle = true,
                                               bool cacheResource = true,
                                               bool firstPriority = false,
                                               bool fromCache = false)
        where T0 : Object
        where T1 : Object
    {
        if (string.IsNullOrEmpty(bundleName0))
        {
            return;
        }
        var strs0 = BundleNameSplit(bundleName0);
        var t0 = default(T0);

        if (string.IsNullOrEmpty(bundleName1))
        {
            return;
        }
        var strs1 = BundleNameSplit(bundleName1);
        var t1 = default(T1);

        var count = 0;

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs0[0]), strs0[1], (T0 res) =>
        {
            t0 = res;
            count++;
            if (count == 2)
            {
                callBack(t0, t1);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs1[0]), strs1[1], (T1 res) =>
        {
            t1 = res;
            count++;
            if (count == 2)
            {
                callBack(t0, t1);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);
    }

    public static void PrepareResource<T0, T1, T2>(string bundleName0,
                                                   string bundleName1,
                                                   string bundleName2,
                                                   Action<T0, T1, T2> callBack,
                                                   bool clearBundle = true,
                                                   bool cacheResource = true,
                                                   bool firstPriority = false,
                                                   bool fromCache = false)
        where T0 : Object
        where T1 : Object
        where T2 : Object
    {
        if (string.IsNullOrEmpty(bundleName0))
        {
            return;
        }
        var strs0 = BundleNameSplit(bundleName0);
        var t0 = default(T0);

        if (string.IsNullOrEmpty(bundleName1))
        {
            return;
        }
        var strs1 = BundleNameSplit(bundleName1);
        var t1 = default(T1);

        if (string.IsNullOrEmpty(bundleName2))
        {
            return;
        }
        var strs2 = BundleNameSplit(bundleName2);
        var t2 = default(T2);

        var count = 0;

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs0[0]), strs0[1], (T0 res) =>
        {
            t0 = res;
            count++;
            if (count == 3)
            {
                callBack(t0, t1, t2);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs1[0]), strs1[1], (T1 res) =>
        {
            t1 = res;
            count++;
            if (count == 3)
            {
                callBack(t0, t1, t2);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);


        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs2[0]), strs2[1], (T2 res) =>
        {
            t2 = res;
            count++;
            if (count == 3)
            {
                callBack(t0, t1, t2);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);
    }

    public static void PrepareResource<T0, T1, T2, T3>(string bundleName0,
                                                       string bundleName1,
                                                       string bundleName2,
                                                       string bundleName3,
                                                       Action<T0, T1, T2, T3> callBack,
                                                       bool clearBundle = true,
                                                       bool cacheResource = true,
                                                       bool firstPriority = false,
                                                       bool fromCache = false)
        where T0 : Object
        where T1 : Object
        where T2 : Object
        where T3 : Object
    {
        if (string.IsNullOrEmpty(bundleName0))
        {
            return;
        }
        var strs0 = BundleNameSplit(bundleName0);
        var t0 = default(T0);

        if (string.IsNullOrEmpty(bundleName1))
        {
            return;
        }
        var strs1 = BundleNameSplit(bundleName1);
        var t1 = default(T1);

        if (string.IsNullOrEmpty(bundleName2))
        {
            return;
        }
        var strs2 = BundleNameSplit(bundleName2);
        var t2 = default(T2);

        if (string.IsNullOrEmpty(bundleName3))
        {
            return;
        }
        var strs3 = BundleNameSplit(bundleName3);
        var t3 = default(T3);

        var count = 0;

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs0[0]), strs0[1], (T0 res) =>
        {
            t0 = res;
            count++;
            if (count == 4)
            {
                callBack(t0, t1, t2, t3);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs1[0]), strs1[1], (T1 res) =>
        {
            t1 = res;
            count++;
            if (count == 4)
            {
                callBack(t0, t1, t2, t3);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);


        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs2[0]), strs2[1], (T2 res) =>
        {
            t2 = res;
            count++;
            if (count == 4)
            {
                callBack(t0, t1, t2, t3);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);

        BundleLoader.Instance.GetBundleResource(AddBundleExt(strs3[0]), strs3[1], (T3 res) =>
        {
            t3 = res;
            count++;
            if (count == 4)
            {
                callBack(t0, t1, t2, t3);
            }
        }, clearBundle, cacheResource, firstPriority, fromCache);
    }

    public static void PrepareScene(string bundleName, Action<WWW> callBack)
    {
        var strs = BundleNameSplit(bundleName);
        if (Instance.UseAssetBundle)
        {
            Instance.StartCoroutine(BundleLoader.Instance.GetSceneResource(AddBundleExt(strs[0]), strs[1], callBack));
        }
        else
        {
            callBack(null);
        }
    }
    public delegate IEnumerator AfterLoadDelegate(string sceneName);
    public static IEnumerator LoadSceneImpl(string sceneName, WWW www, Action afterLoaded = null, AfterLoadDelegate afterLoadDelegate = null)
    {
        Logger.Debug("Before LoadLevelAsync. [{0}]", sceneName);

        if (Instance.UseAssetBundle && www.error != null)
        {
            Logger.Log2Bugly("loadingSceneResource error! error =" + www.error);
            yield break;
        }

        //因为不销毁了,所以切场景之前需要暂停player身上的脚本,否则会有各种报错,例如autocombat
        if (null != ObjManager.Instance.MyPlayer)
        {
            ObjManager.Instance.MyPlayer.gameObject.SetActive(false);
        }

        Application.LoadLevel(sceneName);

        Logger.Debug("Load scene completed. [{0}]", sceneName);
#if UNITY_EDITOR
        var objs = GameObject.FindObjectsOfType<GameObject>();
        for (int i = 0; i < objs.Length; i++)
        {

            var obj = objs[i];
            if ( null != obj && (obj.transform.parent == null) )
            {
                ResourceManager.ChangeShader(obj.transform);
            }
        }
#endif


        if (null != afterLoaded)
        {
            yield return new WaitForEndOfFrame();
            afterLoaded();
        }

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (www != null)
        {
            Logger.Info("Unload scene resource [{0}]", sceneName);
            www.assetBundle.Unload(false);
            www.Dispose();
        }
        //BundleLoader.Instance.mQueueLockerTimes = false;

        if (null != afterLoadDelegate)
        {
            yield return Instance.StartCoroutine(afterLoadDelegate(sceneName));
        }
    }


    public static ResourceHolder<T> PrepareResourceWithHolder<T>(string bundleName,
                                                                 bool clearBundle = true,
                                                                 bool cacheResource = true,
                                                                 bool firstPriority = false,
                                                                 bool fromCache = false,
                                                                 bool forceFromUnityCache = false)
        where T : Object
    {
        //Logger.Debug ("PrepareResourceWithHolder {0}", bundleName);
        var strs = BundleNameSplit(bundleName);
        return BundleLoader.Instance.PrepareResource<T>(AddBundleExt(strs[0]), strs[1], clearBundle, cacheResource,
            firstPriority, fromCache, forceFromUnityCache);
    }

    //读取声音资源
    public delegate void LoadSoundFinish(
        string modelName,
        AudioClip audioClip,
        object param1,
        object param2,
        object param3 = null);

    public static void PrepareSoundResource(string soundFullName,
                                            LoadSoundFinish delFinish,
                                            object param1,
                                            object param2,
                                            object param3)
    {
        PrepareResource<AudioClip>(soundFullName, clip =>
        {
            if (null != delFinish)
            {
                delFinish(soundFullName, clip, param1, param2, param3);
            }
        }, true, false);
    }


    public static string AddBundleExt(string bundleName)
    {
        return bundleName + BUNDLEEXT;
    }

    public static string[] BundleNameSplit(string bundleName)
    {
        var path = Path.Combine(Path.GetDirectoryName(bundleName), Path.GetFileNameWithoutExtension(bundleName));
        path = path.Replace("\\", "/");
        var name = Path.GetFileName(bundleName);
        return new[] {path, name};
    }

    #endregion

    #region cache相关方法

    internal void AddResourcesToCache(string path, Object obj, int size)
    {
        var key = path;

        if (null == obj || String.IsNullOrEmpty(key))
        {
            return;
        }

        LinkedListNode<KeyValuePair<string, ResourceInfo>> node;
        if (mLinkedListNodes.TryGetValue(key, out node))
        {
            mResourcesCache.Remove(node);
            mResourcesCache.AddFirst(node);
            return;
        }

        node = mResourcesCache.AddFirst(new KeyValuePair<string, ResourceInfo>(key, new ResourceInfo(size, obj)));
        mLinkedListNodes.Add(key, node);
        mfCacheMB ++;
    }

    public bool TryGetResourceFromCache(string path, out Object resource)
    {
        var key = path;

        LinkedListNode<KeyValuePair<string, ResourceInfo>> node;
        if (mLinkedListNodes.TryGetValue(key, out node))
        {
            mResourcesCache.Remove(node);
            mResourcesCache.AddFirst(node);

            resource = node.Value.Value.Resource;
            return true;
        }

        resource = null;
        return false;
    }

    public void RemoveFromCache(string path)
    {
        var key = path;

        LinkedListNode<KeyValuePair<string, ResourceInfo>> node;
        if (mLinkedListNodes.TryGetValue(key, out node))
        {
            mResourcesCache.Remove(node);
            mLinkedListNodes.Remove(key);
            mfCacheMB--;
        }
    }

    public void LoadResourceIntoCacheAsync()
    {
        var s = new string[]
        {
            "UI/SceneMap/SceneMap.prefab",
            "UI/Common/EquipPack.prefab",
            "UI/SkillFrame/SkillFrame.prefab",
            "UI/Team/Team/TeamFrame.prefab",
            "UI/Mission/MissionList.prefab",
            "UI/Reward/RewardFrame.prefab",
            "UI/Achievement/AchievementFrame.prefab",
            "UI/HandBook/HandBook.prefab",
            "UI/Dungeon/DungeonFrame.prefab",
            "UI/Rank/RankFrame.prefab",
            "UI/Store/StoreFrame.prefab",
            "UI/Elf/ElfFrame.prefab",
            "UI/Wing/WingFrame.prefab",
            "UI/BattleUnion/BattleUnionFrame.prefab",
            "UI/Activity/Activity.prefab",
            "UI/Play/PlayFrame.prefab",
            "UI/OperationActivity/OperationActivityFrame.prefab",
        };

        foreach (var s1 in s)
        {
          //  AsyncLoadingQueue.Enqueue(s1);
        }

        //StartCoroutine(LoadResourceIntoCacheCoroutine());
    }

    public Queue<string> AsyncLoadingQueue = new Queue<string>();

    public event Action<string> OnAsyncLoadCompleted;
    private bool LoadingCompleted = true;

    private IEnumerator LoadResourceIntoCacheCoroutine()
    {
        LoadingCompleted = false;
        var res = AsyncLoadingQueue.Dequeue();

        if (string.IsNullOrEmpty(res))
        {
            LoadingCompleted = true;
            yield break;
        }

        var strs = BundleNameSplit(res);
        yield return StartCoroutine(BundleLoader.Instance.LoadResourceIntoCacheAsync(AddBundleExt(strs[0]), strs[1]));

        if (OnAsyncLoadCompleted != null)
        {
            try
            {
                OnAsyncLoadCompleted(res);
            }
            catch(Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        LoadingCompleted = true;
    }

    public void ClearCache(bool clearAll = false)
    {
        if (clearAll)
        {
            mLinkedListNodes.Clear();
            mResourcesCache.Clear();
            mfCacheMB = 0;
        }
        else
        {
            if (mfCacheMB < GameSetting.Instance.ResourceCacheMaxSize + 20)
            {
                return;
            }

            while (mfCacheMB > GameSetting.Instance.ResourceCacheMaxSize && mResourcesCache.Last != null)
            {
                mfCacheMB--;
                mLinkedListNodes.Remove(mResourcesCache.Last.Value.Key);
                mResourcesCache.RemoveLast();
            }
        }
    }

    #endregion
}


//加载资源进度条用
public class LoadResourceHelper
{
    public LoadResourceHelper()
    {
        CurLoadedSize = 0;
    }

    private readonly List<ResourceInfo> mListResourceInfo = new List<ResourceInfo>();
    public int CurLoadedSize { get; private set; }

    public int TotalSize
    {
        get { return mListResourceInfo.Count; }
    }

    //添加需要预加载的资源到队列里，永久保留的资源cache用false,commonbundle不能clearbundle
    public void AddLoadInfo(string resourcePath,
                            string resourceName,
                            bool clearBundle = true,
                            bool cache = true,
                            bool sync = false,
                            bool compressed = true)
    {
        var info = new ResourceInfo
        {
            ResourcePath = resourcePath,
            ResourceName = resourceName,
            bClearBundle = clearBundle,
            bCache = cache,
            Async = sync,
            bCompressed = compressed
        };
        mListResourceInfo.Add(info);
    }

    public void AddLoadInfo(ResourceInfo info)
    {
        mListResourceInfo.Add(info);
    }

    public void AddLoadInfo(string fullPath, bool clearBundle = true, bool cache = true, bool sync = false, bool compressed = true)
    {
        var strs = ResourceManager.BundleNameSplit(fullPath);
        var info = new ResourceInfo
        {
            ResourcePath = strs[0],
            ResourceName = strs[1],
            bClearBundle = clearBundle,
            bCache = cache,
            Async = sync,
            bCompressed = compressed
        };
        mListResourceInfo.Add(info);
    }

    //开始加载，加载完成后会自动清理，可以再次使用
    public void BeginLoad(Action finish)
    {
        LoadResourceImpl(mListResourceInfo.GetEnumerator(), finish);
    }

    //获取加载进度
    public float GetLoadingPrecent()
    {
        if (TotalSize < 1)
        {
            return 0;
        }
        return (float) CurLoadedSize/TotalSize;
    }

    private static int step =100;
    private void LoadResourceImpl(IEnumerator<ResourceInfo> enumerator, Action finish)
    {
        if (!enumerator.MoveNext())
        {
            try
            {
                if (finish != null)
                {
                    finish();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            return;
        }
        step++;
        var info = enumerator.Current;
      

        var extension = Path.GetExtension(info.ResourceName);
        if (extension != null && extension.Equals(".prefab"))
        {
            ResourceManager.PrepareResource<GameObject>(info.ResourcePath, res =>
            {
                CurLoadedSize++;

                LoadResourceImpl(enumerator, finish);
            }, info.bClearBundle, info.bCache, info.Async, info.bCompressed);
        }
        else
        {
            ResourceManager.PrepareResource<Object>(info.ResourcePath, res =>
            {
                CurLoadedSize++;

                LoadResourceImpl(enumerator, finish);
            }, info.bClearBundle, info.bCache, info.Async, info.bCompressed);
        }
    }

    public struct ResourceInfo
    {
        public bool Async;
        public bool bCache;
        public bool bClearBundle;
        public bool bCompressed;
        public string ResourceName;
        public string ResourcePath;
    }
}