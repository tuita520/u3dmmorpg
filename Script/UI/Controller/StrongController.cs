/********************************************************************************* 

                         Scorpion



  *FileName:StrongFrameCtrler

  *Version:1.0

  *Date:2017-07-14

  *Description:

**********************************************************************************/
#region using

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;
using Shared;

#endregion

namespace ScriptController
{
    public class StrongFrameCtrler : IControllerBase
    {
        #region 构造函数
        public StrongFrameCtrler()
        {
            EventDispatcher.Instance.AddEventListener(StrongCellClickedEvent.EVENT_TYPE, OnItemClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_NewStrongOperation.EVENT_TYPE, NewStrongOperation);

            CleanUp();
        }

        private void NewStrongOperation(IEvent ievent)
        {
            var evt = ievent as UIEvent_NewStrongOperation;
            if (evt != null && evt.operation == 100)
            {
                OnShow();
            }
        }

        #endregion

        #region 成员变量
        private StrongDataModel DataModel;
        private bool FirstRun = true;
        private readonly Dictionary<int, int> IdToSort = new Dictionary<int, int>();
        private int SelectIndex = -1;
        private enum eStrongType
        {
            Vip = 1, //VIP等级
            EquipEnhance = 2, //装备强化
            EquipRefine = 3, //装备精炼
            EquipLevel = 4, //装备等阶
            ElfLevel = 5, //灵兽总等级
            WingAdvance = 6, //翅膀进阶阶数
            WingTrain = 7, //翅膀培养重数
            SkillLevel = 8, //技能等级
            TalentUse = 9, //天赋使用点
            MonsterBounty = 10, //怪物悬赏数量
            HandbookCount = 11, //图鉴收集数量
            RankLevel = 12, //军衔等级
            StatueCount = 13, //守护神像数量
            SailingQuality = 14, //航海船饰品质
            Count = 15
        }
        #endregion

        #region 事件
        private void OnItemClickEvent(IEvent ievent)
        {
            var _e = ievent as StrongCellClickedEvent;
            var _id = DataModel.Lists[_e.Index].Id;
            var _tbStrongType = Table.GetStrongType(_id);
            SelectIndex = _e.Index;
            EventDispatcher.Instance.DispatchEvent(new UIEvent_StrongSetGridLookIndex(1, SelectIndex));
            GameUtils.GotoUiTab(_tbStrongType.UiId, _tbStrongType.Tab);
        }
        #endregion

        #region 逻辑函数
        private void FirstShowRank(List<StrongItemDataModel> lists)
        {
            lists.Sort((a, b) =>
            {
                if (a.IsOpen < b.IsOpen)
                {
                    return 1;
                }
                if (a.IsOpen == b.IsOpen)
                {
                    if (a.State < b.State)
                    {
                        return -1;
                    }
                    if (a.State == b.State)
                    {
                        if (a.Sort < b.Sort)
                        {
                            return -1;
                        }
                        return 1;
                    }
                    return 1;
                }
                return -1;
            });
        }

