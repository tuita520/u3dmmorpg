using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EventSystem;
using ClientDataModel;
using System;
using GameUI;
using DataTable;
using System.ComponentModel;
using ScriptManager;

namespace GameUI
{
    public class ChickenFightFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
    
        public void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif
            var controllerBase = UIManager.Instance.GetController(UIConfig.ChickenFightUI);
            if (controllerBase == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase.GetDataModel("Chicken"));
            Binding.SetBindDataSource(controllerBase.GetDataModel("Mission"));
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

        public void OnClickClose()
        {
            var e = new Close_UI_Event(UIConfig.ChickenFightUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClickStoreBtn()
        {
            var tableSerice = Table.GetService(16);
            if (null == tableSerice)
            {
                return;
            }
            var e = new Show_UI_Event(UIConfig.StoreEquip, new StoreArguments { Tab = tableSerice.Param[0] });
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnLineUpStart()
        {
            var e = new ChickenFightChoosePageEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnLineUpStop()
        {
            var e = new ChickenFightChoosePageEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClickActivityRule()
        {
            var e = new ChickenFightChoosePageEvent(2);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnActivityRuleClose()
        {
            var e = new ChickenFightChoosePageEvent(3);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClickRankReward()
        {
            var e = new ChickenFightChoosePageEvent(4);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClickOpenBtn()
        {
            var e = new ChickenFightChoosePageEvent(6);
            EventDispatcher.Instance.DispatchEvent(e);
        }
    }
}
