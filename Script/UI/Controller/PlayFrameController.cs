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
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion


namespace ScriptController
{
    public class PlayFrameController : IControllerBase
    {   
        bool isInit = false;
        private ItemIdDataModel ItemDataModel;
        #region 初始化

        public PlayFrameController()
        {
            EventDispatcher.Instance.AddEventListener(UIEvent_PlayFrameTabSelectEvent.EVENT_TYPE, OnTabSelected);
            EventDispatcher.Instance.AddEventListener(UIEvent_PlayFrameRewardClick.EVENT_TYPE, OnRewardClick);
            EventDispatcher.Instance.AddEventListener(UIEvent_OnClickGotoActivity.EVENT_TYPE, OnClickGotoActivity);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInit);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpdate);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagUpdate);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnLevelUp);
            CleanUp();
        }

        #endregion

        #region 数据

        public FrameState State { get; set; }
    
        private PlayFrameDataModel DataModel;
        private readonly Dictionary<int, List<DailyActivity>> DailyActs =
            new Dictionary<int, List<DailyActivity>>();

        //如果这些flag发生变化，就刷新Cells.
        //TabIdx => flag ids
        private readonly Dictionary<int, List<int>> CellFlags = new Dictionary<int, List<int>>();

        //如果这些exdata发生变化，就刷新Cells.
        //TabIdx => exdata ids
        private readonly Dictionary<int, List<int>> CellExdatas = new Dictionary<int, List<int>>();

        //缓存各个类型奖励表格数据
        private readonly List<GiftRecord> GiftRecords = new List<GiftRecord>();

        //如果这些flag发生变化，就刷新活跃度奖励.
        private readonly List<int> ActivityGiftFlags = new List<int>();

        //计时器对象
        private object RefreshTimer;

        private List<DateTime> CurrentTimeSpan = new List<DateTime>();
        private List<PlayFrameCellData> CurrentCellData = new List<PlayFrameCellData>();

        private int TabIdx
        {
            get { return DataModel.TabIdx; }
            set
            {
                if (DataModel.TabIdx == value)
                {
                    return;
                }
                DataModel.TabIdx = value;
                RefreshCells(DataModel.TabIdx);
            }
        }

        #endregion

        #region IControllerBase 接口

        public void CleanUp()
        {
            DataModel = new PlayFrameDataModel();

            GiftRecords.Clear();
            //如果没有达到条件，不会有日常活跃奖励
            if (PlayerDataManager.Instance.GetFlag(119) == true)
            {
                Table.ForeachGift(record =>
                {
                    var type = (eRewardType)record.Type;
                    if (eRewardType.DailyActivityReward == type)
                    {
                        GiftRecords.Add(record);
                        ActivityGiftFlags.Add(record.Flag);
                    }
                    return true;
                });
            }

            var rewards = DataModel.Rewards;
            for (int i = 0, imax = GiftRecords.Count; i < imax; ++i)
            {
                var record = GiftRecords[i];
                var reward = rewards[i];
                reward.GiftId = record.Id;
                reward.ItemId = record.Param[ActivityRewardParamterIndx.ItemId];
                reward.ItemCount = record.Param[ActivityRewardParamterIndx.Count];
            }
        }

        public void OnShow()
        {
            RefreshCells(DataModel.TabIdx);
            RefreshRedPoint();
        }

        private void RefreshRedPoint()
        {
            if (IsShowDailyRedPoint())
            {
                PlayerDataManager.Instance.NoticeData.DailyActivityFlag = true;
                DataModel.IsShowDailyNotice = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.DailyActivityFlag = false;
                DataModel.IsShowDailyNotice = false;
            }
        }


        public void Close()
        {
            isInit = false;
            CurrentCellData.Clear ();
            CurrentTimeSpan.Clear ();
            NetManager.Instance.StopCoroutine(UpDateTime());
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            if (data != null)
            {
                TabIdx = data.Tab;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "RefreshCells")
            {
                RefreshCells((int) PlayFrameTab.DailyActivity);
            }
            return null;
        }

        #endregion

        #region 事件响应

        private void OnTabSelected(IEvent ievent)
        {
            var e = ievent as UIEvent_PlayFrameTabSelectEvent;
            TabIdx = e.TabIdx;
        }

        private void OnRewardClick(IEvent ievent)
        {
            var e = ievent as UIEvent_PlayFrameRewardClick;
            var reward = DataModel.Rewards[e.Index];
            if (reward.CanGet && !reward.HasGot)
            {
                ActivationReward((int) eActivationRewardType.TableGift, reward.GiftId);
            }
            else
            {
                var e1 = new UIEvent_ClickChest(reward.ItemId);
                EventDispatcher.Instance.DispatchEvent(e1);
            }
        }

        private void ActivationReward(int type, int id)
        {
            GameLogic.Instance.StartCoroutine(ActivationRewardCoroutine(type, id));
        }

        private IEnumerator ActivationRewardCoroutine(int type, int id)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Debug(".............ClaimRewardCoroutine..................begin");
                var msg = NetManager.Instance.ActivationReward(type, id);
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

        private void OnClickGotoActivity(IEvent ievent)
        {
            var e = ievent as UIEvent_OnClickGotoActivity;
            var cell = DataModel.Cells[e.Idx];
            var textId = cell.TextId;
            var tbDA = Table.GetDailyActivity(cell.TableId);
        
            if (tbDA.DetailType == (int) eActivityType.VipDailyGift)
            {
//vip每日礼包
                if (textId == 100000894)
                {
                    ActivationReward((int) eActivationRewardType.DailyVipGift, cell.TableId);
                }
                else if (textId == 100000896)
                {
                    //UIID 79：充值和vip界面；tab 0：充值tab页
                    GameUtils.GotoUiTab(79, 0);
                }
            }
            else if (tbDA.DetailType == (int) eActivityType.MonthCard)
            {
//月卡
                if (textId == 100000894)
                {
                    ActivationReward((int) eActivationRewardType.MonthCard, cell.TableId);
                }
                else if (textId == 100000896)
                {
                    //UIID 79：充值和vip界面；tab 0：充值tab页
                    GameUtils.GotoUiTab(79, 0);
                }
            }
            else
            {
                GameUtils.GotoUiTab(tbDA.UIId, tbDA.UITab, tbDA.UIId2,1);//最后参数1:是用于判断日常活跃是否返回二级界面
            }
        }

        private void OnExDataInit(IEvent ievent)
        {
            RefreshActivityRewards(true);
            RefreshDailyActivitys(true);
            RefreshCells((int) PlayFrameTab.DailyActivity, true);
        }

        private void OnExDataUpdate(IEvent ievent)
        {
            var e = ievent as ExDataUpDataEvent;
            if (e.Key == (int) eExdataDefine.e15)
            {
                RefreshActivityRewards();
            }
            if (State == FrameState.Close)
            {
                return;
            }
            var exdatas = CellExdatas[TabIdx];
            if (exdatas.Contains(e.Key))
            {
                RefreshCells(DataModel.TabIdx);
            }
        }

        private void OnFlagUpdate(IEvent ievent)
        {
            var e = ievent as FlagUpdateEvent;
            if (ActivityGiftFlags.Contains(e.Index))
            {
                RefreshActivityRewards();
            }
            if (State == FrameState.Close)
            {
                return;
            }
            var flags = CellFlags[TabIdx];
            if (flags.Contains(e.Index))
            {
                RefreshCells(DataModel.TabIdx);
            }
        }

        private void OnLevelUp(IEvent ievent)
        {
            RefreshDailyActivitys();
            if (State == FrameState.Close)
            {
                return;
            }
            var e = ievent as Event_LevelUp;
            RefreshCells(DataModel.TabIdx);
        }

        #endregion

        #region 私有逻辑

        private void RefreshDailyActivitys(bool isInit=false)
        {
            var playerData = PlayerDataManager.Instance;

            DailyActs.Clear();
            CellFlags.Clear();
            CellExdatas.Clear();
            for (var type = PlayFrameTab.DailyActivity; type < PlayFrameTab.Count; type++)
            {
                var t = (int) type;
                DailyActs.Add(t, new List<DailyActivity>());
                CellFlags.Add(t, new List<int>());
                CellExdatas.Add(t, new List<int>());
            }

            Table.ForeachDailyActivity(record =>
            {           
                var type = record.Type;
                DailyActivity a = new DailyActivity();
                a.dailyActivityRecord = record;
                a.state = 0;
                var result = playerData.CheckCondition(record.OpenCondition);
                if (result != 0)
                {
                    result = playerData.CheckCondition(record.WillOpenCondition);
                    if (result != 0)
                    {
                        return true;
                    }
                    a.state = (int)PlayFrameTab.NoToDpen; ;
                    //type = (int)PlayFrameTab.AboutToOpen;
                }

                List<DailyActivity> records;
                if (!DailyActs.TryGetValue(type, out records))
                {
                    return true;
                }
                records.Add(a);
                return true;
            });

            foreach (var dailyAct in DailyActs)
            {
                var tabIdx = dailyAct.Key;
                var records = dailyAct.Value;
                var fs = CellFlags[tabIdx];
                var es = CellExdatas[tabIdx];
                foreach (var record in records)
                {
                    var param0 = record.dailyActivityRecord.CommonParam[0];
                    var type = (eActivityType)record.dailyActivityRecord.DetailType;
                    es.Add(record.dailyActivityRecord.ExDataId);
                    switch (type)
                    {
                        case eActivityType.VipDailyGift:
                        case eActivityType.MonthCard:
                        {
                            fs.Add(param0);
                        }
                            break;
                    }
                }
            }

            RefreshTimeLimitedActivity(isInit);
        }

        private void RefreshCells(int tabIdx, bool isInit = false)
        {
            if (tabIdx == (int) PlayFrameTab.TimedActivity)
            {
                RefreshTimeLimitedActivity();
                return;
            }

            List<DailyActivity> records;
            if (!DailyActs.TryGetValue(tabIdx, out records))
            {
                return;
            }

            var cells = new List<PlayFrameCellData>();
            foreach (var record in records)
            {
                var args = new CreateCellDataArgs();
                args.nowCount = 0;
                args.maxCount = 0;
                args.state = ActivityCellState.Start;
                args.textId = 100000613; //参加
                var _tbDavi = Table.GetDailyActivity(record.dailyActivityRecord.Id);
                if (!GameUtils.CheckIsWeekLoopOk(_tbDavi.WeekLoop))
                {
                    continue;
                }
                var cell = CreateCellData(record, args);
                cells.Add(cell);
                cell.IsRewardShow1 = 1;
                cell.IsRewardShow2 = 1;
                cell.IsRewardShow3 = 1;

                for (int i = 0, imax = _tbDavi.RewardShow.Count; i < imax; i++)
                {               
                    if (i == 0)
                    {
                    
                        cell.RewardId1 = _tbDavi.RewardShow[i];
                        cell.IsRewardShow1 = 0;
                    }

                    if (i == 1)
                    {
                        cell.RewardId2 = _tbDavi.RewardShow[i];
                        cell.IsRewardShow2 = 0;
                    }
                    if (i == 2)
                    {
                        cell.RewardId3 = _tbDavi.RewardShow[i];
                        cell.IsRewardShow3 = 0;
                    }           
                }
                if (/*tabIdx == (int) PlayFrameTab.AboutToOpen*/record.state == (int)PlayFrameTab.NoToDpen)
                {
                    cell.State = (int)ActivityCellState.WillStart;
                    cell.Reason = record.dailyActivityRecord.Desc;
                }
                if (cell.BtnId == -1)
                {
                    switch (cell.State)
                    {
                        case 0:
                            //参加
                            cell.BtnId = 1;
                            break;
                        case 1:
                            //等级不够,未开启    
                            cell.ShowColor = 2;
                            cell.BtnId = 2;
                            break;
                        case 2:
                            //领取奖励
                            cell.ShowColor = 1;
                            cell.BtnId = 4;
                            break;
                        case 4:
                            //完成
                            cell.ShowColor = 1;
                            cell.BtnId = 3;
                            break;
                    }
                }          
            }

            cells.Sort((l, r) =>
            {
                if (l.State < r.State)
                {
                    return -1;
                }
                if (l.State > r.State)
                {
                    return 1;
                }

                if (l.SortPriority < r.SortPriority)
                {
                    return -1;
                }
                if (l.SortPriority > r.SortPriority)
                {
                    return 1;
                }

                return 0;
            });
            DataModel.Cells = new ObservableCollection<PlayFrameCellData>(cells);

            if (cells.Count == 0)
            {
                DataModel.EmptyTipDic = tabIdx + 100000900;
            }
            else
            {
                DataModel.EmptyTipDic = -1;
            }

            if (tabIdx == (int) PlayFrameTab.DailyActivity)
            {
                var count = cells.Count(c => c.State == (int) ActivityCellState.Start);
                //DataModel.IsShowDailyNotice = count > 0;日常按钮小红点改为活跃度奖励未领取时显示
                if (isInit)
                {
                    var showReward = IsShowDailyRedPoint();
                    PlayerDataManager.Instance.NoticeData.DailyActivityFlag = DataModel.IsShowTimeLimitNotice ||
                                                                              DataModel.IsShowDailyNotice || showReward;  
                }
            }
        }

        private PlayFrameCellData CreateCellData(DailyActivity record, CreateCellDataArgs args)
        {
            var playerData = PlayerDataManager.Instance;
            var cell = new PlayFrameCellData();
            cell.TableId = record.dailyActivityRecord.Id;

            var exdataId = record.dailyActivityRecord.ExDataId;
            var param0 = record.dailyActivityRecord.CommonParam[0];
            var param1 = record.dailyActivityRecord.CommonParam[1];
            var type = (eActivityType)record.dailyActivityRecord.DetailType;
            var btnId = -1;
            switch (type)
            {
                case eActivityType.DailyTask:
                {
                    var tbSU = Table.GetSkillUpgrading(param0);
                    var values = tbSU.Values;
                    var myLevel = PlayerDataManager.Instance.GetLevel();
                    foreach (var value in values)
                    {
                        var tbMission = Table.GetMissionBase(value);
                        var tbCondition = Table.GetConditionTable(tbMission.Condition);
                        if (PlayerDataManager.Instance.GetFlag(tbCondition.TrueFlag[0]) &&
                            myLevel >= tbCondition.ItemCountMin[0] && myLevel <= tbCondition.ItemCountMax[0])
                        {
                            args.maxCount = tbCondition.ExdataMax[0];
                            args.nowCount = playerData.GetExData(tbCondition.ExdataId[0]);
                            args.state = args.nowCount < args.maxCount
                                ? ActivityCellState.Start
                                : ActivityCellState.Complete;
                            break;
                        }
                    }
                }
                    break;
                case eActivityType.StoryDungeon:
                {
                    var tbPF = Table.GetPlotFuben(param0);
                    if (tbPF == null)
                    {
                        break;
                    }
                    args.nowCount = playerData.GetExData(exdataId);
                    foreach (var fubenId in tbPF.Difficulty)
                    {
                        var tbFuben = Table.GetFuben(fubenId);
                        if (tbFuben == null)
                        {
                            break;
                        }
                        var result = playerData.CheckCondition(tbFuben.EnterConditionId);
                        if (result != 0)
                        {
                            break;
                        }
                        args.maxCount += tbFuben.TodayCount + playerData.GetExData(tbFuben.ResetExdata);
                    }
                    if (args.nowCount < args.maxCount)
                    {
                        args.state = ActivityCellState.Start;
                    }
                    else
                    {
                        args.state = ActivityCellState.Complete;
                    }
                }
                    break;
                case eActivityType.Arena:
                {
                    args.nowCount = playerData.GetExData(exdataId);
                    args.maxCount = Table.GetClientConfig(201).ToInt();
                    var totalCount = args.maxCount + Table.GetClientConfig(204).ToInt();
                    if (args.nowCount < args.maxCount)
                    {
                        args.state = ActivityCellState.Start;
                    }
                    else if (args.nowCount < totalCount)
                    {
                        args.state = ActivityCellState.Complete;
                    }
                    else
                    {                 
                        args.state = ActivityCellState.CanNotAttend;                                     
                    }
                }
                    break;
                case eActivityType.NormalCount:
                {
                    args.nowCount = playerData.GetExData(exdataId);
                    args.maxCount = record.dailyActivityRecord.DisplayCount;
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
                case eActivityType.Infinity:
                {
                    args.nowCount = playerData.GetExData(exdataId);
                    args.maxCount = record.dailyActivityRecord.DisplayCount;
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
                case eActivityType.AcientBF:
                {
                    args.nowCount = playerData.GetExData(eExdataDefine.e545);
                    args.maxCount = record.dailyActivityRecord.DisplayCount;
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
                case eActivityType.Question:
                {
                    args.nowCount = playerData.GetExData(exdataId);
                    args.maxCount = Table.GetClientConfig(581).ToInt();
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
                case eActivityType.VipAddCount:
                {
                    args.nowCount = playerData.GetExData(exdataId);
                    args.maxCount = record.dailyActivityRecord.DisplayCount;
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
                case eActivityType.IgnoreCount:
                {
                    cell.IsShowCount = false;
                }
                    break;
                case eActivityType.VipDailyGift:
                {
                    args.nowCount = 0;
                    args.maxCount = record.dailyActivityRecord.DisplayCount;
                    var vipLevel = playerData.GetItemCount((int) eResourcesType.VipLevel);
                    if (vipLevel <= 0)
                    {
                        args.textId = 100000896; //购买
                    }
                    else if (playerData.GetFlag(param0)) //flag 2506:每天的vip礼包是否已领取
                    {
                        args.nowCount = 1;
                    }
                    else
                    {
                        args.textId = 100000894; //领取
                        btnId = 4;
                    }
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
                case eActivityType.MonthCard:
                {
                    args.nowCount = 0;
                    args.maxCount = record.dailyActivityRecord.DisplayCount;
                    var exdata64Idx = Table.GetClientConfig(418).ToInt();
                    var endTime = playerData.GetExData64(exdata64Idx);
                    var endDate = Extension.FromServerBinary(endTime);
                    var now = Game.Instance.ServerTime;
                    if (now > endDate)
                    {
                        args.textId = 100000896; //购买
                    }
                    else if (playerData.GetFlag(param0)) //flag 2507:每天的月卡礼包是否已领取
                    {
                        args.nowCount = 1;
                    }
                    else
                    {
                        args.textId = 100000894; //领取
                        btnId = 4;
                    }
                    args.state = args.nowCount < args.maxCount ? ActivityCellState.Start : ActivityCellState.Complete;
                }
                    break;
            }

            if (record.dailyActivityRecord.FinishCanJoin == 0)
            {
                if (args.state == ActivityCellState.Complete)
                {
                    args.state = ActivityCellState.CanNotAttend;
                }
            }

            var activityValue = record.dailyActivityRecord.ActivityValue;
            var activityCount = record.dailyActivityRecord.ActivityCount;
            if (activityCount == -1 && activityValue > 0)
            {
                activityCount = args.maxCount;
            }
            var slashText = GameUtils.GetDictionaryText(230027);
            var wuText = GameUtils.GetDictionaryText(270024);

            cell.State = (int) args.state;
            cell.BtnId = btnId;
            cell.SortPriority = record.dailyActivityRecord.SortPriority;
            cell.TextId = args.textId;
            cell.Count = string.Format(slashText, Math.Min(args.nowCount, args.maxCount), args.maxCount);
            cell.Activity = activityCount <= 0
                ? wuText
                : (string.Format(slashText, activityValue*Math.Min(args.nowCount, activityCount),
                    activityValue*activityCount));

            return cell;
        }
        private void RefreshLimitTime()
        {
            var tabIdx = (int)PlayFrameTab.TimedActivity;
            List<DailyActivity> records;
            if (!DailyActs.TryGetValue(tabIdx, out records))
            {
                return;
            }
            var playerData = PlayerDataManager.Instance;

            var isShowTimeLimitNotice = false;
            var now = Game.Instance.ServerTime;
            var nearestActivityTime = now.AddYears(10);
            var cells = new List<PlayFrameCellData>();
            foreach (var record in records)
            {
                var args = new CreateCellDataArgs();
                args.nowCount = 0;
                args.maxCount = 0;
                args.state = ActivityCellState.Start;
                args.textId = 100000613; //参加
                var param0 = record.dailyActivityRecord.CommonParam[0];

                var cell = CreateCellData(record, args);
                cells.Add(cell);

                {
                    var tbSU = Table.GetSkillUpgrading(param0);
                    var openLastMin = tbSU.Param[0];
                    var speId = tbSU.Param[1];
                    var values = tbSU.Values;
                    var actState = -1;
                    var startTime = default(DateTime);
                    if (speId != -1 && playerData.ActivityState.Count > speId)
                    {
                        actState = playerData.ActivityState[speId];
                    }
                    if (actState < (int)eActivityState.WillEnd)
                    {
                        foreach (var value in values)
                        {
                            var hour = value / 100;
                            var min = value % 100;
                            var startTime1 = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
                            var endTime1 = startTime1.AddMinutes(openLastMin);
                            if (endTime1 >= now)
                            {
                                startTime = startTime1;
                                break;
                            }
                        }
                    }
                    if (startTime == default(DateTime))
                    {
                        var value = values[0];
                        var hour = value / 100;
                        var min = value % 100;
                        startTime = new DateTime(now.Year, now.Month, now.Day, hour, min, 0).AddDays(1);
                        cell.State = (int)ActivityCellState.CanNotAttend;
                    }

                    cell.StartTime = startTime;
                    if (startTime <= now)
                    {
                        if (cell.State == (int)ActivityCellState.Start)
                        {
                            cell.IsShowNotice = startTime <= now;
                            isShowTimeLimitNotice = true;
                        }
                        startTime = startTime.AddMinutes(openLastMin);
                        nearestActivityTime = nearestActivityTime < startTime ? nearestActivityTime : startTime;
                    }
                    else
                    {

                        if (cell.State == (int)ActivityCellState.Start)
                        {
                            cell.State = (int)ActivityCellState.WillStart;
                        }
                        //cell.Reason = string.Format(GameUtils.GetDictionaryText(100000612), startTime.ToString("HH:mm"));
                        cell.Reason = GameUtils.GetAllTimeDiffString(startTime, true);
                        nearestActivityTime = nearestActivityTime < startTime ? nearestActivityTime : startTime;
                    }

                }
                {
                    SetNextRefreshCellTime(nearestActivityTime);
                }

                DataModel.IsShowTimeLimitNotice = isShowTimeLimitNotice;
            }
        }
        private void RefreshTimeLimitedActivity(bool isInit = false)
        {
            var tabIdx = (int) PlayFrameTab.TimedActivity;
            List<DailyActivity> records;
            if (!DailyActs.TryGetValue(tabIdx, out records))
            {
                return;
            }
            CurrentCellData.Clear();
            CurrentTimeSpan.Clear();
            var playerData = PlayerDataManager.Instance;

            var isShowTimeLimitNotice = false;
            var now = Game.Instance.ServerTime;
            var nearestActivityTime = now.AddYears(10);
            var cells = new List<PlayFrameCellData>();
            foreach (var record in records)
            {           
                var args = new CreateCellDataArgs();
                args.nowCount = 0;
                args.maxCount = 0;
                args.state = ActivityCellState.Start;
                args.textId = 100000613; //参加
                var param0 = record.dailyActivityRecord.CommonParam[0];

                var cell = CreateCellData(record, args);
                cells.Add(cell);
   
                var _tbDavi = Table.GetDailyActivity(record.dailyActivityRecord.Id);
                cell.IsRewardShow1 = 1;
                cell.IsRewardShow2 = 1;
                cell.IsRewardShow3 = 1;
                for (int i = 0, imax = _tbDavi.RewardShow.Count; i < imax; i++)
                {
                    if (i == 0)
                    {
                   
                        cell.RewardId1 = _tbDavi.RewardShow[i];
                        cell.IsRewardShow1 = 0;
                    }

                    if (i == 1)
                    {
                        cell.RewardId2 = _tbDavi.RewardShow[i];
                        cell.IsRewardShow2 = 0;
                    }
                    if (i == 2)
                    {
                        cell.RewardId3 = _tbDavi.RewardShow[i];
                        cell.IsRewardShow3 = 0;
                    }
                }

                {
                    var tbSU = Table.GetSkillUpgrading(param0);
                    var openLastMin = tbSU.Param[0];
                    var speId = tbSU.Param[1];
                    var values = tbSU.Values;
                    var actState = -1;
                    var startTime = default(DateTime);
                    if (speId != -1 && playerData.ActivityState.Count > speId)
                    {
                        actState = playerData.ActivityState[speId];
                    }
                    if (actState < (int) eActivityState.WillEnd)
                    {
                        foreach (var value in values)
                        {
                            var hour = value/100;
                            var min = value%100;
                            var startTime1 = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
                            var endTime1 = startTime1.AddMinutes(openLastMin); 
                        
                            if (endTime1 >= now)
                            {
                                startTime = startTime1;
                                //
                                TimeSpan span = startTime1 - DateTime.Now;
                                string timeForm = string.Format("距开启还有{0}:{1}:{2}", span.Hours, span.Minutes, span.Seconds);
                                cell.Reason = timeForm;
                                if (!CurrentCellData.Contains(cell))
                                {
                                    CurrentCellData.Add(cell);
                                    CurrentTimeSpan.Add(startTime1);
                                    if (!isInit)
                                    {
                                        NetManager.Instance.StartCoroutine(UpDateTime());
                                    }
                                }
                                //
                                break;
                            }
                        }
                    }

                    if (startTime == default(DateTime))
                    {
                        var value = values[0];
                        var hour = value/100;
                        var min = value%100;
                        startTime = new DateTime(now.Year, now.Month, now.Day, hour, min, 0).AddDays(1);
                        cell.State = (int) ActivityCellState.CanNotAttend;
                    }

                    cell.StartTime = startTime;
                    if (startTime <= now)
                    {
                        if (cell.State == (int) ActivityCellState.Start)
                        {
                            cell.IsShowNotice = startTime <= now;
                            isShowTimeLimitNotice = true;
                        }
                        startTime = startTime.AddMinutes(openLastMin);
                        nearestActivityTime = nearestActivityTime < startTime ? nearestActivityTime : startTime;
                    }
                    else
                    {

                        if (cell.State == (int)ActivityCellState.Start)
                        {
                            cell.State = (int) ActivityCellState.WillStart;
                        }                 
                        //cell.Reason = string.Format(GameUtils.GetDictionaryText(100000612), startTime.ToString("HH:mm"));
                        //cell.Reason = GameUtils.GetAllTimeDiffString(startTime, true);
                        nearestActivityTime = nearestActivityTime < startTime ? nearestActivityTime : startTime;
                    }              
                }
                //未开启限时活动
                if (record.state == (int)PlayFrameTab.NoToDpen)
                {
                    cell.State = (int)ActivityCellState.NoEnoughtLV;
                    cell.Reason = record.dailyActivityRecord.Desc;
                }               
           
                switch (cell.State)
                {
                    //case 0:
                    //case 2:
                    //    cell.BtnId = 1;
                    //    break;
                    //case 1:
                    //    cell.BtnId = 2;
                    //    break;
                    //case 3:
                    //    cell.BtnId = 3;
                    //    break;
                    case 0:
                        //参加
                        cell.BtnId = 1;
                        break;
                    case 1:
                        cell.BtnId = 2;
                        break;
                    case 2:
                        //领取奖励
                        cell.ShowColor = 1;
                        cell.BtnId = 4;
                        break;
                    case 3:
                        //未开启
                        cell.ShowColor = 2;
                        cell.BtnId = 2;
                        break;
                    case 4:
                        //当日活动结束    
                        cell.BtnId = 5;
                        break;               
                }          
            }

            {
                SetNextRefreshCellTime(nearestActivityTime);
            }
            isInit = true;
            cells.Sort((l, r) =>
            {
                if (l.State < r.State)
                {
                    return -1;
                }
                if (l.State > r.State)
                {
                    return 1;
                }

                if (l.StartTime < r.StartTime)
                {
                    return -1;
                }
                if (l.StartTime > r.StartTime)
                {
                    return 1;
                }

                if (l.SortPriority < r.SortPriority)
                {
                    return -1;
                }
                if (l.SortPriority > r.SortPriority)
                {
                    return 1;
                }

                return 0;
            });

            if (TabIdx == tabIdx)
            {
                DataModel.Cells = new ObservableCollection<PlayFrameCellData>(cells);

                if (cells.Count == 0)
                {
                    DataModel.EmptyTipDic = tabIdx + 100000900;
                }
                else
                {
                    DataModel.EmptyTipDic = -1;
                }
            }
            DataModel.IsShowTimeLimitNotice = isShowTimeLimitNotice;
            if (isInit)
            {
                var showReward = IsShowDailyRedPoint();
                playerData.NoticeData.DailyActivityFlag = DataModel.IsShowTimeLimitNotice || DataModel.IsShowDailyNotice || showReward;
            }

        }

        private void SetNextRefreshCellTime(DateTime time)
        {
            if (RefreshTimer == null)
            {
                RefreshTimer = TimeManager.Instance.CreateTrigger(time, () =>
                {
                    RefreshTimer = null;
                    RefreshTimeLimitedActivity();
                });
            }
            else
            {
                var trigger = (Trigger) RefreshTimer;
                if (trigger.DueTime != time)
                {
                    TimeManager.Instance.ChangeTime(RefreshTimer, time);
                }
            }
        }

        private void RefreshActivityRewards(bool isInit=false)
        {
            var playerData = PlayerDataManager.Instance;
            var vitality = playerData.GetExData(eExdataDefine.e15);
            DataModel.Vitality = vitality;
            DataModel.VitalitySliderValue = vitality/200f;
            var dailyActivityFlag = DataModel.IsShowTimeLimitNotice;
            var rewards = DataModel.Rewards;
            for (int i = 0, imax = GiftRecords.Count; i < imax; ++i)
            {
                var record = GiftRecords[i];
                var reward = rewards[i];
                reward.CanGet = vitality >= record.Param[ActivityRewardParamterIndx.NeedScore];
                reward.HasGot = playerData.GetFlag(record.Flag);
                if (reward.IsShowNotice)
                {
                    dailyActivityFlag = true;
                }
            }
            if (IsShowDailyRedPoint())
            {
                playerData.NoticeData.DailyActivityFlag = true;
            }
            else
            {
                if (isInit)
                {
                    playerData.NoticeData.DailyActivityFlag = DataModel.IsShowTimeLimitNotice || DataModel.IsShowDailyNotice ||
                                                              dailyActivityFlag;
                }    
            }

            RefreshRedPoint();
        }

        private bool IsShowDailyRedPoint()
        {
            var reward = DataModel.Rewards.Cast<DailyActivityCell>().FirstOrDefault(r => r.IsShowNotice);
            if (reward == null)
            {
                return false;
            }
            if (!reward.HasGot)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
   
        private IEnumerator UpDateTime()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                TimeTick ();
            }
        }

        private void TimeTick ()
        {
            for (int i = 0; i < CurrentTimeSpan.Count; i++)
            {
                if (CurrentCellData[i].State != (int)PlayFrameTab.NoToDpen && CurrentCellData[i].State != (int)ActivityCellState.ActivityOver
                    && CurrentCellData[i].State != (int)ActivityCellState.Start)
                {
                    TimeSpan span = CurrentTimeSpan[i] - DateTime.Now;
                    string timeForm = string.Format("距开启还有{0}:{1}:{2}", span.Hours, span.Minutes, span.Seconds);
                    CurrentCellData[i].Reason = timeForm;
                }          
            }
        }
        #endregion
    }
}