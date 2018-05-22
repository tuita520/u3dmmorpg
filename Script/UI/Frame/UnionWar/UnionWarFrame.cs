using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventSystem;
using UnityEngine;

namespace GameUI
{
    public class UnionWarFrame:MonoBehaviour
    {
        #region 绑定和解绑
        public BindDataRoot Binding;
        private bool delBind = true;
        private void OnEnable()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            if (delBind)
            {
                EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUI);

                var controllerBase = UIManager.Instance.GetController(UIConfig.UnionWarFrame);
                if (controllerBase == null)
                {
                    return;
                }
                Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            }
            delBind = true;

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        private void OnEvent_CloseUI(IEvent iEvent)
        {
            var e = iEvent as CloseUiBindRemove;
            if (e.Config != UIConfig.BossHomeUI)
            {
                return;
            }
            if (e.NeedRemove == 0)
            {
                delBind = false;
            }
            else
            {
                if (delBind == false)
                {
                    DeleteBindEvent();
                }
                delBind = true;
            }
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            if (delBind == false)
            {
                DeleteBindEvent();
            }
            delBind = true;
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
            if (delBind == false)
            {
                DeleteBindEvent();
            }
            delBind = true;
#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }
        private void DeleteBindEvent()
        {
            EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUI);

            Binding.RemoveBinding();
        }
#endregion

        #region 按钮点击
        /// <summary>
        /// 返回按钮点击 0
        /// </summary>
        public void OnClickBack()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(0));
        }
        /// <summary>
        /// 游戏规则点击 1
        /// </summary>
        public void OnClickPlayerRules()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(1));
        }
        /// <summary>
        /// 活动报名点击 2
        /// </summary>
        public void OnClickJoinActivity()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(2));
        }
        /// <summary>
        /// 赛程查询点击 3
        /// </summary>
        public void OnClickCompetitionProcessQuery()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(3));
        }
        /// <summary>
        /// 进入战场点击 4
        /// </summary>
        public void OnClickEnterBattleField()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(4));
        }
        /// <summary>
        /// 赛程 突围赛点击 5
        /// </summary>
        public void OnClickBreakOutCompetition()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(5));
        }
        /// <summary>
        ///赛程 决赛点击 6
        /// </summary>
        public void OnClickFinalCompetition()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(6));
        }
        /// <summary>
        ///排名 突围赛第一轮tag点击 7
        /// </summary>
        public void OnClickBreakOut1()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(7));
        }
        /// <summary>
        ///排名  突围赛第二轮tag点击 8
        /// </summary>
        public void OnClickBreakOut2()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(8));
        }
        /// <summary>
        /// 排名 突围赛第三轮tag点击 9
        /// </summary>
        public void OnClickBreakOut3()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(9));
        }
        /// <summary>
        /// 排名 突围赛第四轮tag点击 10
        /// </summary>
        public void OnClickBreakOut4()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(10));
        }
        /// <summary>
        /// 排名 决赛tag点击 11
        /// </summary>
        public void OnClickFinalRank()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(11));
        }
        /// <summary>
        /// 玩法细则 报名阶段点击 12
        /// </summary>
        public void OnClickEnrollStep()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(12));
        }
        /// <summary>
        /// 玩法细则 突围阶段点击 13
        /// </summary>
        public void OnClickBreakOutStep()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(13));
        }
        /// <summary>
        /// 玩法细则 决赛阶段点击 14
        /// </summary>
        public void OnClickFinalStep()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(14));
        }
        /// <summary>
        /// 玩法细则 比赛玩法 15
        /// </summary>
        public void OnClickCompetitionRules()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(15));
        }
        /// <summary>
        /// 玩法细则 积分规则 16
        /// </summary>
        public void OnClickScoreRules()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(16));
        }
        /// <summary>
        /// 玩法细则 比赛奖励 17
        /// </summary>
        public void OnClickCompetitionRewards()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(17));
        }
        /// <summary>
        /// 排名点击 18
        /// </summary>
        public void OnClickRank()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(18));
        }
        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        public void OnClickClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_UnionWarBtnClick(19));
        }
        #endregion


    }
}
