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
    public class GiftRankController : IControllerBase
    {
        #region 成员变量
        private RankDataModel DataModel;
        private Dictionary<int, RankType> PageIndex2RankType = new Dictionary<int, RankType>(); 
        #endregion

        #region 构造函数
        public GiftRankController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnInitExtDataEvent);
            EventDispatcher.Instance.AddEventListener(GiftRankCellClick.EVENT_TYPE, OnClickLeaderboaedItemEvent);
            EventDispatcher.Instance.AddEventListener(GiftRankOperationEvent.EVENT_TYPE, OnLeaderboardWorkEvent);
        }
        #endregion

        #region 固有函数
        public void Close()
        {
            DataModel.SelectCellData.CharacterId = 0uL;
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
            for (var rankType = RankType.DailyGift; rankType <= RankType.TotalGift; ++rankType)
            {
                DataModel.RandLists[(int)rankType] = new ObservableCollection<RankCellDataModel>();
                PageIndex2RankType[i++] = rankType;
            }

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
            var _count = DataModel.RandLists.Count;
            if (_count == 0)
            {
                return;
            }
            for (var i = 0; i < _count; i++)
            {
                DataModel.ShowPages[i] = false;
            }
            DataModel.ShowPages[0] = true;
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
                var _msg = NetManager.Instance.GetRankList(-1, rankType);
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
                        if (_flag == 1)
                        {
                            _cell.IsSel = true;
                            DataModel.SelectCellData = _cell;
                            PlayerDataManager.Instance.ApplyPlayerInfo(_cell.CharacterId, RefurbishRole);
                        }
                        else
                        {
                            _cell.IsSel = false;
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
            EventDispatcher.Instance.DispatchEvent(new UpdateGiftModelViewEvent(DataModel.SelectList.Count));
        }

        private void OnChangeShowPageOnAlterIndicate(object sender, PropertyChangedEventArgs e)
        {
            for (var i = 0; i < DataModel.RandLists.Count; i++)
            {
                if (DataModel.ShowPages[i])
                {
                    DemandLeaderboardList((int)PageIndex2RankType[i]);
                }
            }
        }
        private void RefurbishRole(PlayerInfoMsg info)
        {
            if (DataModel.ShowPages[6])
            {
                var _e = new RankRefreshModelView(info, true);
                DataModel.TargetWorshipCount = info.WorshipCount;
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            else
            {
                if (info.Id == 0uL)
                {
                    return;
                }
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

        private void RefurbishLeaderboardItem(int index)
        {
            var _DataModelShowPagesCount0 = DataModel.RandLists.Count;
            for (var i = 0; i < _DataModelShowPagesCount0; i++)
            {
                if (DataModel.ShowPages[i])
                {
                    var _list = DataModel.RandLists[(int)PageIndex2RankType[i]];
                    var _listCount1 = _list.Count;
                    for (var j = 0; j < _listCount1; j++)
                    {
                        _list[j].IsSel = j == index;
                        if (_list[j].IsSel)
                        {
                            DataModel.SelectCellData = _list[j];
                        }
                    }
                    break;
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

                            var _animationId = GameUtils.RankWorshipAction[_playerInfo.TypeId];
                            var _e = new RankNotifyLogic(1, _animationId);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        if (DataModel.SelfWorshipCount != _count + 1)
                        {
                            DataModel.SelfWorshipCount = _count + 1;
                        }
                        if (DataModel.SelfWorshipCount >= DataModel.WorshipCountMax)
                        {
                            PlayerDataManager.Instance.NoticeData.RankingCanLike = false;
                        }
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
            var _e = ievent as GiftRankCellClick;
            var _index = _e.Index;
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

        private void OnLeaderboardWorkEvent(IEvent ievent)
        {
            var _e = ievent as GiftRankOperationEvent;
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










    }
}