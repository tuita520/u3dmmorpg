/********************************************************************************* 

                         Scorpion



  *FileName:SceneMapFrameCtrler

  *Version:1.0

  *Date:2017-07-12

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
using DataTable;
using EventSystem;
using ObjCommand;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class SceneMapFrameCtrler : IControllerBase
    {
        #region 构造函数
        public SceneMapFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(RefresSceneMap.EVENT_TYPE, OnRefurbishSceneMapEvent);
            EventDispatcher.Instance.AddEventListener(Postion_Change_Event.EVENT_TYPE, OnPositionChangeEvent);
            EventDispatcher.Instance.AddEventListener(MapSceneClickLoction.EVENT_TYPE, OnMapSceneClickLoctionEvent);
            EventDispatcher.Instance.AddEventListener(MapSceneClickCell.EVENT_TYPE, OnMapSceneClickCellEvent);
            EventDispatcher.Instance.AddEventListener(MapSceneDrawPath.EVENT_TYPE, OnMapSceneDrawPathEvent);
            EventDispatcher.Instance.AddEventListener(MapSceneCancelPath.EVENT_TYPE, OnMapSceneCancelPathEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SceneMap_AddSceneItemDataModel.EVENT_TYPE, OnEnhanceSceneProvisionEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SceneMap_BtnTranfer.EVENT_TYPE, OnBtnTranferEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnLevelupEvent);

            //EventDispatcher.Instance.AddEventListener(Character_Create_Event.EVENT_TYPE, OnCreateCharacter);
            //EventDispatcher.Instance.AddEventListener(Character_Remove_Event.EVENT_TYPE, OnRemoveCharacter);
            EventDispatcher.Instance.AddEventListener(PlayerCampChangeEvent.EVENT_TYPE, OnPlayerCampExchangeEvent);
            EventDispatcher.Instance.AddEventListener(StrongpointStateChangedEvent.EVENT_TYPE, OnStrongpointStateChangedEvent);
            EventDispatcher.Instance.AddEventListener(ScenePlayerInfoEvent.EVENT_TYPE, OnScenePlayerInfoEvent);
            EventDispatcher.Instance.AddEventListener(MapNpcInfoEvent.EVENT_TYPE, OnMapNpcInfoEvent);

            EventDispatcher.Instance.AddEventListener(MapSceneMsgOperation.EVENT_TYPE, OnMapSceneMsgManipulationEvent);
            EventDispatcher.Instance.AddEventListener(AcientBattleFieldCurrBossEvent.EVENT_TYPE, OnAcientBattleFieldEnterSceneEvent);
        }
        #endregion

        #region 成员变量
        private float MAP_HIGHT = 390.0f;
        private bool mIsDrawPath;
        private SceneRecord mSceneRecord;
        private List<SceneNpcDataModel> NpcDataModels = new List<SceneNpcDataModel>();
        private int SelectedSceneId;
        private SceneMapDataModel DataModel { get; set; }
        private bool IsInit = false;
        private Dictionary<int, int> LodeDic = new Dictionary<int, int>();
        #endregion

        #region 事件
        private void OnEnhanceSceneProvisionEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SceneMap_AddSceneItemDataModel;
            if (_e.DataModel != null)
            {
                DataModel.BigMapData.Add(_e.DataModel);
            }
        }
        private void OnBtnTranferEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SceneMap_BtnTranfer;
            var vipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
            var _sceneRecord = Table.GetScene(_e.SceneId);
            if (_sceneRecord == null)
            {
                return;
            }

            if (PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold < _sceneRecord.ConsumeMoney)
            {
                if (vipLevel >= 4)
                {
                    if (PlayerDataManager.Instance.GetLevel() < _sceneRecord.LevelLimit)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000108));
                        return;
                    }
                    if (_sceneRecord.IsPublic != 1)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005011));
                        return;
                    }
                    if (GameLogic.Instance.Scene != null)
                    {
                        if (GameLogic.Instance.Scene.SceneTypeId == _e.SceneId)
                        {
                            return;
                        }
                    }
                    NetManager.Instance.StartCoroutine(VIPGotoCoroutine(_e.SceneId));
                    return;
                }
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210203));
                return;
            }
            if (PlayerDataManager.Instance.GetLevel() < _sceneRecord.LevelLimit)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000108));
                return;
            }
            if (_sceneRecord.IsPublic != 1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005011));
                return;
            }
            if (GameLogic.Instance.Scene != null)
            {
                if (GameLogic.Instance.Scene.SceneTypeId == _e.SceneId)
                {
                    return;
                }
            }
            if (DataModel.CheckShowConsumeMsg)
            {
                NetManager.Instance.StartCoroutine(GotoCoroutine(_e.SceneId));
            }
            else
            {
                if (vipLevel < 4)
                {
                    SelectedSceneId = _e.SceneId;
                    var _tbScene = Table.GetScene(_e.SceneId);
                    //   是否消耗{0}金币，传送至{1}？
                    DataModel.ConsumeMsg = string.Format(GameUtils.GetDictionaryText(270243), _sceneRecord.ConsumeMoney,
                        _tbScene.Name);
                    DataModel.ShowConsumeMsg = true;
                }
                else
                {
                    NetManager.Instance.StartCoroutine(VIPGotoCoroutine(_e.SceneId));
                }

            }
        }
        private void OnLevelupEvent(IEvent ievent)
        {
            var _playerLevel = PlayerDataManager.Instance.GetLevel();
            {
                // foreach(var VARIABLE in DataModel.BigMapData)
                var _enumerator3 = (DataModel.BigMapData).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _VARIABLE = _enumerator3.Current;
                    {
                        var _sceneTable = Table.GetScene(_VARIABLE.SceneId);

                        _VARIABLE.Enable = (_sceneTable.IsPublic == 1) && (_sceneTable.LevelLimit <= _playerLevel);
                    }
                }
            }
        }
        private void OnMapNpcInfoEvent(IEvent ievent)
        {
            var _e = ievent as MapNpcInfoEvent;
            var _infos = _e.Info.Data;
            foreach (var info in _infos)
            {
                if (info.Alive)
                {
                    OnNpcProduce(info.TableId);
                }
                else
                {
                    OnNpcDie(info.TableId);
                }
            }
        }
        private void OnMapSceneCancelPathEvent(IEvent ievent)
        {
            //MapSceneCancelPath e = ievent as MapSceneCancelPath;
            DataModel.PathList.Clear();
            mIsDrawPath = false;
        }
        private void OnMapSceneClickCellEvent(IEvent ievent)
        {
            var _e = ievent as MapSceneClickCell;

            var _itemData = _e.Data;

            ObjManager.Instance.MyPlayer.LeaveAutoCombat();
            var _tbVip = PlayerDataManager.Instance.TbVip;

            if (mSceneRecord.Type == 0)
            {
                if (_tbVip.AreaLimitTrans == 1)
                {
                    FlyTo(_itemData.SceneLoction.x, _itemData.SceneLoction.z);
                    return;
                }
                do
                {
                    _tbVip = Table.GetVIP(_tbVip.Id + 1);
                } while (0 == _tbVip.AreaLimitTrans);
                GameUtils.GuideToBuyVip(_tbVip.Id, 270254);
            }

            mIsDrawPath = true;
            DrawTargetWayLoction(_itemData.SceneLoction, 1.0f);

            GameControl.Executer.Stop();

            var _command = new MoveCommand(ObjManager.Instance.MyPlayer, _itemData.SceneLoction, 1.0f);
            GameControl.Executer.PushCommand(_command);

            if (_itemData.CharType == 2)
            {
                var _command1 = new FuncCommand(() => { ObjManager.Instance.MyPlayer.EnterAutoCombat(); });
                GameControl.Executer.PushCommand(_command1);
            }
            else if (_itemData.CharType == 1)
            {
                var _command1 = new FuncCommand(() => { MissionManager.Instance.OpenMissionByNpcId(_itemData.NpcId); });
                GameControl.Executer.PushCommand(_command1);
            }
        }
        private void OnMapSceneClickLoctionEvent(IEvent ievent)
        {
            var _e = ievent as MapSceneClickLoction;
            var _loc = TransformMapToScene(_e.Loction);
            GameControl.Executer.Stop();
            ObjManager.Instance.MyPlayer.LeaveAutoCombat();
            var _isVip = false;
            if (_isVip)
            {
                FlyTo(_loc.x, _loc.z);
                return;
            }
            ObjManager.Instance.MyPlayer.fastReachSceneID = GameLogic.Instance.Scene.SceneTypeId;
            ObjManager.Instance.MyPlayer.fastReachPos = _loc;
            ObjManager.Instance.MyPlayer.MoveTo(_loc, 0.05f, true);
            GameControl.Executer.Stop();
            mIsDrawPath = true;
            DrawTargetWayLoction(_loc);
        }
        private void OnMapSceneDrawPathEvent(IEvent ievent)
        {
            var _e = ievent as MapSceneDrawPath;

            mIsDrawPath = true;
            DrawTargetWayLoction(_e.Postion, _e.Offset);
        }
        private void OnMapSceneMsgManipulationEvent(IEvent ievent)
        {
            var _e = ievent as MapSceneMsgOperation;
            switch (_e.Type)
            {
                case 0:
                    OkWasteMsg();
                    break;
                case 1:
                    AbolishWasteMsg();
                    break;
                case 2:
                    ExamineWasteClick();
                    break;
            }
        }
        private void OnPlayerCampExchangeEvent(IEvent ievent)
        {
            if (mSceneRecord == null)
            {
                return;
            }
            var _fubenId = mSceneRecord.FubenId;
            var _tbFuben = Table.GetFuben(_fubenId);
            if (_tbFuben == null)
            {
                return;
            }
            var _objMy = ObjManager.Instance.MyPlayer;
            if (_objMy == null)
            {
                return;
            }
            var _camp = _objMy == null ? -2 : _objMy.GetCamp();
            if (_tbFuben.MainType == (int)eDungeonMainType.Pvp)
            {
                var _assistType = (eDungeonAssistType)_tbFuben.AssistType;
                switch (_assistType)
                {
                    case eDungeonAssistType.Battlefield1:
                    case eDungeonAssistType.Battlefield2:
                    case eDungeonAssistType.Battlefield3:
                    case eDungeonAssistType.Battlefield4:
                    case eDungeonAssistType.Battlefield5:
                        {
                            foreach (var dataModel in DataModel.NpcList)
                            {
                                SetSprite(Table.GetMapTransfer(dataModel.Id), dataModel, _camp);
                                if (dataModel.CharType == 8)
                                {
                                    if (_camp == 4)
                                    {
                                        dataModel.Color = MColor.red;
                                    }
                                    else
                                    {
                                        dataModel.Color = MColor.green;
                                    }
                                }
                                else if (dataModel.CharType == 9)
                                {
                                    if (_camp == 4)
                                    {
                                        dataModel.Color = MColor.green;
                                    }
                                    else
                                    {
                                        dataModel.Color = MColor.red;
                                    }
                                }
                            }
                        }
                        break;
                    case eDungeonAssistType.FrostBF1:
                    case eDungeonAssistType.FrostBF2:
                    case eDungeonAssistType.FrostBF3:
                    case eDungeonAssistType.FrostBF4:
                    case eDungeonAssistType.FrostBF5:
                        {
                            var _controller = UIManager.Instance.GetController(UIConfig.MissionTrackList);
                            var _data = (MissionTrackListDataModel)_controller.GetDataModel("");

                            var _infos = _data.FubenInfoList;
                            var _info = _infos.Cast<FubenInfoItemDataModel>().FirstOrDefault(i => i.Type == 1);

                            foreach (var dataModel in DataModel.NpcList)
                            {
                                SetSprite(Table.GetMapTransfer(dataModel.Id), dataModel, _camp);
                                var _charType = dataModel.CharType;
                                switch (_charType)
                                {
                                    case 8:
                                        {
                                            dataModel.Color = GameUtils.GetTableColor(201);
                                        }
                                        break;
                                    case 9:
                                        {
                                            dataModel.Color = GameUtils.GetTableColor(200);
                                        }
                                        break;
                                    case 10:
                                    case 11:
                                    case 12:
                                        {
                                            if (_info == null)
                                            {
                                                continue;
                                            }
                                            var _tbTransfer = Table.GetMapTransfer(dataModel.Id);
                                            var _idx = _charType - 10;
                                            var _state = _info.States[_idx];
                                            switch (_state)
                                            {
                                                case -1:
                                                    dataModel.Color = GameUtils.GetTableColor(0);
                                                    break;
                                                case 0:
                                                    dataModel.Color = GameUtils.GetTableColor(200);
                                                    dataModel.NpcName = _tbTransfer.Name + GameUtils.GetDictionaryText(220448);
                                                    break;
                                                case 1:
                                                    dataModel.Color = GameUtils.GetTableColor(201);
                                                    dataModel.NpcName = _tbTransfer.Name + GameUtils.GetDictionaryText(220447);
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        private void OnPositionChangeEvent(IEvent ievent)
        {
            var _e = ievent as Postion_Change_Event;
            PositionChange(_e.Loction);
        }
        private void OnRefurbishSceneMapEvent(IEvent ievent)
        {
            var _e = ievent as RefresSceneMap;
            DataModel.SceneId = _e.SceneId;
            mSceneRecord = Table.GetScene(_e.SceneId);
            if (mSceneRecord == null)
            {
                return;
            }
            DataModel.ScenrIcon = mSceneRecord.ShowNameIcon;
            NpcDataModels.Clear();

            var _scale = 1f * mSceneRecord.TerrainHeightMapWidth / mSceneRecord.TerrainHeightMapLength;
            if (_scale > 1.0f)
            {
                DataModel.MapWidth = (int)(MAP_HIGHT);
                DataModel.MapHeight = (int)(MAP_HIGHT / _scale);
            }
            else
            {
                DataModel.MapWidth = (int)(MAP_HIGHT * _scale);
                DataModel.MapHeight = (int)MAP_HIGHT;
            }
            var _camp = ObjManager.Instance.MyPlayer.GetCamp();
            DataModel.NpcList.Clear();
            DataModel.PlayerList.Clear();
            Table.ForeachMapTransfer((record =>
            {
                if (record.SceneID != _e.SceneId)
                {
                    return true;
                }
                var _npcData = new SceneNpcDataModel();
                _npcData.Id = record.Id;
                _npcData.NpcId = record.NpcID;
                _npcData.SceneLoction = new Vector3(record.PosX, 0, record.PosZ);
                _npcData.MapLoction = TransformSceneToMap(_npcData.SceneLoction);
                _npcData.Color = GameUtils.GetTableColor(record.DisplayColor);
                _npcData.NpcName = record.Name;
                _npcData.CharType = record.Type;
                SetSprite(record, _npcData, _camp);
                _npcData.Width = record.PicWidth;
                _npcData.Height = record.PIcHight;
                _npcData.LabLoc = new Vector3(record.OffsetX, record.OffsetY, 0);
                _npcData.NpcType = record.ShowChar;
                _npcData.DisplaySort = record.DisplaySort;
                _npcData.Mark = record.Mark;
                NpcDataModels.Add(_npcData);
                return true;
            }));
            NpcDataModels.Sort((a, b) =>
            {
                if (a.CharType == b.CharType)
                {
                    return (a.DisplaySort < b.DisplaySort) ? -1 : 1;
                }
                return (a.CharType < b.CharType) ? -1 : 1;
            });

            var _obj = ObjManager.Instance.MyPlayer;
            if (_obj)
            {
                PositionChange(_obj.Position);
            }
            RefurbishNpcList();
        }
        private void OnScenePlayerInfoEvent(IEvent ievent)
        {
            var _e = ievent as ScenePlayerInfoEvent;
            var _datas = _e.Info.Data;
            var _objMy = ObjManager.Instance.MyPlayer;
            if (_objMy == null)
            {
                return;
            }
            var _playerList = DataModel.PlayerList;
            _playerList.Clear();
            foreach (var data in _datas)
            {
                if (data.Id == _objMy.GetObjId())
                {
                    continue;
                }
                var _playerOne = new ScenePlayerDataModel();
                var _pos = data.Pos;
                _playerOne.SceneLoction = new Vector3(GameUtils.DividePrecision(_pos.x), 0, GameUtils.DividePrecision(_pos.y));
                _playerOne.MapLoction = TransformSceneToMap(_playerOne.SceneLoction);
                _playerOne.Camp = data.Camp;
                _playerOne.SpriteName = "map_icon_Transfer";
                switch (_playerOne.Camp)
                {
                    case 7:
                        _playerOne.Color = GameUtils.GetTableColor(503);
                        break;
                    case 8:
                        _playerOne.Color = GameUtils.GetTableColor(502);
                        break;
                    case 9:
                        _playerOne.Color = GameUtils.GetTableColor(501);
                        break;
                }
                _playerList.Add(_playerOne);
            }
        }
        private void OnStrongpointStateChangedEvent(IEvent ievent)
        {
            var _e = ievent as StrongpointStateChangedEvent;
            if (_e.State != (int)eStrongpointState.Occupied && _e.State != (int)eStrongpointState.Idle)
            {
                return;
            }
            var _idx = _e.Index;
            if (DataModel.NpcList == null || DataModel.NpcList.Count == 0) return;

            var _npc = DataModel.NpcList.FirstOrDefault(n => n.Mark == _idx);
            if (_npc == null)
            {
                Logger.Error("In OnStrongpointStateChanged(), npc == null");
                return;
            }
            var _tbTransfer = Table.GetMapTransfer(_npc.Id);
            if (_tbTransfer == null)
            {
                Logger.Error("In OnStrongpointStateChanged(), tbTransfer == null");
                return;
            }
            var _name = _tbTransfer.Name;
            switch (_e.Camp)
            {
                case -1: //无人占领
                case 3:
                    _npc.Color = GameUtils.GetTableColor(10);
                    _npc.SpriteName = "Server_r";
                    _npc.Width = 30;
                    _npc.Height = 30;
                    break;
                case 4: //火龙窟蓝方
                    _name += GameUtils.GetDictionaryText(220448);
                    _npc.Color = GameUtils.GetTableColor(200);
                    break;
                case 5: //火龙窟红方
                    _name += GameUtils.GetDictionaryText(220447);
                    _npc.Color = GameUtils.GetTableColor(201);
                    break;
                case 7: //攻城战守方
                    _npc.Color = GameUtils.GetTableColor(503);
                    _npc.SpriteName = "Server_y";
                    _npc.Width = 30;
                    _npc.Height = 30;
                    break;
                case 8: //攻城进攻方1
                    _npc.Color = GameUtils.GetTableColor(502);
                    _npc.SpriteName = "Server_g";
                    _npc.Width = 30;
                    _npc.Height = 30;
                    break;
                case 9: //攻城进攻方2
                    _npc.Color = GameUtils.GetTableColor(501);
                    _npc.SpriteName = "Server_w";
                    _npc.Width = 30;
                    _npc.Height = 30;
                    break;
                default:
                    Logger.Error("In OnStrongpointStateChanged(), e.Side == 0");
                    return;
            }
            _npc.NpcName = _name;

            var _objMy = ObjManager.Instance.MyPlayer;
            if (_objMy == null)
            {
                return;
            }
            var _camp = _objMy.GetCamp();
            switch (_e.Camp)
            {
                case 4: //火龙窟蓝方
                case 5: //火龙窟红方
                    var _iconId = (_e.Camp == _camp || _tbTransfer.LiveIcon2 == -1) ? _tbTransfer.LiveIcon : _tbTransfer.LiveIcon2;
                    var _tbIcon = Table.GetIcon(_iconId);
                    if (_tbIcon != null)
                    {
                        _npc.Atlas = _tbIcon.Atlas;
                        _npc.SpriteName = _tbIcon.Sprite;
                    }
                    break;
            }
        }


        #endregion

        #region 逻辑函数
        private void AbolishWasteMsg()
        {
            DataModel.ShowConsumeMsg = false;
        }
        private void ExamineWasteClick()
        {
            DataModel.ShowConsumeMsg = !DataModel.ShowConsumeMsg;
        }
        private Vector3 TransformMapToScene(Vector3 loc)
        {
            var _x = loc.x / DataModel.MapWidth * mSceneRecord.TerrainHeightMapWidth + mSceneRecord.TerrainHeightMapWidth / 2.0f;
            var _z = loc.y / DataModel.MapHeight * mSceneRecord.TerrainHeightMapLength + mSceneRecord.TerrainHeightMapLength / 2.0f;
            return new Vector3(_x, 0, _z);
        }

        private Vector3 TransformSceneToMap(Vector3 loc)
        {
            var _x = (loc.x - mSceneRecord.TerrainHeightMapWidth / 2.0f) / mSceneRecord.TerrainHeightMapWidth * DataModel.MapWidth;
            var _y = (loc.z - mSceneRecord.TerrainHeightMapLength / 2.0f) / mSceneRecord.TerrainHeightMapLength *
                     DataModel.MapHeight;
            return new Vector3(_x, _y, 0);
        }
        private void DrawWayLoction(List<Vector3> pos)
        {
            var _enumerator1 = (DataModel.PathList).GetEnumerator();
            while (_enumerator1.MoveNext())
            {
                var _model = _enumerator1.Current;
                {
                    _model.IsShow = false;
                }
            }

            DataModel.PathList.Clear();


            pos.Insert(0, ObjManager.Instance.MyPlayer.Position);

            var _target = GainPointsOnLines(pos, 10.0f);
            {
                var _list2 = _target;
                var _listCount2 = _list2.Count;
                for (var _i2 = 0; _i2 < _listCount2; ++_i2)
                {
                    var _vector3 = _list2[_i2];
                    {
                        var _pathData = new ScenePathDataModel();
                        _pathData.Loction = _vector3;
                        DataModel.PathList.Add(_pathData);
                    }
                }
            }
        }
        private void DrawTargetWayLoction(Vector3 point, float offset = 0.05f)
        {
            if (!ObjManager.Instance.MyPlayer)
            {
                return;
            }

            var _tagetPos = ObjManager.Instance.MyPlayer.CalculatePath(point, offset);
            if (_tagetPos.Count == 0)
            {
                mIsDrawPath = false;
                return;
            }
            DrawWayLoction(_tagetPos);
        }
        private void FlyTo(float x, float y, int sceneId = -1)
        {
            if (sceneId == -1)
            {
                sceneId = GameLogic.Instance.Scene.SceneTypeId;
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
            GameUtils.FlyTo(sceneId, x, y);
        }
        private List<Vector3> GainPointsOnLines(List<Vector3> l, float d)
        {
            var _result = new List<Vector3>();
            var _dist = 0.0f;
            var _lCount0 = l.Count - 1;
            for (var i = 0; i < _lCount0; i++)
            {
                var _s = TransformSceneToMap(l[i]);
                var _e = TransformSceneToMap(l[i + 1]);
                var _dir = (_e - _s).normalized;
                while ((_e - _s).magnitude + _dist > d)
                {
                    _s = _s + _dir * (d - _dist);
                    _dist = 0;
                    _result.Add(_s);
                }

                _dist += (_e - _s).magnitude;
            }
            return _result;
        }
        private IEnumerator GotoCoroutine(int sceneid)
        {
            using (new BlockingLayerHelper(0))
            {
                var _table = Table.GetScene(sceneid);
                var _msg = NetManager.Instance.ChangeSceneRequest(sceneid);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (null != SceneManager.Instance)
                        {
                            var _message = string.Format(GameUtils.GetDictionaryText(210204), _table.Name, _table.ConsumeMoney);
                            PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold -= _table.ConsumeMoney;
                            SceneManager.Instance.ChangeSceneOverMessage = _message;
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private IEnumerator VIPGotoCoroutine(int sceneid)
        {
            using (new BlockingLayerHelper(0))
            {
                var _table = Table.GetScene(sceneid);
                var _msg = NetManager.Instance.ChangeSceneRequest(sceneid);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                      //  mIsDrawPath = false;
                        //解决寻路过程中传送到下个地图 上个地图的路线没有消失的问题
                        if (DataModel.PathList.Count > 0)
                        {
                            for (var i = DataModel.PathList.Count - 1; i >= 0; i--)
                            {
                                var _pathDataModel = DataModel.PathList[i];
                                _pathDataModel.IsShow = false;
                                DataModel.PathList.Remove(_pathDataModel);
                            }
                        }
                        if (null != SceneManager.Instance)
                        {

                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }
        private void OkWasteMsg()
        {
            NetManager.Instance.StartCoroutine(GotoCoroutine(SelectedSceneId));
        }
        private void OnNpcProduce(int tableId)
        {
            var _objMy = ObjManager.Instance.MyPlayer;
            if (_objMy == null)
            {
                return;
            }
            var _camp = _objMy.GetCamp();
            if (mSceneRecord == null)
            {
                return;
            }
            foreach (var dataModel in DataModel.NpcList)
            {
                if (tableId != dataModel.Id)
                {
                    continue;
                }
                var _tbTransfer = Table.GetMapTransfer(dataModel.Id);
                if (_tbTransfer == null)
                {
                    continue;
                }
                dataModel.NpcName = _tbTransfer.Name;
                dataModel.Color = GameUtils.GetTableColor(_tbTransfer.DisplayColor);
                SetSprite(_tbTransfer, dataModel, _camp);
                break;
            }
        }

        private int AcientBattleFieldCurrBossId = -1;
        private void OnAcientBattleFieldEnterSceneEvent(IEvent iEvent)
        {
            var v = iEvent as AcientBattleFieldCurrBossEvent;
            if (v != null)
            {
                AcientBattleFieldCurrBossId = v.CurrBossId;
            }
        }

        private void OnNpcDie(int tableId)
        {
            foreach (var dataModel in DataModel.NpcList)
            {
                if (dataModel.Id != tableId)
                {
                    continue;
                }
                var _tbTransfer = Table.GetMapTransfer(dataModel.Id);
                if (_tbTransfer == null)
                {
                    continue;
                }
                //EventDispatcher.Instance.DispatchEvent(new UIBossHomeDieRefreshEvent(_tbTransfer.NpcID));
                if (_tbTransfer.NpcID == AcientBattleFieldCurrBossId)
                {
                    EventDispatcher.Instance.DispatchEvent(new ActivityAndDungeonCombatResultEvent(eDungeonCompleteType.Success));
                    AcientBattleFieldCurrBossId = -1;
                }
                dataModel.NpcName = _tbTransfer.Name;
                dataModel.Color = MColor.grey;
                var _tbIcon = Table.GetIcon(_tbTransfer.DeadIcon);
                if (_tbIcon == null)
                {
                    break;
                }
                dataModel.Atlas = _tbIcon.Atlas;
                dataModel.SpriteName = _tbIcon.Sprite;
                break;
            }
        }
        private void OnPropertyChangeToggle(object sender, PropertyChangedEventArgs e)
        {
            RefurbishNpcList();
        }
        private void PositionChange(Vector3 objLoction)
        {
            if (ObjManager.Instance.MyPlayer == null
                || ObjManager.Instance.MyPlayer.ObjTransform == null)
            {
                return;
            }
            var _v = ObjManager.Instance.MyPlayer.ObjTransform.eulerAngles;
            DataModel.PalyerLocX = (int)objLoction.x;
            DataModel.PalyerLocY = (int)objLoction.z;
            DataModel.SelfMapLoction = TransformSceneToMap(objLoction);
            DataModel.SelfMapRotation = new Vector3(0, 0, -_v.y - 45f);
            if (DataModel.PathList.Count > 0)
            {
                var _start = false;
                for (var i = DataModel.PathList.Count - 1; i >= 0; i--)
                {
                    var _pathDataModel = DataModel.PathList[i];

                    if (_start)
                    {
                        _pathDataModel.IsShow = false;
                        DataModel.PathList.Remove(_pathDataModel);
                    }
                    else
                    {
                        if ((_pathDataModel.Loction - DataModel.SelfMapLoction).sqrMagnitude < 20.0f)
                        {
                            _pathDataModel.IsShow = false;
                            DataModel.PathList.Remove(_pathDataModel);
                            _start = true;
                        }
                    }
                }
            }
            if (DataModel.PathList.Count == 0 && mIsDrawPath)
            {
                mIsDrawPath = false;
            }
        }
        private void RefurbishNpcList()
        {
            var _list = new List<SceneNpcDataModel>();
            {
                var _list4 = NpcDataModels;
                var _listCount4 = _list4.Count;
                for (var __i4 = 0; __i4 < _listCount4; ++__i4)
                {
                    var _model = _list4[__i4];
                    {
                        //战旗和矿脉
                        if (_model.CharType == 15)
                        {
                            if (DataModel.ToggleList[3])
                                _list.Add(_model);
                        }
                        else if (_model.CharType == 16)
                        {
                            if (DataModel.ToggleList[4])
                                _list.Add(_model);
                        }
                        else if (_model.CharType > 2 || DataModel.ToggleList[_model.CharType])
                        {
                            _list.Add(_model);
                        }
                    }
                }
            }
            DataModel.NpcList = new ObservableCollection<SceneNpcDataModel>(_list);
        }
        private void SetSprite(MapTransferRecord record, SceneNpcDataModel npcData, int myCamp)
        {
            var _iconId = (record.Camp == myCamp || record.LiveIcon2 == -1) ? record.LiveIcon : record.LiveIcon2;
            var _tbIcon = Table.GetIcon(_iconId);
            if (_tbIcon != null)
            {
                npcData.Atlas = _tbIcon.Atlas;
                npcData.SpriteName = _tbIcon.Sprite;
            }
            switch (npcData.CharType)
            {
                case 8:
                case 9:
                    npcData.Color = record.Camp == myCamp ? MColor.green : MColor.red;
                    break;
            }
        }
        #endregion

        //private void OnCreateCharacter(IEvent ievent)
        //{
        //    var e = ievent as Character_Create_Event;
        //    var charId = e.CharacterId;
        //    var obj = ObjManager.Instance.FindCharacterById(charId);
        //    if (obj == null)
        //    {
        //        return;
        //    }
        //    if (obj.GetObjType() != OBJ.TYPE.NPC)
        //    {
        //        return;
        //    }
        //    OnNpcCreate();
        //}

        //private void OnRemoveCharacter(IEvent ievent)
        //{
        //    var e = ievent as Character_Remove_Event;
        //    var charId = e.CharacterId;
        //    var obj = ObjManager.Instance.FindCharacterById(charId);
        //    if (obj == null)
        //    {
        //        return;
        //    }
        //    if (obj.GetObjType() != OBJ.TYPE.NPC)
        //    {
        //        return;
        //    }
        //    if (mSceneRecord == null)
        //    {
        //        return;
        //    }
        //    OnNpcDie(obj.GetDataId());
        //}

        


        #region 固有函数
        public void CleanUp()
        {
            if (DataModel != null)
            {
                DataModel.ToggleList.PropertyChanged -= OnPropertyChangeToggle;
            }
            mSceneRecord = null;
            DataModel = new SceneMapDataModel();

            DataModel.ToggleList.PropertyChanged += OnPropertyChangeToggle;
            var _DataModelToggleListCount1 = DataModel.ToggleList.Count;
            for (var i = 0; i < _DataModelToggleListCount1; i++)
            {
                DataModel.ToggleList[i] = true;
            }
            MAP_HIGHT = Table.GetClientConfig(1006).Value.ToInt();
            if (!IsInit)
            {
                Table.ForeachLode(tb =>
                {
                    LodeDic.Add(tb.Id, tb.LodeInMap);
                    return true;
                });
                IsInit = true;
            }
        }

        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "ConvertSceneToMap")
            {
                var _loc = (Vector3)param[0];
                return TransformSceneToMap(_loc);
            }
            return null;
        }

        public void OnShow()
        {
            var _e = new SceneMapNotifyTeam(true);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        public void Close()
        {
            var _e = new SceneMapNotifyTeam(false);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        public void Tick()
        {
        }
        public void RefreshData(UIInitArguments data)
        {
            {
                // foreach(var model in DataModel.PathList)
                var _enumerator5 = (DataModel.PathList).GetEnumerator();
                while (_enumerator5.MoveNext())
                {
                    var _model = _enumerator5.Current;
                    {
                        _model.IsShow = false;
                    }
                }
            }
            DataModel.PathList.Clear();

            if (mIsDrawPath)
            {
                var _target = ObjManager.Instance.MyPlayer.TargetPos;
                DrawWayLoction(_target);
            }
            DataModel.ShowConsumeMsg = false;
            if (LodeDic.ContainsValue(DataModel.SceneId))
            {
                DataModel.IsShowLodeButton = true;
            }
            else
            {
                DataModel.IsShowLodeButton = false;
            }
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name != "")
            {
                // foreach(var dataModel in DataModel.BigMapData)
                var _enumerator6 = (DataModel.BigMapData).GetEnumerator();
                while (_enumerator6.MoveNext())
                {
                    var _dataModel = _enumerator6.Current;
                    {
                        if (_dataModel.SceneId == Int32.Parse(name))
                        {
                            return _dataModel;
                        }
                    }
                }
            }
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

    }
}