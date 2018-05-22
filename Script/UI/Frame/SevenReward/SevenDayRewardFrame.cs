using System;
using UnityEngine;
using System.Collections;
using EventSystem;

namespace GameUI
{
	public class SevenDayRewardFrame : MonoBehaviour
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

				var controller = UIManager.Instance.GetController(UIConfig.SevenDayReward);
				Binding.SetBindDataSource(controller.GetDataModel(""));

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
			EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SevenDayReward, true));
		}

	}
}
