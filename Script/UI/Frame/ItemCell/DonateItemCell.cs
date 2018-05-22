using System;
#region using

using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class DonateItemCell : MonoBehaviour
    {
        private ListItemLogic itemList;

        public void ItemClick()
        {
            var conler = UIManager.Instance.GetController(UIConfig.BackPackUI);
            var packType = "";
            if (conler != null)
            {
                packType = conler.CallFromOtherClass("GetPackType", null).ToString();

            }

            if (itemList != null)
            {
                var itemData = itemList.Item as BagItemDataModel;
                var index = itemList.Index;

                if (itemData.ItemId != -1)
                {

                    var tbItem = Table.GetItemBase(itemData.ItemId);

                    ///宝箱类型特殊处理
                    if (tbItem.Type != 23500)
                    {

                        var e = new DonateItemClickEvent();
                        e.BagId = itemData.BagId;
                        e.Index = itemData.Index;
                        e.TableId = itemData.ItemId;
                        e.ItemIndex = index;
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