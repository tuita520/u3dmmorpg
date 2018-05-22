/********************************************************************************* 

                         Scorpion



  *FileName:CachotFrameCtrler

  *Version:1.0

  *Date:2017-07-13

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
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class CachotFrameCtrler : IControllerBase
    {
        private int copyId = 0;
        private TeamTargetChangeDataModel TeamModule = new TeamTargetChangeDataModel();
        private eDungeonCompleteType DungeonCompleteType = eDungeonCompleteType.Failed;

        #region 静态变量

        private static Dictionary<int, int> s_dicDealErrs = new Dictionary<int, int>
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

        #endregion

        #region 成员变量
   
        private int m_iDrawId;
        private int m_iDrawIndex;
        //0 none 1 rank 2 fight 3 rank & fight
        private DungeonDataModel DataModel { get; set; }
        private QueueUpDataModel m_QueueUpData
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.QueueUpData; }
        }

        #endregion

        #region 构造函数

        public CachotFrameCtrler()
        {
            CleanUp();
        
            EventDispatcher.Instance.AddEventListener(TeamWorldWorldSpeakCopyEvent.EVENT_TYPE, ChatTeam);
            EventDispatcher.Instance.AddEventListener(TeamWorldAutoMatchCopyEvent.EVENT_TYPE, AutoMatch);
            EventDispatcher.Instance.AddEventListener(TeamWorldAutoMatchCopyCancelEvent.EVENT_TYPE, cancelAutoMatch);
            EventDispatcher.Instance.AddEventListener(AutoMatchState_Event.EVENT_TYPE, OnAutoMatchState);
            EventDispatcher.Instance.AddEventListener(DungeonGroupCellClick2.EVENT_TYPE, OnClickCachotOrganizationCellEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnInitionExDataEvent);
            EventDispatcher.Instance.AddEventListener(DungeonBtnClick.EVENT_TYPE, OnClickButtonEvent);
            EventDispatcher.Instance.AddEventListener(DungeonInfosMainInfo.EVENT_TYPE, OnClickChooseCoreCachotEvent);
            EventDispatcher.Instance.AddEventListener(QueneUpdateEvent.EVENT_TYPE, OnRenewalQueneEvent);
            EventDispatcher.Instance.AddEventListener(DungeonSweepRandAward.EVENT_TYPE, OnCachotCleanOffRandomPrizeEvent);
            EventDispatcher.Instance.AddEventListener(DungeonResetCountUpdate.EVENT_TYPE, OnCachotToClearNumRenewalEvent);
            EventDispatcher.Instance.AddEventListener(DungeonEnterCountUpdate.EVENT_TYPE, OnCachotGoInNumRenewalEvent);
            EventDispatcher.Instance.AddEventListener(DungeonSetScan.EVENT_TYPE, OnSettingDisplayScannedEvent);
            EventDispatcher.Instance.AddEventListener(ExitFuBenWithOutMessageBoxEvent.EVENT_TYPE, OnQuitGameInstanceWithOutMsgBoxEvent);
            EventDispatcher.Instance.AddEventListener(ActivityAndDungeonCombatResultEvent.EVENT_TYPE, OnDungeonCombatResultEvent);

        }

        private void OnDungeonCombatResultEvent(IEvent iEvent)
        {
            var v = iEvent as ActivityAndDungeonCombatResultEvent;
            if (v != null)
            {
                DungeonCompleteType = v.Type;
            }        
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            DataModel = new DungeonDataModel();
            DataModel.ScanItemId = 22053;       
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            DataModel.IsShowScan = 0;
            var _playerData = PlayerDataManager.Instance;
            {
                // foreach(var info in DataModel.MainInfos)
                var _enumerator9 = (DataModel.MainInfos).GetEnumerator();
                while (_enumerator9.MoveNext())
                {
                    var _info = _enumerator9.Current;
                    {
                        {
                            // foreach(var i in info.Infos)
                            var _enumerator21 = (_info.Infos).GetEnumerator();
                            while (_enumerator21.MoveNext())
                            {
                                var _i = _enumerator21.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1)
                                    {
                                        var _tbDungeon = Table.GetFuben(_id);
                                        _i.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            {
                // foreach(var info in DataModel.TeamInfos)
                var _enumerator10 = (DataModel.TeamInfos).GetEnumerator();
                while (_enumerator10.MoveNext())
                {
                    var _info = _enumerator10.Current;
                    {
                        {
                            // foreach(var i in info.Infos)
                            var _enumerator22 = (_info.Infos).GetEnumerator();
                            while (_enumerator22.MoveNext())
                            {
                                var _i = _enumerator22.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1)
                                    {
                                        var _tbDungeon = Table.GetFuben(_id);
                                        _i.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // vip
            {
                foreach (var vipData in DataModel.VipInfos)
                {
                    foreach (var info in vipData.Infos)
                    {
                        var _i = info;
                        {
                            var _id = _i.Id;
                            if (_id != -1)
                            {
                                var _tbDungeon = Table.GetFuben(_id);
                                _i.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                            }
                        }
                    }
                }
            }



            var _arg = data as DungeonArguments;
            if (_arg != null)
            {
                var _dungeondId = _arg.Tab;
                var _tbFuben = Table.GetPlotFuben(_dungeondId);
                var _diffcult = 0;
                if (_arg.Args != null && _arg.Args.Count > 0)
                {
                    _diffcult = _arg.Args[0];
                    if (_diffcult < 0 || _diffcult > 3)
                    {
                        _diffcult = 0;
                    }
                }
                if (_tbFuben != null)
                {
                    if (_tbFuben.FubenType == (int)eDungeonType.Main)
                    {
                        DataModel.ToggleSelect = 0;
                        var _index = 0;
                        var _count = DataModel.MainInfos.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _d = DataModel.MainInfos[i];
                            if (_d.Id == _dungeondId)
                            {
                                _index = i;
                                break;
                            }
                        }
                        ChooseCoreCachotTeam(_index, _diffcult);
                    }
                    else if (_tbFuben.FubenType == (int)eDungeonType.Team)
                    {
                        DataModel.ToggleSelect = 1;
                        var _index = 0;
                        var _count = DataModel.TeamInfos.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _d = DataModel.TeamInfos[i];
                            if (_d.Id == _dungeondId)
                            {
                                _index = i;
                                break;
                            }
                        }
                        ChooseTeamCachotGroup(_index);
                    }
                    else if (_tbFuben.FubenType == (int)eDungeonType.Vip)
                    {
                        DataModel.ToggleSelect = 2;
                        var _index = 0;
                        var _count = DataModel.VipInfos.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _d = DataModel.VipInfos[i];
                            if (_d.Id == _dungeondId)
                            {
                                _index = i;
                                break;
                            }
                        }
                        ChooseVipCachotGroup(_index, _diffcult);
                    }
                    return;
                }
            }
            DataModel.ToggleSelect = 0;
            ChooseCoreCachotTeam(0);
            UpdateUIStated();
            AnalyticalNotices();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name.Equals("Team"))
                return TeamModule;
            return DataModel;
        }

        public void OnChangeScene(int sceneId)
        {
            if (sceneId == -1)
            {
                return;
            }
            UpdateQueueingMsg();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
            DataModel.IsShowScan = 0;
      
            TeamModule.isLeader = IsLeader();
            TeamModule.teamState = IsHavaTeam();
            if (DataModel.ToggleSelect == 1)
            {
                ChooseTeamCachotMsg(2);
            }
        }

        public FrameState State { get; set; }

        #endregion

        #region 逻辑函数

        private void AnalyticalNotices()
        {
            var _count = 0;
            {
                var _enumerator14 = (DataModel.MainInfos).GetEnumerator();
                while (_enumerator14.MoveNext())
                {
                    var _info = _enumerator14.Current;
                    {
                        var _restCount = 0;
                        {
                            var _enumerator25 = (_info.Infos).GetEnumerator();
                            while (_enumerator25.MoveNext())
                            {
                                var _i = _enumerator25.Current;
                                {
                                    if (_i.IsLock == false)
                                    {
                                        _restCount += _i.TotalCount - _i.EnterCount;
                                    }
                                }
                            }
                        }
                        _info.DungeonCount = _restCount;
                        _count += _restCount;
                    }
                }
            }
            PlayerDataManager.Instance.NoticeData.DungeonMain = _count;
        }

        private IEnumerator MakeSureGoTeamCachotCoroutine(int deungeonId, int isOk)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ResultTeamEnterFuben(deungeonId, isOk);
                yield return _msg.SendAndWaitUntilDone();

                //副本是否进入成功无任何返回值。
                //         if (msg.State == MessageState.Reply)
                //         {
                //             if (msg.ErrorCode == (int)ErrorCodes.OK)
                //             {
                //                 //                 QueueUpData.QueueId = -1;
                //             }
                //             else
                //             {
                //                 UIManager.Instance.ShowNetError(msg.ErrorCode);
                //                 Logger.Warn(".....ResultTeamEnterFuben.......{0}.", msg.ErrorCode);
                //             }
                //         }
                //         else
                //         {
                //             Logger.Warn(".....ResultTeamEnterFuben.......{0}.", msg.State);
                //         }
                //副本是否进入成功无任何返回值。
                if (isOk == 1)
                {
                    m_QueueUpData.QueueId = -1;
                    UpdateQueueingMsg();
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_WindowShowDungeonQueue(Game.Instance.ServerTime, -1));

                    var e = new LineMemberConfirmEvent(PlayerDataManager.Instance.CharacterGuid, isOk);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
        }

        //处理网络错误消息
        private bool DisposedMistakeCode(int errCode, int fubenId, List<ulong> playerIds)
        {
            if (s_dicDealErrs.Keys.Contains(errCode))
            {
                var _dicId = s_dicDealErrs[errCode];
                if (playerIds.Count <= 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dicId));
                }
                else
                {
                    var _teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
                    var _team = _teamData.TeamList.Where(p => p.Guid != 0ul && p.Level > 0);
                    var _players = _team.Where(p => playerIds.Contains(p.Guid));
                    var _names = _players.Aggregate(string.Empty, (current, p) => current + (p.Name + ","));
                    if (_names.Length <= 0)
                    {
                        return true;
                    }
                    //特殊处理！！！
                    if (errCode == (int)ErrorCodes.Error_LevelNoEnough)
                    {
                        var _tbFuben = Table.GetFuben(fubenId);
                        var _assistType = (eDungeonAssistType)_tbFuben.AssistType;
                        if (_assistType == eDungeonAssistType.BloodCastle || _assistType == eDungeonAssistType.DevilSquare)
                        {
                            var _playerData = PlayerDataManager.Instance;
                            var _fubenCount = _playerData.GetExData(_tbFuben.TotleExdata);
                            if (_fubenCount > 0)
                            {
                                _dicId = 489;
                            }
                            else
                            {
                                _dicId = 491;
                            }
                        }
                    }
                    _names = _names.Substring(0, _names.Length - 1);
                    var _content = string.Format(GameUtils.GetDictionaryText(_dicId), _names);
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_content));
                }
                return true;
            }
            return false;
        }

        private void GoIntoCoreCachot()
        {
            if (PlayerDataManager.Instance.IsInPvPScnen())
            {
                GameUtils.ShowHintTip(456);
                return;
            }
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
            if (_sceneId == _tbDungeon.SceneId)
            {
                //已经在此副本当中了
                var _e = new ShowUIHintBoard(270081);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _playerData = PlayerDataManager.Instance;
            var _dicCom = _playerData.CheckCondition(_tbDungeon.EnterConditionId);
            if (_dicCom != 0)
            {
                //不符合副本进入条件 270234
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dicCom));
                return;
            }
            if (_data.EnterCount == _data.TotalCount)
            {
                if (_data.ResetCount < _tbDungeon.TodayBuyCount)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(434));
                }
                else
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(438));
                }
                return;
            }

            var _tbDungeonNeedItemIdLength1 = _tbDungeon.NeedItemId.Count;
            for (var i = 0; i < _tbDungeonNeedItemIdLength1; i++)
            {
                if (_tbDungeon.NeedItemId[i] != -1)
                {
                    if (_playerData.GetItemCount(_tbDungeon.NeedItemId[i]) < _tbDungeon.NeedItemCount[i])
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                        return;
                    }
                }
            }
            //GameUtils.EnterFuben(DataModel.SelectDungeon.InfoData.Id);
            NoticedGoIntoCachotWar(_tbDungeon.FightPoint,
                () =>
                {
                    if (DataModel != null)
                        GameUtils.EnterFuben(DataModel.SelectDungeon.InfoData.Id);
                });
        }

        private void NoticedGoIntoCachotWar(int needFightValue, Action callback)
        {
            var _noticePercent = 100;
            var _confRecord = Table.GetClientConfig(498);
            if (_confRecord != null)
            {
                var _intValue = _confRecord.ToInt();
                if (_intValue != -1)
                    _noticePercent = _intValue;
            }
            var _myFightValue = PlayerDataManager.Instance.PlayerDataModel.Attributes.FightValue;
            if (_myFightValue < needFightValue * _noticePercent / 100)
            {
                UIManager.Instance.ShowMessage(
                    MessageBoxType.OkCancel,
                    1712,
                    "",
                    callback);
            }
            else
            {
                callback();
            }
        }

        private void GoIntoVipCachot()
        {
            if (PlayerDataManager.Instance.IsInPvPScnen())
            {
                GameUtils.ShowHintTip(456);
                return;
            }
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
            if (_sceneId == _tbDungeon.SceneId)
            {
                //已经在此副本当中了
                var _e = new ShowUIHintBoard(270081);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _playerData = PlayerDataManager.Instance;
            var _dicCom = _playerData.CheckCondition(_tbDungeon.EnterConditionId);
            if (_dicCom != 0)
            {
                //不符合副本进入条件 270234
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dicCom));
                return;
            }
            if (_data.EnterCount == _data.TotalCount)
            {
                if (_data.ResetCount < _tbDungeon.TodayBuyCount)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(490));
                }
                else
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(490));
                }
                return;
            }

            var _tbDungeonNeedItemIdLength1 = _tbDungeon.NeedItemId.Count;
            for (var i = 0; i < _tbDungeonNeedItemIdLength1; i++)
            {
                if (_tbDungeon.NeedItemId[i] != -1)
                {
                    if (_playerData.GetItemCount(_tbDungeon.NeedItemId[i]) < _tbDungeon.NeedItemCount[i])
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                        return;
                    }
                }
            }
            //GameUtils.EnterFuben(DataModel.SelectDungeon.InfoData.Id);
            NoticedGoIntoCachotWar(_tbDungeon.FightPoint,
                () =>
                {
                    if (DataModel != null)
                        GameUtils.EnterFuben(DataModel.SelectDungeon.InfoData.Id);
                });
        }

        private void GoIntoTeamCachot()
        {
            if (PlayerDataManager.Instance.IsInPvPScnen())
            {
                GameUtils.ShowHintTip(456);
                return;
            }
            var _teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
            var _count = _teamData.TeamList.Count(i => i.Guid != 0);
            if (_count == 0)
            {
                //
                //var e = new ShowUIHintBoard(439);
                //EventDispatcher.Instance.DispatchEvent(e);

                GameUtils.EnterFuben(DataModel.SelectDungeon.InfoData.Id);

                return;
            }
            if (_teamData.TeamList[0].Guid != ObjManager.Instance.MyPlayer.GetObjId())
            {
                //
                var _e = new ShowUIHintBoard(440);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
            if (_sceneId == _tbDungeon.SceneId)
            {
                //已经在此副本当中了
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270081));
                return;
            }

            if (GameLogic.Instance != null && GameLogic.Instance.Scene != null)
            {
                var _oldTbScene = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
                var _newTbScene = Table.GetScene(_sceneId);

                if (_oldTbScene != null && _newTbScene != null)
                {
                    if (_oldTbScene.FubenId != -1 && _newTbScene.FubenId != -1)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210123));
                        return;
                    }
                }
            }

            var _playerData = PlayerDataManager.Instance;
            var _dicCom = _playerData.CheckCondition(_tbDungeon.EnterConditionId);
            if (_dicCom != 0)
            {
                //不符合副本进入条件 270234
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dicCom));
                return;
            }

            if (_tbDungeon.QueueParam == -1)
            {
                return;
            }

            var _tbQueue = Table.GetQueue(_tbDungeon.QueueParam);


            if (_count < _tbQueue.CountLimit)
            {
                //             UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 441, "",
                //                 () => { NetManager.Instance.StartCoroutine(EnterTeamDungeonCoroutine()); });
                //             return;
            }
            if (_count > _tbQueue.CountLimit)
            {
                //队伍人数大于副本的要求人数
                var _e = new ShowUIHintBoard(469);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            if (_data.EnterCount == _data.TotalCount)
            {
                if (_data.ResetCount < _tbDungeon.TodayBuyCount)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(434));
                }
                else
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(438));
                }
                return;
            }
            if (PlayerDataManager.Instance.TeamInviteClickFubenID == _id)
            {
                int seconds = (System.DateTime.Now - PlayerDataManager.Instance.TeamInviteClickFubenTime).Seconds;
                if (seconds >= 0 && seconds < 3)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001442));
                    return;
                }
            }
            NoticedGoIntoCachotWar(_tbDungeon.FightPoint,
                () =>
                {
                    NetManager.Instance.StartCoroutine(GoIntoTeamCachotCoroutine());
                }
                );
            //NetManager.Instance.StartCoroutine(EnterTeamDungeonCoroutine());
        }

        private IEnumerator GoIntoTeamCachotCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                PlayerDataManager.Instance.TeamInviteClickFubenID = DataModel.SelectDungeon.InfoData.Id;
                PlayerDataManager.Instance.TeamInviteClickFubenTime = System.DateTime.Now;

                var _id = DataModel.SelectDungeon.InfoData.Id;
                var _msg = NetManager.Instance.TeamEnterFuben(_id, -1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_QueueUpData.QueueId = -1;
                        UpdateQueueingMsg();
                        PlatformHelper.UMEvent("Fuben", "Enter", _id);
                    }
                    else if (DisposedMistakeCode(_msg.ErrorCode, _id, _msg.Response.Items))
                    {
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenID)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_QueueCountMax)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveQueue)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Unline)
                    {
                        //有队友不在线
                        var _e = new ShowUIHintBoard(448);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenCountNotEnough)
                    {
                        //{0}副本次数不够
                        var _charId = _msg.Response.Items[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(466);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                    {
                        //{{0}道具不足
                        var _charId = _msg.Response.Items[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(467);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_LevelNoEnough)
                    {
                        //{{0}不符合副本条件
                        var _charId = _msg.Response.Items[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(468);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterNoTeam
                             || _msg.ErrorCode == (int)ErrorCodes.Error_CharacterOutLine
                             || _msg.ErrorCode == (int)ErrorCodes.Error_TeamNotSame
                             || _msg.ErrorCode == (int)ErrorCodes.Error_TeamNotFind)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....EnterTeamDungeonCoroutine.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....EnterTeamDungeonCoroutine.......{0}.", _msg.State);
                }
            }
        }

        private IEnumerator GoOutCachotCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ExitDungeon(-1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _id = DataModel.SelectDungeon.InfoData.Id;
                        PlatformHelper.UMEvent("Fuben", "Exit", _id.ToString());
                    }
                    else
                    {
                        Logger.Error(".....ExitDungeon.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....ExitDungeon.......{0}.", _msg.State);
                }
            }
        }

        private int AcquireQueneingCachotID()
        {
            if (m_QueueUpData.QueueId != -1)
            {
                var _tbQuene = Table.GetQueue(m_QueueUpData.QueueId);
                if (_tbQuene != null)
                {
                    if (_tbQuene.AppType == 0)
                    {
                        return _tbQuene.Param;
                    }
                }
            }
            return -1;
        }


        private void RefreshCurrentNum(int id, int cur, int max)
        {        
            if (id != DataModel.SelectDungeon.InfoData.Id)
                return;
            DataModel.CurCanSweep = max > cur;
            DataModel.CurSweepTimes = string.Format(GameUtils.GetDictionaryText(100001160), max - cur);
        }


        private void UpdateUIStated()
        {
            var queueId = PlayerDataManager.Instance.PlayerDataModel.QueueUpData.QueueId;
            if (queueId == -1)
            {
                DataModel.IsQueue = false;
                UpdateQueueingMsg();
            }
        }

        private void UpdateCachotStated()
        {
            var _sceneId = SceneManager.Instance.CurrentSceneTypeId;
            var _selectInfo = DataModel.SelectDungeon.InfoData;

            var _dungeonId = _selectInfo.Id;

            var _tbScene = Table.GetScene(_sceneId);
            if (_tbScene != null)
            {
                if (_tbScene.FubenId == _dungeonId)
                {
                    _selectInfo.State = 2;
                    return;
                }
            }
            _selectInfo.State = 0;
            //         var lineupDungeon = GetQueneDungeonId();
            //         {
            //             // foreach(var group in DataModel.MainInfos)
            //             var __enumerator26 = (DataModel.MainInfos).GetEnumerator();
            //             while (__enumerator26.MoveNext())
            //             {
            //                 var group = __enumerator26.Current;
            //                 {
            //                     var state = 0;
            //                     for (int i = 0; i < 3; i++)
            //                     {
            //                         var info = group.Infos[i];
            //                         var tbDungeon = Table.GetFuben(info.Id);
            //                         if (tbDungeon == null)
            //                         {
            //                             continue;
            //                         }
            //                         if (tbDungeon.SceneId == sceneId)
            //                         {
            //                             info.State = 1;
            //                             state = 1;
            //                         }
            //                         else
            //                         {
            //                             info.State = 0;
            //                         }
            //                     }
            //                     group.State = state;
            //                 }
            //             }
            //         }
            //         {
            //             // foreach(var group in DataModel.TeamInfos)
            //             var __enumerator27 = (DataModel.TeamInfos).GetEnumerator();
            //             while (__enumerator27.MoveNext())
            //             {
            //                 var group = __enumerator27.Current;
            //                 {
            //                     bool isFight = false;
            //                     bool isRank = false;
            //                     for (int i = 0; i < 3; i++)
            //                     {
            //                         var info = group.Infos[i];
            //                         var tbDungeon = Table.GetFuben(info.Id);
            //                         if (tbDungeon == null)
            //                         {
            //                             continue;
            //                         }
            // 
            //                         if (info.Id == lineupDungeon)
            //                         {
            //                             info.State = 2;
            //                             isRank = true;
            //                             continue;
            //                         }
            //                         if (tbDungeon.SceneId == sceneId)
            //                         {
            //                             info.State = 1;
            //                             isFight = true;
            //                             continue;
            //                         }
            //                         info.State = 0;
            //                     }
            // 
            //                     if (isRank && isFight)
            //                     {
            //                         group.State = 3;
            //                     }
            //                     else if (isFight)
            //                     {
            //                         group.State = 1;
            //                     }
            //                     else if (isRank)
            //                     {
            //                         group.State = 2;
            //                     }
            //                     else
            //                     {
            //                         group.State = 0;
            //                     }
            //                 }
            //             }
            //         }
        }

        private void UpdateQueueingMsg()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _dungeonId = _data.Id;
            var _tbDungeon = Table.GetFuben(_dungeonId);
            if (_tbDungeon == null)
            {
                return;
            }
            if (_tbDungeon.QueueParam != -1 && _tbDungeon.QueueParam == m_QueueUpData.QueueId)
            {
                DataModel.IsQueue = true;
            }
            else
            {
                DataModel.IsQueue = false;
            }

            UpdateCachotStated();
        }

        private IEnumerator ReplacementCachotCoroutine(int dungeonId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ResetFuben(dungeonId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _e = new ShowUIHintBoard(436);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        DataModel.ResetCount++;
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenResetCountNotEnough
                             || _msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....ResetMainDungeonCoroutine.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....ResetMainDungeonCoroutine.......{0}.", _msg.State);
                }
            }
        }

        private void ReplacementCoreCachot()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            var _tbItemBase = Table.GetItemBase(_tbDungeon.ResetItemId);

            if (DataModel.SelectDungeon.InfoData.IsLock)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(532));
                return;
            }

            var _tbVip = PlayerDataManager.Instance.TbVip;
            if (_tbDungeon.TodayBuyCount + _tbVip.PlotFubenResetCount <= _data.ResetCount)
            {
                var _oldCount = _tbVip.PlotFubenResetCount;
                do
                {
                    _tbVip = Table.GetVIP(_tbVip.Id + 1);
                } while (_tbVip != null && _oldCount >= _tbVip.PlotFubenResetCount);

                if (_tbVip == null)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(437));
                }
                else
                {
                    GameUtils.GuideToBuyVip(_tbVip.Id);
                }
                return;
            }


            //重置一次副本次数需要消耗{0}×{1}，是否继续?
            var _content = string.Format(GameUtils.GetDictionaryText(463), _tbItemBase.Name, _tbDungeon.ResetItemCount);
            var _call = new Action(() =>
            {
                PlayerDataManager.Instance.NoticeData.DugeonNotMessage = true;
                ReplacementCoreCachotOkay(_tbDungeon.ResetItemId, _tbDungeon.ResetItemCount);
            });
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _content, "", _call);
        }

        private void ReplacementCoreCachotOkay(int item, int needCount)
        {
            var _count = PlayerDataManager.Instance.GetItemCount(item);
            if (_count < needCount)
            {
                GameUtils.ShowHintTip(210102);
                var e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                EventDispatcher.Instance.DispatchEvent(e);
                return;
            }

            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            NetManager.Instance.StartCoroutine(ReplacementCachotCoroutine(_id));
        }

        private void ReplacementTeamCachot()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            if (_tbDungeon.TodayBuyCount <= _data.ResetCount)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(437));
                return;
            }
            var _count = PlayerDataManager.Instance.GetItemCount(_tbDungeon.ResetItemId);
            if (_count < _tbDungeon.ResetItemCount)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(437));
                return;
            }
            var _tbItemBase = Table.GetItemBase(_tbDungeon.ResetItemId);
            //重置一次副本次数需要消耗{0}×{1}，是否继续?
            var _content = string.Format(GameUtils.GetDictionaryText(463), _tbItemBase.Name, _tbDungeon.ResetItemCount);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _content, "", ReplacementTeamCachotOkay);
        }

        private void ReplacementTeamCachotOkay()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;

            NetManager.Instance.StartCoroutine(ReplacementCachotCoroutine(_id));
        }

        //--------------------------------------------------------------------Main-----------------
        private void ChooseCoreCachotTeam(int index, int subIndex = 0)
        {
            var _data = DataModel.MainInfos[index];
            var _count = DataModel.MainInfos.Count;
            for (var j = 0; j < _count; j++)
            {
                var _info = DataModel.MainInfos[j];
                if (j == index)
                {
                    _info.IsSelect = true;
                }
                else
                {
                    if (_info.IsSelect)
                    {
                        _info.IsSelect = false;
                    }
                }
            }

            DataModel.SelectDungeon.GroupData = _data;
            DataModel.SelectDungeon.GroupData.CurrentSelect = index;
            var _playerData = PlayerDataManager.Instance;
            {
                var _enumerator7 = (_data.Infos).GetEnumerator();
                while (_enumerator7.MoveNext())
                {
                    var _t = _enumerator7.Current;
                    {
                        var _id = _t.Id;
                        if (_id != -1)
                        {
                            var tbDungeon = Table.GetFuben(_id);
                            _t.IsLock = _playerData.CheckCondition(tbDungeon.EnterConditionId) != 0;
                        }
                    }
                }
            }
            ChooseCoreCachotMsg(subIndex);
            var _e1 = new DungeonNetRetCallBack(11);
            EventDispatcher.Instance.DispatchEvent(_e1);
        }

        private void ChooseCoreCachotMsg(int i)
        {
            var _data = DataModel.SelectDungeon.GroupData;
            DataModel.SelectDungeon.InfoData = _data.Infos[i];
            DataModel.SelectDungeon.Index = i;
            DataModel.SelectDungeon.IsDiffictLevelShow = 1;

            var _id = _data.Infos[i].Id;
            var _tbDungeon = Table.GetFuben(_id);
            if (_tbDungeon == null)
            {
                return;
            }
            var _time = PlayerDataManager.Instance.GetExData(_tbDungeon.TimeExdata);
            if (_time <= 0)
            {
                DataModel.MainTime = GameUtils.GetDictionaryText(460);
            }
            else
            {
                DataModel.MainTime = GameUtils.TimeStringMS(_time / 60, _time % 60);
            }
            var _tbVip = PlayerDataManager.Instance.TbVip;
            DataModel.ResetCount = DataModel.SelectDungeon.GroupData.Infos[i].ResetCount;
            DataModel.ResetMaxCount = _tbDungeon.TodayBuyCount + _tbVip.PlotFubenResetCount;
            _data.NeedLevel = Table.GetPlotFuben(_data.Id).OpenLevel[i];
            DataModel.ScanCount = PlayerDataManager.Instance.GetItemCount(DataModel.ScanItemId);
            DataModel.ScanGrey = MoppingUpCriteria() ? 0 : 1;
            UpdateCachotStated();
        }

        //----------------------------------------------------------------------Team-----------------
        private void ChooseTeamCachotGroup(int index, int subIndex = 2)
        {
            var _data = DataModel.TeamInfos[index];

            var _count = DataModel.TeamInfos.Count;
            for (var j = 0; j < _count; j++)
            {
                var _info = DataModel.TeamInfos[j];
                if (j == index)
                {
                    _info.IsSelect = true;
                }
                else
                {
                    if (_info.IsSelect)
                    {
                        _info.IsSelect = false;
                    }
                }
            }

            DataModel.SelectDungeon.GroupData = _data;
            var _playerData = PlayerDataManager.Instance;
            {
                var _enumerator8 = (_data.Infos).GetEnumerator();
                while (_enumerator8.MoveNext())
                {
                    var _t = _enumerator8.Current;
                    {
                        var _id = _t.Id;
                        if (_id != -1)
                        {
                            var _tbDungeon = Table.GetFuben(_id);
                            _t.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                        }
                    }
                }
            }
            ChooseTeamCachotMsg(subIndex);
            var _e1 = new DungeonNetRetCallBack(12);
            EventDispatcher.Instance.DispatchEvent(_e1);
        }

        private void ChooseTeamCachotMsg(int i)
        {
            i = 2;
            if (DataModel == null) return;
            var _data = DataModel.SelectDungeon.GroupData;
            if (_data == null || _data.Infos.Count<3) return;
            DataModel.SelectDungeon.InfoData = _data.Infos[i];
            DataModel.SelectDungeon.Index = i;
            DataModel.SelectDungeon.IsDiffictLevelShow = 0;
            _data.NeedLevel = Table.GetPlotFuben(_data.Id).OpenLevel[i];

            UpdateQueueingMsg();                        
            var isEnough = PlayerDataManager.Instance.GetLevel() >= _data.NeedLevel;
            if (isEnough)
            {                
                TeamModule.isShowTeam = IsLeader();
                TeamModule.teamState = IsHavaTeam();
                copyId = _data.Infos[i].Id;
            }
            else
            {
                TeamModule.isShowTeam = isEnough;
                TeamModule.teamState = 3;//0 无队伍  1 有队伍不是队长 2 是队长  3 全部隐藏
            }
        }

        //----------------------------------------------------------------------VIP------------------
        private void ChooseVipCachotGroup(int index, int subIndex = 0)
        {
            var _data = DataModel.VipInfos[index];
            var _count = DataModel.VipInfos.Count;
            for (var j = 0; j < _count; j++)
            {
                var _info = DataModel.VipInfos[j];
                if (j == index)
                {
                    _info.IsSelect = true;
                }
                else
                {
                    if (_info.IsSelect)
                    {
                        _info.IsSelect = false;
                    }
                }
            }

            DataModel.SelectDungeon.GroupData = _data;
            var _playerData = PlayerDataManager.Instance;
            {
                var _enumerator7 = (_data.Infos).GetEnumerator();
                while (_enumerator7.MoveNext())
                {
                    var _t = _enumerator7.Current;
                    {
                        var _id = _t.Id;
                        if (_id != -1)
                        {
                            var _tbDungeon = Table.GetFuben(_id);
                            _t.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                        }
                    }
                }
            }
            ChooseVipCachotMsg(subIndex);
            var _e1 = new DungeonNetRetCallBack(13);
            EventDispatcher.Instance.DispatchEvent(_e1);
        }

        private void ChooseVipCachotMsg(int i)
        {
            var _data = DataModel.SelectDungeon.GroupData;
            DataModel.SelectDungeon.InfoData = _data.Infos[i];
            DataModel.SelectDungeon.Index = i;
            DataModel.SelectDungeon.IsDiffictLevelShow = 0;

            var _id = _data.Infos[i].Id;
            var _tbDungeon = Table.GetFuben(_id);
            if (_tbDungeon == null)
            {
                return;
            }
            var _time = PlayerDataManager.Instance.GetExData(_tbDungeon.TimeExdata);
            if (_time <= 0)
            {
                DataModel.MainTime = GameUtils.GetDictionaryText(460);
            }
            else
            {
                DataModel.MainTime = GameUtils.TimeStringMS(_time / 60, _time % 60);
            }
            var _tbVip = PlayerDataManager.Instance.TbVip;
            DataModel.ResetCount = DataModel.SelectDungeon.GroupData.Infos[i].ResetCount;
            DataModel.ResetMaxCount = _tbDungeon.TodayBuyCount + _tbVip.PlotFubenResetCount;
            _data.NeedLevel = Table.GetPlotFuben(_data.Id).OpenLevel[i];
            DataModel.ScanCount = PlayerDataManager.Instance.GetItemCount(DataModel.ScanItemId);
            DataModel.ScanGrey = MoppingUpCriteria() ? 0 : 1;

            UpdateCachotStated();
        }



        private void RefreshFubenGoIntoNumTime()
        {
            foreach (var maingroup in DataModel.MainInfos)
            {
                maingroup.EnterCount = 0;
                maingroup.TotalCount = 0;
                foreach (var info in maingroup.Infos)
                {
                    maingroup.EnterCount += info.EnterCount;
                    maingroup.TotalCount += info.TotalCount;
                }

            }
            foreach (var teamgroup in DataModel.TeamInfos)
            {
                teamgroup.EnterCount = 0;
                teamgroup.TotalCount = 0;
                foreach (var info in teamgroup.Infos)
                {
                    teamgroup.EnterCount += info.EnterCount;
                    teamgroup.TotalCount += info.TotalCount;
                }
            }
            foreach (var vipgroup in DataModel.VipInfos)
            {
                vipgroup.EnterCount = 0;
                vipgroup.TotalCount = 0;
                foreach (var info in vipgroup.Infos)
                {
                    vipgroup.EnterCount += info.EnterCount;
                    vipgroup.TotalCount += info.TotalCount;
                }
            }
        }

        private bool MoppingUpCriteria()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            var _playerData = PlayerDataManager.Instance;

            var _dic = _playerData.CheckCondition(_tbDungeon.EnterConditionId);
            if (_dic != 0)
            {
                //不符合副本扫荡条件   270233
                return false;
            }
            if (_tbDungeon.TimeExdata == -1)
            {
                return false;
            }
            var _time = _playerData.GetExData(_tbDungeon.TimeExdata);
            if (_time == 0 || _time > _tbDungeon.SweepLimitMinutes * 60)
            {
                return false;
            }
            if (DataModel.ScanCount <= 0)
            {
                return false;
            }
            return true;
        }

        private void MoppingUpCoreCachot()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _id = _data.Id;
            var _tbDungeon = Table.GetFuben(_id);
            var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
            if (_sceneId == _tbDungeon.SceneId)
            {
                //已经在此副本当中了
                var _e = new ShowUIHintBoard(270081);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _playerData = PlayerDataManager.Instance;
            var _dic = _playerData.CheckCondition(_tbDungeon.EnterConditionId);
            if (_dic != 0)
            {
                //不符合副本扫荡条件   270233
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dic));
                return;
            }
            if (_tbDungeon.TimeExdata == -1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(464));
                return;
            }
            var _time = _playerData.GetExData(_tbDungeon.TimeExdata);
            if (_time == 0 || _time > _tbDungeon.SweepLimitMinutes * 60)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(435));
                return;
            }
            if (_data.EnterCount >= _data.TotalCount)
            {
                if (_data.ResetCount < _tbDungeon.TodayBuyCount)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(434));
                }
                else
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(597));
                }
                return;
            }
            if (_data.State == 1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270220));
                return;
            }

            var _tbDungeonNeedItemIdLength0 = _tbDungeon.NeedItemId.Count;
            for (var i = 0; i < _tbDungeonNeedItemIdLength0; i++)
            {
                if (_tbDungeon.NeedItemId[i] != -1)
                {
                    if (_playerData.GetItemCount(_tbDungeon.NeedItemId[i]) < _tbDungeon.NeedItemCount[i])
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                        return;
                    }
                }
            }
            if (_playerData.GetItemCount(GameUtils.SweepCouponId) < 1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005024));
                PlayerDataManager.Instance.ShowItemInfoGet(GameUtils.SweepCouponId);
                return;
            }
            NetManager.Instance.StartCoroutine(MoppingUpCoreCachotCoroutine());
        }

        private IEnumerator MoppingUpCoreCachotCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _data = DataModel.SelectDungeon.InfoData;
                var _id = _data.Id;
                var _msg = NetManager.Instance.SweepFuben(_id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //                     if (msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                        //                     {
                        //                         var e1 = new ShowUIHintBoard(445);
                        //                         EventDispatcher.Instance.DispatchEvent(e1);    
                        //                     }
                        DataModel.ScanCount -= 1;
                        DataModel.ScanGrey = MoppingUpCriteria() ? 0 : 1;
                        DataModel.SweepData.ItemInfos.Clear();
                        DataModel.SweepData.Gold = 0;
                        DataModel.SweepData.Exp = 0;
                        {
                            var _list13 = _msg.Response.Items;
                            var _listCount13 = _list13.Count;
                            for (var _i13 = 0; _i13 < _listCount13; ++_i13)
                            {
                                var _i = _list13[_i13];
                                {
                                    if (_i.ItemId == -1)
                                    {
                                        continue;
                                    }
                                    if (_i.ItemId == 2)
                                    {
                                        DataModel.SweepData.Gold = _i.Count;
                                    }
                                    else if (_i.ItemId == 1)
                                    {
                                        DataModel.SweepData.Exp = _i.Count;
                                    }
                                    else
                                    {
                                        var _idData = new BagItemDataModel
                                        {
                                            ItemId = _i.ItemId,
                                            Count = _i.Count
                                        };
                                        _idData.Exdata.InstallData(_i.Exdata);
                                        DataModel.SweepData.ItemInfos.Add(_idData);
                                    }
                                }
                            }
                        }
                        var _awardModel = new BagItemDataModel();
                        m_iDrawId = _msg.Response.DrawId;
                        m_iDrawIndex = _msg.Response.SelectIndex;
                        DataModel.SweepData.AwardItems[m_iDrawIndex].ItemId = _msg.Response.DrawItem.ItemId;
                        DataModel.SweepData.AwardItems[m_iDrawIndex].Count = _msg.Response.DrawItem.Count;
                        DataModel.SweepData.AwardItems[m_iDrawIndex].Exdata.InstallData(_msg.Response.DrawItem.Exdata);
                        DataModel.IsShowScan = 1;
                        var e = new DungeonNetRetCallBack(10);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenID
                             || _msg.ErrorCode == (int)ErrorCodes.Error_FubenNoPass
                             || _msg.ErrorCode == (int)ErrorCodes.Error_PassFubenTimeNotEnough
                             || _msg.ErrorCode == (int)ErrorCodes.Error_LevelNoEnough
                             || _msg.ErrorCode == (int)ErrorCodes.ItemNotEnough
                             || _msg.ErrorCode == (int)ErrorCodes.Error_FubenCountNotEnough
                             || _msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....ResetMainDungeonCoroutine.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....ResetMainDungeonCoroutine.......{0}.", _msg.State);
                }
            }
        }

        private void TeamsRecruit()
        {
            IControllerBase _teamController = UIManager.Instance.GetController(UIConfig.TeamFrame);
            if (_teamController != null)
            {

                var _teamData = _teamController.GetDataModel("") as TeamDataModel;
                if (_teamData != null)
                {
                    if (_teamData.TeamId <= 0)
                        EventDispatcher.Instance.DispatchEvent(new Event_TeamCreate());
                    else if (_teamController.CallFromOtherClass("IsLeader", null) == (object)false)
                    {
                        GameUtils.ShowNetErrorHint((int)ErrorCodes.Error_CharacterNotLeader);
                        return;
                    }
                    EventDispatcher.Instance.DispatchEvent(new ChatTeamEvent(DataModel.SelectDungeon.InfoData.Id,
                        _teamData.TeamId));
                }
            }
        }

        private void TeamsRecr()
        {
            IControllerBase _teamController = UIManager.Instance.GetController(UIConfig.TeamFrame);
            if (_teamController != null)
            {

                var _teamData = _teamController.GetDataModel("") as TeamDataModel;
                if (_teamData != null)
                {
                    if (_teamData.TeamId <= 0)
                        EventDispatcher.Instance.DispatchEvent(new Event_TeamCreate());
                    else if (_teamController.CallFromOtherClass("IsLeader",null) == (object)false)
                    {
                        GameUtils.ShowNetErrorHint((int)ErrorCodes.Error_CharacterNotLeader);
                        return;
                    }
                }
            }
        }
        private void TeamGetSerchList()
        {
            var e = new Show_UI_Event(UIConfig.TeamFrame);
            EventDispatcher.Instance.DispatchEvent(e);

            //var e1 = new UIEvent_TeamFrame_NearTeam();
            //EventDispatcher.Instance.DispatchEvent(e1);

            //var e1 = new TeamSearchList_Event(1, DataModel.SelectDungeon.InfoData.Id);
            //EventDispatcher.Instance.DispatchEvent(e1);
            PlayerDataManager.Instance.NoticeData.DungeonType = 1;
            var e1 = new TeamTargetChangeItemByOther_Event(1, DataModel.SelectDungeon.InfoData.Id);
            EventDispatcher.Instance.DispatchEvent(e1);

            //EventDispatcher.Instance.DispatchEvent(new OpenTeamFromOtherEvent(1));
        }
        private void TeamsCachotBattleArrary()
        {
            var _data = DataModel.SelectDungeon.InfoData;
            var _dungeonId = _data.Id;
            if (DataModel.IsQueue)
            {
                NetManager.Instance.StartCoroutine(TeamCachotLineupsAbolishCoroutine());
            }
            else
            {
                if (PlayerDataManager.Instance.IsInPvPScnen())
                {
                    GameUtils.ShowHintTip(456);
                    return;
                }

                var _tbDungeon = Table.GetFuben(_dungeonId);
                var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
                if (_sceneId == _tbDungeon.SceneId)
                {
                    ////已经在此副本当中了
                    var _e = new ShowUIHintBoard(270081);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
                var _playerData = PlayerDataManager.Instance;
                var _dicCom = _playerData.CheckCondition(_tbDungeon.EnterConditionId);
                if (_dicCom != 0)
                {
                    //不符合副本进入条件 270234
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dicCom));
                    return;
                }
                if (_data.EnterCount == _data.TotalCount)
                {
                    if (_data.ResetCount < _tbDungeon.TodayBuyCount)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(434));
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(438));
                    }
                    return;
                }

                var _dic = PlayerDataManager.Instance.CheckCondition(_tbDungeon.EnterConditionId);
                if (_dic != 0)
                {
                    //不符合副本扫荡条件
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dic));
                    return;
                }

                //if (tbDungeon.FightPoint > PlayerDataManager.Instance.PlayerDataModel.Attributes.FightValue)
                //{
                //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(444));
                //    return;
                //}

                var _teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;
                var _teamCount = _teamData.TeamList.Count;
                var _memberCount = 0;
                for (var i = 0; i < _teamCount; i++)
                {
                    if (_teamData.TeamList[i].Guid != 0ul)
                    {
                        _memberCount++;
                    }
                }
                if (_memberCount > 0)
                {
                    var _tbQuenu = Table.GetQueue(_tbDungeon.QueueParam);
                    if (_memberCount > _tbQuenu.CountLimit)
                    {
                        //var e = new ShowUIHintBoard("队伍人数超出副本所需人数上限");
                        var _e = new ShowUIHintBoard(465);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        return;
                    }
                }

                //如果在排其它的队
                if (m_QueueUpData.QueueId != -1)
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 41004, "", () =>
                    {
                        //EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                        NetManager.Instance.StartCoroutine(TeamCachotLineupsCoroutine());
                    });
                    return;
                }

                //NetManager.Instance.StartCoroutine(TeamDungeonLineupCoroutine());
                NoticedGoIntoCachotWar(_tbDungeon.FightPoint,
                    () =>
                    {
                        NetManager.Instance.StartCoroutine(TeamCachotLineupsCoroutine());
                    }
                    );
            }
        }

        private IEnumerator TeamCachotLineupsAbolishCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _data = DataModel.SelectDungeon.InfoData;
                var _dungeonId = _data.Id;
                var _tbDungeon = Table.GetFuben(_dungeonId);
                var _msg = NetManager.Instance.MatchingCancel(_tbDungeon.QueueParam);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DataModel.QueueId = -1;
                        m_QueueUpData.QueueId = -1;
                        UpdateQueueingMsg();
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_WindowShowDungeonQueue(Game.Instance.ServerTime,
                            -1));
                        EventDispatcher.Instance.DispatchEvent(new QueueCanceledEvent());
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_QueueCountMax)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveQueue)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....MatchingCancel.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....MatchingCancel.......{0}.", _msg.State);
                }
            }
        }

        private IEnumerator TeamCachotLineupsCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _data = DataModel.SelectDungeon.InfoData;
                var _dungeonId = _data.Id;
                var _tbDungeon = Table.GetFuben(_dungeonId);

                var _msg = NetManager.Instance.MatchingStart(_tbDungeon.QueueParam);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.InitQueneData(_msg.Response.Info);
                        UpdateQueueingMsg();
                        DataModel.QueueId = _data.Id;
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenID)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_QueueCountMax)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveQueue)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Unline)
                    {
                        //有队友不在线
                        var _e = new ShowUIHintBoard(448);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenCountNotEnough)
                    {
                        //{0}副本次数不够
                        var _charId = _msg.Response.CharacterId[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(466);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                    {
                        //{{0}道具不足
                        var _charId = _msg.Response.CharacterId[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(467);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_LevelNoEnough)
                    {
                        //{{0}不符合副本条件
                        var _charId = _msg.Response.CharacterId[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(100001474);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AlreadyInThisDungeon)
                    {
                        //{0}有队员在副本中
                        var _charId = _msg.Response.CharacterId[0];
                        var _name = PlayerDataManager.Instance.GetTeamMemberName(_charId);
                        if (!string.IsNullOrEmpty(_name))
                        {
                            var _str = GameUtils.GetDictionaryText(493);
                            _str = string.Format(_str, _name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenID
                             || _msg.ErrorCode == (int)ErrorCodes.Error_FubenNotInOpenTime)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....MatchingStart.......{0}........", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Warn(".....MatchingStart.......{0}.", _msg.State);
                }
            }
        }


        #endregion

        #region 事件函数

        private void OnClickButtonEvent(IEvent ievent)
        {
            var _e = ievent as DungeonBtnClick;
            switch (_e.Type)
            {
                case eDungeonType.Main:
                {
                    switch (_e.Index)
                    {
                        case 1:
                        {
                            GoIntoCoreCachot();
                        }
                            break;
                        case 2:
                        {
                            ReplacementCoreCachot();
                        }
                            break;
                        case 3:
                        {
                            MoppingUpCoreCachot();
                        }
                            break;
                    }
                }
                    break;
                case eDungeonType.Team:
                {
                    switch (_e.Index)
                    {
                        case 1:
                        {
                            GoIntoTeamCachot();
                        }
                            break;
                        case 2:
                        {
                            TeamsCachotBattleArrary();
                        }
                            break;
                        case 3:
                        {
                            ReplacementTeamCachot();
                        }
                            break;
                        case 4:
                        {
                            NetManager.Instance.StartCoroutine(MakeSureGoTeamCachotCoroutine(_e.ExData, 1));
                        }
                            break;
                        case 5:
                        {
                            NetManager.Instance.StartCoroutine(MakeSureGoTeamCachotCoroutine(_e.ExData, 0));
                        }
                            break;
                        case 6:
                        {//招募队友
                            TeamsRecruit();
                        }
                            break;
                        case 7:
                            TeamsRecr();
                            break;
                        case 8:
                            TeamGetSerchList();
                            break;
                    }
                }
                    break;
                case eDungeonType.Vip:
                {
                    if (_e.Index == 1)
                    {
                        GoIntoVipCachot();
                    }
                }
                    break;

                case eDungeonType.Invalid:
                {
                    switch (_e.Index)
                    {
                        case 100:
                        {
                            var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
                            var _tbScene = Table.GetScene(_sceneId);
                            var _tbFuben = Table.GetFuben(_tbScene.FubenId);
                            if (_tbFuben == null)
                            {
                                return;
                            }
                            switch (_tbFuben.AssistType)
                            {
                                case 4:
                                case 5:
                                {
                                    if (PlayerDataManager.Instance.PlayerDataModel.DungeonState == (int)eDungeonState.Start)
                                    {
                                        //活动未完成，此时退出只能获得极少数参与奖励！
                                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 220444, "",
                                            () => { NetManager.Instance.StartCoroutine(GoOutCachotCoroutine()); });
                                        return;
                                    }
                                }
                                    break;
                                case 9:
                                case 10:
                                case 11:
                                case 12:
                                case 13:
                                case 15:
                                case 16:
                                case 17:
                                case 18:
                                case 19:
                                {
                                    //是否退出战场
                                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                                        GameUtils.GetDictionaryText(270080), "",
                                        () => { NetManager.Instance.StartCoroutine(GoOutCachotCoroutine()); });
                                }
                                    return;
                            }                                
                            if (DungeonCompleteType != eDungeonCompleteType.Success)
                            {
                                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 220504, "",
                                    () => { NetManager.Instance.StartCoroutine(GoOutCachotCoroutine()); });
                            }
                            else
                            {
                                NetManager.Instance.StartCoroutine(GoOutCachotCoroutine());
                                DungeonCompleteType = eDungeonCompleteType.Failed;
                            }
                        }
                            break;
                    }
                }
                    break;
            }
        }

        private void OnClickCachotOrganizationCellEvent(IEvent ievent)
        {
            var _e = ievent as DungeonGroupCellClick2;
            var _dungeonType = eDungeonType.Main;
            if (_e.Type == 1)
            {
                DataModel.ToggleSelect = 1;
                _dungeonType = eDungeonType.Team;
            }
            else if (_e.Type == 2)
            {
                DataModel.ToggleSelect = 2;
                _dungeonType = eDungeonType.Vip;
            }
            else
            {
                DataModel.ToggleSelect = 0;
                _dungeonType = eDungeonType.Main;
            }
            DataModel.IsShowScan = 0;
            switch (_dungeonType)
            {
                case eDungeonType.Main:
                {
                    ChooseCoreCachotTeam(_e.Index);
                }
                    break;
                case eDungeonType.Exp:
                    break;
                case eDungeonType.Gold:
                    break;
                case eDungeonType.Team:
                {
                    ChooseTeamCachotGroup(_e.Index);
                }
                    break;
                case eDungeonType.Vip:
                {
                    ChooseVipCachotGroup(_e.Index);
                }
                    break;
                default:
                    break;
            }
        }

        private void OnClickChooseCoreCachotEvent(IEvent ievent)
        {
            var _e = ievent as DungeonInfosMainInfo;
            switch (_e.Type)
            {
                case eDungeonType.Main:
                {
                    ChooseCoreCachotMsg(_e.Index);
                }
                    break;
                case eDungeonType.Exp:
                    break;
                case eDungeonType.Team:
                {
                    ChooseTeamCachotMsg(_e.Index);
                }
                    break;
                case eDungeonType.Gold:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnCachotGoInNumRenewalEvent(IEvent ievent)
        {
            var _e = ievent as DungeonEnterCountUpdate;
            {
                // foreach(var info in DataModel.MainInfos)
                var _enumerator1 = (DataModel.MainInfos).GetEnumerator();
                while (_enumerator1.MoveNext())
                {
                    var _info = _enumerator1.Current;
                    {
                        {
                            // foreach(var i in info.Infos)
                            var _enumerator15 = (_info.Infos).GetEnumerator();
                            while (_enumerator15.MoveNext())
                            {
                                var _i = _enumerator15.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1 && _e.DungeonId == _id)
                                    {
                                        var _tbDungeon = Table.GetFuben(_id);

                                        _i.ResetCount = PlayerDataManager.Instance.GetExData(_tbDungeon.ResetExdata);
                                        _i.EnterCount = PlayerDataManager.Instance.GetExData(_tbDungeon.TodayCountExdata);
                                        _i.TotalCount = _i.ResetCount + _tbDungeon.TodayCount;
                                        _i.CompleteCount = _i.EnterCount.ToString();
                                        RefreshCurrentNum(_id, _i.EnterCount, _i.TotalCount);
                                        if (_i.TotalCount > 0)
                                        {
                                            _i.CompleteCount += "/" + _i.TotalCount;
                                        }
                                        AnalyticalNotices();
                                        RefreshFubenGoIntoNumTime();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            {
                // foreach(var info in DataModel.TeamInfos)
                var _enumerator2 = (DataModel.TeamInfos).GetEnumerator();
                while (_enumerator2.MoveNext())
                {
                    var _info = _enumerator2.Current;
                    {
                        {
                            // foreach(var i in info.Infos)
                            var _enumerator16 = (_info.Infos).GetEnumerator();
                            while (_enumerator16.MoveNext())
                            {
                                var _i = _enumerator16.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1 && _e.DungeonId == _id)
                                    {
                                        var tbFuben = Table.GetFuben(_id);

                                        _i.ResetCount = PlayerDataManager.Instance.GetExData(tbFuben.ResetExdata);
                                        _i.EnterCount = PlayerDataManager.Instance.GetExData(tbFuben.TodayCountExdata);
                                        _i.TotalCount = _i.ResetCount + tbFuben.TodayCount;
                                        RefreshCurrentNum(_id, _i.EnterCount, _i.TotalCount);
                                        _i.CompleteCount = _i.EnterCount.ToString();
                                        if (_i.TotalCount > 0)
                                        {
                                            _i.CompleteCount += "/" + _i.TotalCount;
                                        }
                                        if (_i.IsDynamicReward)
                                        {
                                            for (int j = 0, jmax = tbFuben.DisplayCount.Count; j < jmax; ++j)
                                            {
                                                var _itemId = tbFuben.DisplayReward[j];
                                                var _skillUpgradeId = tbFuben.DisplayCount[j];
                                                if (_itemId == -1)
                                                {
                                                    break;
                                                }
                                                var _itemCount =
                                                    Table.GetSkillUpgrading(_skillUpgradeId)
                                                        .GetSkillUpgradingValue(_i.EnterCount + 1);
                                                _i.RewardCount[j] = _itemCount;
                                            }
                                        }
                                        AnalyticalNotices();
                                        RefreshFubenGoIntoNumTime();
                                        //return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // vip
            foreach (var varInfo in DataModel.VipInfos)
            {
                foreach (var iValue in varInfo.Infos)
                {
                    var _i = iValue;
                    {
                        var _id = _i.Id;
                        if (_id != -1 && _e.DungeonId == _id)
                        {
                            var _tbDungeon = Table.GetFuben(_id);

                            _i.ResetCount = PlayerDataManager.Instance.GetExData(_tbDungeon.ResetExdata);
                            _i.EnterCount = PlayerDataManager.Instance.GetExData(_tbDungeon.TodayCountExdata);
                            _i.TotalCount = _i.ResetCount + _tbDungeon.TodayCount;
                            RefreshCurrentNum(_id, _i.EnterCount, _i.TotalCount);
                            _i.CompleteCount = _i.EnterCount.ToString();
                            if (_i.TotalCount > 0)
                            {
                                _i.CompleteCount += "/" + _i.TotalCount;
                            }
                            AnalyticalNotices();
                            RefreshFubenGoIntoNumTime();
                            return;
                        }
                    }
                }
            }
        }
        private void OnCachotToClearNumRenewalEvent(IEvent ievent)
        {
            var _e = ievent as DungeonResetCountUpdate;
            {
                // foreach(var info in DataModel.MainInfos)
                var _enumerator3 = (DataModel.MainInfos).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _info = _enumerator3.Current;
                    {
                        {
                            // foreach(var i in info.Infos)
                            var _enumerator17 = (_info.Infos).GetEnumerator();
                            while (_enumerator17.MoveNext())
                            {
                                var _i = _enumerator17.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1 && _e.DungeonId == _id)
                                    {
                                        var _tbDungeon = Table.GetFuben(_id);

                                        _i.ResetCount = PlayerDataManager.Instance.GetExData(_tbDungeon.ResetExdata);
                                        _i.EnterCount = PlayerDataManager.Instance.GetExData(_tbDungeon.TodayCountExdata);
                                        _i.TotalCount = _i.ResetCount + _tbDungeon.TodayCount;
                                        RefreshCurrentNum(_id, _i.EnterCount, _i.TotalCount);
                                        _i.CompleteCount = _i.EnterCount.ToString();
                                        if (_i.TotalCount > 0)
                                        {
                                            _i.CompleteCount += "/" + _i.TotalCount;
                                        }
                                        AnalyticalNotices();
                                        RefreshFubenGoIntoNumTime();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            {
                // foreach(var info in DataModel.TeamInfos)
                var _enumerator4 = (DataModel.TeamInfos).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _info = _enumerator4.Current;
                    {
                        {
                            // foreach(var i in info.Infos)
                            var _enumerator18 = (_info.Infos).GetEnumerator();
                            while (_enumerator18.MoveNext())
                            {
                                var _i = _enumerator18.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1 && _e.DungeonId == _id)
                                    {
                                        var _tbDungeon = Table.GetFuben(_id);

                                        _i.ResetCount = PlayerDataManager.Instance.GetExData(_tbDungeon.ResetExdata);
                                        _i.EnterCount = PlayerDataManager.Instance.GetExData(_tbDungeon.TodayCountExdata);
                                        _i.TotalCount = _i.ResetCount + _tbDungeon.TodayCount;
                                        RefreshCurrentNum(_id, _i.EnterCount, _i.TotalCount);
                                        _i.CompleteCount = _i.EnterCount.ToString();
                                        if (_i.TotalCount > 0)
                                        {
                                            _i.CompleteCount += "/" + _i.TotalCount;
                                        }
                                        AnalyticalNotices();
                                        RefreshFubenGoIntoNumTime();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnCachotCleanOffRandomPrizeEvent(IEvent ievent)
        {
            var _e = ievent as DungeonSweepRandAward;
            var _choose = _e.Index;
            var _sweepData = DataModel.SweepData;
            var _tbDraw = Table.GetDraw(m_iDrawId);
            _sweepData.AwardItems[_choose].ItemId = _tbDraw.DropItem[m_iDrawIndex];
            _sweepData.AwardItems[_choose].Count = _tbDraw.Count[m_iDrawIndex];

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
                _sweepData.AwardItems[_flag1].ItemId = _itemId;
                _sweepData.AwardItems[_flag1].Count = _tbDraw.Count[_flag2];
                var _tbItem = Table.GetItemBase(_itemId);
                if (_tbItem.Type >= 10000 && _tbItem.Type <= 10099)
                {
                    GameUtils.EquipRandomAttribute(_sweepData.AwardItems[_flag1]);
                }
                _flag1++;
                _flag2++;
            }
        }

        private void OnInitionExDataEvent(IEvent ievent)
        {
            var _playerData = PlayerDataManager.Instance;
            if (DataModel.MainInfos.Count == 0)
            {
                Table.ForeachPlotFuben(record =>
                {
                    if (record.FubenType != (int)eDungeonType.Main)
                    {
                        return true;
                    }
                    var _mddata = new DungeonGroupDataModel();
                    _mddata.Id = record.Id;
                    var _fubenTable = Table.GetFuben(record.Difficulty[0]);
                    _mddata.IconId = _fubenTable.FaceIcon;
                    for (var i = 0; i < 3; i++)
                    {
                        var _info = _mddata.Infos[i];
                        _info.Id = record.Difficulty[i];

                        var _tbFuben = Table.GetFuben(_info.Id);
                        if (_tbFuben != null)
                        {
                            for (int j = 0, jmax = _tbFuben.DisplayCount.Count; j < jmax; ++j)
                            {
                                _info.RewardCount[j] = _tbFuben.DisplayCount[j];
                            }
                        }
                    }

                    DataModel.MainInfos.Add(_mddata);
                    return true;
                });
            }

            if (DataModel.TeamInfos.Count == 0)
            {
                Table.ForeachPlotFuben(record =>
                {
                    if (record.FubenType != (int)eDungeonType.Team)
                    {
                        return true;
                    }
                    var _mddata = new DungeonGroupDataModel();
                    _mddata.Id = record.Id;
                    for (var i = 0; i < 3; i++)
                    {
                        var _info = _mddata.Infos[i];
                        _info.Id = record.Difficulty[i];

                        var _tbFuben = Table.GetFuben(_info.Id);
                        if (_tbFuben != null)
                        {
                            _info.IsDynamicReward = _tbFuben.IsDyncReward == 1;
                            _mddata.IconId = _tbFuben.FaceIcon;
                            if (!_info.IsDynamicReward)
                            {
                                for (int j = 0, jmax = _tbFuben.DisplayCount.Count; j < jmax; ++j)
                                {
                                    _info.RewardCount[j] = _tbFuben.DisplayCount[j];
                                }

                            }
                        }

                    }
                    var _tbFuben2 = Table.GetFuben(record.Difficulty[0]);
                    if (_tbFuben2 != null)
                    {
                        var _tbQueue = Table.GetQueue(_tbFuben2.QueueParam);
                        if (_tbQueue != null)
                        {
                            _mddata.PlayerCount = _tbQueue.CountLimit;
                        }
                    }
                    _mddata.ShowCount = false;
                    DataModel.TeamInfos.Add(_mddata);
                    return true;
                });
            }

            // vip副本
            if (DataModel.VipInfos.Count == 0)
            {
                Table.ForeachPlotFuben(record =>
                {
                    if (record.FubenType != (int)eDungeonType.Vip)
                    {
                        return true;
                    }
                    var _mddata = new DungeonGroupDataModel();
                    _mddata.Id = record.Id;
                    for (var i = 0; i < 3; i++)
                    {
                        var _info = _mddata.Infos[i];
                        _info.Id = record.Difficulty[i];

                        var _tbFuben = Table.GetFuben(_info.Id);
                        if (_tbFuben != null)
                        {
                            for (int j = 0, jmax = _tbFuben.DisplayCount.Count; j < jmax; ++j)
                            {
                                _info.RewardCount[j] = _tbFuben.DisplayCount[j];
                            }
                            _mddata.IconId = _tbFuben.FaceIcon;
                        }
                    }
                    var _tbFuben2 = Table.GetFuben(record.Difficulty[0]);
                    if (_tbFuben2 != null)
                    {
                        var _tbCondition = Table.GetConditionTable(_tbFuben2.EnterConditionId);
                        if (_tbCondition.ItemId[0] == 15) //15号物品代表VIP等级
                        {
                            _mddata.NeedVipLevel = _tbCondition.ItemCountMin[0];
                        }
                    }

                    DataModel.VipInfos.Add(_mddata);
                    return true;
                });
            }




            {
                var _enumerator5 = (DataModel.MainInfos).GetEnumerator();
                while (_enumerator5.MoveNext())
                {
                    var _info = _enumerator5.Current;
                    {
                        {
                            var _enumerator19 = (_info.Infos).GetEnumerator();
                            while (_enumerator19.MoveNext())
                            {
                                var _i = _enumerator19.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1)
                                    {
                                        var _tbDungeon = Table.GetFuben(_id);
                                        if (_tbDungeon != null)
                                        {
                                            _i.EnterCount = _playerData.GetExData(_tbDungeon.TodayCountExdata);
                                            _i.ResetCount = _playerData.GetExData(_tbDungeon.ResetExdata);
                                            _i.TotalCount = _i.ResetCount + _tbDungeon.TodayCount;
                                            _i.CompleteCount = _i.EnterCount.ToString();
                                            if (_i.TotalCount > 0)
                                            {
                                                _i.CompleteCount += "/" + _i.TotalCount;
                                            }
                                            _i.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }


            {
                var _enumerator6 = (DataModel.TeamInfos).GetEnumerator();
                while (_enumerator6.MoveNext())
                {
                    var _info = _enumerator6.Current;
                    {
                        {
                            var _enumerator20 = (_info.Infos).GetEnumerator();
                            while (_enumerator20.MoveNext())
                            {
                                var _i = _enumerator20.Current;
                                {
                                    var _id = _i.Id;
                                    if (_id != -1)
                                    {
                                        var _tbFuben = Table.GetFuben(_id);
                                        if (_tbFuben == null) continue;
                                        _i.EnterCount = _playerData.GetExData(_tbFuben.TodayCountExdata);
                                        _i.ResetCount = _playerData.GetExData(_tbFuben.ResetExdata);
                                        _i.TotalCount = _i.ResetCount + _tbFuben.TodayCount;
                                        RefreshCurrentNum(_id, _i.EnterCount, _i.TotalCount);
                                        _i.CompleteCount = _i.EnterCount.ToString();
                                        if (_i.TotalCount > 0)
                                        {
                                            _i.CompleteCount += "/" + _i.TotalCount;
                                        }
                                        _i.IsLock = _playerData.CheckCondition(_tbFuben.EnterConditionId) != 0;
                                        if (_i.IsDynamicReward)
                                        {
                                            for (int j = 0, jmax = _tbFuben.DisplayCount.Count; j < jmax; ++j)
                                            {
                                                var _itemId = _tbFuben.DisplayReward[j];
                                                var _skillUpgradeId = _tbFuben.DisplayCount[j];
                                                if (_itemId == -1)
                                                {
                                                    break;
                                                }
                                                var _itemCount = Table.GetSkillUpgrading(_skillUpgradeId)
                                                    .GetSkillUpgradingValue(_i.EnterCount + 1);
                                                _i.RewardCount[j] = _itemCount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            //VIP
            foreach (var vipData in DataModel.VipInfos)
            {
                foreach (var info in vipData.Infos)
                {
                    var _id = info.Id;
                    if (_id != -1)
                    {
                        var _tbDungeon = Table.GetFuben(_id);
                        info.EnterCount = _playerData.GetExData(_tbDungeon.TodayCountExdata);
                        info.ResetCount = _playerData.GetExData(_tbDungeon.ResetExdata);
                        info.TotalCount = info.ResetCount + _tbDungeon.TodayCount;
                        info.CompleteCount = info.EnterCount.ToString();
                        RefreshCurrentNum(_id, info.EnterCount, info.TotalCount);
                        if (info.TotalCount > 0)
                        {
                            info.CompleteCount += "/" + info.TotalCount;
                        }
                        info.IsLock = _playerData.CheckCondition(_tbDungeon.EnterConditionId) != 0;
                    }
                }
            }

            AnalyticalNotices();

            RefreshFubenGoIntoNumTime();
        }

        private void OnRenewalQueneEvent(IEvent ievent)
        {
            UpdateQueueingMsg();
        }
        private void OnSettingDisplayScannedEvent(IEvent ievent)
        {
            var _e = ievent as DungeonSetScan;
            DataModel.IsShowScan = _e.ShowScan;
        }

        private void OnQuitGameInstanceWithOutMsgBoxEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(GoOutCachotCoroutine());
        }

        #endregion 

        bool IsLeader()
        {
            var myUid = PlayerDataManager.Instance.GetGuid();
            var isLeader = myUid == DataModule.TeamList[0].Guid;
            return isLeader;
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
        private TeamDataModel DataModule
        {
            get { return PlayerDataManager.Instance.TeamDataModel; }
            set { PlayerDataManager.Instance.TeamDataModel = value; }
        }

        void ChatTeam(IEvent ievent)
        {
            ChatTeamByTargetEvent eve = new ChatTeamByTargetEvent(DataModule.TeamId,
                1,
                copyId,
                0,
                0);
            EventDispatcher.Instance.DispatchEvent(eve);
            ChangeTeam();
        }

        void AutoMatch(IEvent ievent)
        {
            ChangeTeam(IsLeader());
        }
        void ChangeTeam(bool isAuto = false)
        {
            var recoard = Table.GetFuben(copyId);
            var condition = recoard.EnterConditionId;
            var open = false;
            var enterLevel = 0;
            var maxLevel = 0;

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
                TeamTargetChangeItemDataModel recItem = new TeamTargetChangeItemDataModel();
                recItem.targetItemGroupType = 1;
                recItem.targetItemId = recoard.Id;
                recItem.targetItemName = recoard.Name;
                recItem.isBelongIndex = 1;
                recItem.levelMini = enterLevel;
                var record = Table.GetClientConfig(103);
                if (null != record)
                    maxLevel = int.Parse(record.Value);
            }

            if (PlayerDataManager.Instance.currentTeamTarget.isBelongIndex != 1 || PlayerDataManager.Instance.currentTeamTarget.targetItemId != copyId)
            {
                var msg = NetManager.Instance.ChangetTeamTarget(1,
                    copyId,
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

            if (isAuto)
            {
                int isHaveTeam = DataModule.HasTeam == true ? 1 : 0;
                var msg1 = NetManager.Instance.AutoMatchBegin(isHaveTeam, 1, copyId);
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
    }
}