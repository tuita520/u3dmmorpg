#region using

using System;
using System.Collections;
using System.Collections.Generic;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ObjCommand;
using ScorpionNetLib;
using UnityEngine;

namespace ScriptManager
{

    #endregion

    public class DialogueData
    {
        public string DialogContent;
        public int NpcDataId; //-1表示自己
        public string Name;
        public int IsBlurBackground ;//是否模糊效果
    }

    public class MissionManager : Singleton<MissionManager>
    {
        //当前正在做的任务
        public int CurrentDoingCircleMission = -1;
        public MissionBaseModel CurrentMissionData;
        //当前正在对话中的任务id
        public MissionBaseRecord CurrentMissionTableData;
        public int NpcId;
        public ulong NpcObjId;
        public int curMissionType = -1;

        public MissionDataModel MissionData
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.Missions; }
        }

        //接受任务
        public void AcceptMission(int id)
        {
            NetManager.Instance.StartCoroutine(AcceptMissionCoroutine(id));
        }

        private IEnumerator AcceptMissionCoroutine(int id)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.AcceptMission(id);
                yield return msg.SendAndWaitUntilDone();

                if (msg.State != MessageState.Reply)
                {
                    Logger.Debug("AcceptMissionCoroutine:MessageState.Timeout");
                    yield break;
                }

                if (msg.ErrorCode != (int) ErrorCodes.OK)
                {
                    UIManager.Instance.ShowNetError(msg.ErrorCode);
                    if ((int) ErrorCodes.Error_AcceptMission == msg.ErrorCode)
                    {
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                        PlayerDataManager.Instance.ApplyMissions(-1);
                        Logger.Debug("AcceptMissionCoroutine:ErrorCodes.Error_AcceptMission == msg.ErrorCode");
                    }
                    yield break;
                }

                //更新任务数据
                MissionData.Datas.Remove(msg.Response.MissionId);
                var missionBaseModel = new MissionBaseModel
                {
                    MissionId = msg.Response.MissionId
                };
                for (var i = 0; i != 5; ++i)
                {
                    missionBaseModel.Exdata[i] = msg.Response.Exdata[i];
                }
                MissionData.Datas.Add(msg.Response.MissionId, missionBaseModel);
                EventDispatcher.Instance.DispatchEvent(new Event_UpdateMissionData());

                Logger.Debug("Accept mission[{0}]", msg.Response.MissionId);
                PlatformHelper.UMEvent("Mission", "Accept", id);
            }

            OnAcceptMission(id);
        }

        public void ChangedSceneByMission(int sceneId, int missionId)
        {
            NetManager.Instance.StartCoroutine(ChangedSceneByMissionCoroutine(sceneId, missionId));
        }

        public IEnumerator ChangedSceneByMissionCoroutine(int sceneid, int missionId)
        {
            var table = Table.GetScene(sceneid);
            if (null == table)
            {
                yield break;
            }

            var msg = NetManager.Instance.ChangeSceneRequestByMission(sceneid, missionId);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int) ErrorCodes.OK)
                {
                }
                else
                {
                    UIManager.Instance.ShowNetError(msg.ErrorCode);
                }
            }
        }

        public void CheckAndAutoCommitMission()
        {
            if (null == GameLogic.Instance)
            {
                return;
            }
            GameLogic.Instance.StartCoroutine(CheckAndAutoCommitMissionCoroutine());
        }

        //检查是否有已经完成的任务，如果有就回去交
        private IEnumerator CheckAndAutoCommitMissionCoroutine()
        {
            yield return new WaitForSeconds(0.4f);
            {
                // foreach(var data in MissionData.Datas)
                var __enumerator2 = (MissionData.Datas).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var data = __enumerator2.Current;
                    {
                        if (null == data.Value)
                        {
                            Logger.Debug("null == data.Value");
                            continue;
                        }

                        if (data.Value.Exdata.Count <= 0)
                        {
                            Logger.Debug("data.Value.Exdata.Count<=0");
                            continue;
                        }

                        var state = (eMissionState) data.Value.Exdata[0];
                        if (eMissionState.Finished != state)
                        {
                            continue;
                        }

                        var table = Table.GetMissionBase(data.Value.MissionId);

                        if (null == table)
                        {
                            Logger.Debug("null==table[{0}]", data.Value.MissionId);
                            continue;
                        }

                        var type = (eMissionType) table.FinishCondition;
                        if (
                            eMissionType.AcceptProgressBar == type ||
                            eMissionType.AreaProgressBar == type)
                        {
                            var viewType = table.ViewType;
                            if (curMissionType == viewType)
                            {
                                GoToMissionPlace(data.Value.MissionId);
                            }
                            break;
                        }
                    }
                }
            }
        }

        //交任务
        public void CommitMission(int id)
        {
            NetManager.Instance.StartCoroutine(CommitMissionCoroutine(id));
        }

        private IEnumerator CommitMissionCoroutine(int id)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.CommitMission(id);
                yield return msg.SendAndWaitUntilDone();

                if (msg.State != MessageState.Reply)
                {
                    Logger.Debug("MessageState.Reply CommitMissionCoroutine mission[{0}]", msg.State);
                    yield break;
                }


                var errorCode = (ErrorCodes) msg.ErrorCode;
                if (ErrorCodes.OK != errorCode)
                {
                    Logger.Debug("CommitMissionCoroutine ErrorCode[{0}] mission[{1}]", msg.ErrorCode, id);
                    if (ErrorCodes.Error_NotHaveMission == errorCode)
                    {
                        PlayerDataManager.Instance.ApplyMissions(-1);
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                    }
                    else // if (ErrorCodes.Error_ItemNoInBag_All == errorCode)
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip(errorCode));
                    }
                    yield break;
                }

                //删除已经提交的任务
                if (null != MissionData)
                {
                    MissionData.Datas.Remove(id);
                }

                //原有任务数据
                var data = Instance.MissionData.Datas;

                //交完任务后服务端同步过来变化的任务
                var missions = msg.Response.Missions;

                //收集一下新的可接的任务
                var listAcceptableMission = new List<int>();
            
                // foreach(var pair in missions)
                var __enumerator4 = (missions).GetEnumerator();
                while (__enumerator4.MoveNext())
                {
                    var pair = __enumerator4.Current;
                    {
                        var newMission = pair.Value;
                        MissionBaseModel mission = null;
                        if (!data.TryGetValue(newMission.MissionId, out mission))
                        {
                            mission = new MissionBaseModel
                            {
                                MissionId = newMission.MissionId
                            };
                            data.Add(newMission.MissionId, mission);

                        }

                        //收集一下新的可接的任务
                        if (eMissionState.Acceptable == (eMissionState) newMission.Exdata[0])
                        {
                            listAcceptableMission.Add(mission.MissionId);
                        }

                        for (var i = 0; i != 5; ++i)
                        {
                            mission.Exdata[i] = newMission.Exdata[i];
                        }
                    }
                }
                foreach (var m in missions)
                {
                    if (id == PlayerDataManager.Instance.CurTaskId && (int)(m.Value.MissionId/10000) == (int)(PlayerDataManager.Instance.CurTaskId/10000))
                    {
                        PlayerDataManager.Instance.CurTaskId = m.Value.MissionId;
                        MissionManager.Instance.GoToMissionPlace(m.Value.MissionId);
                        break;
                    }                    
                }

                EventDispatcher.Instance.DispatchEvent(new Event_UpdateMissionData());

                Logger.Debug("Complete mission[{0}]", id);
                PlatformHelper.UMEvent("Mission", "Complete", id);

                OnCommitMission(id, listAcceptableMission);
            }
        }

        //完成任务(类似于读条 进入区域这种 客户端发起的行为走这个函数)
        public void CompleteMission(int id)
        {
            NetManager.Instance.StartCoroutine(CompleteMissionCoroutine(id));
        }

        private IEnumerator CompleteMissionCoroutine(int id)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.CompleteMission(id);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    UIManager.Instance.RemoveBlockLayer();

                    var errorCode = (ErrorCodes) msg.ErrorCode;
                    if (ErrorCodes.OK != errorCode)
                    {
                        if (ErrorCodes.Error_NotHaveMission == errorCode)
                        {
                            PlayerDataManager.Instance.ApplyMissions(-1);
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(msg.ErrorCode);
                        }
                        Logger.Debug("CompleteMission..................." + msg.ErrorCode);

                        //yield break;
                    }

                    //                 MissionBaseModel data = null;
                    //                 if (!MissionData.Datas.TryGetValue(id, out data))
                    //                 {
                    //                     Logger.Fatal("Don't have this mission[" + id.ToString() + "]");
                    //                 }
                    //PlatformHelper.UMEvent("Mission", "Complete", id);
                }
                else
                {
                    PlatformHelper.Event("mission", "id", id);
                    Logger.Debug("CompleteMission..................." + msg.State);
                }
            }
        }

        private IEnumerator DelayCallback(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);

            if (null != callback)
            {
                callback();
            }
        }

        //获得当前任务id
        public int GetCurrentMissionId()
        {
            if (null != CurrentMissionTableData)
            {
                return CurrentMissionTableData.Id;
            }
            return -1;
        }

        public MissionBaseModel GetMission(int missionId)
        {
            MissionBaseModel mis;
            if (MissionData.Datas.TryGetValue(missionId, out mis))
            {
                return mis;
            }
            return null;
        }

        //获得任务数据
        public MissionBaseModel GetMissionById(int missionId)
        {
            MissionBaseModel data = null;
            if (MissionData.Datas.TryGetValue(missionId, out data))
            {
            }
            return data;
        }

        //判断这个npc身上是否有关联任务

        public eMissionState GetMissionState(int id)
        {
            MissionBaseModel data = null;
            if (!MissionData.Datas.TryGetValue(id, out data))
            {
                return eMissionState.Unfinished;
            }
            return (eMissionState) data.Exdata[0];
        }

        //根据任务状态去往任务所需地点
        public void GoToMissionPlace(int id)
        {
            MissionBaseModel data = null;
            if (!MissionData.Datas.TryGetValue(id, out data))
            {
                return;
            }
            GameControl.Executer.Stop();
            var table = Table.GetMissionBase(id);
            if (null == table)
            {
                Logger.Error("GoToMissionPlace({0})  null == table", id);
                return;
            }
            var type = (eMissionMainType) table.ViewType;
            curMissionType = (int)type;

            var state = (eMissionState) data.Exdata[0];
            if (eMissionState.Acceptable == state)
            {
                if (-1 == table.NpcStart)
                {
                    var arg = new MissionListArguments();
                    arg.Tab = 2;
                    arg.MissionId = id;

                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
                    //GameLogic.Instance.StartCoroutine(ShowMissionById(2,id));
                    return;
                }
                ObjManager.Instance.MyPlayer.LeaveAutoCombat((int)(id/10000)!=(int)(PlayerDataManager.Instance.CurTaskId/10000));

                   var e = new MapSceneDrawPath(new Vector3(table.PosX, 0, table.PosY), 1.0f);
                    EventDispatcher.Instance.DispatchEvent(e);
                 
                      if (ObjManager.Instance.MyPlayer.IsAttakNoMove())
                    {
                        float delta = ObjManager.Instance.MyPlayer.RemindNoMoveTime - Time.time + 0.1f;
                        GameControl.Executer.PushCommand(new StayCommand(delta));
                    }
                    var command = GameControl.GoToCommand(table.NpcScene, table.PosX, table.PosY, 2.0f);
                    GameControl.Executer.PushCommand(command);

                    var command1 = new FuncCommand(() => { Instance.EnableNpcFacing(table.NpcStart); });
                    GameControl.Executer.PushCommand(command1);

                    var command2 = new FuncCommand(() => { Instance.OpenMissionById(id, table.NpcStart); });
                    GameControl.Executer.PushCommand(command2);
               //- --------------------------------------------------------
    
            }
            else if (eMissionState.Finished == state)
            {
                //如果是日常任务就直接弹界面

                if (eMissionMainType.Daily == type ||
                    eMissionMainType.Circle == type||
                    eMissionMainType.Farm == type)
                {
                    var arg = new MissionListArguments();
                    arg.Tab = 3;
                    arg.MissionId = id;
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
                    //GameLogic.Instance.StartCoroutine(ShowMissionById(3,id));
                    return;
                }
                if (eMissionMainType.SubStoryLine == type && -1 == table.FinishNpcId)
                {
                    var arg = new MissionListArguments();
                    arg.Tab = 2;
                    arg.MissionId = id;
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
                    return;
                }
                ObjManager.Instance.MyPlayer.LeaveAutoCombat((int)(id / 10000) != (int)(PlayerDataManager.Instance.CurTaskId / 10000));

                var e = new MapSceneDrawPath(new Vector3(table.FinishPosX, 0, table.FinishPosY), 1.0f);
                EventDispatcher.Instance.DispatchEvent(e);

                var command = GameControl.GoToCommand(table.FinishSceneId, table.FinishPosX, table.FinishPosY, 2.0f);
                GameControl.Executer.PushCommand(command);

                var command1 = new FuncCommand(() => { Instance.EnableNpcFacing(table.FinishNpcId); });
                GameControl.Executer.PushCommand(command1);

                var command2 = new FuncCommand(() => { Instance.OpenMissionById(id, table.FinishNpcId); });
                GameControl.Executer.PushCommand(command2);
            }
            else if (eMissionState.Unfinished == state)
            {
                if (eMissionMainType.Circle == type || eMissionMainType.Farm == type)
                {
//记录一下当前正在做的环任务
                    CurrentDoingCircleMission = id;
                }
                else
                {
                    CurrentDoingCircleMission = -1;
                }

                EventDispatcher.Instance.DispatchEvent(new UIEvent_DoMissionGoTo());

                if (0 == table.TrackType)
                {
                    //到地图某一地方
                    ObjManager.Instance.MyPlayer.LeaveAutoCombat((int)(id / 10000) != (int)(PlayerDataManager.Instance.CurTaskId / 10000));
                    var e = new MapSceneDrawPath(new Vector3(table.TrackParam[1], 0, table.TrackParam[2]), 1.0f);
                    EventDispatcher.Instance.DispatchEvent(e);
                    if (id > 40000 && id < 60000)
                    {
                        PlayerDataManager.Instance.CurTaskId = id;                    
                    }
                    if (ObjManager.Instance.MyPlayer.IsAttakNoMove())
                    {
                        float delta = ObjManager.Instance.MyPlayer.RemindNoMoveTime - Time.time + 0.1f;
                        GameControl.Executer.PushCommand(new StayCommand(delta));
                    }
                    
                    var fubenId = table.FinishParam[0];                        
                    var eraInfo = EraManager.Instance.IsEraBookFuben(fubenId);
                    
                    var command = GameControl.GoToCommand(table.TrackParam[0], table.TrackParam[1], table.TrackParam[2]);
                    GameControl.Executer.PushCommand(command);                                       
                }
                else if (1 == table.TrackType)//直接开某个界面
                {
                    if (13 == table.TrackParam[0])
                    {                                       
                        var arg = new MissionListArguments();                    
                        arg.Tab = table.TrackParam[1];
                        //arg.IsFromMisson = true;
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionList, arg));
                    }
                    else if (104 == table.TrackParam[0])
                    {
                        var fubenId = table.FinishParam[0];     
                        var eraInfo = EraManager.Instance.IsEraBookFuben(fubenId);
                        if (eraInfo == null) return;
                        if (eraInfo.Record.ActiveParam.Count > 0)
                        {
                            if (eraInfo.Record.ActiveParam[0] == id)
                            {
                                //跳转到玛雅书
                                EventDispatcher.Instance.DispatchEvent(new GoToEraBookShiShiEvent(eraInfo, fubenId, id));
                            }
                        }
                        //EraManager.Instance.GotoCurrentPage();
                    }
                    else
                    {
                        GameUtils.GotoUiTab(table.TrackParam[0], table.TrackParam[1], table.TrackParam[2]);    
                    }
				

                    return;
                }

                switch ((eMissionType) table.FinishCondition)
                {
                    case eMissionType.KillMonster:
                    case eMissionType.CheckItem:
                    case eMissionType.ObtainItem:
                    {
                        //计算下和该任务完成地点相同的其他任务
                        var missionList = new List<int>();
                        missionList.Add(id);
                        {
                            // foreach(var tempData in MissionData.Datas)
                            var __enumerator3 = (MissionData.Datas).GetEnumerator();
                            while (__enumerator3.MoveNext())
                            {
                                var tempData = __enumerator3.Current;
                                {
                                    var missionData = tempData.Value;

                                    if (missionData.MissionId == id)
                                    {
                                        continue;
                                    }

                                    //正在做的任务
                                    if (eMissionState.Unfinished != (eMissionState) missionData.Exdata[0])
                                    {
                                        continue;
                                    }

                                    //只检查杀怪和获得物品的任务
                                    var tableTemp = Table.GetMissionBase(missionData.MissionId);
                                    var typeTemp = (eMissionType) tableTemp.FinishCondition;
                                    if (eMissionType.KillMonster != typeTemp &&
                                        eMissionType.CheckItem != typeTemp&&
                                        eMissionType.ObtainItem != typeTemp)
                                    {
                                        continue;
                                    }

                                    //检查任务完成地点
                                    if (tableTemp.TrackParam[0] != table.TrackParam[0] ||
                                        tableTemp.TrackParam[1] != table.TrackParam[1] ||
                                        tableTemp.TrackParam[2] != table.TrackParam[2])
                                    {
                                        continue;
                                    }

                                    missionList.Add(missionData.MissionId);
                                }
                            }
                        }


                        var command1 = new FuncCommand(() =>
                        {
                            GameControl.Instance.TargetObj = null;
                            ObjManager.Instance.MyPlayer.EnterAutoCombat(missionList);
                        });
                        GameControl.Executer.PushCommand(command1);
                    }
                        break;
                    case eMissionType.AcceptProgressBar:
                    case eMissionType.AreaProgressBar:
                    {
                    
                        var command1 = new FuncCommand(() =>
                        {
                            if (OBJ.CHARACTER_STATE.IDLE != ObjManager.Instance.MyPlayer.GetCurrentStateName())
                            {
                                return;
                            }
						
                            ObjManager.Instance.MyPlayer.OnDisMount();

                            var dicId = table.FinishParam[1];
                            var e = new ShowMissionProgressEvent(GameUtils.GetDictionaryText(dicId));
                            EventDispatcher.Instance.DispatchEvent(e);
                            Logger.Info("FuncCommand CallBackFun.......................1");

                            Action endCallback = () =>
                            {
                                ObjManager.Instance.MyPlayer.PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                                CompleteMission(id);
                                if (-1 != table.FinishParam[2])
                                {
                                    GameLogic.Instance.SceneEffect.StopEffect(table.FinishParam[2]);
                                }
                                var e1 = new HideMissionProgressEvent(id);
                                EventDispatcher.Instance.DispatchEvent(e1);
                            };

                            Action interruptCallback = () =>
                            {
                                if (-1 != table.FinishParam[2])
                                {
                                    GameLogic.Instance.SceneEffect.StopEffect(table.FinishParam[2]);
                                }
                                var e1 = new HideMissionProgressEvent(id);
                                EventDispatcher.Instance.DispatchEvent(e1);
                            };
                            GameLogic.Instance.StopCoroutine("LaunchAction");
                            var animationId = -1;
                            if (!int.TryParse(table.PlayCollectionAct, out animationId))
                            {
                                Logger.Error("int.TryParse(table.PlayCollectionAct) failed [{0}]", table.Id);
                            }

                            if (-1 == animationId)
                            {
                                animationId = 11;
                            }

                            GameLogic.Instance.StartCoroutine(GameUtils.LaunchAction(animationId, table.FinishParam[0],
                                endCallback, interruptCallback));
                            if (-1 != table.FinishParam[2])
                            {
                                GameLogic.Instance.SceneEffect.PlayEffect(table.FinishParam[2]);
                            }
                        });
                        GameControl.Executer.PushCommand(command1);
                    }
                        break;
                    case eMissionType.EquipItem:
                    case eMissionType.EnhanceEquip:
                    {
                    }
                        break;
                    case eMissionType.AreaStroy:
                    {
//播放剧情
                        if (-1 != CurrentMissionTableData.FinishParam[0])
                        {
                            var storyId = CurrentMissionTableData.FinishParam[0];

                            Action endCallback = () => { CompleteMission(id); };

                            PlayCG.PlayById(storyId, endCallback);
                        }
                    }
                        break;
                    case eMissionType.BuyItem:
                    case eMissionType.DepotTakeOut:
                    {
                        var command1 = new FuncCommand(() => { OpenMissionByNpcId(table.FinishParam[2]); });
                        GameControl.Executer.PushCommand(command1);
                    }
                        break;
                    case eMissionType.Dungeon:
                    {
                        var sceneId = table.FinishParam[0];

                        var currentSceneId = GameLogic.Instance.Scene.SceneTypeId;
                        if (sceneId != currentSceneId)
                        {
                            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                                GameUtils.GetDictionaryText(220507),
                                GameUtils.GetDictionaryText(1503),
                                () => { ChangedSceneByMission(sceneId, id); });
                        }
                        else
                        {
                            var player = ObjManager.Instance.MyPlayer;
                            if (null != player)
                            {
                                player.EnterAutoCombat();
                            }
                        }
                    }
                        break;
                    case eMissionType.Tollgate:

                        var fubenId = table.FinishParam[0];                        
                                             

                        if (table.FinishParam[2] != -1)
                        {
                            var tabFuben = Table.GetFuben(fubenId);
                            var currentSceneId1 = GameLogic.Instance.Scene.SceneTypeId;

                            if (tabFuben != null)
                            {
                                if (tabFuben.SceneId != currentSceneId1)
                                {
                                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                                        GameUtils.GetDictionaryText(220507),
                                        GameUtils.GetDictionaryText(1503),
                                        () => { ChangedSceneByMission(tabFuben.SceneId, id); });  
                                }
                            }
                        }
                        else // (1060 【需求】从主线任务点击可以进入副本)之前前端没有添加这个类型的处理，若第三个param为-1，执行原有的default代码逻辑，按策划需求:第三个param为1时，跳转副本的逻辑处理
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210118));
                        break;
                    default:
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210118));
                    }
                        break;
                }
            }
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionList));
        }

        public int HasMission(int npcId)
        {
            if (-1 == npcId)
            {
                return -1;
            }

            var list = new List<Sortable>();
            {
                // foreach(var pair in MissionData.Datas)
                var __enumerator1 = (MissionData.Datas).GetEnumerator();
                while (__enumerator1.MoveNext())
                {
                    var pair = __enumerator1.Current;
                    {
                        var missionId = pair.Key;
                        var tableData = Table.GetMissionBase(missionId);
                        if (null == tableData)
                        {
                            continue;
                        }

                        //任务状态
                        var state = (eMissionState) pair.Value.Exdata[0];
                        var type = (eMissionMainType) tableData.ViewType;

                        var sort = new Sortable();
                        sort.id = missionId;
                        sort.priority = 0;
                        if (tableData.FinishNpcId == npcId && eMissionState.Finished == state)
                        {
                            if (eMissionMainType.MainStoryLine == type)
                            {
                                sort.priority = 1;
                            }
                            else
                            {
                                sort.priority = 2;
                            }
                        }
                        else if (tableData.NpcStart == npcId && eMissionState.Acceptable == state)
                        {
                            if (eMissionMainType.MainStoryLine == type)
                            {
                                sort.priority = 3;
                            }
                            else
                            {
                                sort.priority = 4;
                            }
                        }

                        if (sort.priority <= 0)
                        {
                            continue;
                        }

                        list.Add(sort);
                    }
                }
            }

            if (list.Count > 0)
            {
                list.Sort((a, b) => { return (a.priority < b.priority) ? -1 : 1; });
                return list[0].id;
            }
            return -1;
        }

        //计算任务显示内容
        public static string MissionContent(MissionBaseRecord missionData, ReadonlyList<int> data)
        {
            var str = "(";
            if (data[0] == (int) eMissionState.Finished)
            {
                str += GameUtils.GetDictionaryText(1031);
            }
            else if (data[0] == (int) eMissionState.Acceptable)
            {
                str += GameUtils.GetDictionaryText(1034);
            }
            else if (data[0] == (int) eMissionState.Failed)
            {
                str += GameUtils.GetDictionaryText(1033);
            }
            else
            {
                var missionType = (eMissionType) missionData.FinishCondition;
                switch (missionType)
                {
                    case eMissionType.Finish:
                    {
                        str += GameUtils.GetDictionaryText(1031);
                    }
                        break;
                    case eMissionType.AcceptProgressBar:
                    case eMissionType.AreaProgressBar:
                    case eMissionType.AcceptStroy:
                    {
                        str += string.Format("{0}/{1}", 0, 1);
                    }
                        break;
                    //case eMissionType.Tollgate:
                    //{
                    //    if (missionData.FinishParam[2] == 0)
                    //    {
                    //        return string.Empty;
                    //    }
                    //}
                    //    break;
                    default:
                    {
                        str += string.Format("{0}/{1}", data[2], data[4]);
                    }
                        break;
                }
            }


            str += ")";

            if (data[0] == (int) eMissionState.Finished)
            {
                str = "[5DFF00]" + str + "[-]";
            }

            return str;
        }

        public int NextMissionAction(int missionId, int npcId, float delay = 0.1f)
        {
            var nextMissionId = -1;
            if (-1 != npcId)
            {
                nextMissionId = HasMission(npcId);
            }

            if (-1 != nextMissionId)
            {
                OpenMissionById(nextMissionId, npcId);
                //yield break;
                return nextMissionId;
            }

            GoToMissionPlace(missionId);
            return -1;
            //GameLogic.Instance.StartCoroutine(NextMissionActionCoroutine(missionId, npcId, delay));
        }

        private IEnumerator NextMissionActionCoroutine(int missionId, int npcId, float delay)
        {
            yield return new WaitForSeconds(delay);
            var nextMissionId = -1;

            if (-1 != npcId)
            {
                nextMissionId = HasMission(npcId);
            }

            if (-1 != nextMissionId)
            {
                OpenMissionById(nextMissionId, npcId);
                yield break;
            }

            GoToMissionPlace(missionId);
        }

        //当接受任务时
        private void OnAcceptMission(int id)
        {
            EventDispatcher.Instance.DispatchEvent(new Event_RefreshFuctionOnState());

            var tableData = Table.GetMissionBase(id);
            if (null == tableData)
            {
                return;
            }

            if (eMissionType.Dungeon == (eMissionType) tableData.FinishCondition)
            {
                ChangedSceneByMission(tableData.FinishParam[0], id);
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                return;
            }


            var npcId = NpcId;

            //场景特效
            GameLogic.Instance.SceneEffect.OnAcceptMission(id);


            //播放接受任务特效
            //EffectManager.Instance.CreateEffect(10, ObjManager.Instance.MyPlayer.gameObject);

            //尝试触发新功能系统
            if (GameLogic.Instance.GuideTrigger.OnMissionAccept(id))
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                return;
            }

            //是否需要显示对话框


            //播放剧情
            var callBack = new Action(() =>
            {
                if (eMissionType.AcceptStroy == (eMissionType) tableData.FinishCondition)
                { // 播放剧情
                    var roleId = PlayerDataManager.Instance.GetRoleId();
                    var idx = -1;
                    if (roleId >= 0 && roleId < tableData.FinishParam.Count)
                    {
                        idx = tableData.FinishParam[roleId];
                    }
                    if (-1 == idx)
                    {
                        idx = tableData.FinishParam[0];
                    }
                    if (-1 != idx)
                    {
                        //关闭UI
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                        var call = new Action(() => { NextMissionAction(id, NpcId); });
                        var willDestroy = true;
                        //120任务硬编码
                        if (120 == id)
                        {
                            call = () => { ChangedSceneByMission(5, 120); };
                            willDestroy = false;
                        }

                        PlayCG.PlayById(idx, call, willDestroy);
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                    }
                }
                else
                {
                    {
//如果是支线任务，并且不是从NPC那里接受的，就不继续做什么
                        var type = (eMissionMainType) tableData.ViewType;
                        if (eMissionMainType.SubStoryLine == type)
                        {
                            if (-1 == tableData.NpcStart)
                            {
                                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                                return;
                            }
                        }
                    }


                    if (-1 == NextMissionAction(id, npcId, 0.5f))
                    {
                        //关闭UI
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                    }
                }
            });

            //显示任务剧情对话
            if (-1 != tableData.TalkId)
            {
                //关闭UI
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                StartDialog(tableData.TalkId, callBack);
                return;
            }

            //判断剧情播放
            callBack();
        }

        //当任务完成时
        private void OnCommitMission(int id, List<int> newMissionList = null)
        {
            EventDispatcher.Instance.DispatchEvent(new Event_RefreshFuctionOnState());

            curMissionType = -1;
            var npcId = NpcId;

            //任务场景特效
            GameLogic.Instance.SceneEffect.OnCommitMission(id);

            //关闭任务窗口
            //EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
            var roleId = PlayerDataManager.Instance.GetRoleId();
            var table = Table.GetMissionBase(id);
            if (table != null && roleId < table.GetSkill.Count)
            {
                var skillId = table.GetSkill[roleId];
                if (skillId >= 0)
                {
                    PlayerDataManager.Instance.LearnSkill(skillId);
                }
            }
            //尝试触发新功能系统
            if (null != newMissionList)
            {
                {
                    var __list5 = newMissionList;
                    var __listCount5 = __list5.Count;
                    for (var __i5 = 0; __i5 < __listCount5; ++__i5)
                    {
                        var missionId = __list5[__i5];
                        {
                            if (GameLogic.Instance.GuideTrigger.OnMissionBecomeAcceptable(missionId))
                            {
                                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                                return;
                            }
                        }
                    }
                }
            }


            //比方特效
            //EffectManager.Instance.CreateEffect(11, ObjManager.Instance.MyPlayer.gameObject);

            {
//潜规则
                //if (id == 6 && 0 == PlayerDataManager.Instance.GetRoleId())
                //{
                //    if (0 == PlayerDataManager.Instance.GetCurrentEquipSkillCount())
                //    {
                //        //关闭任务窗口
                //        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                //        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

                //        var e = new SkillEquipMainUiAnime(4, 0);
                //        EventDispatcher.Instance.DispatchEvent(e);
                //        return;
                //    }
                //}
                //else if (id == 6 && 1 == PlayerDataManager.Instance.GetRoleId())
                //{
                //    if (0 == PlayerDataManager.Instance.GetCurrentEquipSkillCount())
                //    {
                //        //关闭任务窗口
                //        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                //        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

                //        var e = new SkillEquipMainUiAnime(104, 0);
                //        EventDispatcher.Instance.DispatchEvent(e);
                //        return;
                //    }
                //}
                //else if (id == 6 && 2 == PlayerDataManager.Instance.GetRoleId())
                //{
                //    if (0 == PlayerDataManager.Instance.GetCurrentEquipSkillCount())
                //    {
                //        //关闭任务窗口
                //        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                //        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

                //        var e = new SkillEquipMainUiAnime(204, 0);
                //        EventDispatcher.Instance.DispatchEvent(e);
                //        return;
                //    }
                //}
                //else if (id == 107)
                //{
                //    if (2 == PlayerDataManager.Instance.GetCurrentEquipSkillCount())
                //    {
                //        if (0 == PlayerDataManager.Instance.GetRoleId())
                //        {
                //            //关闭任务窗口
                //            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                //            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

                //            var e = new SkillEquipMainUiAnime(6, 2);
                //            EventDispatcher.Instance.DispatchEvent(e);
                //            return;
                //        }
                //        else if (1 == PlayerDataManager.Instance.GetRoleId())
                //        {
                //            //关闭任务窗口
                //            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                //            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

                //            var e = new SkillEquipMainUiAnime(106, 2);
                //            EventDispatcher.Instance.DispatchEvent(e);
                //            return;
                //        }
                //        else if (2 == PlayerDataManager.Instance.GetRoleId())
                //        {
                //            //关闭任务窗口
                //            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                //            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

                //            var e = new SkillEquipMainUiAnime(207, 2);
                //            EventDispatcher.Instance.DispatchEvent(e);
                //            return;
                //        }	
                //    }
				
                //}
                if (id == 412)
                {
                    GameUtils.GotoUiTab(92, -1, 15000);

                    //关闭任务窗口
                    //EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                    //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.CharacterUI));
                    //return;
                }
            }

            //每日任务就不接了着了
            if (null == table)
            {
                //关闭任务窗口
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                return;
            }

            if (eMissionMainType.Daily == (eMissionMainType) table.ViewType ||
                eMissionMainType.Circle == (eMissionMainType)table.ViewType || 
                eMissionMainType.Farm == (eMissionMainType)table.ViewType)
            {
                //关闭任务窗口
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                return;
            }

            //有限播放剧情
            if (roleId >= 0 && roleId < table.StoryId.Length)
            {
                var storyId = table.StoryId[roleId];
                if (storyId != -1)
                {
                    //关闭任务窗口
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                    PlayCG.PlayById(storyId, () => { NextMissionAction(table.NextMission, npcId); });                    
                    return;
                }
            }

            //弹出完成提示
            if (-1 != table.UIGuideId)
            {
                ShowMissionUI(id);
                //关闭任务窗口
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
                return;
            }

            //没有剧情就打开下一个任务
            if (-1 == NextMissionAction(table.NextMission, npcId, 0.5f))
            {
                //关闭任务窗口
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
            }
        }

        // 如果需要展示装备，显示装备显示displaymodel，否则，显示missiontip界面
        private void ShowMissionUI(int missionId)
        {
            var table = Table.GetMissionBase(missionId);
            if (null == table || -1 == table.UIGuideId)
            {
                return;
            }

            var roleId = PlayerDataManager.Instance.GetRoleId();

            //职业奖励
            var modelShow = false;
            var rewardId0 = table.RoleRewardId[roleId, 0];
            if (rewardId0 > 0)
            {
                var tbEquip = Table.GetEquipBase(rewardId0);
                if (tbEquip != null)
                {
                    var tbMont = Table.GetWeaponMount(tbEquip.EquipModel);
                    if (tbMont != null && tbMont.Path != "")
                    {
                        GameUtils.GotoUiTab(table.UIGuideId, table.UIGuideTab, rewardId0);
                        return;
                    }
                }

            }

            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionTip, new MissionTipArguments(missionId)));
        }

        public void OnEnterNormalScene()
        {
            foreach (var pair in MissionData.Datas)
            {
                var id = pair.Value.MissionId;
                var table = Table.GetMissionBase(id);
                if (null == table)
                {
                    continue;
                }
                if (1 != pair.Value.Exdata[0])
                {
                    continue;
                }
                if (eMissionMainType.MainStoryLine != (eMissionMainType) table.ViewType)
                {
                    continue;
                }

                if (eMissionType.Dungeon == (eMissionType) table.FinishCondition
                    || eMissionType.Tollgate == (eMissionType)table.FinishCondition)
                {
                    GoToMissionPlace(id);
                    return;
                }
            }
        }

        public void OnMissionComplete(int id)
        {
            GameLogic.Instance.SceneEffect.OnMissionComplete(id);
        }

        public void EnableNpcFacing(int npcId)
        {
            ObjNPC objNpc = null;
            if (null != ObjManager.Instance)
            {
                foreach (var obj in ObjManager.Instance.ObjPool)
                {
                    if (obj.Value.GetObjType() == OBJ.TYPE.NPC)
                    {
                        if (obj.Value.GetDataId() == npcId)
                        {
                            objNpc = obj.Value as ObjNPC;
                            break;
                        }
                    }
                }
            }
            if (null != objNpc)
            {
                objNpc.TowardPlayer = true;
            }
        }

        //打开当前任务
        public bool OpenMissionById(int Id, int npcId = -1, ulong objId = TypeDefine.INVALID_ULONG)
        {
            var tableData = Table.GetMissionBase(Id);
            if (null == tableData)
            {
                return false;
            }

            MissionBaseModel data = null;
            if (!MissionData.Datas.TryGetValue(Id, out data))
            {
                Logger.Fatal("Don't have this mission[" + Id + "]");
                return false;
            }

            NpcId = npcId;
            NpcObjId = objId;
            CurrentMissionData = data;
            CurrentMissionTableData = Table.GetMissionBase(Id);
            //打开任务的时候关闭引导
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.NewbieGuide));
            EventDispatcher.Instance.DispatchEvent(new UIEvent_UpdateCurrentMission(-1));

            //if (!UIConfig.MissionFrame.Visible())
            {
                UIManager.Instance.ShowUI(UIConfig.MissionFrame);
            }

            return true;
        }

        //打开NPC
        public bool OpenMissionByNpcId(int npcId, ulong objId = TypeDefine.INVALID_ULONG)
        {
            var missionId = HasMission(npcId);
            if (-1 != missionId)
            {
                return OpenMissionById(missionId, npcId, objId);
            }
            return OpenMissionNpcDialog(npcId, objId);
            return false;
        }

        //打开NPC对话
        public bool OpenMissionNpcDialog(int npcId, ulong objId = TypeDefine.INVALID_ULONG)
        {
            NpcId = npcId;
            NpcObjId = objId;
            CurrentMissionData = null;
            CurrentMissionTableData = null;

            var tableCharacter = Table.GetCharacterBase(npcId);
            if (null != tableCharacter)
            {
                var tableNpc = Table.GetNpcBase(tableCharacter.ExdataId);
                if (null != tableNpc)
                {
                    for (var i = 0; i < tableNpc.pop.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(tableNpc.pop[i]))
                        {
                            //打开任务的时候关闭引导
                            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.NewbieGuide));
                            EventDispatcher.Instance.DispatchEvent(new UIEvent_UpdateCurrentMission(-1));
                            UIManager.Instance.ShowUI(UIConfig.MissionFrame);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        //对当前任务的操作
        public void OperateCurrentMission()
        {        
            if (CurrentMissionData != null)
            {
                var state = (eMissionState)CurrentMissionData.Exdata[0];
                if (eMissionState.Acceptable == state)
                {
                    AcceptMission(CurrentMissionData.MissionId);
                }
                else if (eMissionState.Finished == state)
                {
                    CommitMission(CurrentMissionData.MissionId);
                }
            }       
        }

        private IEnumerator ShowMissionById(int page, int id)
        {
            yield return new WaitForSeconds(0.3f);
            EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(page));
            EventDispatcher.Instance.DispatchEvent(new Event_ShowMissionDataDetail(id));
        }

        //开始一段对话
        public void StartDialog(int id, Action callback = null)
        {
            var dialogue = new List<DialogueData>();

            //分析对话内容
            var tableTalk = Table.GetTalk(id);
            while (null != tableTalk)
            {
                var content = GameUtils.GetDictionaryText(tableTalk.Content);
                var data = new DialogueData
                {
                    DialogContent = content,
                    NpcDataId = tableTalk.Model,
                    IsBlurBackground = tableTalk.IsMask
                };
                if (-1 == data.NpcDataId)
                {
                    data.Name = PlayerDataManager.Instance.GetName();
                }
                else
                {
                    var tableCharacter = Table.GetCharacterBase(data.NpcDataId);
                    if (null != tableCharacter)
                    {
                        var tableNpc = Table.GetNpcBase(tableCharacter.ExdataId);
                        if (null != tableNpc)
                        {
                            data.Name = tableNpc.Name;
                        }
                        if (string.IsNullOrEmpty(data.Name))
                        {
                            data.Name = tableCharacter.Name;
                        }
                    }
                }

                dialogue.Add(data);
                tableTalk = Table.GetTalk(tableTalk.NextTalk);
            }

            //打开UI
            EventDispatcher.Instance.DispatchEvent(new Event_ShowDialogue(dialogue, callback));
            UIManager.Instance.ShowUI(UIConfig.DialogFrame);
        }

        private class Sortable
        {
            public int id;
            public int priority;
        }
    }
}