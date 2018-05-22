using ScriptManager;
using ScriptManager;
using System;
#region using

using System.Collections.Generic;
using DataContract;
using DataTable;
using UnityEngine;

#endregion

public class GameSetting : MonoBehaviour
{
    //渠道配置
    public static string Channel;
    public const string GameQuilatyKey = "GameQuilatyKey";
    public const string GameResolutionKey = "GameResolutionKey";
    public static GameSetting Instance;
    public const string LowFpsKey = "LowFpsKey";
    public const string ShowWaitingTipKey = "ShowWaitingTipKey";
    public const string ShowAnnouncementKey = "ShowAnnouncementKey";
    public const string LoginAssistantKey = "LoginAssistantKey";
    public const string CustomChat = "CustomChat";
    public const string ShowOtherPlayerTitle = "ShowOtherPlayerTitle";
    //Character速度增量
    public float CharacterSpeedDelta = 0.1f;
    public List<string> DirectoryAddress;
    //方向同步差值(只有当方向偏差到这个值才同步一次)
    public float DirSyncDelta = 0.05f;
    //方向同步最小时间间隔
    public float DirSyncInterval = 0.1f;
    //是否开启引导
    public bool EnableGuide = false;
    //是否开启新功能提示
    public bool EnableNewFunctionTip = false;
    public float Height = 1;
    //是否无视按钮条件
    public bool IgnoreButtonCondition = false;
    public float Length = 10;
    public bool LoadingProcessGameInit = false;
    //#if UNITY_EDITOR
    public Logger.LogLevelType LogLevel = Logger.LogLevelType.Debug;
    //android释放内存界限
    public int LowMemorySize = 100;
    //主角释放技能位置误差
    public float MainPlayerSkillPosErrorDistance = 2.0f;
    public float MaxDistance_DropItem = 2.0f;
    public float MaxDistance_NPC = 2.0f;
    public string[] mCommonBundleList;
    //是否开启HDR
    private bool mEnableHDR = true;
    //是否开启后期效果
    private bool mEnablePostEffect = true;
	private bool mEnableColorAdjuestEffect = true;
    private int mGameQualityLevel = 3;
    private int mGameResolutionLevel = 3;
    //最小被击动作时间
    public float MinHurtActionInterval = 0.5f;
    private int mOriginHeight;
    private int mOriginWidth;
    //移动同步最小时间间隔
    public float MoveSyncInterval = 0.3f;
    public bool CombineCharacterMesh = false;
    //移动同步阈值
    public float MoveSyncShreholdSqr = 4f;
    // 可以在这里加入需要预加载的资源 ----这个已经不能预加载ui了,因为把uicommon的加载往后移动了,这里只能预加载不依赖uicommon
    public List<string> mResourceList;

    //预加载的ui资源
    public List<string> mUIResourceList;

    private bool mShowBlobShadow = true;
    private bool mShowDynamicShadow = true;
    private bool mShowEffect = true;
    private bool mShowOtherPlayer = true;
    //city weaknotice
    public int NoticeRefreshTimeFrist;
    public int NoticeRefreshTimeLast;
    //NPC释放技能位置误差 
    public float NPCSkillPosErrorDistance = 0.01f;
    //NPC停止位置误差
    public float NPCStopPosErrorDistance = 1.5f;
    //其他玩家释放技能位置误差
    public float OtherPlayerSkillPosErrorDistance = 1.5f;
    //其他玩家停止位置误差
    public float OtherPlayerStopPosErrorDistance = 2.0f;
    //其他玩家速度增量
    public float OtherSpeedDelta = 0.2f;
    //节电降低屏幕亮度
    public bool PowerSaveEnabe = false;
    //预加载的字
    public string PrepareString;
    public float[] ResolutionRadio = {1.0f, 0.5f, 0.75f, 1.0f};
    public float ResourceCacheMaxSize = 1.0f;
    //临时存储检查是否在审核中
    public int ReviewState;
    private int scaleHeight;
    private int scaleWidth;
    //是否开启目标辅助功能
    public bool TargetSelectionAssistant = true;
    // #else
    // 	public Logger.LogLevelType LogLevel = Logger.LogLevelType.Error;
    // #endif
    public bool UpdateEnable = false;
    //是否使用UI脚本
    public bool UseUIScript = false;

