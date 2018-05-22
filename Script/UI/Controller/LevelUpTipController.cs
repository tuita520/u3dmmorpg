

/********************************************************************************* 

                         Scorpion




  *FileName:LevelUpTipController

  *Version:1.0

  *Date:2017-06-16

  *Description:

**********************************************************************************/

#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class RankImprovePromptFrameCtrler : IControllerBase
    {

        #region 静态变量

        #endregion

        #region 成员变量
        private LevelUpTipDataModel m_DataModel;
        #endregion

        #region 构造函数
        public RankImprovePromptFrameCtrler()
        {
            EventDispatcher.Instance.AddEventListener(UIEvent_SyncLevelUpAttrChange.EVENT_TYPE, OnRankChangeEvent);

            CleanUp();
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_DataModel = new LevelUpTipDataModel();
            InitLevelUpTable();
        }

        public void RefreshData(UIInitArguments data)
        {
            m_DataModel.RoleId = PlayerDataManager.Instance.GetRoleId();
            m_DataModel.ShowBtn = PlayerDataManager.Instance.GetFlag(1001) ? 1 : 0;
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
        private void OnRankChangeEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SyncLevelUpAttrChange;
            var _newAttrData = _e.AttrData.NewAttr;
            var _oldAttrData = _e.AttrData.OldAttr;
            var _oldLevel = 0;
            var _newLevel = 0;
            if (!_oldAttrData.TryGetValue((int)eAttributeType.Level, out _oldLevel))
            {
                return;
            }
            if (!_newAttrData.TryGetValue((int)eAttributeType.Level, out _newLevel))
            {
                return;
            }

            var _tableId = -1;
            for (var i = RankImproveRecordIdList.Count - 1; i >= 0; i--)
            {
                if (_oldLevel < RankImproveRecordIdList[i] && _newLevel >= RankImproveRecordIdList[i])
                {
                    _tableId = RankImproveRecordIdList[i];
                    break;
                }
            }
            if (_tableId == -1)
            {
                return;
            }

            var _tbGetlevel = Table.GetLevelupTips(_tableId);
            if (_tbGetlevel.IsShow != 1)
            {
                return;
            }

            var _length = _tbGetlevel.DictTip.Length;
            for (var i = 0; i < _length; i++)
            {
                var _item = _tbGetlevel.DictTip[i];
                if (_item != -1)
                {
                    if (i == 0)
                    {
                        m_DataModel.Tips[i] = String.Format(GameUtils.GetDictionaryText(_item), _newLevel);
                    }
                    else
                    {
                        m_DataModel.Tips[i] = GameUtils.GetDictionaryText(_item);
                    }
                }
                else
                {
                    m_DataModel.Tips[i] = String.Empty;
                }
            }
            m_DataModel.Level = _newLevel;
            foreach (var _item in _newAttrData)
            {
                switch (_item.Key)
                {
                    case (int)eAttributeType.PhyPowerMax:
                    {
                        m_DataModel.NewPhyPowerMax = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.MagPowerMax:
                    {
                        m_DataModel.NewMagPowerMax = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.PhyArmor:
                    {
                        m_DataModel.NewPhyArmor = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.MagArmor:
                    {
                        m_DataModel.NewMagArmor = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.HpMax:
                    {
                        m_DataModel.NewHpMax = _item.Value;
                    }
                        break;
                }
            }
            foreach (var _item in _oldAttrData)
            {
                switch (_item.Key)
                {
                    case (int)eAttributeType.PhyPowerMax:
                    {
                        m_DataModel.OldPhyPowerMax = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.MagPowerMax:
                    {
                        m_DataModel.OldMagPowerMax = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.PhyArmor:
                    {
                        m_DataModel.OldPhyArmor = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.MagArmor:
                    {
                        m_DataModel.OldMagArmor = _item.Value;
                    }
                        break;
                    case (int)eAttributeType.HpMax:
                    {
                        m_DataModel.OldHpMax = _item.Value;
                    }
                        break;
                }
            }
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.LevelUpTip));
        }
        #endregion



        #region 升级界面提示逻辑

        private readonly List<int> RankImproveRecordIdList = new List<int>(); //升级提示表id
        //public Dictionary<int,int>  LevelUpOldAttr = new Dictionary<int, int>();   //升级前属性值
        //public Dictionary<int, int> LevelUpNewAttr = new Dictionary<int, int>();  //升级后属性值
        //private Coroutine AttrDelayCoroutine = null;   //升级前后的延迟
        //private int CharacterOldLevel = -1;      //记载旧等级，主要考虑第一次同步等级不提示。

        private void InitLevelUpTable()
        {
            Table.ForeachLevelupTips(table =>
            {
                RankImproveRecordIdList.Add(table.Id);
                return true;
            });
        }

        #endregion
    }
}