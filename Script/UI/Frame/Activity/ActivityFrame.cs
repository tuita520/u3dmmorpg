using ScriptManager;
using System;
#region using

using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;


#endregion

namespace GameUI
{
    public class ActivityFrame : MonoBehaviour
    {
        private UILabel lb_CD_EMO;
        private GameObject obj_CD_EMO;
        private int CDTime = 5;
        private UILabel lb_CD;
        private GameObject obj_CD;
        //private UIButton btn_WorldSpeak;
        private UIButton btn_TeamState;//队伍状态
        private UIButton btn_AutoMatch;
        private UIButton btn_AutoMatchStop;

        public BindDataRoot Binding;
        private NewActivityDataModel dataModel;
        public UIDragRotate DragRorate;
        //public TimerLogic HomePageTimerLogic;
        private IControllerBase controller;
        public CreateFakeCharacter ModelRoot;
        private Vector3 rootOriPosModel;
        private Transform rootTransModel;
        private bool removeBinding = true;

        private void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif

            btn_AutoMatchStop = transform.FindChildRecursive("AutoMatchStop").GetComponent<UIButton>();
            if (null != btn_AutoMatchStop) EventDelegate.Add(btn_AutoMatchStop.onClick,
                 new EventDelegate(() => { onclickbtn_AutoMatchStop(btn_AutoMatchStop.gameObject); }));

            //世界喊话
            //btn_WorldSpeak = transform.FindChildRecursive("WorldSpeak").GetComponent<UIButton>();
            //if (null != btn_WorldSpeak) EventDelegate.Add(btn_WorldSpeak.onClick,
            //     new EventDelegate(() => { onclickbtn_WorldSpeak(btn_WorldSpeak.gameObject); }));

            //队伍状态
            btn_TeamState = transform.FindChildRecursive("TeamState").GetComponent<UIButton>();
            if (null != btn_TeamState) EventDelegate.Add(btn_TeamState.onClick,
                 new EventDelegate(() => { onclickbtn_TeamState(btn_TeamState.gameObject); }));

            var TeammemberState = transform.FindChildRecursive("TeammemberState").GetComponent<UIButton>();
            if (null != TeammemberState) EventDelegate.Add(TeammemberState.onClick,
                 new EventDelegate(() => { onclickbtn_TeamState(TeammemberState.gameObject); }));

            var TeammemberState1 = transform.FindChildRecursive("TeammemberState1").GetComponent<UIButton>();
            if (null != TeammemberState) EventDelegate.Add(TeammemberState1.onClick,
                 new EventDelegate(() => { onclickbtn_TeamState(TeammemberState1.gameObject); }));
            
            btn_AutoMatch = transform.FindChildRecursive("AutoMatch").GetComponent<UIButton>();
            if (null != btn_AutoMatch) EventDelegate.Add(btn_AutoMatch.onClick,
                 new EventDelegate(() => { onclickbtn_AutoMatch(btn_AutoMatch.gameObject); }));
            obj_CD = transform.FindChildRecursive("CD").gameObject;
            if (null != obj_CD)
                obj_CD.SetActive(false);
            lb_CD = transform.FindChildRecursive("CDtime").GetComponent<UILabel>();

            btn_AutoMatchStop = transform.FindChildRecursive("AutoMatchStop_EMO").GetComponent<UIButton>();
            if (null != btn_AutoMatchStop) EventDelegate.Add(btn_AutoMatchStop.onClick,
                 new EventDelegate(() => { onclickbtn_AutoMatchStop(btn_AutoMatchStop.gameObject); }));

            //世界喊话
            //btn_WorldSpeak = transform.FindChildRecursive("WorldSpeak_EMO").GetComponent<UIButton>();
            //if (null != btn_WorldSpeak) EventDelegate.Add(btn_WorldSpeak.onClick,
            //     new EventDelegate(() => { onclickbtn_WorldSpeak(btn_WorldSpeak.gameObject); }));

            //队伍状态
            btn_TeamState = transform.FindChildRecursive("TeamState_EMO").GetComponent<UIButton>();
            if (null != btn_TeamState) EventDelegate.Add(btn_TeamState.onClick,
                 new EventDelegate(() => { onclickbtn_TeamState(btn_TeamState.gameObject); }));

