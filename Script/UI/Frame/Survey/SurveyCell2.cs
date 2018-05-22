
#region using

using ClientDataModel;
using System;
using System.Collections.Generic;
using EventSystem;
using UnityEngine;
#endregion

namespace GameUI
{
    public class SurveyCell2 : MonoBehaviour
    {
        public ListItemLogic ItemLogic;
        public void OnClickIcon()
        {
            var data = ItemLogic.Item as SurveyCell2DataModel;
            if (data != null)
            {
                data.bSelect = !data.bSelect;
                if (data.bSelect == true && data.bMul == false)
                {
                    EventDispatcher.Instance.DispatchEvent(new SurveyCheckOptEvent(data.key, data.value));
                }
            }
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
    }
}