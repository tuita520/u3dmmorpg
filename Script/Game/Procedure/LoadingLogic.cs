#region using

using System;
using System.Collections;
using System.Diagnostics;
using ScriptManager;
using DataTable;
using EventSystem;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

public class LoadingLogic : MonoBehaviour
{
    public static LoadingLogic Instance;
    private AsyncOperation ChangeSceneAsync;
    public float LoadingDelay = 0.5f;
    private float mLoadingProgressReal;
    private float mLoadingProgressShow;

	private GameObject LoadingRoot;
    private void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        Instance = this;
        var res = ResourceManager.PrepareResourceSync<GameObject>("UI/LoadingWindow");
		LoadingRoot = Instantiate(res) as GameObject;
		LoadingRoot.name = res.name;
		//LoadingRoot.transform.parent = UIManager.Instance.GetUIRoot(UIType.TYPE_BASE);
	    LoadingRoot.transform.parent = UIManager.Instance.UIRoot.transform;
        LoadingRoot.transform.localPosition = new Vector3(0, 0, -3000);
		LoadingRoot.transform.localScale = Vector3.one;
		LoadingRoot.transform.rotation = Quaternion.identity;

		EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.1f));
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    public float GetLoadingProgress()
    {
        return mLoadingProgressShow;
    }

    private IEnumerator AfterLoad(string sceneName)
    {

		EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.2f));

		Stopwatch sw = new Stopwatch();
		sw.Start();

         if (ResourceManager.Instance.UseAssetBundle)
        {
//             UIManager.Instance.PreLoadUI(UIConfig.ElfUI);
//             EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.45f));
//             yield return new WaitForEndOfFrame();

            UIManager.Instance.PreLoadUI(UIConfig.WingUI);
            EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.50f));
            yield return new WaitForEndOfFrame();

            UIManager.Instance.PreLoadUI(UIConfig.HandBook);
            EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.55f));
        }

		sw.Stop();
		Logger.Debug("AfterLoad.1-----------------"+sw.ElapsedMilliseconds);
        if (GameSetting.Instance.LoadingProcessGameInit)
        {
			sw.Reset();
			sw.Start();
            if (null != GameLogic.Instance)
            {
                yield return ResourceManager.Instance.StartCoroutine(GameLogic.Instance.EnterGameCoroutine());
            }
			EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(1f));
	        yield return new WaitForEndOfFrame();
			sw.Stop();
			Logger.Debug("AfterLoad.2-----------------" + sw.ElapsedMilliseconds);
            try
            {
				sw.Reset();
				sw.Start();
				/*
                if (null != DestroyObj)
                {
                    Destroy(DestroyObj);
                }
				 * */
				if (null != gameObject)
                {
                    Destroy(gameObject);
                }
				
                Resources.UnloadUnusedAssets();
                GC.Collect();
                //LuaManager.Instance.Lua.Collect();
                EventDispatcher.Instance.DispatchEvent(new UIEvent_RefleshNameBoard());

				sw.Stop();
				Logger.Debug("AfterLoad.3-----------------" + sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Logger.Error("LoadSceneImpl------------\n" + e.Message);
            }
        }
    }

    // Update is called once per frame
    private void OnDestroy()
    {
#if !UNITY_EDITOR
try
{
#endif
		GameObject.Destroy(LoadingRoot);
		 Instance = null;
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    public void SetLoadingProgress(float progress)
    {
        mLoadingProgressReal = progress;
        if (Math.Abs(progress - 1.0f) < 0.001)
        {
            mLoadingProgressShow = 1.0f;
        }
    }

    // Use this for initialization
    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif
        mLoadingProgressReal = 0.5f;
        mLoadingProgressShow = 0;

        var tbscene = Table.GetScene(SceneManager.Instance.CurrentSceneTypeId);
        var sceneName = tbscene.ResName;

        ResourceManager.PrepareScene(Resource.GetScenePath(sceneName), www =>
        {
            if (!ResourceManager.Instance.UseAssetBundle || www.error == null)
            {
                ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl(sceneName, www,null, AfterLoad));
            }
        });

        if (true == GameSetting.Instance.LoadingProcessGameInit)
        {
            DontDestroyOnLoad(gameObject);
        }
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (mLoadingProgressShow < mLoadingProgressReal)
        {
            mLoadingProgressShow += Random.Range(0.0555f, 0.0666f);
        }

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
}