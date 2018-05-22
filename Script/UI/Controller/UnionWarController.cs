using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientDataModel;
using ClientService;
using EventSystem;
using ScorpionNetLib;
using ScriptManager;
using UnityEngine;

namespace ScriptController
{
    public class UnionWarController : IControllerBase
    {
        public UnionWarController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(Event_UnionWarBtnClick.EVENT_TYPE, OnBtnClick);
            EventDispatcher.Instance.AddEventListener(Event_ApplyUnionWarInfo.EVENT_TYPE, ApplyUnionBattleInfo);
        }
        private FrameState mState;
        private UnionWarDataModel DataModel = new UnionWarDataModel();
        /// <summary>
        /// frame按钮点击
        /// </summary>
        /// <param name="iEvent"></param>
        private void OnBtnClick(IEvent iEvent)
        {
            var e = iEvent as Event_UnionWarBtnClick;
            if (e != null)
            {
                var index = e.Index;
                switch (index)
                {
                    case 0:
                        SwitchPanel(0);
                        break;
                    case 1:
                        SwitchPanel(3);
                        break;
                    case 2:
                        EnrollCompetition();
                        break;
                    case 3:
                        SwitchPanel(1);
                        break;
                    case 4:
                        EnterBattleField();
                        break;
                    case 5:
                        SwitchProgressTag(0);
                        break;
                    case 6:
                        SwitchProgressTag(1);
                        break;
                    case 7:
                        SwitchRankTag(0);
                        break;
                    case 8:
                        SwitchRankTag(1);
                        break;
                    case 9:
                        SwitchRankTag(2);
                        break;
                    case 10:
                        SwitchRankTag(3);
                        break;
                    case 11:
                        SwitchRankTag(4);
                        break;
                    case 12:
                        SwitchRulesTag(0);
                        break;
                    case 13:
                        SwitchRulesTag(1);
                        break;
                    case 14:
                        SwitchRulesTag(2);
                        break;
                    case 15:
                        SwitchRulesTag(3);
                        break;
                    case 16:
                        SwitchRulesTag(4);
                        break;
                    case 17:
                        SwitchRulesTag(5);
                        break;
                    case 18:
                        SwitchPanel(2);
                        break;
                    case 19:
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.UnionWarFrame));
                        break;
                }
            }
        }

        #region 按钮点击方法实现
       
        /// <summary>
        /// 活动报名
        /// </summary>
        private void EnrollCompetition()
        {
            var info = PlayerDataManager.Instance.BattleUnionDataModel;
            if (info != null)
            {//报名发包
                NetManager.Instance.StartCoroutine(
                    EnrollCometitionCor(info.MyUnion.UnionID, PlayerDataManager.Instance.CharacterGuid));
            }
        }

        
        /// <summary>
        /// 进入战场
        /// </summary>
        private void EnterBattleField()
        {
            var info = PlayerDataManager.Instance.BattleUnionDataModel;
            if (info != null)
            {//报名发包
                NetManager.Instance.StartCoroutine(
                    EnterBattleFieldCor(info.MyUnion.UnionID, PlayerDataManager.Instance.CharacterGuid));
            }
        }

        
        /// <summary>
        /// 切换界面
        /// </summary>
        /// <param name="index"></param>
        private void SwitchPanel(int index)//0 默认 1 赛程 2 排名 3 玩法
        {
            DataModel.PanelTag = index;
        }
        /// <summary>
        /// 切换赛程标签
        /// </summary>
        private void SwitchProgressTag(int index) //0 突围赛 1 决赛
        {
            DataModel.ProcessTag = index;
        }
        /// <summary>
        /// 切换排名标签
        /// </summary>
        /// <param name="index"></param>
        private void SwitchRankTag(int index)//4 ：决赛 0 ：突围1 1： 突围2  2：突围3  3：突围4
        {
            DataModel.RankTag = index;
        }
        /// <summary>
        /// 切换玩法标签
        /// </summary>
        private void SwitchRulesTag(int index)//0：报名阶段 1：突围阶段 2：决赛阶段 3：比赛玩法 4：积分规则 5：比赛奖励
        {
            DataModel.RulesTag = index;
        }
        #endregion
        #region 消息Coroutine
        private IEnumerator EnrollCometitionCor(int p1, ulong p2)
        {
            var _msg = NetManager.Instance.CSEnrollUnionBattle(p1, p2);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply)
            {
                if (_msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    DataModel.IsEnroll = true;//报名成功
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(50001000));
                }
                else
                {
                    UIManager.Instance.ShowNetError(_msg.ErrorCode);
                }
            }
        }
        private IEnumerator EnterBattleFieldCor(int p1, ulong p2)
        {
            var _msg = NetManager.Instance.CSEnterUnionBattle(p1, p2);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply)
            {
                if (_msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    //进入战场
                }
                else
                {
                    UIManager.Instance.ShowNetError(_msg.ErrorCode);
                }
            }
        }

        private void ApplyUnionBattleInfo(IEvent iEvent)
        {
            var info = PlayerDataManager.Instance.BattleUnionDataModel;
            if (info != null)
            {
                NetManager.Instance.StartCoroutine(
                    ApplyUnionBattleInfoCor(info.MyUnion.UnionID));
            }
        }
        private IEnumerator ApplyUnionBattleInfoCor(int unionID)
        {
            var _msg = NetManager.Instance.CSGetUnionBattleInfo(unionID);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply)
            {
                if (_msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    //请求盟战信息返回
                    var timeStart = DateTime.FromBinary(_msg.Response.EnrollStartTime);
                    var timeEnd = DateTime.FromBinary(_msg.Response.EnrollEndTime);
                    DataModel.StrTime = string.Format("{0}--{1}", timeStart.ToString("yyyy/MM/dd HH:mm:ss"),
                        timeEnd.ToString("yyyy/MM/dd HH:mm:ss"));
                    DataModel.IsEnroll = _msg.Response.IsEnroll == 1;
                    if (DateTime.Now > timeStart && DateTime.Now < timeEnd)
                    {
                        DataModel.CanPlay = true;
                    }
                    else
                    {
                        DataModel.CanPlay = false;
                    }
                }
                else
                {
                    UIManager.Instance.ShowNetError(_msg.ErrorCode);
                }
            }
        }
#endregion
        #region 接口方法
        public FrameState State
        {
            get { return mState; }
            set { mState = value; }
        }

        public void CleanUp()
        {
            
        }

        public void OnShow()
        {
            
        }

        public void Close()
        {
            
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            if (data != null)
            {
                SwitchPanel(data.Tab);
            }
        }

        public System.ComponentModel.INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void OnChangeScene(int sceneId)
        {
            
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }
#endregion
    }
}
