#region using

using System;
using System.Collections.Generic;
using DataContract;
using ScriptManager;
using DataTable;
using EventSystem;
using FastShadowReceiver;
using PathologicalGames;
using Shared;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

public class Scene : MonoBehaviour
{
    private static int mLogicId = -1;
    private int lastLogicId = -1;
    private RenderTexture shadowTexture;

    public GameObject StaticChildren;

    public enum SkillRangeIndicatorType
    {
        Fan,
        Circle,
        Rectangle
    }

    public static int LogicId
    {
        get { return mLogicId; }
        private set { mLogicId = value; }
    }

    /// <summary>
    ///     获得地形高度,效率高，但是请确保Scene已经初始化好
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    /// <summary>
    ///     获得地形高度,效率高，但是请确保Scene已经初始化好
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    /// <summary>
    ///     点击地面时显示地图上的圈
    /// </summary>
    /// <param name="pos"></param>
    public void ActiveMovingCircle(Vector3 pos, Color col)
    {
        if (null == mMovingCircle)
        {
            return;
        }
        if (!mMovingCircle.gameObject.activeSelf)
        {
            mMovingCircle.gameObject.SetActive(true);
        }
        mMovingCircle.Col = col;
        var mMovingCircletransformchildCount0 = mMCTransform.childCount;
        for (var i = 0; i < mMovingCircletransformchildCount0; ++i)
        {
            var child = mMCTransform.GetChild(i).gameObject;
            if (null != child)
            {
                var particle = child.GetComponent<ParticleSystem>();
                if (null != particle)
                {
                    particle.time = 0.0f;
                }
            }
        }
        //pos.y = GetTerrainHeight(pos) + 0.05f;
        pos.y += 0.05f;
        mMCTransform.position = pos;
    }

    private void CallBackPlayCgOver()
    {
        var sceneId = SceneTypeId;
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
        if ((tbFuben.MainType == (int) eDungeonMainType.Fuben
            || tbFuben.MainType == (int)eDungeonMainType.PhaseFuben)
            && (tbFuben.AssistType == (int) eDungeonAssistType.Story
                || tbFuben.AssistType == (int) eDungeonAssistType.Daily
                || tbFuben.AssistType == (int) eDungeonAssistType.Team
                || tbFuben.AssistType == (int)eDungeonAssistType.PhaseDungeon))
        {
            if (GameControl.Instance)
            {
                GameControl.Instance.TargetObj = null;
            }
            if (ObjManager.Instance != null
                && ObjManager.Instance.MyPlayer != null)
            {
                ObjManager.Instance.MyPlayer.EnterAutoCombat();
            }
        }
        var player = ObjManager.Instance.MyPlayer;
        if (null == player)
        {
            Logger.Debug("null==player");
            return;
        }
        if (tbFuben.AssistType != (int) eDungeonAssistType.PhaseDungeon)//相位副本不需要传送特效
        {
            var tableData = Table.GetEffect(2002);
            if (tableData != null)
            {
                EffectManager.Instance.CreateEffect(tableData, player, null, null, null,
                 (tableData.BroadcastType == 0 && player.GetObjType() == OBJ.TYPE.MYPLAYER) || tableData.BroadcastType == 1);
            }
        }
     
    }

    //创建圆点阴影
    public BlobShadow CreateBlobShadow(GameObject shadowRoot, float size, List<Object> objs)
    {
        if (shadowRoot)
        {
            var shadow = shadowRoot.GetComponent<BlobShadow>();
            if (shadow == null)
            {
                shadow = shadowRoot.AddComponent<BlobShadow>();
            }
            shadow.size = size;
            objs.Add(shadow);
            return shadow;
        }
        return null;
    }

