using ScriptManager;
using System;
#region using
using DataTable;
using EventSystem;
using UnityEngine;
using ClientDataModel;
using System.Collections;
using System.Collections.Generic;
/*-------------------------------------------------------------------
Copyright 2015 Minty Game LTD. All Rights Reserved.
Maintained by  wangxing 
-------------------------------------------------------------------*/

#endregion


public class TowerRewardLogic : MonoBehaviour
{
    public BindDataRoot Binding;
    private IControllerBase mController;
    public Coroutine DrawCoroutine;
    public UILabel AutoDrawLabel;
    public void OnBtnConfirm()
    {
        int cur = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e623);
        var tb = Table.GetClimbingTower(cur + 1);
        if (tb != null)
        {
            GameUtils.EnterFuben(tb.FubenId);
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ClimbingTowerRewardUI));
        }
        else
        {
            GameUtils.ExitFuben();
        }
    }

    public void OnBtnBack()
    {
        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ClimbingTowerRewardUI));
    }
    private void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif
        
        mController = UIManager.Instance.GetController(UIConfig.ClimbingTowerRewardUI);
        if (mController == null)
        {
            return;
        }
        Binding.SetBindDataSource(mController.GetDataModel(""));
        StartAutoDraw();

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

    private void ClearCoroutine()
    {
         if (DrawCoroutine != null)
        {
            NetManager.Instance.StopCoroutine(DrawCoroutine);
            DrawCoroutine = null;
        }
    }

   
    public void OnDisable()
    {
#if !UNITY_EDITOR
try
{
#endif

        ClearCoroutine();
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    private void StartAutoDraw()
    {
        ClearCoroutine();
        DrawCoroutine = NetManager.Instance.StartCoroutine(AutoDrawCoroutine());
    }
    private IEnumerator AutoDrawCoroutine()
    {
        var nowtime = Game.Instance.ServerTime;
        var dt = Game.Instance.ServerTime.AddSeconds(8);
        while (nowtime < dt)
        {
            AutoDrawLabel.text = GameUtils.GetTimeDiffString(dt);
            yield return new WaitForSeconds(0.3f);
            nowtime = Game.Instance.ServerTime;
        }
        OnBtnConfirm();
    }
}
