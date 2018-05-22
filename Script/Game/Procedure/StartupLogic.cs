#region using

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using ScriptManager;
using DataTable;
using GameUI;
using UnityEngine;
using Debug = UnityEngine.Debug;

#endregion

public class StartupLogic : MonoBehaviour
{
    public static StartupLogic Instance;
   
    public LoadResourceHelper helper = new LoadResourceHelper();
    private float mCacheSize;
    private float mExLoadingPercent;
   
    private string UpdateUrl;
	private UpdateHelper updateHelper;

    public Stopwatch sw = new Stopwatch();
    public Stopwatch sw2 = new Stopwatch();
    private bool cellularLateUpdate = false;
    private bool cellularUpdate = false;

	public StartupWindow StartupView;

    private void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        sw.Start();
        sw2.Start();
        Instance = this;
        initVersion();
#if !UNITY_EDITOR 
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public void printTime(object tag)
    {
        return;
        Logger.Info(string.Format(" step: {0} spend: {1}", tag, sw2.Elapsed.TotalSeconds));
        sw2.Reset();
        sw2.Start();
    }

    private void initVersion()
    {
        var version = "-1";
        string versionConfig;
        var gameVersionPath = Path.Combine(Application.streamingAssetsPath, "Game.ver");
        //先读包内版本号
        if (GameUtils.GetStringFromPackage(gameVersionPath, out versionConfig))
        {
            var config = versionConfig.Split(',');
            var resourceVersion = config[4];

            //读取之前更新过的版本号
            var downLoadVersionPath = Path.Combine(UpdateHelper.DownloadRoot, "Resources.ver"); 
            if (File.Exists(downLoadVersionPath))
            {
                int localVersion;
                if (GameUtils.GetIntFromFile(downLoadVersionPath, out localVersion))
                {
                    resourceVersion = localVersion.ToString();
                }
            }

            version = string.Format("version {0}.{1}", config[3], resourceVersion);
        }

		StartupView.VersionLabel.text = version;
    }

    private void BegainLoading()
    {
        printTime(2);
        // 
        GameUtils.RecordKeyPoint("BeginLoading", 13);

        //editor在不用bundle下不用加载common
        if (ResourceManager.Instance.UseAssetBundle)
        {
            {
                var __array1 = GameSetting.Instance.CommonBundleList;
                var __arrayLength1 = __array1.Length;
                for (var __i1 = 0; __i1 < __arrayLength1; ++__i1)
                {
                    var str = __array1[__i1];
                    {
                        helper.AddLoadInfo(str, false, false, true);
                    }
                }
            }
        }

        {
            var __list2 = GameSetting.Instance.ResourceList;
            var __listCount2 = __list2.Count;
            for (var __i2 = 0; __i2 < __listCount2; ++__i2)
            {
                var str = __list2[__i2];
                {
                    helper.AddLoadInfo(str);
                }
            }
        }

        //表太多了,先把缓存扩大,加载表格完成后再把缓存改回来
        mCacheSize = GameSetting.Instance.ResourceCacheMaxSize;
        GameSetting.Instance.ResourceCacheMaxSize = 500f;

        //速度慢等待提示
        if (PlayerPrefs.GetInt(GameSetting.ShowWaitingTipKey, 0) == 0)
        {
			if (null != StartupView.WaitingTip)
            {
				StartupView.WaitingTip.SetActive(true);
            }
        }

        printTime(3);

        helper.BeginLoad(() =>
        {
            StartCoroutine(OnLoadOver());
        });
    }

    public void ErrorTipButton()
    {
        updateHelper = null;
		StartupView.ErrorTip.gameObject.SetActive(false);
        Start();
    }

    public float GetLoadingPercent()
    {
#if !UNITY_EDITOR
try
{
#endif

        return helper.GetLoadingPrecent()*0.7f + mExLoadingPercent*0.3f;
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
    return 0;
}
#endif
    }

