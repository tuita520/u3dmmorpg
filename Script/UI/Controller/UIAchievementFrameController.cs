/********************************************************************************* 

                         Scorpion



  *FileName:UIAccomplishmentFrameCtrler

  *Version:1.0

  *Date:2017-07-012

  *Description:

**********************************************************************************/  
#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class UIAccomplishmentFrameCtrler : IControllerBase
    {

        #region 静态变量

        //总成就积分扩展数据索引id
        private const int s_iTOTAL_SCORE_DATA_IDX = 50;
        private const int eraAchievementType = 10;

        #endregion

        #region 成员变量

        //缓存按类型划分成就
        private readonly Dictionary<int, List<AchievementRecord>> m_dicAnalyzeTable =
            new Dictionary<int, List<AchievementRecord>>();

        private int mCurrentPage = -1;
        //缓存扩展计数影响哪些成就
        private readonly Dictionary<int, List<AchievementRecord>> m_dicExtDataDic =
            new Dictionary<int, List<AchievementRecord>>();

        //缓存标记位影响哪些成就
        private readonly Dictionary<int, List<AchievementRecord>> m_dicFlagDataDic =
            new Dictionary<int, List<AchievementRecord>>();

        private bool m_bInit;
        //缓存等级影响哪些成就
        private readonly Dictionary<int, List<AchievementRecord>> m_dicLevelDic = new Dictionary<int, List<AchievementRecord>>();
        private int mTotalAchievement;
        private FrameState m_State;
        private AchievementDataModel m_DataModel { get; set; }

        #endregion

        #region 构造函数

        public UIAccomplishmentFrameCtrler()
        {
            CleanUp();


            EventDispatcher.Instance.AddEventListener(Enter_Scene_Event.EVENT_TYPE, evn => { Initial(); });

            EventDispatcher.Instance.AddEventListener(Event_ShowAchievementPage.EVENT_TYPE, OnDisplayAccomplishmentPageEvent);


            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagRefeshEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpgradeDataEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnUpLVEvent);

            EventDispatcher.Instance.AddEventListener(UI_EventApplyChengJiuItem.EVENT_TYPE, OnClaimAwardEvent);
        }

        #endregion

        #region 固有函数

        public void Close()
        {
            m_DataModel.SubPage = false;
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            mCurrentPage = -1;
            {
                // foreach(var item in DataModel.Catalog)
                var _enumerator1 = (m_DataModel.Catalog).GetEnumerator();
                while (_enumerator1.MoveNext())
                {
                    var item = _enumerator1.Current;
                    {
                        item.Checked = false;
                    }
                }
            }
            m_DataModel.Catalog[0].Checked = true;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public void CleanUp()
        {
            m_DataModel = new AchievementDataModel();
            InitialTableData();
            m_bInit = false;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            throw new NotImplementedException(name);
        }

        public void OnShow()
        {
            unShowCatalog();
            m_DataModel.SubPage = false;
            if (m_DataModel.Catalog.Count>0)
                m_DataModel.Catalog[0].Checked = true;
        }

        public FrameState State
        {
            get { return m_State; }
            set { m_State = value; }
        }

        #endregion

        #region 逻辑函数


        //扩展数据
        private List<int> ExtData
        {
            get { return PlayerDataManager.Instance.ExtData; }
        }

        //标记位
        private BitFlag FlagData
        {
            get { return PlayerDataManager.Instance.FlagData; }
        }

        //填充一个成就数据到数据源
        private void FillAccomplishment(AchievementRecord table, AchievementItemDataModel achievement)
        {
            achievement.Id = table.Id;
            achievement.Title = table.Name;
            if (-1 != table.Exdata)
            {
                var _progress = Mathf.Min(PlayerDataManager.Instance.GetAccomplishmentProgress(achievement.Id), table.ExdataCount);
                achievement.Progress = _progress * 1.0f / table.ExdataCount;
                achievement.ProgressLabel = string.Format("{0}/{1}", GameUtils.GetBigValueStr(_progress),
                    GameUtils.GetBigValueStr(table.ExdataCount));

                if (GameUtils.GetBigValueStr(table.ExdataCount) == "1")            
                    achievement.ShowPorgress = false;            
                else
                    achievement.ShowPorgress = true;
            }
            else
            {
                achievement.ShowPorgress = false;
            }

            var _state = PlayerDataManager.Instance.GetAccomplishmentStatus(achievement.Id);
            achievement.State = (int)_state;
            /*
		if (eRewardState.HasGot == state)
		{
			achievement.State = GameUtils.GetDictionaryText(1035);
			achievement.CanGetReward = false;
		}
		else if (eRewardState.CanGet == state)
		{
			achievement.State = GameUtils.GetDictionaryText(1036);
			achievement.CanGetReward = true;
		}
		else if (eRewardState.CannotGet == state)
		{
			achievement.State = GameUtils.GetDictionaryText(1037);
			achievement.CanGetReward = false;
		}
		*/
            var _tableItemIdLength0 = table.ItemId.Length;
            for (var i = 0; i < _tableItemIdLength0; i++)
            {
                var _itemId = table.ItemId[i];
                achievement.Rewards[i].ItemId = _itemId;
                achievement.Rewards[i].Count = table.ItemCount[i];
            }
        }

        private void GetAccomplishmentRewards(int id)
        {
            if (eRewardState.CanGet != PlayerDataManager.Instance.GetAccomplishmentStatus(id))
            {
                return;
            }
            GameLogic.Instance.StartCoroutine(GetAccomplishmentRewardsCoroutine(id));
        }

        private IEnumerator GetAccomplishmentRewardsCoroutine(int id)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Debug(".............ClaimAchievementRewardCoroutine..................begin");
                var _msg = NetManager.Instance.RewardAchievement(id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State != MessageState.Reply)
                {
                    Logger.Debug("[ClaimAchievementRewardCoroutine] msg.State != MessageState.Reply");
                    yield break;
                }

                if (_msg.ErrorCode != (int)ErrorCodes.OK)
                {
                    Logger.Debug("[ClaimAchievementRewardCoroutine] ErrorCodes=[{0}]", _msg.ErrorCode);

                    if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                    yield break;
                }
                //如果成就领取成功，就把标记位设置一下，防止连续领取
                var _table = Table.GetAchievement(id);
                if (null != _table)
                {
                    FlagData.SetFlag(_table.RewardFlagId);
                    OnFlagChange(_table.RewardFlagId);
                }

                const int flagId = 492;
                //成就奖励领取成功清除一个标记
                if (!PlayerDataManager.Instance.GetFlag(flagId))
                {
                    var _list = new Int32Array();
                    _list.Items.Add(flagId);
                    PlayerDataManager.Instance.SetFlagNet(_list);
                }
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(452));

                Logger.Debug(".............ClaimAchievementRewardCoroutine..................end");
            }
        }

        //获得扩展数据值
        private int GetExtData(int idx)
        {
            if (idx >= 0 && idx < ExtData.Count)
            {
                return ExtData[idx];
            }
            return 0;
        }

        //获得总成就积分
        private int GetAmountIntegral()
        {
            return GetExtData(s_iTOTAL_SCORE_DATA_IDX);
        }

        //初始化
        private void Initial()
        {
            if (m_bInit)
            {
                return;
            }

            m_DataModel.TotalScore = GetAmountIntegral();
            Table.ForeachAchievement(table =>
            {
                //-1类型的是大类，不是成就数据
                if (-1 == table.Type)
                {
                    return true;
                }
                if (table.Type == eraAchievementType)
                {
                    return true;
                }

                if (table.Type < 0 || table.Type >= m_DataModel.Summary.Count)
                {
                    Logger.Error("table.Type[{0}] out of range [{1}]", table.Type, table.Name);
                    return true;
                }
                var _summary = m_DataModel.Summary[table.Type];

                if (eRewardState.CannotGet != PlayerDataManager.Instance.GetAccomplishmentStatus(table.Id))
                {
                    _summary.CompletedNum++;
                }
                if (eRewardState.CanGet == PlayerDataManager.Instance.GetAccomplishmentStatus(table.Id))
                {
                    //计算小红点个数
                    var _idx = table.Type + 1;
                    if (_idx >= 0 && _idx < m_DataModel.Catalog.Count)
                    {
                        m_DataModel.Catalog[_idx].Count++;
                    }
                }
                return true;
            });
            {
                // foreach(var summary in DataModel.Summary)
                var _enumerator2 = (m_DataModel.Summary).GetEnumerator();
                while (_enumerator2.MoveNext())
                {
                    var _summary = _enumerator2.Current;
                    {
                        _summary.Progress = 0 == _summary.TotalNum ? 0 : _summary.CompletedNum * 1.0f / _summary.TotalNum;
                        _summary.ProgressLabel = GameUtils.GetBigValueStr(_summary.CompletedNum) + "/" +
                                                 GameUtils.GetBigValueStr(_summary.TotalNum);
                        var _percent = (int)Math.Ceiling(_summary.Progress * 100);
                        _summary.ProgressString = _percent + "%";
                    }
                }
            }

            RenewalNotice();
            RefeshIntergral();
            m_DataModel.Catalog[0].Checked = true;

            m_bInit = true;
        }

        //初始化表格数据
        private void InitialTableData()
        {
            //先加入总览按钮
            {
                var _newCatalog = new AchievementSummaryBtnDataModel();
                _newCatalog.Title = GameUtils.GetDictionaryText(300834);
                _newCatalog.TypeId = -1;
                m_DataModel.Catalog.Add(_newCatalog);
            }

            //遍历表格分类
            var showDict = new Dictionary<int, AchievementSummaryItemDataModel>();
            Table.ForeachAchievement(table =>
            {
                if (table.ViewLevel >= 999)
                    return true;
                //-1类型的是大类，不是成就数据
                if (-1 == table.Type)
                {
                    if (table.Id == eraAchievementType)
                        return true;

                    //左侧分类按钮列表
                    var _newCatalog = new AchievementSummaryBtnDataModel();
                    _newCatalog.Title = table.Name;
                    _newCatalog.TypeId = table.Id;
                    m_DataModel.Catalog.Add(_newCatalog);

                    //成就大类列表
                    var _newSummary = new AchievementSummaryItemDataModel();
                    _newSummary.Title = table.Name;
                    _newSummary.TypeId = table.Id;
                    _newSummary.IconId = table.Icon;
                    m_DataModel.Summary.Add(_newSummary);

                    showDict[_newSummary.TypeId] = _newSummary;

                    return true;
                }

                //该成就的分类
                AchievementSummaryItemDataModel _summary;
                if (!showDict.TryGetValue(table.Type, out _summary))
                {
                    return true;
                }
                //var _summary = m_DataModel.Summary[table.Type];
                _summary.TotalNum++;


                {
                    //分析表格，根据类型缓存表格数据
                    List<AchievementRecord> _list = null;
                    if (!m_dicAnalyzeTable.TryGetValue(table.Type, out _list))
                    {
                        _list = new List<AchievementRecord>();
                        m_dicAnalyzeTable.Add(table.Type, _list);
                    }
                    _list.Add(table);
                }


                {
                    //缓存扩展数据所影响的成就
                    if (-1 != table.Exdata)
                    {
                        var _list = new List<AchievementRecord>();
                        if (!m_dicExtDataDic.TryGetValue(table.Exdata, out _list))
                        {
                            _list = new List<AchievementRecord>();
                            m_dicExtDataDic.Add(table.Exdata, _list);
                        }

                        _list.Add(table);
                    }
                }

                {
                    //缓存标记位影响哪些成就
                    List<AchievementRecord> _list = null;
                    var _flagId = table.RewardFlagId;
                    if (-1 != _flagId)
                    {
                        if (!m_dicFlagDataDic.TryGetValue(_flagId, out _list))
                        {
                            _list = new List<AchievementRecord>();
                            m_dicFlagDataDic.Add(_flagId, _list);
                        }
                        _list.Add(table);
                    }

                    _flagId = table.FinishFlagId;
                    if (-1 != _flagId)
                    {
                        if (!m_dicFlagDataDic.TryGetValue(_flagId, out _list))
                        {
                            _list = new List<AchievementRecord>();
                            m_dicFlagDataDic.Add(_flagId, _list);
                        }
                        _list.Add(table);
                    }

                    _flagId = table.ClientDisplay;
                    if (-1 != _flagId)
                    {
                        if (!m_dicFlagDataDic.TryGetValue(_flagId, out _list))
                        {
                            _list = new List<AchievementRecord>();
                            m_dicFlagDataDic.Add(_flagId, _list);
                        }
                        _list.Add(table);
                    }
                }

                {
                    //缓存等级影响哪些成就
                    List<AchievementRecord> _list = null;
                    var _level = table.ViewLevel;
                    if (-1 != _level)
                    {
                        if (!m_dicLevelDic.TryGetValue(_level, out _list))
                        {
                            _list = new List<AchievementRecord>();
                            m_dicLevelDic.Add(_level, _list);
                        }
                        _list.Add(table);
                    }
                }

                mTotalAchievement++;

                return true;
            });
        }

        //该成就是否完成了
        private bool IsAchieveAccomplishment(int id)
        {
            return PlayerDataManager.Instance.GetAccomplishmentStatus(id) != eRewardState.CannotGet;
        }

        //当标记位改变时
        private void OnFlagChange(int idx)
        {
            List<AchievementRecord> _list = null;
            if (!m_dicFlagDataDic.TryGetValue(idx, out _list))
            {
                return;
            }

            //我的等级
            var _MyLevel = PlayerDataManager.Instance.GetLevel();

            var _updateTypeList = new List<int>();
            {
                var _list3 = _list;
                var _listCount3 = _list3.Count;
                for (var _i3 = 0; _i3 < _listCount3; ++_i3)
                {
                    var _table = _list3[_i3];
                    {
                        if (_table.Type < 0 || _table.Type >= m_DataModel.Summary.Count)
                        {
                            continue;
                        }

                        var _state = PlayerDataManager.Instance.GetAccomplishmentStatus(_table.Id);

                        //这个成就的可见标记位改变了，并且当前显示的就这个成就列表
                        if (-1 != _table.ClientDisplay && _table.ClientDisplay == idx && mCurrentPage == _table.Type)
                        {
                            var _needAdd = false;
                            if (eRewardState.CannotGet != _state)
                            {
                                //完成就直接显示
                                _needAdd = true;
                            }
                            else if (0 != FlagData.GetFlag(_table.ClientDisplay))
                            {
                                if (_MyLevel >= _table.ViewLevel)
                                {
                                    //达到了显示条件才显示
                                    _needAdd = true;
                                }
                            }

                            //这个成就变可见了
                            if (_needAdd)
                            {
                                var _find = false;
                                {
                                    // foreach(var data in DataModel.CurrentAchievementItemList)
                                    var _enumerator12 = (m_DataModel.CurrentAchievementItemList).GetEnumerator();
                                    while (_enumerator12.MoveNext())
                                    {
                                        var _data = _enumerator12.Current;
                                        {
                                            if (_data.Id == _table.Id)
                                            {
                                                _find = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!_find)
                                {
                                    //没找到就要加入到当前列表
                                    var _achievement = new AchievementItemDataModel();
                                    m_DataModel.CurrentAchievementItemList.Add(_achievement);
                                    FillAccomplishment(_table, _achievement);
                                }
                            }
                        }

                        if (_state == eRewardState.CannotGet)
                        {
                            continue;
                        }

                        //这个类型需要重新计算
                        if (!_updateTypeList.Contains(_table.Type))
                        {
                            _updateTypeList.Add(_table.Type);
                        }
                        {
                            // foreach(var achievement in DataModel.CurrentAchievementItemList)
                            var _enumerator13 = (m_DataModel.CurrentAchievementItemList).GetEnumerator();
                            while (_enumerator13.MoveNext())
                            {
                                var _achievement = _enumerator13.Current;
                                {
                                    if (_achievement.Id == _table.Id)
                                    {
                                        _achievement.State = (int)_state;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //刷新改变的类型
            if (_updateTypeList.Count > 0)
            {
                var _tempList = m_DataModel.CurrentAchievementItemList;
                var _changed = false;
                for (var i = 1; i < _tempList.Count; i++)
                {
                    var _temp = _tempList[i];
                    if (eRewardState.CanGet == (eRewardState)_temp.State)
                    {
                        _tempList.RemoveAt(i);
                        _tempList.Insert(0, _temp);
                        _changed = true;
                    }
                }
                if (_changed)
                {
                    SortByState(_tempList.ToList());
                    m_DataModel.CurrentAchievementItemList = new ObservableCollection<AchievementItemDataModel>(allLst);
                }

                {
                    var _list4 = _updateTypeList;
                    var _listCount4 = _list4.Count;
                    for (var _i4 = 0; _i4 < _listCount4; ++_i4)
                    {
                        var _type = _list4[_i4];
                        {
                            RenewalType(_type);
                        }
                    }
                }
                RenewalNotice();
            }


            RefeshIntergral();
        }

        // 0：已领取  1：可领取  2：未获得
        private List<AchievementItemDataModel> canGetLst = new List<AchievementItemDataModel>();
        private List<AchievementItemDataModel> cannotGetLst = new List<AchievementItemDataModel>();
        private List<AchievementItemDataModel> hasGotLst = new List<AchievementItemDataModel>();
        private List<AchievementItemDataModel> allLst = new List<AchievementItemDataModel>();
        private void SortByState(List<AchievementItemDataModel> lst)
        {
            canGetLst.Clear();
            cannotGetLst.Clear();
            hasGotLst.Clear();
            allLst.Clear();
            for (int i = 0; i < lst.Count; i++)
            {
                if ((eRewardState) lst[i].State == eRewardState.CanGet)
                {
                    canGetLst.Add(lst[i]);
                }
                else if ((eRewardState)lst[i].State == eRewardState.CannotGet)
                {
                    cannotGetLst.Add(lst[i]);
                }
                else if ((eRewardState)lst[i].State == eRewardState.HasGot)
                {
                    hasGotLst.Add(lst[i]);
                }
            }
            allLst.AddRange(canGetLst);
            allLst.AddRange(cannotGetLst);
            allLst.AddRange(hasGotLst);
        }

        //更新总阶分
        private void RefeshIntergral()
        {
            var _count = 0;
            {
                // foreach(var item in DataModel.Summary)
                var _enumerator9 = (m_DataModel.Summary).GetEnumerator();
                while (_enumerator9.MoveNext())
                {
                    var _item = _enumerator9.Current;
                    {
                        _count += _item.CompletedNum;
                    }
                }
            }

            m_DataModel.Percent = _count * 1.0f / mTotalAchievement;
        }

        //刷类型
        private void RenewalType(int type)
        {
            List<AchievementRecord> _list = null;
            if (!m_dicAnalyzeTable.TryGetValue(type, out _list))
            {
                return;
            }

            if (type < 0 || type >= m_DataModel.Summary.Count)
            {
                return;
            }

            var _summary = m_DataModel.Summary[type];

            var _completedNum = 0;
            var _canGetNum = 0;
            {
                var _list11 = m_dicAnalyzeTable[type];
                var _listCount11 = _list11.Count;
                for (var _i11 = 0; _i11 < _listCount11; ++_i11)
                {
                    var _table = _list11[_i11];
                    {
                        var _state = PlayerDataManager.Instance.GetAccomplishmentStatus(_table.Id);
                        if (eRewardState.CannotGet == _state)
                        {
                        }
                        else
                        {
                            _completedNum++;
                            if (eRewardState.CanGet == _state)
                            {
                                _canGetNum++;
                            }
                        }
                    }
                }
            }
            //小红点
            m_DataModel.Catalog[type + 1].Count = _canGetNum;

            //成就总览
            _summary.CompletedNum = _completedNum;
            _summary.Progress = 0 == _list.Count ? 0 : _summary.CompletedNum * 1.0f / _list.Count;
            //summary.ProgressLabel = GameUtils.GetBigValueStr(summary.CompletedNum) + "/" + GameUtils.GetBigValueStr(list.Count);
            _summary.ProgressLabel = _summary.CompletedNum + "/" + _list.Count;
            var _percent = (int)Math.Ceiling(_summary.Progress * 100);
            _summary.ProgressString = _percent + "%";
        }

        private void RenewalNotice()
        {
            var _has = false;

            //主界面上的
            //PlayerDataManager.Instance.NoticeData.HasAchievement = false;
            {
                // foreach(var catlog in DataModel.Catalog)
                var _enumerator10 = (m_DataModel.Catalog).GetEnumerator();
                while (_enumerator10.MoveNext())
                {
                    var _catlog = _enumerator10.Current;
                    {
                        if (_catlog.Count > 0)
                        {
                            _has = true;
                            break;
                        }
                    }
                }
            }

            PlayerDataManager.Instance.NoticeData.HasAchievement = _has;
        }

        //显示某个成就子页
        private void DisplayAccomplishmentPage(int id, float percent = 0.0f)
        {
            if (id != mCurrentPage)
            {
                mCurrentPage = id;

                RefreshAccomplishment();
            }
            m_DataModel.Catalog[mCurrentPage+1].Checked = true;
        }

        //刷新当前页
        private void RefreshAccomplishment()
        {
            var _MyLevel = PlayerDataManager.Instance.GetLevel();

            m_DataModel.CurrentAchievementItemList.Clear();

            var _tempList = new List<AchievementItemDataModel>();

            List<AchievementRecord> _list = null;
            if (!m_dicAnalyzeTable.TryGetValue(mCurrentPage, out _list))
            {
                return;
            }
            {
                var _list8 = _list;
                var _listCount8 = _list8.Count;
                for (var _i8 = 0; _i8 < _listCount8; ++_i8)
                {
                    var _table = _list8[_i8];
                    {
                        //并且也没完成
                        if (!IsAchieveAccomplishment(_table.Id))
                        {
                            //没达到可视等级隐藏
                            if (_table.ViewLevel > 0 && _MyLevel < _table.ViewLevel || _table.Type == 9)
                            {
                                continue;
                            }

                            //扩展数据可见性判断
                            if (-1 != _table.ClientDisplay)
                            {
                                if (0 == FlagData.GetFlag(_table.ClientDisplay))
                                {
                                    continue;
                                }
                            }
                        }

                        var _achievement = new AchievementItemDataModel();
                        FillAccomplishment(_table, _achievement);
                        var _state = (eRewardState)_achievement.State;
                        if (eRewardState.CanGet == _state)
                        {
                            _tempList.Insert(0, _achievement);
                        }
                        else
                        {
                            _tempList.Add(_achievement);
                        }
                    }
                }
                SortByState(_tempList);
            }

            m_DataModel.CurrentAchievementItemList = new ObservableCollection<AchievementItemDataModel>(allLst);
        }


        #endregion

        #region 事件函数

        //点击获得成就奖励
        private void OnClaimAwardEvent(IEvent ievent)
        {
            var _e = ievent as UI_EventApplyChengJiuItem;
            if (null != _e)
            {
                GetAccomplishmentRewards(_e.Id);
            }
        }

        //当扩展数据更新时
        private void OnExDataUpgradeDataEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            var _idx = _e.Key;

            if (_idx == s_iTOTAL_SCORE_DATA_IDX)
            {
                //是成就总积分改变了
                m_DataModel.TotalScore = GetAmountIntegral();
                return;
            }

            List<AchievementRecord> _list = null;
            if (!m_dicExtDataDic.TryGetValue(_idx, out _list))
            {
                return;
            }
            {
                var _list5 = _list;
                var _listCount5 = _list5.Count;
                for (var _i5 = 0; _i5 < _listCount5; ++_i5)
                {
                    var _table = _list5[_i5];
                    {
                        {
                            // foreach(var achievement in DataModel.CurrentAchievementItemList)
                            var _enumerator14 = (m_DataModel.CurrentAchievementItemList).GetEnumerator();
                            while (_enumerator14.MoveNext())
                            {
                                var _achievement = _enumerator14.Current;
                                {
                                    if (_achievement.Id == _table.Id)
                                    {
                                        if (-1 != _table.Exdata && _idx == _table.Exdata)
                                        {
                                            var _progress = Mathf.Min(PlayerDataManager.Instance.GetAccomplishmentProgress(_achievement.Id),
                                                _table.ExdataCount);
                                            _achievement.Progress = _progress * 1.0f / _table.ExdataCount;
                                            _achievement.ProgressLabel = string.Format("{0}/{1}",
                                                GameUtils.GetBigValueStr(_progress),
                                                GameUtils.GetBigValueStr(_table.ExdataCount));
                                            _achievement.ShowPorgress = true;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //标记位更新
        private void OnFlagRefeshEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            var _idx = _e.Index;
            OnFlagChange(_idx);
        }

        //等级提升时
        private void OnUpLVEvent(IEvent ievent)
        {
            var _e = ievent as Event_LevelUp;
            var _MyLevel = PlayerDataManager.Instance.GetLevel();

            List<AchievementRecord> _list = null;
            if (!m_dicLevelDic.TryGetValue(_MyLevel, out _list))
            {
                return;
            }
            {
                var _list6 = _list;
                var _listCount6 = _list6.Count;
                for (var _i6 = 0; _i6 < _listCount6; ++_i6)
                {
                    var _table = _list6[_i6];
                    {
                        if (_table.Type != mCurrentPage)
                        {
                            continue;
                        }

                        var _achievement = new AchievementItemDataModel();
                        m_DataModel.CurrentAchievementItemList.Add(_achievement);
                        FillAccomplishment(_table, _achievement);
                    }
                }
            }
        }

        //显示成就子页
        private void OnDisplayAccomplishmentPageEvent(IEvent ievent)
        {
            {
                // foreach(var item in DataModel.Catalog)
                unShowCatalog();
            }

            var _evn = ievent as Event_ShowAchievementPage;

            if (_evn.Id < -1 || _evn.Id >= (m_DataModel.Catalog.Count-1))//数据保护防止索引超出范围
            {
                _evn.Id = -1;
            }
            if (-1 == _evn.Id)
            {
                m_DataModel.SubPage = false;
                m_DataModel.Catalog[0].Checked = true;
            }
            else
            {
                m_DataModel.SubPage = true;
                DisplayAccomplishmentPage(_evn.Id, _evn.Percent);
            }
        }


        private void unShowCatalog()
        {
            if (m_DataModel.Catalog != null && m_DataModel.Catalog.Count > 0)             
            {
                var _enumerator7 = (m_DataModel.Catalog).GetEnumerator();
                while (_enumerator7.MoveNext())
                {
                    var _item = _enumerator7.Current;
                    {
                        _item.Checked = false;
                    }
                }
            }
        }


        #endregion  
    }
}