    //创建动态阴影(比较消耗)
    public void CreateDynamicShadow(Vector3 lightDir,
                                    GameObject shadowRoot,
                                    GameObject renderRoot,
                                    List<Object> objs,
                                    int shadowLayer = -1,
                                    int cullingMask = -1)
    {
        ResourceManager.PrepareResource<GameObject>(Resource.PrefabPath.DynamicShadowCaster, resCaster =>
        {
            if (gameObject == null)
            {
                return;
            }

            if (shadowRoot == null)
            {
                return;
            }

            var caster = Instantiate(resCaster) as GameObject;
            caster.GetComponent<TransformConstraint>().target = shadowRoot.transform;

            ResourceManager.PrepareResource<GameObject>(Resource.PrefabPath.DynamicShadowReceiver, resReceiver =>
            {
                if (gameObject == null)
                {
                    return;
                }

                if (cullingMask == -1)
                {
                    caster.GetComponent<Camera>().cullingMask = LayerMask.GetMask(GAMELAYER.MainPlayer);
                }
                else
                {
                    caster.GetComponent<Camera>().cullingMask = cullingMask;
                }

                caster.GetComponent<Camera>().SetReplacementShader(Shader.Find("Unlit/Transparent Cutout"), "RenderType");

                var receiver = Instantiate(resReceiver) as GameObject;
                if (-1 != shadowLayer)
                {
                    receiver.layer = shadowLayer;
                }
                var meshShadowReceiver = receiver.GetComponent<MeshShadowReceiver>();
                meshShadowReceiver.meshTree = MeshTree;
                meshShadowReceiver.meshTransform = gameObject.transform;
                meshShadowReceiver.customProjector = caster.GetComponent<CustomProjector>();

                if (null != shadowTexture)
                {
                    Destroy(shadowTexture);
                }
                // create render texture for the camera
                shadowTexture = new RenderTexture(256, 256, 8, RenderTextureFormat.Default);
                caster.GetComponent<Camera>().targetTexture = shadowTexture;
                receiver.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_ShadowTex", shadowTexture);

                var shadowRootTransform = ShadowRoot.transform;
                var receiverTransform = receiver.transform;
                caster.transform.parent = shadowRootTransform;
                receiverTransform.parent = shadowRootTransform;

#if UNITY_EDITOR
                ResourceManager.ChangeShader(receiverTransform);
#endif

                objs.Add(caster);
                objs.Add(receiver);

                ShadowRoot.SetActive(GameSetting.Instance.ShowDynamicShadow);
            });
        });
    }

