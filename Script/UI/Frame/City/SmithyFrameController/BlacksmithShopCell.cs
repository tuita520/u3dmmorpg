using System;
#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;


#endregion

namespace GameUI
{
	public class BlacksmithShopCell : MonoBehaviour
	{
	    private ListItemLogic listCellLogic;
	
	    public void OnClickMenuCell()
	    {
	        if (listCellLogic != null)
	        {
	            EventDispatcher.Instance.DispatchEvent(new SmithyCellClickedEvent(listCellLogic.Item as CastMenuDataModel));
	        }
	    }
	
	    // Use this for initialization
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        listCellLogic = gameObject.GetComponent<ListItemLogic>();
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