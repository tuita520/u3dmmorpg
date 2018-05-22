using ScriptManager;
using System;
#region using

using System.Collections.Generic;
using ClientDataModel;
using EventSystem;
using UnityEngine;
using ClientService;
#endregion

namespace GameUI
{
	public class TeamFrameFrame : MonoBehaviour
	{
        #region 
        bool isInit                                 = false;
        GameObject obj_PlayerInfo                   = null;
        GameObject obj_MatchState                   = null;

        public GameObject obj_MyTeam                       = null;
        public GameObject obj_ChangeGoal                   = null;
        public GameObject obj_SearchTeam                   = null;
        public GameObject obj_InvitePlayer                 = null;
        public GameObject obj_Applylist                    = null;
        public GameObject obj_NearPlayer                   = null;

        public UIScrollView ui_ScrollView                   = null;
        public UIScrollBar ui_ScrollBar                     = null;

        public UIButton btn_AutoMatchBase;
        public UIButton btn_AutoMatch;
        public UIButton btn_StopMatch;

        public UIButton btn_CreateBase;
        public UIButton btn_Create;
        public UIButton btn_Search;

        public UIButton btn_TeamGoal;
        public UIButton btn_TeamGoalClose;

        public UIButton btn_MiniPlus;
        public UIButton btn_MiniSubstract;
        public UIButton btn_MaxPlus;
        public UIButton btn_MaxSubstract;
        public UIButton btn_TargetChangeConfirm;

        public GameObject obj_CD;
        public UILabel lb_CD;
        float CDTime = 5;

        UIButton btn_ApplyListClear;
        UIToggle tog_NearBy;
        #endregion

        private readonly List<ObjFakeCharacter> Characters = new List<ObjFakeCharacter>();
	    private bool mRemoveBind = true;
	    public BindDataRoot TeamFrameBindData;
	    public List<TeamMemberCell> TeamMemberCellLogics;
	    //关闭按钮
	    private void ModelView(IEvent ievent)
	    {
	        var ee = ievent as UIEvent_TeamFrame_RefreshModel;
	        ModelView();
	    }
	
	    private void ModelView()
	    {
	        {
	            var __list3 = Characters;
	            var __listCount3 = __list3.Count;
	            for (var __i3 = 0; __i3 < __listCount3; ++__i3)
	            {
	                var model = __list3[__i3];
	                {
	                    if (null != model)
	                    {
	                        model.Destroy();
	                    }
	                }
	            }
	        }
	        Characters.Clear();
	
	        var controllerBase = UIManager.Instance.GetController(UIConfig.TeamFrame);
	        if (controllerBase == null)
	        {
	            return;
	        }
	        var teamData = controllerBase.GetDataModel("") as TeamDataModel;
	        if (teamData == null)
	        {
	            return;
	        }
	        for (var i = 0; i < 5; i++)
	        {
	            var one = teamData.TeamList[i];
	            if (one.Level == 0)
	            {
	                continue;
	            }
	            var i1 = i;
	            one.Equips[12] = -1;
	            Characters.Add(ObjFakeCharacter.Create(one.TypeId, one.Equips, character =>
	            {
	                var xform = character.gameObject.transform;
	
	                xform.parent = TeamMemberCellLogics[i1].ModelRoot.transform;
	                xform.localPosition = new Vector3(0f, -110f, 0f);
	                xform.localScale = new Vector3(138, 138, 138);
	                xform.forward = Vector3.back;
	                xform.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
	            }, LayerMask.NameToLayer(GAMELAYER.UI)));
	        }
	    }
	
