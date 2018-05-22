using BehaviourMachine;
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
    public class SNSFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public UIEventTrigger[] mTabs;
        public void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif


            for (int i = 0; i < mTabs.Length; i++)
            {
                UIEventTrigger e = mTabs[i];
                int j = i;
                e.onClick.Add(new EventDelegate(()=>{OnClickTab(j);}));
            }
            
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
        public void OClickClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SNSFrameUI));
        }

        private void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif

            var controllerBase = UIManager.Instance.GetController(UIConfig.SNSFrameUI);
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

        public void OnClickTab(int idx)
        {
            EventDispatcher.Instance.DispatchEvent(new SNSTabEvent(idx));
        }

        public void OnClickSendQuestion()
        {

            EventDispatcher.Instance.DispatchEvent(new MailOperactionEvent(7));
        }
        public void OnSendQuestion()
        {
            EventDispatcher.Instance.DispatchEvent(new MailOperactionEvent(8));
        }

        public void OnDelQAMail()
        {
            var e = new MailOperactionEvent(6);
            EventDispatcher.Instance.DispatchEvent(e);
        }
    }
    
}