using System;
#region using

using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class DepotItemCell : MonoBehaviour
    {
        private ListItemLogic itemList;

        public void ItemClick()
        {
            if (itemList != null)
            {
                var itemData = itemList.Item as BagItemDataModel;

                if (itemData.ItemId != -1)
                {

                    var tbItem = Table.GetItemBase(itemData.ItemId);

                    ///宝箱类型特殊处理
                    if (tbItem.Type != 23500)
                    {

                        var e = new DepotItemClickEvent();
                        e.BagId = itemData.BagId;
                        e.Index = itemData.Index;
                        e.TableId = itemData.ItemId;
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                }
                else
                {
                    if (itemData.Status == (int)eBagItemType.Lock || itemData.Status == (int)eBagItemType.FreeLock)
                    {
                        var e = new PackUnlockEvent(itemData);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                }
            }
        }

        // Use this for initialization
        private void Start()
        {
#if !UNITY_EDITOR
	try
	{
#endif

            itemList = gameObject.GetComponent<ListItemLogic>();


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