	    //关闭界面
	    public void OnClick_Close()
	    {

	        var e2 = new Close_UI_Event(UIConfig.OperationList);
	        EventDispatcher.Instance.DispatchEvent(e2);
	
	        var e = new Close_UI_Event(UIConfig.TeamFrame);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    //离开队伍
	    public void OnClick_Kick(int index)
	    {
	        var e = new UIEvent_TeamFrame_Kick(index);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    //离开队伍
	    public void OnClick_Leave()
	    {
	        var e = new UIEvent_TeamFrame_Leave();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    //点击模型
	    public void OnClick_Model(int index)
	    {
            var parent = UIManager.GetInstance().GetUIRoot(UIType.TYPE_TIP);
            UIConfig.OperationList.Loction = parent.transform.worldToLocalMatrix *
                                             TeamMemberCellLogics[index].ModelView.worldCenter;
            UIConfig.OperationList.Loction.x += 64;
            UIConfig.OperationList.Loction.y += 100;
            UIConfig.OperationList.Loction.z = 0;
            var e = new TeamMemberShowMenu(index);
            EventDispatcher.Instance.DispatchEvent(e);
	    }

        //点击队伍玩家头像
        public void OnClick_HeadIcon(int index)
        {
            var parent = UIManager.GetInstance().GetUIRoot(UIType.TYPE_TIP);
            UIConfig.OperationList.Loction =
                parent.transform.InverseTransformPoint(TeamMemberCellLogics[index].ModelView.transform.position);
            UIConfig.OperationList.Loction.x += -150;
            UIConfig.OperationList.Loction.y += -20;
            UIConfig.OperationList.Loction.z = 0;
            var e = new TeamMemberShowMenu(index);
            EventDispatcher.Instance.DispatchEvent(e);
        }
	
	    //tab：附近玩家
	    public void OnClick_NearPlayer()
	    {
	        var e2 = new Close_UI_Event(UIConfig.OperationList);
	        EventDispatcher.Instance.DispatchEvent(e2);
	        var e = new UIEvent_TeamFrame_NearPlayer();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    //tab：附近队伍
	    public void OnClick_NearTeam()
	    {
	        var e2 = new Close_UI_Event(UIConfig.OperationList);
	        EventDispatcher.Instance.DispatchEvent(e2);
	        var e = new UIEvent_TeamFrame_NearTeam();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    //tab：自己队伍
	    public void OnClick_SelfTeam()
	    {
	        var e2 = new Close_UI_Event(UIConfig.OperationList);
	        EventDispatcher.Instance.DispatchEvent(e2);
	    }
	
	    //变更：自动加入
	    public void OnClickAutoAccept()
	    {
	        var e = new UIEvent_TeamFrame_AutoAccept();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    //变更：自动申请
	    public void OnClickAutoJion()
	    {
	        var e = new UIEvent_TeamFrame_AutoJion();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    private void OnCloseUiBindRemove(IEvent ievent)
	    {
	        var e = ievent as CloseUiBindRemove;
	        if (e.Config != UIConfig.TeamFrame)
	        {
	            return;
	        }
	        if (e.NeedRemove == 0)
	        {
	            mRemoveBind = false;
	        }
	        else
	        {
	            if (mRemoveBind == false)
	            {
	                RemoveBindEvent();
	            }
	            mRemoveBind = true;
	        }
	    }
	
	    private void OnDestroy()
	    {
#if !UNITY_EDITOR
try
{
#endif

            isInit = false;
#if !UNITY_EDITOR
	try
	{
#endif
	        if (mRemoveBind == false)
	        {
	            RemoveBindEvent();
	        }
	        mRemoveBind = true;
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
	
	    private void OnDisable()
	    {
#if !UNITY_EDITOR
	        try
	        {
#endif
	        if (mRemoveBind)
	        {
	            RemoveBindEvent();
	        }
	
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
            if (mRemoveBind)
	        {
                EventDispatcher.Instance.AddEventListener(NotifyCloseSearchEvent.EVENT_TYPE, NotifyCloseSearch);
                EventDispatcher.Instance.AddEventListener(TeamWorldSpeakNewEvent.EVENT_TYPE, TeamWorldSpeakNew);
                EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_RefreshModel.EVENT_TYPE, ModelView);
	            EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUiBindRemove);
	
	            var controllerBase = UIManager.Instance.GetController(UIConfig.TeamFrame);
	            if (controllerBase == null)
	            {
	                return;
	            }
	            TeamFrameBindData.SetBindDataSource(controllerBase.GetDataModel(""));
                TeamFrameBindData.SetBindDataSource(controllerBase.GetDataModel ("TeamTargetChange"));

                ModelView();
	        }
	        mRemoveBind = true;

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
            InitFrame();
	    }
	
	    private void RemoveBindEvent()
	    {
            EventDispatcher.Instance.RemoveEventListener(NotifyCloseSearchEvent.EVENT_TYPE, NotifyCloseSearch);
            EventDispatcher.Instance.RemoveEventListener(TeamWorldSpeakNewEvent.EVENT_TYPE, TeamWorldSpeakNew);
            EventDispatcher.Instance.RemoveEventListener(UIEvent_TeamFrame_RefreshModel.EVENT_TYPE, ModelView);
	        EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUiBindRemove);
	        TeamFrameBindData.RemoveBinding();
	        {
	            var __list5 = Characters;
	            var __listCount5 = __list5.Count;
	            for (var __i5 = 0; __i5 < __listCount5; ++__i5)
	            {
	                var mychar = __list5[__i5];
	                {
	                    if (mychar)
	                    {
	                        mychar.Destroy();
	                    }
	                }
	            }
	        }
	        Characters.Clear();
	    }
	
	    private void Start()
	    { 
#if !UNITY_EDITOR
	        try
	        {
#endif
	        for (var i = 0; i < 5; i++)
	        {
	            var cell = TeamMemberCellLogics[i];
	            var j = i;
	
	            if (i != 0)
	            {
                    cell.KickButton.onClick.Add(new EventDelegate(()
                        =>
                    {
                        OnClick_Kick(j);
                    }
                    ));
	            }
                if (cell.GetComponent<UIButton>() != null)
                {
                    cell.GetComponent<UIButton>().onClick.Add(new EventDelegate(()
                   =>
                    {
                        OnClick_HeadIcon(j);
                    }
                   ));
                }      
	            var trigger = cell.ModelView.GetComponent<UIEventTrigger>();
	            trigger.onClick.Add(new EventDelegate(() => { OnClick_Model(j); }));
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
	
	
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
	    }

        void InitFrame()
        {
            if (!isInit)
            {
                if (null != obj_MyTeam) obj_MyTeam.SetActive(true);
                if (null != obj_ChangeGoal) obj_ChangeGoal.SetActive(false);
                if (null != obj_SearchTeam) obj_SearchTeam.SetActive(false);
                if (null != obj_InvitePlayer) obj_InvitePlayer.SetActive(false);
                if (null != obj_Applylist) obj_Applylist.SetActive(false);
                if (null != obj_NearPlayer) obj_NearPlayer.SetActive(false);

                if (null != obj_MyTeam)
                {
                    GameObject obj_BtnList = obj_MyTeam.transform.FindChildRecursive("Btnlist").gameObject;
                    if (null != obj_BtnList) obj_BtnList.SetActive(false);
                }
                if (null != btn_TeamGoal) EventDelegate.Add(btn_TeamGoal.onClick,
                  new EventDelegate(() => { onclickTeamGoal(btn_TeamGoal.gameObject); }));
                if (null != btn_TeamGoalClose) EventDelegate.Add(btn_TeamGoalClose.onClick,
                  new EventDelegate(() => { onclickTeamGoalClose(btn_TeamGoalClose.gameObject); }));

                if (null != btn_AutoMatchBase) EventDelegate.Add(btn_AutoMatchBase.onClick,
                new EventDelegate(() => { onclickAutoMatchBase(btn_AutoMatchBase.gameObject); }));
                if (null != btn_StopMatch) EventDelegate.Add(btn_StopMatch.onClick,
                    new EventDelegate(() => { onclickStopMatch(btn_StopMatch.gameObject); }));
                if (null != btn_CreateBase) EventDelegate.Add(btn_CreateBase.onClick,
                    new EventDelegate(() => { onclickCreateBase(btn_CreateBase.gameObject); }));

                if (null != btn_MiniPlus) EventDelegate.Add(btn_MiniPlus.onClick,
                    new EventDelegate(() => { onclickLevelMiniPlus(btn_MiniPlus.gameObject); }));
                if (null != btn_MiniSubstract) EventDelegate.Add(btn_MiniSubstract.onClick,
                    new EventDelegate(() => { onclickLevelMiniSubstract(btn_MiniSubstract.gameObject); }));
                if (null != btn_MaxPlus) EventDelegate.Add(btn_MaxPlus.onClick,
                    new EventDelegate(() => { onclickLevelMaxPlus(btn_MaxPlus.gameObject); }));
                if (null != btn_MaxSubstract) EventDelegate.Add(btn_MaxSubstract.onClick,
                    new EventDelegate(() => { onclickLevelMaxSubstract(btn_MaxSubstract.gameObject); }));
                if (null != btn_TargetChangeConfirm) EventDelegate.Add(btn_TargetChangeConfirm.onClick,
                    new EventDelegate(() => { onclickbtn_TargetChangeConfirm(btn_TargetChangeConfirm.gameObject); }));
                if (null != btn_Search) EventDelegate.Add(btn_Search.onClick,
                    new EventDelegate(() => { onclickbtn_Search(btn_Search.gameObject); }));
                if (null != obj_SearchTeam)
                {
                    UIButton btnSearchClose = obj_SearchTeam.transform.FindChildRecursive("close").GetComponent<UIButton>();
                    if (null != btnSearchClose) EventDelegate.Add(btnSearchClose.onClick,
                          new EventDelegate(() => { onclickBtnSearchClose(btnSearchClose.gameObject); }));
                }
                btn_ApplyListClear = obj_Applylist.transform.FindChildRecursive("clear").GetComponent<UIButton> ();
                if (null != btn_ApplyListClear) EventDelegate.Add(btn_ApplyListClear.onClick,
                    new EventDelegate(() => { onclickBtn_ApplyListClear(btn_ApplyListClear.gameObject); }));
                
            }
            if(null != obj_ChangeGoal) obj_ChangeGoal.gameObject.SetActive(false);
            if (null != obj_CD) obj_CD.SetActive(false);
            lb_CD.text = "";
            isInit = true;
        }

        void onclickAutoMatchBase(GameObject obj)
        {
            onclickSearchAutoMatch();
        }

        void onclickStopMatch(GameObject obj)
        {
            var msg = NetManager.Instance.AutoMatchCancel(1);
            msg.SendAndWaitUntilDone();
        }

        void onclickCreateBase(GameObject obj)
        {
            var e = new DungeonBtnClick(7, eDungeonType.Team);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickTeamGoal(GameObject obj)
        {
            var controllerBase = UIManager.Instance.GetController(UIConfig.TeamFrame);
            if (controllerBase == null)
            {
                return;
            }
            var myModel = controllerBase.GetDataModel("") as TeamDataModel;
            if (myModel.AutoMatch == 1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220131)));
                return;
            }
            PlayerDataManager.Instance.NoticeData.DungeonType = 1;
            if (null != obj_ChangeGoal) obj_ChangeGoal.SetActive(true);
            var e = new TeamTargetChange_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickTeamGoalClose(GameObject obj)
        {
            if (null != obj_ChangeGoal) obj_ChangeGoal.SetActive(false);
        }

        void onclickLevelMiniPlus(GameObject obj)
        {
            var e = new TeamTargetChangeLevelPlus_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickLevelMiniSubstract(GameObject obj)
        {
            var e = new TeamTargetChangeLevelSubStract_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickLevelMaxPlus(GameObject obj)
        {
            var e = new TeamTargetChangeLevelMaxPlus_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickLevelMaxSubstract(GameObject obj)
        {
            var e = new TeamTargetChangeLevelMaxSubStract_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickbtn_TargetChangeConfirm(GameObject obj)
        {
            var e = new TeamTargetChangeConfirm_Event();
            EventDispatcher.Instance.DispatchEvent(e);
            onclickTeamGoalClose(null);
        }

        void onclickbtn_Search(GameObject obj)
        {
            //var aa = new TeamTargetChange_Event();
            //EventDispatcher.Instance.DispatchEvent(aa);

            if (null != obj_SearchTeam)
                obj_SearchTeam.SetActive(true);
            var e = new TeamSearchList_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void onclickBtnSearchClose(GameObject obj)
        {
            if (null != obj_SearchTeam)
                obj_SearchTeam.SetActive(false);
            var controllerBase = UIManager.Instance.GetController(UIConfig.TeamFrame);
            if (controllerBase != null)
            {
                var myModel = controllerBase.GetDataModel("") as TeamDataModel;
                if (null != myModel)
                    if (myModel.OpenFromOther != 0)
                        OnClick_Close();
            }
        }

		public void onclickApplyBtn (GameObject obj)
		{
            if (obj != null) obj.SetActive(true);
            var e = new TeamApplyListClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void onclickCloseApplyBtn(GameObject obj)
        {
            if (obj != null) obj.SetActive(false);
        }

        public void onclickSearchBtn(GameObject obj)
        {
			var e = new TeamSearchListClick_Event();

			string objName = obj.name;
			switch (objName) 
			{
			case "NearTeamCell1":
				e.index = 0;
				break;
			case "NearTeamCell2":
                e.index = 1;
				break;
			case "NearTeamCell3":
                e.index = 2;
				break;
			case "NearTeamCell4":
                e.index = 3;
				break;
			}

			EventDispatcher.Instance.DispatchEvent(e);
        }

        public void onclickSearchRefresh()
        {
            var e = new TeamSearchRefreshClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void onclickSearchCreate()
        {
            var e = new DungeonBtnClick(7, eDungeonType.Team);
            EventDispatcher.Instance.DispatchEvent(e);
            onclickBtnSearchClose(null);
        }

        public void onclickSearchAutoMatch()
        {
            var e = new TeamAutoMatchClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
            if (null != obj_SearchTeam) obj_SearchTeam.SetActive(false);
        }

        public void onclickInviteNearby()
        {
            if (null != obj_InvitePlayer) obj_InvitePlayer.transform.FindChildRecursive ("nearby").GetComponent<UIToggle>().value = true;
            
           
            var e = new TeamInviteNearbyClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
            ui_ScrollView.ResetPosition();
            ui_ScrollBar.value = 0;
            ui_ScrollBar.alpha = 0;
        }

        public void onclickInviteFriends()
        {
            
           
            var e = new TeamInviteFriendsClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
            ui_ScrollView.ResetPosition();
            ui_ScrollBar.value = 0;
            ui_ScrollBar.alpha = 0;
        }

        public void onclickInviteBattleUnion()
        {
           
           
            var e = new TeamInviteBattleUnionClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
            ui_ScrollView.ResetPosition();
            ui_ScrollBar.value = 0;
            ui_ScrollBar.alpha = 0;
        }

        public void onclickInviteBtn()
        {
            var e = new TeamInviteClick_Event();
            EventDispatcher.Instance.DispatchEvent(e);
            if (null != obj_InvitePlayer) obj_InvitePlayer.SetActive(true);
            onclickInviteNearby();
        }

        public void onclickInviteClose()
        {
            if (null != obj_InvitePlayer) obj_InvitePlayer.SetActive(false);
        }

        public void ChatTeamClick()
        {
           // PlayerDataManager.Instance.NoticeData.DungeonType = 1;
            EventDispatcher.Instance.DispatchEvent(new ChatTeamClickEvent ());
        }

        void UpdateCD()
        {
            CDTime = CDTime - 1;
            int tim = (int)CDTime;
            if (CDTime < 0)
            {
                this.CancelInvoke("UpdateCD");
                if (null != obj_CD) obj_CD.SetActive(false);
                return;
                
            }
            lb_CD.text = tim.ToString () + "秒";
        }

        void onclickBtn_ApplyListClear(GameObject obj)
        {
            var e = new TeamClearApplyList_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        void TeamWorldSpeakNew(IEvent ievent)
        {
            if (null != obj_CD) obj_CD.SetActive(true);
            this.InvokeRepeating("UpdateCD", 1f, 1f);
            CDTime = 5;
            lb_CD.text = CDTime.ToString() + "秒";
        }

        public void OnClickMiliBuy()
        {
            NumPadLogic.ShowNumberPad(1, 399, (result) =>
            {

            }, 3);
        }
        public void OnClickMaxBuy()
        {
            NumPadLogic.ShowNumberPad(1, 400, (result) =>
            {

            }, 4);
        }

        void NotifyCloseSearch(IEvent ievent)
        {
            if (null != obj_SearchTeam)
                obj_SearchTeam.SetActive(false);
        }
    }
}