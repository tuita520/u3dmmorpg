/********************************************************************************* 

                         Scorpion




  *FileName:NewActivityController

  *Version:1.0

  *Date:2017-06-23

  *Description:

**********************************************************************************/

#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ObjCommand;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class NewActivityController : IControllerBase
    {
        private int activityId = 0;
        private TeamTargetChangeDataModel TeamModule = new TeamTargetChangeDataModel();
        #region 构造函数
        public NewActivityController()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(TeamWorldAutoMatchNewEvent.EVENT_TYPE, cancelAutoMatch);
            EventDispatcher.Instance.AddEventListener(AutoMatchState_Event.EVENT_TYPE, OnAutoMatchState);
            EventDispatcher.Instance.AddEventListener(UIEvent_NewActivityTabClickEvent.EVENT_TYPE, OnTabClick);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnInitExData);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpData);
            EventDispatcher.Instance.AddEventListener(UIEvent_NewActivityCellClickEvent.EVENT_TYPE, OnClickCell);
            EventDispatcher.Instance.AddEventListener(UIEvent_NewActivityCloseSecUIEvent.EVENT_TYPE, OnClickCloseSecondUI);
            EventDispatcher.Instance.AddEventListener(UIEvent_ButtonClicked.EVENT_TYPE, OnBtnClicked);
            EventDispatcher.Instance.AddEventListener(QueneUpdateEvent.EVENT_TYPE, OnQueneUpdated);
            EventDispatcher.Instance.AddEventListener(DungeonEnterCountUpdate.EVENT_TYPE, OnDungeonEnterCountUpdate);
            EventDispatcher.Instance.AddEventListener(OnClickBuyTiliEvent.EVENT_TYPE, OnClickBuyTili);
            EventDispatcher.Instance.AddEventListener(LoadSceneOverEvent.EVENT_TYPE, OnLoadSceneOver);
            EventDispatcher.Instance.AddEventListener(OnCLickGoToActivityByMainUIEvent.EVENT_TYPE, OpenByClickMainUI);
            EventDispatcher.Instance.AddEventListener(OnCLickGoToActivityByMain2UIEvent.EVENT_TYPE, OpenByClickMainUI2);
            EventDispatcher.Instance.AddEventListener(ClientBroadCastEvent.EVENT_TYPE, ClientBroadCast);
            EventDispatcher.Instance.AddEventListener(ActivityWorldSpeackClickEvent.EVENT_TYPE, ChatTeam);
            EventDispatcher.Instance.AddEventListener(ActivityAutoMatchClickEvent.EVENT_TYPE, AutoMatch);
            EventDispatcher.Instance.AddEventListener(ActivitySearchTeamClickEvent.EVENT_TYPE, SerachTeam);

            
        }
        #endregion

        private static Dictionary<int, int> DealErrs = new Dictionary<int, int>
        {
           {(int) ErrorCodes.Error_LevelNoEnough, 100001474},
            {(int) ErrorCodes.Error_FubenCountNotEnough, 466},
            {(int) ErrorCodes.ItemNotEnough, 467},
            {(int) ErrorCodes.Error_FubenRewardNotReceived, 497},
            {(int) ErrorCodes.Unline, 498},
            {(int) ErrorCodes.Error_CharacterOutLine, 498},
            {(int) ErrorCodes.Error_AlreadyInThisDungeon, 493},
            {(int) ErrorCodes.Error_CharacterCantQueue, 544}
        };

        private Dictionary<int,ActivityCellDataModel> dicExIdx2Cell = new Dictionary<int, ActivityCellDataModel>();  //exdata 对活动序列号的dic 

        //用于和界面进行绑定的数据
        private NewActivityDataModel m_DataModel;
        private List<DynamicActivityRecord> DynamicActRecords;
        private List<WorldBOSSRecord> WorldBossRecord; 
        private Coroutine TimteRefresh;
        private QueueUpDataModel QueueUpData
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.QueueUpData; }
        }
        private int NextQueueId;
        private Coroutine mCoSafeRemoveListener;

        private int mNearActId = -1;
        private Coroutine MainUiTimteRefresh;

        private int mBroadCastId = -1;
        private Coroutine MainUiTimteRefresh2;

        private bool ReturnSecond = true;
        private int maxAnswer = 20;
        private int TiliMaxValue;
        private void OnInitExData(IEvent ievent)
        {
            RefreshActData(true);
            //灵兽岛数据
            for (int i = 0; i < m_DataModel.tabModel.Count; i++)
            {
                for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                {
                    var data = m_DataModel.tabModel[i].cells[j];
                    if (data == null)
                    {
                        continue;
                    }
                    var playerData = PlayerDataManager.Instance;
                    if (data.tableId == 9)
                    {
                        data.TiliValue = playerData.GetExData(eExdataDefine.e630);
                        data.TiliMaxValue = TiliMaxValue;
                        data.TiliPercent = Math.Min((float)data.TiliValue / 100, 1.0f);

                        var vipLevel = playerData.GetItemCount((int)eResourcesType.VipLevel);
                        var tbVip = Table.GetVIP(vipLevel);
                        if (tbVip != null)
                        {
                            var result = playerData.GetExData(eExdataDefine.e631);
                            data.TiliBuyCount = result;
                            data.TIliBuyMaxCount = tbVip.PetIslandBuyTimes;
                        }
                    }
                    // 累计经验
                    if (data.tableId == 11)
                    {
                        data.LeiJiExp = playerData.GetExData(eExdataDefine.e592).ToString();
                        data.NeedDiamond = playerData.GetExData(eExdataDefine.e594).ToString();
                        if (int.Parse(data.LeiJiExp) > 0)
                        {
                            data.IsShowExp = 1;
                        }
                        else
                        {
                            data.IsShowExp = 0;
                        }
                    }
                    else if (data.tableId == 12)
                    {
                        data.LeiJiExp = playerData.GetExData(eExdataDefine.e591).ToString();
                        data.NeedDiamond = playerData.GetExData(eExdataDefine.e593).ToString();
                        if (int.Parse(data.LeiJiExp) > 0)
                        {
                            data.IsShowExp = 1;
                        }
                        else
                        {
                            data.IsShowExp = 0;
                        }
                    }
                }
            }

            // 启动timer  1s一次
            if (TimteRefresh == null)
            {
                TimteRefresh = NetManager.Instance.StartCoroutine(UpDateTime());
            }
            else
            {
                NetManager.Instance.StopCoroutine(TimteRefresh);
                TimteRefresh = null;
                TimteRefresh = NetManager.Instance.StartCoroutine(UpDateTime());
            }
        }
        private void OnExDataUpData(IEvent ievent)
        {
            var e = ievent as ExDataUpDataEvent;
            if (e == null)
            {
                return;
            }
            {
                if (dicExIdx2Cell.ContainsKey(e.Key))
                {
                    var cell = dicExIdx2Cell[e.Key];
                    cell.enterCount = e.Value;
                    return;
                }
            }
            if (e.Key == (int) eExdataDefine.e592 || e.Key == (int) eExdataDefine.e594 ||
                e.Key == (int) eExdataDefine.e591 || e.Key == (int) eExdataDefine.e593)
            {
                var playerData = PlayerDataManager.Instance;
                for (int i = 0; i < m_DataModel.tabModel.Count; i++)
                {
                    for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                    {
                        var data = m_DataModel.tabModel[i].cells[j];
                        if (data == null)
                        {
                            continue;
                        }
                        if (data.tableId == 11)
                        {
                            data.LeiJiExp = playerData.GetExData(eExdataDefine.e592).ToString();
                            data.NeedDiamond = playerData.GetExData(eExdataDefine.e594).ToString();
                            if (int.Parse(data.LeiJiExp) > 0)
                            {
                                data.IsShowExp = 1;
                            }
                            else
                            {
                                data.IsShowExp = 0;
                            }
                        }
                        else if (data.tableId == 12)
                        {
                            data.LeiJiExp = playerData.GetExData(eExdataDefine.e591).ToString();
                            data.NeedDiamond = playerData.GetExData(eExdataDefine.e593).ToString();
                            if (int.Parse(data.LeiJiExp) > 0)
                            {
                                data.IsShowExp = 1;
                            }
                            else
                            {
                                data.IsShowExp = 0;
                            }
                        }
                    }
                }
                RefreshCurTabData(m_DataModel.CurTabIndex);
                RefreshCurSecondData(m_DataModel.CurTabIndex, m_DataModel.CurSecondUiIndex);
            }

            if (e.Key == (int)eExdataDefine.e630 || e.Key == (int)eExdataDefine.e631)
            {
                //灵兽岛数据
                for (int i = 0; i < m_DataModel.tabModel.Count; i++)
                {
                    for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                    {
                        var data = m_DataModel.tabModel[i].cells[j];
                        if (data == null)
                        {
                            continue;
                        }
                        if (data.tableId == 9)
                        {
                            data.TiliValue = PlayerDataManager.Instance.GetExData(eExdataDefine.e630);
                            data.TiliMaxValue = TiliMaxValue;
                            data.TiliPercent = Math.Min((float)data.TiliValue / 100, 1.0f);

                            var vipLevel = PlayerDataManager.Instance.GetItemCount((int)eResourcesType.VipLevel);
                            var tbVip = Table.GetVIP(vipLevel);
                            if (tbVip != null)
                            {
                                var result = PlayerDataManager.Instance.GetExData(eExdataDefine.e631);
                                data.TiliBuyCount = result;
                                data.TIliBuyMaxCount = tbVip.PetIslandBuyTimes;
                            }
                        }
                    }
                }

                RefreshCurTabData(m_DataModel.CurTabIndex);
                RefreshCurSecondData(m_DataModel.CurTabIndex, m_DataModel.CurSecondUiIndex);
            }
        }


        private IEnumerator UpDateTime()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                TimeTick();
            }
            yield break;
        }

        private void TimeTick()
        {
            var now = Game.Instance.ServerTime;
            var nearActId = -1;
            var nearTime = DateTime.MaxValue;
            var nearFubenId = -1;
            //地图统领，黄金部队 副本  
            //TODO 这个地方有效率问题 三层for循环 最差循环400次左右
            for (int i = 0; i < m_DataModel.tabModel.Count; i++)
            {
                for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                {
                    var data = m_DataModel.tabModel[i].cells[j];
                    if (data == null || data.tableId == -1)
                    {
                        continue;
                    }

                    // 不满足周循环的 直接continue
                    var tbDynamic = Table.GetDynamicActivity(data.tableId);
                    if (tbDynamic != null)
                    {
                        if (CheckActOpen(tbDynamic.Id) == 1)
                        {
                            var tbFuben = Table.GetFuben(data.fuBenId);
                            if (tbFuben == null)
                            {
                                continue;
                            }
                            var startTime = new DateTime(now.Year, now.Month, now.Day, tbFuben.OpenTime[0] / 100, tbFuben.OpenTime[0] % 100, 0);
                            var canEnterTime = startTime.AddMinutes(tbFuben.CanEnterTime);
                            if (now < startTime)
                            {
                                data.Time = GameUtils.GetTimeDiffString(startTime);
                                data.timeState = 0;
                                data.isGrey = true;
                            }
                            else
                            {
                                if (now < canEnterTime)
                                {
                                    nearTime = canEnterTime;
                                    nearActId = data.tableId;
                                    nearFubenId = tbFuben.Id;

                                    data.Time = GameUtils.GetTimeDiffString(canEnterTime - now);
                                    data.timeState = 1;
                                    data.isGrey = false;
                                }
                                else
                                {
                                    var days = GameUtils.CheckActOpenNext(data.tableId);
                                    var nextDate = startTime.AddDays(days);
                                    data.Time = nextDate.ToString("MM/dd");
                                    data.isGrey = true;
                                    data.timeState = 0;
                                }
                            }
                            continue;
                        }

                        if (!GameUtils.CheckIsWeekLoopOk(tbDynamic.WeekLoop))
                        {
                            continue;
                        }
                    }

                    if (data.type == 3) //地图统领，黄金部队
                    {
                        // 先遍历出来这么多里面 时间最近的一个
                        if (data.worldMosnterBtns == null || data.worldMosnterBtns.Btns.Count <= 0)
                        {
                            continue;
                        }
                        var destTime = now.AddYears(10);
                        foreach (var value in data.worldMosnterBtns.Btns)
                        {
                            var tbWolrdBoss = Table.GetWorldBOSS(value.TableId);
                            var times = tbWolrdBoss.RefleshTime.Split('|');
                            if (!times[0].Contains(':'))
                            {
                                continue;
                            }
                            var bMatchOpenTime = false;
                            foreach (var t in times)
                            {
                                var tt = t.Split(':');
                                var time = new DateTime(now.Year, now.Month, now.Day, tt[0].ToInt(), tt[1].ToInt(), 0);
                                if (time >= now && destTime > time)
                                {
                                    bMatchOpenTime = true;
                                    destTime = time;
                                    break;
                                }
                            }
                            if (!bMatchOpenTime)
                            {
                                var tt = times[0].Split(':');
                                var time = new DateTime(now.Year, now.Month, now.Day, tt[0].ToInt(), tt[1].ToInt(), 0).AddDays(1);
                                if (destTime > time)
                                {
                                    destTime = time;
                                }
                            }
                        }

                        data.Time = GameUtils.GetTimeDiffString(destTime, true);
                        data.timeState = 2;
                    }
                    else if (data.type == 0) //副本
                    {
                        if (data.fuBenId == -1)
                        {
                            continue;
                        }
                        var tbFuben = Table.GetFuben(data.fuBenId);
                        if (tbFuben == null)
                        {
                            continue;
                        }
                        for (int k = 0; k < tbFuben.OpenTime.Count; ++k)
                        {
                            if (tbFuben.OpenTime[k] == -1)
                            {
                                break;
                            }
                            var startTime = new DateTime(now.Year, now.Month, now.Day, tbFuben.OpenTime[k] / 100, tbFuben.OpenTime[k] % 100, 0);
                            if (now <= startTime) // 今天 或者 昨天
                            {
                                if (k == 0) // 今天第一次还没开始  取昨天最后一次
                                {
                                    var yestday = now.AddDays(-1);
                                    var canEnterTime = new DateTime(yestday.Year, yestday.Month, yestday.Day, tbFuben.OpenTime[tbFuben.OpenTime.Count - 1] / 100, tbFuben.OpenTime[tbFuben.OpenTime.Count - 1] % 100, 0);
                                    canEnterTime = canEnterTime.AddMinutes(tbFuben.CanEnterTime);
                                    if (now <= canEnterTime) // 找昨天最后一次看是否在进入时间内  可进入  显示昨天最后一次开启的进入倒计时
                                    {
                                        data.Time = GameUtils.GetTimeDiffString(canEnterTime, true);
                                        data.timeState = 1;

                                        if (canEnterTime < nearTime)
                                        {
                                            nearTime = canEnterTime;
                                            nearActId = data.tableId;
                                            nearFubenId = tbFuben.Id;
                                        }
                                        break;
                                    }
                                    else // 不可进入 显示当前次数的时间开始倒计时
                                    {
                                        data.Time = GameUtils.GetTimeDiffString(startTime, true);
                                        data.timeState = 0;
                                        break;
                                    }
                                }
                                else // 今天已经开始了
                                {
                                    var canEnterTime2 = new DateTime(now.Year, now.Month, now.Day, tbFuben.OpenTime[k - 1] / 100, tbFuben.OpenTime[k - 1] % 100, 0); // 不会越界 k>0 了
                                    canEnterTime2 = canEnterTime2.AddMinutes(tbFuben.CanEnterTime);
                                    if (now <= canEnterTime2) // 判断上一次的时间能否进入 能进入显示为进入倒计时
                                    {
                                        data.Time = GameUtils.GetTimeDiffString(canEnterTime2, true);
                                        data.timeState = 1;

                                        if (canEnterTime2 < nearTime)
                                        {
                                            nearTime = canEnterTime2;
                                            nearActId = data.tableId;
                                            nearFubenId = tbFuben.Id;
                                        }

                                        break;
                                    }
                                    else // 不可进入 显示当前次数的时间开始倒计时
                                    {
                                        data.Time = GameUtils.GetTimeDiffString(startTime, true);
                                        data.timeState = 0;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (k == tbFuben.OpenTime.Count - 1) // 今天所有时间都过了  看今天最后一次进入时间 是否没过    过了的话找明天开始时间
                                {
                                    var canEnterTime3 = startTime.AddMinutes(tbFuben.CanEnterTime);
                                    if (now <= canEnterTime3) // 今天还能进
                                    {
                                        data.Time = GameUtils.GetTimeDiffString(canEnterTime3, true);
                                        data.timeState = 1;

                                        if (canEnterTime3 < nearTime)
                                        {
                                            nearTime = canEnterTime3;
                                            nearActId = data.tableId;
                                            nearFubenId = tbFuben.Id;
                                        }
                                        break;
                                    }
                                    else // 不可进入 找明天开启倒计时
                                    {
                                        var tomorrow = now.AddDays(1);
                                        var canEnterTime2 = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, tbFuben.OpenTime[0] / 100, tbFuben.OpenTime[0] % 100, 0);
                                        data.Time = GameUtils.GetTimeDiffString(canEnterTime2, true);
                                        data.timeState = 0;
                                        break;
                                    }
                                }
                            }
                        }                  
                    }
                    // 累计经验
                    if (data.tableId == 11 || data.tableId == 12)
                    {
                        DateTime dt = Game.Instance.ServerTime.Date.AddDays(1); ;
                        var sp = (dt - Game.Instance.ServerTime);
                        data.LeftTime = GameUtils.GetTimeDiffString(sp);
                    }
                }
            }

            if (State == FrameState.Open)
            {
                RefreshCurTabData(m_DataModel.CurTabIndex, true);
                RefreshCurSecondData(m_DataModel.CurTabIndex, m_DataModel.CurSecondUiIndex, true);
            }

            // 主界面通知的活动
            if (nearActId != mNearActId && nearFubenId != -1 && nearActId != -1)
            {
                if (CheckActOpen(nearActId) == 0)
                {
                    return;
                }

                if (nearActId == 14)
                {
                    var worldBossState = PlayerDataManager.Instance.ActivityState[(int) eActivity.WorldBoss];
                    if (worldBossState == (int) eActivityState.WillEnd || worldBossState == (int) eActivityState.End)
                    {
                        mNearActId = -1;
                        return;
                    }
                }

                mNearActId = nearActId;
                // 变了
                if (MainUiTimteRefresh == null)
                {
                    MainUiTimteRefresh = NetManager.Instance.StartCoroutine(RefreshMainUITime(41023, 1, nearFubenId, nearTime));
                }
                else
                {
                    NetManager.Instance.StopCoroutine(MainUiTimteRefresh);
                    MainUiTimteRefresh = null;
                    MainUiTimteRefresh = NetManager.Instance.StartCoroutine(RefreshMainUITime(41023, 1, nearFubenId, nearTime));
                }
            }
        }
        private IEnumerator RefreshMainUITime(int dicId, int state, int fubenId, DateTime time)
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                if (mNearActId == 14)
                {
                    var worldBossState = PlayerDataManager.Instance.ActivityState[(int)eActivity.WorldBoss];
                    if (worldBossState == (int)eActivityState.WillEnd || worldBossState == (int)eActivityState.End)
                    {
                        m_DataModel.MainUTimeDown = "";
                        mNearActId = -1;
                        yield break;
                    } 
                }

                var formateStr = GameUtils.GetDictionaryText(dicId);

                var nearestTime = time;
                var deltaTime = nearestTime - Game.Instance.ServerTime;
                if (nearestTime <= Game.Instance.ServerTime)
                {
                    m_DataModel.MainUTimeDown = "";
                    mNearActId = -1;
                    yield break;
                }

                if (deltaTime.TotalSeconds > 0)
                {
                    var timeStr = GameUtils.GetTimeDiffString(nearestTime);
                    var tbFuben = Table.GetFuben(fubenId);
                    m_DataModel.MainUTimeDown = string.Format(formateStr, tbFuben.Name, timeStr);

                    for (var i = 0; i < m_DataModel.tabModel[m_DataModel.CurTabIndex].cells.Count; ++i)
                    {
                        var data = m_DataModel.tabModel[m_DataModel.CurTabIndex].cells[i];
                        if (data == null)
                        {
                            continue;
                        }

                        if (m_DataModel.curTabModel.cells.Count <= i)
                        {
                            continue;
                        }
                        DeoPrison(timeStr, QueueUpData.QueueId, 1 ,data.QueueState);
                        RefreshLineCom(timeStr, 1, data.QueueState);
                    }              
                }
                else
                {
                    m_DataModel.MainUTimeDown = "";
                    mNearActId = -1;
                    yield break;
                }

            }
            yield break;
        }

        private void OnTabClick(IEvent ievent)
        {
            var e = ievent as UIEvent_NewActivityTabClickEvent;
            if (e == null)
            {
                return;
            }
            if (m_DataModel == null || m_DataModel.tabModel == null)
            {
                return;
            }
            // 刷UI
            if (m_DataModel.tabModel.Count > e.TabIdx)
            {
                m_DataModel.CurTabIndex = e.TabIdx;
                RefreshCurTabData(e.TabIdx);
            }
        }

        private void RefreshCurTabData(int index, bool justTime = false)
        {
            index = Math.Max(index, 0);
            if (m_DataModel.tabModel.Count <= index)
            {
                return;
            }

            for (var i = 0; i < m_DataModel.tabModel[index].cells.Count; ++i)
            {
                var data = m_DataModel.tabModel[index].cells[i];
                if (data == null)
                {
                    continue;
                }

                if (m_DataModel.curTabModel.cells.Count <= i)
                {
                    continue;
                }
                if (justTime)
                {
                    m_DataModel.curTabModel.cells[i].Time = data.Time;
                    m_DataModel.curTabModel.cells[i].timeState = data.timeState;
                    //DeoPrison(data.Time, QueueUpData.QueueId, data.timeState,data.QueueState);
                    //RefreshLineCom(data.Time, data.timeState,data.QueueState);
               
                    continue;
                }
                m_DataModel.curTabModel.cells[i].Clone(data);
            }
        }

   

        private bool RefreshCurSecondData(int tabIdx, int secIdx, bool isJustTime = false)
        {
            if (secIdx < 0)
            {
                return false;
            }
            if (tabIdx < 0)
            {
                return false;
            }

            tabIdx = Math.Max(tabIdx, 0);

            if (m_DataModel.tabModel.Count <= tabIdx || m_DataModel.tabModel[tabIdx].cells.Count <= secIdx)
            {
                return false;
            }

            if (m_DataModel.tabModel[tabIdx].cells[secIdx].type == 0 && m_DataModel.tabModel[tabIdx].cells[secIdx].fuBenId == -1) //是副本类型 但是没有符合条件的副本可以进入
            {
                return false;
            }

            if (isJustTime)
            {
                m_DataModel.curSecondModel.Time = m_DataModel.tabModel[tabIdx].cells[secIdx].Time;
                m_DataModel.curSecondModel.timeState = m_DataModel.tabModel[tabIdx].cells[secIdx].timeState;
                return true;
            }

            m_DataModel.curSecondModel.Clone(m_DataModel.tabModel[tabIdx].cells[secIdx]);

            return true;
        }


        #region 固有函数
        public void CleanUp()
        {
            m_DataModel = new NewActivityDataModel();
            maxAnswer = int.Parse(Table.GetClientConfig(581).Value);
            TiliMaxValue = int.Parse(Table.GetClientConfig(1117).Value);
            //初始化“地图统领”和“黄金部队”的按钮
            WorldBossRecord = new List<WorldBOSSRecord>();
            Table.ForeachWorldBOSS(record =>
            {
                if (record.Type > 1)
                {
                    return true;
                }
                if (record.IsDisplayClient == 0)
                {
                    return true;
                }
                WorldBossRecord.Add(record);
                return true;
            });

            //初始化滚动条
            DynamicActRecords = new List<DynamicActivityRecord>();
            Table.ForeachDynamicActivity(record =>
            {
                DynamicActRecords.Add(record);
                return true;
            });
            if (DynamicActRecords.Count > 0)
            {
                DynamicActRecords.Sort((x, y) =>
                {
                    if (x.Sort < y.Sort)
                    {
                        return -1;
                    }
                    if (x.Sort > y.Sort)
                    {
                        return 1;
                    }
                    return 0;
                });
            }
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as ActivityArguments;
            if (args == null)
            {
                return;
            }

            ReturnSecond = true;
            if (args.Args!=null && args.Args.Count > 1)
            {
                ReturnSecond = args.Args[1] == -1;
            }
             
            // 如果是直接打开二级界面则需要  设置一些DataModel
            m_DataModel.PageId = -1;

            // 一级界面ID
            var enterTabIndex = Math.Max(args.Tab, 0);

            // 直接进入二级界面
            var enterSecondIndex = -1;
            if (args.Args != null && args.Args.Count > 0)
            {
                enterSecondIndex = args.Args[0];
            }
        

            RefreshActData();
            TimeTick();

            // 初始化 当前显示Tab页数据
            if (m_DataModel.tabModel.Count > enterTabIndex)
            {
                m_DataModel.CurTabIndex = enterTabIndex;
                RefreshCurTabData(enterTabIndex);
            }

            // 初始化 当前显示的二级界面数据
            if (enterSecondIndex >= 0)
            {
                //m_DataModel.CurSecondUiIndex = enterSecondIndex;
                //RefreshCurSecondData(enterTabIndex, enterSecondIndex);
                OnClickCell(enterSecondIndex);
            }

            UpdateQueue(false);
        }

        private void RefreshActData(bool IsInit = false)
        {
            List<ActivityCellDataModel> tempList0 = new List<ActivityCellDataModel>();
            List<ActivityCellDataModel> tempList1 = new List<ActivityCellDataModel>();
            List<ActivityCellDataModel> tempList2 = new List<ActivityCellDataModel>();
            List<ActivityCellDataModel> tempList3 = new List<ActivityCellDataModel>();

            FubenRecord shiLianGuyuRecord = null;
            var enumorator = DynamicActRecords.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var record = enumorator.Current;
                if (record == null)
                    continue;
                // 初始化 所有tab页数据
                var cell = new ActivityCellDataModel();
                cell.iconId = record.DisplayPic;
                cell.name = record.Name;
                cell.desc = record.SpecialDesc;
                cell.openDesc = record.NoOpenStr;

                if (CheckActOpen(record.Id) >= 0)
                {
                    var days = GameUtils.CheckActOpenNext(record.Id);
                    if (days >= 0)
                    {
                        var week = Convert.ToInt32(Game.Instance.ServerTime.AddDays(days).DayOfWeek);
                        cell.openDesc = string.Format(GameUtils.GetDictionaryText(100002215), GameUtils.GetDictionaryText(100002216 + week));
                        var fubenId = GetMyLevelFubenId(record);
                        var tbFuben = Table.GetFuben(fubenId);
                        if (tbFuben != null && tbFuben.OpenTime.Count >= 1)
                        {
                            var time = (tbFuben.OpenTime[0]/100) + ":" + (tbFuben.OpenTime[0]%100).ToString(("00"));
                            cell.openDesc += time;
                        }
                    }
                    else
                    {
                        if (GameUtils.CheckIsWeekLoopOk(record.WeekLoop)) //如果满足周循环
                        {
                            var nearWeek = -1;
                            for (var i = 0; i < record.WeekLoop.Count; ++i)
                            {
                                var week = record.WeekLoop[i];
                                if (week == 0)
                                    week = 7;
                                if (nearWeek == -1)
                                    nearWeek = week;
                                else
                                {
                                    if (week < nearWeek)
                                        nearWeek = week;
                                }
                            }

                            if (nearWeek == 7)
                                nearWeek = 0;

                            if (nearWeek != -1)
                            {
                                cell.openDesc = string.Format(GameUtils.GetDictionaryText(100002215), GameUtils.GetDictionaryText(100002216 + nearWeek));
                            }
                        }
                        else
                        {
                            var nearWeek = GameUtils.GetNearWeekLoop(record.WeekLoop);
                            if (nearWeek != -1)
                            {
                                cell.openDesc = string.Format(GameUtils.GetDictionaryText(100002215), GameUtils.GetDictionaryText(100002216 + nearWeek));
                            }
                        }         
                    }
                }
                else
                {
                    if (!GameUtils.CheckIsWeekLoopOk(record.WeekLoop)) //如果不满足周循环
                    {
                        var nearWeek = GameUtils.GetNearWeekLoop(record.WeekLoop);
                        if (nearWeek != -1)
                        {
                            cell.openDesc = string.Format(GameUtils.GetDictionaryText(100002215), GameUtils.GetDictionaryText(100002216 + nearWeek));
                        }
                    }                
                }
                cell.openedDesc = record.OpenDec;
                cell.Time = GameUtils.GetDictionaryText(100000016); //默认全天开启
                cell.isShowTime = CheckIsShowTime(record);
                cell.isShowOPendDesc = CheckIsShowOpenDesc(record);
                cell.type = record.Type;
                cell.fuBenId = GetMyLevelFubenId(record);
                var enterCount = -1;
                var maxCount = -1;
                if (record.ExDataId > 0)
                {
                    maxCount = record.MaxCount;
                    enterCount = PlayerDataManager.Instance.GetExData(record.ExDataId);
                    dicExIdx2Cell[record.ExDataId] = cell;
                }
                else
                {
                    GetEnterAndMaxCount(cell.fuBenId, ref enterCount, ref maxCount);
                }
                cell.enterCount = enterCount;
                cell.maxCount = maxCount;
                cell.order = record.Order2;
                cell.QueueState = 0;
                cell.timeState = -1;
                cell.tableId = record.Id;
                cell.TiliValue = PlayerDataManager.Instance.GetExData(eExdataDefine.e630);
                cell.TiliMaxValue = TiliMaxValue;
                cell.TiliPercent = Math.Min((float)cell.TiliValue / 100, 1.0f);
                var vipLevel = PlayerDataManager.Instance.GetItemCount((int)eResourcesType.VipLevel);
                var tbVip = Table.GetVIP(vipLevel);
                if (tbVip != null)
                {
                    var result = PlayerDataManager.Instance.GetExData(eExdataDefine.e631);
                    cell.TiliBuyCount = result;
                    cell.TIliBuyMaxCount = tbVip.PetIslandBuyTimes;
                }
                cell.isGrey = CheckIsGrey(record, enterCount, maxCount, ref cell);

                if (cell.fuBenId == -1 && record.Type == 0) // 副本类型 但是没匹配到可以进入的副本
                {
                    cell.isShowTime = false;
                }
                if (cell.fuBenId == -1)
                {
                    cell.isShowYuYueBtn = false;
                }
                else
                {
                    var tbFuben = Table.GetFuben(cell.fuBenId);
                    if (tbFuben != null && tbFuben.OpenTime.Count > 0 && tbFuben.OpenTime[0] != -1)
                    {
                        cell.isShowYuYueBtn = true;
                    }
                    else
                    {
                        cell.isShowYuYueBtn = false;
                    }

                    if (record.Id == 2)
                    {
                        shiLianGuyuRecord = tbFuben;
                    }
                }

                int idx = 0;
                if (record.Id == 13)// 黄金部队
                {
                    cell.worldMosnterBtns.Btns.Clear();
                    idx = 0;
                    foreach (var bossData in WorldBossRecord)
                    {
                        if (bossData.Type != 1)
                        {
                            continue;
                        }
                        BtnState temp = new BtnState();
                        temp.Index = idx++;
                        temp.TableId = bossData.Id;
                        temp.Selected = temp.Index == 0;
                        temp.Enabled = true;
                        cell.worldMosnterBtns.Btns.Add(temp);
                        if (temp.Selected)
                        {
                            cell.worldMosnterBtns.CurBtn = temp;
                            var tbWorldBoss = Table.GetWorldBOSS(cell.worldMosnterBtns.CurBtn.TableId);
                            var tbSceneNpc = Table.GetSceneNpc(tbWorldBoss.SceneNpc);
                            cell.worldMosnterBtns.ModelId = tbSceneNpc.DataID;
                        }
                    }
                }
                else if (record.Id == 15) // 地图统领
                {
                    cell.worldMosnterBtns.Btns.Clear();
                    idx = 0;
                    foreach (var bossData in WorldBossRecord)
                    {
                        if (bossData.Type != 0)
                        {
                            continue;
                        }
                        BtnState temp = new BtnState();
                        temp.Index = idx++;
                        temp.TableId = bossData.Id;
                        temp.Selected = temp.Index == 0;
                        temp.Enabled = true;
                        cell.worldMosnterBtns.Btns.Add(temp);
                        if (temp.Selected)
                        {
                            cell.worldMosnterBtns.CurBtn = temp;
                            var tbWorldBoss = Table.GetWorldBOSS(cell.worldMosnterBtns.CurBtn.TableId);
                            var tbSceneNpc = Table.GetSceneNpc(tbWorldBoss.SceneNpc);
                            cell.worldMosnterBtns.ModelId = tbSceneNpc.DataID;
                        }
                    }
                }
                else if (record.Id == 11) //恶魔
                {
                    cell.LeiJiExp = PlayerDataManager.Instance.GetExData(eExdataDefine.e592).ToString();
                    cell.NeedDiamond = PlayerDataManager.Instance.GetExData(eExdataDefine.e594).ToString();
                    cell.LeftTime = string.Empty;
                    if (int.Parse(cell.LeiJiExp) > 0)
                    {
                        cell.IsShowExp = 1;
                    }
                    else
                    {
                        cell.IsShowExp = 0;
                    }
                }
                else if (record.Id == 12) //血色
                {
                    cell.LeiJiExp = PlayerDataManager.Instance.GetExData(eExdataDefine.e591).ToString();
                    cell.NeedDiamond = PlayerDataManager.Instance.GetExData(eExdataDefine.e593).ToString();
                    cell.LeftTime = string.Empty;
                    if (int.Parse(cell.LeiJiExp) > 0)
                    {
                        cell.IsShowExp = 1;
                    }
                    else
                    {
                        cell.IsShowExp = 0;
                    }
                }
                else if (record.Id == 3)//你问我答
                {
                    if (IsInit)
                    {
                        var times = PlayerDataManager.Instance.GetExData(436);
                        PlayerDataManager.Instance.NoticeData.AnswerQuestion = times < maxAnswer && cell.isShowTime;                        
                    }
                }

                if (record.SufaceTab == 0)
                {
                    tempList0.Add(cell);
                }
                else if (record.SufaceTab == 1)
                {
                    tempList1.Add(cell);
                }
                else if (record.SufaceTab == 2)
                {
                    tempList2.Add(cell);
                }
                else if (record.SufaceTab == 3)
                {
                    tempList3.Add(cell);
                }
            }

            tempList0.Sort((x, y) =>
            {
                if (x.order < y.order)
                {
                    return -1;
                }
                if (x.order > y.order)
                {
                    return 1;
                }
                return 0;
            });
            tempList1.Sort((x, y) =>
            {
                if (x.order < y.order)
                {
                    return -1;
                }
                if (x.order > y.order)
                {
                    return 1;
                }
                return 0;
            });
            tempList2.Sort((x, y) =>
            {
                if (x.order < y.order)
                {
                    return -1;
                }
                if (x.order > y.order)
                {
                    return 1;
                }
                return 0;
            });
            tempList3.Sort((x, y) =>
            {
                if (x.order < y.order)
                {
                    return -1;
                }
                if (x.order > y.order)
                {
                    return 1;
                }
                return 0;
            });

            SetTabData(tempList0, 0);
            SetTabData(tempList1, 1);
            SetTabData(tempList2, 2);
            SetTabData(tempList3, 3);

            if (shiLianGuyuRecord != null)
            {
                RefreshFubenCount(shiLianGuyuRecord, true);
            }
        }
        private void SetTabData(List<ActivityCellDataModel> tempList, int idx)
        {
            if (m_DataModel.tabModel.Count <= idx)
            {
                return;
            }

            for (int i = 0; i < tempList.Count; ++i)
            {
                if (m_DataModel.tabModel[idx].cells.Count <= i)
                {
                    continue;
                }

                m_DataModel.tabModel[idx].cells[i].Clone(tempList[i]);
            }
        }

        private void GetEnterAndMaxCount(int fubenId, ref int enterCount, ref int maxCount)
        {
            if (fubenId != -1)
            {
                var tbFuben = Table.GetFuben(fubenId);
                if (tbFuben != null)
                {
                    var enterExdata = tbFuben.TodayCountExdata;
                    enterCount = PlayerDataManager.Instance.GetExData(enterExdata);

                    maxCount = tbFuben.TodayCount;
                    if (tbFuben.AssistType == 4)
                    {
                        //恶魔
                        maxCount += PlayerDataManager.Instance.TbVip.DevilBuyCount;
                    }
                    else if (tbFuben.AssistType == 5)
                    {
                        //血色
                        maxCount += PlayerDataManager.Instance.TbVip.BloodBuyCount;
                    }
                }
            }
        }
    
        private bool CheckIsShowTime(DynamicActivityRecord record)
        {
            var status = CheckActOpen(record.Id);
            if (status == 0)
            {
                return false;
            }
            else if (status == 1)
            {
            
            }
            else
            {
                if (!GameUtils.CheckIsWeekLoopOk(record.WeekLoop))
                {
                    return false;
                }            
            }

            //if (record.Type == 0 || record.Type == 3)
            {
                if (record.OpenCondition == -1)
                {
                    return true; 
                }
                if (PlayerDataManager.Instance.CheckCondition(record.OpenCondition) == 0)
                {
                    return true;
                }
                return false;
            }

            return false;
        }

        private bool CheckIsShowOpenDesc(DynamicActivityRecord record)
        {
            var status = CheckActOpen(record.Id);
            if (status == 0)
            {
                return true;
            }
            else if (status != 1)
            {
                // 周循环 不符合条件直接变灰
                if (!GameUtils.CheckIsWeekLoopOk(record.WeekLoop))
                {
                    return true;
                }
            }


            if (record.OpenCondition == -1)
            {
                return false;
            }

            if (PlayerDataManager.Instance.CheckCondition(record.OpenCondition) == 0)
            {
                return false;
            }

            return true;
        }

        private bool kaifuActInit;
        private HashSet<int> kaiFuChangeAct = new HashSet<int>();
        private int CheckActOpen(int actId)
        {
            if (!kaifuActInit)
            {
                Table.ForeachKaiFu(record =>
                {
                    for (var i = 0; i < record.Week.Length; ++i)
                    {
                        var act = record.Week[i];
                        if (act > 0)
                        {
                            kaiFuChangeAct.Add(act);
                        }
                    }
                    return true;
                });
                kaifuActInit = true;
            }

            if (kaiFuChangeAct.Contains(actId))
            {
                var openId = GameUtils.GetOpenServerAct();
                if (openId <= 0)
                    return -1;

                if (openId != actId)
                {
                    return 0;
                }
                return 1;
            }

            return -1;
        }

        private bool CheckIsGrey(DynamicActivityRecord record, int enterCount, int maxCount, ref ActivityCellDataModel cell)
        {
            if (cell.fuBenId == -1 && record.Type == 0) // 副本类型 但是没匹配到可以进入的副本
            {
                return true;
            }

            if (cell.fuBenId == record.DefaultFB && record.Type == 0) // 副本类型 但是没匹配到可以进入的副本 设置为了默认副本
            {
                return true;
            }

            // 未开启
            var status = CheckActOpen(record.Id);
            if (status == 0)
            {
                return true;
            }
            else if (status != 1)
            {
                // 周循环 不符合条件直接变灰
                if (!GameUtils.CheckIsWeekLoopOk(record.WeekLoop))
                {
                    return true;
                }            
            }

        

            // 剩余次数为0时灰掉    未达成副本进入条件是灰掉
            if (record.Id == 14 || record.Id == 11 || record.Id == 12 || record.Id == 0 || record.Id == 5 || record.Id == 6 || record.Id == 4 || record.Id == 1 || record.Id == 7)
            {
                if (record.OpenCondition == -1 && maxCount > enterCount)
                {
                    return false;
                }

                if (PlayerDataManager.Instance.CheckCondition(record.OpenCondition) == 0 && maxCount > enterCount)
                {
                    return false;
                }
            }
            else
            {
                if (record.OpenCondition == -1)
                {
                    return false;
                }

                if (PlayerDataManager.Instance.CheckCondition(record.OpenCondition) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnClickCloseSecondUI(IEvent ievent)
        {
            if (ReturnSecond)
            {
                var e = ievent as UIEvent_NewActivityCloseSecUIEvent;
                if (e == null)
                {
                    return;
                }
                m_DataModel.PageId = -1;
            }
            else
            {            
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ActivityUI));
                ReturnSecond = true;
            }   
        }

        private void OnClickCell(IEvent ievent)
        {
            var e = ievent as UIEvent_NewActivityCellClickEvent;
            if (e == null)
            {
                return;
            }
            OnClickCell(e.CellIndex);
        
        }
        private void OnClickCell(int index)
        {
            m_DataModel.CurSecondUiIndex = index;
            m_DataModel.IsShowSearchDyn = true;
            var tbPageId = -1;
            var actType = -1;
            var tabData = m_DataModel.tabModel[m_DataModel.CurTabIndex];
            if (tabData != null && tabData.cells != null && tabData.cells[m_DataModel.CurSecondUiIndex] != null)
            {
                var cellData = tabData.cells[m_DataModel.CurSecondUiIndex];
                if (cellData != null)
                {
//                 if (tabData.cells[m_DataModel.CurSecondUiIndex].isGrey) //如果是灰的直接不让点开
//                 {
//                     GameUtils.ShowHintTip(45001);
//                     return;
//                 }

                    var tbAct =  Table.GetDynamicActivity(cellData.tableId);
                    if (tbAct != null)
                    {
                        tbPageId = tbAct.PageId;
                        actType = tbAct.Type;

                        if (tbAct.Id == 1 || tbAct.Id == 6 || tbAct.Id == 11 || tbAct.Id == 12)
                        {
                            int getCheckType = PlayerDataManager.Instance.CheckCondition(tbAct.OpenCondition);
                            if (getCheckType != 0)
                            {
                                m_DataModel.IsShowSearchDyn = getCheckType == 0;
                                m_DataModel.teamState = 3;
                                TeamModule.isShowTeam = getCheckType == 0;
                            }
                            else
                            {
                                //开启        
                                m_DataModel.IsShowSearchDyn = !IsLeader();
                                m_DataModel.teamState = IsHavaTeam();
                                TeamModule.isShowTeam = IsLeader();                                
                            }
                            activityId = tbAct.Id;
                        }                        
                    }

                    //禅道3342需求
                    var tbFuBrn = Table.GetFuben(cellData.fuBenId);
                    if (tbFuBrn != null)
                    {
                        if (tbFuBrn.MainType == (int)eDungeonMainType.Activity ||
                            tbFuBrn.MainType == (int)eDungeonMainType.ScrollActivity)
                        {
                            if (tbFuBrn.AssistType == (int)eDungeonAssistType.CityGoldSingle ||
                                tbFuBrn.AssistType == (int)eDungeonAssistType.CityExpSingle ||
                                tbFuBrn.AssistType == (int)eDungeonAssistType.OrganRoom ||
                                tbFuBrn.AssistType == (int)eDungeonAssistType.AncientBattlefield)
                            {
                                m_DataModel.IsShowSearchDyn = false;
                                m_DataModel.teamState = 3;
                                TeamModule.isShowTeam = false;
                            }
                        }
                    }

                    //TeamModule.isLeader = false;
                    //if (tbAct.Id == 1 || tbAct.Id == 6 || tbAct.Id == 11 || tbAct.Id == 12)
                    //{
                    //    TeamModule.isLeader = IsLeader();
                    //    activityId = tbAct.Id;
                    //}
                }
            }

            if (actType == -1)
            {
                return;
            }

            if (actType == 1) // 答题
            {
                var tbDy = Table.GetDynamicActivity(3);
                GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);
            }
            else if (actType == 2) // 爬塔
            {
                var tbDy = Table.GetDynamicActivity(10);
                GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);
            }
            else if (actType == 4) //古域战场
            {
                var tbDy = Table.GetDynamicActivity(16);
                GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);
            }
            else if (actType == 5) //BOSS之家
            {
                var tbDy = Table.GetDynamicActivity(17);
                GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);
            }
            else
            {
                if (tbPageId == -1)
                {
                    var activityData = tabData.cells[m_DataModel.CurSecondUiIndex];
                    if (activityData.tableId == 18)
                    {
                        var tbDy = Table.GetDynamicActivity(18);
                        GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);
                    }
                    else
                    {
                        return;
                    }
                }
            
                // 设置二级界面Model
                if (RefreshCurSecondData(m_DataModel.CurTabIndex, index))
                {
                    m_DataModel.PageId = tbPageId;
                }
                else
                {
                    GameUtils.ShowHintTip(45001);
                }
            }
        }
        private TeamDataModel DataModule
        {
            get { return PlayerDataManager.Instance.TeamDataModel; }
            set { PlayerDataManager.Instance.TeamDataModel = value; }
        }
        int IsHavaTeam()
        {
            bool hasTeam = false;
            for (int i = 0; i < DataModule.TeamList.Count; i++)
            {
                if (DataModule.TeamList[i].Guid > 0)
                {
                    hasTeam = true;
                    break;
                }
            }
            if (!hasTeam) //没队伍 0 
                return 0;
            var myUid = PlayerDataManager.Instance.GetGuid();
            if (myUid == DataModule.TeamList[0].Guid) //是队长
                return 2;
            else //不是队长
                return 1;
        }
        //界面上各种按钮的响应函数
        private void OnBtnClicked(IEvent ievent)
        {          
            var e = ievent as UIEvent_ButtonClicked;
            switch (e.Type)
            {
                case BtnType.Activity_Enter: //进入活动
                {
                    var checkResult = CheckEnterCondition(m_DataModel.curSecondModel);
                    if (checkResult == CheckConditionResult.None)
                    {
                        return;
                    }
                    if (m_DataModel.curSecondModel.timeState != 0)
                    {
                        //可以进副本
                    }
                    else
                    {
                        //当前不在副本开启时间
                        GameUtils.ShowHintTip(494);
                        return;
                    }

                    var tbFuben = Table.GetFuben(m_DataModel.curSecondModel.fuBenId);
                    if (checkResult == CheckConditionResult.Team)
                    {
                        if (QueueUpData.QueueId == -1)
                        {
                            EnterTeamDungeon(m_DataModel.curSecondModel.fuBenId);
                        }
                        else if (QueueUpData.QueueId == tbFuben.QueueParam)
                        {
                            MatchingCancel(tbFuben.QueueParam, m_DataModel.curSecondModel);
                            EnterTeamDungeon(m_DataModel.curSecondModel.fuBenId);
                        }
                        else
                        {
                            //正在排别的副本，是否进入
                            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270218, "", () =>
                            {
                                EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                                EnterTeamDungeon(m_DataModel.curSecondModel.fuBenId);
                            });
                        }
                    }
                    else if (checkResult == CheckConditionResult.Single)
                    {
                        if (QueueUpData.QueueId == -1)
                        {
                            GameUtils.EnterFuben(tbFuben.Id);
                        }
                        else if (QueueUpData.QueueId == tbFuben.QueueParam)
                        {
                            MatchingCancel(tbFuben.QueueParam, m_DataModel.curSecondModel);
                            GameUtils.EnterFuben(tbFuben.Id);
                        }
                        else
                        {
                            //正在排别的副本，是否进入
                            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270218, "", () =>
                            {
                                EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                                GameUtils.EnterFuben(tbFuben.Id);
                            });
                        }
                    }
                }
                    break;

                case BtnType.DynamicActivity_Enter: //进入动态活动副本
                {
                    var checkResult = CheckEnterCondition(m_DataModel.curSecondModel);
                    if (checkResult == CheckConditionResult.None)
                    {
                        return;
                    }
                    if (m_DataModel.curSecondModel.timeState != 0)
                    {
                        //可以进副本
                    }
                    else
                    {
                        //当前不在副本开启时间
                        GameUtils.ShowHintTip(494);
                        return;
                    }

                    var tbFuben = Table.GetFuben(m_DataModel.curSecondModel.fuBenId);
                    if (tbFuben.AssistType == (int)eDungeonAssistType.AncientBattlefield) // 古战场
                    {
                        var restPlayTime = tbFuben.TimeLimitMinutes * 60 - m_DataModel.curSecondModel.enterCount;
                        if (restPlayTime <= 0)
                        {
                            //没有时间了
                            GameUtils.ShowHintTip(457);
                            return;
                        }
                    }

                    if (m_DataModel.curSecondModel.tableId == 9)//灵兽岛判断体力
                    {
                        if (PlayerDataManager.Instance.GetExData(eExdataDefine.e630) <= 0)
                        {
                            var needDiamond = Table.GetClientConfig(934).ToInt();
                            var tiliCount = Table.GetClientConfig(935).ToInt();
                            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                                string.Format(GameUtils.GetDictionaryText(210124), needDiamond, tiliCount, GameUtils.GetDictionaryText(270306)), "",
                                () => { NetManager.Instance.StartCoroutine(OnClickBuyTili()); }, null, false, true);

                            return;
                        }
                    }

                    if (checkResult == CheckConditionResult.Team)
                    {
                        if (QueueUpData.QueueId == -1)
                        {
                            EnterTeamDungeon(m_DataModel.curSecondModel.fuBenId);
                        }
                        else if (QueueUpData.QueueId == tbFuben.QueueParam)
                        {
                            MatchingCancel(tbFuben.QueueParam, m_DataModel.curSecondModel);
                            EnterTeamDungeon(m_DataModel.curSecondModel.fuBenId);
                        }
                        else
                        {
                            //正在排别的副本，是否进入
                            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270218, "", () =>
                            {
                                EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                                EnterTeamDungeon(m_DataModel.curSecondModel.fuBenId);
                            });
                        }
                    }
                    else if (checkResult == CheckConditionResult.Single)
                    {

                        //蛮荒孤岛组队状态不能进入
                        if (m_DataModel.curSecondModel.fuBenId >= 20000 && m_DataModel.curSecondModel.fuBenId <= 20005)
                        {
                            var teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
                            if (teamData == null) return;
                            if (teamData.HasTeam)
                            {
                                GameUtils.ShowHintTip(100001486);
                                return;
                            }
                        }

                        if (QueueUpData.QueueId == -1)
                        {
                            GameUtils.EnterFuben(tbFuben.Id);
                        }
                        else if (QueueUpData.QueueId == tbFuben.QueueParam)
                        {
                            MatchingCancel(tbFuben.QueueParam, m_DataModel.curSecondModel);
                            GameUtils.EnterFuben(tbFuben.Id);
                        }
                        else
                        {
                            //正在排别的副本，是否进入
                            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270218, "", () =>
                            {
                                EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                                GameUtils.EnterFuben(tbFuben.Id);
                            });
                        }
                    }
                }
                    break;

                case BtnType.Activity_Queue: //预约活动
                case BtnType.DynamicActivity_Queue: //预约活动
                {
                    if (m_DataModel.curSecondModel.timeState == 1)
                    {
                        if (m_DataModel.curSecondModel.QueueState == 0)
                        {
                            GameUtils.ShowHintTip(496);
                            return;
                        }
                    }

                    var checkResult = CheckEnterCondition(m_DataModel.curSecondModel);
                    if (checkResult == CheckConditionResult.None)
                    {
                        return;
                    }

                    var tbFuben = Table.GetFuben(m_DataModel.curSecondModel.fuBenId);

                    if (tbFuben == null) return;

                    //蛮荒孤岛组队状态不能预约
                    if (m_DataModel.curSecondModel.fuBenId >= 20000 && m_DataModel.curSecondModel.fuBenId <= 20005)
                    {
                        var teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
                        if (teamData == null) return;
                        if (teamData.HasTeam)
                        {
                            GameUtils.ShowHintTip(100001485);
                            return;
                        }                     
                    }

                    if (QueueUpData.QueueId == -1)
                    {
                        MatchingStart(tbFuben.QueueParam);
                    }
                    else if (QueueUpData.QueueId == tbFuben.QueueParam)
                    {
                        MatchingCancel(tbFuben.QueueParam, m_DataModel.curSecondModel);
                    }
                    else
                    {
                        //正在排别的副本，是否取消并预约本活动
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 41004, "", () =>
                        {
                            NextQueueId = tbFuben.QueueParam;
                            EventDispatcher.Instance.AddEventListener(QueueCanceledEvent.EVENT_TYPE, OnOtherQueueCanceled);
                            mCoSafeRemoveListener = RemoveQueueCanceledEventListener(5f);
                            EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                        });
                    }
                }
                    break;
 
                case BtnType.Activity_FlytoMonster:
                {
                    var tbVip = PlayerDataManager.Instance.TbVip;
                    if (tbVip.SceneBossTrans == 0)
                    {
                        do
                        {
                            tbVip = Table.GetVIP(tbVip.Id + 1);
                        } while (0 == tbVip.SceneBossTrans);

                        GameUtils.GuideToBuyVip(tbVip.Id);
                        return;
                    }
                    if (GotoMonster(e.Type) == false)
                        break;

                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ActivityUI));
                }
                    break;
                case BtnType.Activity_GotoMonster:
                {
                    if (GotoMonster(e.Type) == false)
                        break;

                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ActivityUI));
                }
                    break;

                case BtnType.Activity_GetDoubleExp:
                {
                    if (int.Parse(m_DataModel.curSecondModel.LeiJiExp) <= 0)
                    {
                        GameUtils.ShowNetErrorHint((int)ErrorCodes.Error_ExpNotEnough);
                        return;
                    }

                    if (PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes) < int.Parse(m_DataModel.curSecondModel.NeedDiamond))
                    {
                        GameUtils.ShowNetErrorHint((int)ErrorCodes.DiamondNotEnough);
                        return;
                    }
                    var mActivityId = 0;
                    if (m_DataModel.curSecondModel.tableId == 12)
                    {
                        mActivityId = 1;
                    }
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                        string.Format(GameUtils.GetDictionaryText(100001121), m_DataModel.curSecondModel.NeedDiamond), "",
                        () => { NetManager.Instance.StartCoroutine(ApplyBuyLeijiExp(mActivityId)); }, null, false, true);
                }
                    break;
            }
        }
        private IEnumerator ApplyBuyLeijiExp(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TakeMultyExpAward(type);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {

                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....ApplyBuyLeijiExp.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Warn(".....ApplyBuyLeijiExp.......{0}.", msg.State);
                }
            }
        }
        private bool GotoMonster(BtnType type)
        {
            var myLevel = PlayerDataManager.Instance.GetLevel();
            if (m_DataModel == null || m_DataModel.tabModel == null || 
                m_DataModel.tabModel.Count <= m_DataModel.CurTabIndex || m_DataModel.tabModel[m_DataModel.CurTabIndex].cells.Count <= m_DataModel.CurSecondUiIndex)
            {
                return false;
            }
            var btns = m_DataModel.tabModel[m_DataModel.CurTabIndex].cells[m_DataModel.CurSecondUiIndex].worldMosnterBtns;
            var curBtn = btns.CurBtn;
            var tbBoss = Table.GetWorldBOSS(curBtn.TableId);
            if (tbBoss == null)
            {
                return false;
            }
            var tbSceneNpc = Table.GetSceneNpc(tbBoss.SceneNpc);
            if (tbSceneNpc == null)
            {
                return false;
            }
            var tbScene = Table.GetScene(tbSceneNpc.SceneID);
            if (tbScene == null)
            {
                return false;
            }
            if (tbScene.IsPublic != 1)
            {
                //场景未开放
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005011));
                return false;
            }
            if (tbScene.LevelLimit > myLevel)
            {
                //等级不足
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210110));
                return false;
            }

            ObjManager.Instance.MyPlayer.LeaveAutoCombat();

            if (type == BtnType.Activity_FlytoMonster)
            {
                if (tbSceneNpc.PosX >= 0.0 && tbSceneNpc.PosX >= 0.0)
                {
                    GameUtils.FlyTo(tbSceneNpc.SceneID, (float)tbSceneNpc.PosX, (float)tbSceneNpc.PosZ);
                }
                else
                {
                    GameUtils.FlyTo(tbScene.Id, (float)tbScene.Entry_x, (float)tbScene.Entry_z);
                }
            }
            else
            {
                if (tbSceneNpc.PosX >= 0.0 && tbSceneNpc.PosX >= 0.0)
                {
                    var command = GameControl.GoToCommand(tbSceneNpc.SceneID, (float)tbSceneNpc.PosX, (float)tbSceneNpc.PosZ, 1.0f);
                    GameControl.Executer.ExeCommand(command);
                }
                else
                {
                    var command = GameControl.GoToCommand(tbScene.Id, (float)tbScene.Entry_x, (float)tbScene.Entry_z, 1.0f);
                    GameControl.Executer.ExeCommand(command);
                }
            }

            return true;
        }

        //用来移除对QueueCanceledEvent事件的监听
        private Coroutine RemoveQueueCanceledEventListener(float sec)
        {
            if (mCoSafeRemoveListener != null)
            {
                NetManager.Instance.StopCoroutine(mCoSafeRemoveListener);
                mCoSafeRemoveListener = null;
            }
            return NetManager.Instance.StartCoroutine(RemoveQueueCanceledEventListenerCoroutine(sec));
        }

        private IEnumerator RemoveQueueCanceledEventListenerCoroutine(float sec)
        {
            yield return new WaitForSeconds(sec);
            EventDispatcher.Instance.RemoveEventListener(QueueCanceledEvent.EVENT_TYPE, OnOtherQueueCanceled);
            mCoSafeRemoveListener = null;
        }

        //其它的排队已经取消了，现在可以排新的队了
        private void OnOtherQueueCanceled(IEvent ievent)
        {
            MatchingStart(NextQueueId);
            EventDispatcher.Instance.RemoveEventListener(QueueCanceledEvent.EVENT_TYPE, OnOtherQueueCanceled);
            mCoSafeRemoveListener = null;
        }

        private void OnQueneUpdated(IEvent ievent)
        {
            UpdateQueue();
        }
        private void UpdateQueue(bool isRefreshCurData = true)
        {
            if (m_DataModel == null || m_DataModel.tabModel == null)
            {
                return;
            }
            var data = QueueUpData;
            var queueId = data.QueueId;
            if (queueId == -1)
            {
                for (int i = 0; i < m_DataModel.tabModel.Count; i++)
                {
                    for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                    {
                        m_DataModel.tabModel[i].cells[j].QueueState = 0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_DataModel.tabModel.Count; i++)
                {
                    for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                    {
                        var tbFuben = Table.GetFuben(m_DataModel.tabModel[i].cells[j].fuBenId);
                        if (tbFuben != null)
                        {
                            if (tbFuben.QueueParam == queueId)
                            {
                                m_DataModel.tabModel[i].cells[j].QueueState = 1;
                            }
                            else
                            {
                                m_DataModel.tabModel[i].cells[j].QueueState = 0;
                            }
                        }
                    }
                }
            }

            if (isRefreshCurData)
            {
                RefreshCurTabData(m_DataModel.CurTabIndex);
                RefreshCurSecondData(m_DataModel.CurTabIndex, m_DataModel.CurSecondUiIndex);
            }
        }
        //预约活动副本
        private void MatchingStart(int queueId)
        {
            NetManager.Instance.StartCoroutine(MatchingStartCoroutine(queueId));
        }

        private IEnumerator MatchingStartCoroutine(int queueId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.MatchingStart(queueId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                    }
                    else
                    {
                        var tbQueue = Table.GetQueue(queueId);
                        if (tbQueue != null && DealWithErrorCode(msg.ErrorCode, tbQueue.Param, msg.Response.CharacterId))
                        {
                        }
                        else
                        {
                            GameUtils.ShowNetErrorHint(msg.ErrorCode);
                            Logger.Error(".....MatchingStart.......{0}.", msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    Logger.Warn(".....MatchingStart.......{0}.", msg.State);
                }
            }
        }

        //取消预约活动副本
        private void MatchingCancel(int queueId, ActivityCellDataModel fuben)
        {
            NetManager.Instance.StartCoroutine(MatchingCancelCoroutine(queueId, fuben));
        }

        private IEnumerator MatchingCancelCoroutine(int queueId, ActivityCellDataModel fuben)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.MatchingCancel(queueId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        fuben.QueueState = 0;
                        QueueUpData.QueueId = -1;
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_WindowShowDungeonQueue(Game.Instance.ServerTime,
                            -1));
                        EventDispatcher.Instance.DispatchEvent(new QueueCanceledEvent());
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....MatchingCancel.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....MatchingCancel.......{0}.", msg.State);
                }
            }
        }

        private bool DealWithErrorCode(int errCode, int fubenId, List<ulong> playerIds)
        {
            if (DealErrs.Keys.Contains(errCode))
            {
                var dicId = DealErrs[errCode];
                if (playerIds.Count <= 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(dicId));
                }
                else
                {
                    var teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
                    var team = teamData.TeamList.Where(p => p.Guid != 0ul && p.Level > 0);
                    var players = team.Where(p => playerIds.Contains(p.Guid));
                    var names = players.Aggregate(string.Empty, (current, p) => current + (p.Name + ","));
                    if (names.Length <= 0)
                    {
                        return true;
                    }
                    //特殊处理！！！
                    if (errCode == (int)ErrorCodes.Error_LevelNoEnough)
                    {
                        var tbFuben = Table.GetFuben(fubenId);
                        var assistType = (eDungeonAssistType)tbFuben.AssistType;
                        if (assistType == eDungeonAssistType.BloodCastle || assistType == eDungeonAssistType.DevilSquare)
                        {
                            var playerData = PlayerDataManager.Instance;
                            var fubenCount = playerData.GetExData(tbFuben.TotleExdata);
                            if (fubenCount > 0)
                            {
                                dicId = 489;
                            }
                            else
                            {
                                dicId = 491;
                            }
                        }
                    }
                    names = names.Substring(0, names.Length - 1);
                   /** bool isSpecial = false;
                    if (fubenId > 4000 && fubenId < 4008)
                        isSpecial = true;
                    else if (fubenId > 5000 && fubenId < 5008)
                        isSpecial = true;
                    if (isSpecial) dicId = 100002240;**/
                    var content = string.Format(GameUtils.GetDictionaryText(dicId), names);
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(content));
                }
                return true;
            }
            return false;
        }

        private enum CheckConditionResult
        {
            None, //不符合条件

            OkNotFuben,   //符合条件，不是副本
            Single,       //符合条件，且是单人
            Team          //符合条件，且是组队
        }

        //检查进副本的条件（是队伍还是个人，以及是否满足进入条件）
        private CheckConditionResult CheckEnterCondition(ActivityCellDataModel curActivity)
        {
            switch (curActivity.type)
            {
                case 0: //副本
                {
                    //蛮荒孤岛
                    //if(curActivity.fuBenId == 20000)
                    //{
                    //    if(PlayerDataManager.Instance.GetLevel() < 100)
                    //    {
                    //        GameUtils.ShowHintTip(45001);
                    //        return CheckConditionResult.None;
                    //    }
                    //}
                    if (curActivity.fuBenId == -1)
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }
                    var tbFuben = Table.GetFuben(curActivity.fuBenId); //此处只用来判断，是不是组队副本 所以无需按等级去取得副本ID
                    if (tbFuben == null)
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }

                    {
                        //判断活动进入条件是否满足
                        int getCheckType = PlayerDataManager.Instance.CheckCondition(tbFuben.EnterConditionId);
                        if (getCheckType != 0)
                        {
                            GameUtils.ShowHintTip(getCheckType);
                            return CheckConditionResult.None;
                        }
                    }

                    if (CheckActOpen(curActivity.tableId) == 0)
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }

                    // 周循环
                    var tbDynicActivity = Table.GetDynamicActivity(curActivity.tableId);
                    if (tbDynicActivity == null)
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }

                    if (!GameUtils.CheckIsWeekLoopOk(tbDynicActivity.WeekLoop)) //如果不满足周循环
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }

                    var teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
                    if (teamData == null)
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }
                    var team = teamData.TeamList.Where(p => p.Guid != 0ul && p.Level > 0);
                    var teamCount = team.Count();
                    if (teamCount == 0 || tbFuben.CanGroupEnter != 1)
                    {
                        return CheckSingleCondition(curActivity);
                    }
                    return CheckTeamCondition(curActivity);

                    // 保底如果灰色不让进
                    if (curActivity.isGrey)
                    {
                        GameUtils.ShowHintTip(240178);
                        return CheckConditionResult.None;
                    }
                }
                case 1: //答题
                {
                    var idAnswer = curActivity.tableId;
                    if (idAnswer != null)
                    {
                        var tbAnswer = Table.GetDailyActivity(idAnswer);
                        if (tbAnswer != null)
                        {
                            if (PlayerDataManager.Instance.CheckCondition(tbAnswer.OpenCondition) == 0)
                            {
                                return CheckConditionResult.OkNotFuben;
                            }
                            else
                            {
                                return CheckConditionResult.None;
                            }
                        }
                        else
                        {
                            return CheckConditionResult.None;
                        }                  
                    }
                    else
                    {
                        return CheckConditionResult.None;
                    }               
                }
                case 2: //爬塔
                {
                    if (PlayerDataManager.Instance.CheckCondition(2014) == 0)
                    {
                        return CheckConditionResult.OkNotFuben;
                    }
                    else
                    {
                        GameUtils.ShowHintTip(45001);
                        return CheckConditionResult.None;
                    }
                }
                case 3: //全天
                {
                    return CheckConditionResult.OkNotFuben;
                }
                default:
                    break;
            }

            GameUtils.ShowHintTip(45001);
            return CheckConditionResult.None;
        }

        //检查单人进副本的条件
        private CheckConditionResult CheckSingleCondition(ActivityCellDataModel activity)
        {
            if (activity.fuBenId == -1)
            {
                return CheckConditionResult.None;
            }

            var tbFuben = Table.GetFuben(activity.fuBenId);
            if (tbFuben == null)
            {
                Logger.Error("In CheckSingleCondition(), tbFuben == null, fubenId = {0}", activity.fuBenId);
                return CheckConditionResult.None;
            }

            var playerData = PlayerDataManager.Instance;
            var maxCount = tbFuben.TodayCount;
            if (tbFuben.AssistType == 4)
            {
                //恶魔
                maxCount += playerData.TbVip.DevilBuyCount;
            }
            else if (tbFuben.AssistType == 5)
            {
                //血色
                maxCount += playerData.TbVip.BloodBuyCount;
            }

            var enterExdata = tbFuben.TodayCountExdata;
            var enterCount = playerData.GetExData(enterExdata);
            if (tbFuben.TodayCount != -1 && enterCount >= maxCount)
            {
                //副本次数达到上限
                GameUtils.ShowHintTip(490);
                return CheckConditionResult.None;
            }

            return CheckItemEnough(tbFuben);
        }

        //检查组队进副本的条件
        private CheckConditionResult CheckTeamCondition(ActivityCellDataModel activity)
        {
            // Team
            var teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
            if (teamData == null)
            {
                return CheckConditionResult.None;
            }
            var team = teamData.TeamList.Where(p => p.Guid != 0ul && p.Level > 0);

            if (teamData.TeamList[0].Guid != ObjManager.Instance.MyPlayer.GetObjId())
            {
                //我不是队长
                GameUtils.ShowHintTip(440);
                return CheckConditionResult.None;
            }

            var fubenId1 = activity.fuBenId;
            if (fubenId1 == -1)
            {
                return CheckConditionResult.None;
            }
            var tbFuben1 = Table.GetFuben(fubenId1);
            if (tbFuben1 == null)
            {
                return CheckConditionResult.None;
            }
            var tbScene1 = Table.GetScene(tbFuben1.SceneId);
            if (tbScene1 == null)
            {
                return CheckConditionResult.None;
            }
        

            var lvMin = tbScene1.LevelLimit;
            var lvMax = Constants.LevelMax;
            {
                var tbScene2 = Table.GetScene(tbFuben1.SceneId + 1);
                if (tbFuben1.OpenTime[0] != -1 && tbScene2 != null && tbScene2.Name == tbScene1.Name)
                {
                    lvMax = tbScene2.LevelLimit;
                }            
            }

            if (!IsSpecialActivity(fubenId1))
            {
                //检查等级
                var name = string.Empty;
                {
                    // foreach(var t in team)
                    var __enumerator5 = (team).GetEnumerator();
                    while (__enumerator5.MoveNext())
                    {
                        var t = __enumerator5.Current;
                        {
                            if (t.Level < lvMin || t.Level > lvMax)
                            {
                                name += t.Name + ",";
                            }
                        }
                    }
                }
                if (name.Length > 0)
                {
                    name = name.Substring(0, name.Length - 1);
                    EventDispatcher.Instance.DispatchEvent(
                        new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(495), name)));
                    return CheckConditionResult.None;
                }
            }
            if (CheckSingleCondition(activity) == CheckConditionResult.None)
            {
                return CheckConditionResult.None;
            }

            if (CheckSingleCondition(activity) == CheckConditionResult.OkNotFuben)
            {
                return CheckConditionResult.OkNotFuben;
            }

            return CheckConditionResult.Team;
        }

        private CheckConditionResult CheckItemEnough(FubenRecord tbFuben)
        {
            for (int i = 0, imax = tbFuben.NeedItemId.Count; i < imax; ++i)
            {
                var id = tbFuben.NeedItemId[i];
                var count = tbFuben.NeedItemCount[i];
                if (id == -1)
                {
                    break;
                }
                if (PlayerDataManager.Instance.GetItemCount(id) < count)
                {
                    //材料不足
                    var tbItem = Table.GetItemBase(id);
                    var content = string.Format(GameUtils.GetDictionaryText(270246), tbItem.Name);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, content, GameUtils.GetDictionaryText(1503),
                        () => { GameUtils.GotoUiTab(26, tbItem.Exdata[3]); });
                    return CheckConditionResult.None;
                }
            }
            return CheckConditionResult.Single;
        }

        private int GetMyLevelFubenId(DynamicActivityRecord record)
        {
            //倒序便利查找适合我等级的副本
            var fuBenIds = record.FuBenID;
            if (fuBenIds == null)
            {
                return -1;
            }

            for (var i = fuBenIds.Count() - 1; i >= 0; --i)
            {
                if (fuBenIds[i] == -1)
                {
                    continue;
                }
                var tbFuben = Table.GetFuben(fuBenIds[i]);
                if (tbFuben == null)
                {
                    continue;
                }
                //副本完成次数为0次，则取引导关id（血色，恶魔）
                if (record.Id == 11 || record.Id == 12)
                {
                    var totalCount = PlayerDataManager.Instance.GetExData(tbFuben.TotleExdata);
                    if (totalCount == 0)
                    {
                        return record.FrontID;
                    }
                }

                var warnDict = PlayerDataManager.Instance.CheckCondition(tbFuben.EnterConditionId);
                if (warnDict != 0)
                {
                    continue;
                }

                return tbFuben.Id;
            }

            //return -1;
            return record.DefaultFB;
        }

        //组队进入活动副本
        private void EnterTeamDungeon(int fubenId)
        {
            var tbFuben = Table.GetFuben(fubenId);
            if (tbFuben == null)
            {
                return;
            }

            var sceneId = GameLogic.Instance.Scene.SceneTypeId;
            if (sceneId == tbFuben.SceneId)
            {
                //已经在此副本当中了
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270081));
                return;
            }

            if (GameLogic.Instance != null && GameLogic.Instance.Scene != null)
            {
                var oldTbScene = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
                var newTbScene = Table.GetScene(sceneId);

                if (oldTbScene != null && newTbScene != null)
                {
                    if (oldTbScene.FubenId != -1 && newTbScene.FubenId != -1)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210123));
                        return;
                    }
                }
            }
            if (PlayerDataManager.Instance.TeamInviteClickFubenID == fubenId)
            {
                int seconds = (System.DateTime.Now - PlayerDataManager.Instance.TeamInviteClickFubenTime).Seconds;
                if (seconds >= 0 && seconds < 5)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001442));
                    return;
                }
            }

            PlayerDataManager.Instance.TeamInviteClickFubenID = fubenId;
            PlayerDataManager.Instance.TeamInviteClickFubenTime = System.DateTime.Now;
            NetManager.Instance.StartCoroutine(EnterTeamDungeonCoroutine(fubenId));
        }

        private IEnumerator EnterTeamDungeonCoroutine(int fubenId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamEnterFuben(fubenId, PlayerDataManager.Instance.ServerId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                    }
                    else if (DealWithErrorCode(msg.ErrorCode, fubenId, msg.Response.Items))
                    {
                        PlayerDataManager.Instance.TeamInviteClickFubenID = 0;
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....EnterTeamDungeonCoroutine.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....EnterTeamDungeonCoroutine.......{0}.", msg.State);
                }
            }
        }

        //怪物列表的点击响应函数（“地图统领”和“黄金部队”的列表）
        private void OnBossBtnClicked(IEvent ievent)
        {
            var e = ievent as BossCellClickedEvent;
            if (e == null)
            {
                return;
            }
            if (m_DataModel == null || m_DataModel.tabModel == null || 
                m_DataModel.tabModel.Count <= m_DataModel.CurTabIndex || m_DataModel.tabModel[m_DataModel.CurTabIndex].cells.Count <= m_DataModel.CurSecondUiIndex)
            {
                return;
            }
            var clickedBtn = e.BtnState;
            if (clickedBtn.Selected)
            {
                return;
            }
            clickedBtn.Selected = true;

            var btns = m_DataModel.tabModel[m_DataModel.CurTabIndex].cells[m_DataModel.CurSecondUiIndex].worldMosnterBtns;//m_DataModel.curSecondModel.worldMosnterBtns;
            btns.CurBtn.Selected = false;
            if (btns.Btns.Count() > btns.CurBtn.Index)
            {
                btns.Btns[btns.CurBtn.Index].Selected = false;
            }

            btns.CurBtn = clickedBtn;
            if (btns.Btns.Count() > clickedBtn.Index)
            {
                btns.Btns[clickedBtn.Index].Selected = true;
            }


            ChangeBossDataId(clickedBtn);

            RefreshCurTabData(m_DataModel.CurTabIndex);
            RefreshCurSecondData(m_DataModel.CurTabIndex, m_DataModel.CurSecondUiIndex);
        }
        //宠物模型展示
        private void ChangeBossDataId(BtnState btnState)
        {
            var tbWorldBoss = Table.GetWorldBOSS(btnState.TableId);
            var tbSceneNpc = Table.GetSceneNpc(tbWorldBoss.SceneNpc);

            if (m_DataModel == null || m_DataModel.tabModel == null ||
                m_DataModel.tabModel.Count <= m_DataModel.CurTabIndex || m_DataModel.tabModel[m_DataModel.CurTabIndex].cells.Count <= m_DataModel.CurSecondUiIndex)
            {
                return;
            }
            m_DataModel.tabModel[m_DataModel.CurTabIndex].cells[m_DataModel.CurSecondUiIndex].worldMosnterBtns.ModelId = tbSceneNpc.DataID;
            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewActivityModelChangeEvent());
        }

        //副本次数发生变化的响应函数
        private void OnDungeonEnterCountUpdate(IEvent ievent)
        {
            var e = ievent as DungeonEnterCountUpdate;
            var dungeonId = e.DungeonId;
            var count = e.Count;
            var tbFuben = Table.GetFuben(dungeonId);
            if (tbFuben == null)
            {
                Logger.Log2Bugly("tbFuben = null");
                return;
            }

            RefreshFubenCount(tbFuben, false);
            return;
        }

        private void RefreshFubenCount(FubenRecord tbFuben, bool isFromOpenUi)
        {
            for (int i = 0; i < m_DataModel.tabModel.Count; i++)
            {
                for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                {
                    var data = m_DataModel.tabModel[i].cells[j];
                    if (data == null)
                    {
                        continue;
                    }
                    if (data.fuBenId > 0 && data.fuBenId == tbFuben.Id)
                    {
                        var enterCount = data.enterCount;
                        var maxCount = data.maxCount;
                        GetEnterAndMaxCount(data.fuBenId, ref enterCount, ref maxCount);
                        //如果是古战场，则需要做特殊处理
                        if (tbFuben.AssistType == (int)eDungeonAssistType.AncientBattlefield)
                        {
                            // 如果是没匹配到  设置为默认的了 不初始化了
                            var tbDy = Table.GetDynamicActivity(data.tableId);
                            if (tbDy == null)
                            {
                                continue;
                            }
                            if (data.fuBenId == tbDy.DefaultFB)
                            {
                                continue;
                            }

                            var restTimeSec = tbFuben.TimeLimitMinutes * 60 - enterCount;
                            if (restTimeSec < 0)
                            {
                                restTimeSec = 0;
                            }

                            data.Time = GameUtils.GetTimeDiffString((int)restTimeSec);
                            data.timeState = 3;
                            data.isShowTime = true;
                            if (restTimeSec > 0)
                            {
                                data.isGrey = false;
                            }
                            else
                            {
                                data.isGrey = true;
                            }

                            //检查，如果当前正在 古战场副本内，则要刷新副本倒计时
                            if (GameLogic.Instance == null)
                            {
                                continue;
                            }
                            if (GameLogic.Instance.Scene == null)
                            {
                                continue;
                            }
                            var tbScene1 = GameLogic.Instance.Scene.TableScene;
                            if (tbScene1.FubenId == -1)
                            {
                                continue;
                            }
                            var tbFuben1 = Table.GetFuben(tbScene1.FubenId);
                            if (tbFuben1.AssistType == (int)eDungeonAssistType.AncientBattlefield)
                            {
                                //是古战场
                                //如果是打开UI调过来的， 不要刷新副本倒计时
                                if (isFromOpenUi)
                                {
                                    data.isShowTime = false;
                                    continue;
                                }
                                var dueTime = Game.Instance.ServerTime.AddSeconds(restTimeSec);
                                EventDispatcher.Instance.DispatchEvent(new NotifyDungeonTime(dueTime.ToBinary()));
                            }
                        }
                        if (enterCount == maxCount)
                        {
                            //血色和恶魔次数满了之后刷新推送
                            if (tbFuben.AssistType == (int)eDungeonAssistType.DevilSquare)
                            {
                                EventDispatcher.Instance.DispatchEvent(new UIEvent_RefreshPush(2, 0));
                            }
                            else if (tbFuben.AssistType == (int)eDungeonAssistType.BloodCastle)
                            {
                                EventDispatcher.Instance.DispatchEvent(new UIEvent_RefreshPush(3, 0));
                            }
                        }
                    }
                }
            }

            RefreshCurTabData(m_DataModel.CurTabIndex);
            RefreshCurSecondData(m_DataModel.CurTabIndex, m_DataModel.CurSecondUiIndex);
        }

        private void OnClickBuyTili(IEvent ievent)
        {
            var e = ievent as OnClickBuyTiliEvent;
            if (e == null)
            {
                return;
            }

            if (e.mType == 0)
            {
                var needDiamond = Table.GetClientConfig(934).ToInt();
                var tiliCount = Table.GetClientConfig(935).ToInt();
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                    string.Format(GameUtils.GetDictionaryText(210124), needDiamond, tiliCount, GameUtils.GetDictionaryText(270306)), "",
                    () => { NetManager.Instance.StartCoroutine(OnClickBuyTili()); }, null, false, true);
            }
            else
            {
                NetManager.Instance.StartCoroutine(OnClickBuyTili());
            }
        }
        private IEnumerator OnClickBuyTili()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.BuyEnergyByType(0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {

                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.DiamondNotEnough)
                    {
                        var e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                        EventDispatcher.Instance.DispatchEvent(e);

                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....PetIsLandBuyTili.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....PetIsLandBuyTili.......{0}.", msg.State);
                }
            }
        }
        private void OpenByClickMainUI(IEvent e)
        {
            if (mNearActId == -1)
            {
                return;
            }
            var secondId = -1;
            var firstId = -1;
            for (int i = 0; i < m_DataModel.tabModel.Count; i++)
            {
                for (var j = 0; j < m_DataModel.tabModel[i].cells.Count; ++j)
                {
                    var data = m_DataModel.tabModel[i].cells[j];
                    if (data == null)
                    {
                        continue;
                    }
                    if (data.tableId == mNearActId)
                    {
                        secondId = j;
                        firstId = i;
                        break;
                    }
                }
            }
            if (secondId == -1 || firstId == -1)
            {
                return;
            }
            var argList = new List<int>();
            argList.Add(secondId);
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ActivityUI, new ActivityArguments
            {
                Tab = firstId,
                Args = argList
            }));
        }

        private void OpenByClickMainUI2(IEvent e)
        {
            if (mBroadCastId == -1)
            {
                return;
            }

            var tbBroad = Table.GetBroadcast(mBroadCastId);
            if (tbBroad == null)
            {
                return;
            }

            if (tbBroad.ConditonId != -1)
            {
                var hint = PlayerDataManager.Instance.CheckCondition(tbBroad.ConditonId);
                if (hint != 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(hint));
                    return;
                }
            }

            if (mBroadCastId == 4)//珍宝商人广播特殊处理
            {
                var sceneNpcId = tbBroad.NpcId;
                var tbSceneNpc = Table.GetSceneNpc(sceneNpcId);
                if (null == tbSceneNpc)
                    return;
                var tbNpcBase = Table.GetNpcBase(tbSceneNpc.DataID);
                if (null == tbNpcBase)
                    return;
                var serveiceId = tbNpcBase.Service[0];
                var tableSerice = Table.GetService(serveiceId);
                if (null == tableSerice)
                    return;
                ObjManager.Instance.MyPlayer.LeaveAutoCombat();
                var ee = new MapSceneDrawPath(new Vector3((int)tbSceneNpc.PosX, 0, (int)tbSceneNpc.PosZ), 1.0f);
                EventDispatcher.Instance.DispatchEvent(ee);

                var command = GameControl.GoToCommand(tbSceneNpc.SceneID, (int)tbSceneNpc.PosX, (int)tbSceneNpc.PosZ, 2.0f);
                GameControl.Executer.PushCommand(command);

                var command1 = new FuncCommand(() => { MissionManager.Instance.EnableNpcFacing(tbSceneNpc.DataID); });
                GameControl.Executer.PushCommand(command1);

                //var command2 = new FuncCommand(() => { MissionManager.Instance.OpenMissionById(tbSceneNpc.DataID); });
                //GameControl.Executer.PushCommand(command2);

                var command3 = new FuncCommand(() =>
                {
                    PlayerDataManager.Instance.isTaskWildShop = true;
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.StoreUI,
                        new StoreArguments { Tab = tableSerice.Param[0], Args = new List<int> { (int)NpcServeType.TreasureShop } }));
                });
                GameControl.Executer.PushCommand(command3);
                return;
            }

            GameUtils.GotoUiTab(tbBroad.UIParam1, tbBroad.UIParam2, tbBroad.UIParam3);
        }

        private void ClientBroadCast(IEvent ievent)
        {
            var e = ievent as ClientBroadCastEvent;
            if (e == null || e.tableId < 0)
            {
                return;
            }

            var tbBroadCast = Table.GetBroadcast(e.tableId);
            if (tbBroadCast == null)
            {
                return;
            }

            if (!GameUtils.CheckIsWeekLoopOk(tbBroadCast.WeekLoop))
            {
                return;
            }

            mBroadCastId = e.tableId;
            if (MainUiTimteRefresh2 == null)
            {
                var time = Game.Instance.ServerTime.AddSeconds(tbBroadCast.TimeDown);
                MainUiTimteRefresh2 = NetManager.Instance.StartCoroutine(RefreshMainUITime2(tbBroadCast.DictId, time));
            }
            else
            {
                NetManager.Instance.StopCoroutine(MainUiTimteRefresh2);
                MainUiTimteRefresh2 = null;

                var time = Game.Instance.ServerTime.AddSeconds(tbBroadCast.TimeDown);
                MainUiTimteRefresh2 = NetManager.Instance.StartCoroutine(RefreshMainUITime2(tbBroadCast.DictId, time));
            }

        }
        private IEnumerator RefreshMainUITime2(int dicId, DateTime time)
        {
            while (true)
            {
                yield return new WaitForSeconds(3.0f);

                var formateStr = GameUtils.GetDictionaryText(dicId);

                var nearestTime = time;
                var deltaTime = nearestTime - Game.Instance.ServerTime;
                if (nearestTime <= Game.Instance.ServerTime)
                {
                    m_DataModel.MainUTimeDown2 = "";
                    yield break;
                }

                if (deltaTime.TotalSeconds > 0)
                {
                    m_DataModel.MainUTimeDown2 = formateStr;
                }
                else
                {
                    m_DataModel.MainUTimeDown2 = "";
                    yield break;
                }
            }
            yield break;
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (m_DataModel == null)
            {
                return null;
            }
            if (name.Equals("Team"))
                return TeamModule;
            return m_DataModel;
        }

        public void Close()
        {
            EventDispatcher.Instance.RemoveEventListener(BossCellClickedEvent.EVENT_TYPE, OnBossBtnClicked);
        }

        //场景加载结束的响应函数
        private void OnLoadSceneOver(IEvent ievent)
        {
            if (MainUiTimteRefresh == null)
            {
                m_DataModel.MainUTimeDown = "";
            }

            if (MainUiTimteRefresh2 == null)
            {
                m_DataModel.MainUTimeDown2 = "";
            }
        }
        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name.Equals("IsDevilSquareMaxCount"))
            {
                foreach (var item in m_DataModel.tabModel)
                {
                    foreach (var cell in item.cells)
                    {
                        if (cell.tableId == 11)
                        {
                            return cell.enterCount == cell.maxCount;
                        }
                    }
                }
            }
            if (name.Equals("IsBloodCastleMaxCount"))
            {
                foreach (var item in m_DataModel.tabModel)
                {
                    foreach (var cell in item.cells)
                    {
                        if (cell.tableId == 12)
                        {
                            return cell.enterCount == cell.maxCount;
                        }
                    }
                }
            }

            return null;
        }

        public void OnShow()
        {
            EventDispatcher.Instance.AddEventListener(BossCellClickedEvent.EVENT_TYPE, OnBossBtnClicked);
            TeamModule.isLeader = IsLeader();
            if (m_DataModel != null)
            {                
                if (m_DataModel.CurTabIndex == 2)
                {
                    var tabData = m_DataModel.tabModel[m_DataModel.CurTabIndex];
                    if (tabData != null && tabData.cells != null && tabData.cells[m_DataModel.CurSecondUiIndex] != null)
                    {
                        var cellData = tabData.cells[m_DataModel.CurSecondUiIndex];
                        if (cellData != null)
                        {
                            var tbAct = Table.GetDynamicActivity(cellData.tableId);
                            if (tbAct != null)
                            {
                                if (tbAct.Id == 1 || tbAct.Id == 6 || tbAct.Id == 11 || tbAct.Id == 12)
                                {
                                    OnClickCell(m_DataModel.CurSecondUiIndex);
                                }
                            }
                        }
                    }                    
                }
            }            
        }

        public FrameState State { get; set; }
        #endregion

        bool IsLeader()
        {
            var myUid = PlayerDataManager.Instance.GetGuid();
            var isLeader = myUid == DataModel.TeamList[0].Guid;
            return isLeader;
        }

        private TeamDataModel DataModel
        {
            get { return PlayerDataManager.Instance.TeamDataModel; }
            set { PlayerDataManager.Instance.TeamDataModel = value; }
        }

        void ChatTeam(IEvent ievent)
        {
            ChatTeamByTargetEvent eve = new ChatTeamByTargetEvent(DataModel.TeamId,
                2,
                activityId,
                0,
                0);
            EventDispatcher.Instance.DispatchEvent(eve);
            ChangeTeam();
        }

        void AutoMatch(IEvent ievent)
        {
            if(TeamModule.isLeader)ChangeTeam(true);
        }

        void SerachTeam(IEvent ievent)
        {
            var recoard = Table.GetDynamicActivity(activityId);
            if (null == recoard) return;
            var targetId = recoard.Id;
            if (recoard.IsOpenTeam == 1)
            {
                targetId = recoard.Id;
            }
            var e = new Show_UI_Event(UIConfig.TeamFrame);
            EventDispatcher.Instance.DispatchEvent(e);
            PlayerDataManager.Instance.NoticeData.DungeonType = 0;
            var e1 = new TeamTargetChangeItemByOther_Event(2, targetId);
            EventDispatcher.Instance.DispatchEvent(e1);
        }
        void ChangeTeam(bool isAuto = false)
        {
            // 获取活动的默认等级
            var recoard = Table.GetDynamicActivity(activityId);
            if (null == recoard) return;

            if (recoard.IsOpenTeam == 1)
            {
                for (int d = 0; d < recoard.FuBenID.Length; d++)
                {
                    var tabFuben = Table.GetFuben(recoard.FuBenID[d]);//GetFuben
                    if (null == tabFuben) continue;
                    var condition = tabFuben.EnterConditionId;
                    var open = false;
                    var enterLevel = 0;
                    open = PlayerDataManager.Instance.CheckCondition(condition) == 0;

                    if (open)
                    {
                        var conditionTab = Table.GetConditionTable(condition);
                        for (int r = 0; r < conditionTab.ItemId.Length; r++)
                        {
                            if (conditionTab.ItemId[r] == 0)
                            {
                                enterLevel = conditionTab.ItemCountMin[r];
                            }
                        }

                        if (enterLevel == 0)
                        {
                            enterLevel = 1;
                        }


                        var maxLevel = 0;
                        var record = Table.GetClientConfig(103);
                        if (null != record)
                            maxLevel = int.Parse(record.Value);

                        
                        if (PlayerDataManager.Instance.currentTeamTarget.isBelongIndex != 2 || PlayerDataManager.Instance.currentTeamTarget.targetItemId != activityId)
                        {
                            var msg = NetManager.Instance.ChangetTeamTarget(2,
                                activityId,
                                enterLevel,
                                maxLevel, 0);
                            msg.SendAndWaitUntilDone();

                            if (msg.State == ScorpionNetLib.MessageState.Reply)
                            {
                                if (msg.ErrorCode == (int)ErrorCodes.Error_ChangeTeamTargetFail_001)
                                {
                                    int dicId = 220133;
                                    GameUtils.ShowHintTip(dicId);
                                }
                            }
                        }
                    
                        break;
                    }
                }
            }
            if (isAuto)
            {
                int isHaveTeam = DataModel.HasTeam == true ? 1 : 0;
                var msg1 = NetManager.Instance.AutoMatchBegin(isHaveTeam, 2, activityId);
                msg1.SendAndWaitUntilDone();
            }
        }

        private void OnAutoMatchState(IEvent ievent)
        {
            var e = ievent as AutoMatchState_Event;
            TeamModule.IsShowAutoMatch = e.param == 0;
        }

        private void cancelAutoMatch(IEvent ievent)
        {
            var msg = NetManager.Instance.AutoMatchCancel(1);
            msg.SendAndWaitUntilDone();
        }

        private void DeoPrison(string tim, int fubenId,int timeState = 0,int queueState = 0)
        {
            return;
            if (tim == "30秒" && timeState==1 && queueState==1)
            {
                var sceneType = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
                if (sceneType == null)
                {
                    return;
                }
                if (fubenId >= 100 && fubenId <= 117)
                {
                    if (sceneType.Type != (int)eSceneType.Fuben)
                    {
                        var tbQueue = Table.GetQueue(fubenId);
                        var tbFuben = Table.GetFuben(tbQueue.Param);
                        //是否现在进入：{0}
                        UIManager.Instance.ShowLimitMessage(MessageBoxType.OkCancel,
                            string.Format(GameUtils.GetDictionaryText(270012), tbFuben.Name), "",
                            () =>
                            {
                                var e = new UIEvent_MatchingBack_Event(1);
                                EventDispatcher.Instance.DispatchEvent(e);
                            },
                            () =>
                            {
                                var e = new UIEvent_MatchingBack_Event(0);
                                EventDispatcher.Instance.DispatchEvent(e);
                            }, false, true, true, 10);
                    }
                    else
                    {
                        var tbQueue = Table.GetQueue(fubenId);
                        var tbFuben = Table.GetFuben(tbQueue.Param);
                        //是否现在进入：{0}
                        UIManager.Instance.ShowLimitMessage(MessageBoxType.OkCancel,
                            string.Format(GameUtils.GetDictionaryText(100002230), tbFuben.Name), "",
                            () =>
                            {
                                var e = new UIEvent_MatchingBack_Event(1);
                                EventDispatcher.Instance.DispatchEvent(e);
                            },
                            () =>
                            {
                                var e = new UIEvent_MatchingBack_Event(0);
                                EventDispatcher.Instance.DispatchEvent(e);
                            }, false, true, true, 10);
                    }                
                }
            }
        }
        private ServerListDataModel datamodel;
        private void RefreshLineCom(string tim, int timeState = 0, int queueState = 0)
        {
            var longTime = "";
            if (Table.GetClientConfig(222).ToInt() < 60)
            {
                longTime = Table.GetClientConfig(222).ToInt().ToString();
            }
            else
            {
                longTime = "1分1秒";
            }
            if (tim == longTime && timeState == 1 && queueState==1)
            {
                EventDispatcher.Instance.DispatchEvent(new LimitActiveRefreshLineConEvent());            
            }
        }

        private bool IsSpecialActivity(int activityId)
        {
            bool isSpecial = false;
            if (activityId > 4000 && activityId < 4008)
            {
                isSpecial = true;
            }

            if (activityId > 5000 && activityId < 5008)
            {
                isSpecial = true;
            }

            return isSpecial;
        }
    }
}