    //是否使用多线程加载表格
    public bool ThreadLoadTable = false;
    //是否控制动画播放速率
    public int SlowdownAnimationFrameRate = 1;
    //加载表格线程数
    public int ThreadCount = 10;

    public int ForceShowAnn { get; set; }
    public string Isbn1 { get; set; }
    public string Isbn2 { get; set; }
    public bool Blur
    {
        set
        {
            if (!GameLogic.Instance || !GameLogic.Instance.MainCamera)
            {
                return;
            }

            var bloom = GameLogic.Instance.MainCamera.GetComponent<DistortionAndBloom>();
            if (bloom)
            {
                bloom.Blur = value;
            }
        }
    }

    public bool CameraShakeEnable { get; set; }

    public string[] CommonBundleList
    {
        get { return mCommonBundleList; }
    }

    public bool EnableHDR
    {
        get { return mEnableHDR; }
        set
        {
            if (mEnableHDR == value)
            {
                return;
            }

            mEnableHDR = value;

            // 如果没有开启后期处理，开了HDR也没啥用
            if (!EnablePostEffect)
            {
                return;
            }

            if (!GameLogic.Instance || !GameLogic.Instance.MainCamera)
            {
                return;
            }

            //GameLogic.Instance.MainCamera.hdr = mEnableHDR;
        }
    }

    public bool EnablePostEffect
    {
        get { return mEnablePostEffect; }
        set
        {
            if (mEnablePostEffect == value)
            {
                return;
            }

            mEnablePostEffect = value;

            if (!GameLogic.Instance || !GameLogic.Instance.MainCamera)
            {
                return;
            }

            var bloom = GameLogic.Instance.MainCamera.GetComponent<DistortionAndBloom>();
            if (!mEnablePostEffect)
            {
                if (bloom == null)
                {
                    GameLogic.Instance.MainCamera.hdr = false;
                }
                else
                {
                    bloom.enabled = false;
                    GameLogic.Instance.MainCamera.hdr = false;
                }
            }
            else
            {
                if (bloom != null)
                {
                    bloom.enabled = true;
                    //GameLogic.Instance.MainCamera.hdr = mEnableHDR;
                }
                else
                {
                    ResourceManager.PrepareResource<Material>(Resource.Material.BloomMaterial, mat =>
                    {
                        if (GameLogic.Instance == null)
                        {
                            return;
                        }
                        bloom = GameLogic.Instance.MainCamera.gameObject.AddComponent<DistortionAndBloom>();
                        bloom.material = mat;
                        //GameLogic.Instance.MainCamera.hdr = mEnableHDR;
                    });
                }
            }
        }
    }
	public bool EnableColorAdjuestEffect
	{
		get { return mEnableColorAdjuestEffect; }
		set
		{
			if (mEnableColorAdjuestEffect == value)
			{
				return;
			}

			mEnableColorAdjuestEffect = value;

			if (!GameLogic.Instance || !GameLogic.Instance.MainCamera)
			{
				return;
			}
		}
	}

