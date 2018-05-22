#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ScriptManager;
using C5;
using DataTable;
using EventSystem;
using SignalChain;
using UnityEngine;
using ClientDataModel;
using ClientService;
using DataContract;
using Mono.Collections.Generic;
using NCalc.Domain;
using ScorpionNetLib;

#endregion

namespace GameUI
{
    public class MainScreenFrame : MonoBehaviour, IChainRoot, IChainListener
    {
        private bool IsShowHpTransition;
        private bool ChijiHpTransition;
        private bool isInint = false;
        private const float BlockContinueTime = 8.0f;
        //是否已经准备过字
        private static bool s_isPretreatString;
        public GameObject Arrow;
        public BindDataRoot Binding;
        //block input
        public UIWidget BlockInputWidget;
        public Transform BuffRoot;
        public Transform CharCursor;
        private readonly Dictionary<ulong, ListItemLogic> itemLogicDict = new Dictionary<ulong, ListItemLogic>();
        public GameObject DownArrow;
        public GameObject ExpLable;
        public UILabel FightReady;
        private Coroutine HideExpCo;
        public GameObject JoyStick;
        public List<StackLayout> Layout;
        private float currentBlockTime;
        private bool isEnable;
        private bool showSKill;
        private int learnSkillID = -1;
        private Coroutine countdownCoroutine;
        private DateTime countdownTime;
        private eCountdownType countdownType;
        private bool displayCountdownTimer;
        //offlineframe
        public GameObject PkBg;
        public GameObject AutoFightBtn;
        public List<UIButton> PkBtn;
        public GameObject RadarTitle;
        public GameObject SkillBar;
        public UISpriteAnimation SkillEffectAni;
        public GameObject InspireEffect;
        //buff list
        public List<Transform> SkillList;
        // move learn skill
        public UISprite SkillMove;
        public UILabel SkillName;
        public TweenAlpha SkillNameTween;
        public Transform SkillTarget;
        public UIToggle Team;
        public GameObject Transition;
        public GameObject PlayerHpTransition;
        private int PlayTransitionTime;
        public GameObject UpArrow;
        public StackLayout MissionLayout;

        public UILabel TimeLabel;
        public UILabel TimeShow;
        public UILabel CountDownLabel;

        public GameObject LiuGuang;

        public GameObject MieshiBtn;

        public int m_ActivityState;

        public UILabel BossTalk;
        public float mBossTalkTimer = -1.0f;
        public float Playerhp = 50;

        public UIPopupList PopList;
        public UILabel VoiceType;

        public UIButton EnterVoiceBtn;
        public UIButton ExitVoiceBtn;

        public GameObject SummonMonsterBtn;
        public GameObject UI_jinengEffect;

        private UILabel EnterVoiceEnterTipLabel;//主播按钮提示文字
        private MonsterDataModel MieshiMonsterDataModel;
        private GameObject FirstEnterGameObject;
        //public GameObject 


        /// <summary>
        /// fix iphone x
        /// </summary>

        private List<Transform> leftTransforms = new List<Transform>();
        private List<Transform> rightTransforms = new List<Transform>();
        private List<Vector3> leftTransformsPos = new List<Vector3>();
        private List<Vector3> rightTransformsPos = new List<Vector3>();
        private bool bInitSafePos = false;
        private bool isIphoneX = false;

        private DeviceOrientation LastDir = DeviceOrientation.Unknown;
        private readonly string[] leftPath =
        {
            "Panel2/SkillRoot/JoyStickRoot",
            "Panel4/MissionTrackList",
            "Panel1/MyPlayerRoot/ActivityIcon",
            "Panel1/TargetRoot",
            "Panel1/BuffRoot",
          //  "Panel5/MainMenuRoot/DownMenuRoot"
        };


        private readonly string[] rightPath =
        {
        //    "Panel5/MainMenuRoot/UpMenuRoot",
            "Panel5/MainMenuRoot/FunctionOpen",
            "Panel4/DeviceInfo",
            "Panel4/Shortcut",
            "Panel4/Advertisement",
            "Panel4/DungeonQueue",
         //   "Panel3/RadarFrame",
            "Panel2/SkillRoot/SkillRootAndAutoFightRoot"
        };


        private void InitFixIphonex()
        {
            isIphoneX = PlatformHelper.IsIphoneX();

            if (!isIphoneX) return;

            if (bInitSafePos) return;


            leftTransforms.Clear();
            leftTransformsPos.Clear();
            rightTransforms.Clear();
            rightTransformsPos.Clear();
            for (int i = 0; i < leftPath.Length; i++)
            {
                var tran = transform.FindChild(leftPath[i]);
                if (tran)
                {
                    leftTransforms.Add(tran);
                    var widget = tran.GetComponent<UIWidget>();
                    if (widget)
                    {
                        leftTransformsPos.Add(new Vector3(widget.leftAnchor.absolute, widget.rightAnchor.absolute, 0));
                    }
                    else
                    {
                        Logger.Error("can find widget " + leftPath[i]);
                    }
                }
            }

            for (int i = 0; i < rightPath.Length; i++)
            {
                var tran = transform.FindChild(rightPath[i]);
                if (tran)
                {
                    rightTransforms.Add(tran);
                    var widget = tran.GetComponent<UIWidget>();
                    if (widget)
                    {
                        rightTransformsPos.Add(new Vector3(widget.leftAnchor.absolute, widget.rightAnchor.absolute, 0));
                    }
                    else
                    {
                        Logger.Error("can find widget " + leftPath[i]);
                    }
                }
            }

            bInitSafePos = true;
        }


        private void FixIphoneX(IEvent ievent)
        {
            var e = ievent as UIEvent_FixIphoneX;
            if (e != null) UpdateIphoneXSafeArea(e.Dir);
        }

        private void AutoFixIphoneX()
        {
            if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
            {
                UpdateIphoneXSafeArea(-1);
            }
            else if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
            {
                UpdateIphoneXSafeArea(1);
            }
            LastDir = Input.deviceOrientation;
        }

        private void UpdateIphoneXSafeArea(int dir)
        {
            if (!isIphoneX) return;

            switch (dir)
            {
                case -1:
                    {
                        ResetSafeAreaPos();
                        SetSafeAreaPos(leftTransforms, new Vector3(55, 0, 0));
                    }
                    break;
                case 1:
                    {
                        ResetSafeAreaPos();
                        SetSafeAreaPos(rightTransforms, new Vector3(-55, 0, 0));
                    }
                    break;
            }
        }

        private void ResetSafeAreaPos()
        {
            for (var i = 0; i < leftTransforms.Count; i++)
            {
                var tf = leftTransforms[i];
                if (tf)
                {
                    var widget = tf.GetComponent<UIWidget>();
                    var vec3 = leftTransformsPos[i];
                    widget.leftAnchor.absolute = (int)vec3.x;
                    widget.rightAnchor.absolute = (int)vec3.y;
                }
            }

            for (var i = 0; i < rightTransforms.Count; i++)
            {
                var tf = rightTransforms[i];
                if (tf)
                {
                    var widget = tf.GetComponent<UIWidget>();
                    var vec3 = rightTransformsPos[i];
                    widget.leftAnchor.absolute = (int)vec3.x;
                    widget.rightAnchor.absolute = (int)vec3.y;
                }
            }
        }

        private void SetSafeAreaPos(List<Transform> transList, Vector3 offset)
        {
            for (var i = 0; i < transList.Count; i++)
            {
                var tf = transList[i];
                if (tf)
                {
                    var widget = tf.GetComponent<UIWidget>();
                    if (widget)
                    {
                        widget.leftAnchor.absolute += (int)offset.x;
                        widget.rightAnchor.absolute += (int)offset.x;
                        widget.UpdateAnchors();
                    }
                }
            }
        }


