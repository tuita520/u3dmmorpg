using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EventSystem;
using ClientDataModel;
using System;
using GameUI;
using DataTable;
using System.ComponentModel;
namespace GameUI
{
    public class FieldFinalFrame : MonoBehaviour
    {
        public FieldFinalDataModel DataModel;
        public BindDataRoot Binding;
        private void OnEnable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
                var controller = UIManager.Instance.GetController(UIConfig.FieldFinalUI);
                DataModel = controller.GetDataModel("") as FieldFinalDataModel;
                Binding.SetBindDataSource(DataModel);
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
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.FieldFinalUI));
        }
    }
}