            btn_AutoMatch = transform.FindChildRecursive("AutoMatch_EMO").GetComponent<UIButton>();
            if (null != btn_AutoMatch) EventDelegate.Add(btn_AutoMatch.onClick,
                 new EventDelegate(() => { onclickbtn_AutoMatch(btn_AutoMatch.gameObject); }));
            obj_CD_EMO = transform.FindChildRecursive("CD_EMO").gameObject;
            if (null != obj_CD_EMO)
                obj_CD_EMO.SetActive(false);
            lb_CD_EMO = transform.FindChildRecursive("CDtime_EMO").GetComponent<UILabel>();
#if !UNITY_EDITOR
	try
	{
#endif
            rootTransModel = ModelRoot.transform;
#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }

        private void FakeObjectCreate(int dataId)
        {
            ModelRoot.DestroyFakeCharacter();
            if (-1 == dataId)
            {
                return;
            }

            var tableNpc = Table.GetCharacterBase(dataId);
            if (null == tableNpc)
            {
                return;
            }

            ModelRoot.Create(dataId, null, character =>
            {
                character.SetScale(tableNpc.CameraMult / 10000.0f);
                character.ObjTransform.localRotation = Quaternion.identity;
                rootTransModel.localPosition = rootOriPosModel + new Vector3(0, tableNpc.CameraHeight / 10000.0f, 0);
                character.PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                DragRorate.Target = character.transform;
            });
        }

        public void OnBtnDynamicActivityQueueClicked()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.DynamicActivity_Queue));
        }

        public void OnBtnGotoBoss()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.Activity_GotoMonster));
        }

        public void OnBtnFlytoBoss()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.Activity_FlytoMonster));
        }

        public void OnBtnQueueClicked()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.Activity_Queue));
        }

        public void OnBtnGetDoubleExp()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.Activity_GetDoubleExp));
        }

        public void OnButtonClose()
        {
            // 	        if (dataModel.PageId > 0 && dataModel.FirstPageId == 0)
            // 	        {
            // 	            dataModel.index = 0;
            // 	        }
            // 	        else
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ActivityUI));
            }

            EventDispatcher.Instance.DispatchEvent(new ActivityClose_Event());
        }

        public void OnButtonEnterActivity()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.Activity_Enter));
        }

        public void OnButtonEnterDynamicActivity()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.DynamicActivity_Enter));
        }

        private void OnCloseUI(IEvent ievent)
        {
            var e = ievent as CloseUiBindRemove;
            if (e.Config != UIConfig.ActivityUI)
            {
                return;
            }
            if (e.NeedRemove == 0)
            {
                removeBinding = false;
            }
            else
            {
                if (removeBinding == false)
                {
                    DeleteBindEvent();
                }
                removeBinding = true;
            }
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            ModelRoot.DestroyFakeCharacter();
            if (removeBinding == false)
            {
                DeleteBindEvent();
            }
            removeBinding = true;
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
            ModelRoot.DestroyFakeCharacter();
            FakeObjectCreate(-1);
            if (removeBinding)
            {
                DeleteBindEvent();
            }

            EventDispatcher.Instance.RemoveEventListener(UIEvent_NewActivityModelChangeEvent.EVENT_TYPE, ChangeModelId);
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
            if (removeBinding)
            {
                EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUI);

                controller = UIManager.Instance.GetController(UIConfig.ActivityUI);
                if (controller == null)
                {
                    return;
                }
                dataModel = controller.GetDataModel("") as NewActivityDataModel;

                dataModel.PropertyChanged += OnEventPropertyChanged;

                Binding.SetBindDataSource(controller.GetDataModel(""));
                Binding.SetBindDataSource(controller.GetDataModel("Team"));
            }
            //FakeObjectCreate(dataModel.BossDataId);
            removeBinding = true;
            ChangeModelId();
            EventDispatcher.Instance.AddEventListener(UIEvent_NewActivityModelChangeEvent.EVENT_TYPE, ChangeModelId);

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        private void OnEventPropertyChanged(object o, PropertyChangedEventArgs args)
        {
            // 	        if (args.PropertyName == "BossDataId")
            // 	        {
            //                 if (dataModel == null || dataModel.tabModel == null ||
            //                     dataModel.tabModel.Count <= dataModel.CurTabIndex || dataModel.tabModel[dataModel.CurTabIndex].cells.Count <= dataModel.CurSecondUiIndex)
            //                 {
            //                     return;
            //                 }
            //                 FakeObjectCreate(dataModel.tabModel[dataModel.CurTabIndex].cells[dataModel.CurSecondUiIndex].worldMosnterBtns.ModelId);
            // 	        }
        }

        /// <summary>
        /// ----------------------------------------------------------------------------------NEW FUNC BEGIN---------------------------------------------------------------------------------------------------------------------
        /// </summary>
        private void ChangeModelId(IEvent ievent)
        {
            ChangeModelId();
        }
        private void ChangeModelId()
        {
            if (dataModel == null || dataModel.tabModel == null ||
                dataModel.tabModel.Count <= dataModel.CurTabIndex || dataModel.tabModel[dataModel.CurTabIndex].cells.Count <= dataModel.CurSecondUiIndex)
            {
                return;
            }
            FakeObjectCreate(dataModel.tabModel[dataModel.CurTabIndex].cells[dataModel.CurSecondUiIndex].worldMosnterBtns.ModelId);
        }

        public void OnTab1Click()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityTabClickEvent(0));
        }
        public void OnTab2Click()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityTabClickEvent(1));
        }
        public void OnTab3Click()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityTabClickEvent(2));
        }
        public void OnTab4Click()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityTabClickEvent(3));
        }

        public void OnClickCellOne()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityCellClickEvent(0));
        }
        public void OnClickCellTwo()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityCellClickEvent(1));
        }
        public void OnClickCellThree()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityCellClickEvent(2));
        }
        public void OnClickCellFour()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityCellClickEvent(3));
        }
        public void OnClickCellFive()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityCellClickEvent(4));
        }

        public void OnCloseSecondUI()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityCloseSecUIEvent());
        }

        /// <summary>
        /// ---------------------------------------------------------------------------------NEW FUNC END-------------------------------------------------------------------------------------------------------
        /// </summary>

        public void OnTabBloodCastle()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ActivityTabSelectEvent(1));
        }

        public void OnTabDevilSquare()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ActivityTabSelectEvent(0));
        }

        public void OnTabGoldArmy()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ActivityTabSelectEvent(4));
        }

        public void OnTabMapCommander()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ActivityTabSelectEvent(3));
        }

        public void OnTabWorldBoss()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ActivityTabSelectEvent(2));
        }

        private void DeleteBindEvent()
        {
            Binding.RemoveBinding();
            dataModel.PropertyChanged -= OnEventPropertyChanged;
            EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUI);
        }
        public void OnClickBuyTili()
        {
            EventDispatcher.Instance.DispatchEvent(new OnClickBuyTiliEvent(0));
        }

        private void Start()
        {
#if !UNITY_EDITOR
	try
	{
#endif

            rootOriPosModel = rootTransModel.localPosition;
            //HomePageTimerLogic.TargetTime = Game.Instance.ServerTime.AddYears(10);

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        public void UpdateMainPageTimer()
        {
            EventDispatcher.Instance.DispatchEvent(new UpdateActivityTimerEvent(UpdateActivityTimerType.MainPage));
        }

        public void UpdateTimer()
        {
            EventDispatcher.Instance.DispatchEvent(new UpdateActivityTimerEvent(UpdateActivityTimerType.Single));
        }

        //void onclickbtn_WorldSpeak(GameObject obj)
        //{
        //    PlayerDataManager.Instance.NoticeData.DungeonType = 0;
        //    EventDispatcher.Instance.DispatchEvent(new ActivityWorldSpeackClickEvent());

        //    this.InvokeRepeating("UpdateCD", 1f, 1f);
        //    CDTime = 5;
        //    var form = Table.GetDictionary(220139);

        //    if (null != form)
        //    {
        //        lb_CD.text = string.Format(form.Desc[0], CDTime.ToString());
        //        lb_CD_EMO.text = string.Format(form.Desc[0], CDTime.ToString());
        //    }
        //    obj_CD.SetActive(true);
        //    obj_CD_EMO.SetActive(true);
        //}

        void onclickbtn_TeamState(GameObject obj)
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.TeamFrame));
        }

        void onclickbtn_AutoMatch(GameObject obj)
        {
            PlayerDataManager.Instance.NoticeData.DungeonType = 0;
            EventDispatcher.Instance.DispatchEvent(new ActivityAutoMatchClickEvent());
        }
        void onclickbtn_AutoMatchStop(GameObject obj)
        {
            EventDispatcher.Instance.DispatchEvent(new TeamWorldAutoMatchNewEvent());
        }
        void UpdateCD()
        {
            CDTime = CDTime - 1;
            int tim = (int)CDTime;
            if (CDTime < 0)
            {
                this.CancelInvoke("UpdateCD");
                if (null != obj_CD) obj_CD.SetActive(false);
                if (null != obj_CD_EMO) obj_CD_EMO.SetActive(false);
                return;

            }
            var form = Table.GetDictionary(220139);

            if (null != form)
            {
                lb_CD.text = string.Format(form.Desc[0], tim.ToString());
                lb_CD_EMO.text = string.Format(form.Desc[0], tim.ToString());
            }
        }

        public void OnClickSearchDyn()
        {
            //var e = new Show_UI_Event(UIConfig.TeamFrame);
            //EventDispatcher.Instance.DispatchEvent(e);
            //var e1 = new UIEvent_TeamFrame_NearTeam();
            //EventDispatcher.Instance.DispatchEvent(e1);

            //EventDispatcher.Instance.DispatchEvent(new OpenTeamFromOtherEvent(1));
             EventDispatcher.Instance.DispatchEvent(new ActivitySearchTeamClickEvent());
            
        }
    }
}