    /// <summary>
    ///     创建技能范围指示器
    /// </summary>
    /// <param name="root">指示器要挂在的GameObject</param>
    /// <param name="type">指示器类型</param>
    /// <param name="arg0">如果是Fan或Circle，半径；如果是Rectangle，Width</param>
    /// <param name="arg1">如果是Fan，角度；如果是Rectangle，Length</param>
    /// <param name="c">颜色</param>
    public void CreateSkillRangeIndicator(GameObject root,
                                          SkillRangeIndicatorType type,
                                          float arg0,
                                          float arg1,
										  Color c, //Color.black 表示用材质默认颜色
                                          Action<GameObject, GameObject> act = null,
                                          bool flash = false,
											float time = 0)
    {
        if (root == null)
        {
            return;
        }

        if (MeshTree == null)
        {
            return;
        }

        string prefab;
        var flashStr = flash ? "Flash" : string.Empty;
        if (type == SkillRangeIndicatorType.Fan)
        {
            prefab = Resource.Dir.Material + "Fan" + flashStr + "_" + arg1 + ".mat";
        }
        else if (type == SkillRangeIndicatorType.Circle)
        {
            prefab = Resource.Dir.Material + "Circle" + flashStr + ".mat";
        }
        else if (type == SkillRangeIndicatorType.Rectangle)
        {
            prefab = Resource.Dir.Material + "Rectangle" + flashStr + ".mat";
        }
        else
        {
            return;
        }

        ResourceManager.PrepareResource<GameObject, GameObject, Material>(Resource.PrefabPath.SkillIndicatorCaster,
            Resource.PrefabPath.SkillIndicatorReceiver, prefab,
            (resCaster, resReceiver, matRes) =>
            {
                if (root == null)
                {
                    return;
                }

                var mat = new Material(matRes);

                var caster = Instantiate(resCaster) as GameObject;
                var receiver = Instantiate(resReceiver) as GameObject;
                receiver.SetActive(false);

                var meshShadowReceiver = receiver.GetComponent<MeshShadowReceiver>();
                meshShadowReceiver.meshTree = MeshTree;
                meshShadowReceiver.meshTransform = gameObject.transform;
                var customProjector = caster.GetComponent<CustomProjector>();
                meshShadowReceiver.customProjector = customProjector;

                if (type == SkillRangeIndicatorType.Circle || type == SkillRangeIndicatorType.Fan)
                {
                    customProjector.orthographicSize = arg0;
                }
                else
                {
                    customProjector.orthographicSize = arg1/2;
                    customProjector.aspectRatio = arg0/arg1;
                }

                var rootTransform = root.transform;
                var casterTransform = caster.transform;
                casterTransform.parent = rootTransform;
                receiver.transform.parent = rootTransform;
				receiver.GetComponent<SkillIndecatorControl>().BeginCountDown(time);
				
                casterTransform.localPosition = Vector3.up;
                casterTransform.localRotation = Quaternion.Euler(90, 0, 0);

                if (type == SkillRangeIndicatorType.Rectangle)
                {
                    casterTransform.localPosition = new Vector3(0, 1, arg1/2);
                }

                // avoid material leak
                if (receiver.GetComponent<Renderer>().sharedMaterial)
                {
                    Destroy(receiver.GetComponent<Renderer>().sharedMaterial);
                }

                receiver.GetComponent<Renderer>().sharedMaterial = mat;
	            if (c != Color.black)
	            {
					receiver.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", c);    
	            }
                
                receiver.SetActive(true);
                if (act != null)
                {
                    act(caster, receiver);
                }
#if UNITY_EDITOR
                ResourceManager.ChangeShader(receiver.transform);
#endif
            });
    }

    public void DisableActiveMovingCircle()
    {
        if (null == mMovingCircle)
        {
            return;
        }
        if (mMovingCircle.gameObject.activeSelf)
        {
            mMovingCircle.gameObject.SetActive(false);
        }
    }

    private void ExcuteDungeonLogicOpration(bool isReconnectDungeon = false)
    {
        if (lastLogicId == LogicId)
        {
            return;
        }
        var tbFubenLogic = Table.GetFubenLogic(LogicId);
        if (tbFubenLogic == null)
        {
            return;
        }
        lastLogicId = LogicId;
        for (int i = 0, imax = tbFubenLogic.AdvanceOpTYpe.Length; i < imax; ++i)
        {
            var opType = (eFubenPhaseOpType) tbFubenLogic.AdvanceOpTYpe[i];
            if (opType == eFubenPhaseOpType.None)
            {
                break;
            }
            var param1 = tbFubenLogic.AdvanceParam1[i];
            switch (opType)
            {
                case eFubenPhaseOpType.PlayAnimation:
                {
                    if (isReconnectDungeon)
                    {
//下线后回来的第一次
                        break;
                    }
                    PlayCG.PlayById(param1, CallBackPlayCgOver);
                }
                    break;
                case eFubenPhaseOpType.PlayBgMusic:
                {
                    if (isReconnectDungeon)
                    {
//下线后回来的第一次
                        SoundManager.Instance.StopBGM(0);
                        SoundManager.Instance.PlayBGMusic(param1, 0, 3, false);
                    }
                    else
                    {
                        SoundManager.Instance.PlayBGMusic(param1, 3, 3, false);
                    }
                    Logger.Info("-----------------eFubenPhaseOpType.PlayBgMusic:id = {0}-------------------", param1);
                }
                    break;
            }
        }
    }

    public void HideSelectReminder()
    {
        if (mSelectReminderTrans == null)
        {
            return;
        }
        mSelectReminderTrans.parent = null;
        if (mSelectReminderTrans.gameObject.activeSelf)
        {
            mSelectReminderTrans.gameObject.SetActive(false);
        }
    }

