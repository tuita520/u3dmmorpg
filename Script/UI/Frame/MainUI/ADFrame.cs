using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ScriptManager;
using DataTable;
using EventSystem;
using Shared;


public class ADFrame : MonoBehaviour
{
    private void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        trigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(5), CloseUI);
        
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    private void CloseUI()
    {
        if (trigger != null)
        {
            TimeManager.Instance.DeleteTrigger(trigger);
        }
        trigger = null;
        this.gameObject.SetActive(false);
    }
    private object trigger = null;

    public void OnClickGoTo()
    {
        var c = UIConfig.GetConfig(PlayerDataManager.Instance.TbVip.UIId);
        if (c == null)
        {
            return;
        }
        var arg = c.NewArgument();
        arg.Tab = PlayerDataManager.Instance.TbVip.UITabId;          
        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(c, arg));
    }
}
