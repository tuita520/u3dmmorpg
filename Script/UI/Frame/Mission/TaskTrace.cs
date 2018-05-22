using ScriptManager;
using System.Collections;
using SignalChain;
using System;
#region using

using System.Collections.Generic;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class TaskTrace : MonoBehaviour, IChainRoot, IChainListener
	{
	    public UIButton HideBtn;
	    public UIWidget Hit;
	    public UIToggle InfotToggle;
	    private MissionTrackListDataModel taskListDM;
	    public UIButton ShowBtn;
	    public List<UIEventTrigger> TeamMembers;
	    public UIToggle TeamToggle;
        public List<StackLayout> LayoutList;
        private bool flag;
        public GameObject StackGame;

        public void Listen<T>(T message)
        {
            if (message is string && (message as string) == "ActiveChanged")
            {
                flag = true;
            }
        }

        private void LateUpdate()
        {
#if !UNITY_EDITOR
	try
	{
#endif

            if (!flag)
            {
                return;
            }
            flag = false;
            var listCount3 = LayoutList.Count;
            for (var i = 0; i < listCount3; ++i)
            {
                var layout = LayoutList[i];
                if (layout != null)
                {
                    layout.ResetLayout();
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

	    public void OnClick_Era()
	    {
	        if (EraManager.Instance.CurrentEraId == 0)
	        {
	            OnClick_1();
	        }
	        else
	        {
                EraManager.Instance.GotoCurrentPage();
	        }
	    }

	    public void OnClick_1()
	    {
	        if (-1 != taskListDM.List[0].MissionId)
	        {
	            MissionManager.Instance.GoToMissionPlace(taskListDM.List[0].MissionId);
	        }
	    }
	
	    public void OnClick_2()
	    {
	        if (-1 != taskListDM.List[1].MissionId)
	        {
	            MissionManager.Instance.GoToMissionPlace(taskListDM.List[1].MissionId);
	        }
	    }
	
	    public void OnClick_3()
	    {
	        if (taskListDM.List.Count < 3)
	        {
	            return;
	        }
	
	        /*
			if (-1 != mDataModel.List[2].MissionId)
			{
				MissionManager.Instance.GoToMissionPlace(mDataModel.List[2].MissionId);
			}
			*/
	
	        var missionId = taskListDM.List[2].MissionId;
	        if (missionId == MissionManager.Instance.CurrentDoingCircleMission)
	        {
	//如果当前任务是正在追踪的环任务那就GoTo
	            MissionManager.Instance.GoToMissionPlace(missionId);
	            return;
	        }
	
	        var arg = new MissionListArguments();
	        arg.Tab = 3;
	        arg.MissionId = missionId;
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
	    }
	
	    public void OnClick_4()
	    {
	        if (taskListDM.List[3].MissionId > 5000 && PlayerDataManager.Instance.GetExData(eExdataDefine.e282) <= 0)
	        {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220989));
	            return;
	        }
            var missionId = taskListDM.List[3].MissionId;
	        if (missionId == MissionManager.Instance.CurrentDoingCircleMission)
	        {
	//如果当前任务是正在追踪的环任务那就GoTo
	            MissionManager.Instance.GoToMissionPlace(missionId);
	            return;
	        }
	
	        var arg = new MissionListArguments();
	        arg.Tab = 3;
	        arg.MissionId = missionId;
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
	    }

	    public void OnClick_5()
	    {
            var missionId = taskListDM.List2[0].MissionId;
	        if (missionId == MissionManager.Instance.CurrentDoingCircleMission)
	        {
	            //如果当前任务是正在追踪的环任务那就GoTo
	            MissionManager.Instance.GoToMissionPlace(missionId);
	            return;
	        }

	        var arg = new MissionListArguments();
	        arg.Tab = 4;
	        arg.MissionId = missionId;
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
	    }
	    public void OnClickMainTeamBtn()
	    {
            EventDispatcher.Instance.DispatchEvent(new MissionOrTeamEvent(1));
            var e = new TeamOperateEvent(0, TeamToggle.value);
            EventDispatcher.Instance.DispatchEvent(e);

            EventDispatcher.Instance.DispatchEvent(new Event_MissionTabClick(2));
	    }

        public void OnClickMainNoTeamBtn()
        {
            var e = new TeamOperateEvent(1, TeamToggle.value);
            EventDispatcher.Instance.DispatchEvent(e);
        }
	
	    private void OnEvent_ClickTeamMember(int index)
	    {
            //var worldPos = UICamera.currentCamera.ScreenToWorldPoint(UICamera.lastTouchPosition);
            //var localPos = TeamMembers[index].transform.root.InverseTransformPoint(worldPos);
            //UIConfig.OperationList.Loction = localPos;

            //var e = new TeamMemberShowMenu(index);
            //EventDispatcher.Instance.DispatchEvent(e);
            OnClickMainTeamBtn();
        }
	
	    private void OnDestroy()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        EventDispatcher.Instance.RemoveEventListener(TeamChangeEvent.EVENT_TYPE, OnEvent_TeamChange);
            EventDispatcher.Instance.RemoveEventListener(UIEvent_MainUITeamFrame_Show.EVENT_TYPE, OnEvent_MainUITeam);
	
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

            var controllerBase = UIManager.Instance.GetController(UIConfig.MissionTrackList);
            taskListDM = controllerBase.GetDataModel("") as MissionTrackListDataModel;

		    if (!EventDispatcher.Instance.HasEventListener(TeamChangeEvent.EVENT_TYPE, OnEvent_TeamChange))
		    {
				EventDispatcher.Instance.AddEventListener(TeamChangeEvent.EVENT_TYPE, OnEvent_TeamChange);    
		    }
			if (!EventDispatcher.Instance.HasEventListener(UIEvent_MainUITeamFrame_Show.EVENT_TYPE, OnEvent_MainUITeam))
		    {
				EventDispatcher.Instance.AddEventListener(UIEvent_MainUITeamFrame_Show.EVENT_TYPE, OnEvent_MainUITeam);
		    }

	        StartCoroutine(DelayShowStack());

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }

        private IEnumerator DelayShowStack()
        {
            StackGame.SetActive(false);
            yield return new WaitForEndOfFrame();            
            StackGame.SetActive(true);
        }
	
	    private void OnEvent_TeamChange(IEvent ievent)
	    {
	        var e = ievent as TeamChangeEvent;
	        if (e.Type == 10)
	        {
	            //InfotToggle.value = true;
	            //TeamToggle.value = false;
	
	            InfotToggle.Set(true);
	            TeamToggle.Set(false);
	            EventDispatcher.Instance.DispatchEvent(new Event_MissionTabClick(0));
	        }
	    }

        public void OnEvent_MainUITeam(IEvent eve)
        {
            InfotToggle.Set(false);
            TeamToggle.Set(true);
        }
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        var controllerBase = UIManager.Instance.GetController(UIConfig.MissionTrackList);
	        taskListDM = controllerBase.GetDataModel("") as MissionTrackListDataModel;
	
	        for (var i = 0; i < 5; i++)
	        {
	            var trigger = TeamMembers[i];
	            var j = i;
	            trigger.onClick.Add(new EventDelegate(() => { OnEvent_ClickTeamMember(j); }));
	        }
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void ToggleBtn()
	    {
	        if (ShowBtn.active)
	        {
	            ShowBtn.active = false;
	            HideBtn.active = true;
	            Hit.alpha = 1;
	        }
	        else
	        {
	            ShowBtn.active = true;
	            HideBtn.active = false;
	            Hit.alpha = 0;
	        }
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
	
	    public void UpdateInfoTimer()
	    {
            EventDispatcher.Instance.DispatchEvent(new MissionTrackUpdateTimerEvent());
	    }

        public void OnPressSwitch()
        {
            EventDispatcher.Instance.DispatchEvent(new MissionTrackOpenSwitch(-1));
        }

        public void OnClickSwitchDaily()
        {
            EventDispatcher.Instance.DispatchEvent(new MissionTrackOpenSwitch((int)eMissionMainType.Daily));
        }
        public void OnClickSwitchGuild()
        {
            EventDispatcher.Instance.DispatchEvent(new MissionTrackOpenSwitch((int)eMissionMainType.Gang));
        }

	    public void OnClickSwitchHunt()
	    {
	        EventDispatcher.Instance.DispatchEvent(new MissionTrackOpenSwitch((int)eMissionMainType.Farm));
	    }
        private void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif

            //if (null != InfotToggle) EventDelegate.Add(InfotToggle.onChange,
            //     new EventDelegate(() => { InfotToggleChange(InfotToggle.gameObject); }));
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

        //void InfotToggleChange(GameObject obj)
        //{
        //    if(InfotToggle.value)EventSystem.EventDispatcher.Instance.DispatchEvent(new EventSystem.OnClickToggleTaskEvent(obj.name));
        //}
    }
}