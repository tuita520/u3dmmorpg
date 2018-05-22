
using System;
#region using

using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class EquipCell : MonoBehaviour
	{
	    public IconFrame IconData;
	
	    public void OnClickEquip()
	    {
	        if (IconData.BagItemData.ItemId != -1)
	        {
                var tbItemBase = Table.GetItemBase(IconData.BagItemData.ItemId);
                //时装特殊处理
                if (tbItemBase.Type >= 10500 && tbItemBase.Type <= 10502)
	            {
                    GameUtils.ShowItemIdTip(IconData.BagItemData.ItemId);
	                return;
	            }
                if (tbItemBase.Type < 20000)
                {
                    var e = new Show_UI_Event(UIConfig.EquipComPareUI,
                        new EquipCompareArguments
                        {
                            Data = IconData.BagItemData,
                            ShowType = eEquipBtnShow.EquipPack,
                            ResourceType = 1
                        });
                    EventDispatcher.Instance.DispatchEvent(e);
                }
                //EventDispatcher.Instance.DispatchEvent(new EquipCellSelect(IconData.BagItemData, IconData.BagItemData.Index));
	        }
	    }

	    public void OnClickEquipEx()
	    {//增强界面点击选中
            EventDispatcher.Instance.DispatchEvent(new EquipCellSelect(IconData.BagItemData, IconData.BagItemData.Index));
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