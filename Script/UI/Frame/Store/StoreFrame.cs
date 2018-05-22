using ScriptManager;
using GameUI;
using System;
#region using

using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class StoreFrame : MonoBehaviour
	{
	    public Transform BackPackRoot;
	    public BindDataRoot BindData;
	    private BagFrame theBag;
	    private bool isDeleteBind = true;
	
	    private void CreateBag()
	    {
	        var objres = ResourceManager.PrepareResourceSync<GameObject>("UI/BackPack.prefab");
	        var obj = Instantiate(objres) as GameObject;
	        if (null != BackPackRoot && obj != null)
	        {
	            var objTransform = obj.transform;
	            //objTransform.parent = BackPackRoot;
	            objTransform.SetParentEX(BackPackRoot);
	            objTransform.localScale = Vector3.one;
	            objTransform.localPosition = Vector3.zero;
	            obj.SetActive(true);
	
	            theBag = obj.GetComponent<BagFrame>();
	            if (theBag)
	            {
	                theBag.AddBindEvent();
	            }
	        }
	    }
	
	    public void OnClickBuyInfoAdd()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(14));
	    }
	
	    public void OnClickBuyInfoAddPress()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(16));
	    }
	
	    public void OnClickBuyInfoAddUnPress()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(18));
	    }
	
	    public void OnClickBuyInfoBuy()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(12));
	    }
	
	    public void OnClickBuyInfoClose()
	    {
	        var e = new StoreOperaEvent(11);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickBuyInfoDel()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(15));
	    }
	
	    public void OnClickBuyInfoDelPress()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(17));
	    }
	
	    public void OnClickBuyInfoDelUnPress()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(19));
	    }
	
	    public void OnClickBuyInfoMax()
	    {
	        EventDispatcher.Instance.DispatchEvent(new StoreOperaEvent(13));
	    }

        public void OnClickCalculatorBuy()
        {
            NumPadLogic.ShowNumberPad(1, 99, (result) =>
            {

            }, 2);
        }
	    public void OnClickClose()
	    {
	        var e = new Close_UI_Event(UIConfig.StoreUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
        public void OnClickCharacter()
        {
            OnClickClose();
            var e = new Show_UI_Event(UIConfig.CharacterUI);
            EventDispatcher.Instance.DispatchEvent(e);
            PlayerDataManager.Instance.WeakNoticeData.BagTotal = false;
         }
        public void VipFunctionStore()
        {
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel) < 3)
            {
                var str = GameUtils.GetDictionaryText(100000675);
                GameUtils.ShowHintTip(str);
            }
            else
            {
                OnClickClose();
                var e = new UIEvent_RechargeFrame_OnClick(3);
                EventDispatcher.Instance.DispatchEvent(e);

            }
        }

        public void OnClickShiZhuang()
        {
            OnClickClose();
            var e = new Show_UI_Event(UIConfig.ShiZhuangUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }
	
	    private void OnEvent_CloseUI(IEvent ievent)
	    {
	        var e = ievent as CloseUiBindRemove;
	        if (e.Config != UIConfig.StoreUI)
	        {
	            return;
	        }
	        if (e.NeedRemove == 0)
	        {
	            isDeleteBind = false;
	        }
	        else
	        {
	            if (isDeleteBind == false)
	            {
	                DeleteListener();
	            }
	            isDeleteBind = true;
	        }
	    }
	
	    private void OnDestroy()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        if (isDeleteBind == false)
	        {
	            DeleteListener();
	        }
	        isDeleteBind = true;
            //这个值得修改暂时这么写,以后要细分打开的from
            PlayerDataManager.Instance.isTaskWildShop = false;

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        if (isDeleteBind)
	        {
	            DeleteListener();
	        }
            
            //PlayerDataManager.Instance.isTaskWildShop = false;
#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }
	
	    private void OnEnable()
	    {
#if !UNITY_EDITOR
	try
	{
#endif
            GameObject tab = transform.FindChildRecursive("tab").gameObject;
            if (null != tab) tab.SetActive(true);
            if (PlayerDataManager.Instance.isTaskWildShop)
            {
                tab.SetActive(false);
            }

            if (isDeleteBind)
	        {
	            EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUI);
	            var control = UIManager.Instance.GetController(UIConfig.StoreUI);
	            BindData.SetBindDataSource(control.GetDataModel(""));
	            if (theBag)
	            {
	                theBag.AddBindEvent();
	            }
	        }
	        isDeleteBind = true;
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    private void DeleteListener()
	    {
	        EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUI);
	        BindData.RemoveBinding();
	        if (theBag != null)
	        {
	            theBag.RemoveBindEvent();
	        }
	    }
	
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        CreateBag();
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