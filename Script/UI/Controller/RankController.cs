/********************************************************************************* 

                         Scorpion



  *FileName:LeaderboardFrameCtrler

  *Version:1.0

  *Date:2017-06-19

  *Description:

**********************************************************************************/
#region using

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

#endregion

namespace ScriptController
{
    public class LeaderboardFrameCtrler : IControllerBase
    {
        #region 成员变量
        private RankDataModel DataModel;
        private Dictionary<int, RankType> PageIndex2RankType = new Dictionary<int, RankType>();
        private int[] rankTypes = new int[3];//区别排行数据是否为真
        private int applyCount = 0;//请求真实排行榜玩家数据次数
        private int trueRankDataCount = 0;//真实的排行榜玩家数据个数
        private ulong RankCharacterId = 0;
        #endregion

        #region 构造函数
        public LeaderboardFrameCtrler()
        {
            CleanUp(); 
            EventDispatcher.Instance.AddEventListener(FightLeaderMasterEvent.EVENT_TYPE, PullFightLeaderMasterData);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnInitExtDataEvent);
            EventDispatcher.Instance.AddEventListener(RankCellClick.EVENT_TYPE, OnClickLeaderboaedItemEvent);
            EventDispatcher.Instance.AddEventListener(RankOperationEvent.EVENT_TYPE, OnLeaderboardWorkEvent);
            EventDispatcher.Instance.AddEventListener(OnRankNpcClick_Event.EVENT_TYPE, OnClickRankNPC);
        }
        #endregion

        #region 固有函数
        public void Close()
        {
            //         for (int i = 0; i < 4; i++)
            //         {
            //             DataModel.ShowPages[i] = false;
            //         }
        }

        public void Tick()
        {
        }

