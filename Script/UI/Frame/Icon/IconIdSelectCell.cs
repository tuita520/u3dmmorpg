using System;
#region using

using ClientDataModel;
using UnityEngine;
using EventSystem;
#endregion

namespace GameUI
{
	public class IconIdSelectCell : MonoBehaviour
	{
	    public ListItemLogic ItemLogic;
	
	    public void OnClickIcon()
	    {
	        var data = ItemLogic.Item as ItemIdSelectDataModel;
	        if (data != null)
	        {
	            if (data.ItemId != -1)
	            {
	                GameUtils.ShowItemIdTip(data.ItemId);
	            }
	        }
	    }

	    public void OnClickIconSelect()
	    {
            var data = ItemLogic.Item as ItemIdSelectDataModel;
	        if (data != null)
	        {
	            if (data.Select == false)
	            {
	                data.Select = true;
                    EventDispatcher.Instance.DispatchEvent(new IconIdSelectEvent(ItemLogic.Index, true));
	            }
	            else
	            {
                    data.Select = false;
                    EventDispatcher.Instance.DispatchEvent(new IconIdSelectEvent(ItemLogic.Index, false));

                    //if (data.ItemId != -1)
                    //{
                    //    GameUtils.ShowItemIdTip(data.ItemId);
                    //}
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