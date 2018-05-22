#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class UIMissionTrackListController : IControllerBase
    {
        public UIMissionTrackListController()
        {
            CleanUp();

            var DataModelListCount0 = DataModel.List.Count;
            for (var i = 0; i < DataModelListCount0; i++)
            {
                DataModel.List[i] = new MissionTrackItemDataModel
                {
                    MissionId = -1,
                    Title = "",
                    Track = ""
                };
            }
            var DataModelListCount1 = DataModel.List2.Count;
            for (var i = 0; i < DataModelListCount1; i++)
            {
                DataModel.List2[i] = new MissionTrackItemDataModel
                {
                    MissionId = -1,
                    Title = "",
                    Track = ""
                };
            }
            EventDispatcher.Instance.AddEventListener(Event_UpdateMissionData.EVENT_TYPE, OnEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_DoMissionGoTo.EVENT_TYPE, OnEvent);
            EventDispatcher.Instance.AddEventListener(MissionTraceUpdateEvent.EVENT_TYPE, OnEvent);
            EventDispatcher.Instance.AddEventListener(RefreshDungeonInfoEvent.EVENT_TYPE, RefreshDungeonInfo);
            EventDispatcher.Instance.AddEventListener(MissionTrackUpdateTimerEvent.EVENT_TYPE, OnMissionTrackUpdateTimer);
            EventDispatcher.Instance.AddEventListener(Event_EraProgressUpdate.EVENT_TYPE, EraProgressUpdate);
            EventDispatcher.Instance.AddEventListener(Event_UnlockEraBook.EVENT_TYPE, EraUnlock);
            EventDispatcher.Instance.AddEventListener(Event_OpenEraBook.EVENT_TYPE, OpenEraBook);
            EventDispatcher.Instance.AddEventListener(MissionTrackOpenSwitch.EVENT_TYPE, OnSwitchTask);
            EventDispatcher.Instance.AddEventListener(FreeClickEvent.EVENT_TYPE, OnFreeClick);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnEvent);

        }

        private MissionTrackListDataModel DataModel;
        private int nLastLogicId = -1;
        private string UpdateTimeFormatStr;
        private int UpdateTimeIdx;
        private int UpdateTimeType;
        private int changeToProgress = -1;
        private float changeUpdateTime;
        private bool IsFirst = true;


        private static int GetWeight(MissionBaseModel a)
        {
            int ret = 0;
            try
            {
                int stat = a.Exdata[0]; //状态
                switch (stat)
                {
                    case (int)eMissionState.Finished:
                        stat = 5;
                        break;
                    case (int)eMissionState.Unfinished:
                        stat = 3;
                        break;
                    case (int)eMissionState.Acceptable:
                        stat = 1;
                        break;
                    default:
                        stat = 0;
                        break;
                }

                var missionid = PlayerDataManager.Instance.GetExData(713);
                if (a.MissionId == missionid)
                {
                    ret = 100 * 1000 * 1000;//mainui任务列表支线任务显示追踪任务
                }
                int priority = Table.GetMissionBase(a.MissionId).MissionPriority;
                priority = priority > 0 ? priority : 0;
                ret += stat * 1000 * 1000 + priority * 10 * 1000 * 1000 + a.MissionId;

                //a.Exdata[4] = ret;
            }
            catch
            {
                ret = a.MissionId;
            }
            return ret;
        }
        private static int MissionCompare(MissionBaseModel a, MissionBaseModel b)
        {
            //return a.MissionId < b.MissionId ? 1 : -1;
            int x = GetWeight(b);
            int y = GetWeight(a);

            return GetWeight(b) - GetWeight(a);
        }
        private void OnEvent(IEvent ievent)
        {
            if (PlayerDataManager.Instance.GetLoginApplyState().GetFlag((int)eLoginApplyType.Exdata) == 0)
                return;
            var missionData = MissionManager.Instance.MissionData;
            var idx = 0;
            int[] ColArray = { 100, 101, 102, 102, 102 };

            //任务数据 填入3个列表
            var missionList = new List<MissionBaseModel>[5];
            for (var i = 0; i < missionList.Length; i++)
            {
                missionList[i] = new List<MissionBaseModel>();
            }
            {
                // foreach(var pair in missionData.Datas)
                var __enumerator2 = (missionData.Datas).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var pair = __enumerator2.Current;
                    {
                        var m = pair.Value;
                        var table = Table.GetMissionBase(m.MissionId);
                        if (null == table)
                        {
                            continue;
                        }
                        var type = table.ViewType;
                        CheckMissionType(m.MissionId, ref type);
                        if (type >= 0 && type <= 4)
                        {
                            missionList[type].Add(m);
                        }
                    }
                }
            }

            //根据任务状态排序
            for (var i = 1; i < missionList.Length; i++)
            {
                missionList[i].Sort(MissionCompare);
            }

            if (missionList.Length > 1)
            {
                var infos = EraManager.Instance.GetEraInfos(1);
                for (int i = 0; i < missionList[1].Count; i++)
                {
                    for (int j = 0; j < infos.Count; j++)
                    {
                        if (infos[j].Record.ActiveParam[0] <= 0) continue;
                        if (missionList[1][i].MissionId == infos[j].Record.ActiveParam[0])
                        {
                            EventDispatcher.Instance.DispatchEvent(new UpdateBranchMissionDataEvent(missionList[1][i].MissionId));
                            break;
                        }
                    }
                }
            }


            //特殊处理环任务，如果当前正在做的环任务排在前面
            var tempList = missionList[2];
            if (tempList.Count > 0)
            {
                var id = MissionManager.Instance.CurrentDoingCircleMission;
                if (-1 != id)
                {
                    for (var i = 0; i < tempList.Count; i++)
                    {
                        if (id == tempList[i].MissionId)
                        {
                            var temp = tempList[i];
                            tempList.RemoveAt(i);
                            tempList.Insert(0, temp);
                        }
                    }
                }
            }

            //取3个任务的第一，显示出来
            var DataModelListCount1 = DataModel.List.Count;

            //任务数据
            for (var i = 0; i < DataModelListCount1 && i < missionList.Length; i++)
            {
                DataModel.List[i].MissionId = -1;

                if (missionList[i].Count <= 0)
                {
                    continue;
                }

                var mission = missionList[i][0];
                var id = mission.MissionId;
                var table = Table.GetMissionBase(id);

                var dataModel = DataModel.List[i];
                dataModel.MissionId = table.Id;
                dataModel.Title = table.Name;
                var state = (eMissionState)mission.Exdata[0];
                dataModel.state = (int)state;
                dataModel.Track = (eMissionState.Finished == state
                    ? table.FinishDescription
                    : table.TrackDescription) + MissionManager.MissionContent(table, mission.Exdata);

                dataModel.Col = GameUtils.GetTableColor(ColArray[i]);
            }

            //任务类型列表数据的扩展列表 List2 
            var DataModelListCount2 = DataModel.List2.Count;
            for (var i = 0; i < DataModelListCount2 && i < missionList.Length - DataModelListCount1; i++)
            {
                DataModel.List2[i].MissionId = -1;

                if (missionList[i + DataModelListCount1].Count <= 0)
                {
                    continue;
                }

                var mission = missionList[i + DataModelListCount1][0];
                var id = mission.MissionId;
                var table = Table.GetMissionBase(id);

                var dataModel = DataModel.List2[i];
                dataModel.MissionId = table.Id;
                dataModel.Title = table.Name;
                var state = (eMissionState)mission.Exdata[0];
                dataModel.state = (int)state;
                dataModel.Track = (eMissionState.Finished == state
                                      ? table.FinishDescription
                                      : table.TrackDescription) + MissionManager.MissionContent(table, mission.Exdata);

                dataModel.Col = GameUtils.GetTableColor(ColArray[i + DataModelListCount1]);
            }
            //根据是否还有任务来决定是否显示此任务
            if (DataModel.SwitchType == 0 || 
                ((DataModel.SwitchType == (int)eMissionMainType.Gang || 
                (int)eMissionMainType.Daily == DataModel.SwitchType || 
                (int)eMissionMainType.Farm == DataModel.SwitchType) && 
                missionList[DataModel.SwitchType].Count == 0))
            {
                if (missionList[(int)eMissionMainType.Daily].Count > 0)
                {
                    DataModel.SwitchType = (int)eMissionMainType.Daily;
                    return;
                }
                if (missionList[(int)eMissionMainType.Gang].Count > 0)
                {
                    DataModel.SwitchType = (int)eMissionMainType.Gang;
                    return;
                }
                if (missionList[(int)eMissionMainType.Farm].Count > 0)
                {
                    DataModel.SwitchType = (int)eMissionMainType.Farm;
                    return;
                }
                DataModel.SwitchType = 0;
                //没有帮派任务自动显示日常任务
                //if (missionList[(int)eMissionMainType.Gang].Count == 0)
                //{
                //    DataModel.SwitchType = missionList[(int)eMissionMainType.Daily].Count > 0
                //        ? (int)eMissionMainType.Daily
                //        : 0;
                //}
                ////没有日常任务自动显示帮派任务
                //else if (missionList[(int)eMissionMainType.Daily].Count == 0)
                //{
                //    DataModel.SwitchType = (int)eMissionMainType.Gang;
                //}
                ////日常任务，帮派任务都没有，显示狩猎任务
                //else if (missionList[(int)eMissionMainType.Gang].Count == 0 &&
                //         missionList[(int)eMissionMainType.Daily].Count == 0)
                //{
                //    DataModel.SwitchType = (int)eMissionMainType.Farm;
                //}
                //else
                //{
                //    DataModel.SwitchType = (int)eMissionMainType.Daily;
                //}
            }
            //else if ((DataModel.SwitchType == (int)eMissionMainType.Gang || (int)eMissionMainType.Daily == DataModel.SwitchType || (int)eMissionMainType.Farm == DataModel.SwitchType) && missionList[DataModel.SwitchType].Count == 0)
            //{
            //var other = DataModel.SwitchType == (int)eMissionMainType.Gang ? (int)eMissionMainType.Daily : (int)eMissionMainType.Gang;
            //DataModel.SwitchType = missionList[other].Count > 0 ? other : 0;
            //}
        }
        /// <summary>
        /// 检查任务类型
        /// </summary>
        /// <param name="missionId">任务id</param>
        /// <returns>任务类型</returns>
        private void CheckMissionType(int missionId, ref int type)
        {
            if (type == (int)eMissionMainType.Farm)
            {
                //type = (int)eMissionMainType.Farm;
            }
            else if (missionId > 50000 && missionId < 60000)
            {
                type = (int)eMissionMainType.Gang;
            }
            else if (eMissionMainType.Circle == (eMissionMainType)type)
            {
                type = (int)eMissionMainType.Daily;
            }
        }
        private void OnMissionTrackUpdateTimer(IEvent ievent)
        {
            var now = Game.Instance.ServerTime;
            if (DataModel.TargetTime < now)
            {
                return;
            }
            var infoList = DataModel.FubenInfoList;
            var info = infoList[UpdateTimeIdx];
            var str = string.Format(UpdateTimeFormatStr, GameUtils.GetTimeDiffString(DataModel.TargetTime));
            if (UpdateTimeType == 0)
            {
                info.Title = str;
            }
            else
            {
                info.Track = str;
            }
        }
        private void OnFreeClick(IEvent e)
        {
            SetSwitchBtn(0);
        }
        private void OnSwitchTask(IEvent ievent)
        {
            var e = ievent as MissionTrackOpenSwitch;
            if (e == null)
                return;

            if (DataModel.IsOpenSwitch == 1 && e.tab == -1)
            {
                SetSwitchBtn(0);
                return;
            }
            if (e.tab >= 0)
            {
                if (e.tab == 2 && DataModel.List[2].MissionId <= 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210211));
                    return;
                }
                else if (e.tab == 3 && DataModel.List[3].MissionId <= 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210212));
                    return;
                }
                else if (e.tab == 4 && DataModel.List2[0].MissionId <= 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210213));
                    return;
                }
                DataModel.SwitchType = e.tab;
                //改变标签默认设置当前任务
                SetSwitchBtn(0);
            }
            else
            {
                SetSwitchBtn(1);
            }
        }



        private object mPickIntervalTrigger = null;
        private void SetSwitchBtn(int isOpen)
        {
            DataModel.IsOpenSwitch = isOpen;
            if (isOpen == 1)
            {
                DataModel.strDaily = string.Format(GameUtils.GetDictionaryText(274049), PlayerDataManager.Instance.GetExData(443), Table.GetClientConfig(126).Value.ToInt());
                DataModel.strGuild = string.Format(GameUtils.GetDictionaryText(274050), PlayerDataManager.Instance.GetExData(715), Table.GetClientConfig(127).Value.ToInt());
                DataModel.strHunt = string.Format(GameUtils.GetDictionaryText(274051));
                mPickIntervalTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(3f), () =>
                {
                    DataModel.IsOpenSwitch = 0;
                    TimeManager.Instance.DeleteTrigger(mPickIntervalTrigger);
                    PlayerDataManager.Instance.mPickIntervalTrigger = null;
                });
            }
            else
            {
                if (mPickIntervalTrigger != null)
                {
                    TimeManager.Instance.DeleteTrigger(mPickIntervalTrigger);
                    PlayerDataManager.Instance.mPickIntervalTrigger = null;
                }

            }
        }

        private void OpenEraBook(IEvent ievent)
        {
            var e = ievent as Event_OpenEraBook;
            if (e != null && PlayerDataManager.Instance.NoticeData.GotoEraId > 0)
            {
                var gotoPage = EraManager.Instance.GetJumpToPage(PlayerDataManager.Instance.NoticeData.GotoEraId);
                EraManager.Instance.GotoEraBookPage(gotoPage, true);
            }
            else
            {
                var tempEra = -1;
                var _tempEra = EraManager.Instance.GetEraIdByNotTakeReward();
                var data = DataModel.EraTraceData;

                if (data.IsMax)
                {
                    tempEra = data.CurrentEraId > -1 ? data.CurrentEraId : -1;
                }
                else if (_tempEra != -1)
                {
                    tempEra = _tempEra;
                }
                else
                {
                    tempEra = EraManager.Instance.CurrentEraId - 1 > -1 ? EraManager.Instance.CurrentEraId - 1 : 0;
                }

                if (tempEra != -1)
                {
                    EraManager.Instance.SelectCurrentEra(tempEra);
                }

                if (tempEra > 100)
                {
                    EraManager.Instance.GotoEraBookPage(12, true);
                }
                else
                {
                    EraManager.Instance.GotoEraBookPage(1, true);
                }
            }
            PlayerDataManager.Instance.NoticeData.GotoEraId = -1;
        }
        private void EraUnlock(IEvent ievent)
        {
            var e = ievent as Event_UnlockEraBook;
            PlayerDataManager.Instance.NoticeData.LockEraBook = false;
            if (e != null)
            {
                PlayerDataManager.Instance.NoticeData.GotoEraId = e.EraId;
            }
        }

        private void EraProgressUpdate(IEvent ievent)
        {
            var data = DataModel.EraTraceData;
            var isInit = (data.CurrentEraId == -1);
            if (data.CurrentEraId != EraManager.Instance.CurrentEraId)
            {
                data.CurrentEraId = EraManager.Instance.CurrentEraId;
                data.CurrentProgress = 0;
            }

            //if (data.CurrentEraId != -1)
            {
                //var skillInfo = DataModel.EraSkillInfo;
                //var eraId = data.CurrentEraId;
                //if (eraId != -1)
                //{
                //    skillInfo.EraId = eraId;
                //    var skillId = GetSkillIdByEraId(skillInfo.EraId);
                //    skillInfo.SkillId = skillId;
                //    var altas = GetAtlasBySkillId(skillInfo.SkillId, skillInfo.EraId);
                //    skillInfo.Atlas = altas;
                //}
                SetEraSkillInfo();
            }

            if (data.CurrentEraId == -1)
            {
                data.CurrentEraId = -2;
                data.CurrentEraId = -1;
            }

            data.MaxProgress = EraManager.Instance.MaxProgress * 100;
            changeToProgress = EraManager.Instance.CurrentProgress * 100;
            if (EraManager.Instance.GetCurrentEraState() == 1 && !EraManager.Instance.IsRealMax())
            {
                changeToProgress -= 20;
            }

            if (isInit)
            {
                data.CurrentProgress = changeToProgress;
                SetProgress();
            }

            if (data.CurrentEraId < 0)
            {
                var pageInfo = EraManager.Instance.GetPageInfo(0);
                if (pageInfo != null)
                {
                    var count = pageInfo.eraList.Count;
                    if (PlayerDataManager.Instance.GetFlag(pageInfo.eraList[count - 1].Record.FinishFlagId))
                    {
                        PlayerDataManager.Instance.NoticeData.LockEraBook = false;
                        return;
                    }
                }
                PlayerDataManager.Instance.NoticeData.LockEraBook = true;
            }
        }

        private void SetEraSkillInfo()
        {
            var eraId = -1;
            if (PlayerDataManager.Instance.IsInFubenScnen())
            {
                if (SceneManager.Instance == null) return;
                var fubenId = SceneManager.Instance.GetFubenId();
                var eraInfo = EraManager.Instance.IsEraBookFuben(fubenId);
                if (eraInfo != null)
                {
                    eraId = eraInfo.Record.Id;
                }
            }
            else
            {
                eraId = EraManager.Instance.CurrentEraId;
            }
            if (eraId != -1)
            {
                var skillInfo = DataModel.EraSkillInfo;
                skillInfo.EraId = eraId;
                var skillId = GetSkillIdByEraId(skillInfo.EraId);
                skillInfo.SkillId = skillId;
                var altas = GetAtlasBySkillId(skillInfo.SkillId, skillInfo.EraId);
                skillInfo.Atlas = altas;
            }
        }


        private int GetSkillIdByEraId(int eraId)
        {
            var currSkillId = 0;
            var mayaBaseTb = Table.GetMayaBase(eraId);

            if (mayaBaseTb == null)
            {
                Logger.Debug("GetMayaBase ID == null");
                return currSkillId;
            }
            var roleId = PlayerDataManager.Instance.GetRoleId();
            var skillList = mayaBaseTb.SkillIds;
            if (mayaBaseTb.SkillIds.Count > 0 && mayaBaseTb.SkillIds[0] > 0)
            {
                currSkillId = skillList[roleId];
            }
            else
            {
                currSkillId = -1;
            }
            return currSkillId;
        }


        private string GetAtlasBySkillId(int skillId, int eraId)
        {
            string strAtlas = string.Empty;
            var tbSkill = Table.GetSkill(skillId);
            if (tbSkill == null)
            {
                var tbEra = Table.GetMayaBase(eraId);
                if (tbEra == null)
                {
                    Logger.Debug(" GetMayaBase is Null");
                    return strAtlas;
                }
                var tIcon = Table.GetIcon(tbEra.IconId);
                if (tIcon != null)
                {
                    strAtlas = tIcon.Atlas;
                }
            }
            else
            {
                var tbIcon = Table.GetIcon(tbSkill.Icon);
                if (tbIcon != null)
                {
                    strAtlas = tbIcon.Atlas;
                }
            }
            return strAtlas;
        }


        private void UpdateProgress(int delta)
        {
            var data = DataModel.EraTraceData;
            data.CurrentProgress += delta;
            if (data.CurrentProgress >= changeToProgress)
            {
                data.CurrentProgress = changeToProgress;
                changeToProgress = -1;
            }

            SetProgress();
        }

        private void CheckEraMax()
        {
            var data = DataModel.EraTraceData;
            data.IsMax = EraManager.Instance.IsRealMax();
            var inFuben = PlayerDataManager.Instance.IsInFubenScnen();
            if (inFuben)
            {
                data.IsMax = false;
            }

            if (data.IsMax)
            {
                data.ProgressText = "";
            }
        }

        private void SetProgress()
        {
            var data = DataModel.EraTraceData;
            var num = 0.0;
            if (DataModel.EraTraceData.MaxProgress != 0)
            {
                num = Math.Round(100.0 * data.CurrentProgress / data.MaxProgress, 0);
            }
            data.ProgressText = string.Format("{0}%", num);

            if (data.CurrentProgress >= changeToProgress)
            {
                CheckEraMax();
            }
            data.IsMax = (num == 100);
        }




        private void RefreshDungeonInfo(IEvent ievent)
        {
            var e = ievent as RefreshDungeonInfoEvent;
            var fubenInfo = e.FubenInfo;
            var fubenInfoUnits = fubenInfo.Units;
            var infoList = DataModel.FubenInfoList;

            DataModel.EraBookIconId = -1;

            if (-1 == fubenInfo.LogicId)
            {
                //这种就是发字典id和服务端发来的参数
                for (var j = 0; j < DataModel.FubenInfoList.Count; j++)
                {
                    var data = DataModel.FubenInfoList[j];
                    if (j >= fubenInfo.Units.Count)
                    {
                        data.Title = "";
                        data.Track = "";
                        continue;
                    }
                    var unit = fubenInfo.Units[j];
                    if (-1 == unit.Index)
                    {
                        data.Title = "";
                        data.Track = "";
                        continue;
                    }

                    var temp = new List<object>();
                    for (var idx = 0; idx < unit.Params.Count; idx++)
                    {
                        temp.Add(unit.Params[idx]);
                    }
                    var str = string.Format(GameUtils.GetDictionaryText(unit.Index), temp.ToArray());
                    data.Title = str;
                    data.Track = "";
                    data.InfoIdx = 0;
                }
                return;
            }

            var tbFubenLogic = Table.GetFubenLogic(fubenInfo.LogicId);
            if (tbFubenLogic == null)
            {
                return;
            }
            if (fubenInfo.LogicId.Equals(7851))
            {
                if (IsFirst)
                {
                    var ee = new ShowHpTransitionEvent();
                    EventDispatcher.Instance.DispatchEvent(ee);
                    IsFirst = false;
                }
            }
            var now = Game.Instance.ServerTime;

            if (nLastLogicId != fubenInfo.LogicId)
            {
                nLastLogicId = fubenInfo.LogicId;
                if (DataModel.TargetTime >= now)
                {
                    DataModel.TargetTime = now.AddYears(-10);
                }
            }

            var eraInfo = EraManager.Instance.IsEraBookFuben(GameLogic.Instance.Scene.TableScene.FubenId);
            if (eraInfo != null)
            {
                DataModel.EraBookText = string.Format(GameUtils.GetDictionaryText(596), eraInfo.Record.Name);
                DataModel.EraBookIconId = eraInfo.Record.IconId;
                var dictId = 0;
                var roleId = PlayerDataManager.Instance.GetRoleId();
                if (roleId < eraInfo.Record.FubenDescIds.Count)
                {
                    dictId = eraInfo.Record.FubenDescIds[roleId];
                }
                else if (eraInfo.Record.FubenDescIds.Count > 0)
                {
                    dictId = eraInfo.Record.FubenDescIds[0];
                }

                DataModel.EraDesc = GameUtils.GetDictionaryText(dictId);
                for (var j = 0; j < infoList.Count; ++j)
                {
                    var info = infoList[j];
                    info.InfoIdx = -1;
                    info.Type = 0;
                }
                return;
            }

            //显示副本信息
            var i = 0;
            for (var imax = fubenInfoUnits.Count; i < imax; ++i)
            {
                var unit = fubenInfoUnits[i];
                var info = infoList[i];
                var type = (eFubenInfoType)tbFubenLogic.FubenInfo[i];
                var dicId = tbFubenLogic.FubenParam1[i];
                info.InfoIdx = unit.Index;
                info.Title = GameUtils.GetDictionaryText(dicId);
                switch (type)
                {
                    case eFubenInfoType.KillMonster:
                        {
                            info.Track = unit.Params[0] + "/" + tbFubenLogic.FubenParam2[i];
                        }
                        break;
                    case eFubenInfoType.Percent:
                        {
                            var infoIdx = i;
                            var camp = ObjManager.Instance.MyPlayer.GetCamp();
                            if (camp == 5)
                            {
                                //红方，需要反过来显示
                                infoIdx = 1 - i;
                                unit = fubenInfoUnits[infoIdx];
                            }

                            var descStr = GameUtils.GetDictionaryText(tbFubenLogic.FubenParam2[infoIdx]);
                            info.Track = string.Format(descStr, unit.Params[0], unit.Params[1]);
                        }
                        break;
                    case eFubenInfoType.Score:
                    case eFubenInfoType.PlayerCount:
                        {
                            info.Track = unit.Params[0].ToString();
                        }
                        break;
                    case eFubenInfoType.BattleFieldScore: //寒霜据点的战场积分
                        {
                            var infoIdx = i;
                            if (ObjManager.Instance.MyPlayer.GetCamp() == 4)
                            {
                                //蓝方，需要反过来显示
                                infoIdx = 1 - i;
                                unit = fubenInfoUnits[infoIdx];
                            }
                            info.Track = unit.Params[0] + "/" + tbFubenLogic.FubenParam2[infoIdx];
                        }
                        break;
                    case eFubenInfoType.StrongpointInfo: //据点信息
                        {
                            if (info.Type != 1)
                            {
                                info.Type = 1;
                            }
                            for (int j = 0, jmax = info.States.Count; j < jmax; j++)
                            {
                                info.States[j] = unit.Params[j];
                            }
                        }
                        break;
                    case eFubenInfoType.Timer:
                        {
                            if (DataModel.TargetTime >= now)
                            {
                                break;
                            }
                            UpdateTimeIdx = i;
                            UpdateTimeType = 1;
                            UpdateTimeFormatStr = "{0}";
                            var seconds = unit.Params[0]; //tbFubenLogic.FubenParam2[i];
                            DataModel.TargetTime = now.AddSeconds(seconds);
                            info.Track = GameUtils.GetTimeDiffString(DataModel.TargetTime);
                        }
                        break;
                    case eFubenInfoType.Timer2:
                        {
                            info.Title = string.Empty;
                            info.Track = string.Empty;
                            var seconds = unit.Params[0]; //tbFubenLogic.FubenParam2[i];
                            EventDispatcher.Instance.DispatchEvent(new Event_MieShiStartCountDownData(seconds));
                        }

                        break;
                    case eFubenInfoType.ShowDictionary0:
                        {
                            var descStr = GameUtils.GetDictionaryText(tbFubenLogic.FubenParam2[i]);
                            info.Track = descStr;
                        }
                        break;
                    case eFubenInfoType.ShowDictionary1:
                        {
                            var descStr = GameUtils.GetDictionaryText(tbFubenLogic.FubenParam2[i]);
                            info.Track = string.Format(descStr, unit.Params[0]);
                            if (i == 2)//取副本逻辑信息第三个是圣坛血量
                            {
                                DataModel.iChancelHpPer = unit.Params[0];
                                DataModel.ChanceName = info.Title;
                            }
                        }
                        break;
                    case eFubenInfoType.ShowDictionary2:
                        {
                            var descStr = GameUtils.GetDictionaryText(tbFubenLogic.FubenParam2[i]);
                            info.Track = string.Format(descStr, unit.Params[0], unit.Params[1]);
                        }
                        break;
                    case eFubenInfoType.ShowDictionary3:
                        {
                            var descStr = GameUtils.GetDictionaryText(tbFubenLogic.FubenParam2[i]);
                            info.Track = string.Format(descStr, unit.Params[0], unit.Params[1], unit.Params[2]);
                        }
                        break;
                    case eFubenInfoType.ShowDictionary6:
                        {
                            var descStr = GameUtils.GetDictionaryText(tbFubenLogic.FubenParam2[i]);
                            info.Track = string.Format(descStr, unit.Params[0], unit.Params[1], unit.Params[2], unit.Params[3], unit.Params[4], unit.Params[5]);
                            String[] NpcNameList = GameUtils.SplitString(info.Title, '\n');
                            for (int m = 0; m < DataModel.MonsterHpPerList.Count; m++)
                            {
                                DataModel.MonsterHpPerList[m] = unit.Params[m];
                                if (m < NpcNameList.Length)
                                {
                                    DataModel.BatteryNameList[m] = NpcNameList[m];
                                }
                            }
                            if (unit.Params.Count == 7)
                            {
                                DataModel.iPlayerCount = unit.Params[6];
                            }
                        }
                        break;
                    case eFubenInfoType.AllianceWarInfo:
                        {
                            info.Track = string.Empty;
                            var allianceId = unit.Params[0];
                            var count = unit.Params[1];
                            var dic = PlayerDataManager.Instance._battleCityDic;
                            if (i == 0)
                            {
                                if (!dic.ContainsKey(allianceId))
                                {
                                    info.Title = string.Format(info.Title, GameUtils.GetDictionaryText(270024), count);
                                }
                                else
                                {
                                    info.Title = string.Format(info.Title, dic[allianceId].Name, count);
                                }
                                continue;
                            }
                            if (!dic.ContainsKey(allianceId))
                            {
                                info.Title = string.Empty;
                                continue;
                            }
                            var title = string.Format(info.Title, dic[allianceId].Name, count);
                            var state = (eAllianceWarState)fubenInfoUnits[3].Params[0];
                            if (state == eAllianceWarState.ExtraTime)
                            {
                                UpdateTimeIdx = 0;
                                UpdateTimeType = 0;
                                UpdateTimeFormatStr = string.Empty;
                                DataModel.TargetTime = now.AddYears(-10);
                            }
                            else if (count >= 3)
                            {
                                title += GameUtils.GetDictionaryText(41013);
                                UpdateTimeIdx = i;
                                UpdateTimeType = 0;
                                UpdateTimeFormatStr = title;

                                var time = unit.Params[2];
                                var hour = time / 10000;
                                var min = (time / 100) % 100;
                                var sec = time % 100;
                                DataModel.TargetTime = new DateTime(now.Year, now.Month, now.Day, hour, min, sec);

                                title = string.Format(title, GameUtils.GetTimeDiffString(DataModel.TargetTime));
                            }
                            else
                            {
                                var j = 1;
                                for (; j < 3; j++)
                                {
                                    if (fubenInfoUnits[j].Params[1] >= 3)
                                    {
                                        break;
                                    }
                                }
                                if (j == 3)
                                {
                                    DataModel.TargetTime = now.AddYears(-10);
                                }
                            }
                            info.Title = title;
                        }
                        break;
                    case eFubenInfoType.AllianceWarState:
                        {
                            var state = (eAllianceWarState)unit.Params[0];
                            if (state == eAllianceWarState.ExtraTime)
                            {
                                if (DataModel.TargetTime >= now)
                                {
                                    break;
                                }
                                var title = GameUtils.GetDictionaryText(41014);

                                UpdateTimeIdx = i;
                                UpdateTimeType = 0;
                                UpdateTimeFormatStr = title;

                                var time = unit.Params[1];
                                var hour = time / 10000;
                                var min = (time / 100) % 100;
                                var sec = time % 100;
                                DataModel.TargetTime = new DateTime(now.Year, now.Month, now.Day, hour, min, sec);
                                info.Title = title + string.Format(title, GameUtils.GetTimeDiffString(DataModel.TargetTime));
                            }
                            else
                            {
                                info.Title = string.Empty;
                            }
                            info.Track = string.Empty;
                        }
                        break;
                }
            }
            for (var imax = infoList.Count; i < imax; ++i)
            {
                var info = infoList[i];
                info.InfoIdx = -1;
                info.Type = 0;
            }
        }

        public void OnShow()
        {
        }

        public void Close()
        {
        }

        public void Tick()
        {
            if (changeToProgress >= 0)
            {
                changeUpdateTime += Time.deltaTime;
                if (changeUpdateTime > 0.1f)
                {
                    changeUpdateTime = 0.0f;
                    UpdateProgress(10);
                }
            }
        }

        public void RefreshData(UIInitArguments data)
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void CleanUp()
        {
            DataModel = new MissionTrackListDataModel();
        }

        public void OnChangeScene(int sceneId)
        {
            if (-1 == sceneId)
            {
                return;
            }

            DataModel.TargetTime = Game.Instance.ServerTime.AddYears(-10);
            var tbScene = Table.GetScene(sceneId);
            if (null == tbScene)
            {
                return;
            }
            foreach (var info in DataModel.FubenInfoList)
            {
                for (int i = 0, imax = info.States.Count; i < imax; i++)
                {
                    info.States[i] = -1;
                }
            }
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }
    }
}