        private List<int> GainCount(eStrongType type, List<int> param2)
        {
            var _result = new List<int> { 0, 0, 0 };
            var _resultCount = _result.Count;
            switch (type)
            {
                case eStrongType.Vip:
                {
                    var _level = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
                    _result[0] = _level;
                    _result[1] = _level;
                    _result[2] = _level;
                }
                    break;
                case eStrongType.EquipEnhance:
                {
                    var _totalCount = 0;
                    PlayerDataManager.Instance.ForeachEquip(item =>
                    {
                        var _exchance = item.Exdata.Enchance;
                        if (_exchance >= 0)
                        {
                            _totalCount += _exchance;
                        }
                    });
                    _result[0] = _totalCount;
                    _result[1] = _totalCount;
                    _result[2] = _totalCount;
                }
                    break;
                case eStrongType.EquipRefine:
                {
                    float totalCount = 0;
                    var _count = 0;
                    PlayerDataManager.Instance.ForeachEquip(item =>
                    {
                        if (item.ItemId == -1)
                        {
                            return;
                        }
                        var _tbEquipBase = Table.GetEquipBase(item.ItemId);
                        if (_tbEquipBase == null)
                        {
                            return;
                        }
                        if (_tbEquipBase.AddAttrMaxValue <= 0)
                            return;
                        var _append = (float)item.Exdata.Append / _tbEquipBase.AddAttrMaxValue * 100;
                        _append = _append > 100 ? 100 : _append;
                        _count++;
                        totalCount += _append;
                    });
                    var _avg = 0;
                    if (_count != 0)
                    {
                        _avg = (int)totalCount / _count;
                    }
                    _result[0] = _avg;
                    _result[1] = _avg;
                    _result[2] = _avg;
                }
                    break;
                case eStrongType.EquipLevel:
                {
                    var _levelCount = 0;
                    PlayerDataManager.Instance.ForeachEquip(item =>
                    {
                        if (item.ItemId == -1)
                        {
                            return;
                        }
                        var _tbEquipBase = Table.GetEquipBase(item.ItemId);
                        if (_tbEquipBase == null)
                        {
                            return;
                        }
                        _levelCount += _tbEquipBase.Ladder;
                    });
                    _result[0] = _levelCount;
                    _result[1] = _levelCount;
                    _result[2] = _levelCount;
                }
                    break;
                case eStrongType.ElfLevel:
                {
                    var _elfController = UIManager.Instance.GetController(UIConfig.ElfUI);
                    var _elfData = _elfController.GetDataModel("") as ElfDataModel;
                    var _totalLevel = 0;
                    foreach (var item in _elfData.ElfList)
                    {
                        if (item.ItemId > 0 && (item.State == 1 || item.State == 2))//state  1 出战 2 展示
                        {
                            _totalLevel += item.Exdata.Level;
                        }
                    }
                    _result[0] = _totalLevel;
                    _result[1] = _totalLevel;
                    _result[2] = _totalLevel;
                }
                    break;
                case eStrongType.WingAdvance:
                {
                    var _level = PlayerDataManager.Instance.GetExData(eExdataDefine.e308);
                    _result[0] = _level;
                    _result[1] = _level;
                    _result[2] = _level;
                }
                    break;
                case eStrongType.WingTrain:
                {
                    var _trainCount = 0;
                    var _wingController = UIManager.Instance.GetController(UIConfig.WingUI);
                    var _wingData = _wingController.GetDataModel("") as WingDataModel;
                    var _trainId = _wingData.ItemData.ExtraData[1];
                    _trainCount=Table.GetWingTrain(_trainId).TrainCount;
                    //var _extraData = _wingData.ItemData.ExtraData;
                    
                    //var _count = System.Math.Min(_extraData.Count, (int)eWingExDefine.eGrowMax + 1);
                    //var _count = 1;//现在只用第一个扩展计数座位当前翅膀段数
                    //for (var i = 0; i < _count; i++)
                    //{
                    //    if (i % 2 == 1)
                    //    {
                    //        var _tbTrain = Table.GetWingTrain(_extraData[i]);
                    //        _trainCount += _tbTrain.TrainCount;
                    //    }
                    //}
                    _result[0] = _trainCount;
                    _result[1] = _trainCount;
                    _result[2] = _trainCount;
                }
                    break;
                case eStrongType.SkillLevel:
                {
                    var _skillCount = 0;
                    var _skillData = PlayerDataManager.Instance.PlayerDataModel.SkillData.OtherSkills;
                    foreach (var item in _skillData)
                    {
                        _skillCount += item.SkillLv;
                    }
                    _result[0] = _skillCount;
                    _result[1] = _skillCount;
                    _result[2] = _skillCount;
                }
                    break;
                case eStrongType.TalentUse:
                {
                    var _count = GameUtils.GetAllSkillTalentCount();
                    _result[0] = _count;
                    _result[1] = _count;
                    _result[2] = _count;
                }
                    break;
                case eStrongType.MonsterBounty:
                {
                    var _count = PlayerDataManager.Instance.TotalBountyCount;
                    _result[0] = _count;
                    _result[1] = _count;
                    _result[2] = _count;
                }
                    break;
                case eStrongType.HandbookCount:
                {
                    var _count = PlayerDataManager.Instance.TotalGroupCount;
                    _result[0] = _count;
                    _result[1] = _count;
                    _result[2] = _count;
                }
                    break;
                case eStrongType.RankLevel:
                {
                    var _count = PlayerDataManager.Instance.GetExData(eExdataDefine.e250);
                    _result[0] = _count;
                    _result[1] = _count;
                    _result[2] = _count;
                }
                    break;
                case eStrongType.StatueCount:
                {
                    var _statueController = UIManager.Instance.GetController(UIConfig.AreanaUI);
                    var _statueData = _statueController.GetDataModel("Statue") as StatueDataModel;
                    var _totalLevel = 0;
                    foreach (var item in _statueData.StatueInfos)
                    {
                        if (item.IsOpen)
                        {
                            var _tbStatue = Table.GetStatue(item.DataIndex);
                            _totalLevel += _tbStatue.Level;
                        }
                    }
                    _result[0] = _totalLevel;
                    _result[1] = _totalLevel;
                    _result[2] = _totalLevel;
                }
                    break;
                case eStrongType.SailingQuality:
                {
                    var _sailController = UIManager.Instance.GetController(UIConfig.SailingUI);
                    var _sailData = _sailController.GetDataModel("") as SailingDataModel;
                    var _totalLevel = 0;
                    foreach (var item in _sailData.ShipEquip.EquipItem)
                    {
                        if (item.BaseItemId > 0)
                        {
                            _totalLevel += item.nLevel;
                        }
                    }
                    _result[0] = _totalLevel;
                    _result[1] = _totalLevel;
                    _result[2] = _totalLevel;
                }
                    break;
            }
            return _result;
        }

