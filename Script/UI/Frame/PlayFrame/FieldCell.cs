#region using

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;
using ClientService;
using ScorpionNetLib;
#endregion

namespace GameUI
{
	public class FieldCell : MonoBehaviour
	{
	    public ListItemLogic ItemLogic;
	
	    public void OnClickBtn()
	    {//点击领取
	        FieldMissionBaseDataModel data = ItemLogic.Item as FieldMissionBaseDataModel;
            if(data != null)
    	        EventDispatcher.Instance.DispatchEvent(new FieldActivityEvent(10,data.Id));
	    }
	}
}