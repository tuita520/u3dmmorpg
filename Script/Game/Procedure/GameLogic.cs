#region using

using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Script.Camera;
using System.Diagnostics;
using ScriptManager;
using BehaviourMachine;
using ClientService;
using DataTable;
using EventSystem;
using FastShadowReceiver;
using GameUI;
using ScorpionNetLib;
using Shared;
using Thinksquirrel.Utilities;
using UnityEngine;

#endregion

public class GameLogic : MonoBehaviour
{
    #region 成员

    //单例(注意他的生命周期只在游戏场景)
    public static GameLogic Instance;

    //主摄像机
    public Camera MainCamera = null;

    [HideInInspector] public Scene Scene;

    [HideInInspector] public SceneEffectManager SceneEffect;

    [HideInInspector] public GuideTrigger GuideTrigger;

    [HideInInspector]
    public int MultiTouch { get; set; }

    [HideInInspector]
    public float MultiDistance { get; set; }

    [NonSerialized] public float[] MultiPos = {0, 0};

    private BattleSkillRootFrame SkillBar;

    //ShadowReceiver Mask，保存一下，用以获得地面高度的射线
    private static int ShadowReceiverLayerMask = -1;
    public static bool IsFristLogin;
	public static bool HasCache = false;

    //是否是新创建的角色进来的，是就播放CG （被CLSharp调用的不要用bool）
    public static int PlayFirstEnterGameCG;
    public bool LoadOver { get; private set; }

    public string ScenePrefab;

    private GameControl mControl;

    #endregion

    #region Mono

    private void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif

        Instance = this;
        MainCamera.gameObject.AddComponent<CameraController>();
        MainCamera.gameObject.AddComponent<CameraShake>();
        gameObject.AddComponent<InputManager>();
        if (GameSetting.Instance.RenderTextureEnable)
        {
            try
            {
                var uicam = UIManager.Instance.UICamera;
                if (null != uicam)
                {
                    var rtc = uicam.gameObject.GetComponent<RenderTextureCreator>();
                    if (null == rtc)
                    {
						rtc = uicam.gameObject.AddComponent<RenderTextureCreator>();
                    }
	                rtc.enabled = true;
                    rtc.BindCamera();
                }
                else
                {
                    Logger.Error("cant find UIRoot/Camera");
                }
            }
            catch (Exception e)
            {
                Logger.Error("cant find uicamera");
            }

        }
        //Scene = gameObject.GetComponent<Scene>();
        SceneEffect = gameObject.AddComponent<SceneEffectManager>();
        GuideTrigger = gameObject.AddComponent<GuideTrigger>();
        mControl = gameObject.AddComponent<GameControl>();
        var mTime = Game.Instance.ServerTime;

        ShadowReceiverLayerMask = LayerMask.GetMask(GAMELAYER.ShadowReceiver, GAMELAYER.Terrain);

        LoadOver = false;

