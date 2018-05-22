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
    public class FieldMissionFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public void OnClickClose()
        {
            var e = new Close_UI_Event(UIConfig.FieldMissionUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif

            var controllerBase = UIManager.Instance.GetController(UIConfig.FieldMissionUI);
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

        public void OnClickJoin()
        {//ȥ����
            OnClickClose();
            var e = new Show_UI_Event(UIConfig.FieldMineUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickOpenShop()
        {
            OnClickClose();
            var tableSerice = Table.GetService(16);
            if (null == tableSerice)
            {
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.StoreEquip,
                new StoreArguments { Tab = tableSerice.Param[0] }));
        }

        public void OnClickMissionUI()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(1));
        }

        public void OnClickMissionUI_1()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(1,0));
        }
        public void OnClickMissionUI_2()
        {
            if (PlayerDataManager.Instance.GetExData(eExdataDefine.e282) <= 0)
            {
                GameUtils.ShowHintTip(220991);
            }
            else 
                EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(1,1));
        }

        public void OnClickRankUI_1()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(3, 0));
        }
        public void OnClickRankUI_2()
        {
            //if (PlayerDataManager.Instance.GetExData(eExdataDefine.e282) <= 0)
            //{
            //    GameUtils.ShowHintTip(220991);
            //}
            //else 
                EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(3, 1));
        }
        public void OnClickRule()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(2));
        }

        public void OnClickRank()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(3));
        }

        public void OnClickCloseTip()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(0));
        }
    }
}
