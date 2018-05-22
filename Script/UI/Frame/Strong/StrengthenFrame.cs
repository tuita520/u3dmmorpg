using ScriptManager;
using System;
#region using

using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class StrengthenFrame : MonoBehaviour
	{
	    public BindDataRoot Binding;
	    private float lastScrollOffset;
	    private Vector3 lastScrollPos;
	    public UIScrollViewSimple StrongScrollView;
	
	    public void Close()
	    {
// 	        lastScrollOffset = StrongScrollView.oldoffset;
// 	        lastScrollPos = StrongScrollView.transform.localPosition;
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.NewStrongUI));
	    }
	
	    private void ScrollCeter(IEvent ievent)
	    {
// 	        var e = ievent as UIEvent_StrongSetGridLookIndex;
// 	        if (e.Type == 1)
// 	        {
// 	            lastScrollOffset = StrongScrollView.oldoffset;
// 	            lastScrollPos = StrongScrollView.transform.localPosition;
// 	        }
// 	        else if (e.Type == 0)
// 	        {
// 	            if (e.Index == -1)
// 	            {
// 	                StrongScrollView.SetLookIndex(e.Index, false);
// 	            }
// 	            else
// 	            {
// 	                StrongScrollView.MoveToOffset(lastScrollPos, lastScrollOffset);
// 	            }
// 	        }
	    }
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
            Binding.RemoveBinding();
	        EventDispatcher.Instance.RemoveEventListener(UIEvent_StrongSetGridLookIndex.EVENT_TYPE, ScrollCeter);
	
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
            //todo
            var controllerBase2 = UIManager.Instance.GetController(UIConfig.StrongUI);
            if (controllerBase2 == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase2.GetDataModel(""));
            Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);

            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewStrongOperation(100));


	        EventDispatcher.Instance.AddEventListener(UIEvent_StrongSetGridLookIndex.EVENT_TYPE, ScrollCeter);
	
	
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