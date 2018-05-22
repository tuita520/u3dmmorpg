using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EventSystem;


namespace GameUI
{
    public class VipRewardFrame : MonoBehaviour
    {

        // Use this for initialization
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

        public void OnClickGetReward()
        {
            EventDispatcher.Instance.DispatchEvent(new GetVipRewardEvent());
            //ActivationReward((int)eActivationRewardType.DailyVipGift, cell.TableId);
        }

        public void OnClickRechage()
        {
            GameUtils.GotoUiTab(79, 0);
        }
    }
}