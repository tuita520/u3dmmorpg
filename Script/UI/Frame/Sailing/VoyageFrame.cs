using ScriptManager;
using System;
#region using

using System.Collections.Generic;
using EventSystem;
using UnityEngine;
using DataTable;
#endregion

namespace GameUI
{
	public class VoyageFrame : MonoBehaviour //,IChainRoot,IChainListener
	{
	    public BindDataRoot Binding;
	    public List<UIButton> EquipButtons;
	    public UISpriteAnimation LevelUpAnimation;
	    private GameObject mFlyPrefabExp;
	    private GameObject MyPrefab;
	    public GameObject ScrollViewTempBag;
	    public UIToggle btnToggle1;
	    public UIToggle btnToggle2;
	    public List<UIToggle> Colors;
	    public List<UIToggle> TempColors; 
	    //线的点击
	    public void AutoShipClick()
	    {
	        var e = new UIEvent_SailingOperation(4,1);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }

	    public void AutoCancelShipClick()
	    {
            var e = new UIEvent_SailingOperation(4,0);
            EventDispatcher.Instance.DispatchEvent(e);
	    }

	
	    public void CloseWoodTips()
	    {
	        var e = new UIEvent_SailingOperation(8);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void LevelBackClick()
	    {
	        var e = new UIEvent_SailingOperation(5);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void LevelUpClick()
	    {
	        var e = new UIEvent_SailingOperation(3);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
        public void ShowPreviewUIClick()
        {
            var e = new ShowPreviewUIEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClosePreviewUI()
        {
            var e = new ShowPreviewUIEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }
	    //亮点的点击
	    public void LightPointClick(int index)
	    {
	        var e = new UIEvent_SailingLightPoint();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
        public void OnClickAccess()
        {
            var index = 5;
            var tb = Table.GetSailing(index);
            if(tb == null || tb.CanCall <= 0)
                return;

            if (PlayerDataManager.Instance.GetItemCount(tb.NeedType) < tb.ItemCount)
            {
                var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            if (PlayerDataManager.Instance.IsCheckSailingTip)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingLightPointAccess(index));
                return;
            }

            string str = string.Format(GameUtils.GetDictionaryText(100002105), tb.ItemCount);
            EventDispatcher.Instance.DispatchEvent(new SailingShowMessageBoxEvent(0,true,str));

            //UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "", () =>
            //{
            //    EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingLightPointAccess(index));
            //});    
        }

        public void MessageBox_OnOkClick()
        {
            var index = 5;
            EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingLightPointAccess(index));
            EventDispatcher.Instance.DispatchEvent(new SailingShowMessageBoxEvent(1,false, ""));
        }

        public void MessageBox_OnCancleClick()
        {
            EventDispatcher.Instance.DispatchEvent(new SailingShowMessageBoxEvent(2,false, ""));
        }

        public void MessageBox_OnCloseClick()
        {
            EventDispatcher.Instance.DispatchEvent(new SailingShowMessageBoxEvent(3,false, ""));
        }


	
	    public void OnClickArrangeBtn()
	    {
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingOperation(0));
	    }
	
	    public void OnClickBtnBack()
	    {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingReturnBtn(1));
            //var e = new Close_UI_Event(UIConfig.SailingUI);
            //EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickBtnClose()
	    {
	        var e = new Close_UI_Event(UIConfig.SailingUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnclickEatAll()
	    {
	        int flag = 0;
	        List<bool> checkBox = new List<bool>();
	        for (int i = 0; i < Colors.Count; i++)
	        {
	            flag |= Colors[i].mIsActive ? 1 << i : 0;
                checkBox.Add(Colors[i].mIsActive);
	        }
            EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingCheckType(checkBox));
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingOperation(1,flag));            
	    }

	    public void OnClickSplit()
	    {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingOperation(9));
	    }
	
	    public void OnClickEquipItem(int index)
	    {
	        var e = new UIEvent_SailingPackItemUI();
	        e.PutOnOrOff = 1;
            e.Index = index;
            e.BagId = (int)eBagType.MedalUsed;
	        EventDispatcher.Instance.DispatchEvent(e);
	    }

	    public void OnClickTakeOff()
	    {
            var e = new UIEvent_SailingPutOnClick();
            e.PutOnOrOff = 0;
            EventDispatcher.Instance.DispatchEvent(e);
	    }
        public void OnClickTakeOn()
        {
            var e = new UIEvent_SailingPutOnClick();
            e.PutOnOrOff = 1;
            EventDispatcher.Instance.DispatchEvent(e);
        }
	
	    public void OnClickPickAll(bool isauto = false)
	    {
            int flag = 0;
            for (int i = 0; i < TempColors.Count; i++)
            {
                flag |= TempColors[i].mIsActive ? 1 << i : 0;
            }
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingPickAll(flag,isauto));
	    }
	