    /// <summary>
    ///     高 3
    ///     中 2
    ///     低 1
    /// </summary>
    public int GameQualityLevel
    {
        get { return mGameQualityLevel; }
        set
        {
            if (mGameQualityLevel != value)
            {
                //初始化时候设置高效果创建角色好看不需要存储
                if (value == -2)
                {
                    value = 3;
                }
                //闭眼睛设置最低效果,也不需要存储状态
                else if (value == -3)
                {
                    value = 1;
                }
                else if (value > 0)
                {
                    PlayerPrefs.SetInt(GameQuilatyKey, value);
                    PlayerPrefs.Save();
                }

                GameResolutionLevel = value;
                var tureArray = new Int32Array();
                var falseArray = new Int32Array();
                bool visibleEye = true;
                var controller = UIManager.Instance.GetController(UIConfig.SettingUI);
                if (null != controller)
                {
                    var obj = controller.CallFromOtherClass("GetEyeIsOpen", null);
                    visibleEye = (bool)obj;
                }

                mGameQualityLevel = value;
                if (3 == value)
                {
                    mEnableHDR = true;
                    EnablePostEffect = true;
	                EnableColorAdjuestEffect = false;
                    ShowBlobShadow = true;
                    if (visibleEye)
                    {
                        ShowOtherPlayer = true;
                        ShowEffect = true;
                        falseArray.Items.Add(483);
                        falseArray.Items.Add(482);
                    }
                    ShowDynamicShadow = true;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.blendWeights = BlendWeights.TwoBones;
                    QualitySettings.particleRaycastBudget = 0;
                    QualitySettings.pixelLightCount = 0;
                    QualitySettings.vSyncCount = 0;
                    SlowdownAnimationFrameRate = 1;
                }
                else if (value == 2)
                {
                    mEnableHDR = false;
                    EnablePostEffect = false;
					EnableColorAdjuestEffect = true;
                    ShowBlobShadow = true;
                    ShowDynamicShadow = false;
                    if (visibleEye)
                    {
                        ShowEffect = true;
                        ShowOtherPlayer = true;
                        falseArray.Items.Add(482);
                        falseArray.Items.Add(483);
                    }
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.blendWeights = BlendWeights.OneBone;
                    QualitySettings.particleRaycastBudget = 0;
                    QualitySettings.pixelLightCount = 0;
                    QualitySettings.vSyncCount = 0;
                    SlowdownAnimationFrameRate = 2;
                }
                else
                {
                    mEnableHDR = false;
                    EnablePostEffect = false;
					EnableColorAdjuestEffect = false;
                    ShowBlobShadow = false;
                    ShowDynamicShadow = false;
                    if (visibleEye)
                    {
                        ShowOtherPlayer = true;
                        ShowEffect = false;
                        falseArray.Items.Add(482);
                        tureArray.Items.Add(483);
                    }
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.blendWeights = BlendWeights.OneBone;
                    QualitySettings.particleRaycastBudget = 0;
                    QualitySettings.pixelLightCount = 0;
                    QualitySettings.vSyncCount = 0;
                    SlowdownAnimationFrameRate = 2;
                }

                if (NetManager.Instance.Connected)
                {
                    PlayerDataManager.Instance.SetFlagNet(tureArray, falseArray);
                }

                ObjManager.Instance.ResetShadow();
            }
        }
    }

    /// <summary>
    ///     高 3
    ///     中 2
    ///     低 1
    /// </summary>
    public int GameResolutionLevel
    {
        get { return mGameResolutionLevel; }
        set
        {
            if (mGameResolutionLevel != value)
            {
                mGameResolutionLevel = value;
                PlayerPrefs.SetInt(GameResolutionKey, value);
                if (mOriginWidth == 0)
                {
                    mOriginWidth = Screen.currentResolution.width;
                    mOriginHeight = Screen.currentResolution.height;
                }
                var radio = GameUtils.GetResolutionRadio();
                scaleHeight = Mathf.CeilToInt(mOriginHeight*radio);
                scaleWidth = Mathf.CeilToInt(mOriginWidth*radio);
              //  Screen.SetResolution(scaleWidth, scaleHeight, true);
                ResourceManager.Instance.ResizeRenderTexture();
            }
        }
    }
    private int[] mMaxVisibleModelCount;
    public int MaxVisibleModelCount
    {
        get
        {
            if(mMaxVisibleModelCount == null)
            {
                if (Table.GetClientConfig(1003) != null)
                {
                    mMaxVisibleModelCount = new int[3];
                    mMaxVisibleModelCount[0] = int.Parse(Table.GetClientConfig(1003).Value);
                    mMaxVisibleModelCount[1] = int.Parse(Table.GetClientConfig(1004).Value);
                    mMaxVisibleModelCount[2] = int.Parse(Table.GetClientConfig(1005).Value);
                }
                else
                {
                    return 0;
                }
            }

            if (GameQualityLevel == 3)
            {
                return mMaxVisibleModelCount[2];
            }
            if (GameQualityLevel == 2)
            {
                return mMaxVisibleModelCount[1];
            }
            return mMaxVisibleModelCount[0];
        }
    }

