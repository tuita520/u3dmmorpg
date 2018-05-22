#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

public class Game : MonoBehaviour
{
    public static Game Instance;
    public GameObject DelayShowObj;
    private List<IManager> mMgrList;
    public RechargeActivityData RechargeActivityData;
    public bool ServerInfoCached;
    public DateTime LoginTime { set; private get; }

    public List<IManager> MgrList
    {
        get { return mMgrList; }
        private set { }
    }

    public int OnLineSeconds
    {
        get { return (int) (ServerTime - LoginTime).TotalSeconds; }
    }

    public DateTime ServerTime
    {
        get { return DateTime.Now - ServerTimeDiff; }
    }

    public TimeSpan ServerTimeDiff { set; get; }
    public TimeSpan ServerZoneDiff { set; get; }

    private void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif

#if UNITY_EDITOR
        GameObject rm = null;
        rm = GameObject.Find("ResourceManager");
        while (rm)
        {
            DestroyImmediate(rm);
            rm = GameObject.Find("ResourceManager");
        }

#endif


#if !UNITY_EDITOR
        StartCoroutine(DelayShowScreen());
        PlatformHelper.PlayLogoMovie();
#endif
        TargetBindingProperty.Register();

        // var channel = PlatformHelper.GetChannelString();
        // Logger.Debug("PlatformHelper.GetChannelString = " + channel);
        GameSetting.Channel = GameUtils.GetChannelString();

        PlatformHelper.Initialize();
        if (null != Instance)
        {
            Logger.Fatal("ERROR!!!!!!!!!!!!!!!!!!! Game has been created");
            return;
        }
        Instance = this;

        var Fps = PlayerPrefs.GetInt(GameSetting.LowFpsKey, 60);
        Application.targetFrameRate = Fps;
#if UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif
        //注册所有解析表中表达式中的函数
        ExpressionHelper.RegisterAllFunction();
        PlayCG.Instance.Init();

        mMgrList = new List<IManager>
        {
            ObjManager.Instance,
            SceneManager.Instance,
            PlayerDataManager.Instance,
            ResourceManager.Instance,
            EffectManager.Instance,
            UIManager.Instance,
            SoundManager.Instance
        };

        DontDestroyOnLoad(gameObject);


#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    /// <summary>
    ///     更新完成后，重新加载资源前调用
    /// </summary>
    public void BeforeStartLoading()
    {
        CleanupAllGameData();
    }

    public void ChangeSceneToLogin()
    {
        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ServerListUI));

        Action act = () =>
        {
            CleanupAllGameData(false);
            const string sceneName = "Login";
            ResourceManager.PrepareScene(Resource.GetScenePath(sceneName), www =>
            {
                LoginLogic.State = LoginLogic.LoginState.BeforeLogin;
                ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl(sceneName, www));
            });

        };

        GameUtils.ExitLogin(act);