    private void OnCheckResVersion(UpdateHelper.CheckVersionResult result, string message)
    {
        var action = new Action(() =>
        {
            if (result == UpdateHelper.CheckVersionResult.NONEEDUPDATE)
            {
                BegainLoading();
            }
            else if (result == UpdateHelper.CheckVersionResult.NEEDUPDATE)
            {
				StartCoroutine(updateHelper.UpdateMd5List(OnUpdateMd5List));
            }
            else if (result == UpdateHelper.CheckVersionResult.GAMENEEDUPDATE)
            {
                UpdateUrl = message;
                StartupView.BigUpdateTip.SetActive(true);
            }
            else if (UpdateHelper.CheckVersionResult.ERROR == result)
            {
                ShowErrorTip(message);
            }
        }
            );

        if (GameSetting.Instance.ForceShowAnn == 1)
        {
            AnnouncementHelper.ShowAnnouncement(UpdateHelper.AnnoucementURL, action);
        }
        else
        {
            action();
        }
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        try
        {
#endif

        Instance = null;
		GameObject.Destroy(StartupView.gameObject);
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public void OnGotoUpdate()
    {
        Application.OpenURL(UpdateUrl);
    }

    private IEnumerator OnLoadOver()
    {
        GameUtils.RecordKeyPoint("EndLoading", 16);

        var saveCo = StartCoroutine(TableManager.SaveTableToCache());
        yield return saveCo;

        PlayerPrefs.SetInt(GameSetting.ShowWaitingTipKey, 1);

        printTime(4);
        mExLoadingPercent = 0.3f;
	    StartupView.Percent = 0.3f;
        {
            var __list3 = Game.Instance.MgrList;
            var __listCount3 = __list3.Count;
            for (var __i3 = 0; __i3 < __listCount3; ++__i3)
            {
                var mgr = __list3[__i3];
                {
                    var co = StartCoroutine(mgr.Init());
                    yield return co;
                }
            }
        }
        printTime(5);

		
        //加载Lua bundle
//         yield return StartCoroutine(LuaManager.Instance.LoadLuaRes());
// 
// 
//         try
//         {
//             //初始化Lua
//             LuaManager.Instance.InitLua();
//             LuaComponent.s_luaState = LuaManager.Instance.Lua;
//             LuaManager.Instance.Lua.DoFile("Main.lua");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError(ex.ToString());
//         }
// 
//         printTime(6);
		
        mExLoadingPercent = 0.5f;
		StartupView.Percent = 0.5f;
        try
        {
            Table.Init();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }

        while (!TableManager.IsFinish())
        {
            yield return new WaitForEndOfFrame();
        }
        
        printTime(7);

        try
        {
            mExLoadingPercent = 0.6f;
			StartupView.Percent = 0.6f;
            GameSetting.Instance.ResourceCacheMaxSize = mCacheSize;
            ExpressionHelper.initializeStaticString();
            Game.Instance.BeforeStartLoading();
            PlayerDataManager.InitExtDataEvent();
            Dijkstra.Init();
            GameUtils.Init();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }

        printTime(8);

        yield return new WaitForEndOfFrame();
        mExLoadingPercent = 0.7f;
		StartupView.Percent = 0.7f;
        const string sceneName = "Login";
        ResourceManager.PrepareScene(Resource.GetScenePath(sceneName), www =>
        {
            mExLoadingPercent = 1.0f;
			StartupView.Percent = 1.0f;
            LoginLogic.State = LoginLogic.LoginState.BeforeLogin;
            ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl(sceneName, www,
                () =>
                {
                    printTime(9);
                    Debug.Log("enter login scene spend time:" + sw.Elapsed.TotalSeconds);
                }));
        });


//        if (1 == Table.GetClientConfig(1154).ToInt())
//        {
//#if !UNITY_EDITOR
//#if UNITY_ANDROID || UNITY_IOS
//            GVoiceManager.Instance.Init();
//#endif
//#endif
//        }
    }

    private void OnUpdateFinish(bool success, string message = "")
    {
        if (success)
        {
            if (cellularLateUpdate)
            {
                GameUtils.RecordKeyPoint("LateUpdateFinish", 10);
            }

            if (cellularUpdate)
            {
                GameUtils.RecordKeyPoint("UpdateFinish", 12);
            }


            if (!string.IsNullOrEmpty(message))
            {
               // ShowRestTip(message);
                PlatformHelper.RestartApp();
            }
            else
            {
                StartupView.ProgressBar.gameObject.SetActive(true);
				StartupView.UpdatePanel.SetActive(false);
                BegainLoading();
            }
        }
        else
        {
            ShowErrorTip(message);
        }
    }

    private void OnUpdateMd5List(UpdateHelper.UpdateResult result, string message)
    {
        if (result == UpdateHelper.UpdateResult.GetMd5ListFail)
        {
            ShowErrorTip(message);
            return;
        }



        if (UpdateHelper.CheckWiFi())
        {
            StartupView.WifiTip.SetActive(false);
            StartupView.UpdatePanel.SetActive(true);
            StartCoroutine(updateHelper.StartUpdateAll(OnUpdateFinish));
        }
        else
        {
            StartupView.TotalSize.text = message;
            switch (result)
            {
                case UpdateHelper.UpdateResult.GetMd5ListSuccess:
                    {
                        cellularUpdate = true;
                        if (cellularUpdate)
                        {
                            GameUtils.RecordKeyPoint("UpdateTip", 11);
                        }

                        float downloadSize;
                        if (float.TryParse(message, out downloadSize))
                        {
                            if (downloadSize < 1.5f)
                            {
                                StartupView.WifiTip.SetActive(false);
                                StartupView.UpdatePanel.SetActive(false);
                                StartCoroutine(updateHelper.StartUpdateAll(OnUpdateFinish));
                                break;
                            }
                        }
                        StartupView.WifiTip.SetActive(true);
                        StartupView.ProgressBar.gameObject.SetActive(false);
                        StartupView.LateUpdateTip.gameObject.SetActive(false);
                        StartupView.UpdateTip.gameObject.SetActive(true);

                    }
                    break;
                case UpdateHelper.UpdateResult.GetMd5ListSuccessAndLateUpdate:
                    StartupView.WifiTip.SetActive(true);
                    StartupView.LateUpdateTip.gameObject.SetActive(true);
                    StartupView.UpdateTip.gameObject.SetActive(false);
                    cellularLateUpdate = true;
                    if (cellularLateUpdate)
                    {
                        GameUtils.RecordKeyPoint("LateUpdateTip", 9);
                    }
                    break;
            }
        }
    }

    private void ShowErrorTip(string message)
    {
		StartupView.ErrorLabel.text = message;
		StartupView.ErrorTip.SetActive(true);
    }

    private void ShowRestTip(string message)
    {
		StartupView.ResetLabel.text = message;
		StartupView.ResetTip.SetActive(true);
    }

    public void ResetTipOk()
    {
        PlatformHelper.RestartApp();
    }

    private void ShowLogo()
    {
    }

    // Use this for initialization
    private void Start()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        printTime(1);
        updateHelper = new UpdateHelper();
        if (GameSetting.Instance.UpdateEnable)
        {
            // 
            GameUtils.RecordKeyPoint("StartUpdate", 8);
            updateHelper.CheckVersion(OnCheckResVersion, true);
        }
        else
        {
            OnUpdateFinish(true);
        }

		StartupView.UpdatePanel.SetActive(false);

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    // Update is called once per frame
    private void Update()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (null != updateHelper)
        {
			StartupView.UpdateProgress.value = updateHelper.UpdatePrecent;
			StartupView.ProgressLabel.text = string.Format("{0}/{1}", updateHelper.DownloadedSize, updateHelper.TotalSize);
			StartupView.CountLabel.text = string.Format("{0}/{1}", updateHelper.CurrentCount, updateHelper.TotalCount);
			StartupView.StatusLabel.text = updateHelper.UpdateStatus;
        }

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public void WifiTipCancel()
    {
        Application.Quit();
    }

    public void WifiTipOk()
    {
#if !UNITY_EDITOR
try
{
#endif

		StartupView.WifiTip.SetActive(false);
		StartupView.UpdatePanel.SetActive(true);
        StartCoroutine(updateHelper.StartUpdateAll(OnUpdateFinish));

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
	
}
