using ScriptManager;
using System;
#region using

using System.Collections.Generic;
using EventSystem;
using SignalChain;
using UnityEngine;

#endregion

namespace GameUI
{
	public class AttrFrame : MonoBehaviour, IChainRoot, IChainListener
	{
	    public List<UIEventTrigger> AddBtns;
	    private List<UIEventTrigger> setBtns;//点击数字区域直接设置加的数量
	    public BindDataRoot Binding;
	    public List<UIEventTrigger> DelBtns;
	    public List<StackLayout> Layouts;
	    private bool flag;

	    private void Awake()
	    {
	        setBtns=new List<UIEventTrigger>();
	        var trans = transform;
            var strength = trans.Find("AttributeInfo/Addpiont/Strength");
	        if (strength != null)
	        {
	            var trigger = strength.GetComponent<UIEventTrigger>();
	                setBtns.Add(trigger);
	            
	        }
            var agility = trans.Find("AttributeInfo/Addpiont/Agility");
            if (agility != null)
	        {
                var trigger = agility.GetComponent<UIEventTrigger>();
	                setBtns.Add(trigger);
	            
	        }
            var intelligence = trans.Find("AttributeInfo/Addpiont/Intelligence");
	        if (agility != null)
	        {
                var trigger = intelligence.GetComponent<UIEventTrigger>();
	                setBtns.Add(trigger);
	            
	        }
            var endurance = trans.Find("AttributeInfo/Addpiont/Endurance");
	        if (agility != null)
	        {
                var trigger = endurance.GetComponent<UIEventTrigger>();
	                setBtns.Add(trigger);
	        }
	    }
	    private void LateUpdate()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
	
	        if (flag)
	        {
	            flag = false;
	            {
	                var __list1 = Layouts;
	                var __listCount1 = __list1.Count;
	                for (var __i1 = 0; __i1 < __listCount1; ++__i1)
	                {
	                    var layout = __list1[__i1];
	                    {
	                        if (layout)
	                        {
	                            layout.ResetLayout();
	                        }
	                    }
	                }
	            }
	        }
	
	#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
	#endif
	    }

        public void OnCkickPointOperate(int type, int index = -1)
	    {
	        var e = new AttributePointOperate(type, index);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	    public void OnClickBtnAutoAdd()
	    {
            OnCkickPointOperate(6);
	    }
	
	    public void OnClickBtnCancel()
	    {
	        OnCkickPointOperate(5);
	    }
	
	    public void OnClickBtnClose()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.AttributeUI));
	
	        //  EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.CharacterUI));
	    }

        public void OpenAdvancedProperty()
        {
           EventDispatcher.Instance.DispatchEvent(new OpenAdvancedProperty_Event(0));
        }

        public void CloseAdvancedProperty()
        {
            EventDispatcher.Instance.DispatchEvent(new CloseAdvancedProperty_Event(0));
        }
	
	    public void OnClickBtnConfirm()
	    {
	        OnCkickPointOperate(4);
	    }
	
	    public void OnClickBtnRecommond()
	    {
	        OnCkickPointOperate(2);
	    }
	
	    public void OnClickBtnReset()
	    {
	        OnCkickPointOperate(3);
	    }
	
	    public void OnClickdontShowFruit()
	    {
	        OnCkickPointOperate(600);
	    }
	
	    public void OnClickShowFruit()
	    {
	        OnCkickPointOperate(500);
	    }
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
	
	        Binding.RemoveBinding();
	
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
	        var controllerBase = UIManager.Instance.GetController(UIConfig.AttributeUI);
	        if (controllerBase == null)
	        {
	            return;
	        }
	        Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);
	        Binding.SetBindDataSource(controllerBase.GetDataModel(""));
	        flag = true;

            Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);
	#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
	#endif
	    }
	
	    private void Start()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
	
	
	        var AddBtnsCount0 = AddBtns.Count;
	        for (var i = 0; i < AddBtnsCount0; i++)
	        {
	            var j = i;
	            var deleget = new EventDelegate(() => { OnCkickPointOperate(1, j); });
	            AddBtns[i].onClick.Add(deleget);
	            var delegetPress = new EventDelegate(() => { OnCkickPointOperate(11, j); });
	            AddBtns[i].onPress.Add(delegetPress);
	            var delegetRelease = new EventDelegate(() => { OnCkickPointOperate(21, j); });
	            AddBtns[i].onRelease.Add(delegetRelease);
	        }
	
	        var DelBtnsCount1 = DelBtns.Count;
	        for (var i = 0; i < DelBtnsCount1; i++)
	        {
	            var j = i;
	            var deleget = new EventDelegate(() => { OnCkickPointOperate(0, j); });
	            DelBtns[i].onClick.Add(deleget);
	            var delegetPress = new EventDelegate(() => { OnCkickPointOperate(10, j); });
	            DelBtns[i].onPress.Add(delegetPress);
	            var delegetRelease = new EventDelegate(() => { OnCkickPointOperate(20, j); });
	            DelBtns[i].onRelease.Add(delegetRelease);
	        }
	        var SetBtnsCount2 = setBtns.Count;
            for (int i = 0; i < SetBtnsCount2; i++)
	        {
	            var j = i;
	            if (setBtns[i] != null)
	            {
	                var deleget = new EventDelegate(() => { OnCkickPointOperate(30, j); });
	                setBtns[i].onClick.Add(deleget);
	            }
	        }
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
	#endif
	    }
	
	    public void Listen<T>(T message)
	    {
	        flag = true;
	    }
	}
}