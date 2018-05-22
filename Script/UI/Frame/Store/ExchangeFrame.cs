using ScriptManager;
using System;
#region using

using System.ComponentModel;
using EventSystem;
using UnityEngine;
using ClientDataModel;
#endregion

namespace GameUI
{
    public class ExchangeFrame : MonoBehaviour
    {
        
        public BindDataRoot Binding;
        private IControllerBase controller;
        private ExchangeDataModel dataModel;
        // Use this for initialization
        void Start()
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

        // Update is called once per frame
        void Update()
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
        public void OnClickClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ExchangeUI));
        }

        private void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif

            Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);
            controller = UIManager.Instance.GetController(UIConfig.ExchangeUI);
            if (controller == null)
            {
                return;
            }
            Binding.SetBindDataSource(controller.GetDataModel(""));

            EventDispatcher.Instance.DispatchEvent(new ExChangeInit_Event());
            
        
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
         
        }
        public void OnClickExchangeGold()
        {
            EventDispatcher.Instance.DispatchEvent(new ExChange_Event(0));
        }
        public void OnClickExchangeExp()
        {
            EventDispatcher.Instance.DispatchEvent(new ExChange_Event(1));
        }
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
    }
}