        private void ShowNotFirstRank(List<StrongItemDataModel> lists)
        {
            lists.Sort((a, b) =>
            {
                if (a.FirstSort < b.FirstSort)
                {
                    return -1;
                }
                return 1;
            });
        }
        #endregion

        #region 固有函数
        public void RefreshData(UIInitArguments data)
        {
        }

        public void CleanUp()
        {
            DataModel = new StrongDataModel();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnShow()
        {

            var tbFilter = Table.GetClientConfig(1211);
            var filterStr = tbFilter.Value.Split('|');
            var filterList = new List<int>();
            for (int i = 0; i < filterStr.Length; i++)
            {
                filterList.Add(filterStr[i].ToInt());
            }
            var _level = PlayerDataManager.Instance.GetLevel();
            var tbStrongData = Table.GetStrongData(_level);
            if (tbStrongData == null)
            {
                return;
            }

            DataModel.SuggestForce = tbStrongData.SujectForce;
            var _nowForce = PlayerDataManager.Instance.PlayerDataModel.Attributes.FightValue;
            var _barValue = (float)_nowForce / DataModel.SuggestForce;
            _barValue = ((_barValue < 0.3f ? 0.3f : _barValue) > 1 ? 1 : _barValue);
            DataModel.BarValue = _barValue;
            var _count = tbStrongData.TypeId.Length;
            var _param1List = new List<int> { -1, -1, -1 };
            var _param2List = new List<int> { -1, -1, -1 };
            var _lists = new List<StrongItemDataModel>();
            var _sortCount = 0;
            for (var i = 0; i < _count; i++)
            {
                if (i == (int)eStrongType.Count - 1)
                {
                    break;
                }
                var _strongItem = new StrongItemDataModel();
                var typeId = tbStrongData.TypeId[i];
                if (filterList.Contains(typeId))
                {
                    continue;
                }
                var _tbStrongType = Table.GetStrongType(typeId);
                if (_tbStrongType == null)
                {
                    continue;
                }

                if (_tbStrongType.Sort == -1)
                {
                    continue;
                }
                _param1List[0] = tbStrongData.Param[i, 0];
                _param1List[1] = tbStrongData.Param[i, 2];
                _param1List[2] = tbStrongData.Param[i, 4];
                _param2List[0] = tbStrongData.Param[i, 1];
                _param2List[1] = tbStrongData.Param[i, 3];
                _param2List[2] = tbStrongData.Param[i, 5];
                var _varList = GainCount((eStrongType)tbStrongData.TypeId[i], _param2List);
                var _state = 3;
                for (var j = 0; j < 3; j++)
                {
                    if (_param1List[j] > _varList[j])
                    {
                        _state = j;
                        break;
                    }
                }
                _strongItem.State = _state;
                _strongItem.Id = _tbStrongType.Id;
                if (IdToSort.ContainsKey(_tbStrongType.Id))
                {
                    _strongItem.FirstSort = IdToSort[_tbStrongType.Id];
                }
                _strongItem.Sort = _tbStrongType.Sort;
                if (!GameSetting.Instance.IgnoreButtonCondition)
                {
                    if (_tbStrongType.ConditionId == -1)
                    {
                        _strongItem.IsOpen = 1;
                    }
                    else
                    {
                        _strongItem.IsOpen = PlayerDataManager.Instance.CheckCondition(_tbStrongType.ConditionId) == 0 ? 1 : 0;
                    }
                }
                else
                {
                    _strongItem.IsOpen = 1;
                }

                if (_strongItem.IsOpen == 0)
                {
                    var _tbCondition = Table.GetConditionTable(_tbStrongType.ConditionId);
                    _strongItem.OpenStr = GameUtils.GetDictionaryText(_tbCondition.FlagTrueDict);
                }

                //if (tbStrongType.Param[1] == -1)
                //{
                if (_state == 0)
                {
                    _strongItem.NowStateStr = string.Format(_tbStrongType.ShowStr, _varList[_state]);
                    _strongItem.WillStateStr = string.Format(_tbStrongType.ShowStr, _param1List[_state]);
                }
                else if (_state >= 2)
                {
                    _strongItem.NowStateStr = string.Format(_tbStrongType.ShowStr, _varList[2]);
                    _strongItem.WillStateStr = string.Format(_tbStrongType.ShowStr, _param1List[2]);
                }
                else
                {
                    _strongItem.NowStateStr = string.Format(_tbStrongType.ShowStr, _varList[_state]);
                    _strongItem.WillStateStr = string.Format(_tbStrongType.ShowStr, _param1List[_state + 1]);
                }
                //}
                //else
                //{
                //    if (state > 2)
                //    {
                //        strongItem.NowStateStr = string.Format(tbStrongType.ShowStr, varList[2], param2List[2]);
                //        strongItem.WillStateStr = string.Format(tbStrongType.ShowStr, param1List[1], param2List[1]);
                //    }
                //    else
                //    {
                //        strongItem.NowStateStr = string.Format(tbStrongType.ShowStr, varList[state], param2List[state]);
                //        strongItem.WillStateStr = string.Format(tbStrongType.ShowStr, param1List[1], param2List[1]);
                //    }

                //}
                _lists.Add(_strongItem);
            }

            if (FirstRun)
            {
                FirstShowRank(_lists);
                var _mCount = _lists.Count;
                for (var i = 0; i < _mCount; i++)
                {
                    IdToSort.Add(_lists[i].Id, i);
                }
                FirstRun = false;
            }
            else
            {
                ShowNotFirstRank(_lists);
            }
            DataModel.Lists = new ObservableCollection<StrongItemDataModel>(_lists);

            EventDispatcher.Instance.DispatchEvent(new UIEvent_StrongSetGridLookIndex(0, SelectIndex));
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

    }
}