using ScriptManager;
using System;
#region using

using ClientDataModel;
using UnityEngine;

#endregion

namespace GameUI
{
	public class IconFrame : MonoBehaviour
	{
	    public BindDataRoot BindRoot;
	    private BagItemDataModel bagItemData;
        public int index = -1;
	
	    public BagItemDataModel BagItemData
	    {
	        get { return bagItemData; }
	        set
	        {
	            bagItemData = value;
	            BindRoot.SetBindDataSource(value);
	        }
	    }
	
	    public void OnClickIcon()
	    {
	        if (GuideManager.Instance.IsGuiding())
	        {
	            return;
	        }
	        if (BagItemData.ItemId != -1)
	        {
	            GameUtils.ShowItemDataTip(BagItemData, eEquipBtnShow.OperateBag);
	        }
	    }
	
	    public void OnClickIconNone()
	    {
	        if (BagItemData.ItemId != -1)
	        {
                var controllerBase = UIManager.Instance.GetController(UIConfig.MyArtifactUI);
                if (controllerBase == null) return;
                if (FrameState.Open == controllerBase.State)
                {
                    var bagItem = controllerBase.GetDataModel("BagItem") as BagItemDataModel;
                    if (bagItem == null) return;
                    if (index != -1)
                    {
                        GameUtils.ShowItemDataTip(bagItem);
                        return;
                    }
                }
                GameUtils.ShowItemDataTip(BagItemData);
	        }
	    }
	
	    public void OnClickIconShare()
	    {
	        if (BagItemData.ItemId != -1)
	        {
	            GameUtils.ShowItemDataTip(BagItemData, eEquipBtnShow.Share);
	        }
	    }
        public void OnClickIconExchange2()
        {
            if (BagItemData.ItemId != -1)
            {
                GameUtils.ShowEquuipExchangeTip(BagItemData, eEquipBtnShow.Share);
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