using System;
using UnityEngine;
using System.Collections;
using EventSystem;
using ClientDataModel;

namespace GameUI
{

	public class UIAchievementFrame : MonoBehaviour
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
				var controller = UIManager.Instance.GetController(UIConfig.AchievementFrame);
				Binding.SetBindDataSource(controller.GetDataModel(""));

			    EventDispatcher.Instance.AddEventListener(EventSystem.CloseUiBindRemove.EVENT_TYPE, OnCloseUIBindingRemove);
			
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


		public void OnDestroy()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (false == removeBind)
			{
				RemoveBindEvent();
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

		public void OnCloseUIBindingRemove(IEvent ievent)
		{
			var e = ievent as CloseUiBindRemove;
			if (e.Config != UIConfig.AchievementFrame)
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
					RemoveBindEvent();
				}
				removeBind = true;
			}
		}

		public void RemoveBindEvent()
		{
			Binding.RemoveBinding();
			EventDispatcher.Instance.RemoveEventListener(EventSystem.CloseUiBindRemove.EVENT_TYPE, OnCloseUIBindingRemove);
		}

		public void OnCloseClick()
		{
			EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.AchievementFrame));
		}

	}
}