/********************************************************************************* 

                         Scorpion



  *FileName:UIAwardFrameCtrler

  *Version:1.0

  *Date:2017-07-13

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
    public class UIAwardFrameCtrler : IControllerBase
    {

        #region 静态变量

        #region Reward State

        private const int s_iCONTINUES_LOGIN_FLAG_IDX = 313;
        private const int s_iCONTINUES_LOGIN_DATA_IDX = 17;
        private const int s_iCONTINUES_RECHECKIN_DATA_IDX = 18;
        private const int s_iTODAY_CHECKIN_FLAG_IDX = 466;
        private const int s_iMONTH_CHECKIN_DAYS_DATA_IDX = 16;
        private const int s_iTODAY_ONLINE_TIME_DATA_IDX = 31;
        private const int s_iACTIVITY_SCORE_DATA_IDX = 15;
        private const int s_lifeCardFlag_IDX = 2510;//终生卡每日领取奖励标记位
        private const int s_lifeCardRechargeFlag_IDX = 2682;//终生卡充值标记位
        private static readonly Dictionary<int, List<eRewardType>> s_dicExdataGift = new Dictionary<int, List<eRewardType>>();
        private static readonly Dictionary<int, List<eRewardType>> s_dicFlagGift = new Dictionary<int, List<eRewardType>>();
        private static readonly Dictionary<eRewardType, List<GiftRecord>> s_dicGiftList =
            new Dictionary<eRewardType, List<GiftRecord>>();
        #endregion

        #endregion

        #region 成员变量

        //缓存标记位影响哪些成就
        private readonly Dictionary<int, List<GiftRecord>> m_dicFlagDataDict = new Dictionary<int, List<GiftRecord>>();
        private bool m_bHasBeenInited;
        ////缓存扩展计数影响哪些成就
        //Dictionary<int, List<GiftRecord>> mExtDataDict = new Dictionary<int, List<GiftRecord>>();

        //缓存各个类型奖励表格数据
        private List<GiftRecord>[] m_listTableCache = new List<GiftRecord>[(int)eRewardType.DailyActivityReward + 1];
        private RewardDataModel RewardData { get; set; }
        #region 补偿奖励
        private readonly Dictionary<int, string> m_dicCompensateName = new Dictionary<int, string>(); //int:type string:name
        private int m_iGoodProp;
        private int m_iExpProp;
        private int m_iDiamondParm;
        private int m_iResParm;
        private int m_iGoldOrDia; //选择为金币或者钻石
        private int m_iCompensateOkType; //选择 0 一键金币  1 一键钻石  2 所选择的item
        private int m_iCompensateSelectIndex;

        private List<int> LeftTimeitemList = new List<int>();
        #endregion

        #endregion

        #region 构造函数

        public UIAwardFrameCtrler()
        {
            InitionTableLater_AnalysedNotice();
            HideTable();

            CleanUp();

            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, evn => { Inition(); });

            EventDispatcher.Instance.AddEventListener(UIEvent_CliamReward.EVENT_TYPE, OnCliamAwardEvent);


            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagRenewalEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExtDataUpInfoEvent);

            EventDispatcher.Instance.AddEventListener(Event_UpdateOnLineReward.EVENT_TYPE, RenewalOnLineAwardEvent);

            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnRenewalLevelAwardEvent);

            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnAnalysedNoticeByFlagedEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnAnalysedNoticeExdataEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, LevelUpNotice);
            EventDispatcher.Instance.AddEventListener(LevelUpInitEvent.EVENT_TYPE, LevelUpNotice);

            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnAnalysedNoticeInitionEvent);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnAnalysedNoticeInitionEvent);

            EventDispatcher.Instance.AddEventListener(UIEvent_GetOnLineSeconds.EVENT_TYPE, OnGAcquireOnLineSecondEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_ActivityCompensateItem.EVENT_TYPE, OnClickCompensateEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_UseGiftCodeEvent.EVENT_TYPE, OnEmployGiftCdKeyEvent);

            EventDispatcher.Instance.AddEventListener(RewardActivityItemClickEvent.EVENT_TYPE, OnClickAwardItemEvent);


            EventDispatcher.Instance.AddEventListener(ExData64InitEvent.EVENT_TYPE, OnExdata64Init);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagInit);
            EventDispatcher.Instance.AddEventListener(GetMonthCardEvent.EVENT_TYPE, GetMonthCard);
            EventDispatcher.Instance.AddEventListener(GetWeekCardRewardEvent.EVENT_TYPE, GetWeekCard);        
            EventDispatcher.Instance.AddEventListener(GetLifeCardRewardEvent.EVENT_TYPE, GetLifeCardReward);
            EventDispatcher.Instance.AddEventListener(ExData64UpDataEvent.EVENT_TYPE, OnExData64Update);
            EventDispatcher.Instance.AddEventListener(UIEvent_GetOfflineItemEvent.EVENT_TYPE, OnGetOfflineExp);


            
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnBagChange);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemCountChange);
            EventDispatcher.Instance.AddEventListener(UIEvent_UseOfflineItemEvent.EVENT_TYPE, OnClickUseOfflineItemEvent);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            RewardData = new RewardDataModel();
            InitionTable();
            m_bHasBeenInited = false;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "InitCompensate")
            {
                var _items = param[0] as Dictionary<int, Compensation>;
                if (_items != null)
                {
                    InitionCompensation(_items);
                }
            }
            return null;
        }

        public void OnShow()
        {
        }

        public void Close()
        {
            //	RewardData.FirstPage = 0;
            //RewardData.Tab = 0;
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var _tab = data as UIRewardFrameArguments;
            if (null != _tab)
            {
                RewardData.Tab = _tab.Tab;
            }
            else
            {
                var _noticeData = PlayerDataManager.Instance.NoticeData;
                if (_noticeData.ActivityTimeLength > 0)
                {
                    RewardData.Tab = 0;
                }
                else if (_noticeData.ActivityLevel > 0)
                {
                    RewardData.Tab = 1;
                }
                else if (_noticeData.ActivityLoginSeries > 0)
                {
                    RewardData.Tab = 2;
                }
                else if (_noticeData.ActivityLoginAddup > 0)
                {
                    RewardData.Tab = 3;
                }
                else if (_noticeData.ActivityCompensateActive > 0)
                {
                    RewardData.Tab = 4;
                }
                else
                {
                    RewardData.Tab = 0;
                }
            }
            RewardData.Compensate.ShowWitchConfirm = 0;
            EventDispatcher.Instance.DispatchEvent(new UI_Event_OffLineExp(3)); //开界面刷新一次离线经验


            var numStr = Table.GetClientConfig(256).Value;
            var nums = numStr.Split('|');
            for (var i = 0; i < nums.Length; ++i)
            {
                LeftTimeitemList.Add(int.Parse(nums[i]));
            }
            RefreshOfflineTime();
            RefreshOfflineItem();

        }

        private void OnBagChange(IEvent ievent)
        {
            var e = ievent as UIEvent_BagChange;
            if (e == null)
            {
                return;
            }

            if (e.HasType(eBagType.BaseItem))
            {
                if (State == FrameState.Open)
                {
                    RefreshOfflineItem();
                }
            }
        }

        private void OnBagItemCountChange(IEvent ievent)
        {
            var e = ievent as UIEvent_BagItemCountChange;
            if (e == null)
            {
                return;
            }

            if (LeftTimeitemList.Contains(e.ItemId))
            {
                if (State == FrameState.Open)
                {
                    RefreshOfflineItem();
                }
            }
        }

        private void RefreshOfflineTime()
        {
            var leftTime = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e742);
            TimeSpan left = new TimeSpan(0, 0, leftTime);
            RewardData.OfflineLeftTime = GameUtils.GetTimeDiffString(left);
            if (leftTime > 0)
            {
                RewardData.ColorOfflineLeftTime = Color.green;
            }
            else
            {
                RewardData.ColorOfflineLeftTime = Color.red;
            }
        }

        private void RefreshOfflineItem()
        {
            RewardData.OfflineTimeItem.ItemId = 0;
            RewardData.OfflineTimeItem.Count = 0;
            foreach (var value in LeftTimeitemList)
            {
                var LeftTimeItem = PlayerDataManager.Instance.GetItemTotalCount(value).Count;
                if (LeftTimeItem > 0)
                {
                    RewardData.OfflineTimeItem.ItemId = value;
                    RewardData.OfflineTimeItem.Count = LeftTimeItem;
                    break;
                }
            }

            if (RewardData.OfflineTimeItem.ItemId <= 0 && LeftTimeitemList.Count > 0)
            {
                RewardData.OfflineTimeItem.ItemId = LeftTimeitemList[0];
                RewardData.OfflineTimeItem.Count = 0;
            }

            var leftTime = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e742);
            var vipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
            var tbVip = Table.GetVIP(vipLevel);
            if (tbVip == null)
            {
                return;
            }
            var maxValue = tbVip.OfflineTimeMax;
            if (leftTime < maxValue && RewardData.OfflineTimeItem.Count > 0)
            {
                RewardData.IsShowUseOfflineItemNotice = true;
            }
            else
            {
                RewardData.IsShowUseOfflineItemNotice = false;
            }
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            return RewardData;
        }

        public FrameState State { get; set; }

        #endregion

        #region 逻辑函数

        private void AdditionMarkDic(int idx, GiftRecord table)
        {
            List<GiftRecord> _list = null;
            if (!m_dicFlagDataDict.TryGetValue(idx, out _list))
            {
                _list = new List<GiftRecord>();
                m_dicFlagDataDict.Add(idx, _list);
            }
            _list.Add(table);
        }


        //缓存表格数据，只调一次
        private void HideTable()
        {
            for (var i = 0; i < m_listTableCache.Length; i++)
            {
                m_listTableCache[i] = new List<GiftRecord>();
            }

            Table.ForeachGift(table =>
            {
                var _type = (eRewardType)table.Type;

                if (eRewardType.OnlineReward == _type)
                {
                    m_listTableCache[table.Type].Add(table);
                    AdditionMarkDic(table.Flag, table);
                }
                else if (eRewardType.LevelReward == _type)
                {
                    m_listTableCache[table.Type].Add(table);
                    AdditionMarkDic(table.Flag, table);
                }
                else if (eRewardType.ContinuesLoginReward == _type)
                {
                    m_listTableCache[table.Type].Add(table);
                }
                else if (eRewardType.MonthCheckinReward == _type)
                {
                    m_listTableCache[table.Type].Add(table);
                }
                //else if (eRewardType.DailyActivity == type)
                //{
                //    mTableCache[table.Type].Add(table);
                //    AddExtDataDict(table.Exdata, table);
                //}
                else if (eRewardType.DailyActivityReward == _type)
                {
                    m_listTableCache[table.Type].Add(table);
                    AdditionMarkDic(table.Flag, table);
                }

                return true;
            });
        }

        //public void AddExtDataDict(int idx, GiftRecord table)
        //{
        //    List<GiftRecord> list = null;
        //    if (!mExtDataDict.TryGetValue(idx, out list))
        //    {
        //        list = new List<GiftRecord>();
        //        mExtDataDict.Add(idx, list);
        //    }
        //    list.Add(table);
        //}

        private void Inition()
        {
            if (m_bHasBeenInited)
            {
                return;
            }
            m_bHasBeenInited = true;

            RenewalOnLineAward();
            OnRenewalLevelAwardEvent();
            RenewalContinuouslyLoginAward();
            OnRenewalSignInMonthAwardEvent();
            RenewalActive();
            RenewalActiveAward();
            //RenewalMonthCard();
        }

        //用表格数据初始化数据源
        private void InitionTable()
        {
            //在线奖励
            RewardData.OnLineReward.Rewards.Clear();
            {
                var _list10 = m_listTableCache[(int)eRewardType.OnlineReward];
                var _listCount10 = _list10.Count;
                for (var _i10 = 0; _i10 < _listCount10; ++_i10)
                {
                    var _table = _list10[_i10];
                    {
                        RewardData.OnLineReward.Rewards.Add(OnLineAward(_table));
                    }
                }
            }
            //等级奖励
            RewardData.LevelReward.Rewards.Clear();
            {
                var _list11 = m_listTableCache[(int)eRewardType.LevelReward];
                var _listCount11 = _list11.Count;
                for (var _i11 = 0; _i11 < _listCount11; ++_i11)
                {
                    var _table = _list11[_i11];
                    {
                        RewardData.LevelReward.Rewards.Add(LevelAward(_table));
                    }
                }
            }
            //连续登录奖励
            {
                var _rewardList = RewardData.ContinuesLoginReward.Rewards;
                var _tableList = m_listTableCache[(int)eRewardType.ContinuesLoginReward];

                var _c = _rewardList.Count;
                for (var i = 0; i < _c; i++)
                {
                    var _reward = _rewardList[i];
                    _reward.ItemId = -1;
                    _reward.Count = 0;
                }

                _c = _tableList.Count;
                for (var i = 0; i < _c; i++)
                {
                    var _table = _tableList[i];
                    var _days = _table.Param[ContinuesLoginRewardParamterIndx.Days];
                    var _idx = _days - 1;

                    if (_idx >= 0 && _idx < _rewardList.Count)
                    {
                        var _reward = _rewardList[_idx];
                        _reward.ItemId = _table.Param[ContinuesLoginRewardParamterIndx.ItemId];
                        _reward.Count = _table.Param[ContinuesLoginRewardParamterIndx.ItemCount];
                    }
                }
            }

            //每月累计签到奖励
            RewardData.MonthCheckinReward.Rewards.Clear();
            {
                {
                    var _list12 = m_listTableCache[(int)eRewardType.MonthCheckinReward];
                    var _listCount12 = _list12.Count;
                    for (var _i12 = 0; _i12 < _listCount12; ++_i12)
                    {
                        var _table = _list12[_i12];
                        {
                            SignInMonthAward(_table);
                        }
                    }
                }
            }

            ////每日活跃任务
            //RewardData.ActivityReward.Activity.Clear();
            //{
            //    var __list13 = mTableCache[(int) eRewardType.DailyActivity];
            //    var __listCount13 = __list13.Count;
            //    for (int __i13 = 0; __i13 < __listCount13; ++__i13)
            //    {
            //        var table = __list13[__i13];
            //        {
            //            RewardData.ActivityReward.Activity.Add(MakeActivity(table));
            //        }
            //    }
            //}

            //每日活跃奖励
            RewardData.ActivityReward.ActivityReward.Clear();
            {
                var _list14 = m_listTableCache[(int)eRewardType.DailyActivityReward];
                var _listCount14 = _list14.Count;
                for (var _i14 = 0; _i14 < _listCount14; ++_i14)
                {
                    var _table = _list14[_i14];
                    {
                        RewardData.ActivityReward.ActivityReward.Add(MakeActiveAward(_table));
                    }
                }
            }

            m_dicCompensateName.Clear();
            Table.ForeachCompensation(table =>
            {
                if (m_dicCompensateName.ContainsKey(table.Type))
                {
                    return true;
                }
                m_dicCompensateName.Add(table.Type, table.Name);
                return true;
            }
                );
            m_iExpProp = int.Parse(Table.GetClientConfig(584).Value);
            m_iGoodProp = int.Parse(Table.GetClientConfig(585).Value);
            m_iDiamondParm = int.Parse(Table.GetClientConfig(586).Value);
            m_iResParm = int.Parse(Table.GetClientConfig(587).Value);
        }

        private void LevelUpNotice(IEvent ievent)
        {
            OnAnalysedNoticeByFlagedEvent(305);
        }


        #region 在线奖励

        private OnLineRewardItemDataModel OnLineAward(GiftRecord table)
        {
            var min = table.Param[OnLineRewardParamterIndx.Minutes] / 60;
            var str = min + GameUtils.GetDictionaryText(1041);
            var dataModel = new OnLineRewardItemDataModel
            {
                Id = table.Id,
                Minutes = str,
                Seconds = table.Param[OnLineRewardParamterIndx.Minutes],
                Item = new ItemIconDataModel
                {
                    ItemId = table.Param[OnLineRewardParamterIndx.ItemId],
                    Count = table.Param[OnLineRewardParamterIndx.ItemCount]
                },
                CanGetReward = false,
                HasGotReward = 0
            };
            return dataModel;
        }



        private void RenewalOnLineAward()
        {
            var _onLineSeconds = AcquireOnLineSecond();
            var _time = TimeSpan.FromSeconds(_onLineSeconds);

            var _canGetCount = 0;
            var _hasGot = 0;
            {
                // foreach(var item in RewardData.OnLineReward.Rewards)
                var _find = false;
                var _enumerator2 = (RewardData.OnLineReward.Rewards).GetEnumerator();
                while (_enumerator2.MoveNext())
                {
                    var _item = _enumerator2.Current;
                    {
                        var _state = AcquireOnLineAwardState(_item.Id);
                        if (_state == eRewardState.HasGot)
                        {
                            _item.HasGotReward = 1;
                            _item.CanGetReward = false;
                            _hasGot++;
                            _item.TimeDesc = "";
                        }
                        else if (_state == eRewardState.CanGet)
                        {
                            _item.HasGotReward = 0;
                            _item.CanGetReward = true;
                            _item.TimeDesc = "";
                            _canGetCount++;
                        }
                        else
                        {
                            _item.HasGotReward = 0;
                            _item.CanGetReward = false;
                            if (_time.TotalSeconds < _item.Seconds)
                            {
                                if (!_find)
                                {
                                    var diff = (int)(_item.Seconds - _time.TotalSeconds);
                                    _item.TimeDesc = string.Format("{0:D2}:{1:D2}", diff / 60, diff % 60);
                                    _find = true;
                                }
                                else
                                {
                                    _item.TimeDesc = "";
                                }
                            }
                            else
                            {
                                _item.TimeDesc = "";
                            }
                        }
                    }
                }
            }
            RewardData.OnLineReward.OnLineTip = string.Format(GameUtils.GetDictionaryText(515), _time.Hours, _time.Minutes,
                _time.Seconds, RewardData.OnLineReward.Rewards.Count - _hasGot);

            var _noticeData = PlayerDataManager.Instance.NoticeData;
            if (null != _noticeData)
            {
                _noticeData.ActivityTimeLength = _canGetCount;
            }
        }

        #endregion

        #region 等级奖励

        private LevelRewardItemDataModel LevelAward(GiftRecord table)
        {
            var _dataModel = new LevelRewardItemDataModel
            {
                Id = table.Id,
                Level = table.Param[LevelRewardParamterIndx.Level].ToString()
            };
            var _intLevelRewardParamterIndxItemId_Max0 = LevelRewardParamterIndx.ItemId_Max;
            for (var i = LevelRewardParamterIndx.ItemId_1; i <= _intLevelRewardParamterIndxItemId_Max0; i++)
            {
                var _itemId = table.Param[i];
                var _count = table.Param[i+5];
                if (-1 == _itemId || -1 == _count)
                {
                    break;
                }

                var _item = new ItemIconDataModel
                {
                    ItemId = _itemId,
                    Count = _count
                };

                var index = i - 1;
                if (index >= _dataModel.Rewards.Count)
                {
                    break;
                }
                _dataModel.Rewards[index] = _item;
            }

            return _dataModel;
        }

        private void OnRenewalLevelAwardEvent()
        {
            {
                // foreach(var item in RewardData.LevelReward.Rewards)
                var _enumerator3 = (RewardData.LevelReward.Rewards).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _item = _enumerator3.Current;
                    {
                        var _state = AcquireLevelAwardState(_item.Id);
                        if (_state == eRewardState.HasGot)
                        {
                            _item.HasGotReward = 1;
                            _item.CanGetReward = false;
                        }
                        else
                        {
                            _item.HasGotReward = 0;
                            _item.CanGetReward = eRewardState.CanGet == _state;
                        }
                    }
                }
            }
        }

        #endregion

        #region 连续登录奖励

        private void ContinuouslyLoginAward(GiftRecord table)
        {
            var _rewards = RewardData.ContinuesLoginReward.Rewards;
            var _days = table.Param[ContinuesLoginRewardParamterIndx.Days];
            // 		int needAdd = days - rewards.Count;
            // 		if (needAdd > 0)
            // 		{
            // 			for (int i = 0; i < needAdd; i++)
            // 			{
            // 				rewards.Add(new ItemIconDataModel());
            // 			}
            // 		}
            var _idx = _days - 1;
            _rewards[_idx].ItemId = table.Param[ContinuesLoginRewardParamterIndx.ItemId];
            _rewards[_idx].Count = 1;
        }

        private void RenewalContinuouslyLoginAward()
        {
            if (HasGotNowadaysLoginAward())
            {
                RewardData.ContinuesLoginReward.HasGot = 1;
                RewardData.ContinuesLoginReward.CanGet = false;
            }
            else
            {
                RewardData.ContinuesLoginReward.HasGot = 0;
                RewardData.ContinuesLoginReward.CanGet = true;
            }
            var _str = string.Format(GameUtils.GetDictionaryText(516), AcquireSuccessionLoginNumOfDays());
            RewardData.ContinuesLoginReward.Tip = _str;

            var _str1 = string.Format(GameUtils.GetDictionaryText(517), AcquireSuccessionLoginNumOfDays());
            RewardData.ContinuesLoginReward.Tip1 = _str1;
            RewardData.ContinuesLoginReward.Days = AcquireSuccessionLoginNumOfDays();
        }

        #endregion

        #region 每月签到

        private void SignInMonthAward(GiftRecord table)
        {
            var _rewards = RewardData.MonthCheckinReward.Rewards;

            var _month = table.Param[MonthCheckinRewardParamterIndx.Month];
            if (999 != _month && Game.Instance.ServerTime.Month != _month)
            {
                return;
            }

            var _days = table.Param[MonthCheckinRewardParamterIndx.Day];
            var _itemId = table.Param[MonthCheckinRewardParamterIndx.ItemId];
            var _itemCount = table.Param[MonthCheckinRewardParamterIndx.ItemCount];
            var _cost = table.Param[MonthCheckinRewardParamterIndx.CostDiamond];

            var _needAdd = _days - _rewards.Count;
            if (_needAdd > 0)
            {
                for (var i = 0; i < _needAdd; i++)
                {
                    _rewards.Add(new MonthCheckinRewardItemDataModel());
                }
            }
            var _idx = _days - 1;
            _rewards[_idx].Id = table.Id;
            _rewards[_idx].ItemId = _itemId;
            _rewards[_idx].Count = _itemCount;
            _rewards[_idx].Index = _idx + 1;
        }



        private void OnRenewalSignInMonthAwardEvent()
        {
            var _monthCheckinReward = RewardData.MonthCheckinReward;
            var _rewards = _monthCheckinReward.Rewards;

            var _date = Game.Instance.ServerTime;

            //本月已签次数
            var _checkinTimes = AcquireMonthReportNum();

            //是否全签
            var _allChecked = _checkinTimes >= _rewards.Count;

            //本月可补签次数
            var _remainCheckTimes = 0;
            if (true != _allChecked)
            {
                var _total = Math.Min(_rewards.Count, _date.Day);
                _remainCheckTimes = _total - _checkinTimes;
                if (!HasNowdaysReport())
                {
                    _remainCheckTimes -= 1;
                }
            }

            _monthCheckinReward.CurrentMonth = string.Format(GameUtils.GetDictionaryText(518), _date.Month);
            _monthCheckinReward.CurrentMonthCheckinTimes = string.Format(GameUtils.GetDictionaryText(519), _checkinTimes);
            _monthCheckinReward.CurrentMonthReCheckinTimes = string.Format(GameUtils.GetDictionaryText(520), _remainCheckTimes);
            _monthCheckinReward.CanCheckin = false;
            _monthCheckinReward.CanReCheckin = false;

            if (_allChecked)
            {
                //全签
                {
                    // foreach(var item in monthCheckinReward.Rewards)
                    var _enumerator4 = (_monthCheckinReward.Rewards).GetEnumerator();
                    while (_enumerator4.MoveNext())
                    {
                        var _item = _enumerator4.Current;
                        {
                            _item.Selected = false;
                            _item.HasGotReward = true;
                        }
                    }
                }
            }
            else
            {
                var _selectIdx = _checkinTimes;
                if (HasNowdaysReport())
                {
                    _selectIdx = _remainCheckTimes > 0 ? _checkinTimes : _checkinTimes - 1;
                }

                var _idx = 0;
                {
                    // foreach(var item in monthCheckinReward.Rewards)
                    var _enumerator5 = (_monthCheckinReward.Rewards).GetEnumerator();
                    while (_enumerator5.MoveNext())
                    {
                        var _item = _enumerator5.Current;
                        {
                            _item.Selected = _idx == _selectIdx;
                            _item.HasGotReward = _idx < _checkinTimes;
                            _idx++;
                        }
                    }
                }

                if (!HasNowdaysReport())
                {
                    _monthCheckinReward.CanCheckin = true;
                }
                else if (_remainCheckTimes > 0)
                {
                    _monthCheckinReward.CanReCheckin = true;
                }
            }
        }

        #endregion

        #region 积分奖励

        private ActivityItemDataModel MakeActive(GiftRecord table)
        {
            var _type = (eRewardType)table.Type;

            var _id = table.Id;
            var _score = table.Param[ActivityParamterIndx.Score].ToString();
            var _desc = GameUtils.GetDictionaryText(table.Param[ActivityParamterIndx.DescId]);

            var _dataModel = new ActivityItemDataModel
            {
                Id = _id,
                Desc = _desc,
                Score = _score
            };
            return _dataModel;
        }

        //每日活跃奖励
        private ActivityRewardItemDataModel MakeActiveAward(GiftRecord table)
        {
            var _id = table.Id;
            var _itemId = table.Param[ActivityRewardParamterIndx.ItemId];
            var _count = table.Param[ActivityRewardParamterIndx.Count];
            var _needScore = table.Param[ActivityRewardParamterIndx.NeedScore];

            var _dataModel = new ActivityRewardItemDataModel
            {
                Id = _id,
                NeedScore = string.Format(GameUtils.GetDictionaryText(528), _needScore),
                Item = new ItemIconDataModel
                {
                    ItemId = _itemId,
                    Count = _count
                }
            };

            return _dataModel;
        }

        private void OnFlagInit(IEvent ievent)
        {
            RenewalMonthCard();
            RenewalWeekCard();
            RefreshLifeCard();
        }
        private void OnExData64Update(IEvent ievent)
        {//月卡时间
            ExData64UpDataEvent e = ievent as ExData64UpDataEvent;
            if (e == null)
                return;
            var exdata64Idx = Table.GetClientConfig(418).ToInt();
            var exdata64Idx_WeekCard = (int)Exdata64TimeType.WeekCardExpirationDate;
            if (e.Key == exdata64Idx)
            {
                RenewalMonthCard();
            }
            if (e.Key == exdata64Idx_WeekCard)
            {
                RenewalWeekCard();
            }
        }
        private void OnExdata64Init(IEvent ievent)
        {
            RenewalMonthCard();
            RefreshLifeCard();
            RenewalWeekCard();
        }

        private bool _bInitLifeCardReady = false;
        private void RefreshLifeCard()
        {
            if (_bInitLifeCardReady == false)
            {
                _bInitLifeCardReady = true;
                return;
            }
            var tbRecharge = Table.GetRecharge(41);
            if (tbRecharge == null)
                return;
            if (!PlayerDataManager.Instance.GetFlag(2682))
            {//非终生卡用户
                RewardData.LifeCard.Status = -1;
                PlayerDataManager.Instance.NoticeData.LifeCard = false;
            }
            else if (PlayerDataManager.Instance.GetFlag(tbRecharge.Param[2]))
            {//已领取
                RewardData.LifeCard.Status = 1;
                PlayerDataManager.Instance.NoticeData.LifeCard = false;
            }
            else
            {//可领取
                RewardData.LifeCard.Status = 0;
                PlayerDataManager.Instance.NoticeData.LifeCard = true;
            }
            int diaCount = tbRecharge.Param[0];
            RewardData.LifeCard.RewardStr = string.Format(GameUtils.GetDictionaryText(100003006), diaCount);
        }

        private bool bInitMonthCardReady = false;
        private void RenewalMonthCard()
        {
            if (bInitMonthCardReady == false)
            {
                bInitMonthCardReady = true;
                return;
            }
            DailyActivityRecord record = Table.GetDailyActivity(2001);
            if (record == null)
                return;
            var exdata64Idx = Table.GetClientConfig(418).ToInt();
            var endTime = PlayerDataManager.Instance.GetExData64(exdata64Idx);
            var endDate = Extension.FromServerBinary(endTime);
            var now = Game.Instance.ServerTime;

        
        
            //剩余天数
            var leftTimes = now > endDate?0:(int)(endDate - now).TotalDays;
            var nowTimes = PlayerDataManager.Instance.GetExData(778);   //已领取天数
            if (now > endDate)
            {//非月卡用户
                RewardData.MonthCard.status = -1;
            }
            else if (PlayerDataManager.Instance.GetFlag(record.CommonParam[0])) //flag 2507:每天的月卡礼包是否已领取
            {//已领取
                RewardData.MonthCard.status = 1;
            }
            else
            {//可领取
                RewardData.MonthCard.status = 0;
                leftTimes ++;
            }
            PlayerDataManager.Instance.NoticeData.MonthCard = RewardData.MonthCard.status == 0;
            int count = Table.GetClientConfig(419).ToInt();
            RewardData.MonthCard.strReward = string.Format(GameUtils.GetDictionaryText(100001307),count);
            RewardData.MonthCard.nowCount = string.Format(GameUtils.GetDictionaryText(100001306), nowTimes);
            RewardData.MonthCard.leftCount = string.Format(GameUtils.GetDictionaryText(100001306), leftTimes);

        }

        private bool bInitWeekCardReady = false;
        private void RenewalWeekCard()
        {
            if (bInitWeekCardReady == false)
            {
                bInitWeekCardReady = true;
                return;
            }
            var tbRecharge = Table.GetRecharge(43);
            if (tbRecharge == null)
                return;
            DailyActivityRecord record = Table.GetDailyActivity(2002);
            if (record == null)
                return;
            var endTime = PlayerDataManager.Instance.GetExData64((int)Exdata64TimeType.WeekCardExpirationDate);
            var endDate = Extension.FromServerBinary(endTime);
            var now = Game.Instance.ServerTime;

            //剩余天数
            var leftTimes = now > endDate ? 0 : (int)(endDate - now).TotalDays;
            var nowTimes = PlayerDataManager.Instance.GetExData(779);   //已领取天数
            if (now > endDate)
            {//非周卡用户
                RewardData.WeekCard.status = -1;
            }
            else if (PlayerDataManager.Instance.GetFlag(record.CommonParam[0])) //flag 2507:每天的月卡礼包是否已领取
            {//已领取
                RewardData.WeekCard.status = 1;
            }
            else
            {//可领取
                RewardData.WeekCard.status = 0;
                leftTimes++;
            }
            PlayerDataManager.Instance.NoticeData.WeekCard = RewardData.WeekCard.status == 0;
            int diaCount = tbRecharge.Param[0];
            RewardData.WeekCard.strReward = string.Format(GameUtils.GetDictionaryText(100001307), diaCount);
            RewardData.WeekCard.nowCount = string.Format(GameUtils.GetDictionaryText(100001306), nowTimes);
            RewardData.WeekCard.leftCount = string.Format(GameUtils.GetDictionaryText(100001306), leftTimes);
        }

        private void RenewalActive()
        {
            //var activity = RewardData.ActivityReward.Activity;
            var _activity = new List<ActivityItemDataModel>(RewardData.ActivityReward.Activity);

            var _list6 = _activity;
            var _listCount6 = _list6.Count;

            var _sort = false;
            for (var _i6 = 0; _i6 < _listCount6; ++_i6)
            {
                var _item = _list6[_i6];
                {
                    var _table = Table.GetGift(_item.Id);
                    var _needTimes = _table.Param[ActivityParamterIndx.NeedTimes];
                    var _progress = AcquireActiveProgress(_item.Id);

                    var _backup = _item.Done;

                    if (_progress < _needTimes)
                    {
                        _item.Done = false;
                    }
                    else
                    {
                        _progress = _needTimes;
                        _item.Done = true;
                    }

                    if (!_sort)
                    {
                        if (_backup != _item.Done)
                        {
                            _sort = true;
                        }
                    }

                    if (_item.Done)
                    {
                        _item.Doing = false;
                    }
                    else
                    {
                        var _uiId = _table.Param[ActivityParamterIndx.UIId];
                        if (-1 != _uiId)
                        {
                            _item.Doing = true;
                        }
                        else
                        {
                            _item.Doing = false;
                        }
                    }
                    _item.Progress = string.Format(_item.Done ? "[00FF00]{0}/{1}[-]" : "[FFDB93]{0}/{1}[-]", _progress,
                        _needTimes);
                }
            }


            if (_sort)
            {
                _activity.Sort(ClassifyActive);
                RewardData.ActivityReward.Activity = new ObservableCollection<ActivityItemDataModel>(_activity);
            }
        }

        private static int ClassifyActive(ActivityItemDataModel a, ActivityItemDataModel b)
        {
            var _va = (a.Done ? 1000000 : 0) + a.Id;

            var _vb = (b.Done ? 1000000 : 0) + b.Id;

            if (_va > _vb)
            {
                return 1;
            }
            if (_vb > _va)
            {
                return -1;
            }
            return 0;
        }

        private void RenewalActiveAward()
        {
            var _score = AcquireActiveScore();
            var _reward = RewardData.ActivityReward.ActivityReward;
            {
                // foreach(var item in reward)
                var _enumerator7 = (_reward).GetEnumerator();
                while (_enumerator7.MoveNext())
                {
                    var _item = _enumerator7.Current;
                    {
                        _item.HasGot = eRewardState.HasGot == AcquireActiveAwardState(_item.Id) ? 1 : 0;
                        _item.CanGet = eRewardState.CanGet == AcquireActiveAwardState(_item.Id);
                    }
                }
            }


            RewardData.ActivityReward.ScoreLabel = _score.ToString();
        }

        #endregion

        #region 补偿奖励

        private void InitionCompensation(Dictionary<int, Compensation> compensationList)
        {
            // foreach(var item in compensationList.Compensations)
            RewardData.Compensate.ItemList.Clear();
            var _list = new List<ActivityCompensateItemDataModel>();
            var _enumerator2 = (compensationList).GetEnumerator();
            while (_enumerator2.MoveNext())
            {
                var _item = _enumerator2.Current;
                {
                    var _itemData = new ActivityCompensateItemDataModel();
                    _itemData.Id = _item.Key;
                    _itemData.NeedGood = 0;
                    _itemData.NeedDia = 0;
                    var _enumerator3 = (_item.Value.Data).GetEnumerator();
                    if (_item.Value.Data.Count == 0)
                    {
                        continue;
                    }
                    var _itemList = new List<ItemIconDataModel>();
                    while (_enumerator3.MoveNext())
                    {
                        var _item2 = _enumerator3.Current;
                        {
                            var dd = new ItemIconDataModel();
                            dd.ItemId = _item2.Key;
                            dd.Count = _item2.Value;
                            _itemList.Add(dd);
                            if (_item2.Key == 1)
                            {
                                _itemData.NeedGood += (_item2.Value + m_iExpProp - 1) / m_iExpProp;
                            }
                            else if (_item2.Key == 2)
                            {
                                _itemData.NeedGood += (_item2.Value + m_iGoodProp - 1) / m_iGoodProp;
                            }
                            else
                            {
                                var _tbItemBase = Table.GetItemBase(_item2.Key);
                                if (_tbItemBase != null)
                                {
                                    _itemData.NeedGood += _item2.Value * _tbItemBase.ItemValue;
                                }
                            }
                        }
                    }
                   
                    if (_itemData.Id == 7) //特殊处理 试炼古域
                    {
                        _itemData.LeftCountLb = Table.GetDictionary(100003310).Desc[GameUtils.LanguageIndex];
                        _itemData.LeftCount = string.Format(Table.GetDictionary(100003311).Desc[GameUtils.LanguageIndex], Math.Ceiling((double)_item.Value.Count / 60));
                    }
                    else
                    {
                        _itemData.LeftCountLb = Table.GetDictionary(100000691).Desc[GameUtils.LanguageIndex];
                        _itemData.LeftCount = _item.Value.Count.ToString();
                    }
                    
                    _itemData.NeedDia = (_itemData.NeedGood + m_iDiamondParm - 1) / m_iDiamondParm;

                    _itemData.GetList = new ObservableCollection<ItemIconDataModel>(_itemList);
                    if (m_dicCompensateName.ContainsKey(_itemData.Id))
                    {
                        _itemData.Name = m_dicCompensateName[_itemData.Id];
                    }

                    _list.Add(_itemData);
                }
            }
            if (_list.Count == 0)
            {
                RewardData.Compensate.IsEmpty = 1;
            }
            else
            {
                RewardData.Compensate.IsEmpty = 0;
            }
            PlayerDataManager.Instance.NoticeData.ActivityCompensateActive = _list.Count;
            RewardData.Compensate.ItemList = new ObservableCollection<ActivityCompensateItemDataModel>(_list);
        }

        private void EmployGiftCdKey(string code)
        {
            NetManager.Instance.StartCoroutine(EmployGiftCdKeyCoroutine(code));
        }

        private IEnumerator EmployGiftCdKeyCoroutine(string code)
        {
            var _msg = NetManager.Instance.UseGiftCode(code);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State != MessageState.Reply)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005000));
                yield break;
            }
            if (_msg.ErrorCode != 0)
            {
                UIManager.Instance.ShowNetError(5101);
            }
            else
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(271013));
            }
        }

        private void AggregateCompensationItem(int type)
        {
            var _newIitem = new ActivityCompensateItemDataModel();
            _newIitem.NeedGood = 0;
            _newIitem.NeedDia = 0;
            var _resDic = new Dictionary<int, int>();
            float _prop = 1;

            for (var i = 0; i < RewardData.Compensate.ItemList.Count; i++)
            {
                var _item = RewardData.Compensate.ItemList[i];
                _newIitem.NeedDia += _item.NeedDia;
                _newIitem.NeedGood += _item.NeedGood;
                for (var j = 0; j < _item.GetList.Count; j++)
                {
                    var tt = _item.GetList[j];
                    if (_resDic.ContainsKey(tt.ItemId))
                    {
                        _resDic[tt.ItemId] += tt.Count;
                    }
                    else
                    {
                        _resDic.Add(tt.ItemId, tt.Count);
                    }
                }
            }
            if (type == 2)
            {
                _prop = (float)m_iResParm / 10000;
            }
            else if (type == 3)
            {
                _prop = 1;
            }
            var _list = new List<ItemIconDataModel>();
            var _enumerator2 = (_resDic).GetEnumerator();
            while (_enumerator2.MoveNext())
            {
                var _item = _enumerator2.Current;
                {
                    var _ii = new ItemIconDataModel();
                    _ii.ItemId = _item.Key;
                    _ii.Count = (int)Math.Ceiling(_item.Value * _prop);
                    _list.Add(_ii);
                }
            }
            _newIitem.GetList = new ObservableCollection<ItemIconDataModel>(_list);
            RewardData.Compensate.SelectedItem = _newIitem;
        }


        private ActivityCompensateItemDataModel CopyActiveCompensationItem(ActivityCompensateItemDataModel item, float prop)
        {
            var _newIitem = new ActivityCompensateItemDataModel();
            _newIitem.Id = item.Id;
            _newIitem.LeftCount = item.LeftCount;
            _newIitem.Name = item.Name;
            _newIitem.NeedDia = item.NeedDia;
            _newIitem.NeedGood = item.NeedGood;
            for (var i = 0; i < item.GetList.Count; i++)
            {
                var _ii = new ItemIconDataModel();
                _ii.ItemId = item.GetList[i].ItemId;
                _ii.Count = (int)Math.Ceiling(item.GetList[i].Count * prop);
                _newIitem.GetList.Add(_ii);
            }
            return _newIitem;
        }


        private void CompensationOperation(int index)
        {
            switch (index)
            {
                case 0: //一键金币
                    if (RewardData.Compensate.ItemList.Count == 0)
                    {
                        return;
                    }
                    AggregateCompensationItem(2);
                    RewardData.Compensate.ShowWitchConfirm = 1;
                    m_iCompensateOkType = 0;
                    break;
                case 1: //一键钻石
                    if (RewardData.Compensate.ItemList.Count == 0)
                    {
                        return;
                    }
                    AggregateCompensationItem(3);
                    RewardData.Compensate.ShowWitchConfirm = 2;
                    m_iCompensateOkType = 1;
                    break;
                case 2: //确定补偿
                    var indexType = 0;
                    if (m_iCompensateOkType == 0)
                    {
                        indexType = -1;
                        m_iGoldOrDia = 0;
                    }
                    else if (m_iCompensateOkType == 1)
                    {
                        indexType = -1;
                        m_iGoldOrDia = 1;
                    }
                    else if (m_iCompensateOkType == 2)
                    {
                        indexType = RewardData.Compensate.SelectedItem.Id;
                    }
                    ReceivingCompensate(index, indexType, m_iGoldOrDia);

                    break;
                case 3: //取消
                    RewardData.Compensate.ShowWitchConfirm = 0;
                    break;
                default:
                    return;
            }
        }

        private void ReceivingCompensate(int index, int indexType, int GoldOrMoney)
        {
            if (GoldOrMoney == 0)
            {
                if (RewardData.Compensate.SelectedItem.NeedGood >
                    PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes))
                {
                    var _e = new ShowUIHintBoard(210100);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.GoldRes);
                    return;
                }
            }
            else if (GoldOrMoney == 1)
            {
                if (RewardData.Compensate.SelectedItem.NeedDia >
                    PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes))
                {
                    var _e = new ShowUIHintBoard(210102);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.DiamondRes);
                    return;
                }
            }
            NetManager.Instance.StartCoroutine(ReceivingCompensateCoroutine(index, indexType, GoldOrMoney));
        }

        private IEnumerator ReceivingCompensateCoroutine(int index, int indexType, int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ReceiveCompensation(indexType, type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _Compensate = RewardData.Compensate.ItemList;
                        RewardData.Compensate.ShowWitchConfirm = 0;
                        if (m_iCompensateOkType == 0)
                        {
                            PlayerDataManager.Instance.NoticeData.ActivityCompensateActive = 0;
                            RewardData.Compensate.IsEmpty = 1;
                            _Compensate.Clear();
                        }
                        else if (m_iCompensateOkType == 1)
                        {
                            PlayerDataManager.Instance.NoticeData.ActivityCompensateActive = 0;
                            RewardData.Compensate.IsEmpty = 1;
                            _Compensate.Clear();
                        }
                        else if (m_iCompensateOkType == 2)
                        {
                            _Compensate.RemoveAt(m_iCompensateSelectIndex);
                            PlayerDataManager.Instance.NoticeData.ActivityCompensateActive = _Compensate.Count;
                            if (_Compensate.Count == 0)
                            {
                                RewardData.Compensate.IsEmpty = 1;
                            }
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        #endregion

        #region Reward State

        private BitFlag FlagData
        {
            get { return PlayerDataManager.Instance.FlagData; }
        }
        private List<int> ExtData
        {
            get { return PlayerDataManager.Instance.ExtData; }
        }

        private int GetExData(int idx)
        {
            if (idx >= 0 && idx < ExtData.Count)
            {
                return ExtData[idx];
            }
            return 0;
        }

        private int AcquireOnLineSecond()
        {
            return GetExData(s_iTODAY_ONLINE_TIME_DATA_IDX) + Game.Instance.OnLineSeconds;
        }

        private eRewardState AcquireOnLineAwardState(int id)
        {
            var _table = Table.GetGift(id);
            if (0 != FlagData.GetFlag(_table.Flag))
            {
                return eRewardState.HasGot;
            }

            var _seconds = _table.Param[OnLineRewardParamterIndx.Minutes];
            var _todayOnlineTime = AcquireOnLineSecond();

            if (_todayOnlineTime >= _seconds)
            {
                return eRewardState.CanGet;
            }
            return eRewardState.CannotGet;
        }

        private eRewardState AcquireLevelAwardState(int id)
        {
            var _table = Table.GetGift(id);
            if (0 != FlagData.GetFlag(_table.Flag))
            {
                return eRewardState.HasGot;
            }
            var _level = PlayerDataManager.Instance.GetLevel();
            var _needLevel = _table.Param[LevelRewardParamterIndx.Level];
            if (_level >= _needLevel)
            {
                return eRewardState.CanGet;
            }
            return eRewardState.CannotGet;
        }

        private bool HasGotNowadaysLoginAward()
        {
            return 0 != FlagData.GetFlag(s_iCONTINUES_LOGIN_FLAG_IDX);
        }

        private int AcquireSuccessionLoginNumOfDays()
        {
            return GetExData(s_iCONTINUES_LOGIN_DATA_IDX);
        }

        private int AcquireMonthReportNum()
        {
            return GetExData(s_iMONTH_CHECKIN_DAYS_DATA_IDX);
        }

        private int AcquireMonthAgainReportNum()
        {
            return GetExData(s_iCONTINUES_RECHECKIN_DATA_IDX);
        }

        private bool HasNowdaysReport()
        {
            return 0 != FlagData.GetFlag(s_iTODAY_CHECKIN_FLAG_IDX);
        }

        private eRewardState AcquireMonthReportAwardState(int id)
        {
            var _table = Table.GetGift(id);
            if (0 != FlagData.GetFlag(_table.Flag))
            {
                return eRewardState.HasGot;
            }
            var _now = Game.Instance.ServerTime;
            var _day = _table.Param[MonthCheckinRewardParamterIndx.Day];
            if (_now.Day > _day)
            {
                return eRewardState.CannotGet;
            }

            return eRewardState.CanGet;
        }

        private int AcquireActiveProgress(int id)
        {
            var _table = Table.GetGift(id);
            var _idx = _table.Exdata;
            if (_idx >= 0 && _idx < ExtData.Count)
            {
                return GetExData(_idx);
            }
            return 0;
        }

        private int AcquireActiveScore()
        {
            return GetExData(s_iACTIVITY_SCORE_DATA_IDX);
        }

        private eRewardState AcquireActiveAwardState(int id)
        {
            var _table = Table.GetGift(id);
            if (0 != FlagData.GetFlag(_table.Flag))
            {
                return eRewardState.HasGot;
            }

            var _score = AcquireActiveScore();
            var _needScore = _table.Param[ActivityRewardParamterIndx.NeedScore];
            if (_score > 0 && _score >= _needScore)
            {
                return eRewardState.CanGet;
            }
            return eRewardState.CannotGet;
        }

        private void SendClaimAwardRequesting(int type, int id)
        {
            GameLogic.Instance.StartCoroutine(ClaimAwardCoroutine(type, id));
        }

        private IEnumerator ClaimAwardCoroutine(int type, int id)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Debug(".............ClaimRewardCoroutine..................begin");
                var _msg = NetManager.Instance.ActivationReward(type, id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State != MessageState.Reply)
                {
                    Logger.Debug("[ClaimRewardCoroutine] msg.State != MessageState.Reply");
                    yield break;
                }

                if (_msg.ErrorCode != (int)ErrorCodes.OK)
                {
                    Logger.Debug("[ClaimRewardCoroutine] ErrorCodes=[{0}]", _msg.ErrorCode);
                    if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000006));
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 }));                            
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip((ErrorCodes)_msg.ErrorCode));
                    }
                    yield break;
                }
            }
            //AnalyseNotice();

            //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(452));

            Logger.Debug(".............ClaimRewardCoroutine..................end");
        }

        private void ClaimOnLineAward(int id)
        {
            var _state = AcquireOnLineAwardState(id);
            if (eRewardState.CanGet != _state)
            {
                return;
            }

            SendClaimAwardRequesting((int)eActivationRewardType.TableGift, id);
        }

        private void CliamLvAward(int id)
        {
            var _state = AcquireLevelAwardState(id);
            if (eRewardState.CanGet != _state)
            {
                return;
            }

            SendClaimAwardRequesting((int)eActivationRewardType.TableGift, id);
        }

        private void ClaimSuccessionLoginAward()
        {
            if (HasGotNowadaysLoginAward())
            {
                Logger.Debug("HasGotTodayLoginReward");
                return;
            }

            var _days = AcquireSuccessionLoginNumOfDays();
            var _maxDay = 0;
            GiftRecord temp = null;
            Table.ForeachGift(table =>
            {
                if ((eRewardType)table.Type == eRewardType.ContinuesLoginReward)
                {
                    var _needDays = table.Param[ContinuesLoginRewardParamterIndx.Days];
                    if (_days >= _needDays)
                    {
                        if (_needDays > _maxDay)
                        {
                            _maxDay = _needDays;
                            temp = table;
                        }
                        return false;
                    }
                }
                return true;
            });

            if (null != temp)
            {
                SendClaimAwardRequesting((int)eActivationRewardType.TableGift, temp.Id);
            }
        }

        private void CheckinNowdays()
        {
            if (HasNowdaysReport())
            {
                return;
            }
            var _days = AcquireMonthReportNum();
            var _checkinDay = _days + 1;
            Table.ForeachGift(table =>
            {
                if ((eRewardType)table.Type == eRewardType.MonthCheckinReward)
                {
                    if (_checkinDay != table.Param[MonthCheckinRewardParamterIndx.Day])
                    {
                        return true;
                    }
                    var _month = table.Param[MonthCheckinRewardParamterIndx.Month];
                    if (999 != _month && _month != Game.Instance.ServerTime.Month)
                    {
                        return true;
                    }
                    SendClaimAwardRequesting((int)eActivationRewardType.TableGift, table.Id);
                    return false;
                }
                return true;
            });
        }

        private void BeginCheckinNowdays()
        {
            if (!HasNowdaysReport())
            {
                return;
            }

            var _days = AcquireMonthReportNum();
            var _checkinDay = _days + 1;
            Table.ForeachGift(table =>
            {
                if ((eRewardType)table.Type == eRewardType.MonthCheckinReward)
                {
                    if (_checkinDay != table.Param[MonthCheckinRewardParamterIndx.Day])
                    {
                        return true;
                    }
                    var _month = table.Param[MonthCheckinRewardParamterIndx.Month];
                    if (999 != _month && _month != Game.Instance.ServerTime.Month)
                    {
                        return true;
                    }
                    //var diamond = table.Param[4] + GetMonthReCheckinTimes() * table.Param[5];
                    //int diamond = table.Param[4] * SkillExtension.Pow(table.Param[5], GetMonthReCheckinTimes());
                    var _diamond =
                        (int)(table.Param[4] * SkillExtension.Pow(table.Param[5] / 10000.0f, AcquireMonthAgainReportNum()));
                    _diamond = _diamond - _diamond % 5;
                    var _str = string.Format(GameUtils.GetDictionaryText(530), _diamond);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                        _str,
                        GameUtils.GetDictionaryText(1503),
                        () => { SendClaimAwardRequesting((int)eActivationRewardType.TableGift, table.Id); });

                    return false;
                }
                return true;
            });
        }

        private void CliamActiveAward(int id)
        {
            var _state = AcquireActiveAwardState(id);
            if (eRewardState.CanGet != _state)
            {
                return;
            }

            SendClaimAwardRequesting((int)eActivationRewardType.TableGift, id);
        }

        private static void InitionTableLater_AnalysedNotice()
        {
            s_dicGiftList.Clear();
            Table.ForeachGift(recoard =>
            {
                var _type = (eRewardType)recoard.Type;
                if (eRewardType.OnlineReward == _type)
                {
                    List<GiftRecord> _list;
                    if (!s_dicGiftList.TryGetValue(_type, out _list))
                    {
                        _list = new List<GiftRecord>();
                        s_dicGiftList[_type] = _list;
                    }
                    _list.Add(recoard);
                }
                else if (eRewardType.LevelReward == _type)
                {
                    List<GiftRecord> _list;
                    if (!s_dicGiftList.TryGetValue(_type, out _list))
                    {
                        _list = new List<GiftRecord>();
                        s_dicGiftList[_type] = _list;
                    }
                    _list.Add(recoard);
                }
                else if (eRewardType.ContinuesLoginReward == _type)
                {
                    List<GiftRecord> _list;
                    if (!s_dicGiftList.TryGetValue(_type, out _list))
                    {
                        _list = new List<GiftRecord>();
                        s_dicGiftList[_type] = _list;
                    }
                    _list.Add(recoard);
                }
                else if (eRewardType.MonthCheckinReward == _type)
                {
                    List<GiftRecord> _list;
                    if (!s_dicGiftList.TryGetValue(_type, out _list))
                    {
                        _list = new List<GiftRecord>();
                        s_dicGiftList[_type] = _list;
                    }
                    _list.Add(recoard);
                }
                else if (eRewardType.DailyActivityReward == _type)
                {
                    List<GiftRecord> _list;
                    if (!s_dicGiftList.TryGetValue(_type, out _list))
                    {
                        _list = new List<GiftRecord>();
                        s_dicGiftList[_type] = _list;
                    }
                    _list.Add(recoard);
                }
                if (recoard.Exdata != -1)
                {
                    List<eRewardType> _list;
                    if (s_dicExdataGift.TryGetValue(recoard.Exdata, out _list))
                    {
                        if (!_list.Contains(_type))
                        {
                            _list.Add(_type);
                        }
                    }
                    else
                    {
                        _list = new List<eRewardType>();
                        s_dicExdataGift[recoard.Exdata] = _list;
                        _list.Add(_type);
                    }
                }
                if (recoard.Flag != -1)
                {
                    List<eRewardType> _list;
                    if (s_dicFlagGift.TryGetValue(recoard.Flag, out _list))
                    {
                        if (!_list.Contains(_type))
                        {
                            _list.Add(_type);
                        }
                    }
                    else
                    {
                        _list = new List<eRewardType>();
                        s_dicFlagGift[recoard.Flag] = _list;
                        _list.Add(_type);
                    }
                }
                return true;
            });
        }

        private void AnalysedNotice()
        {
            if (ExtData.Count == 0)
            {
                return;
            }
            int[] _count = { 0, 0, 0, 0, 0, 0, 0, 0 };

            Table.ForeachGift(recoard =>
            {
                var _type = (eRewardType)recoard.Type;

                if (eRewardType.OnlineReward == _type)
                {
                    if (AcquireOnLineAwardState(recoard.Id) == eRewardState.CanGet)
                    {
                        _count[recoard.Type]++;
                    }
                }
                else if (eRewardType.LevelReward == _type)
                {
                    if (AcquireLevelAwardState(recoard.Id) == eRewardState.CanGet)
                    {
                        _count[recoard.Type]++;
                    }
                }
                else if (eRewardType.ContinuesLoginReward == _type)
                {
                    if (!HasGotNowadaysLoginAward())
                    {
                        _count[recoard.Type] = 1;
                    }
                }
                else if (eRewardType.MonthCheckinReward == _type)
                {
                    if (!HasNowdaysReport() && AcquireMonthReportNum() < 25)
                    {
                        _count[recoard.Type] = 1;
                    }
                }
                else if (eRewardType.DailyActivityReward == _type)
                {
                    if (AcquireActiveAwardState(recoard.Id) == eRewardState.CanGet)
                    {
                        _count[recoard.Type]++;
                    }
                }

                return true;
            });

            var _noticeData = PlayerDataManager.Instance.NoticeData;
            _noticeData.ActivityLevel = _count[(int)eRewardType.LevelReward];
            _noticeData.ActivityLoginSeries = _count[(int)eRewardType.ContinuesLoginReward];
            _noticeData.ActivityTimeLength = _count[(int)eRewardType.OnlineReward];
            _noticeData.ActivityLoginAddup = _count[(int)eRewardType.MonthCheckinReward];
            _noticeData.ActivityDailyActive = _count[(int)eRewardType.DailyActivityReward];
        }

        private void AnalysedNotice_RenewalType(eRewardType type, List<GiftRecord> recordList)
        {
            if (ExtData.Count == 0)
            {
                return;
            }
            var _count = 0;
            {
                var _list1 = recordList;
                var _listCount1 = _list1.Count;
                for (var _i1 = 0; _i1 < _listCount1; ++_i1)
                {
                    var _recoard = _list1[_i1];
                    {
                        var _TbType = (eRewardType)_recoard.Type;
                        if (type != _TbType)
                        {
                            continue;
                        }
                        if (eRewardType.OnlineReward == type)
                        {
                            if (AcquireOnLineAwardState(_recoard.Id) == eRewardState.CanGet)
                            {
                                _count++;
                            }
                        }
                        else if (eRewardType.LevelReward == type)
                        {
                            if (AcquireLevelAwardState(_recoard.Id) == eRewardState.CanGet)
                            {
                                _count++;
                            }
                        }
                        else if (eRewardType.ContinuesLoginReward == type)
                        {
                            if (!HasGotNowadaysLoginAward())
                            {
                                _count = 1;
                            }
                        }
                        else if (eRewardType.MonthCheckinReward == type)
                        {
                            if (!HasNowdaysReport() && AcquireMonthReportNum() < 25) //今日没签到并且还没签满25次
                            {
                                _count = 1;
                            }
                        }
                        else if (eRewardType.DailyActivityReward == type)
                        {
                            if (AcquireActiveAwardState(_recoard.Id) == eRewardState.CanGet)
                            {
                                _count++;
                            }
                        }
                    }
                }
            }
            var _noticeData = PlayerDataManager.Instance.NoticeData;
            var _playerData = PlayerDataManager.Instance;
            switch (type)
            {
                case eRewardType.Invalid:
                    break;
                case eRewardType.GiftBag:
                    break;
                case eRewardType.OnlineReward:
                    _noticeData.ActivityTimeLength = _count;
                    break;
                case eRewardType.LevelReward:
                    _noticeData.ActivityLevel = _count;
                    break;
                case eRewardType.ContinuesLoginReward:
                    _noticeData.ActivityLoginSeries = _count;
                    break;
                case eRewardType.MonthCheckinReward:
                    _noticeData.ActivityLoginAddup = _count;
                    break;
                case eRewardType.DailyActivity:
                    break;
                case eRewardType.DailyActivityReward:
                    _noticeData.ActivityDailyActive = _count;
                    break;
                default:
                    Logger.Error("AnalyseNotice_RefreshType {0}", type);
                    break;
            }
        }

        private void OnAnalysedNoticeByFlagedEvent(int index)
        {
            List<eRewardType> _typeList;
            if (!s_dicFlagGift.TryGetValue(index, out _typeList))
            {
                return;
            }
            {
                var _list2 = _typeList;
                var _listCount2 = _list2.Count;
                for (var _i2 = 0; _i2 < _listCount2; ++_i2)
                {
                    var _type = _list2[_i2];
                    {
                        List<GiftRecord> _recordList;
                        if (!s_dicGiftList.TryGetValue(_type, out _recordList))
                        {
                            return;
                        }
                        AnalysedNotice_RenewalType(_type, _recordList);
                    }
                }
            }
        }

        private void OnAnalysedNoticeExdataEvent(int index)
        {
            List<eRewardType> _typeList;
            if (!s_dicExdataGift.TryGetValue(index, out _typeList))
            {
                return;
            }
            {
                var _list3 = _typeList;
                var _listCount3 = _list3.Count;
                for (var _i3 = 0; _i3 < _listCount3; ++_i3)
                {
                    var _type = _list3[_i3];
                    {
                        List<GiftRecord> _recordList;
                        if (!s_dicGiftList.TryGetValue(_type, out _recordList))
                        {
                            return;
                        }
                        AnalysedNotice_RenewalType(_type, _recordList);
                    }
                }
            }

        }

        #endregion

        #endregion

        #region 事件函数

        private void OnAnalysedNoticeInitionEvent(IEvent ievent)
        {
            AnalysedNotice();
        }
        private void OnClickAwardItemEvent(IEvent ievent)
        {
            var _e = ievent as RewardActivityItemClickEvent;
            RewardData.Tab = _e.Idx;
        }
        //在线奖励
        private void RenewalOnLineAwardEvent(IEvent ievent)
        {
            RenewalOnLineAward();
        }
        //等级奖励
        private void OnRenewalLevelAwardEvent(IEvent ievent)
        {
            OnRenewalLevelAwardEvent();
        }
        //每月签到
        private void OnRenewalSignInMonthAwardEvent(IEvent ievent)
        {
            OnRenewalSignInMonthAwardEvent();
        }
        //补偿奖励
        private void OnClickCompensateEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_ActivityCompensateItem;
            m_iCompensateSelectIndex = _e.Idx;
            m_iCompensateOkType = 2;
            float _prop = 1;
            if (_e.Type == 2)
            {
                _prop = (float)m_iResParm / 10000;
                m_iGoldOrDia = 0;
                RewardData.Compensate.ShowWitchConfirm = 1;
            }
            else if (_e.Type == 3)
            {
                _prop = 1;
                m_iGoldOrDia = 1;
                RewardData.Compensate.ShowWitchConfirm = 2;
            }
            RewardData.Compensate.SelectedItem = CopyActiveCompensationItem(RewardData.Compensate.ItemList[_e.Idx], _prop);
        }

        private void OnEmployGiftCdKeyEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UseGiftCodeEvent;
            EmployGiftCdKey(_e.Code);
        }
        private void OnClickUseOfflineItemEvent(IEvent ievent)
        {
            if (RewardData == null || RewardData.OfflineTimeItem == null || RewardData.OfflineTimeItem.ItemId < 0)
            {
                return;
            }

            var leftTime = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e742);
            var vipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
            var tbVip = Table.GetVIP(vipLevel);
            if (tbVip == null)
            {
                return;
            }
            var maxValue = tbVip.OfflineTimeMax;
            if (leftTime >= maxValue)
            {
                GameUtils.ShowHintTip(200009004);
                return;
            }

            var bagItem = PlayerDataManager.Instance.GetBagItemByItemId((int)eBagType.BaseItem, RewardData.OfflineTimeItem.ItemId);
            if (bagItem == null)
            {
                GameUtils.CheckEnoughItems(RewardData.OfflineTimeItem.ItemId, 1);
                GameUtils.ShowHintTip(270002);
                return;
            }

            GameUtils.UseItem(bagItem);
        }

        #region 小红点

        private void OnAnalysedNoticeByFlagedEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            OnAnalysedNoticeByFlagedEvent(_e.Index);
        }

        private void OnAnalysedNoticeExdataEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            OnAnalysedNoticeExdataEvent(_e.Key);
            if (_e.Key == 778)
            {
                RenewalMonthCard();
            }
            if (_e.Key == 779)
            {
                RenewalWeekCard();
            }
        }

        //     public void AnalyseNoticeByFlag(int index)
        //     {
        //         AnalyseNoticeByFlag(index);
        //     }
        // 
        //     public void AnalyseNoticeByExdata(int index)
        //     {
        //         AnalyseNoticeByExdata(index);
        //     }

        #endregion
        #region 事件

        //当标记位变化时
        private void OnFlagRenewalEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            var _idx = _e.Index;

            if (s_iCONTINUES_LOGIN_FLAG_IDX == _idx)
            {
                RenewalContinuouslyLoginAward();
                return;
            }
            if (s_iTODAY_CHECKIN_FLAG_IDX == _idx)
            {
                OnRenewalSignInMonthAwardEvent();
                return;
            }
            if (2507 == _idx)
            {
                RenewalMonthCard();
                return;
            }
            if (s_lifeCardFlag_IDX == _idx || s_lifeCardRechargeFlag_IDX == _idx)
            {
                RefreshLifeCard();
                return;
            }
            if (2511 == _idx)
            {
                RenewalWeekCard();
                return;
            }

            List<GiftRecord> _list = null;
            if (!m_dicFlagDataDict.TryGetValue(_idx, out _list))
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
                        var _type = (eRewardType)_table.Type;
                        if (eRewardType.OnlineReward == _type)
                        {
                            RenewalOnLineAward();
                        }
                        else if (eRewardType.LevelReward == _type)
                        {
                            OnRenewalLevelAwardEvent();
                        }
                        else if (eRewardType.ContinuesLoginReward == _type)
                        {
                            RenewalContinuouslyLoginAward();
                        }
                        else if (eRewardType.DailyActivityReward == _type)
                        {
                            RenewalActiveAward();
                        }
                    }
                }
            }
        }


        //当扩展数据变化时
        private void OnExtDataUpInfoEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            if (_e == null)
            {
                return;
            }

            var _idx = _e.Key;
            if (s_iTODAY_ONLINE_TIME_DATA_IDX == _idx)
            {
                RenewalOnLineAward();
            }
            else if (s_iCONTINUES_LOGIN_DATA_IDX == _idx)
            {
                RenewalContinuouslyLoginAward();
            }
            else if (s_iMONTH_CHECKIN_DAYS_DATA_IDX == _idx)
            {
                OnRenewalSignInMonthAwardEvent();
            }
            else if (s_iACTIVITY_SCORE_DATA_IDX == _idx)
            {
                RenewalActiveAward();
            }

            if (_e.Key == (int)eExdataDefine.e742)
            {
                RefreshOfflineTime();
                RefreshOfflineItem();
            }

            //List<GiftRecord> list = null;
            //if (!mExtDataDict.TryGetValue(idx, out list))
            //    return;
            //{
            //    var __list9 = list;
            //    var __listCount9 = __list9.Count;
            //    for (int __i9 = 0; __i9 < __listCount9; ++__i9)
            //    {
            //        var table = __list9[__i9];
            //        {
            //            var type = (eRewardType)table.Type;
            //            if (eRewardType.DailyActivity == type)
            //            {
            //                UpdateActivity();
            //            }
            //        }
            //    }
            //}
        }

        private void OnCliamAwardEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_CliamReward;
            if (null == _e)
            {
                return;
            }

            var _type = _e.RewardType;
            var _idx = _e.Idx;

            if (_type == UIEvent_CliamReward.Type.OnLine)
            {
                ClaimOnLineAward(_idx);
            }
            else if (_type == UIEvent_CliamReward.Type.Level)
            {
                CliamLvAward(_idx);
            }
            else if (_type == UIEvent_CliamReward.Type.CheckinToday)
            {
                CheckinNowdays();
            }
            else if (_type == UIEvent_CliamReward.Type.ReCheckinToday)
            {
                BeginCheckinNowdays();
            }
            else if (_type == UIEvent_CliamReward.Type.ClaimContinuesLoginReward)
            {
                ClaimSuccessionLoginAward();
            }
            else if (_type == UIEvent_CliamReward.Type.Activity)
            {
                CliamActiveAward(_idx);
            }
            else if (_type == UIEvent_CliamReward.Type.Compensate)
            {
                CompensationOperation(_idx);
            }
        }

        #endregion
        //Reward State
        private void OnGAcquireOnLineSecondEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_GetOnLineSeconds;
            if (null != _e)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_UpdateOnLineSeconds(AcquireOnLineSecond()));
            }
        }

        private void GetLifeCardReward(IEvent ievent)
        {
            var e = ievent as GetLifeCardRewardEvent;
            if (null == e)
                return;
            GameLogic.Instance.StartCoroutine(GetLifeCardRewardCoroutine());
        }

        private IEnumerator GetLifeCardRewardCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ActivationReward((int)eActivationRewardType.LifeCard, 41);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State != MessageState.Reply)
                {
                    Logger.Debug("[ClaimRewardCoroutine] msg.State != MessageState.Reply");
                    yield break;
                }

                if (msg.ErrorCode != (int)ErrorCodes.OK)
                {
                    Logger.Debug("[ClaimRewardCoroutine] ErrorCodes=[{0}]", msg.ErrorCode);
                    if (msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000006));
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip((ErrorCodes)msg.ErrorCode));
                    }
                    yield break;
                }
            }

            Logger.Debug(".............ClaimRewardCoroutine..................end");
        }

        private void GetMonthCard(IEvent ievent)
        {
            GameLogic.Instance.StartCoroutine(GetMonthCardCoroutine());
        }
        private IEnumerator GetMonthCardCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ActivationReward((int)eActivationRewardType.MonthCard, 2001);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State != MessageState.Reply)
                {
                    Logger.Debug("[ClaimRewardCoroutine] msg.State != MessageState.Reply");
                    yield break;
                }

                if (msg.ErrorCode != (int) ErrorCodes.OK)
                {
                    Logger.Debug("[ClaimRewardCoroutine] ErrorCodes=[{0}]", msg.ErrorCode);
                    if (msg.ErrorCode == (int) ErrorCodes.Error_ItemNoInBag_All)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000006));
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip((ErrorCodes) msg.ErrorCode));
                    }
                    yield break;
                }
            }

            Logger.Debug(".............ClaimRewardCoroutine..................end");
        }

        private void GetWeekCard(IEvent ievent)
        {
            GameLogic.Instance.StartCoroutine(GetWeekCardCoroutine());
        }
        private IEnumerator GetWeekCardCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ActivationReward((int)eActivationRewardType.WeekCard, 2002);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State != MessageState.Reply)
                {
                    Logger.Debug("[ClaimRewardCoroutine] msg.State != MessageState.Reply");
                    yield break;
                }

                if (msg.ErrorCode != (int)ErrorCodes.OK)
                {
                    Logger.Debug("[ClaimRewardCoroutine] ErrorCodes=[{0}]", msg.ErrorCode);
                    if (msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000006));
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip((ErrorCodes)msg.ErrorCode));
                    }
                    yield break;
                }
            }

            Logger.Debug(".............ClaimRewardCoroutine..................end");
        }



        private void OnGetOfflineExp(IEvent ievent)
        {
            var controller = UIManager.Instance.GetController(UIConfig.NewOfflineExpFrame);
            if (controller == null) return;
            NewOffLineExpDataModel offExp_DM = controller.GetDataModel("") as NewOffLineExpDataModel;
            if (offExp_DM == null) return;

            var cost = offExp_DM.Cost;
            if (cost > PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes))
            {
                var _ee = new ShowUIHintBoard(210102);
                EventDispatcher.Instance.DispatchEvent(_ee);

                var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            NetManager.Instance.StartCoroutine(GetOfflineExpCoroutine());
        }
        private IEnumerator GetOfflineExpCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.CSApplyOfflineExpData(2);//只能用钻石
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var controller = UIManager.Instance.GetController(UIConfig.NewOfflineExpFrame);
                        if (controller == null)
                            yield return null;
                        NewOffLineExpDataModel offExp_DM = controller.GetDataModel("") as NewOffLineExpDataModel;
                        if (offExp_DM == null)
                            yield return null;
                        offExp_DM.IsShow = 0;
                        //EventDispatcher.Instance.DispatchEvent(new InitUI_Event(false));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        yield break;
                    }

                }
            }
        }
        #endregion
      
    }
}