//         if (LoginLogic.instance )
//         {
//             const string sceneName = "Login";
//             ResourceManager.PrepareScene(Resource.GetScenePath(sceneName), (www) =>
//             {
//                 LoginLogic.State = LoginLogic.LoginState.BeforeLogin;
//                 ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl(sceneName, www));
//             });    
//         }
//         else
//         {bi
//             LoginLogic.State = LoginLogic.LoginState.BeforeLogin;
//             LoginLogic.instance.Init();    
//         }
    }

    public void ChangeSceneToLoginAndAutoLogin(Action afterChange)
    {
        CleanupAllGameData();
        const string sceneName = "Login";
        ResourceManager.PrepareScene(Resource.GetScenePath(sceneName), www =>
        {
            LoginLogic.State = LoginLogic.LoginState.ThirdLogin;
            LoginLogic.ThirdLoginAction = afterChange;
            ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl(sceneName, www));
        });
    }

    private void CleanupAllGameData(bool stopNet = true)
    {
        try
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_VisibleEyeCanBeStart(false));
            ServerInfoCached = false;
            PlatformHelper.CloseToolBar();
            //正在loading过程中,如果被踢下线,删除掉全局的uiroot
             if (null != LoadingLogic.Instance)
             {
                 var loadingRoot = GameObject.Find("LoadingObject");
                 if (null != loadingRoot)
                 {
                     Destroy(loadingRoot);
                 }
             }


             if (null != ObjManager.Instance.MyPlayer)
             {
                 Destroy(ObjManager.Instance.MyPlayer.gameObject);
             }
            

            var bc = Instance.GetComponent<BrightnessController>();
            if (bc)
            {
                bc.OnTouchOrMouseRelease();
            }

            NetManager.Instance.SyncCenter.Clear();
            EventDispatcher.Instance.RemoveAllEventListeners();
            CleanUpManagers();
            NetManager.Instance.StopAllCoroutines();
            ConditionTrigger.Instance.Init();
            TimeManager.Instance.CleanUp();
            PlayerAttr.Instance.CleanUp();
            UIManager.Instance.ClearCacheUI();
            if (stopNet)
            {
                NetManager.Instance.Stop();
            }
        }
        catch (Exception e)
        {
            Logger.Log2Bugly("----CleanupAllGameData throw exception:{0}", e);
            throw;
        }
    }

    public void CleanUpManagers()
    {
        {
            var __list3 = Instance.MgrList;
            var __listCount3 = __list3.Count;
            for (var __i3 = 0; __i3 < __listCount3; ++__i3)
            {
                var manager = __list3[__i3];
                {
                    manager.Reset();
                }
            }
        }
    }

    //异步播放片头logo会显示一帧游戏画面,所以先隐藏一秒之后再显示
    private IEnumerator DelayShowScreen()
    {
        if (null == DelayShowObj)
        {
            yield break;
        }

        DelayShowObj.SetActive(false);
        yield return new WaitForSeconds(1);
        DelayShowObj.SetActive(true);
    }

    public void EnterStartup()
    {
       //PlatformHelper.UserLogout();
        CleanupAllGameData();
        ResourceManager.Instance.UnloadCommonBundle();
        ResourceManager.Instance.ClearCache(true);
        var game = GameObject.Find("Game");
        DestroyImmediate(game);
        Application.LoadLevel("Startup");
    }

    private IEnumerator ExitSelectCharacterCoroutine(Action callback = null)
    {
        var characterId = PlayerDataManager.Instance.CharacterGuid;

        if (characterId == 0ul)
        {
            if (null != callback)
            {
                callback();
            }
            yield break;
        }

        var msg = NetManager.Instance.ExitSelectCharacter(characterId);
        UIManager.Instance.ShowBlockLayer();
        yield return msg.SendAndWaitUntilDone();
        if (msg.State == MessageState.Reply)
        {
            if (msg.ErrorCode == (int) ErrorCodes.OK)
            {
                ObjManager.Instance.Reset();
                PlayerDataManager.Instance.CharacterLists = msg.Response.Info;
                PlayerDataManager.Instance.SelectedRoleIndex = msg.Response.SelectId;
                PlayerDataManager.Instance.CharacterGuid = 0ul;
                //umeng登出
                PlatformHelper.ProfileSignOff();
                if (null != callback)
                {
                    callback();
                }
            }
            else
            {
                UIManager.Instance.RemoveBlockLayer();
                Logger.Error(".....ExitSelectCharacter.......{0}.", msg.ErrorCode);
                //UIManager.Instance.ShowMessage(MessageBoxType.Ok, "ExitSelectCharacter error:"+ msg.ErrorCode);
                ExitToLogin();
            }
        }
        else
        {
            UIManager.Instance.RemoveBlockLayer();
            Logger.Error(".....ExitSelectCharacter.......time out!");
            ExitToLogin();
            //UIManager.Instance.ShowMessage(MessageBoxType.Ok, "ExitSelectCharacter time out!");
        }
    }

    //所有退回登陆界面总入口
    public void ExitToLogin()
    {
        if (GameUtils.IsOurChannel())
        {
            ChangeSceneToLogin();
        }
        else
        {
            PlatformHelper.UserLogout();
        }
		GVoiceManager.Instance.QuitRoom(true);
        EraManager.Instance.Clear();
        PlayerDataManager.Instance.IsCheckSailingTip = false;        
    }

    public void ExitToSelectRole()
    {
        Action action = () =>
        {
            CleanupAllGameData(false);
            ResourceManager.PrepareScene(Resource.GetScenePath("SelectCharacter"), www =>
            {
                ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl("SelectCharacter", www, () =>
                {
                    LoginLogic.State = LoginLogic.LoginState.LoginSuccess;
                    var serverName = string.Empty;
                    var controller = UIManager.Instance.GetController(UIConfig.ServerListUI);
                    if (null != controller)
                    {
                        var datamodel = controller.GetDataModel("") as ServerListDataModel;
                        if (null != datamodel)
                        {
                            serverName = datamodel.SelectedServer.ServerName;
                        }
                    }

                    
                }));
            });
        };
        NetManager.Instance.StartCoroutine(ExitSelectCharacterCoroutine(action));

		GVoiceManager.Instance.QuitRoom(true);
        EraManager.Instance.Clear();
        PlayerDataManager.Instance.IsCheckSailingTip = false;        
    }

    public void ExitToServerList(bool bQuick = false)
    {
        Action action = () =>
        {
            const string sceneName = "Login";
            ResourceManager.PrepareScene(Resource.GetScenePath(sceneName), www =>
            {
                LoginLogic.State = LoginLogic.LoginState.LoginSuccess;
                ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl(sceneName, www, () =>
                {
                    if (ServerInfoCached && bQuick)
                    {
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ServerListUI));
                    }
                    else
                    {
                        CleanupAllGameData(false);
                        NetManager.Instance.StartCoroutine(LoginLogic.LoginSuccess());
                    }
                }));
            });
        };

        NetManager.Instance.StartCoroutine(ExitSelectCharacterCoroutine(action));
		GVoiceManager.Instance.QuitRoom(true);
        EraManager.Instance.Clear();
        PlayerDataManager.Instance.IsCheckSailingTip = false;  
    }

    private void LateUpdate()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        //         if (ChangeSceneList.Count > 0 && ChangeScenestate == eChangeSceneState.Finished)
        //         {
        //             Action act = ChangeSceneList.Dequeue();
        //             act();
        //         }        

        Profiler.BeginSample("UIRect.Update()");
        UIRect.Update();
        Profiler.EndSample();

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    private void LocalNotificationTest()
    {
//         PlatformHelper.ClearAllLocalNotification();
//         PlatformHelper.SetLocalNotification("key1","key1 测试本地通知,这条消息应该在启动游戏一分钟后弹出!",60);
//         PlatformHelper.SetLocalNotification("key2", "key2 测试本地通知,这条消息你看不到!", 120);
//         PlatformHelper.DeleteLocalNotificationWithKey("key2");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
#if !UNITY_EDITOR

        if (LoginLogic.State != LoginLogic.LoginState.InGaming)
        {
            return;
        }

        if (!pauseStatus)
        {
            NetManager.Instance.ResumeStop();
            if (!NetManager.Instance.Connected)
            {
                //正在重新连接...
                //Debug.LogError("正在重新连接...");
      
                // this.StartCoroutine(NetManager.Instance.OnServerLost());
                NetManager.Instance.IsReconnecting = false;
                Game.Instance.ExitToLogin();
            }
        }
        else
        {
            if (NetManager.Instance.Connected && ObjManager.Instance.MyPlayer != null)
            {
                ObjManager.Instance.MyPlayer.StopMove();
            }

            NetManager.Instance.NeedReconnet = true;
            NetManager.Instance.StopAfter(1000*60*4);
        }

		GVoiceManager.Instance.OnApplicationPause(pauseStatus);
#endif
	}

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        {
            var __list2 = mMgrList;
            var __listCount2 = __list2.Count;
            for (var __i2 = 0; __i2 < __listCount2; ++__i2)
            {
                var mgr = __list2[__i2];
                {
                    if (mgr != null)
                    {
                        try
                        {
                            mgr.Destroy();
                        }
                        catch (Exception e)
                        {
                           //
                        }
                    }
                }
            }
        }
        mMgrList.Clear();

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public static void SetGameQuality()
    {
        var defaultQualityLevel = 3;

#if UNITY_IOS
        {
            iPhoneGeneration iOSGen = iPhone.generation;
            if (iOSGen == iPhoneGeneration.iPhone3GS)
            {
                defaultQualityLevel = 1;
            }
            else if (iOSGen == iPhoneGeneration.iPhone4)
            {
                defaultQualityLevel = 1;
            }
            else if (iOSGen == iPhoneGeneration.iPhone4S)
            {
                defaultQualityLevel = 1;
            }
            else if (iOSGen == iPhoneGeneration.iPhone5 || iOSGen == iPhoneGeneration.iPhone5C)
            {
                defaultQualityLevel = 2;
            }
            else if (iOSGen == iPhoneGeneration.iPhone5S || iOSGen == iPhoneGeneration.iPhone6 ||
                     iOSGen == iPhoneGeneration.iPhone6Plus)
            {
                defaultQualityLevel = 3;
            }
            else if (iOSGen == iPhoneGeneration.iPad1Gen)
            {
                defaultQualityLevel = 1;
            }
            else if (iOSGen == iPhoneGeneration.iPad2Gen)
            {
                defaultQualityLevel = 1;
            }
            else if (iOSGen == iPhoneGeneration.iPad3Gen)
            {
                defaultQualityLevel = 2;
            }
            else if (iOSGen == iPhoneGeneration.iPodTouch3Gen)
            {
                defaultQualityLevel = 2;
            }
            else if (iOSGen == iPhoneGeneration.iPodTouch4Gen)
            {
                defaultQualityLevel = 2;
            }
            else
            {
                string device = SystemInfo.deviceModel;

                Logger.Debug("Current device is :" + device);
                if (device == "iPhone")
                {
                    defaultQualityLevel = 3;
                }
                else if (device == "iPad")
                {
                    defaultQualityLevel = 3;
                }
                else if (device == "iPod")
                {
                    defaultQualityLevel = 1;
                }
            }
    
        }
#endif

#if UNITY_ANDROID
        {
            var ram = SystemInfo.systemMemorySize;
            // var vram = SystemInfo.graphicsMemorySize;
            var cpus = SystemInfo.processorCount;

            if (cpus > 4 && ram >= 2800)
            {
                defaultQualityLevel = 3;
            }
            else if (ram >= 1900)
            {
                defaultQualityLevel = 2;
            }
            else
            {
                defaultQualityLevel = 1;
            }
        }
#endif
    if (null == GameSetting.Instance)
        {
            Logger.Error("GameSetting.Instance = null , call SetGameQuality too early!");
            return;
        }
        var level = PlayerPrefs.GetInt(GameSetting.GameQuilatyKey, defaultQualityLevel);
        if (level < 1)
        {
            level = defaultQualityLevel;
        }
        GameSetting.Instance.GameQualityLevel = level;
    }

    private void Start()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        Logger.LogLevel = GameSetting.Instance.LogLevel;

        //进入游戏后在设置画质
        //SetGameQuality();


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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlatformHelper.Exit();
        }

        {
            var __list1 = mMgrList;
            var __listCount1 = __list1.Count;
            for (var __i1 = 0; __i1 < __listCount1; ++__i1)
            {
                var mgr = __list1[__i1];
                {
                    try
                    {
                      //  Profiler.BeginSample(mgr.GetType().ToString());
                        mgr.Tick(Time.deltaTime);
                      //  Profiler.EndSample();
                    }
                    catch
                    {
                        // some mgr failed.
                    }
                }
            }
        }

        Profiler.BeginSample("TimeManager.Updata()");
        TimeManager.Instance.Updata();
        Profiler.EndSample();

        Profiler.BeginSample("GVoiceManager.Updata()");
        GVoiceManager.Instance.Update();
        Profiler.EndSample();

        Profiler.BeginSample("AnimationUpdateFrequencyController.Tick()");
        AnimationUpdateFrequencyController.Tick();
        Profiler.EndSample();

        ObjBase.UpdateVisiblity();

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }
}