/********************************************************************************* 

                         Scorpion



  *FileName:CachotOutcomeFrameCtrler

  *Version:1.0

  *Date:2017-06-16

  *Description:

**********************************************************************************/  
#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataContract;
using DataTable;
using EventSystem;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class CachotOutcomeFrameCtrler : IControllerBase
    {

        #region 静态变量

        #endregion

        #region 成员变量

        private DungeonResultDataModel DataModel;
        private int m_iDrawId;
        private int m_iDrawIndex;
        private int modelId = -1;
        private int StarNum = 0;
        #endregion
    
        #region 构造函数

        public CachotOutcomeFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(DungeonResultChoose.EVENT_TYPE, OnCachotResultChoiceEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_CityDungeonResult.EVENT_TYPE, OnCitiesCachotResultEvent);
            EventDispatcher.Instance.AddEventListener(ShowDungeonResult.EVENT_TYPE, OnShowResult);
            EventDispatcher.Instance.AddEventListener(UIEvent_NewCityDungeonResult.EVENT_TYPE, UIEvent_NewCityDungeon);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            DataModel = new DungeonResultDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as DungeonResultArguments;
            if (_args == null)
            {
                return;
            }

            var _seconds = _args.Second;
            if (_seconds <= GameUtils.FubenStar3Time * 60)
            {
                DataModel.Start = 3;
            }
            else if (_seconds <= GameUtils.FubenStar2Time * 60)
            {
                DataModel.Start = 2;
            }
            else
            {
                DataModel.Start = 1;
            }
            DataModel.Type = 0;
            StarNum = DataModel.Start;
            DataModel.FubenId = _args.FubenId;
            DataModel.FinishTime = string.Format(GameUtils.GetDictionaryText(210404), _seconds / 60, _seconds % 60);
            m_iDrawId = _args.DrawId;
            m_iDrawIndex = _args.DrawIndex;

            modelId = -1;
            if (_args.EraId >= 0)
            {
                DataModel.EraId = _args.EraId;
                DataModel.RoleId = PlayerDataManager.Instance.GetRoleId();
                var tbMaya = Table.GetMayaBase(DataModel.EraId);
                if (tbMaya != null)
                {
                    //modelId = tbMaya.ModelId;
                    RefreshModel(modelId);
                    if (tbMaya.SkillIds.Count > 0 && tbMaya.SkillIds[0]>0)
                    {
                        DataModel.EraSkillId = tbMaya.SkillIds[DataModel.RoleId];
                    }
                    else
                    {
                        DataModel.EraSkillId = -1;
                    }
                }

                NetManager.Instance.StartCoroutine(ShowResultCoroutine(8));
            }
            else
            {
                DataModel.EraId = -1;
            }

            var _itemBase = _args.ItemBase;
            if( m_iDrawIndex < 0 || m_iDrawIndex >= DataModel.AwardItems.Count)
                return;
            if (_itemBase == null )
            {
                _itemBase = new ItemBaseData();
                var _tbDraw = Table.GetDraw(m_iDrawId);
                if (m_iDrawIndex < 0 || m_iDrawIndex >= _tbDraw.DropItem.Length)
                {
                    return;
                }
                _itemBase.ItemId = _tbDraw.DropItem[m_iDrawIndex];
                _itemBase.Count = _tbDraw.Count[m_iDrawIndex];
            }
            {
                DataModel.NormalDungeon.Rewards.Clear();
                {
                    var _item = new ItemIdDataModel();
                    _item.ItemId = _itemBase.ItemId;
                    _item.Count =_itemBase.Count;                
                    DataModel.NormalDungeon.Rewards.Add(_item);
                }
                var _tbFuben = Table.GetFuben(DataModel.FubenId);
                if (_tbFuben != null)
                {
                    if (_tbFuben.RewardCount[0]>0)
                    {
                        var _item = new ItemIdDataModel();
                        _item.ItemId = _tbFuben.RewardId[0];
                        _item.Count = _tbFuben.RewardCount[0];
                        DataModel.NormalDungeon.Rewards.Add(_item);
                    }
                    if (_tbFuben.RewardCount[1]>0)
                    {
                        var _item = new ItemIdDataModel();
                        _item.ItemId = _tbFuben.RewardId[1];
                        _item.Count = _tbFuben.RewardCount[1];
                        DataModel.NormalDungeon.Rewards.Add(_item);
                    }
                }


            }
            //DataModel.AwardItems[m_iDrawIndex].ItemId = _itemBase.ItemId;
            //DataModel.AwardItems[m_iDrawIndex].Count = _itemBase.Count;
            //DataModel.AwardItems[m_iDrawIndex].Exdata.InstallData(_itemBase.Exdata);
        }

        private void RefreshModel(int modelId)
        {
            if (modelId < 0)
                return;

            var e1 = new FubenModelRefreshEvent(modelId, 0);
            EventDispatcher.Instance.DispatchEvent(e1);
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

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "GetStarNum")
            {
                return StarNum;
            }
            return null;
        }

        public void OnShow()
        {
            RefreshModel(modelId); 
        }

        public FrameState State { get; set; }

        #endregion

        #region 逻辑函数

        private IEnumerator ShowResultCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            DataModel.EraId = -1;
        }

        private int GetStarNum()
        {
            return StarNum;
        }

        #endregion

        #region 事件

        //家园副本结果
        private void OnCitiesCachotResultEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_CityDungeonResult;
            if (null == _e)
            {
                return;
            }

            var _paramList = _e.Param;

            //副本类型
            var _paramIdx = 0;
            var _tbFuben = Table.GetFuben(DataModel.FubenId);
            if (_tbFuben == null)
            {
                Logger.Error("In OnCityDungeonResult() tbFuben = null!");
                return;
            }

            var _myLevel = PlayerDataManager.Instance.GetLevel();
            var _assistType = (eDungeonAssistType)_tbFuben.AssistType;
            switch (_assistType)
            {
                case eDungeonAssistType.CityGoldSingle:
                {
                    DataModel.Type = 1;

                    var _Data = DataModel.GoldDungeon;

                    //击杀描述
                    var _kill = 0;
                    var _total = 0;
                    for (var i = 0; i < _Data.Desc.Count; i++)
                    {
                        if (_paramIdx < _paramList.Count)
                        {
                            _kill = _paramList[_paramIdx++];
                            _total = _paramList[_paramIdx++];
                        }
                        _kill = Math.Min(_kill, _total);
                        var _temp = string.Format("{0}/{1}", _kill, _total);
                        if (_kill >= _total)
                        {
                            _Data.Desc[i] = "[00FF00]" + _temp + "[-]";
                        }
                        else if (0 == _kill)
                        {
                            _Data.Desc[i] = "[FF0000]" + _temp + "[-]";
                        }
                        else
                        {
                            _Data.Desc[i] = "[FFFFFF]" + _temp + "[-]";
                        }
                    }

                    _Data.ExtraRewardPercent = 100 + _kill * 20;
                    var _rewardScale = 0.01f * _Data.ExtraRewardPercent;

                    //副本奖励
                    for (int i = 0, imax = _tbFuben.RewardId.Count; i < imax; ++i)
                    {
                        var _reward = _Data.Rewards[i];
                        var _itemId = _tbFuben.RewardId[i];
                        var _itemCount = _tbFuben.RewardCount[i];
                        _reward.ItemId = _itemId;
                        if (_itemId == -1)
                        {
                            continue;
                        }
                        _reward.Count = (int)(_itemCount * _rewardScale + 0.5f);
                    }
                }
                    break;
                case eDungeonAssistType.CityExpMulty:
                case eDungeonAssistType.CityExpSingle:
                {
                    DataModel.Type = 2;

                    var _rank = _paramList[_paramIdx++];
                    var _Data = DataModel.ExpDungeon;
                    _Data.Rank = _rank;
                    StarNum = _Data.Rank;
                    _Data.ShowMulty = _assistType == eDungeonAssistType.CityExpMulty;
                    if (_Data.ShowMulty)
                    {
                        _Data.LeaderName = _e.LeaderName;
                        var _leaderGainedReward = _paramList[_paramIdx++];
                        _Data.ShowMulty = _leaderGainedReward == 0;
                    }

                    //副本奖励
                    for (int i = 0, imax = _tbFuben.RewardId.Count; i < imax; ++i)
                    {
                        var _itemData = _Data.Rewards[i];
                        _itemData.ItemId = _tbFuben.RewardId[i];
                        _itemData.Count = GameUtils.GetRewardCount(_tbFuben, _tbFuben.RewardCount[i], _rank, _myLevel);
                    }

                    //队长奖励
                    if (_Data.ShowMulty)
                    {
                        var _itemData = _Data.LeaderRewards[0];
                        var _count = _tbFuben.ScanReward[0];
                        _count = GameUtils.GetRewardCount(_tbFuben, _count, _rank, _myLevel);
                        _itemData.ItemId = -1;
                        _itemData.Count = _count;

                        _count = _tbFuben.ScanReward[1];
                        _count = GameUtils.GetRewardCount(_tbFuben, _count, _rank, _myLevel);
                        _itemData = _Data.LeaderRewards[1];
                        _itemData.ItemId = _count > 0 ? (int)eResourcesType.CityWood : -1;
                        _itemData.Count = _count;
                    }
                }
                    break;
                case eDungeonAssistType.OrganRoom:
                {
                    DataModel.Type = 3;

                    var _Data = DataModel.GoldDungeon2;

                    _Data.Gold = _paramList[_paramIdx++];
                }
                    break;
                case eDungeonAssistType.FrozenThrone:
                {
                    DataModel.Type = 4;

                    var _rank = _paramList[_paramIdx++];
                    var _Data = DataModel.NormalDungeon;
                    _Data.Rank = _rank;
                    StarNum = _Data.Rank;
                    var _items = new List<ItemIdDataModel>();
                    for (int i = 0, imax = _tbFuben.RewardId.Count; i < imax; ++i)
                    {
                        var _itemId = _tbFuben.RewardId[i];
                        if (_itemId == -1)
                        {
                            break;
                        }
                        var _itemCount = _tbFuben.RewardCount[i];
                        _itemCount = GameUtils.GetRewardCount(_tbFuben, _itemCount, _rank, _myLevel);
                        var _item = new ItemIdDataModel();
                        _item.ItemId = _itemId;
                        _item.Count = _itemCount;
                        _items.Add(_item);
                    }
                    _Data.Rewards = new ObservableCollection<ItemIdDataModel>(_items);
                }
                    break;
                case eDungeonAssistType.CastleCraft1:
                case eDungeonAssistType.CastleCraft2:
                case eDungeonAssistType.CastleCraft3:
                case eDungeonAssistType.CastleCraft4:
                case eDungeonAssistType.CastleCraft5:
                case eDungeonAssistType.CastleCraft6:
                {
                    DataModel.Type = 5;

                    var _rank = _paramList[_paramIdx++];
                    var _score = _paramList[_paramIdx++];
                    var _Data = DataModel.CastleCraft;
                    DataModel.Rank = _rank;
                    _Data.Score = _score;

                    //计算奖励
                    var _isDynamicReward = _tbFuben.IsDynamicExp == 1;
                    var _items = new Dictionary<int, int>();
                    //基础奖励
                    for (int i = 0, imax = _tbFuben.RewardId.Count; i < imax; ++i)
                    {
                        var _itemId = _tbFuben.RewardId[i];
                        if (_itemId == -1)
                        {
                            break;
                        }
                        var _itemCount = _tbFuben.RewardCount[i];
                        _items.modifyValue(_itemId, _itemCount);
                    }

                    //额外经验
                    var _exp = 0;
                    if (_isDynamicReward)
                    {
                        _exp = (int)(1.0 * _tbFuben.DynamicExpRatio * Table.GetLevelData(_myLevel).DynamicExp / 10000 * _score);
                    }
                    if (_exp > 0)
                    {
                        _items.modifyValue((int)eResourcesType.ExpRes, _exp);
                    }

                    //额外荣誉
                    var _honor = _tbFuben.ScanReward[0];
                    if (_isDynamicReward)
                    {
                        _honor = Table.GetSkillUpgrading(_honor).GetSkillUpgradingValue(_rank);
                    }
                    if (_honor > 0)
                    {
                        _items.modifyValue((int)eResourcesType.Honor, _honor);
                    }

                    if (_items.ContainsKey((int)eResourcesType.ExpRes))
                    {
                        _Data.Exp = _items[(int)eResourcesType.ExpRes];
                    }
                    if (_items.ContainsKey((int)eResourcesType.GoldRes))
                    {
                        _Data.Gold = _items[(int)eResourcesType.GoldRes];
                    }
                    if (_items.ContainsKey((int)eResourcesType.Honor))
                    {
                        _Data.Honor = _items[(int)eResourcesType.Honor];
                    }
                }
                    break;
                case eDungeonAssistType.AllianceWar:
                {
                    DataModel.Type = 6;
                    var _Data = DataModel.AttackCity;
                    if (_paramList.Count > 0)
                    {
                        var _sucess = false;
                        var _param = _paramList[0];
                        var _instance = PlayerDataManager.Instance;
                        if (_instance._battleCityDic.ContainsKey(_param))
                        {
                            DataModel.AttackCity.BattleName = _instance._battleCityDic[_param].Name +
                                                              GameUtils.GetDictionaryText(270272);
                        }
                        else
                        {
                            DataModel.AttackCity.BattleName = GameUtils.GetDictionaryText(270291);
                        }
                        if (_instance.BattleUnionDataModel.MyUnion.UnionID == _param)
                        {
                            _Data.ResultType = 1;
                        }
                        else
                        {
                            _Data.ResultType = 0;
                        }
                    }
                    var _items = new List<ItemIdDataModel>();
                    for (int i = 0, imax = _tbFuben.RewardId.Count; i < imax; ++i)
                    {
                        var _itemId = _tbFuben.RewardId[i];
                        if (_itemId == -1)
                        {
                            break;
                        }
                        var _item = new ItemIdDataModel();
                        _item.ItemId = _itemId;
                        _item.Count = _tbFuben.RewardCount[i];
                        _items.Add(_item);
                    }
                    _Data.Rewards = new ObservableCollection<ItemIdDataModel>(_items);
                }
                    break;
                case eDungeonAssistType.MieShiWar:
                {
                    DataModel.FightResult = _paramList[2];
                }
                    break;
            }
        }

        private void OnShowResult(IEvent ievent)
        {
            DataModel.EraId = -1;
        }

        private void OnCachotResultChoiceEvent(IEvent ievent)
        {
            var _e = ievent as DungeonResultChoose;

            var _choose = _e.Index;
            var _tbDraw = Table.GetDraw(m_iDrawId);

            if (_choose < 0 || _choose >= DataModel.AwardItems.Count)
            {
                return;
            }
            if (m_iDrawIndex < 0 || m_iDrawIndex >= _tbDraw.DropItem.Length)
            {
                return;
            }
            DataModel.AwardItems[_choose].ItemId = _tbDraw.DropItem[m_iDrawIndex];
            DataModel.AwardItems[_choose].Count = _tbDraw.Count[m_iDrawIndex];


            var _flag1 = 0;
            var _flag2 = 0;
            for (var i = 0; i < 3; i++)
            {
                if (_flag1 == _choose)
                {
                    _flag1++;
                }
                if (_flag2 == m_iDrawIndex)
                {
                    _flag2++;
                }
                var _itemId = _tbDraw.DropItem[_flag2];
                DataModel.AwardItems[_flag1].ItemId = _itemId;
                DataModel.AwardItems[_flag1].Count = _tbDraw.Count[_flag2];
                var _tbItem = Table.GetItemBase(_itemId);
                if (_tbItem.Type >= 10000 && _tbItem.Type <= 10099)
                {
                    GameUtils.EquipRandomAttribute(DataModel.AwardItems[_flag1]);
                }
                _flag1++;
                _flag2++;
            }
        }

        void UIEvent_NewCityDungeon(IEvent ievent)
        {
            var _e = ievent as UIEvent_NewCityDungeonResult;
            if (null == _e)
            {
                return;
            }
            DataModel.EraId = -1;
            DataModel.FubenId = _e.fubenId;
            DataModel.CompleteType = _e.completeType;
            DataModel.FinishTime = string.Format(GameUtils.GetDictionaryText(210404), _e.seconds / 60, _e.seconds % 60);
            DataModel.TotalExp = (int)_e.exp;

            if (_e.fubenId == 6100)
            {
                DataModel.Type = 7;

                var _paramIdx = 0;
                var _tbFuben = Table.GetFuben(DataModel.FubenId);
                if (_tbFuben == null)
                {
                    Logger.Error("In OnCityDungeonResult() tbFuben = null!");
                    return;
                }

                var _paramList = _e.param;
                var _myLevel = PlayerDataManager.Instance.GetLevel();
                var _rank = _paramList[0];
                var _Data = DataModel.NormalDungeon;
                _Data.Rank = _rank;
                StarNum = _Data.Rank;
                var _items = new List<ItemIdDataModel>();
                for (int i = 0, imax = _tbFuben.RewardId.Count; i < imax; ++i)
                {
                    var _itemId = _tbFuben.RewardId[i];
                    if (_itemId == -1)
                    {
                        break;
                    }
                    var _itemCount = _tbFuben.RewardCount[i];
                    _itemCount = GameUtils.GetRewardCount(_tbFuben, _itemCount, _rank, _myLevel);
                    var _item = new ItemIdDataModel();
                    _item.ItemId = _itemId;
                    _item.Count = _itemCount;
                    _items.Add(_item);
                }
                _Data.Rewards = new ObservableCollection<ItemIdDataModel>(_items);
            }
            else
                DataModel.Type = 8;
        }
        #endregion






    }
}