    /// <summary>
    ///     创建各种根节点
    /// </summary>
    private void InitGameObjRoot()
    {
        if (null == OtherPlayerRoot)
        {
            OtherPlayerRoot = new GameObject();
            OtherPlayerRoot.name = GameObjName.OtherPlayerRootName;
            OtherPlayerRootTransform = OtherPlayerRoot.transform;
        }

        if (null == NpcRoot)
        {
            NpcRoot = new GameObject();
            NpcRoot.name = GameObjName.NpcRootName;
        }

        if (null == TransferRoot)
        {
            TransferRoot = new GameObject();
            TransferRoot.name = GameObjName.TransferRootName;
        }

        if (null == ShadowRoot)
        {
            ShadowRoot = new GameObject();
            ShadowRoot.name = GameObjName.ShadowRootName;
        }

        if (null == DropItemRoot)
        {
            DropItemRoot = new GameObject();
            DropItemRoot.name = GameObjName.DropItemRoot;
        }

        if (null == GlobalSkillIndicatorRoot)
        {
            GlobalSkillIndicatorRoot = new GameObject();
            GlobalSkillIndicatorRoot.name = GameObjName.GlobalSkillIndicatorRoot;
        }

        if (null == EffectRoot)
        {
            EffectRoot = new GameObject();
            EffectRoot.name = GameObjName.EffectRoot;
        }
    }

    /// <summary>
    ///     创建传送们
    /// </summary>
    public void InitPortal()
    {
       // var res = ResourceManager.PrepareResourceSync<GameObject>(Resource.PrefabPath.Transfer);
        //遍历表里所有传送点，收集所有这个场景的传送点
        var func = new Func<TransferRecord, bool>(data =>
        {
            if (data.FromSceneId != SceneTypeId)
            {
                return true;
            }

            ResourceManager.PrepareResource<GameObject>(Resource.PrefabPath.Transfer, (res) =>
            {
                var pos = GameLogic.GetTerrainPosition(data.FromX, data.FromY);
                pos.y += 0.1f; //提高0.1米
                var obj = Instantiate(res, pos, Quaternion.identity) as GameObject;
                obj.name = "Portal";
                obj.transform.parent = TransferRoot.transform;
                var transfer = obj.GetComponent<Transfer>();
                transfer.TransferId = data.Id;
            }
            );

            return true;
        });

        Table.ForeachTransfer(func);
    }

    public static bool IsDungeon(SceneRecord table)
    {
        var type = (eSceneType)table.Type;
        if (eSceneType.Pvp == type)
        {
            return true;
        }
        if (eSceneType.Fuben == type)
        {
            var tableFuben = Table.GetFuben(table.FubenId);
            if (null != tableFuben)
            {
                var aType = (eDungeonAssistType) tableFuben.AssistType;
                if (eDungeonAssistType.PhaseDungeon == aType)
                {
                    return false;
                }
            }
            return true;
        }
        if (eSceneType.BossHome == type)
        {
            return true;
        }
        return false;
    }


    public static bool IsInFuben(SceneRecord table)
    {
        var type = (eSceneType)table.Type;
        if (eSceneType.City == type || eSceneType.Normal == type)
        {
            return false;
        }
        return true;
    }

