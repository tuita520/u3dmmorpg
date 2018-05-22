using System;
#region using
using SignalChain;
using EventSystem;
using UnityEngine;
using DataTable;
using DataContract;
using System.Collections.Generic;
using System.Collections;
using ScorpionNetLib;
using ClientService;
#endregion

namespace GameUI
{
    public class SurveyFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        private IControllerBase controller;

        private void Awake()
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

            controller = UIManager.Instance.GetController(UIConfig.SurveyUI);
            if (controller == null)
            {
                return;
            }
            Binding.SetBindDataSource(controller.GetDataModel(""));
        
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

        #region ����¼�

        public void OnClickSend()
        {
            EventDispatcher.Instance.DispatchEvent(new SurveySendResultEvent());

        }

        public void OnClickClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SurveyUI));
        }
        #endregion ����¼�
    }
}