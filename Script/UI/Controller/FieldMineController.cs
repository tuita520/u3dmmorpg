using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

namespace ScriptController
{
    public class FieldMineController : IControllerBase 
    {
        private FieldMineDataModel DataModel;
        private List<LodeItemDataModel> LodeList = new List<LodeItemDataModel>();
        private ulong modelId { get; set; }
        private bool Inited = false;
        private List<int>IdList = new List<int>();
        System.DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
        private Dictionary<int,ulong> npc2Obj = new Dictionary<int, ulong>();
        private int UIChooseScene = 0;
        public FieldMineController()
        {
            CleanUp();
            if (!Inited)
            {
                Table.ForeachWarFlag((tb) =>
                {
                    var model = new FieldMineItemDataModel();
                    model.Id = tb.Id;
                    model.SceneId = tb.FlagInMap;
                    DataModel.WarFlagList.Add(model);
                    return true;
                });
                Inited = true;
            }
            EventDispatcher.Instance.AddEventListener(FieldFlagMenuItemClickEvent.EVENT_TYPE, OnClickPageBtn);
            EventDispatcher.Instance.AddEventListener(LodeItemClickEvent.EVENT_TYPE, OnClickLodeItemEvent);
            EventDispatcher.Instance.AddEventListener(FieldItemOperationEvent.EVENT_TYPE, OnFlagOperationEvent);
            EventDispatcher.Instance.AddEventListener(UpdateWarFlagModelEvent.EVENT_TYPE, OnFlagModelUpdateEvent);
            EventDispatcher.Instance.AddEventListener(OnSceneLodeUpdateEvent.EVENT_TYPE, OnSceneLodeUpdate);
            EventDispatcher.Instance.AddEventListener(FieldPreviewEvent.EVENT_TYPE, OnFieldPreviewEvent);
        }
        private void OnClickPageBtn(IEvent ievent)
        {
            var e = ievent as FieldFlagMenuItemClickEvent;
            if (e.Idx < 0 || e.Idx >= DataModel.WarFlagList.Count)
            {
                return;
            }
            if (DataModel.FlagSelect == e.Idx)
            {
                return;
            }
            else
            {
                ChooseWarFlag(e.Idx);
            }
        }
        private void ChooseWarFlag(int index)
        {
            for (int i = 0; i < DataModel.WarFlagList.Count; i++)
            {
                var item = DataModel.WarFlagList[i];
                if (i == index)
                {
                    DataModel.WarFlagList[i].IsSelect = true;
                }
                else
                {
                    if (item.IsSelect)
                    {
                        item.IsSelect = false;
                    }
                }
            }
            NetManager.Instance.StartCoroutine(OnApplyLodeInfoCoroutine(index));
        }
        private void OnClickLodeItemEvent(IEvent ievent)
        {
            var e = ievent as LodeItemClickEvent;
            var tbLode = Table.GetLode(e.Idx);
            if (null == tbLode)
            {
                return;
            }
            DataModel.Info.LodeInfo.Id = e.Idx;
            for (int i = 0; i < tbLode.OutputShow.Length; i++)
            {
                DataModel.Info.LodeInfo.RewardId[i] = tbLode.OutputShow[i];
            }
            for (int i = 0; i < DataModel.Info.LodeList.Count; i++)
            {
                if (DataModel.Info.LodeList[i].Id == DataModel.Info.LodeInfo.Id)
                {
                    DataModel.Info.LodeList[i].IsSelect = true;
                }
                else
                {
                    DataModel.Info.LodeList[i].IsSelect = false;
                }
            }
            //GotoLodeInScene(DataModel.Info.LodeInfo.Id);
        }
        private void GotoLodeInScene(int lodeid)
        {
            var tbLode = Table.GetLode(lodeid);
            if (null == tbLode)
            {
                return;
            }
            var tbSceneNpc = Table.GetSceneNpc(tbLode.SceneNpcId);
            if (null == tbSceneNpc)
            {
                return;
            }
            var SceneId = tbSceneNpc.SceneID;
            if (!CheckIsCanChangeMap(SceneId))
            {
                return;
            }
            var TargetPos = new Vector3((float)tbSceneNpc.PosX,0f,(float)tbSceneNpc.PosZ);
            GameControl.Executer.Stop();
            ObjManager.Instance.MyPlayer.LeaveAutoCombat();
            var VipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
            if (VipLevel >= 4)
            {
                FlyTo(TargetPos.x, TargetPos.z, SceneId);
            }
            else
            {
                var command = GameControl.GoToCommand(SceneId, TargetPos.x, TargetPos.z);
                GameControl.Executer.PushCommand(command);
            }
            //开始传送后，关闭界面
            if (State == FrameState.Open)
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.FieldMineUI));                
            }
            var ActivityCtr = UIManager.Instance.GetController(UIConfig.ActivityUI);
            if (null != ActivityCtr)
            {
                if (ActivityCtr.State == FrameState.Open)
                {
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ActivityUI));
                }                
            }
            var SceneMapCtr = UIManager.Instance.GetController(UIConfig.SceneMapUI);
            if (null != SceneMapCtr)
            {
                if (SceneMapCtr.State == FrameState.Open)
                {
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SceneMapUI));
                }
            }
        }
        private bool CheckIsCanChangeMap(int sceneid)
        {
            var tbScene = Table.GetScene(sceneid);
            if (null == tbScene)
            {
                return false;
            }
            else if (tbScene.IsPublic != 1)
            {
                GameUtils.ShowHintTip(200005011);
                return false;
            }
            var PlayerLevel = PlayerDataManager.Instance.GetLevel();
            if (PlayerLevel < tbScene.LevelLimit)
            {
                GameUtils.ShowHintTip(210207);
                return false;
            }
            return true;
        }
        private void FlyTo(float x, float y, int sceneId = -1)
        {
            if (sceneId == -1)
            {
                return;
            }
            if (sceneId == GameLogic.Instance.Scene.SceneTypeId)
            {
                var _vec = new Vector3(x, 0, y);
                _vec.y = GameLogic.GetTerrainHeight(x, y);
                var _path = new NavMeshPath();
                NavMesh.CalculatePath(ObjManager.Instance.MyPlayer.Position, _vec, -1, _path);
                if (_path.corners.Length <= 0)
                {
                    //目标地点不能到达
                    var _e = new ShowUIHintBoard(270116);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
            }
            else
            {
                var tbScene = Table.GetScene(sceneId);
                if (null == tbScene)
                {
                    return;
                }
                else if (tbScene.IsPublic != 1)
                {
                    GameUtils.ShowHintTip(200005011);
                    return;
                }
                var PlayerLevel = PlayerDataManager.Instance.GetLevel();
                if (PlayerLevel < tbScene.LevelLimit)
                {
                    GameUtils.ShowHintTip(210207);
                    return;
                }
            }
            GameUtils.FlyTo(sceneId, x, y);
        }
        private void OnFlagModelUpdateEvent(IEvent ievent)
        {
            var e = ievent as UpdateWarFlagModelEvent;
            if (e == null)
                return;
            switch (e.Index)
            {
                case 0: //flag
                    modelId = e.objId;
                    break;
                case 1: //lode
                    if (npc2Obj.ContainsKey(e.npcId))
                    {
                        npc2Obj[e.npcId] = e.objId;
                    }
                    else
                    {
                        npc2Obj.Add(e.npcId,e.objId);
                    }
                    break;
            }
        
        }
        private void OnFieldPreviewEvent(IEvent ievent)
        {
            var e = ievent as FieldPreviewEvent;
            switch (e.Index)
            {
                case 0:
                    DataModel.IsShowPreview = true;
                    break;
                case 1:
                    DataModel.IsShowPreview = false;
                    break;
            }
        }
        private void OnFlagOperationEvent(IEvent ievent)
        {
            var e = ievent as FieldItemOperationEvent;
            if (null == e)
            {
                return;
            }
            GotoLodeInScene(DataModel.Info.LodeInfo.Id);
            //var tbWarFlag = Table.GetWarFlag(DataModel.Info.Id);
            //if (null == tbWarFlag)
            //{
            //    return;
            //}
            //var sceneId = tbWarFlag.FlagInMap;
            //var tbScene = Table.GetScene(sceneId);
            //if (null == tbScene || null == GameLogic.Instance.Scene)
            //{
            //    return;
            //}
            //if (GameLogic.Instance.Scene.SceneTypeId == sceneId)
            //{
            //    var str = string.Format(GameUtils.GetDictionaryText(290051), tbScene.Name);
            //    GameUtils.ShowHintTip(str);
            //    return;
            //}
            //GameUtils.ChangeMap(sceneId);
        }
        #region 固有函数

        public void CleanUp()
        {
            DataModel = new FieldMineDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as FieldMineUIArguments;
            IdList.Clear();
            var sceneid = GameLogic.Instance.Scene.SceneTypeId;
            for (int i = 0; i < DataModel.WarFlagList.Count; i++)
            {
                IdList.Add(DataModel.WarFlagList[i].SceneId);
                var item = DataModel.WarFlagList[i];
                if (sceneid == item.SceneId)
                {
                    ChooseWarFlag(i);
                    break;
                }
            }
            if (!IdList.Contains(sceneid))
            {
                ChooseWarFlag(0);
            }
            DataModel.PlayerTotalTimes = Table.GetExdata(680).InitValue;
            DataModel.PlayerUsedTimes = DataModel.PlayerTotalTimes - PlayerDataManager.Instance.GetExData(eExdataDefine.e680);
        }
        #endregion
        public void OnChangeScene(int sceneId)
        {
            npc2Obj.Clear();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "UpdateLodeAllianceName")
            {
                UpdateLodeAllianceName(param[0].ToString());
            }
            else if (name == "GetLodeTimer")
            {
               return GetLodeTimer((int)param[0]);
            }
            return null;
        }

        public void OnShow()
        {
        }
        private void UpdateLodeAllianceName(string name)
        {
            var obj = ObjManager.Instance.FindObjById(modelId);
            if (obj == null || obj.GetObjType() != OBJ.TYPE.NPC)
                return;
            var npc = obj as ObjNPC;
            if (npc == null || npc.TableNPC.Id < 242000 || npc.TableNPC.Id > 242009)
                return;
            npc.AllianceName = name;
            npc.OnNameBoardRefresh();
        }

        private string GetLodeTimer(int id) //传入npcID
        {
            var lodeId = GameUtils.GetNpcLodeID(id);
            var tb = Table.GetLode(lodeId);
            if (tb == null)
                return "";
            foreach (var v in PlayerDataManager.Instance.LodeInfo.LodeList)
            {
                var tmp = v.Value;
                if (lodeId != tmp.Id)
                    continue;
                if (tmp.Times > 0)
                {
                    var str = string.Format(GameUtils.GetDictionaryText(290014), tmp.Times, tb.CanCollectNum);
                    return (str);
                }
                else
                {
                    var t = (int)(startTime.AddSeconds(tmp.UpdateTime) - DateTime.Now).TotalSeconds;
                    if (t <= 0)
                    {
                        var str = string.Format(GameUtils.GetDictionaryText(290014), tb.CanCollectNum, tb.CanCollectNum);
                        return (str);
                    }
                    else
                        return (string.Format(GameUtils.GetDictionaryText(290015), GameUtils.GetTimeString(t)));
                }
            }
            return "";
        }
        private IEnumerator OnApplyLodeInfoCoroutine(int Index)
        {
            var msg = NetManager.Instance.ClientApplyHoldLode(PlayerDataManager.Instance.ServerId);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    LodeList.Clear();
                    DataModel.FlagSelect = Index;
                    SetCurId(DataModel.WarFlagList[Index].Id);
                    //刷新所有战旗的所属战盟
                    foreach (var item in msg.Response.LodeList)
                    {
                        foreach (var item2 in DataModel.WarFlagList)
                        {
                            var tbWarFlag = Table.GetWarFlag(item2.Id);
                            if (tbWarFlag.FlagInMap == item.SceneId)
                            {
                                if (0 == item.TeamId)
                                {
                                    item2.BattleUnionName = GameUtils.GetDictionaryText(290007);
                                }
                                else
                                {
                                    item2.BattleUnionName = item.TeamName;
                                }
                                UpdateLodeAllianceName(item.TeamName);
                            }
                        }
                    }
                    //刷新矿脉
                    for (int i = 0; i < DataModel.Info.LodeList.Count; i++)
                    {
                        var lodeData = new LodeItemDataModel
                        {
                            Id = -1
                        };
                        DataModel.Info.LodeList[i] = lodeData;
                    }
                    for (int j = 0; j <  DataModel.Info.LodeInfo.RewardId.Count; j++)
                    {
                        DataModel.Info.LodeInfo.RewardId[j] = -1;
                    }
                    var tbWar = Table.GetWarFlag(DataModel.Info.Id);
                    DataModel.Info.NeedLevel = Table.GetScene(tbWar.FlagInMap).LevelLimit;
                    foreach (var item3 in msg.Response.LodeList)
                    {
                        if (item3.SceneId == tbWar.FlagInMap)
                        {
                            if (item3.LodeList.Count <= 0)
                            {
                                yield break;
                            }
                            foreach (var item4 in item3.LodeList)
                            {
                                LodeItemDataModel lodeItem = new LodeItemDataModel();
                                lodeItem.Id = item4.Value.Id;
                                var tbLode = Table.GetLode(lodeItem.Id);
                                var PosX = tbLode.LodeX;
                                var PosY = tbLode.LodeY;
                                lodeItem.LodePos = new Vector3(PosX, PosY, 0);
                                lodeItem.Name = tbLode.Name;
                                lodeItem.Times = item4.Value.Times;
                                if (lodeItem.Times > 0)
                                {
                                    lodeItem.refreshContent = string.Format(GameUtils.GetDictionaryText(290014), lodeItem.Times, tbLode.CanCollectNum);
                                }
                                else
                                {
                                    var time = startTime.AddSeconds(item4.Value.UpdateTime);
                                    lodeItem.UpdateTimes = (int)(time - Game.Instance.ServerTime).TotalSeconds;
                                    if (lodeItem.UpdateTimes < 0)
                                    {
                                        lodeItem.UpdateTimes = 0;
                                        lodeItem.refreshContent = string.Format(GameUtils.GetDictionaryText(290014), tbLode.CanCollectNum, tbLode.CanCollectNum);
                                    }
                                    else
                                    {
                                        lodeItem.refreshContent = string.Format(GameUtils.GetDictionaryText(290015), GameUtils.GetTimeString(lodeItem.UpdateTimes));
                                    }
                                }
                                LodeList.Add(lodeItem);
                            }
                        }
                    }
                    for (int i = 0; i < LodeList.Count; i++)
                    {
                        DataModel.Info.LodeList[i] = LodeList[i];
                    }
                    if (DataModel.Info.LodeList.Count > LodeList.Count)
                    {
                        for (int i = LodeList.Count; i < DataModel.Info.LodeList.Count; i++)
                        {
                            var lodeData2 = new LodeItemDataModel
                            {
                                Id = -1
                            };
                            DataModel.Info.LodeList[i] = lodeData2;
                        }
                    }
                    //矿脉产出默认第一个矿脉
                    var tbLodeDefault = Table.GetLode(DataModel.Info.LodeList[0].Id);
                    DataModel.Info.LodeInfo.Id = DataModel.Info.LodeList[0].Id;
                    DataModel.Info.LodeList[0].IsSelect = true;
                    for (int i = 0; i < tbLodeDefault.OutputShow.Length; i++)
                    {
                        DataModel.Info.LodeInfo.RewardId[i] = tbLodeDefault.OutputShow[i];
                    }
                }
            }
        }

        public void Close()
        {
        }
        float tick = 0;
        public void Tick()
        {
            tick += Time.deltaTime;
            if (tick > 1)
            {
                tick -= 1;
                PlayerDataManager.Instance.LodeInfo = PlayerDataManager.Instance.LodeInfo;
                if (State == FrameState.Open)
                {
                    foreach (var item in LodeList)
                    {
                        if (item.Times <= 0 && item.UpdateTimes > 0)
                        {
                            item.UpdateTimes -= 1;
                            item.refreshContent = string.Format(GameUtils.GetDictionaryText(290015), GameUtils.GetTimeString(item.UpdateTimes));
                        }
                        else
                        {
                            item.UpdateTimes = 0;
                            var tbLode = Table.GetLode(item.Id);
                            item.refreshContent = string.Format(GameUtils.GetDictionaryText(290014), tbLode.CanCollectNum, tbLode.CanCollectNum);
                        }
                    }
                    return;
                }
            }
        
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        private void SetCurId(int id)
        {
            DataModel.Info.Id = id;
            if (id > 0)
            {
                var tbWarFlag = Table.GetWarFlag(DataModel.Info.Id);
                if (null != tbWarFlag)
                {
                    UIChooseScene = tbWarFlag.FlagInMap;
                }   
            }
        }
        public FrameState State { get; set; }
        #region scene相关
        private void OnSceneLodeUpdate(IEvent ievent)
        {
            foreach(var v in PlayerDataManager.Instance.LodeInfo.LodeList)
            {
                var tmp = v.Value;
                int npcId = GameUtils.GetLodeNpcID(tmp.Id);
                if (npc2Obj.ContainsKey(npcId) == false)
                    continue;
                var objId = npc2Obj[npcId];

                var obj = ObjManager.Instance.FindObjById(objId);
                if (obj == null || obj.GetObjType() != OBJ.TYPE.NPC)
                    continue;
                var npc = obj as ObjNPC;
                if (npc == null)
                    continue;

                var tb = Table.GetLode(tmp.Id);
                if (tb == null)
                    continue;
                if (tmp.Times > 0)
                {
                    var str = string.Format(GameUtils.GetDictionaryText(290014), tmp.Times, tb.CanCollectNum);
                    npc.SetLodeName(str);
                }
                else
                {
                    var t = (int)(startTime.AddSeconds(tmp.UpdateTime) - DateTime.Now).TotalSeconds;
                    if (t <= 0)
                    {
                        var str = string.Format(GameUtils.GetDictionaryText(290014), tb.CanCollectNum, tb.CanCollectNum);
                        npc.SetLodeName(str);
                    }
                    else
                        npc.SetLodeName(string.Format(GameUtils.GetDictionaryText(290015), GameUtils.GetTimeString(t)));
                }
                //if (State == FrameState.Open)
                //{
                //    //if (null != GameLogic.Instance.Scene)
                //    //{
                //    //    if (GameLogic.Instance.Scene.SceneTypeId == UIChooseScene)
                //    //    {
                //    //        for (int i = 0; i < DataModel.WarFlagList.Count; i++)
                //    //        {
                //    //            if (DataModel.WarFlagList[i].SceneId == GameLogic.Instance.Scene.SceneTypeId)
                //    //            {
                //    //                ChooseWarFlag(i);
                //    //            }
                //    //        }
                //    //    }
                //    //}
                //}
            }
        }

        #endregion

    }
}