        public void CleanUp()
        {
            if (DataModel != null)
            {
                DataModel.ShowPages.PropertyChanged -= OnChangeShowPageOnAlterIndicate;
            }
            DataModel = new RankDataModel();
            var i = 0;
            for (var rankType = RankType.FightValue; rankType <= RankType.PetFight; ++rankType, ++i)
            {
                DataModel.RandLists[(int)rankType] = new ObservableCollection<RankCellDataModel>();
                PageIndex2RankType[i] = rankType;
            }
            DataModel.RandLists[(int)RankType.Mount] = new ObservableCollection<RankCellDataModel>();
            PageIndex2RankType[4] = RankType.Mount;

            DataModel.ShowPages.PropertyChanged += OnChangeShowPageOnAlterIndicate;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
            var _characterId = DataModel.SelectCellData.CharacterId;
            PlayerDataManager.Instance.ApplyPlayerInfo(_characterId, RefurbishRole);
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as RankArguments;
            var _count = DataModel.ShowPages.Count;
            if (_count == 0)
            {
                return;
            }
            for (var i = 0; i < _count; i++)
            {
                DataModel.ShowPages[i] = false;
            }
            DataModel.ShowPages[0] = true;
            if (args != null)
            {
                RankCharacterId = args.RankId;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 逻辑函数
        private void DemandLeaderboardList(int rankType)
        {
            NetManager.Instance.StartCoroutine(DemandLeaderboardListCoroutine(rankType));
        }

        private IEnumerator DemandLeaderboardListCoroutine(int rankType)
        {
            using (new BlockingLayerHelper(0))
            {
                var _serverId = PlayerDataManager.Instance.ServerId;
                var _msg = NetManager.Instance.GetRankList(_serverId, rankType);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        InitLeaderboardDatum(_msg.Response);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("GetRankList Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("GetRankList Error!............State..." + _msg.State);
                }
            }
        }

        private void InitLeaderboardDatum(RankList list)
        {
            var _flag = 0;
            DataModel.CurrentSelect = 0;
            DataModel.SelfRank = -1;
            var _type = list.RankType;
            DataModel.RandLists[_type].Clear();
            var _selfGuid = ObjManager.Instance.MyPlayer.GetObjId();
            {
                var _list1 = list.RankData;
                var _listCount1 = _list1.Count;
                for (var _i1 = 0; _i1 < _listCount1; ++_i1)
                {
                    var _one = _list1[_i1];
                    {
                        _one.Name = GameUtils.ServerStringAnalysis(_one.Name);
                        _flag++;
                        var _cell = new RankCellDataModel
                        {
                            CharacterId = _one.Id,
                            Name = _one.Name,
                            Value = _one.Value,
                            Id = _flag
                        };
                        if (RankCharacterId != 0 && RankCharacterId == _one.Id)
                        {
                            DataModel.CurrentSelect = _flag - 1;
                        }
                        //ranklist.Add(cell);
                        DataModel.RandLists[_type].Add(_cell);
                        if (_cell.CharacterId == _selfGuid)
                        {
                            DataModel.SelfRank = _flag;
                        }
                    }
                }
            }
            DataModel.SelectList = DataModel.RandLists[_type];
            RefurbishLeaderboardItem(DataModel.CurrentSelect);
            if (DataModel.CurrentSelect > 5)
            {
                var e = new RankScrollViewMoveEvent(DataModel.CurrentSelect);
                EventDispatcher.Instance.DispatchEvent(e);
            }
        }

        private void OnChangeShowPageOnAlterIndicate(object sender, PropertyChangedEventArgs e)
        {
            for (var i = 0; i < DataModel.ShowPages.Count; i++)
            {
                if (DataModel.ShowPages[i])
                {
                    DemandLeaderboardList((int)PageIndex2RankType[i]);
                }
            }
        }
        private void RefurbishRole(PlayerInfoMsg info)
        {
            bool isFirst = false;
            if (null == PlayerDataManager.Instance.FightLeaderMaster) isFirst = true;
            if (isFirst)
            {
                EventDispatcher.Instance.DispatchEvent(new FightLeaderMasterEvent());
            }
            if (DataModel.ShowPages[6])
            {
                var _e = new RankRefreshModelView(info, true);
                DataModel.TargetWorshipCount = info.WorshipCount;
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            else if (DataModel.ShowPages[4])
            {
                if (info.MountId <= 0)
                {
                    return;
                }

                var tbMount = Table.GetMount(info.MountId);
                if (tbMount == null)
                    return;

                var _e = new RankRefreshModelView(info, false, true);
                DataModel.TargetWorshipCount = info.WorshipCount;
                DataModel.MountId = tbMount.Id;
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            else
            {
                var _e = new RankRefreshModelView(info, false);
                DataModel.TargetWorshipCount = info.WorshipCount;
                EventDispatcher.Instance.DispatchEvent(_e);
            }
        }

        private void RefurbishRespectFigure()
        {
            var _exdata = PlayerDataManager.Instance.GetExData(312);
            DataModel.SelfWorshipCount = DataModel.WorshipCountMax - _exdata;
        }

        private void RefurbishLeaderboardItem(int index,ulong RankCharacterId = 0)
        {
            var _DataModelShowPagesCount0 = DataModel.ShowPages.Count;
            for (var i = 0; i < _DataModelShowPagesCount0; i++)
            {
                if (DataModel.ShowPages[i])
                {
                    var _list = DataModel.RandLists[(int)PageIndex2RankType[i]];
                    var _listCount1 = _list.Count;
                    if (RankCharacterId == 0)
                    {
                        for (var j = 0; j < _listCount1; j++)
                        {
                            _list[j].IsSel = j == index;
                            if (_list[j].IsSel)
                            {
                                DataModel.SelectCellData = _list[j];
                                DataModel.CurrentSelect = j;
                            }
                        }
                        break;
                    }
                    else
                    {
                        for (var j = 0; j < _listCount1; j++)
                        {
                            _list[j].IsSel = _list[j].CharacterId == RankCharacterId;
                            if (_list[j].IsSel)
                            {
                                DataModel.SelectCellData = _list[j];
                                DataModel.CurrentSelect = j;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            var _characterId = DataModel.SelectCellData.CharacterId;
            PlayerDataManager.Instance.ApplyPlayerInfo(_characterId, RefurbishRole);
        }

        private void RespectRole()
        {
            var _charId = DataModel.SelectCellData.CharacterId;
            if (_charId == ObjManager.Instance.MyPlayer.GetObjId())
            {
                var _e = new ShowUIHintBoard(220501);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            if (DataModel.SelfWorshipCount >= DataModel.WorshipCountMax)
            {
                //已经没有崇拜次数了
                var _e = new ShowUIHintBoard(3000002);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }


            NetManager.Instance.StartCoroutine(RespectRoleCoroutine());
        }

        private IEnumerator RespectRoleCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _charId = DataModel.SelectCellData.CharacterId;
                var _msg = NetManager.Instance.WorshipCharacter(_charId);
                var _count = DataModel.SelfWorshipCount;
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DataModel.TargetWorshipCount++;
                        var _playerInfo = PlayerDataManager.Instance.GetCharacterSimpleInfo(_charId);
                        if (_playerInfo != null)
                        {
                            _playerInfo.WorshipCount++;
                            //////var _animationId = GameUtils.RankWorshipAction[_playerInfo.TypeId];
                            //////var _e = new RankNotifyLogic(1, _animationId);
                            //////EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        if (DataModel.SelfWorshipCount != _count + 1)
                        {
                            DataModel.SelfWorshipCount = _count + 1;
                        }
                        if (DataModel.SelfWorshipCount >= DataModel.WorshipCountMax)
                        {
                            PlayerDataManager.Instance.NoticeData.RankingCanLike = false;
                        }
                        DataModel.CurrentSelect++;
                        if (DataModel.CurrentSelect >= DataModel.SelectList.Count)
                        {
                            yield break;
                        }
                        RefurbishLeaderboardItem(DataModel.CurrentSelect);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterSame)
                    {
                        var _e = new ShowUIHintBoard(220501);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_WorshipAlready)
                    {
                        var _e = new ShowUIHintBoard(3000001);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_WorshipCount)
                    {
                        var _e = new ShowUIHintBoard(3000002);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("WorshipCharacter Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("WorshipCharacter Error!............State..." + _msg.State);
                }
            }
        }

        #endregion

        #region 事件
        private void OnClickLeaderboaedItemEvent(IEvent ievent)
        {
            var _e = ievent as RankCellClick;
            var _index = _e.Index;
            DataModel.CurrentSelect = _index;
            RefurbishLeaderboardItem(_index);
        }

        private void OnInitExtDataEvent(IEvent ievent)
        {
            DataModel.WorshipCountMax = Table.GetExdata(312).InitValue;
            RefurbishRespectFigure();
            var flag = PlayerDataManager.Instance.GetFlag(119);
            if (flag)
            {
                if (DataModel.SelfWorshipCount < DataModel.WorshipCountMax)
                {
                     PlayerDataManager.Instance.NoticeData.RankingCanLike = true;
                }                
            }
        }

        private void PullFightLeaderMasterData(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(FightRankListCoroutine(0));
        }

        private void NotifyRefreshRankModel()
        {
            // 先刷新真实的排行数据
            var fightLeaderData = PlayerDataManager.Instance.FightLeaderMaster;
            int count = fightLeaderData.Count;
            for (int i = 0; i < count; i++)
            {
                if (fightLeaderData[i] != null)
                {
                    rankTypes[fightLeaderData[i].TypeId] = 1;
                    var _e = new FightLeaderMasterRefreshModelView(fightLeaderData[i], fightLeaderData[i].TypeId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }

            // 然后补全排行NPC数据
            for (int i = 0; i < 3; i++)
            {
                if (rankTypes[i] != 1)
                {
                    var _e = new FightLeaderMasterRefreshModelView(null, i);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        private void OnLeaderboardWorkEvent(IEvent ievent)
        {
            var _e = ievent as RankOperationEvent;
            switch (_e.Type)
            {
                case 1:
                {
                    var _charId = DataModel.SelectCellData.CharacterId;
                    if (_charId == ObjManager.Instance.MyPlayer.GetObjId())
                    {
                        var _e1 = new Show_UI_Event(UIConfig.CharacterUI);
                        EventDispatcher.Instance.DispatchEvent(_e1);
                        return;
                    }
                    PlayerDataManager.Instance.ShowPlayerInfo(DataModel.SelectCellData.CharacterId);
                }
                    break;
                case 2:
                {
                    RespectRole();
                }
                    break;
                case 3:
                {
                    RefurbishRespectFigure();
                }
                    break;
            }
        }
        #endregion

        private IEnumerator FightRankListCoroutine(int rankType)
        {
            using (new BlockingLayerHelper(0))
            {
                var _serverId = PlayerDataManager.Instance.ServerId;
                var _msg = NetManager.Instance.GetFightRankList(_serverId, rankType);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (null != PlayerDataManager.Instance.FightLeaderMaster) PlayerDataManager.Instance.FightLeaderMaster.Clear();
                        var data = _msg.Response.RankData;
                        trueRankDataCount = data.Count;
                        if (0 == trueRankDataCount)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                var _e = new FightLeaderMasterRefreshModelView(null, i);
                                EventDispatcher.Instance.DispatchEvent(_e);
                            }
                        }
                        else
                        {
                            applyCount = 0;
                            for (int d = 0; d < data.Count; d++)
                            {
                                PlayerDataManager.Instance.ApplyPlayerInfo(data[d].Id, FightRankListRefurbishRole);
                            }   
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("GetRankList Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("FightRankList Error!............State..." + _msg.State);
                }
            }
        }

        private void FightRankListRefurbishRole(PlayerInfoMsg info)
        {
            PlayerDataManager.Instance.FightLeaderMaster.Add(info);
            applyCount++;
            if (trueRankDataCount == applyCount)
            {
                NotifyRefreshRankModel();
            }
        }

        private void OnClickRankNPC(IEvent ievent)
        {
            var e = ievent as OnRankNpcClick_Event;
            if (null == e)
            {
                return;
            }
            var dicId = 0;
            var npcId = e.NpcId;
            switch (npcId)
            {
                case 108:
                {
                    dicId = 100001432;//全服第一剑士\n虚位以待
                }
                    break;
                case 109:
                {
                    dicId = 100001433;//全服第一魔法师\n虚位以待
                }
                    break;
                case 110:
                {
                    dicId = 100001434;//全服第一弓箭手\n虚位以待
                }
                    break;
            }
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(dicId));
        }



    }
}