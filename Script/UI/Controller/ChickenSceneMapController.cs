#region using

using System.Collections;
using System.Collections.Generic;
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
    public class ChickenSceneMapController : IControllerBase
    {
        private ChickenSceneMapDataModel DataModel { get; set; }
        private float MAP_HIGHT = 512;
        private bool mIsDrawPath;
        private SceneRecord mSceneRecord;
        //  private Dictionary<ulong, MapRadarDataModel> dataModelsDict = new Dictionary<ulong, MapRadarDataModel>();
        private MsgCheckenRankList msgR;
        private List<MapRadarDataModel> dataModelsList = new List<MapRadarDataModel>();
        private object trigger;
        private float Radius;
        private int RadiusMin;
        private int TimeSpace = 5;
        private int ReduceNum = 2;

        private float eveReduce;
        public ChickenSceneMapController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(RefresSceneMap.EVENT_TYPE, OnRefresSceneMap);
            EventDispatcher.Instance.AddEventListener(Postion_Change_Event.EVENT_TYPE, OnPostionChange);
            EventDispatcher.Instance.AddEventListener(ChickenMapSceneClickLoction.EVENT_TYPE, OnMapSceneClickLoction);
            EventDispatcher.Instance.AddEventListener(MapSceneDrawPath.EVENT_TYPE, OnMapSceneDrawPath);
            EventDispatcher.Instance.AddEventListener(MapSceneCancelPath.EVENT_TYPE, OnMapSceneCancelPath);
            EventDispatcher.Instance.AddEventListener(RefreshChijRankiListEvent.EVENT_TYPE, OnChickenInfoSyna);
            EventDispatcher.Instance.AddEventListener(ChickenSafeChangeEvent.EVENT_TYPE, ChangeSafeZone);

            var temp = Table.GetClientConfig(1505);
            if (temp != null)
                RadiusMin = int.Parse(temp.Value);
            temp = Table.GetClientConfig(1503);
            if (temp != null)
                TimeSpace = int.Parse(temp.Value);
              temp = Table.GetClientConfig(1502);
            if (temp != null)
                ReduceNum = int.Parse(temp.Value);
            eveReduce = (float)ReduceNum/TimeSpace;
            //
        }

        void ChangeSafeZone(IEvent iev)
        {
            ChickenSafeChangeEvent evt = iev as ChickenSafeChangeEvent;
            Radius = evt.chickenInfo.Radius;
            var p = new Vector2(GameUtils.DividePrecision(evt.chickenInfo.CenterPos.x), GameUtils.DividePrecision(evt.chickenInfo.CenterPos.y));
            DataModel.PositionVec = ConvertSceneToMap(new Vector3(p.x, GameLogic.GetTerrainHeight(p.x, p.y), p.y));
            float width = ConvertScaleToMap(evt.chickenInfo.Radius * 2);
            DataModel.ScaleVec = new Vector3(width, width, width);
            if (trigger == null)
            {
                trigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1f), () =>
                {
                    Radius -= eveReduce;
                    float widthEv = ConvertScaleToMap(Radius * 2);
                    DataModel.ScaleVec = new Vector3(widthEv, widthEv, widthEv);
                    if (Radius <= RadiusMin)
                    {
                        TimeManager.Instance.DeleteTrigger(trigger);
                        trigger = null;
                    }
                }, 1000);
            }
        }
        private void OnChickenInfoSyna(IEvent evt)
        {
            RefreshChijRankiListEvent e = evt as RefreshChijRankiListEvent;
            if (e == null)
                return;
            msgR = e.msg;
            InitModel();
        }

        void InitModel()
        {
            if (msgR == null)
                return;
            if (msgR.bosDie == false)
            {
                CreateMinimapCharacter(new Vector3(msgR.PosX, 0, msgR.PosZ), 2, 10000000, -1);
            }
            else
            {
                RemoveMinimapCharacter(10000000);
            }

            DataModel.CharaModels.Clear();
            for (int i = 0; i < dataModelsList.Count; i++)
            {
                RemoveMinimapCharacter(dataModelsList[i].CharacterId);
            }
            dataModelsList.Clear();
            if (msgR.RankList != null)
            {
                ulong enemyId = 0;
                for (int i = 0; i < msgR.RankList.Count; i++)
                {
                    if (msgR.RankList[i] == null)
                        continue;
                    if (msgR.RankList[i].Guid == ObjManager.Instance.MyPlayer.GetObjId())
                    {
                        enemyId = msgR.RankList[i].EnemyGuid;
                        break;
                    }
                }
                for (int i = 0; i < msgR.RankList.Count; i++)
                {
                    if (msgR.RankList[i] == null)
                        continue;
                    if (msgR.RankList[i].Guid == ObjManager.Instance.MyPlayer.GetObjId())
                        continue;
                    /*** if (i == 0) //魔王
                        {
                            CreateMinimapCharacter(new Vector3(msgR.RankList[i].PosX, 0, msgR.RankList[i].PosZ), 3,msgR.RankList[i].Guid, -1);
                        }
                        else//玩家自己
                        {
                            CreateMinimapCharacter(new Vector3(msgR.RankList[i].PosX, 0, msgR.RankList[i].PosZ), 0,msgR.RankList[i].Guid, -1);
                        }
                    ***/
                    ObjCharacter character = ObjManager.Instance.FindCharacterById(msgR.RankList[i].Guid);
                    if (character != null)
                    {
                        msgR.RankList[i].PosX = character.Position.x;
                        msgR.RankList[i].PosZ = character.Position.z;
                    }
                    if (i == 0) //魔王
                    {
                        CreateMinimapCharacter(new Vector3(msgR.RankList[i].PosX, 0, msgR.RankList[i].PosZ), 3,
                            msgR.RankList[i].Guid, -1);
                    }
                    else
                    {
                        if (msgR.RankList[i].Guid == enemyId)
                            CreateMinimapCharacter(new Vector3(msgR.RankList[i].PosX, 0, msgR.RankList[i].PosZ), 1,
                                msgR.RankList[i].Guid, -1);
                        else
                            CreateMinimapCharacter(new Vector3(msgR.RankList[i].PosX, 0, msgR.RankList[i].PosZ), 0,
                                msgR.RankList[i].Guid, -1);
                    }
                }
            }
        }
        private Vector3 ConvertMapToScene(Vector3 loc)
        {
            var x = loc.x / DataModel.MapWidth * mSceneRecord.TerrainHeightMapWidth + mSceneRecord.TerrainHeightMapWidth / 2.0f;
            var z = loc.y / DataModel.MapHeight * mSceneRecord.TerrainHeightMapLength + mSceneRecord.TerrainHeightMapLength / 2.0f;

            return new Vector3(x, 0, z);
        }

        private Vector3 ConvertSceneToMap(Vector3 loc)
        {
            var x = (loc.x - mSceneRecord.TerrainHeightMapWidth / 2.0f) / mSceneRecord.TerrainHeightMapWidth * DataModel.MapWidth;
            var y = (loc.z - mSceneRecord.TerrainHeightMapLength / 2.0f) / mSceneRecord.TerrainHeightMapLength *
                    DataModel.MapHeight;

            return new Vector3(x, y, 0);
        }
        private float ConvertScaleToMap(float width)
        {
            return mSceneRecord.TerrainHeightMapWidth * width / 256;
        }
        private void DrawPathLoction(List<Vector3> pos)
        {
            var __enumerator1 = (DataModel.PathList).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var model = __enumerator1.Current;
                {
                    model.IsShow = false;
                }
            }

            DataModel.PathList.Clear();


            pos.Insert(0, ObjManager.Instance.MyPlayer.Position);

            var target = GetPointsOnLines(pos, 10.0f);
            {
                var __list2 = target;
                var __listCount2 = __list2.Count;
                for (var __i2 = 0; __i2 < __listCount2; ++__i2)
                {
                    var vector3 = __list2[__i2];
                    {
                        var pathData = new ScenePathDataModel();
                        pathData.Loction = vector3;
                        DataModel.PathList.Add(pathData);
                    }
                }
            }
        }

        private void OnRefresSceneMap(int scendId)
        {
            if (!SceneManager.Instance.isInChickenFuben())
            {
                ClearMonsterList();
                return;
            }

            DataModel.SceneId = scendId;
            mSceneRecord = Table.GetScene(scendId);
            if (mSceneRecord == null)
            {
                return;
            }

            var scale = 1f * mSceneRecord.TerrainHeightMapWidth / mSceneRecord.TerrainHeightMapLength;
            if (scale > 1.0f)
            {
                DataModel.MapWidth = (int)(MAP_HIGHT);
                DataModel.MapHeight = (int)(MAP_HIGHT / scale);
            }
            else
            {
                DataModel.MapWidth = (int)(MAP_HIGHT * scale);
                DataModel.MapHeight = (int)MAP_HIGHT;
            }

            var obj = ObjManager.Instance.MyPlayer;
            if (obj)
            {
                PostionChange(obj.Position);
            }
        }

        private void DrawTargetPathLoction(Vector3 point, float offset = 0.05f)
        {
            if (!ObjManager.Instance.MyPlayer)
            {
                return;
            }

            var tagetPos = ObjManager.Instance.MyPlayer.CalculatePath(point, offset);
            if (tagetPos.Count == 0)
            {
                mIsDrawPath = false;
                return;
            }
            DrawPathLoction(tagetPos);
        }

        private List<Vector3> GetPointsOnLines(List<Vector3> l, float d)
        {
            var result = new List<Vector3>();
            var dist = 0.0f;
            var lCount0 = l.Count - 1;
            for (var i = 0; i < lCount0; i++)
            {
                var s = ConvertSceneToMap(l[i]);
                var e = ConvertSceneToMap(l[i + 1]);
                var dir = (e - s).normalized;
                while ((e - s).magnitude + dist > d)
                {
                    s = s + dir * (d - dist);
                    dist = 0;
                    result.Add(s);
                }

                dist += (e - s).magnitude;
            }
            return result;
        }

        public void Tick()
        {
        }
        private void OnMapSceneCancelPath(IEvent ievent)
        {
            DataModel.PathList.Clear();
            mIsDrawPath = false;
        }

        private void OnMapSceneClickLoction(IEvent ievent)
        {
            var e = ievent as ChickenMapSceneClickLoction;
            var loc = ConvertMapToScene(e.Loction);
            if (ObjManager.Instance.MyPlayer.Dead)
            {
                return;
            }

            if (ObjManager.Instance.MyPlayer.MoveTo(loc))
            {
                ObjManager.Instance.MyPlayer.LeaveAutoCombat();
                mIsDrawPath = true;
                DrawTargetPathLoction(loc);
            }
            else
            {
                DataModel.PathList.Clear();
                mIsDrawPath = false;
            }
        }

        private void OnMapSceneDrawPath(IEvent ievent)
        {
            if (mSceneRecord == null || State != FrameState.Open)
                return;

            var e = ievent as MapSceneDrawPath;

            mIsDrawPath = true;
            DrawTargetPathLoction(e.Postion, e.Offset);
        }
        private void OnPostionChange(IEvent ievent)
        {
            var e = ievent as Postion_Change_Event;
            PostionChange(e.Loction);
        }


        private void OnRefresSceneMap(IEvent ievent)
        {
            var e = ievent as RefresSceneMap;
            OnRefresSceneMap(e.SceneId);
        }

        private void PostionChange(Vector3 objLoction)
        {
            if (mSceneRecord == null)
                return;

            if (ObjManager.Instance.MyPlayer == null
                || ObjManager.Instance.MyPlayer.ObjTransform == null)
            {
                return;
            }
            var v = ObjManager.Instance.MyPlayer.ObjTransform.eulerAngles;
            DataModel.PalyerLocX = (int)objLoction.x;
            DataModel.PalyerLocY = (int)objLoction.z;
            DataModel.SelfMapLoction = ConvertSceneToMap(objLoction);
            DataModel.SelfMapRotation = new Vector3(0, 0, -v.y - 45f);
            if (DataModel.PathList.Count > 0)
            {
                if (ObjManager.Instance.MyPlayer.Dead || (!ObjManager.Instance.MyPlayer.IsMoving()))
                {
                    mIsDrawPath = false;
                    DataModel.PathList.Clear();
                    return;
                }

                var start = false;
                for (var i = DataModel.PathList.Count - 1; i >= 0; i--)
                {
                    var pathDataModel = DataModel.PathList[i];

                    if (start)
                    {
                        pathDataModel.IsShow = false;
                        DataModel.PathList.Remove(pathDataModel);
                    }
                    else
                    {
                        if ((pathDataModel.Loction - DataModel.SelfMapLoction).sqrMagnitude < 20.0f)
                        {
                            pathDataModel.IsShow = false;
                            DataModel.PathList.Remove(pathDataModel);
                            start = true;
                        }
                    }
                }
            }
            if (DataModel.PathList.Count == 0 && mIsDrawPath)
            {
                mIsDrawPath = false;
            }
        }

        private void ClearMonsterList()
        {
            var enumorator = DataModel.CharaModels.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var pos = enumorator.Current;

                var e1 = new ChickenSceneMapRemoveRadar(pos.CharacterId);
                EventDispatcher.Instance.DispatchEvent(e1);
            }

            DataModel.CharaModels.Clear();
            //  dataModelsDict.Clear();
            dataModelsList.Clear();
        }

        public void CleanUp()
        {
            DataModel = new ChickenSceneMapDataModel();
        }


        private void OnRemoveCharacter(IEvent ievent)
        {
            var e = ievent as Character_Remove_Event;
            if (e != null)
            {
                var charId = e.CharacterId;
                RemoveMinimapCharacter(charId);
            }
        }

        MapRadarDataModel FindDataModel(ulong charId)
        {
            for (int i = 0; i < dataModelsList.Count; i++)
            {
                if (dataModelsList[i].CharacterId == charId)
                {
                    return dataModelsList[i];
                    break;
                }
            }
            return null;
        }
        private void RemoveMinimapCharacter(ulong charId)
        {
            var data = FindDataModel(charId);
            var e1 = new ChickenSceneMapRemoveRadar(charId);
            EventDispatcher.Instance.DispatchEvent(e1);
            if (data != null)
            {
                if (DataModel.CharaModels.Contains(data))
                    DataModel.CharaModels.Remove(data);
                if (dataModelsList.Contains(data))
                    dataModelsList.Remove(data);
            }
        }

        private void CreateMinimapCharacter(Vector3 pos, int type, ulong id, int npcId)
        {
            MapRadarDataModel radarDataModel = FindDataModel(id);

            if (radarDataModel == null)
            {
                radarDataModel = new MapRadarDataModel();
                radarDataModel.CharacterId = id;
                radarDataModel.CharType = 1;
                //  radarDataModel.Name = "";
                /**  if (npcId != -1)
                  {
                      var tbNpc = Table.GetNpcBase(npcId);
                      if (tbNpc != null)
                      {
                          radarDataModel.Name = tbNpc.Name;
                      }

                      var mapTrans = SceneManager.Instance.GetMapTransferList(GameLogic.Instance.Scene.SceneTypeId);
                      var enumerator = mapTrans.GetEnumerator();
                      while (enumerator.MoveNext())
                      {
                          if (enumerator.Current != null && enumerator.Current.NpcID == npcId)
                          {
                              radarDataModel.Pos = new Vector3(enumerator.Current.OffsetX, enumerator.Current.OffsetY, 0.0f);
                              break;
                          }
                      }
                  }**/
                radarDataModel.Loction = ConvertSceneToMap(pos);
                //   dataModelsDict[id] = radarDataModel;
                dataModelsList.Add(radarDataModel);
                DataModel.CharaModels.Add(radarDataModel);
            }
            else
            {
                radarDataModel.Loction = ConvertSceneToMap(pos);
                radarDataModel.CharType = 1;
                //  dataModelsDict[id].CharType = 1;
                //   dataModelsDict[id].Loction = ConvertSceneToMap(pos);
            }
            var prefab = "";
            if (type == 0)//player
            {
                prefab = "UI/SceneMap/MieShiMapPlayer.prefab";
            }
            else if (type == 1)//仇人
            {
                prefab = "UI/SceneMap/ChickenMapEnemy.prefab";
            }
            else if (type == 2)//鸡王
            {
                prefab = "UI/SceneMap/ChickenMapBoss.prefab";
            }
            else if (type == 3)//魔王
            {
                prefab = "UI/SceneMap/ChickenMapKing.prefab";
            }
            var e1 = new ChickenSceneMapRadar(radarDataModel, 1, prefab);
            EventDispatcher.Instance.DispatchEvent(e1);
        }

        private Vector3 RotaVector3(Vector3 start, Vector3 axit, float angle)
        {
            var aaa =
                new Vector3(
                    (start.x - axit.x) * Mathf.Cos(angle * Mathf.PI / 180) -
                    (start.y - axit.y) * Mathf.Sin(angle * Mathf.PI / 180)
                    + axit.x,
                    (start.x - axit.x) * Mathf.Sin(angle * Mathf.PI / 180)
                    + (start.y -
                       axit.y)
                    * Mathf.Cos(angle * Mathf.PI / 180) + axit.y);

            return aaa;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "ConvertSceneToMap")
            {
                var loc = (Vector3)param[0];
                return ConvertSceneToMap(loc);
            }
            return null;
        }

        public void OnShow()
        {
            var e = new SceneMapNotifyTeam(true);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void Close()
        {
            var e = new SceneMapNotifyTeam(false);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void RefreshData(UIInitArguments data)
        {
            var __enumerator5 = (DataModel.PathList).GetEnumerator();
            while (__enumerator5.MoveNext())
            {
                var model = __enumerator5.Current;
                {
                    model.IsShow = false;
                }
            }
            DataModel.PathList.Clear();
            if (mIsDrawPath)
            {
                var target = ObjManager.Instance.MyPlayer.TargetPos;
                DrawPathLoction(target);
            }
            InitModel();
        
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
    }
}