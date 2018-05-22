﻿#region using

using ClientDataModel;
using DataTable;
using UnityEngine;
using EventSystem;

#endregion

namespace GameUI
{
	public class IconIdFrame : MonoBehaviour
	{
	    public BindDataRoot BindRoot;
	    private ItemIdDataModel itemIdDM = new ItemIdDataModel();
	    public ListItemLogic item;
	    public int Count
	    {
	        get { return itemIdDM.Count; }
	        set { itemIdDM.Count = value; }
	    }
	
	    public ItemIdDataModel ItemData
	    {
	        get { return itemIdDM; }
	        set
	        {
	            itemIdDM = value;
	            if (BindRoot != null)
	            {
	                BindRoot.SetBindDataSource(itemIdDM);
	            }
	        }
	    }
	
	    [TableBinding("ItemBase")]
	    public int ItemId
	    {
	        get
	        {
	            if (item != null && item.Item != null)
	            {
	                var data = item.Item as ItemIdDataModel;
	                if (data != null)
	                    return data.ItemId;
	            }
	            return itemIdDM.ItemId;
	        }
	        set
	        {
	            itemIdDM.ItemId = value;
	            if (BindRoot != null)
	            {
	                BindRoot.SetBindDataSource(itemIdDM);
	            }
	        }
	    }
	
	    public void OnClick()
	    {
	        OnClickIcon();
	    }
	
	    public void OnClickIcon()
	    {
	        if (ItemId != -1)
	        {

                var tbItem = Table.GetItemBase(ItemId);
                if (tbItem.Type != 23500)
                {
                    //var e = new PackItemClickEvent();
                    //e.BagId = itemData.BagId;
                    //e.Index = itemList.Index;
                    //EventDispatcher.Instance.DispatchEvent(e);
                    GameUtils.ShowItemIdTip(ItemId);
                }
                else
                {
                    var e = new UIEvent_ClickChest(ItemId);
                    //e.Args.Tab = itemData.ItemId;
                    e.From = "Store";
                    EventDispatcher.Instance.DispatchEvent(e);
                }

               // GameUtils.ShowItemIdTip(ItemId);
	        }
	    }
	}
}