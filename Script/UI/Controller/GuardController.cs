
/********************************************************************************* 

                         Scorpion




  *FileName:GuardController

  *Version:1.0

  *Date:2017-06-15

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class DefendFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        private int m_Se = 73000; //复活SkillUpgrading id
        private int m_choosedIndex;
        private GuardItemDataModel m_choosedItem;
        private readonly int m_upGradeingIdSkill = 73000; //复活SkillUpgrading id
        private readonly int m_totalRebornNum = 20;
        private GuardDataModel m_DataModel;
        #endregion

        #region 构造函数
        public DefendFrameCtrler()
        {
            EventDispatcher.Instance.AddEventListener(GuardStateChange.EVENT_TYPE, OnDefendStateChangeEvent);
            EventDispatcher.Instance.AddEventListener(GuardItemOperation.EVENT_TYPE, OnItemOperationEvent);
            EventDispatcher.Instance.AddEventListener(GuardUIOperation.EVENT_TYPE, OnDefendOperationEvent);
            CleanUp();
        }
        #endregion

        #region 固有函数
        public void RefreshData(UIInitArguments data)
        {
            var _count = m_DataModel.Lists.Count;
            var _selectId = 0;
            for (var i = 0; i < _count; i++)
            {
                var _item = m_DataModel.Lists[i];
                if (_item.State == (int)DefendState.Dead)
                {
                    _selectId = i;
                    break;
                }
            }
            SetChoosedItem(_selectId);
        }

        public void CleanUp()
        {
            m_DataModel = new GuardDataModel();
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

        public void OnShow()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private void OnDefendOperationEvent(IEvent ievent)
        {
            var _e = ievent as GuardUIOperation;
            switch (_e.Type)
            {
                case 0:
                {
                    RebornDefend();
                }
                    break;
            }
        }

        private void OnItemOperationEvent(IEvent ievent)
        {
            var _e = ievent as GuardItemOperation;
            var _index = _e.Index;
            if (_e.Type == 0)
            {
                SetChoosedItem(_index);
            }
        }

        private void OnDefendStateChangeEvent(IEvent ievent)
        {
            var _e = ievent as GuardStateChange;
            var _count = _e.Lists.Count;
            if (_count <= 4)
            {
                for (var i = 0; i < _e.Lists.Count; i++)
                {
                    m_DataModel.Lists[i].State = _e.Lists[i];
                }
            }
            m_DataModel.RebornCount = m_totalRebornNum - _e.ReliveCount;
        }

        #endregion




        private IEnumerator AllianceWarRespawnGuard(int selectIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.AllianceWarRespawnGuard(selectIndex);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

  
        private void RebornDefend()
        {
            if (m_DataModel.RebornCount <= 0)
            {
                // "守卫已经复活了20次，无法继续复活！"
                var _ee = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(271000), 20));
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            if (m_DataModel.NeedDiaCount > PlayerDataManager.Instance.GetRes((int) eResourcesType.DiamondRes))
            {
                var _ee = new ShowUIHintBoard(300401);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            NetManager.Instance.StartCoroutine(AllianceWarRespawnGuard(m_choosedIndex));
        }

        private void RenewNeed()
        {
            var _values = Table.GetSkillUpgrading(m_upGradeingIdSkill).Values;
            if (m_DataModel.RebornCount <= 0)
            {
                m_DataModel.NeedDiaCount = _values[m_totalRebornNum - 1];
            }
            else
            {
                m_DataModel.NeedDiaCount = _values[m_totalRebornNum - m_DataModel.RebornCount];
            }
        }

        private void SetChoosedItem(int index)
        {
            if (m_choosedItem != null)
            {
                m_choosedItem.Selected = false;
            }
            m_choosedItem = m_DataModel.Lists[index];
            m_choosedItem.Selected = true;
            m_choosedIndex = index;
            RenewNeed();
        }

  

        private enum DefendState
        {
            ALive = 0,
            Dead = 1
        }
    }
}