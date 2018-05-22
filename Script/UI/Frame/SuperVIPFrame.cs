using System;
using UnityEngine;
using System.Collections;
using DataContract;
using EventSystem;

namespace GameUI
{
    public class SuperVIPFrame : MonoBehaviour
    {
        public BindDataRoot Binding;

        void Start()
        {
#if !UNITY_EDITOR
try
{
#endif

            var controllerBase = UIManager.Instance.GetController(UIConfig.SuperVipUI);
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

        public void BtnClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SuperVipUI));
        }
        public void BtnRecharge()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 }));
        }
    }

}

