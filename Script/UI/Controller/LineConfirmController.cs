/********************************************************************************* 

                         Scorpion




  *FileName:LineConfirmController

  *Version:1.0

  *Date:2017-06-16

  *Description:

**********************************************************************************/

#region using

using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataContract;
using DataTable;
using EventSystem;
using Shared;

#endregion

namespace ScriptController
{
    public class LineEnsureFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        private LineConfirmDataModel m_DataModel;
        private LineMemberDataModel m_noneMemberData = new LineMemberDataModel();
        private object m_closeTrigger;
        #endregion

        #region 构造函数
        public LineEnsureFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(LineMemberConfirmEvent.EVENT_TYPE, OnLineMembEnsureEvent);
            EventDispatcher.Instance.AddEventListener(LineMemberClickEvent.EVENT_TYPE, OnLineMemClicEvent);
            EventDispatcher.Instance.AddEventListener(TeamChangeEvent.EVENT_TYPE, OnGroupChangeEvent);
            EventDispatcher.Instance.AddEventListener(LimitActiveRefreshLineConEvent.EVENT_TYPE, RefreshLimitActiveEvent);
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            if (m_closeTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(m_closeTrigger);
                m_closeTrigger = null;
            }
            m_DataModel = new LineConfirmDataModel();
        }
        private UIInitArguments msg { get; set; }
        private int num = 0;
        public void RefreshData(UIInitArguments data)
        {
            var id = PlayerDataManager.Instance.PlayerDataModel.QueueUpData;
            IControllerBase controller = UIManager.Instance.GetController(UIConfig.LineConfim);
            if (id.QueueId >= 100 && id.QueueId <= 117 && num ==0)
            {
                msg = data;
                num += 1;
                if (null != controller)
                {
                    controller.State = FrameState.Open;
                }
                return;
            }
            if (null != controller)
            {
                controller.State = FrameState.Close;
            }
            var _arg = data as LineConfirmArguments;
            if (_arg == null)
            {
                return;
            }
        
            OnCancelChoose();
            var _msg = _arg.Msg;
            var _hasDouble = false;

            var _leaderCheck = false;

            if (_msg.Characters.Count == 0)
            {
                return;
            }
            m_DataModel.TotalCount = _msg.Characters.Count;            
            m_DataModel.IsShowMini = false;
            m_DataModel.QueueId = _msg.QueueId;
            if (_msg.QueueId != -1)
            {
                var _tbQueue = Table.GetQueue(_msg.QueueId);
                if (_tbQueue != null)
                {
                    if (_tbQueue.AppType == 1)
                    {
                        _hasDouble = true;
                    }
                }
            }
            else
            {
                if (_msg.Characters[0].CharacterId == PlayerDataManager.Instance.GetGuid())
                {
                    _leaderCheck = true;
                }
            }


            if (_hasDouble == false)
            {
                m_DataModel.IsBattle = false;

                for (var i = 0; i < 5; i++)
                {
                    var _member = m_DataModel.OtherList[i];
                    _member.Reset();
                }

                var c = _msg.Characters.Count;
                for (var i = 0; i < c; i++)
                {
                    var _member = m_DataModel.SelfList[i];
                    var _charInfo = _msg.Characters[i];
                    SetMembMsg(_member, _charInfo);
                }
                for (var i = c; i < 5; i++)
                {
                    var _member = m_DataModel.SelfList[i];
                    _member.Reset();
                }

                if (_leaderCheck)
                {
                    m_DataModel.SelfList[0].IsConfirm = true;
                    m_DataModel.ConfirmCount = 1;
                }
            }
            else
            {
                m_DataModel.IsBattle = true;
                var _c = _msg.Characters.Count;
                var _half = (_c + 1) / 2;

                for (int i = 0, j = _half; i < _half; i++, j++)
                {
                    var _member = m_DataModel.SelfList[i];
                    var _charInfo = _msg.Characters[i];
                    SetMembMsg(_member, _charInfo);

                    _member = m_DataModel.OtherList[i];
                    if (j >= _msg.Characters.Count)
                    {
                        _member.Reset();
                        break;
                    }
                    _charInfo = _msg.Characters[j];
                    SetMembMsg(_member, _charInfo);
                }
                for (var i = _half; i < 5; i++)
                {
                    var _member = m_DataModel.SelfList[i];
                    _member.Reset();

                    _member = m_DataModel.OtherList[i];
                    _member.Reset();
                }
            }

            var _countDown = Table.GetClientConfig(222).ToInt();
            m_DataModel.CoolDown = Game.Instance.ServerTime.AddSeconds(_countDown);

            if (m_closeTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(m_closeTrigger);
            }

            m_closeTrigger = TimeManager.Instance.CreateTrigger(m_DataModel.CoolDown, () =>
            {
                TimeManager.Instance.DeleteTrigger(m_closeTrigger);
                var _e = new Close_UI_Event(UIConfig.LineConfim);
                EventDispatcher.Instance.DispatchEvent(_e);
           
                //if (!PlayerDataManager.Instance.IsInFubenScnen())
                //{
                //    if ((_msg.QueueId >= 100 && _msg.QueueId <= 117 || _msg.QueueId == 200))
                //    {
                //        //即使有玩家未同意，倒计时结束以后也能进入游戏
                //        EventDispatcher.Instance.DispatchEvent(new UIEvent_ButtonClicked(BtnType.Activity_Enter));
                //    }
                //    else
                //    {
                //         var con = UIManager.Instance.GetController(UIConfig.DungeonUI);
                //         if (null != con)
                //         {
                //             var datamodel = con.GetDataModel("") as DungeonDataModel;
                //             if (null != datamodel)
                //             {
                //                 GameUtils.EnterFuben(datamodel.SelectDungeon.InfoData.Id);
                //             }
                //         }                       
                //    }                
                //}                
                m_closeTrigger = null;
            });
            num = 0;
        }

        private void RefreshLimitActiveEvent(IEvent eve)
        {
            if (msg == null)
            {
                return;
            }
            var e = new Show_UI_Event(UIConfig.LineConfim, msg);
            EventDispatcher.Instance.DispatchEvent(e);
        } 

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private void OnLineMemClicEvent(IEvent ievent)
        {
            var _e = ievent as LineMemberClickEvent;
            switch (_e.Type)
            {
                case 0:
                {
                    ClickCharMsg(m_DataModel.SelfList, _e.Index);
                }
                    break;
                case 1:
                {
                    ClickCharMsg(m_DataModel.OtherList, _e.Index);
                }
                    break;
                case 2:
                {
                    OnCancelChoose();
                }
                    break;
                case 3:
                {
                    m_DataModel.IsShowMini = false;
                }
                    break;
                case 4:
                {
                    m_DataModel.IsShowMini = true;
                }
                    break;
            }
        }

        private void OnLineMembEnsureEvent(IEvent ievent)
        {
            var _e = ievent as LineMemberConfirmEvent;

            var _isRet = _e.Type != 0;
            for (var i = 0; i < 5; i++)
            {
                var _member = m_DataModel.SelfList[i];
                if (_member.ChararterId == _e.CharacterId)
                {
                    if (_isRet)
                    {
                        _member.IsConfirm = true;
                    }
                    else
                    {
                        _member.IsConcel = true;
                    }
                    break;
                }

                _member = m_DataModel.OtherList[i];
                if (_member.ChararterId == _e.CharacterId)
                {
                    if (_isRet)
                    {
                        _member.IsConfirm = true;
                    }
                    else
                    {
                        _member.IsConcel = true;
                    }
                    break;
                }
            }
            if (_isRet == false)
            {
                //有人取消后3秒钟关闭
                var _t = Game.Instance.ServerTime.AddSeconds(3);
                if (m_closeTrigger != null)
                {
                    TimeManager.Instance.DeleteTrigger(m_closeTrigger);
                }
                m_closeTrigger = TimeManager.Instance.CreateTrigger(_t, () =>
                {
                    TimeManager.Instance.DeleteTrigger(m_closeTrigger);
                    var _e1 = new Close_UI_Event(UIConfig.LineConfim);
                    EventDispatcher.Instance.DispatchEvent(_e1);
                    m_closeTrigger = null;
                });
            }
            else
            {
                CheckEnsureNum();
            }
        }

        private void OnGroupChangeEvent(IEvent ievent)
        {
            //TeamChangeEvent e = ievent as TeamChangeEvent;
            if (State != FrameState.Open)
            {
                return;
            }
            if (m_DataModel.QueueId != -1)
            {
                return;
            }
            if (m_closeTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(m_closeTrigger);
                m_closeTrigger = null;
            }
            var _e = new Close_UI_Event(UIConfig.LineConfim);
            EventDispatcher.Instance.DispatchEvent(_e);
        }
        #endregion




        private void CheckEnsureNum()
        {
            var _count = 0;
            m_DataModel.ConfirmCount = 0;
            for (var i = 0; i < 5; i++)
            {
                var _member = m_DataModel.SelfList[i];
                if (_member.ChararterId != 0 && _member.IsConfirm)
                {
                    _count++;
                }
                _member = m_DataModel.OtherList[i];
                if (_member.ChararterId != 0 && _member.IsConfirm)
                {
                    _count++;
                }
            }
            m_DataModel.ConfirmCount = _count;
        }

        private void OnCancelChoose()
        {
            m_DataModel.SelectData = m_noneMemberData;
        }

        private void ClickCharMsg(ReadonlyObjectList<LineMemberDataModel> list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }
            m_DataModel.SelectData = list[index];
        }

   

        private void SetMembMsg(LineMemberDataModel member, TeamCharacterOne charInfo)
        {
            member.ChararterId = charInfo.CharacterId;
            member.Type = charInfo.RoleId;
            member.Ladder = charInfo.Ladder;
            member.Level = charInfo.Level;
            member.Name = charInfo.CharacterName;
            member.FightPoint = charInfo.FightPoint;
            member.IsConfirm = charInfo.QueueResult == 1;
            member.IsConcel = charInfo.QueueResult == 0;
            member.rebornId = GetLadderIconId (charInfo.Ladder,charInfo.RoleId);
        }

        int GetLadderIconId(int ladder, int pro)
        {
            int defaultIconId = 0;
            var tabTrans = Table.GetTransmigration(ladder);
            var tabActor = Table.GetActor(pro);
            if (null != tabTrans && null != tabActor)
            {
                switch (pro)
                {
                    case 0: // 剑士
                        defaultIconId = tabTrans.zsRebornIconSquare;
                        break;
                    case 1: // 法师
                        defaultIconId = tabTrans.fsRebornIconSquare;
                        break;
                    case 2: // 弓箭手
                        defaultIconId = tabTrans.gsRebornIconSquare;
                        break;
                    //case 3: // 游侠
                    //    defaultIconId = tabTrans.fsRebornIconSquare;
                    //    break;
                }
            }

            return defaultIconId;
        }
    }
}