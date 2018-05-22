#region using
using System.Collections.Generic;
using ClientDataModel;
using DataTable;
using UnityEngine;
using EventSystem;

#endregion

namespace GameUI
{
	public class IconElfFrame : MonoBehaviour
	{
	    public BindDataRoot BindRoot;
        public List<UILabel> Lables;
        private IconElfDataModel itemIdDM = new IconElfDataModel();
        private Color ColorLable { get; set; }

	    public int Count
	    {
	        get { return itemIdDM.Count; }
	        set
	        {
	            itemIdDM.Count = value;
	            UpdateColor();
	        }
	    }

        public IconElfDataModel ItemData
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
	        get { return itemIdDM.ItemId; }
	        set
	        {
	            itemIdDM.ItemId = value;
	            if (BindRoot != null)
	            {
	                BindRoot.SetBindDataSource(itemIdDM);
	            }
	        }
	    }

	    public int TotalCount
	    {
            get { return itemIdDM.TotalCount; }
	        set
	        {
	            itemIdDM.TotalCount = value;
	            if (BindRoot != null)
	            {
	                BindRoot.SetBindDataSource(itemIdDM);
	                UpdateColor();
	            }
	        }
	    }

	    public void UpdateColor()
	    {
            if (itemIdDM.TotalCount < itemIdDM.Count)
            {
                ColorLable = MColor.red;
            }
            else
            {
                ColorLable = MColor.white;
            }

            var __list1 = Lables;
            var __listCount1 = __list1.Count;
            for (var __i1 = 0; __i1 < __listCount1; ++__i1)
            {
                var lable = __list1[__i1];
                {
                    if (lable.color != ColorLable)
                    {
                        lable.color = ColorLable;
                    }
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

                var tbItem = Table.GetItemBase(itemIdDM.ItemId);
                if (tbItem.Type != 23500)
                {
                    GameUtils.ShowItemIdTip(ItemId);
                }
                else
                {
                    var e = new UIEvent_ClickChest(itemIdDM.ItemId);
                    e.From = "Store";
                    EventDispatcher.Instance.DispatchEvent(e);
                }
	        }
	    }
	}
}