        EventDispatcher.Instance.AddEventListener(DungeonCompleteEvent.EVENT_TYPE, OnDungeonComplete);
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

 
    private void Start()
    {
#if !UNITY_EDITOR
        try
        {
#endif


        if (true != GameSetting.Instance.LoadingProcessGameInit)
        {
            StartCoroutine(EnterGameCoroutine());
        }
        bool flag = GameSetting.Instance.ShowOtherPlayer;
        GameSetting.Instance.ShowOtherPlayer = flag;
        //InvokeRepeating("OnLineTickSecond", 1, 1);
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

//     void OnLineTickSecond()
//     {
//         EventDispatcher.Instance.DispatchEvent(new UIEvent_PerSecond());
// 
//         if (0 == OnLineSeconds % 60)
//         {
//             EventDispatcher.Instance.DispatchEvent(new UIEvent_PerMinute());
//         }
//     }
	public void AttachControl()
	{
		//控制模块
		try
		{
			InputManager.Instance.OnMoveDestination = mControl.MoveTo;
			InputManager.Instance.SelectTarget = mControl.SelectTarget;

		    if (UIManager.Instance.MainUIFrame != null)
		    {
		        var main = UIManager.Instance.MainUIFrame.GetComponent<MainScreenFrame>();
		        var joystick = main.GetComponentsInChildren<JoyStickLogic>(true);
		        if (joystick != null)
		        {
		            joystick[0].OnMoveDirection = mControl.MoveDirection;
		        }
		        else
		        {
                    Logger.Error("joystick is Null");
		        }
		        SkillBar = main.SkillBar.GetComponent<BattleSkillRootFrame>();
		        if (SkillBar != null)
		        {
		            SkillBar.OnClickEvent = mControl.OnClickEvent;
		        }
                else
		        {
                    Logger.Error("SkillBar is Null");
		        }
		    }
		    else
		    {
                Logger.Error("MainUI is Null");
		    }
		}
		catch (Exception e)
		{
			Logger.Error("step 7------------------{0}\n{1}", e.Message, e.StackTrace);
		}
	}

    private void Update()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (null != SkillBar)
        {
            TickSkillCd();
            SkillBar.Tick();
        }

        if (!LoadOver)
        {
            return;
        }

        if (0 == Time.frameCount%30)
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UpdateOnLineReward());
        }
        if (0 == Time.frameCount%30)
        {
            CityManager.Instance.UpdatePetMissionState();
        }
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        try
        {
#endif

	    try
	    {
			GuideManager.Instance.StopGuiding();
	    }
	    catch (Exception e)
	    {
			Logger.Error(e.Message);
	    }

	    try
	    {
			PlayCG.Instance.Reset();
	    }
	    catch (Exception e)
	    {
		    
		    Logger.Error(e.Message);
	    }

	    try
	    {
			if (null != HeadBoardManager.Instance)
			{
				HeadBoardManager.Instance.Cleanup();
			}
			if (null != DamageBoardManager.Instance)
			{
				DamageBoardManager.Instance.Cleanup();
			}
			if (null != GainItemTipManager.Instance)
			{
				GainItemTipManager.Instance.Cleanup();
			}
			if (null != UIHintBoardManager.Instance)
			{
				UIHintBoardManager.Instance.Cleanup();
			}
            ResetTimeScale();
	    }
	    catch (Exception e)
	    {

			Logger.Error(e.Message);
	    }


		try
		{
			UIManager.Instance.ClearUI();
		}
		catch (Exception e)
		{

			Logger.Error(e.Message);
		}

		try
		{
			ObjManager.Instance.Reset();
		}
		catch (Exception e)
		{

			Logger.Error(e.Message);
		}

		try
		{
			ComplexObjectPool.Destroy();
		}
		catch (Exception e)
		{

			Logger.Error(e.Message);
		}

		try
		{
			if (null != UIManager.Instance && null != UIManager.Instance.UICamera)
			{
				var rt = UIManager.Instance.UICamera.GetComponent<RenderTextureCreator>();
				if (null != rt)
				{
					rt.enabled = false;
				}
			}
		}
		catch (Exception e)
		{

			Logger.Error(e.Message);
		}
       
 		
	    
        EventDispatcher.Instance.RemoveEventListener(DungeonCompleteEvent.EVENT_TYPE, OnDungeonComplete);

		Instance = null;
        HasCache = false;

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    #endregion

    #region 事件响应

    //响应副本结束事件，副本结束时，要播放慢镜头
    private object CompTrigger;

    private void OnDungeonComplete(IEvent ievent)
    {
        var sceneId = Scene.SceneTypeId;
        var tbScene = Table.GetScene(sceneId);
        if (tbScene == null)
        {
            return;
        }
        if (tbScene.FubenId == -1)
        {
            return;
        }
        var tbFuben = Table.GetFuben(tbScene.FubenId);
        if (tbFuben == null)
        {
            return;
        }
        if (tbFuben.IsPlaySlow != 1)
        {
            return;
        }
        Time.timeScale = Table.GetClientConfig(701).Value.ToInt()/10000f;
        var lastTime = Table.GetClientConfig(700).Value.ToInt();
        if (CompTrigger != null)
        {
            TimeManager.Instance.DeleteTrigger(CompTrigger);
            CompTrigger = null;
        }

        CompTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddMilliseconds(lastTime), () =>
        {
            TimeManager.Instance.DeleteTrigger(CompTrigger);
            CompTrigger = null;
            Time.timeScale = 1f;
        });
    }

