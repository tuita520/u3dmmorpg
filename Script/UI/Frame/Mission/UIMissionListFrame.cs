using System;
using UnityEngine;
using System.Collections;
using ScriptManager;
using EventSystem;

namespace GameUI
{
	public class UIMissionListFrame : MonoBehaviour
	{

		public BindDataRoot Binding;
		private bool removeBind = true;

		public void Start()
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

		public void OnEnable()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (true == removeBind)
			{

				var controller = UIManager.Instance.GetController(UIConfig.MissionList);
				Binding.SetBindDataSource(controller.GetDataModel(""));
				Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);

				EventDispatcher.Instance.AddEventListener(EventSystem.CloseUiBindRemove.EVENT_TYPE, OnCloseUiBindRemove);
			}
			removeBind = true;

		
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}



		public void OnDisable()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (true == removeBind)
			{
				RemoveBindingEvent();
			}
		
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}


		public void OnDestroy()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (false == removeBind)
			{
				RemoveBindingEvent();
			}
			removeBind = true;
		
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}


		public void OnCloseUiBindRemove(IEvent ievent)
		{

			var e = ievent as CloseUiBindRemove;

			if (e.Config != UIConfig.MissionList)
			{
				return;
			}

			if (e.NeedRemove == 0)
			{
				removeBind = false;
			}
			else
			{
				if (removeBind == false)
				{
					RemoveBindingEvent();
				}
				removeBind = true;

			}

		}

		public void RemoveBindingEvent()
		{
			Binding.RemoveBinding();
			EventDispatcher.Instance.RemoveEventListener(EventSystem.CloseUiBindRemove.EVENT_TYPE, OnCloseUiBindRemove);

		}

		public void OnCloseClick()
		{
			EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionList, true));
		}

		public void OnClickMissionAuto()
		{
			EventDispatcher.Instance.DispatchEvent(new Event_MissionList_AutoNext());
		}

	    public void OnClickReSetMission()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Event_ReSetMission());
	    }
        public void OnClickMissionTrace()
        {
            EventDispatcher.Instance.DispatchEvent(new MissionTraceEvent());
        }

		public void OnLook1Click()
		{
			EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(1));
		}

		public void OnLook2Click()
		{
			EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(2));
		}

		public void OnLook3Click()
		{
			EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(3));
		}

	    public void OnLook4Click()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(4));
	    }
	}
}
