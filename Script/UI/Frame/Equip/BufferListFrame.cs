using ClientDataModel;
using System;
#region using

using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class BufferListFrame : MonoBehaviour
    {
        public BindDataRoot Binding;

        private void OnDisable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            Binding.RemoveBinding();

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
            var controllerBase = UIManager.Instance.GetController(UIConfig.BufferListUI);
            if (controllerBase == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase.GetDataModel(""));

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


#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        public void OnClick_Close()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.BufferListUI));
        }
    }
}