    public void LoadTable()
    {
        //前面加载的场景就是这个场景Id
        SceneTypeId = SceneManager.Instance.CurrentSceneTypeId;
        TableScene = Table.GetScene(SceneTypeId);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        /*
		if( true==DrawTerrainHeight && null != TerrainHeightData)
		{

			var TerrainHeightDataMapWidth1 =  TerrainHeightData.MapWidth;
			for (int i = 0; i <TerrainHeightDataMapWidth1; i++)
			{
				var TerrainHeightDataMapHeight2 =  TerrainHeightData.MapHeight;
				for (int j = 0; j <TerrainHeightDataMapHeight2; j++)
				{
					var h = TerrainHeightData.GetTerrainHeight(i, j);
					var p1 = new Vector3(i, h, j);
					var p2 = new Vector3(i + 1, TerrainHeightData.GetTerrainHeight(i+1, j), j);
					var p3 = new Vector3(i, TerrainHeightData.GetTerrainHeight(i, j + 1), j + 1);

					Gizmos.DrawLine(p1, p2);
					Gizmos.DrawLine(p1, p3);
					//Gizmos.DrawLine(p3, p4);
					//Gizmos.DrawLine(p4, p1);
					//Gizmos.DrawCube(new Vector3(i + 0.5f, h, j + 0.5f), new Vector3(1, 0.2f, 1));
				}
			}
		}
		*/
// 		if (true==DrawServeZone)
// 		{
// 			float y = 50;
// 			if (null != ObjManager.Instance.MyPlayer)
// 				y = ObjManager.Instance.MyPlayer.Position.y;
// 			const int Len = 20;
// 			
// 			Vector3 p1 = Vector3.zero;
// 			p1.y = 30;
// 			Vector3 p2 = Vector3.zero;
// 			p2.y = 30;
// 			for (int i = 0; i < Len; i++ )
// 			{
// 				p1.x = 0;
// 				p1.z = i * ZONE_SIDE;
// 				p2.x = p1.x + ZONE_SIDE * Len;
// 				p2.z = p1.z;
// 				Gizmos.DrawLine(p1, p2);
// 
// 				p1.x = i * ZONE_SIDE;
// 				p1.z = 0;
// 				p2.x = p1.x;
// 				p2.z = p1.z + ZONE_SIDE * Len;
// 				Gizmos.DrawLine(p1, p2);
// 			}
// 		}
#endif
    }

    public void PlayMusic()
    {
        var sound = TableScene.Sound;
        if (-1 != sound)
        {
            SoundManager.Instance.PlayBGMusic(sound, 3, 3);
        }
        else
        {
            SoundManager.Instance.StopBGM(0.1f);
        }
    }

    public static void SetLogicId(int id, bool isReconnectDungeon = false)
    {
        if (LogicId == id)
        {
            return;
        }
        LogicId = id;
        if (GameLogic.Instance != null)
        {
            var scene = GameLogic.Instance.Scene;
            if (scene != null)
            {
                var tbScene = Table.GetScene(scene.SceneTypeId);
                if (tbScene.Type == 2)
                {
                    scene.ExcuteDungeonLogicOpration(isReconnectDungeon);
                }
            }
        }
        if (ObjManager.Instance != null
            && ObjManager.Instance.MyPlayer != null
            && ObjManager.Instance.MyPlayer.AutoCombat != null)
        {
            ObjManager.Instance.MyPlayer.AutoCombat.ChangeFubenLogicId();
        }
    }

    public void ShowSelectReminder(ObjCharacter obj, Color col)
    {
        if (obj == null
            || mSelectReminderTrans == null
            || mSelectReminder == null
            || obj.CharModelRecord == null)
        {
            return;
        }
        var trans = obj.transform;
        mSelectReminderTrans.parent = trans;
        mSelectReminderTrans.localPosition = new Vector3(0, 0.1f, 0);
        //mSelectReminderTrans.localRotation = Quaternion.Euler(90,0,0);
        mSelectReminder.SetColorSize(obj.CharModelRecord.ShadowSize*1.5f, col);
        if (mSelectReminderTrans.gameObject.activeSelf == false)
        {
            mSelectReminderTrans.gameObject.SetActive(true);
        }
    }

    #region 成员

    //场景类型id
    public int SceneTypeId { get; private set; }

    //地形高度数据
    //public TerrainHeightData TerrainHeightData;

    //移动光圈
    private MovingCircleController mMovingCircle;

    private SelectReminderLogic mSelectReminder;
    private Transform mSelectReminderTrans;
    //移动光圈的transform
    private Transform mMCTransform;

    [HideInInspector]
    //场景根节点
    public GameObject SceneRoot;