        //----------------------fix iphonex finish

        //-------------------------------------------------PkModel
        public void ChangePkModel(int value)
        {

            PlayerDataManager.Instance.ChangePkModel(value);
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(69990 + value));
            ChangeModel(false);
        }

        private void SetupBufferList()
        {

            if (null != BuffRoot)
            {
                var res = ResourceManager.PrepareResourceSync<GameObject>("UI/MainUI/BuffList.prefab");
                var obj = Instantiate(res) as GameObject;
                var objTransform = obj.transform;
                //objTransform.parent = BuffRoot;
                objTransform.SetParentEX(BuffRoot);
                objTransform.localScale = Vector3.one;
                objTransform.localPosition = Vector3.zero;
                obj.SetActive(true);
            }

            //ComplexObjectPool.NewObject("UI/MainUI/BuffList.prefab", gameObject =>
            //{
            //    if (null != BuffRoot)
            //    {
            //        var objTransform = gameObject.transform;
            //        //objTransform.parent = BuffRoot;
            //        objTransform.SetParentEX(BuffRoot);
            //        objTransform.localScale = Vector3.one;
            //        objTransform.localPosition = Vector3.zero;
            //        gameObject.SetActive(true);
            //    }
            //});
        }

        public void CreateOffineFrameAndBegainExesering()
        {
            //EventDispatcher.Instance.DispatchEvent(new Ui_OffLineFrame_SetVisible(true));
        }

        private void PlayNewIcon(IEvent ievent)
        {
            var evn = ievent as MainUI_FlyIcon_Event;
            if (evn == null)
            {
                Logger.Error("MainUI_FlyIcon_Event  is Null ");
                return;
            }

            // to
            Transform toObj;
            UISprite toSprite = null;

            if (evn.SkillPos >= 0)
            {
                // 特殊处理飞向技能
                toObj = SkillList[evn.SkillPos];
                var skillObj = toObj.gameObject.transform.FindChild("Skill" + (evn.SkillPos + 1));
                if (skillObj != null)
                {
                    toSprite = skillObj.GetComponent<UISprite>();
                }
            }
            else
            {
                toObj = gameObject.transform.FindChild(evn.ToUiName);
                Logger.Debug("evn.ToUiName  = " + evn.ToUiName);
                if (toObj == null)
                {
                    Logger.Error("Get BookIconFrame is Null ");
                    return;
                }
                Logger.Debug(" --- toObj.gameObject.name =" + toObj.gameObject.name);
                toSprite = toObj.gameObject.GetComponent<UISprite>();
            }

            if (toSprite == null)
            {
                Logger.Error("Get BookIconFrame is Null ");
                return;
            }
            else
            {
                evn.FlyIcon.ToWidth = toSprite.width;
                evn.FlyIcon.ToHeight = toSprite.height;
            }

            FlyBookIconFrame(evn, toObj);
        }

        private void FlyBookIconFrame(MainUI_FlyIcon_Event evn, Transform toObj)
        {
            if (evn == null || evn.FlyIcon == null || evn.FlyIcon.FlyObject == null)
            {
                Logger.Debug("FlyBookIconFrame FlyIcon is Null ");
                return;
            }

            var frame = evn.FlyIcon.FlyObject.GetComponentInChildren<BookIconFrame>();
            if (frame == null)
            {
                Logger.Error("GetComponentInChildren <BookIconFrame.cs> is Null ");
                return;
            }
            Logger.Debug("FlyBookIconFrame ..... ");
            frame.Fly(toObj, evn.FlyIcon, (endPos) =>
            {
                Destroy(evn.FlyIcon.FlyObject);
                evn.Callback(endPos);
                //if (evn.SkillPos >= 0)
                //{
                //    EventDispatcher.Instance.DispatchEvent(new UIEvent_EquipSkillEffect(evn.SkillPos));
                //}
            });
        }