	    public void OnClickReturnBtn()
	    {
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingReturnBtn(1));
	    }
	
	    public void OnClickReturnShipBtn()
	    {
	        btnToggle1.value = true;
            
//	        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingReturnBtn(0));
	    }
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        EventDispatcher.Instance.RemoveEventListener(UIEvent_SailingPlayAnimation.EVENT_TYPE, PlayLevelUpAnimation);
	        EventDispatcher.Instance.RemoveEventListener(UIEvent_SailingPlayEatAnim.EVENT_TYPE, PlayEatAllAnim);
	        //EventDispatcher.Instance.RemoveEventListener(UIEvent_SailingFlyAnim.EVENT_TYPE, ExpFlyAnim);
            EventDispatcher.Instance.RemoveEventListener(AutoRecycleMedalEvent.EVENT_TYPE, OnAutoRecycleMedalEvent);
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingOperation(6));
	        Binding.RemoveBinding();


            var e = new UIEvent_SailingOperation(4, 0);
            EventDispatcher.Instance.DispatchEvent(e);

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
	        var controllerBase = UIManager.Instance.GetController(UIConfig.SailingUI);
	        if (controllerBase == null)
	        {
	            return;
	        }
	        Binding.SetBindDataSource(controllerBase.GetDataModel(""));
	        Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel.Bags.Resources);
	        //EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingReturnBtn(1));
	        EventDispatcher.Instance.AddEventListener(UIEvent_SailingPlayAnimation.EVENT_TYPE, PlayLevelUpAnimation);
	        EventDispatcher.Instance.AddEventListener(UIEvent_SailingPlayEatAnim.EVENT_TYPE, PlayEatAllAnim);
	        //EventDispatcher.Instance.AddEventListener(UIEvent_SailingFlyAnim.EVENT_TYPE, ExpFlyAnim);
            EventDispatcher.Instance.AddEventListener(AutoRecycleMedalEvent.EVENT_TYPE, OnAutoRecycleMedalEvent);
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    private void PlayEatAllAnim(IEvent ievent)
	    {
	        var e = ievent as UIEvent_SailingPlayEatAnim;
	        var List = e.List;
	        var mTime = 0.1f;
	        if (ScrollViewTempBag != null)
	        {
	            var to = ScrollViewTempBag.transform.GetChild(e.Index);
	            for (var i = 0; i < List.Count; i++)
	            {
	                var item = ScrollViewTempBag.transform.GetChild(List[i]);   
	                var obj = Instantiate(MyPrefab) as GameObject;
	                mTime += 0.2f;
	                PlayerDataManager.Instance.PlayFlyItem(obj, item, to, e.ItemIds[i], 0, new Vector3(0, 0, 0), mTime,
	                    new Vector3(0, 50, 0));
	            }
	        }
	    }
        private void OnAutoRecycleMedalEvent(IEvent ievent)
        {
            var e = ievent as AutoRecycleMedalEvent;
            if (null == e)
            {
                return;
            }
            OnClickPickAll(true);
        }
	    public void PlayLevelUpAnimation(IEvent ievent)
	    {
	        LevelUpAnimation.Play();
	    }
	
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        var OperateButtonCount0 = EquipButtons.Count;
	        for (var i = 0; i < OperateButtonCount0; i++)
	        {
	            var j = i;
	            var deleget = new EventDelegate(() => { OnClickEquipItem(j); });
	            EquipButtons[i].onClick.Add(deleget);
	        }
	     
            MyPrefab = ResourceManager.PrepareResourceSync<GameObject>("UI/Icon/IconIdFlySailing.prefab");
	        mFlyPrefabExp = ResourceManager.PrepareResourceSync<GameObject>("UI/Icon/IconIdFly.prefab");
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    // Update is called once per frame
	    private void Update()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        // RefreshAttrPanel(null);
	
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