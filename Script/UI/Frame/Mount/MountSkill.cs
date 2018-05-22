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

#endregion

namespace GameUI
{
	public class MountSkill : MonoBehaviour
	{
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
        public void OnClickClose()
	    {
            EventDispatcher.Instance.DispatchEvent(new MountClickBtn_Event(0));
	    }
        private void OnEffectEvent(IEvent ievent)
        {
            MountEffect_Event e = ievent as MountEffect_Event;
            if (e == null)
                return;
            if (e.Type != 1)
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