    //其他玩家根节点
    [HideInInspector] public GameObject OtherPlayerRoot;

    //其他玩家根节点的Transform
    [HideInInspector] public Transform OtherPlayerRootTransform;

    //Npc根节点
    [HideInInspector] public GameObject NpcRoot;

    //Npc根节点
    [HideInInspector] public GameObject TransferRoot;

    //掉落根节点
    [HideInInspector] public GameObject DropItemRoot;

    //阴影根节点
    [HideInInspector] public GameObject ShadowRoot;

    //特效节点
    [HideInInspector] public GameObject EffectRoot;

    [HideInInspector] public SceneRecord TableScene;

    public BinaryMeshTree MeshTree;

    [HideInInspector] public GameObject GlobalSkillIndicatorRoot;

    public float Contrast = 0.3f;
    public Color Color = Color.white;
    public float Bright = 1f;

// #if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
// 	public bool DrawTerrainHeight = false;
// 	public bool DrawServeZone = false;
// 	public float ZONE_SIDE = 10.0f;
// #endif

    #endregion

    #region Mono

    private void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (GameSetting.Instance.ReviewState == 1)
        {
            var reivewRecord = GameSetting.Instance.GetReviewRecord();
            if (reivewRecord != null)
            {
                Color = new Color(reivewRecord.red / 255.0f, reivewRecord.green / 255.0f, reivewRecord.blue / 255.0f);
            }
        }

        LoadTable();

        SceneRoot = gameObject;

//         // all the ground should render before things like shadow, special effect without ztest
//         var renderers = SceneRoot.GetComponentsInChildren<Renderer>();
//         var layer = LayerMask.NameToLayer("ShadowReceiver");
//         var targetLayer = 2000 - 2; // Geometry - 2
//         for (int i = 0; i < renderers.Length; i++)
//         {
//             if (renderers[i].gameObject.layer == layer)
//             {
//                 var mat = renderers[i].sharedMaterials;
//                 for (int j = 0; j < mat.Length; j++)
//                 {
//                     mat[j].renderQueue = targetLayer;
//                 }
//             }
//         }

        if (Camera.main)
        {
            var audioListerner = Camera.main.gameObject.GetComponent<AudioListener>();
            if (null != audioListerner)
            {
                DestroyObject(audioListerner);
            }
        }

        InitGameObjRoot();

        var switcher = gameObject.GetComponent<DayNightSwitcher>();
        if (switcher != null)
        {
            switcher.DayFar = new Texture2D[LightmapSettings.lightmaps.Length];
            for (int i = 0; i < switcher.DayFar.Length; i++)
            {
                switcher.DayFar[i] = LightmapSettings.lightmaps[i].lightmapFar;
            }
        }

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

#if UNITY_EDITOR
        //修正Shader
        ChangeShader();
#endif

        //创建传送门
        if (GameLogic.Instance && string.IsNullOrEmpty(GameLogic.Instance.ScenePrefab))
        {
            InitPortal();
        }

        InitHDR();

        var scene = gameObject;
        //创建MovingCircle
        ResourceManager.PrepareResource<GameObject>(Resource.PrefabPath.MovingCircle, (res) =>
        {
            var obj = Instantiate(res) as GameObject;
            mMovingCircle = obj.GetComponent<MovingCircleController>();
            mMCTransform = mMovingCircle.transform;
            mMCTransform.name = "MovingCircle";
            mMCTransform.gameObject.SetActive(false);
        });

        ResourceManager.PrepareResource<GameObject>(Resource.PrefabPath.SelectReminder, (res) =>
        {
            var obj = Instantiate(res) as GameObject;
            mSelectReminder = obj.GetComponent<SelectReminderLogic>();
            mSelectReminderTrans = mSelectReminder.transform;
            mSelectReminderTrans.name = "SelectReminder";
        });

        PlayMusic();

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    private void ChangeShader()
    {
        ResourceManager.ChangeShader(transform);
    }