        private void PlayMayaFly(IEvent ievent)
        {
            try
            {
                var player = ObjManager.Instance.MyPlayer;
                var item = -1;
                var tableId = 602;
                if (player.RoleId == 0) // 战士
                {
                    item = 600100;
                    tableId = 620;
                }
                else if (player.RoleId == 1) // 法师
                {
                    item = 600101;
                    tableId = 621;
                }
                else if (player.RoleId == 2) // 弓手
                {
                    item = 600102;
                    tableId = 622;
                }
                if (item == -1)
                {
                    return;
                }

                var curRotation = Vector3.zero;
                var targetRotation = Vector3.zero;
                var curPos = Vector3.zero;
                var pos2 = Vector3.zero;
                var curScale = 1.0f;
                var targetScale = 1.0f;
                var time = 1.0f;

                var tbConfig = Table.GetClientConfig(tableId).Value;
                if (tbConfig == null)
                {
                    return;
                }

                var list = tbConfig.Split('|');
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i] == null)
                    {
                        return;
                    }
                    var varlist = list[i].Split('#');
                    if (i == 0) //旋转
                    {
                        var rot1 = varlist[0].Split(',');
                        curRotation = new Vector3(float.Parse(rot1[0]), float.Parse(rot1[1]), float.Parse(rot1[2]));

                        var rot2 = varlist[1].Split(',');
                        targetRotation = new Vector3(float.Parse(rot2[0]), float.Parse(rot2[1]), float.Parse(rot2[2]));
                    }
                    else if (i == 1) // 位移
                    {
                        var pos1 = varlist[0].Split(',');
                        var trans = GameLogic.Instance.MainCamera.transform;
                        curPos = trans.localToWorldMatrix.MultiplyPoint3x4(new Vector3(float.Parse(pos1[0]), float.Parse(pos1[1]), float.Parse(pos1[2])));

                        var pos_2 = varlist[1].Split(',');
                        pos2 = trans.localToWorldMatrix.MultiplyPoint3x4(new Vector3(float.Parse(pos_2[0]), float.Parse(pos_2[1]), float.Parse(pos_2[2])));
                    }
                    else if (i == 2) //缩放
                    {
                        curScale = float.Parse(varlist[0]);
                        targetScale = float.Parse(varlist[1]);
                    }
                    else if (i == 3)
                    {
                        time = float.Parse(list[i]);
                    }
                }

                EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(1));

                var topMount = player.GetMountPoint((int)MountPoint.RightWeapen).position;
                var go = UIManager.Instance.UIRoot.transform.Find("TYPE_BASE/MainUIFrame");
                if (go == null)
                {
                    EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(0));

                    player.EnterAutoCombat();
                    player.ChangeEquipModel(17, -1, item * 100 + 1);
                    GameUtils.ChangeEquip(item);
                    return;
                }
                var tmp = go.GetComponent<SpecialSceneLogic>();
                if (tmp == null)
                {
                    EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(0));

                    player.EnterAutoCombat();
                    player.ChangeEquipModel(17, -1, item * 100 + 1);
                    GameUtils.ChangeEquip(item);
                    return;
                }


                targetRotation = player.GetMountPoint((int)MountPoint.RightWeapen).rotation.eulerAngles;
                tmp.PlaySceneMoveAnimation(item, curRotation, targetRotation, curPos, pos2, topMount, curScale, targetScale, time, o =>
                {
                    EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(0));

                    player.EnterAutoCombat();
                    player.ChangeEquipModel(17, -1, item * 100 + 1);
                    GameUtils.ChangeEquip(item);
                });
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("PlayMayaFly Error{0}", e));
            }
        }


        private void PlayBuffIncreaseAnim(IEvent ievent)
        {
            if (null != BuffRoot)
            {
                var buffLogic = BuffRoot.GetComponentInChildren<MainBufferList>();
                if (null != buffLogic && buffLogic.BuffAnimation != null)
                {
                    if (!buffLogic.BuffAnimation.IsPlaying("BuffIncrease"))
                    {
                        buffLogic.BuffAnimation.Play("BuffIncrease");
                    }
                }
            }
        }

        private IEnumerator HideExpLableCoroutine()
        {
            yield return new WaitForSeconds(1.8f);
            ExpLable.SetActive(false);
        }

        private void LateUpdate()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif

            // 	        if (isEnable)
            // 	        {
            // 	            {
            // 	                var __list2 = Layout;
            // 	                var __listCount2 = __list2.Count;
            // 	                for (var __i2 = 0; __i2 < __listCount2; ++__i2)
            // 	                {
            // 	                    var layout = __list2[__i2];
            // 	                    {
            // 	                        layout.ResetLayout();
            // 	                    }
            // 	                }
            // 	            }
            // 	            isEnable = false;
            // 	        }

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        public void OnActivityTipClicked()
        {
            EventDispatcher.Instance.DispatchEvent(new ActivityTipClickedEvent());
        }

        public void OnClickAuto()
        {
            var e = new MainUiOperateEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickClosePkModel()
        {
            ChangeModel(false);
        }

        public void OnClickContactChat()
        {
            FriendArguments arg = new FriendArguments();
            arg.Tab = 0;
            arg.Type = -1;
            PlayerDataManager.Instance.NoticeData.NewEnemy = 0;
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SNSFrameUI, arg));
        }

        public void OnClickDailyActivity()
        {
            var e = new Show_UI_Event(UIConfig.PlayFrame);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickDungeonAuto()
        {
            var e = new MainUiOperateEvent(3);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClickFastReach()
        {
            var e = new ClickReachBtnEvent();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickDurable()
        {
            var e = new MainUiOperateEvent(2);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickExitDungeon()
        {
            var e = new DungeonBtnClick(100, eDungeonType.Invalid);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickHead()
        {
            var e = new Show_UI_Event(UIConfig.CharacterUI, new CharacterArguments());
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickInspire()
        {
            var e = new MainUiOperateEvent(5);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickMedicineWarn()
        {
            var arg = new StoreArguments { Tab = 0 };
            var e = new Show_UI_Event(UIConfig.StoreUI, arg);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClickFastReachMessageBoxOK()
        {
            var e = new OnClickFastReachMessageBoxOKEvent();
            EventDispatcher.Instance.DispatchEvent(e);

        }

        public void OnClickFastReachMessageBoxCancle()
        {
            var e = new OnClickFastReachMessageBoxCancleEvent();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickPetIslandBuyTili()
        {
            EventDispatcher.Instance.DispatchEvent(new OnClickBuyTiliEvent(0));
        }


        public void OnClickMinimap()
        {
            var e = new ShowSceneMapEvent(-1);
            if (GameLogic.Instance != null && GameLogic.Instance.Scene != null)
            {
                var sceneId = GameLogic.Instance.Scene.SceneTypeId;
                var tbScene = Table.GetScene(sceneId);
                if (tbScene == null)
                {
                    return;
                }
                if (tbScene.IsOpenMap == 0)
                {
                    GameUtils.ShowHintTip(279998);
                    return;
                }

                e.SceneId = tbScene.Id;

            }

            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickMayaBook()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_OpenEraBook());
        }

        public void OnClickModel()
        {
            var flag = !PkBg.activeSelf;
            ChangeModel(flag);
        }

        public void OnClickRardarTitle()
        {
            OnClickMinimap();
            //         RadarTitle.SetActive(!RadarTitle.activeSelf);
            //         if (RadarTitle.activeSelf)
            //         {
            //             UpArrow.SetActive(true);
            //             DownArrow.SetActive(false);
            //         }
            //         else
            //         {
            //             UpArrow.SetActive(false);
            //             DownArrow.SetActive(true);
            //         }
        }

        public void OnClickShowRechargeFrame()
        {
            var e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 1 });
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickSwitch()
        {
            showSKill = !showSKill;
            ChangeState(showSKill);
        }

        public void OnClickUseMedicine()
        {
            var e = new MainUiOperateEvent(4);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickUnionWar()
        {
            EventDispatcher.Instance.DispatchEvent(new MainUiOperateEvent(11));
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif

            EventDispatcher.Instance.RemoveEventListener(MainUiCharRadar.EVENT_TYPE, OnMainUiCharRadar);
            // EventDispatcher.Instance.RemoveEventListener(ResversUIEvent.EVENT_TYPE, RevresState);
            // EventDispatcher.Instance.RemoveEventListener(ShowHideUIUIEvent.EVENT_TYPE, ShowHideUI);
            //EventDispatcher.Instance.RemoveEventListener(RefreshHideIconPosUIUIEvent.EVENT_TYPE, RefreshTopHideUI);

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        private void OnDisable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            Binding.RemoveBinding();

            EventDispatcher.Instance.RemoveEventListener(ShowCountdownEvent.EVENT_TYPE, OnEvent_ShowCountdown);
            EventDispatcher.Instance.RemoveEventListener(SkillEquipMainUiAnime.EVENT_TYPE, OnEvent_EquipSkillAnim);
            EventDispatcher.Instance.RemoveEventListener(UIEvent_BuffIncreaseAnimation.EVENT_TYPE, PlayBuffIncreaseAnim);
            EventDispatcher.Instance.RemoveEventListener(UI_BlockMainUIInputEvent.EVENT_TYPE, OnEvent_BlockMainScreen);
            EventDispatcher.Instance.RemoveEventListener(SceneTransition_Event.EVENT_TYPE, OnEvent_SceneChange);
            EventDispatcher.Instance.RemoveEventListener(Event_UpdateMissionData.EVENT_TYPE, OnEvent_UpdateMission);
            EventDispatcher.Instance.RemoveEventListener(FirstEnterGameEvent.EVENT_TYPE, OnEvent_ShowFirstEnterGame);
            EventDispatcher.Instance.RemoveEventListener(HiedMieShiIcon_Event.EVENT_TYPE, HiedMieshiIcon);
            EventDispatcher.Instance.RemoveEventListener(ShowPopTalk_Event.EVENT_TYPE, ShowBossTalk);
            EventDispatcher.Instance.RemoveEventListener(MainUI_FlyIcon_Event.EVENT_TYPE, PlayNewIcon);
            EventDispatcher.Instance.RemoveEventListener(ShowHpTransitionEvent.EVENT_TYPE, OnShowHpTransitionEvent);
            EventDispatcher.Instance.RemoveEventListener(ShowHpTransitionSetEvent.EVENT_TYPE, OnShowHpTransitionSetEvent);

            //EventDispatcher.Instance.RemoveEventListener(MainUiCharRadar.EVENT_TYPE, OnMainUiCharRadar);
            EventDispatcher.Instance.RemoveEventListener(PlayMayaWeaponFlyEvent.EVENT_TYPE, PlayMayaFly);
            EventDispatcher.Instance.RemoveEventListener(PlayInspireEffectEvent.EVENT_TYPE, OnPlayInspireEffectEvent);
            EventDispatcher.Instance.RemoveEventListener(FlySkillOverPlayEffect_Event.EVENT_TYPE, FlySkillOverPlayEffect_EventCallBack);

            EventDispatcher.Instance.RemoveEventListener(UIEvent_FixIphoneX.EVENT_TYPE, FixIphoneX);

            var evt = new UIEvent_SkillButtonReleased(false, 0);
            EventDispatcher.Instance.DispatchEvent(evt);

            int skillpos = -1;
            if (learnSkillID != -1)
            {
                if (learnSkillID == 30 || learnSkillID == 133 || learnSkillID == 231) // 战士
                {
                    skillpos = 4;
                }
                PlayerDataManager.Instance.LearnSkill(learnSkillID, true, skillpos);
                learnSkillID = -1;
            }
            //防止重新进来时被阻止输入
            BlockMainScreen(false);
            BossTalk.gameObject.SetActive(false);
            StopTransition();
            EventDispatcher.Instance.DispatchEvent(new IsShowMainUIEvent(false));
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        private void OnEnable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            ChijiHpTransition = false;
            if (GameLogic.Instance.Scene != null)
                GameLogic.Instance.Scene.bOutSide = false;
            var controllerBase = UIManager.Instance.GetController(UIConfig.MainUI);
            if (controllerBase == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase.GetDataModel("Radar"));
            Binding.SetBindDataSource(controllerBase.GetDataModel("MainUI"));
            Binding.SetBindDataSource(controllerBase.GetDataModel("SelectTarget"));
            Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);
            Binding.SetBindDataSource(PlayerDataManager.Instance.RewardNotice);
            Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel.SkillData);
            var chatController = UIManager.Instance.GetController(UIConfig.ChatMainFrame);
            Binding.SetBindDataSource(chatController.GetDataModel(""));
            Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);
            var missionController = UIManager.Instance.GetController(UIConfig.MissionTrackList);
            Binding.SetBindDataSource(missionController.GetDataModel(""));
            Binding.SetBindDataSource(controllerBase.GetDataModel("DeviceInfo"));
            var teamController = UIManager.Instance.GetController(UIConfig.TeamFrame);
            Binding.SetBindDataSource(teamController.GetDataModel(""));
            Binding.SetBindDataSource(teamController.GetDataModel("TeamTargetChange"));
            var rechargeController = UIManager.Instance.GetController(UIConfig.RechargeFrame);
            Binding.SetBindDataSource(rechargeController.GetDataModel("RechargeDataModel"));
            Binding.SetBindDataSource(PlayerDataManager.Instance.WeakNoticeData);
            var firstChargeController = UIManager.Instance.GetController(UIConfig.FirstChargeFrame);
            if (firstChargeController != null)
            {
                Binding.SetBindDataSource(firstChargeController.GetDataModel(""));
            }
            var FieldMissionController = UIManager.Instance.GetController(UIConfig.FieldMissionUI);
            if (FieldMissionController != null)
            {
                Binding.SetBindDataSource(FieldMissionController.GetDataModel(""));
            }


            var rebornController = UIManager.Instance.GetController(UIConfig.RebornUi);
            if (rebornController != null)
            {
                Binding.SetBindDataSource(rebornController.GetDataModel(""));
            }

            var activityController = UIManager.Instance.GetController(UIConfig.ActivityUI);
            if (activityController != null)
            {
                Binding.SetBindDataSource(activityController.GetDataModel(""));
            }

            var wingChargeController = UIManager.Instance.GetController(UIConfig.WingChargeFrame);
            if (wingChargeController != null)
            {
                Binding.SetBindDataSource(wingChargeController.GetDataModel(""));
            }

            var rechargeActivityController = UIManager.Instance.GetController(UIConfig.RechargeActivityUI);
            if (rechargeActivityController != null)
            {
                Binding.SetBindDataSource(rechargeActivityController.GetDataModel(""));
            }

            var settingController = UIManager.Instance.GetController(UIConfig.SettingUI);
            if (null != settingController)
            {
                Binding.SetBindDataSource(settingController.GetDataModel(""));
            }

            var operationActivityController = UIManager.Instance.GetController(UIConfig.OperationActivityFrame);
            if (null != operationActivityController)
            {
                Binding.SetBindDataSource(operationActivityController.GetDataModel(""));
            }
            var unionWarController = UIManager.Instance.GetController(UIConfig.UnionWarFrame);
            if (null != unionWarController)
            {
                Binding.SetBindDataSource(unionWarController.GetDataModel(""));
            }
            EventDispatcher.Instance.AddEventListener(ShowCountdownEvent.EVENT_TYPE, OnEvent_ShowCountdown);
            EventDispatcher.Instance.AddEventListener(SkillEquipMainUiAnime.EVENT_TYPE, OnEvent_EquipSkillAnim);
            EventDispatcher.Instance.AddEventListener(UIEvent_BuffIncreaseAnimation.EVENT_TYPE, PlayBuffIncreaseAnim);
            EventDispatcher.Instance.AddEventListener(UI_BlockMainUIInputEvent.EVENT_TYPE, OnEvent_BlockMainScreen);
            EventDispatcher.Instance.AddEventListener(SceneTransition_Event.EVENT_TYPE, OnEvent_SceneChange);
            EventDispatcher.Instance.AddEventListener(Event_UpdateMissionData.EVENT_TYPE, OnEvent_UpdateMission);
            EventDispatcher.Instance.AddEventListener(FirstEnterGameEvent.EVENT_TYPE, OnEvent_ShowFirstEnterGame);
            EventDispatcher.Instance.AddEventListener(HiedMieShiIcon_Event.EVENT_TYPE, HiedMieshiIcon);
            EventDispatcher.Instance.AddEventListener(ShowPopTalk_Event.EVENT_TYPE, ShowBossTalk);
            EventDispatcher.Instance.AddEventListener(MainUI_FlyIcon_Event.EVENT_TYPE, PlayNewIcon);
            EventDispatcher.Instance.AddEventListener(ShowHpTransitionEvent.EVENT_TYPE, OnShowHpTransitionEvent);
            EventDispatcher.Instance.AddEventListener(ShowHpTransitionSetEvent.EVENT_TYPE, OnShowHpTransitionSetEvent);

            EventDispatcher.Instance.AddEventListener(PlayMayaWeaponFlyEvent.EVENT_TYPE, PlayMayaFly);
            EventDispatcher.Instance.AddEventListener(PlayInspireEffectEvent.EVENT_TYPE, OnPlayInspireEffectEvent);
            EventDispatcher.Instance.AddEventListener(FlySkillOverPlayEffect_Event.EVENT_TYPE, FlySkillOverPlayEffect_EventCallBack);
            EventDispatcher.Instance.AddEventListener(MieShiUiToggle_Event.EVENT_TYPE, RefreshMieshiTime);

            EventDispatcher.Instance.AddEventListener(UIEvent_FixIphoneX.EVENT_TYPE,
                FixIphoneX);

            if (isInint == true)
            {
                isInint = false;
                EventDispatcher.Instance.AddEventListener(MainUiCharRadar.EVENT_TYPE, OnMainUiCharRadar);
            }

            FightReady.gameObject.SetActive(false);
            PkBg.SetActive(false);
            var myPlayer = ObjManager.Instance.MyPlayer;
            if (null != myPlayer)
            {
                if (myPlayer.IsInSafeArea())
                {
                    showSKill = false;
                }
                else
                {
                    showSKill = true;
                }
                ChangeState(showSKill);
            }

            SkillMove.gameObject.SetActive(false);
            learnSkillID = -1;
            isEnable = true;

            if (countdownTime > Game.Instance.ServerTime)
            {
                if (countdownCoroutine != null)
                {
                    StopCoroutine(countdownCoroutine);
                }
                if (displayCountdownTimer)
                {
                    countdownCoroutine = StartCoroutine(ShowCountDown());
                }
            }
            currentBlockTime = 0;

            IControllerBase monsterController = UIManager.Instance.GetController(UIConfig.MonsterSiegeUI);


            EventDispatcher.Instance.DispatchEvent(new ShowComposFlag_Event());

            EventDispatcher.Instance.DispatchEvent(new IsShowMainUIEvent(true));
            RenfreshTime(true);


            if (null != BlockInputWidget)
            {
                if (BlockInputWidget.gameObject.activeSelf)
                {
                    BlockInputWidget.gameObject.SetActive(false);
                }
            }

            monsterController = UIManager.Instance.GetController(UIConfig.MonsterSiegeUI);
            if (monsterController != null)
            {
                MonsterSiegeUIFrameArguments ms = new MonsterSiegeUIFrameArguments();
                ms.Tab = 5;
                monsterController.RefreshData(ms);
                // return;
            }



            if (GVoiceManager.Instance.Open)
            {
                if (GVoiceManager.Instance.IsInRoom)
                {
                    EnterVoiceBtn.gameObject.SetActive(false);
                    ExitVoiceBtn.gameObject.SetActive(true);
                }
                else
                {
                    EnterVoiceBtn.gameObject.SetActive(true);
                    ExitVoiceBtn.gameObject.SetActive(false);

                    if (EnterVoiceEnterTipLabel == null)
                    {
                        EnterVoiceEnterTipLabel = EnterVoiceBtn.transform.FindChild("Panel/tipLabel").GetComponent<UILabel>();
                    }

                    if (EnterVoiceEnterTipLabel != null)
                    {
                        EnterVoiceEnterTipLabel.text = string.Format(GameUtils.GetDictionaryText(100002237), GVoiceManager.Instance.OnlineAnchorName);
                    }
                }
            }
            else
            {
                EnterVoiceBtn.gameObject.SetActive(false);
                ExitVoiceBtn.gameObject.SetActive(false);
            }


#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        private void FlySkillOverPlayEffect_EventCallBack(IEvent iEvent)
        {
            if (iEvent != null)
            {
                var ie = iEvent as FlySkillOverPlayEffect_Event;
                if (ie != null)
                {
                    if (ie.SkillPos >= 0)
                    {
                        if (UI_jinengEffect == null) return;
                        GameObject obj = Instantiate(UI_jinengEffect) as GameObject;
                        if (obj == null) return;
                        obj.transform.parent = SkillList[ie.SkillPos];
                        obj.transform.localScale = Vector3.one;
                        obj.transform.localPosition = Vector3.zero;
                        Destroy(obj, 3f);
                    }
                }
            }
        }





        public void RefreshSummonBtn(IEvent e)
        {
            if (SummonMonsterBtn != null)
            {//检测是否可以显示召唤按钮
                bool bShow = true;
                do
                {
                    {//功能开启
                        var tbGuidance = Table.GetGuidance(1002);
                        if (tbGuidance == null)
                            break;
                        if (-1 != tbGuidance.FlagPrepose && !PlayerDataManager.Instance.GetFlag(tbGuidance.FlagPrepose))
                        {
                            bShow = false;
                            break;
                        }
                    }

                    {//场景允许
                        var scene = GameLogic.Instance.Scene;
                        if (null != scene)
                        {
                            if (null != scene.TableScene)
                            {
                                bShow = scene.TableScene.CanSummonMonster == 1;
                            }
                        }
                    }
                    {
                        bShow = bShow && PlayerDataManager.Instance.mFightBook > 0;
                    }

                    SummonMonsterBtn.SetActive(false);
                } while (false);

            }
        }
        public void OnClickSummonMonster()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnSummonMonster());
        }
        public void OnExpHoverOut()
        {
            HideExpCo = NetManager.Instance.StartCoroutine(HideExpLableCoroutine());
        }

        public void OnExpHoverOver()
        {
            ExpLable.SetActive(true);
            if (HideExpCo != null)
            {
                NetManager.Instance.StopCoroutine(HideExpCo);
            }
        }

        public void OnInspireCancel()
        {
            var e = new MainUiOperateEvent(7);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnInspireDia()
        {
            var e = new MainUiOperateEvent(9);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnInspireGold()
        {
            var e = new MainUiOperateEvent(8);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnInspireOk()
        {
            var e = new MainUiOperateEvent(6);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnFirstEnterGameOk()
        {
            var e = new MainUiOperateEvent(10);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        private void OnMainUISwithState(IEvent ievent)
        {
            var e = ievent as MainUISwithState;
            showSKill = e.IsAttack;
            ChangeState(showSKill);
        }
        private void OnPlayInspireEffectEvent(IEvent ievent)
        {
            InspireEffect.SetActive(true);
            Invoke("EndInspireEffect", 0.8f);
        }
        private void EndInspireEffect()
        {
            InspireEffect.SetActive(false);
        }

        public void OnMissionClick()
        {
            if (!Team.value)
            {
                var scene = GameLogic.Instance.Scene;
                if (null != scene)
                {
                    if (null != scene.TableScene)
                    {
                        if (-1 == scene.TableScene.FubenId)
                        {
                            var e = new Show_UI_Event(UIConfig.MissionList);
                            EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(1));
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                    }
                }
            }

            EventDispatcher.Instance.DispatchEvent(new Event_MissionTabClick(0));
            EventDispatcher.Instance.DispatchEvent(new MissionOrTeamEvent(0));
        }

        public void OnMishiMissionClick()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_ShowMieshiFubenInfo());
        }

        public void OnMishiRankingClick()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_ShowMieshiRankingInfo());
        }


        public void OnRechageActivity()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeActivityUI));
        }

        public void OnRewardFastKey()
        {
            PlayerDataManager.Instance.RewardGotoUI();
        }

        private void OnEvent_SceneChange(IEvent ievent)
        {
            ChijiHpTransition = false;
            if (!gameObject.active)
            {
                return;
            }

            if (null != Transition)
            {
                return;
                //Transition.SetActive(true);
                //var tween = Transition.GetComponentInChildren<TweenAlpha>();
                //if (null != tween)
                //{
                //    tween.ResetToBeginning();
                //    tween.SetOnFinished(() => {

                //        if (PlayTransitionTime < 5)
                //        {
                //            tween.ResetToBeginning();
                //            tween.PlayForward();
                //        }
                //        else
                //        {
                //            PlayTransitionTime = 0;
                //            Transition.SetActive(false);
                //        }
                //        PlayTransitionTime++;

                //    });
                //    tween.PlayForward();
                //}
            }
        }

        private void OnEvent_UpdateMission(IEvent ievent)
        {
            if (null != MissionLayout)
            {
                MissionLayout.ResetLayout();
            }
        }

        private void OnMainUiCharRadar(IEvent ievent)
        {
            var e = ievent as MainUiCharRadar;
            if (e == null)
                return;

            var type = e.Type;
            var data = e.DataModel;
            if (type == 1)
            {
                CreateCharRadar(data);
            }
            else
            {
                RemoveCharRadar(data);
            }
        }
        private void CreateCharRadar(RararCharDataModel data)
        {
            var id = data.CharacterId;
            ComplexObjectPool.NewObject("UI/MainUI/CharCursor.prefab", o =>
            {
                if (data.CharType != 0)
                {
                    var charObj = ObjManager.Instance.FindCharacterById(id);
                    if (charObj == null || charObj.Dead)
                    {
                        ComplexObjectPool.Release(o, false, false);
                        return;
                    }
                }
                var oTransform = o.transform;
                //oTransform.parent = CharCursor.transform;
                if (CharCursor == null || CharCursor.transform == null)
                {
                    o.SetActive(false);
                    ComplexObjectPool.Release(o, false, false);
                    return;
                }

                oTransform.SetParentEX(CharCursor.transform);
                oTransform.localScale = Vector3.one;
                if (!o.activeSelf)
                    o.SetActive(true);
                var i = o.GetComponent<ListItemLogic>();
                i.Item = data;
                var r = o.GetComponent<BindDataRoot>();
                r.Source = data;

                itemLogicDict[data.CharacterId] = i;
            }, null, null, false, false, false);
        }
        private void RemoveCharRadar(RararCharDataModel data)
        {
            ListItemLogic obj;
            if (itemLogicDict.TryGetValue(data.CharacterId, out obj))
            {
                if (obj != null)
                {
                    ComplexObjectPool.Release(obj.gameObject, false, false);
                }
                itemLogicDict.Remove(data.CharacterId);
            }
        }

        private void OnEvent_ShowCountdown(IEvent ievent)
        {
            var e = ievent as ShowCountdownEvent;
            switch (e.Type)
            {
                case eCountdownType.BattleFight:
                case eCountdownType.BattleRelive:
                    {
                        if (countdownCoroutine != null)
                        {
                            StopCoroutine(countdownCoroutine);
                        }
                        displayCountdownTimer = true;
                        countdownTime = e.Time.AddSeconds(1);
                        countdownType = e.Type;
                        if (gameObject.activeSelf)
                        {
                            countdownCoroutine = StartCoroutine(ShowCountDown());
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void OnShowFirstCharge()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.FirstChargeFrame));
        }

        public void OnFunctionOnShow()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.FuctionTipFrame));
        }

        public void OnShowWingCharge()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.WingChargeFrame));
        }

        public void OnShowGuardFrame()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.GuardUI));
        }

        private void OnEvent_BlockMainScreen(IEvent ievent)
        {
            var e = ievent as UI_BlockMainUIInputEvent;
            var duration = e.Duration;
            BlockMainScreen(duration > 0);
        }

        public void OnShowSevenDay()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SevenDayReward));
        }

        public void OnShowStrong()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.NewStrongUI));
        }
        public void OnBatteryLevelUpBtn()
        {

        }

        public void OnShowTargetMenu()
        {
            var worldPos = UICamera.currentCamera.ScreenToWorldPoint(UICamera.lastTouchPosition);
            var localPos = SkillTarget.transform.root.InverseTransformPoint(worldPos);
            UIConfig.OperationList.Loction = localPos;

            var e = new MainUiOperateEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        private void OnEvent_ShowFirstEnterGame(IEvent ievent)
        {
            var e = ievent as FirstEnterGameEvent;
            if (e == null)
            {
                return;
            }

            if (e.Type)
            {
                if (FirstEnterGameObject != null)
                {
                    Destroy(FirstEnterGameObject);
                }
                ComplexObjectPool.NewObject("UI/MainUI/FirstEnterGame.prefab", go =>
                {
                    FirstEnterGameObject = go;

                    var objTransform = go.transform;
                    objTransform.SetParentEX(transform);
                    objTransform.localScale = Vector3.one;
                    objTransform.localPosition = Vector3.zero;
                    go.SetActive(true);

                    var btn = objTransform.FindChild("BtnOK");
                    if (btn != null)
                    {
                        var button = btn.GetComponent<UIButton>();
                        button.onClick.Clear();
                        button.onClick.Add(new EventDelegate(OnFirstEnterGameOk));
                    }
                });
            }
            else
            {
                if (FirstEnterGameObject != null)
                {
                    Destroy(FirstEnterGameObject);
                }
            }
        }

        private void OnEvent_EquipSkillAnim(IEvent ievent)
        {
            var e = ievent as SkillEquipMainUiAnime;
            var skillId = e.SkillId;
            var index = e.Index;
            var call = e.Callback;
            if (index < 0)
            {
                Logger.Error("OnSkillEquipMainUiAnime Error Index  = {0}", index);
            }
            learnSkillID = skillId;
            var tbSkill = Table.GetSkill(skillId);
            GameUtils.SetSpriteIcon(SkillMove, tbSkill.Icon);
            SkillMove.gameObject.SetActive(true);
            SkillName.text = tbSkill.Name;
            var tween = SkillMove.GetComponent<TweenPosition>();
            tween.to = SkillList[index].transform.localPosition;
            tween.ResetToBeginning();
            tween.PlayForward();
            SkillNameTween.ResetToBeginning();
            SkillNameTween.PlayForward();

            if (GameSetting.Instance.EnableNewFunctionTip)
            {
                BlockMainScreen(true);
            }

            tween.onFinished.Clear();
            tween.onFinished.Add(new EventDelegate(() =>
            {
                learnSkillID = -1;
                PlayerDataManager.Instance.LearnSkill(skillId, true, index);
                SkillMove.gameObject.SetActive(false);
                BlockMainScreen(false);
                if (null != call)
                {
                    call();
                }
            }));

            if (null != SkillEffectAni)
            {
                // 			var spr = SkillEffectAni.GetComponent<UISprite>();
                // 			if(null!=spr)
                // 			{
                // 				spr.enabled = true;
                // 			}
                SkillEffectAni.Play();
            }
        }



        //灭世之战按钮
        void InitMonsterSiege()
        {
            Show_UI_Event eventMonster = new Show_UI_Event(UIConfig.MonsterSiegeUI);
            eventMonster.Args = new MonsterSiegeUIFrameArguments();
            eventMonster.Args.Tab = 5;
            EventDispatcher.Instance.DispatchEvent(eventMonster);
        }
        public void OnMonsterSiegeBtn()
        {
            //   EventDispatcher.Instance.DispatchEvent(new MieShiSetActivityId_Event(5));
            Show_UI_Event eventMonster = new Show_UI_Event(UIConfig.MonsterSiegeUI);
            EventDispatcher.Instance.DispatchEvent(eventMonster);
        }

        public void OnClickMainActivityBtn()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.FieldMissionUI));
        }

        public void OnBtnGoToNextTower()
        {
            int cur = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e623);
            var tb = Table.GetClimbingTower(cur + 1);
            if (tb != null)
            {
                GameUtils.EnterFuben(tb.FubenId);
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ClimbingTowerRewardUI));
            }
            else
            {
                GameUtils.ExitFuben();
            }
        }



        public void RewardMessageGoto()
        {
            EventDispatcher.Instance.DispatchEvent(new RewardMessageOpetionClick(1));
        }

        public void RewardMessageRecharge()
        {
            EventDispatcher.Instance.DispatchEvent(new RewardMessageOpetionClick(0));
        }

        private void ChangeModel(bool show)
        {
            if (show)
            {
                if (PlayerDataManager.Instance.IsInPvPScnen())
                {
                    return;
                }
                if (PlayerDataManager.Instance.IsInBossHomeScnen())
                {
                    var _e = new ShowUIHintBoard(274070);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
                //MissionRoot.SetActive(false);
            }
            else
            {
                //MissionRoot.SetActive(true);
            }
            PkBg.SetActive(show);
        }

        private void ChangeState(bool state)
        { }

        //-------------------------------------------------FightReady
        private IEnumerator ShowCountDown()
        {
            var target = countdownTime;
            var type = countdownType;
            while (target >= Game.Instance.ServerTime)
            {
                yield return new WaitForSeconds(0.3f);
                var dif = (int)((target - Game.Instance.ServerTime).TotalSeconds);
                if (dif == 1)
                {
                    if (type == eCountdownType.BattleFight)
                    {
                        // 开始战斗！
                        FightReady.text = GameUtils.GetDictionaryText(270113);
                    }
                    else
                    {
                        FightReady.gameObject.SetActive(false);
                    }
                }
                else if (dif == 0)
                {
                    FightReady.gameObject.SetActive(false);
                }
                else
                {
                    FightReady.text = (dif - 1).ToString();
                    if (FightReady.gameObject.activeSelf == false)
                    {
                        FightReady.gameObject.SetActive(true);
                    }
                }
            }
            FightReady.gameObject.SetActive(false);
            FightReady.text = "";
            displayCountdownTimer = false;
            countdownCoroutine = null;
        }

        private void BlockMainScreen(bool flag)
        {
            if (null == BlockInputWidget)
            {
                return;
            }

            if (flag)
            {
                BlockInputWidget.gameObject.SetActive(true);
            }
            else
            {
                BlockInputWidget.gameObject.SetActive(false);
            }
            currentBlockTime = 0;
        }

        private void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif

            isInint = true;

            InitFixIphonex();
            //目前锁定旋转所以上来设置一次就行了
            UpdateIphoneXSafeArea(-1);
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
            //控制模块

            InspireEffect.SetActive(false);
            var PkBtnCount0 = PkBtn.Count;
            for (var i = 0; i < PkBtnCount0; i++)
            {
                var btn = PkBtn[i];
                var j = i;
                btn.onClick.Add(new EventDelegate(() => { ChangePkModel(j); }));
            }

            SetupBufferList();
            //CreateOffineFrame();

            countdownTime = Game.Instance.ServerTime;
            countdownType = eCountdownType.BattleFight;

            //撑大字的纹理
            if (!s_isPretreatString)
            {
                s_isPretreatString = true;
                var txt = ExpLable.GetComponent<UILabel>();
                if (null != txt && null != txt.font && null != txt.font.dynamicFont)
                {
                    txt.font.dynamicFont.RequestCharactersInTexture(GameSetting.Instance.PrepareString);
                }
            }

            //             isInint = true; refreshDownListMenu(DragList);
            // 			var scene = GameLogic.Instance.Scene;
            // 		    if (null != scene)
            // 		    {
            // 			    if (null != scene.TableScene)
            // 			    {
            // 				    if (1==scene.TableScene.IsShowMainUI)
            // 				    {						               
            //                         //ShowMainButton();
            //                         //ShowUpMainButton();                         
            //                         //HideMainButton();    
            //                         DwnPlayAnima(true);             
            // 				    }
            // 				    else
            // 				    {
            // 						//HideMainButton();
            //                         //HideUpMainButton();    
            //                         //ShowMainButton();
            //                         HideMainButton();
            // 				    }
            // 			    }
            // 		    }
            //DwnPlayAnima(true);
            /*
            if (GVoiceManager.Instance.Open)
            {
                PopList.gameObject.SetActive(true);
                if (GVoiceManager.Instance.IsInRoom)
                {
                    PopList.value = PopList.items[PopList.items.Count - 1];
                    VoiceType.text = PopList.value;
                }
            }
            else
            {
                PopList.gameObject.SetActive(false);
            }
            */

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif

        }

        float HP;

        //判断血量显示警告
        public void ShowPlayerHpTransition()
        {

            if (PlayerHpTransition != null)
            {
                HP = (PlayerDataManager.Instance.PlayerDataModel.Attributes.HpPercent) * 100;
                if (HP < Playerhp && HP > 0 || IsShowHpTransition || PlayerDataManager.Instance.InputControlHPEffect || ChijiHpTransition)
                {
                    PlayerHpTransition.SetActive(true);
                }
                else
                {
                    PlayerHpTransition.SetActive(false);
                }
            }

        }
        public void ShowBossTalk(IEvent e)
        {
            var eventShow = e as ShowPopTalk_Event;
            if (BossTalk != null && BossTalk.gameObject != null)
            {
                BossTalk.gameObject.SetActive(true);
                BossTalk.text = eventShow.talk;
                BossTalk.transform.GetComponent<UILabel>().color = Color.red;
                // StartCoroutine(Show()); 
                mBossTalkTimer = Time.realtimeSinceStartup + 10.0f;
            }

        }

        private void OnShowHpTransitionSetEvent(IEvent ievent)
        {
            ShowHpTransitionSetEvent e = ievent as ShowHpTransitionSetEvent;
            if (e != null)
            {
                ChijiHpTransition = e.bShow;
            }
        }
        private void OnShowHpTransitionEvent(IEvent ievent)
        {
            if (IsShowHpTransition == true)
                return;
            IsShowHpTransition = true;
            Invoke("StopHpTransition", 3f);
        }
        private void StopHpTransition()
        {
            IsShowHpTransition = false;
        }
        public IEnumerator Show()
        {
            yield return new WaitForSeconds(4);
            BossTalk.text = "";
            BossTalk.gameObject.SetActive(false);
        }
        public void HiedMieshiIcon(IEvent e)
        {
            MieshiBtn.SetActive(false);
        }
        private void StopTransition()
        {
            if (null != Transition)
            {
                var tween = Transition.GetComponentInChildren<TweenAlpha>();
                if (null != tween)
                {
                    tween.enabled = false;
                }
                Transition.SetActive(false);
            }
        }

        private void Update()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            if (null != BlockInputWidget)
            {
                if (BlockInputWidget.gameObject.active)
                {
                    currentBlockTime += Time.deltaTime;
                    if (currentBlockTime > BlockContinueTime)
                    {
                        BlockInputWidget.gameObject.SetActive(false);
                        currentBlockTime = 0;
                    }
                }
                else
                {
                    if (0 != currentBlockTime)
                    {
                        currentBlockTime = 0;
                    }
                }
            }

            if (LastDir != Input.deviceOrientation)
            {
                //AutoFixIphoneX();
                LastDir = Input.deviceOrientation;
            }

            ShowPlayerHpTransition();
            CheckBossTalk();
            RenfreshTime();

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }
        void CheckBossTalk()
        {
            if (mBossTalkTimer < 0f)
                return;
            if (mBossTalkTimer < Time.realtimeSinceStartup)
            {
                BossTalk.gameObject.SetActive(false);
            }
        }

        private float mieshiTime = 0.0f;

        void RefreshMieshiTime(IEvent e)
        {
            RenfreshTime(true);
        }
        void RenfreshTime(bool bForce = false)//刷新时间
        {
            if (bForce == true)
            {
                mieshiTime = 0.0f;
            }
            else
            {
                mieshiTime += Time.deltaTime;

                if (mieshiTime < 10.0f)
                {
                    return;
                }

                mieshiTime -= 60.0f;
            }
            if (MieshiMonsterDataModel == null)
            {
                var controller = UIManager.Instance.GetController(UIConfig.MonsterSiegeUI);
                if (controller != null)
                {
                    MieshiMonsterDataModel = controller.GetDataModel(string.Empty) as MonsterDataModel;
                }
            }

            if (MieshiMonsterDataModel == null)
                return;

            if (MieshiMonsterDataModel.CurActivityID <= 0)
            {
                return;
            }

            //活动开始的时候
            //if (MieshiMonsterDataModel.ActivityState != 0)
            //{
            //    TimeLabel.color = Color.green;
            //    CountDownLabel.color = Color.green;
            //}
            //else
            //{
            //    TimeLabel.color = Color.red;
            //    CountDownLabel.color = Color.red;
            //}
            TimeShow.color = Color.green;
            TimeLabel.color = Color.red;


            TimeSpan tsSpan = MieshiMonsterDataModel.ActivityTime - DateTime.Now;


            TimeLabel.gameObject.SetActive(tsSpan.Milliseconds > 0);
            if (tsSpan.Milliseconds > 0)
            {//未开启
                {//上半部分
                    DateTime d1 = MieshiMonsterDataModel.ActivityTime.Date;

                    DateTime d2 = DateTime.Today;
                    TimeSpan ts = d1 - d2;
                    if (ts.Days > 0)
                        TimeLabel.text = string.Format(GameUtils.GetDictionaryText(300000150), ts.Days);
                    else
                    {
                        if (MieshiMonsterDataModel.ActivityTime.Minute >= 10)
                        {
                            string t = string.Format("{0}:{1}", MieshiMonsterDataModel.ActivityTime.Hour,
                                MieshiMonsterDataModel.ActivityTime.Minute);
                            TimeLabel.text = string.Format(GameUtils.GetDictionaryText(291017), t);
                        }
                        else
                        {
                            string t = string.Format("{0}:0{1}", MieshiMonsterDataModel.ActivityTime.Hour,
                                MieshiMonsterDataModel.ActivityTime.Minute);
                            TimeLabel.text = string.Format(GameUtils.GetDictionaryText(291017), t);
                        }

                    }
                }
                {//下半部分

                    if (tsSpan.TotalHours > 10)
                    {
                        TimeShow.text = GameUtils.GetDictionaryText(300000152);
                    }
                    else if (tsSpan.TotalHours > 8)
                    {
                        TimeShow.text = GameUtils.GetDictionaryText(300000153);
                    }
                    else if (tsSpan.TotalMinutes > 20)
                    {
                        TimeShow.text = GameUtils.GetDictionaryText(300000154);
                    }
                    else
                    {
                        TimeShow.text = GameUtils.GetDictionaryText(300000155);
                    }
                }
            }
            else
            {//已开启
                TimeShow.text = GameUtils.GetDictionaryText(300000155);

            }




            //            if (MieshiMonsterDataModel.ActivityState == 2 || MieshiMonsterDataModel.ActivityState == 3)
            //            {//战斗中
            ////                TimeLabel.text = Table.GetDictionary(300000076).Desc[0];
            //            }
            //            else if ((MieshiMonsterDataModel.ActivityState > 4))
            //            {//活动结束
            ////                TimeLabel.text = Table.GetDictionary(300000077).Desc[0];
            //            }
            //            else
            //            {//未开始 
            //                if (tsSpan.Milliseconds<=0)
            //                {
            //                    TimeLabel.text = null;
            //                }
            //                else
            //                {
            //                    TimeLabel.text = tsSpan.Days.ToString().PadLeft(1, '0') + "天"
            //                        + tsSpan.Hours.ToString().PadLeft(2, '0') + ":" + tsSpan.Minutes.ToString().PadLeft(2, '0') + ":" + tsSpan.Seconds.ToString().PadLeft(2, '0');
            //                }

            //            }

            if (tsSpan.Days < 1)
            {
                LiuGuang.SetActive(true);
            }
            else
            {
                LiuGuang.SetActive(false);
            }
        }
        public void UpdateTimer()
        {
            EventDispatcher.Instance.DispatchEvent(new UpdateActivityTipTimerEvent());
        }

        public void Listen<T>(T message)
        {
            isEnable = true;
        }


        #region mainui Dungeon

        public void OnclickDungeonQueueShow()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ShowDungeonQueue(1));
        }

        public void OnclickDungeonQueueHide()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ShowDungeonQueue(0));
        }

        public void OnclickDungeonQueueCloseWins()
        {
            var e = new UIEvent_CloseDungeonQueue(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        #endregion

        public void BuAcientBattleFieldEnergy()
        {
            EventDispatcher.Instance.DispatchEvent(new UIAcientBattleFieldOperationClickEvent(1));
        }

        public void OnEnterBtnClick()
        {
            if (GVoiceManager.Instance.IsJoining)
            {
                return;
            }
            if (GVoiceManager.Instance.IsAnchor)
            {
                var ret = GVoiceManager.Instance.JoinAnchorRoom();
                if (ret)
                {
                    EnterVoiceBtn.gameObject.SetActive(false);
                    ExitVoiceBtn.gameObject.SetActive(true);
                }
            }
            else
            {
                NetManager.Instance.StartCoroutine(OnEnterBtnEnumerator());
            }
        }

        public IEnumerator OnEnterBtnEnumerator()
        {
            var ret = false;
            var msg = NetManager.Instance.GetAnchorIsInRoom(0);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                ret = msg.Response == 1;
            }
            if (ret)
            {
                GVoiceManager.Instance.JoinAnchorRoom();
                EnterVoiceBtn.gameObject.SetActive(false);
                ExitVoiceBtn.gameObject.SetActive(true);
            }
            else
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(210600)));
                EventDispatcher.Instance.DispatchEvent(new AnchorEnterRoomEvent(false));
            }
        }


        public void OnExitBtnClick()
        {
            if (GVoiceManager.Instance.IsJoining)
            {
                return;
            }
            var ret = GVoiceManager.Instance.QuitRoom();
            if (ret)
            {
                EnterVoiceBtn.gameObject.SetActive(true);
                ExitVoiceBtn.gameObject.SetActive(false);
                EventDispatcher.Instance.DispatchEvent(new AnchorEnterRoomEvent(false));
            }
        }

        public void OnMenuChanged()
        {
            int idx = PopList.items.IndexOf(PopList.value);

            bool ret = false;
            if ((int)GVoiceManager.RoomType.Silence == idx)
            {
                ret = GVoiceManager.Instance.QuitRoom();
            }
            else if ((int)GVoiceManager.RoomType.Anchor == idx)
            {
                ret = GVoiceManager.Instance.JoinAnchorRoom();
            }
            else if ((int)GVoiceManager.RoomType.Team == idx)
            {
                ret = GVoiceManager.Instance.JoinTeamRoom();
            }
            else if ((int)GVoiceManager.RoomType.League == idx)
            {
                ret = GVoiceManager.Instance.JoinNationalRoom();
            }

            if (!ret)
            {
                if (!GVoiceManager.Instance.IsInRoom)
                {
                    PopList.value = PopList.items[0];
                }
                else
                {
                    var type = (int)GVoiceManager.Instance.CurrentRoomType;
                    if (type >= 0 && type < PopList.items.Count)
                    {
                        PopList.value = PopList.items[type];
                    }

                }
            }
            VoiceType.text = PopList.value;
        }

        public void OnClickSurvey()
        {
           
            // if (true == PlayerDataManager.Instance.NoticeData.bSurvey)
             EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SurveyUI));
        }

        public void OnClickChooseCheckenSkill0()
        {
            EventDispatcher.Instance.DispatchEvent(new OnChooseCheckenSkillUpEvent(0));
        }
        public void OnClickChooseCheckenSkill1()
        {
            EventDispatcher.Instance.DispatchEvent(new OnChooseCheckenSkillUpEvent(1));
        }
        public void OnClickChooseCheckenSkill2()
        {
            EventDispatcher.Instance.DispatchEvent(new OnChooseCheckenSkillUpEvent(2));
        }
        public void OnClickGotoChicken()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_GoToChicken());
            
        }

    }
}