    public List<string> ResourceList
    {
        get { return mResourceList; }
    }

    public bool ShowBlobShadow
    {
        get { return mShowBlobShadow; }
        set
        {
            if (mShowBlobShadow == value)
            {
                return;
            }
            mShowBlobShadow = value;
            BlobShadow.ActiveAllRenders(mShowBlobShadow);
        }
    }

    public bool ShowDynamicShadow
    {
        get { return mShowDynamicShadow; }
        set
        {
            if (mShowDynamicShadow == value)
            {
                return;
            }
            mShowDynamicShadow = value;
            if (GameLogic.Instance != null)
            {
                GameLogic.Instance.Scene.ShadowRoot.SetActive(value);
            }
        }
    }

    public bool ShowEffect
    {
        get { return mShowEffect; }
        set
        {
            mShowEffect = value;
            if (!mShowEffect)
            {
                EffectManager.Instance.StopAllEffect();
            }
        }
    }

    /// <summary>
    ///     设置使其他玩家可见或者不可见
    /// </summary>
    public bool ShowOtherPlayer
    {
        get { return mShowOtherPlayer; }
        set
        {
            mShowOtherPlayer = value;
            if (GameLogic.Instance)
            {
                var camera = GameLogic.Instance.MainCamera;
                if (null != camera)
                {
                    var layer = camera.cullingMask;
                    if (mShowOtherPlayer)
                    {
                        layer |= 1 << LayerMask.NameToLayer(GAMELAYER.OhterPlayer);
                    }
                    else
                    {
                        layer &= ~(1 << LayerMask.NameToLayer(GAMELAYER.OhterPlayer));
                    }
                    camera.cullingMask = layer;
                }
            }

            ObjManager.Instance.ShowHideOtherPlayerTitle(mShowOtherPlayer);
        }
    }

    public bool ShowOtherPlayerNameTitle
    {
        get { return PlayerPrefs.GetInt(ShowOtherPlayerTitle,1)==1?true:false; }
        set
        {
            PlayerPrefs.SetInt(ShowOtherPlayerTitle, value?1:0);
            ObjManager.Instance.ShowHideOtherPlayerTitle(value);
        }
    }
    public bool RenderTextureEnable;

    // review 
    private IosMutiplePlatformRecord reviewRecord = null;
    public IosMutiplePlatformRecord GetReviewRecord()
    {
        if (reviewRecord == null)
        {
            Table.ForeachIosMutiplePlatform(record =>
            {
                if (record.channel == UpdateHelper.Channel)
                {
                    reviewRecord = record;
                    return false;
                }

                return true;
            });    
        }

        return reviewRecord;
    }


    private void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        Instance = this;
        ResolutionRadio = Screen.currentResolution.width > 1920 ? new[]{1, 0.5f, 0.666f, 0.75f} : new[] {1, 0.666f, 0.75f, 1f};
        Logger.Info("Screen resolution = {0}x{1}", Screen.currentResolution.width, Screen.currentResolution.height);

#if !UNITY_EDITOR && (UNITY_IPHONE)
        UpdateEnable = true;
        UseUIScript = false;
        LogLevel = Logger.LogLevelType.Error;
		EnableNewFunctionTip = true;
        ThreadLoadTable = true;
#elif !UNITY_EDITOR && (UNITY_ANDROID || UNITY_WP8)
        UpdateEnable = true;
        LogLevel = Logger.LogLevelType.Error;
		EnableNewFunctionTip = true;
        ThreadLoadTable = true;
        UseUIScript = false;
#endif


#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    // Use this for initialization
    private void Start()
    {
#if !UNITY_EDITOR
        try
        {
#endif


#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    //todo 发布前记得全部更换为正式的

    #region 第三方配置

    //Umeng
    public static string UmengAppKeyAndroid = "58cb92c1cae7e773e1000602";
    public static string UmengAppKeyiOS = "58cb93597f2c7444930008fc";

    //bugly
    public static string buglyAppIdAndroid = "0";
    public static string buglyAppIdiOS = "0";

    #endregion
}