    private void InitHDR()
    {
	    if (GameSetting.Instance.EnablePostEffect)
	    {
		    var mat = ResourceManager.PrepareResourceSync<Material>(Resource.Material.BloomMaterial);
		    {
			    if (GameLogic.Instance.MainCamera)
			    {
				    var bloom = GameLogic.Instance.MainCamera.gameObject.AddComponent<DistortionAndBloom>();
				    bloom.material = mat;
			    }
		    }
	    }
    }

    public void OnDestroy()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        //不要在这里清理,关闭游戏时候会有泄露 by:zff
        // CityManager.Instance.ClearBuildingModel();
        // SoundManager.Instance.StopBGM(0);
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public void ClearLastScene(bool stopBgm = true)
    {
        LogicId = -1;
        CityManager.Instance.ClearBuildingModel();
        if (stopBgm)
        {
            SoundManager.Instance.StopBGM(0);
        }
    }

    #endregion




    #region 吃鸡相关
    private GameObject objCircle;
    //private ObjMyPlayer MyPlayer;
    
    private CameraController Cctrl;
    private bool bChiji = false;
    private float Radius = 999f;
    public bool bOutSide = false;
    private int RadiusMin;
    private int TimeSpace;
    private int ReduceNum;
    private float eveReduce;
    public void ClearChiji()
    {
                          
    }

    public void InitChiji()
    {
        {
            var temp = Table.GetClientConfig(1505);
            if (temp != null)
                RadiusMin = int.Parse(temp.Value);
            temp = Table.GetClientConfig(1503);
            if (temp != null)
                TimeSpace = int.Parse(temp.Value);
            temp = Table.GetClientConfig(1502);
            if (temp != null)
                ReduceNum = int.Parse(temp.Value);
            eveReduce = (float)ReduceNum / TimeSpace;
            var objres = ResourceManager.PrepareResourceSync<GameObject>("Prefab/QiuTi.prefab");
            objCircle = Instantiate(objres) as GameObject;
            objCircle.transform.parent = transform;
            bChiji = true;
        }
        {
            //CameraQuan = GameObject.Instantiate(UIHintBoardManager.Instance.Test_ChiJiAnQuanQu) as GameObject;
            //CameraQuan.transform.parent = UIHintBoardManager.Instance.Test_ChiJi.transform;
            //CameraQuan.transform.localScale = Vector3.one;
            //CameraQuan.transform.localPosition = Vector3.zero;
            //CameraQuan.name = "CameraQuan";
            //CameraQuan.SetActive(false);
        }

    }

    private object trigger;
    public void GetChijiInfo(MsgCheckenSceneInfo info)
    {
        if (transform == null)
            return;
        if (objCircle == null)
        {
            InitChiji();
        }
        var p = new Vector2(GameUtils.DividePrecision(info.CenterPos.x), GameUtils.DividePrecision(info.CenterPos.y));
        objCircle.transform.localPosition = new Vector3(p.x, GameLogic.GetTerrainHeight(p.x, p.y), p.y);
        objCircle.transform.localScale = new Vector3(info.Radius, info.Radius, 200);
        Radius = info.Radius;
        if (trigger == null){
            trigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1f), () =>
            {
                Radius -= eveReduce;
                objCircle.transform.localScale = new Vector3(Radius, Radius, 200);
                if (Radius <= RadiusMin)
                {
                    TimeManager.Instance.DeleteTrigger(trigger);
                    trigger = null;
                }
            },1000);
        }
    }

    private void Update()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (bChiji == false || objCircle == null || ObjManager.Instance.MyPlayer == null)
            return;
        var dis = Vector2.Distance(ObjManager.Instance.MyPlayer.Position.xz(), objCircle.transform.localPosition.xz());
        bool b = dis * dis > (Radius / 2) * (Radius / 2);

        if (bOutSide != b)
        {
            bOutSide = b;
            EventDispatcher.Instance.DispatchEvent(new ShowHpTransitionSetEvent(b));
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    #endregion

}