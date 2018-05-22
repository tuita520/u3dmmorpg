using System;
#region using

using SignalChain;
using EventSystem;
using UnityEngine;
using DataTable;
using DataContract;
using System.Collections.Generic;
using System.Collections;
using ScorpionNetLib;
using ClientService;
using ClientDataModel;

#endregion

namespace GameUI
{
	public class MountFeedCell : MonoBehaviour
	{
	    public ListItemLogic item;
	    public void OnClickBtn()
	    {
	        MountFeedItemDataModel data = item.Item as MountFeedItemDataModel;
            if(data != null)    
    	        EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(40, data.Item.ItemId));
	    }
        public GameObject mEffect;

        private void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif

            EventDispatcher.Instance.AddEventListener(MountEffect_Event.EVENT_TYPE, OnEffectEvent);
        
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

            EventDispatcher.Instance.RemoveEventListener(MountEffect_Event.EVENT_TYPE, OnEffectEvent);
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
        private void OnEffectEvent(IEvent ievent)
        {
            MountEffect_Event e = ievent as MountEffect_Event;
            if (e == null)
                return;
            if (e.Type != 2)
                return;
            MountFeedItemDataModel data = item.Item as MountFeedItemDataModel;
            if (data == null)
                return;
            if (data.Item.ItemId != e.ID)
                return;
            NetManager.Instance.StartCoroutine(OnEffectCoroutine());
        }
        private IEnumerator OnEffectCoroutine()
        {
            mEffect.SetActive(false);
            mEffect.SetActive(true);
            yield return new WaitForSeconds(4);
            mEffect.SetActive(false);
        }
	}
}