    public void ResetTimeScale()
    {
        if (CompTrigger != null)
        {
            TimeManager.Instance.DeleteTrigger(CompTrigger);
            CompTrigger = null;
        }
        Time.timeScale = 1f;
    }

    #endregion

    #region 逻辑方法

    public IEnumerator EnterGameCoroutine()
    {
		

		Stopwatch sw = new Stopwatch();
		Stopwatch swTotal = new Stopwatch();
		swTotal.Start();
	    string log = "";
        //加载场景Prefab
        if (!string.IsNullOrEmpty(ScenePrefab))
        {
			sw.Start();
            var ret = ResourceManager.PrepareResourceWithHolder<GameObject>(ScenePrefab,true,true,true,true,true);
            yield return ret.Wait();
			EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.6f));
            try
            {
                var sceneRoot = Instantiate(ret.Resource) as GameObject;
				sw.Stop();
				log += "\nScenePrefab---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;
                if (null != sceneRoot)
                {
                    sceneRoot.transform.parent = transform;

					sw.Reset();
					sw.Start();
                    // 优化场景特效
                    OptList<ParticleSystem>.List.Clear();
                    sceneRoot.transform.GetComponentsInChildren(true, OptList<ParticleSystem>.List);
                    foreach (var particle in OptList<ParticleSystem>.List)
                    {
                        if (!particle.CompareTag("NoPauseEffect"))
                        {
                            if (particle.gameObject.GetComponent<ParticleOptimizer>() == null)
                            {
                                particle.gameObject.AddComponent<ParticleOptimizer>();
                            }
                        }
                    }
					sw.Stop();
					log += "\nParticleOptimizer---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

					sw.Reset();
					sw.Start();
                    Scene = sceneRoot.GetComponent<Scene>();
                    if (null != Scene)
                    {
                        Scene.InitPortal();
                    }
                    else
                    {
                        Logger.Error("cant find Scene in ScenePerfab!!!");
                    }

					sw.Stop();
					log += "\nInitPortal---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

                }

				sw.Reset();
				sw.Start();
                if (Scene.StaticChildren)
                {
                    StaticBatchingUtility.Combine(Scene.StaticChildren);
                }
                else
                {
                    StaticBatchingUtility.Combine(sceneRoot);
                }
                sw.Stop();
				log += "\nStaticBatchingUtility---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

				sw.Reset();
				sw.Start();

                SoundManager.Instance.SetAreaSoundEnable(SoundManager.Instance.EnableBGM);
//                 var sceneCacheKey = string.Format("{0}.unity3d", ScenePrefab);
//                 ResourceManager.Instance.RemoveFromCache(sceneCacheKey);
				sw.Stop();
				log += "\nSetAreaSoundEnable---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;



            }
            catch (Exception e)
            {
				Logger.Error("step 0------------------{0}\n{1}" , e.Message, e.StackTrace);
            }
        }


        if (ObjManager.Instance == null)
        {
            Logger.Log2Bugly("EnterGameCoroutine ObjManager.Instance = null ");
            yield break;
        }

        //清除ObjManager
        ObjManager.Instance.Reset();


        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.mInitBaseAttr == null)
        {
            Logger.Log2Bugly("EnterGameCoroutine PlayerDataManager.Instance = null ");
            yield break;
        }

		sw.Reset();
		sw.Start();

        var data = PlayerDataManager.Instance.mInitBaseAttr;

        var attr = new InitMyPlayerData();

        //初始化造主角的数据
        try
        {
            attr.ObjId = data.CharacterId;
            attr.DataId = data.RoleId;
            attr.Name = data.Name;
            attr.Camp = data.Camp;
            attr.IsDead = data.IsDead == 1;
            attr.HpMax = data.HpMax;
            attr.HpNow = data.HpNow;
            attr.MpMax = data.MpMax;
            attr.MpNow = data.MpMow;
            attr.X = data.X;
            attr.Y = GetTerrainHeight(data.X, data.Y);
            attr.Z = data.Y;
            attr.MoveSpeed = data.MoveSpeed;
            attr.AreaState = (eAreaState) data.AreaState;
            attr.EquipModel = data.EquipsModel;
            attr.ModelId = data.ModelId;
            attr.MountId = data.MountId;
            {
                var __list1 = data.Buff;
                var __listCount1 = __list1.Count;
                for (var __i1 = 0; __i1 < __listCount1; ++__i1)
                {
                    var buff = __list1[__i1];
                    {
                        attr.Buff.Add(buff.BuffId, buff.BuffTypeId);
                    }
                }
            }
        }
        catch (Exception e)
        {
			Logger.Error("step 1------------------{0}\n{1}", e.Message, e.StackTrace);
        }

        //造主角
        ObjMyPlayer player = null;
        try
        {
            player = ObjManager.Instance.CreateMainPlayer(attr);
            if (player == null)
            {
                Logger.Log2Bugly("EnterGameCoroutine player = null ");
                yield break;
            }

            if (PlayFirstEnterGameCG == 1 && GameSetting.Instance.ReviewState == 1)
            {
                var reivewRecord = GameSetting.Instance.GetReviewRecord();
                if (reivewRecord != null)
                {
                    var transPos = new Vector3();
                    transPos.x = reivewRecord.posX;
                    transPos.y = GetTerrainHeight(reivewRecord.posX, reivewRecord.posY);
                    transPos.z = reivewRecord.posY;
                    player.transform.position = transPos;
                    GameUtils.FlyTo(3, reivewRecord.posX, reivewRecord.posY);
                }
            }


            player.AdjustHeightPosition();        
        }
        catch (Exception e)
        {
			Logger.Error("step 2------------------{0}\n{1}", e.Message, e.StackTrace);
        }

		sw.Stop();
		log += "\nObjMyPlayer---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

		sw.Reset();
		sw.Start();

        //设置buff
        try
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ClearBuffList());

            var count = data.Buff.Count;
            for (var i = 0; i < count; i++)
            {
                var buffResult = data.Buff[i];
                EventDispatcher.Instance.DispatchEvent(new UIEvent_SyncBuffCell(buffResult));
            }
        }
        catch (Exception e)
        {
			Logger.Error("step 3------------------{0}\n{1}", e.Message, e.StackTrace);
        }

		sw.Stop();
		log += "\nUIEvent_ClearBuffList---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

		sw.Reset();
		sw.Start();

        //预加载技能资源
        try
        {
            ObjManager.Instance.PrepareMainPlayerSkillResources();
        }
        catch (Exception e)
        {
			Logger.Error("step 4------------------{0}\n{1}", e.Message, e.StackTrace);
        }
		sw.Stop();
		log += "\nPrepareMainPlayerSkillResources---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

		sw.Reset();
		sw.Start();
        //给主摄像机设置跟随，设置声音
        try
        {
            if (MainCamera == null)
            {
                Logger.Log2Bugly("EnterGameCoroutine MainCamera = null ");
                yield break;
            }
            MainCamera.GetComponent<CameraController>().FollowObj = player.gameObject;
            {
//audio listener
                var audioListerner = MainCamera.gameObject.GetComponent<AudioListener>();
                if (null != audioListerner)
                {
                    DestroyObject(audioListerner);
                }
                var playerAudio = player.gameObject.GetComponent<AudioListener>();
                if (null == playerAudio)
                {
                    player.gameObject.AddComponent<AudioListener>();
                }
            }
        }
        catch (Exception e)
        {
			Logger.Error("step 5------------------{0}\n{1}", e.Message, e.StackTrace);
        }

		sw.Stop();
		log += "\nCameraController---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

		sw.Reset();
		sw.Start();
        //初始化UI
        Coroutine co = null;
        try
        {
             co = StartCoroutine(InitUI());

        }
        catch (Exception e)
        {
			Logger.Error("step 6------------------{0}\n{1}", e.Message, e.StackTrace);
        }
        if (null != co)
        {
            yield return co;
        }
		sw.Stop();
		log += "\nInitUI---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;


		EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.7f));

		sw.Reset();
		sw.Start();
        //UI
       // try
        {
            EventDispatcher.Instance.DispatchEvent(new Enter_Scene_Event(Scene.SceneTypeId));
            EventDispatcher.Instance.DispatchEvent(new RefresSceneMap(Scene.SceneTypeId));

            player.CreateNameBoard();

            if (PlayerDataManager.Instance != null)
            {
//根据场景不一样，自动战斗的优先级也不一样
                PlayerDataManager.Instance.RefrehEquipPriority();
            }
        }
       // catch (Exception e)
      //  {
		//	Logger.Error("step 7------------------{0}\n{1}", e.Message, e.StackTrace);
       // }
		sw.Stop();
		log += "\nUI---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

		
		sw.Reset();
		sw.Start();
        //向服务器发送切换场景结束的包
        if (SceneManager.Instance != null)
        {
            //yield return StartCoroutine(SceneManager.Instance.ChangeSceneOverCoroutine());
			StartCoroutine(SceneManager.Instance.ChangeSceneOverCoroutine());
        }
        else
        {
            Logger.Log2Bugly("EnterGameCoroutine SceneManager.Instance = null ");
        }
		sw.Stop();
		log += "\nChangeSceneOverCoroutine---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;


		sw.Reset();
		sw.Start();
        //客户端切换场景结束事件
        try
        {
            SceneEffect.OnEnterScecne();
            var formersceneid = Scene.SceneTypeId;
            SceneManager.Instance.OnLoadSceneOver(formersceneid);
            EventDispatcher.Instance.DispatchEvent(new LoadSceneOverEvent());
        }
        catch (Exception e)
        {
			Logger.Error("step 9------------------{0}\n{1}", e.Message, e.StackTrace);
        }
		sw.Stop();
		log += "\nOnLoadSceneOver---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

        //如果还没有Cache或者UIManger的Cache里没配这个UI，那就在这Cache
        if (!HasCache)
        {
            sw.Reset();
            sw.Start();
            var args = new UIInitArguments { Args = new List<int> { -1 } };
            yield return StartCoroutine(UIManager.Instance.ShowUICoroutine(UIConfig.EraBookUI, args));

            yield return new WaitForEndOfFrame();
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraBookUI));

            //解决关闭灭世地图会打开书的bug
            for (int i = 0; i < UIManager.Instance.mRecordStack.Count; i++)
            {
                if (UIManager.Instance.mRecordStack[i].Config == UIConfig.EraBookUI)
                {
                    UIManager.Instance.mRecordStack.Remove(UIManager.Instance.mRecordStack[i]);
                    break;                    
                }
            }
            sw.Stop();
            log += "\nEraBookUI---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;
            HasCache = true;
        }

		yield return new WaitForEndOfFrame();
		EventDispatcher.Instance.DispatchEvent(new LoadingPercentEvent(0.9f));
		sw.Reset();
		sw.Start();

        //播放CG
        try
        {
            Action brightnessStartWork = () =>
            {
                LoginLogic.State = LoginLogic.LoginState.InGaming;
                var bc = Game.Instance.GetComponent<BrightnessController>();
                if (bc)
                {
                    bc.ResetTimer();
                }
            };

            if (1 == PlayFirstEnterGameCG)
            {
#if UNITY_EDITOR
                var skip = true;
#else
			bool skip = true;
#endif
                if (0 == PlayerDataManager.Instance.GetRoleId() ||
                    1 == PlayerDataManager.Instance.GetRoleId() ||
                    2 == PlayerDataManager.Instance.GetRoleId())
                {
                    if (int.Parse(Table.GetClientConfig(1205).Value) == 1)
                    {
					    PlayCG.Instance.PlayCGFile("Video/HeroBorn.txt", brightnessStartWork, skip);
                        PlatformHelper.UMEvent("PlayCG", "play", "Video/HeroBorn.txt");
                    }
                }

                EventDispatcher.Instance.DispatchEvent(new FirstEnterGameEvent(true));

                PlayFirstEnterGameCG = 0;
            }
            else
            {
                brightnessStartWork();
            }
        }
        catch (Exception e)
        {
			Logger.Error("step 10------------------{0}\n{1}", e.Message, e.StackTrace);
        }

		try
		{
			if (null != GameLogic.Instance)
			{
				GameLogic.Instance.AttachControl();
			}
		}
		catch (Exception e)
		{
			Logger.Error("step 8------------------{0}\n{1}", e.Message, e.StackTrace);
		}

        mControl.OnLoadSceneOver();

        LoadOver = true;

        //优化loading读条速度，所以meshtree放在读条之后再加载
        if (null != Scene)
        {
            StartCoroutine(DelayLoadTerrainMeshTree());
        }

        StartCoroutine(DelayShowLoginRewardUI());

        if (!HasAdjustSetting)
        {
            HasAdjustSetting = true;
           // StartCoroutine(ChangeSetting()); 暂时屏蔽掉了自动降低配置功能
        }

        //LuaEventManager.Instance.PushEvent("OnEnterGameOver", Scene.SceneTypeId);

		sw.Stop();
		log += "\nOVER---------" + sw.ElapsedMilliseconds + "------------" + swTotal.ElapsedMilliseconds;

	    UnityEngine.Debug.Log(log);
    }

    private static bool HasAdjustSetting;

    private IEnumerator ChangeSetting()
    {
        yield return new WaitForSeconds(10.0f);

        mLoadCompletedTime = Time.fixedTime;
        mLoadCompletedFrame = Time.frameCount;

        yield return new WaitForSeconds(5.0f);

        var frameTime = (Time.fixedTime - mLoadCompletedTime)/(Time.frameCount - mLoadCompletedFrame);

        var willLevel = 1;
        if (frameTime < 0.04f)
        {
            // 高
            willLevel = 3;
        }
        else if (frameTime < 0.07f)
        {
            // 中
            willLevel = 2;
        }
        else
        {
            // 低
            willLevel = 1;
        }

        if (willLevel < GameSetting.Instance.GameQualityLevel)
        {
            //GameSetting.Instance.GameQualityLevel = willLevel;
            EventDispatcher.Instance.DispatchEvent(new UIEvent_QualitySetting(willLevel));
        }
    }

    public IEnumerator InitUI()
    {
        //UIManager.Instance.CreatePrelayer();
		UIManager.Instance.GetController(UIConfig.MainUI).RefreshData(null);
		var showUiCoroutine = StartCoroutine(UIManager.Instance.ShowUICoroutine(UIConfig.MainUI));
		yield return showUiCoroutine;

	    UIManager.Instance.OnEnterScene();

		HeadBoardManager.Instance.Init();
		DamageBoardManager.Instance.Init();
	    GainItemTipManager.Instance.Init();
		UIHintBoardManager.Instance.Init();

        DebugHelper.CreateDebugHelper();
        
			
        if (IsFristLogin)
        {
            //初始化推送
            EventDispatcher.Instance.DispatchEvent(new UIEvent_RefreshPush(-1, 0));



//             if (PlayerDataManager.Instance.CheckCondition(40000) == 0)
//             {
//                 UIManager.Instance.ShowUI(UIConfig.RewardFrame, new UIRewardFrameArguments
//                 {
//                     Tab = 2
//                 });
//                 IsFristLogin = false;
//             }

            //统计数据forkuaifa
            var characterId = PlayerDataManager.Instance.GetGuid().ToString();
            var characterName = PlayerDataManager.Instance.PlayerDataModel.CharacterBase.Name;
            var level = PlayerDataManager.Instance.GetLevel();
            var serverId = PlayerDataManager.Instance.ServerId.ToString();
            var serverName = PlayerDataManager.Instance.ServerName;
            var vipLevel = PlayerDataManager.Instance.GetRes((int) eResourcesType.VipLevel);
            var battleName = PlayerDataManager.Instance.BattleName;
            var ts = PlayerDataManager.Instance.CharacterFoundTime - DateTime.Parse("1970-1-1");
            var time = (int)ts.TotalSeconds;
            var diamond = PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes);

            var timeLevel = DateTime.Now - DateTime.Parse("1970-1-1");
            var timeLevelSecond = (int) timeLevel.TotalSeconds;
            var levelupTime = PlayerPrefs.GetString(characterName + "timeLv", timeLevelSecond.ToString());

            PlatformHelper.CollectionEnterGameDataForKuaifa(characterId, characterName, level, serverId, serverName, vipLevel, battleName, time.ToString(), diamond.ToString(), levelupTime);
        }
    }

    private IEnumerator DelayShowLoginRewardUI()
    {
        yield return new WaitForSeconds(0.05f);
        if (IsFristLogin)
        {
            bool isShowReardFrane = PlayerDataManager.Instance.CheckCondition(40000) == 0;

            IsFristLogin = false;
            EventDispatcher.Instance.DispatchEvent(new InitUI_Event(isShowReardFrane));
        }
    }


    private IEnumerator DelayLoadTerrainMeshTree()
    {
        yield return new WaitForSeconds(2);

        if (null == Scene)
        {
             yield break;
        }

        var path = string.Format("TerrainMeshTree/{0}.asset", ScenePrefab.Replace("Terrain/", ""));
        ResourceManager.Instance.OnAsyncLoadCompleted += LoadMeshTree;
        ResourceManager.Instance.AsyncLoadingQueue.Enqueue(path);
    }

    private void LoadMeshTree(string s)
    {
        var path = string.Format("TerrainMeshTree/{0}.asset", ScenePrefab.Replace("Terrain/", ""));
        if (s == path)
        {
            ResourceManager.Instance.OnAsyncLoadCompleted -= LoadMeshTree;
            ResourceManager.PrepareResource<BinaryMeshTree>(path, (meshTree) =>
            {
                if (null == meshTree)
                {
                    Logger.Error(string.Format("加载资源失败，检查Res/{0}文件是否存在！！", path));
                }
                else
                {
                    Scene.MeshTree = meshTree;
                    if (null != ObjManager.Instance.MyPlayer)
                    {
                        ObjManager.Instance.MyPlayer.InitShadow(GameSetting.Instance.ShowDynamicShadow);
                    }
                }
            });
        }
    }

    //获得地面高度
    public static float GetTerrainHeight(float x, float z)
    {
        var ray = new Ray(new Vector3(x, 110, z), Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 120, ShadowReceiverLayerMask))
        {
            return hit.point.y;
        }
        return 0;
    }
    
    //获得地面高度
    public static float GetTerrainHeight(Vector3 p)
    {
        return GetTerrainHeight(p.x, p.z);
    }

    //获得地面高度位置点
    public static Vector3 GetTerrainPosition(float x, float z)
    {
        return new Vector3(x, GetTerrainHeight(x, z), z);
    }

    //tick技能cd
    private static float mLastRealTime;
    private float mLoadCompletedTime;
    private int mLoadCompletedFrame;

    private void TickSkillCd()
    {
        var skillData = PlayerDataManager.Instance.PlayerDataModel.SkillData;
        var deltaTime = (Time.realtimeSinceStartup - mLastRealTime);
        //公共cd
        if (skillData.CommonCoolDown > 0)
        {
            skillData.CommonCoolDown -= deltaTime;
            if (skillData.CommonCoolDown <= 0)
            {
                skillData.CommonCoolDown = 0;
            }
        }
        //技能cd
        var count = skillData.AllSkills.Count;
        for (var i = 0; i < count; i++)
        {
            var skill = skillData.AllSkills[i];
            if (skill.CoolDownTime > 0)
            {
                skill.CoolDownTime -= deltaTime;
                if (skill.CoolDownTime <= 0)
                {
                    skill.CoolDownTime = 0;
                    if (skill.ChargeLayer != skill.ChargeLayerTotal)
                    {
                        skill.ChargeLayer++;
                        if (skill.ChargeLayer != skill.ChargeLayerTotal)
                        {
                            skill.CoolDownTime = skill.CoolDownTimeTotal;
                        }
                    }
                }
            }
        }

        mLastRealTime = Time.realtimeSinceStartup;
    }

    #endregion
}