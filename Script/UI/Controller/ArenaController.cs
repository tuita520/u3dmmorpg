/********************************************************************************* 

                         Scorpion



  *FileName:ColiseumFrameCtrler

  *Version:1.0

  *Date:2017-06-13

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
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class ColiseumFrameCtrler : IControllerBase
    {

        #region 成员变量

        private ArenaDataModel m_ArenaDataModel;
        private MultyBattleDataModel m_MultyBattleModel;
        private ReadonlyObjectList<AttributeBaseDataModel> m_listAttributeList = new ReadonlyObjectList<AttributeBaseDataModel>(3);
        private BuildingData m_BuildingData;
        private bool m_bIsInit; 
        private BuildingRecord m_TbBuilding;
        private BuildingServiceRecord m_TbBuildingService;
        private List<int> m_listStatueOpenLevel;
        //--------------------------------------------------------------Notice--------
        private Coroutine NoticeArenaCoroutine;
        private Coroutine NoticeStatuCoroutine;
        private StatueDataModel m_StatueDataModel;
        private List<PVPBattleRecord> PvpBattleRecords;
        private MultyBattleCellData LeftCell;
        private MultyBattleCellData RightCell;
        private Dictionary<int, int> FirstRewardDic = new Dictionary<int, int>();
        #endregion

        #region 构造函数

        public ColiseumFrameCtrler()
        {
            m_bIsInit = false;
            CleanUp();
            //event
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExMsgInitialEvent);
            EventDispatcher.Instance.AddEventListener(ArenaExdataUpdate.EVENT_TYPE, OnUpgradeExMsgEvent);
            EventDispatcher.Instance.AddEventListener(ExData64InitEvent.EVENT_TYPE, OnExMsgInitial64Event);
            EventDispatcher.Instance.AddEventListener(ExData64UpDataEvent.EVENT_TYPE, OnUpgradeExData64Event);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagUpgradeEvent);
            EventDispatcher.Instance.AddEventListener(CityDataInitEvent.EVENT_TYPE, OnCityMsgInitialEvent);
            EventDispatcher.Instance.AddEventListener(VipLevelChangedEvent.EVENT_TYPE, OnVipLvChangeEvent);
            //arena
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnUpgradeHonorEvent);
            EventDispatcher.Instance.AddEventListener(ArenaOperateEvent.EVENT_TYPE, OnColiseumOperateEvent);
            EventDispatcher.Instance.AddEventListener(AreanOppentCellClick.EVENT_TYPE, OnColiseumOppentCellTipEvent);
            EventDispatcher.Instance.AddEventListener(ArenaFightRecoardChange.EVENT_TYPE, OnColiseumWarRecordChangeEvent);

            //statue
            EventDispatcher.Instance.AddEventListener(UIEvent_SeeSkills.EVENT_TYPE, OnSeePetAnimalSkillEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_Promotion_Rank.EVENT_TYPE, OnUpgradeRankEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_CityUpdateBuilding.EVENT_TYPE, OnUpgradeBuildEvent);
            EventDispatcher.Instance.AddEventListener(SatueOperateEvent.EVENT_TYPE, OnSatueOperatedEvent);
            EventDispatcher.Instance.AddEventListener(ArenaPetListEvent.EVENT_TYPE, OnColiseumPetAnmialRankEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_CityEvent.EVENT_TYPE, OnSceneEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_PetLevelup.EVENT_TYPE, OnPetLVupEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_OnClickRankBtn.EVENT_TYPE, OnTipRankEvent);

            //Battle
            //EventDispatcher.Instance.AddEventListener(UIEvent_MultyBattleEvent.EVENT_TYPE,OnMultyBattleEvent);
            EventDispatcher.Instance.AddEventListener(MultyBattleOperateEvent.EVENT_TYPE, OnMultiFightOperateEvent);
            EventDispatcher.Instance.AddEventListener(QueneUpdateEvent.EVENT_TYPE, OnRenewalQueueEvent);
            EventDispatcher.Instance.AddEventListener(CheckGetFirstWinRewardEvent.EVENT_TYPE, OnCheckGetFirstWinRewardEvent);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnMarkRenwalEvent);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            m_ArenaDataModel = new ArenaDataModel();
            m_StatueDataModel = new StatueDataModel();
            m_BuildingData = null;
            m_bIsInit = false;
            m_MultyBattleModel = new MultyBattleDataModel();
            PvpBattleRecords = new List<PVPBattleRecord>();
            LeftCell = new MultyBattleCellData();
            RightCell = new MultyBattleCellData();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "PlayerDataModel")
            {
                return PlayerDataManager.Instance.PlayerDataModel;
            }
            if (name == "Arena")
            {
                return m_ArenaDataModel;
            }
            if (name == "Statue")
            {
                return m_StatueDataModel;
            }
            if (name == "MultiBattle")
            {
                return m_MultyBattleModel;
            }
            return null;
        }

        public void Close()
        {
            m_ArenaDataModel.ShowPetSkills = false;
            m_ArenaDataModel.TabPage = 0;
            PlayerDataManager.Instance.CloseCharacterPopMenu();
        }

        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
            var tbScene = Table.GetScene(sceneId);
            if (null == tbScene)
                return;
            if (tbScene.Type == (int)eSceneType.Pvp)
            {
                EventDispatcher.Instance.DispatchEvent(new RestoreMainUIMenu());
            }
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            Initial();
            var _args = data as ArenaArguments;
            if (_args == null)
            {
                return;
            }
            if (_args.Tab > 2)
            {
                return;
            }
            m_ArenaDataModel.TabPage = _args.Tab;
            NetManager.Instance.StartCoroutine(ApplyColiseumMsgCoroutine());
            RenewalColiseum();
            var _buildingData = _args.BuildingData;
            RenewalBuildDataModel(_buildingData);

            StudyNoticeColiseum();
            StudyNoticeEffigy();

            SettingsSatueMsg(0);
            for (int i = 0; i < m_StatueDataModel.StatueInfos.Count; i++)
            {
                RenewalExpNum(m_StatueDataModel.StatueInfos[i]);
            }

            //重置页面显示状态
            var _e = new ArenaNotifyLogic(0);
            EventDispatcher.Instance.DispatchEvent(_e);

            m_ArenaDataModel.ShowPetSkills = false;
            RefreshMultipleBattle();
            AnalyseGiveNoticeBattle();
        }

        //-------------------------------------------------------------Base-------------------
        public FrameState State { get; set; }

        #endregion

        #region 逻辑函数

        private void AdditionRankingExp(StatueInfoDataModel infoData, int exp)
        {
            var _statusId = infoData.DataIndex;
            var _lastExp = infoData.CurExp;
            _lastExp += exp;
            var _tbStatus = Table.GetStatue(_statusId);


            var _needExp = _tbStatus.LevelUpExp;
            var _maxList = new List<int>();
            _maxList.Add(_needExp);
            var _isLvUp = false;
            while (_lastExp >= _needExp)
            {
                if (_tbStatus.NextLevelID == -1)
                {
                    _lastExp = _tbStatus.LevelUpExp;
                    break;
                }
                _lastExp -= _needExp;
                _statusId = _tbStatus.NextLevelID;
                _tbStatus = Table.GetStatue(_tbStatus.NextLevelID);
                _needExp = _tbStatus.LevelUpExp;
                _maxList.Add(_tbStatus.LevelUpExp);
                _isLvUp = true;
            }

            infoData.DataIndex = _statusId;
            infoData.CurExp = _lastExp;

            var _index = m_StatueDataModel.SelectStatue.Index;

            m_BuildingData.Exdata[_index] = _statusId;
            m_BuildingData.Exdata64[_index] = _lastExp;
            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Statue);

            //        StatueDataModel.ExpSlider.MaxValues = maxList;
            //        StatueDataModel.ExpSlider.TargetValue = infoData.CurExp/(float) tbStatus.LevelUpExp + (maxList.Count - 1);
            if (_isLvUp)
            {
                RenewalStatuePurse(infoData);
                RenewalStatueParticularity(infoData);
            }
        }
        private bool CheckNotice()
        {
            var StatueItem = m_StatueDataModel.SelectStatue;
            var RepairId = StatueItem.MaintainItemId[1];
            var CleanId = StatueItem.MaintainItemId[2];
            if (StatueItem.MaintainItemCount[1] <= PlayerDataManager.Instance.GetItemCount(RepairId) ||
                StatueItem.MaintainItemCount[2] <= PlayerDataManager.Instance.GetItemCount(CleanId))
            {
                return true;
            }
            else
            {
                return false;
            }
            return false;
        }
        private void StudyNoticeColiseum()
        {
            if (NoticeArenaCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(NoticeArenaCoroutine);
                NoticeArenaCoroutine = null;
            }
            if (m_ArenaDataModel.EnterCount > 0)
            {
                if (m_ArenaDataModel.RefreshTime <= Game.Instance.ServerTime)
                {
                    var flag = PlayerDataManager.Instance.GetFlag(118);
                    if (flag)
                    {
                        PlayerDataManager.Instance.NoticeData.ArenaCount = true;                        
                    }
                }
                else
                {
                    var scends = (int)(m_ArenaDataModel.RefreshTime - Game.Instance.ServerTime).TotalSeconds;
                    NoticeArenaCoroutine = NetManager.Instance.StartCoroutine(StudyNoticeColiseumCoroutine(scends));
                    PlayerDataManager.Instance.NoticeData.ArenaCount = false;
                }
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.ArenaCount = false;
            }
        }

        private IEnumerator StudyNoticeColiseumCoroutine(int scends)
        {
            yield return new WaitForSeconds(scends);
            StudyNoticeColiseum();
        }

        private void StudyNoticeEffigy()
        {
            if (m_StatueDataModel.HasStatueOpen == false)
            {
                return;
            }
            if (NoticeStatuCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(NoticeStatuCoroutine);
                NoticeStatuCoroutine = null;
            }

            if (m_StatueDataModel.ChallengeCount > 0)
            {
                if (m_StatueDataModel.MaintainCd <= Game.Instance.ServerTime)
                {
                    if (CheckNotice())
                    {
                        PlayerDataManager.Instance.NoticeData.ArenaStatus = PlayerDataManager.Instance.CheckCondition(2014) == 0;
                    }
                    else
                    {
                        PlayerDataManager.Instance.NoticeData.ArenaStatus = false;
                    }
                }
                else
                {
                    if (m_StatueDataModel.MaintainCdFlag)
                    {
                        var scends = (int)(m_StatueDataModel.MaintainCd - Game.Instance.ServerTime).TotalSeconds;
                        NoticeArenaCoroutine = NetManager.Instance.StartCoroutine(StudyNoticeEffigyCoroutine(scends));
                        PlayerDataManager.Instance.NoticeData.ArenaStatus = false;
                    }
                    else
                    {
                        if (CheckNotice())
                        {
                            PlayerDataManager.Instance.NoticeData.ArenaStatus = PlayerDataManager.Instance.CheckCondition(2014) == 0;
                        }
                        else
                        {
                            PlayerDataManager.Instance.NoticeData.ArenaStatus = false;
                        }
                    }
                }
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.ArenaStatus = false;
            }
        }

        private IEnumerator StudyNoticeEffigyCoroutine(int scends)
        {
            yield return new WaitForSeconds(scends);
            StudyNoticeEffigy();
        }

        private IEnumerator ApplyColiseumMsgCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.GetP1vP1LadderPlayer(-1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _info = _msg.Response;
                        var _infoPlayersCount6 = _info.Players.Count;
                        for (var i = 0; i < _infoPlayersCount6; i++)
                        {
                            var _playerInfo = _info.Players[i];
                            var _opponent = m_ArenaDataModel.OpponentList[i];
                            _opponent.FightValue = _playerInfo.FightPoint;
                            _opponent.Guid = _playerInfo.Id;
                            _opponent.Name = _playerInfo.Name;
                            _opponent.RoleId = _playerInfo.TypeId;
                            _opponent.Rank = _info.Ranks[i];
                            _opponent.Level = _playerInfo.Level;
                            _opponent.Reincarnation = _playerInfo.Ladder;
                            _opponent.StarNum = _playerInfo.StarNum;
                            //_playerInfo.StarNum
                            _opponent.VSPlayIcon = GameUtils.GetRebornSquareIconId(_opponent.RoleId, _opponent.Reincarnation);
                        }
                        UpgradeList(_info.NowLadder);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....GetP1vP1LadderPlayer...ErrorCode....{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....GetP1vP1LadderPlayer...State....{0}.", _msg.State);
                }
            }
        }

        private IEnumerator ApplyWarRegisterCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                m_ArenaDataModel.RecoardList.Clear();
                m_ArenaDataModel.RecordCount = 0;
                var _msg = NetManager.Instance.GetP1vP1LadderOldList(-1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _list = new List<ArenaRecoardDataModel>();
                        {
                            var _list6 = _msg.Response.Data;
                            var _listCount6 = _list6.Count;
                            for (var _i6 = 0; _i6 < _listCount6; ++_i6)
                            {
                                var _changeOne = _list6[_i6];
                                {
                                    var _recoardData = new ArenaRecoardDataModel();
                                    _recoardData.Name = _changeOne.Name;
                                    _recoardData.NewRank = _changeOne.NewRank;
                                    _recoardData.OldRank = _changeOne.OldRank;
                                    _recoardData.Type = _changeOne.Type;
                                    FormattingRecord(_recoardData);
                                    _list.Insert(0, _recoardData);
                                }
                            }
                        }
                        m_ArenaDataModel.RecoardList = new ObservableCollection<ArenaRecoardDataModel>(_list);
                        m_ArenaDataModel.RecordCount = _list.Count;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Info("GetP1vP1LadderOldList error=[{0}]", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Info("GetP1vP1LadderOldList State=[{0}]", _msg.State);
                }
            }
        }

        private IEnumerator ApplyAdvancementRankingCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _tbHonor = Table.GetHonor(m_ArenaDataModel.MilitaryRank);
                var _honor = PlayerDataManager.Instance.GetRes((int)eResourcesType.Honor);
                if (_tbHonor == null)
                {
                    yield break;
                }
                var _msg = NetManager.Instance.UpgradeHonor(m_ArenaDataModel.MilitaryRank);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.SetExData((int)eExdataDefine.e250, _tbHonor.NextRank);
                        var _newHonour = _honor - _tbHonor.NeedHonor;
                        PlayerDataManager.Instance.SetRes((int)eResourcesType.Honor, _newHonour);

                        m_ArenaDataModel.MilitaryRank = _tbHonor.NextRank;
                        m_ArenaDataModel.HonorCount = _newHonour;
                        UpgradeHonorList();
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.Error_ResNoEnough)
                        {
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(210104)));
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                            Logger.Error(".....UpgradeHonor...ErrorCode....{0}.", _msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    Logger.Error(".....UpgradeHonor...State....{0}.", _msg.State);
                }
            }
        }

        private void ColiseumOppentWar(int index)
        {
            var _data = m_ArenaDataModel.OpponentList[index];

            var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
            var _tbScene = Table.GetScene(_sceneId);
        
            if (_tbScene.Type == (int)eSceneType.Fuben)
            {
                //当前若是副本 不可直接进入竞技场
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210123));
                return;
            }

            if (_tbScene.Type == (int)eSceneType.Pvp)
            {
                //"竞技场不能直接进入竞技场战斗
                var _e1 = new ShowUIHintBoard(270003);
                EventDispatcher.Instance.DispatchEvent(_e1);
                return;
            }

            if (m_ArenaDataModel.EnterCount > 0 && m_ArenaDataModel.RefreshTime > Game.Instance.ServerTime)
            {
                var _str = GameUtils.GetDictionaryText(220400);
                var _price = 0;
                var _tbClientConfig = Table.GetClientConfig(203);
                int.TryParse(_tbClientConfig.Value, out _price);
                var _strInfo = string.Format(_str, _price);

                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _strInfo, "",
                    () => { NetManager.Instance.StartCoroutine(WarOppentMsgCoroutine(_data, 1)); });
                return;
            }

            if (m_ArenaDataModel.EnterCount <= 0)
            {
                if (m_ArenaDataModel.BuyCount <= 0)
                {
                    var _tbVip = PlayerDataManager.Instance.TbVip;
                    var _oldAdd = _tbVip.PKBuyCount;
                    do
                    {
                        _tbVip = Table.GetVIP(_tbVip.Id + 1);
                    } while (_tbVip != null && _tbVip.PKBuyCount <= _oldAdd);

                    if (_tbVip == null)
                    {
                        var _e1 = new ShowUIHintBoard(220401);
                        EventDispatcher.Instance.DispatchEvent(_e1);
                    }
                    else
                    {
                        GameUtils.GuideToBuyVip(_tbVip.Id, 270297);
                    }
                    return;
                }
                var _str = GameUtils.GetDictionaryText(220431);
                var _tbUpgrade = Table.GetSkillUpgrading(19999);
                var _result = _tbUpgrade.GetSkillUpgradingValue(m_ArenaDataModel.BuyMax - m_ArenaDataModel.BuyCount);
                var _strInfo = string.Format(_str, _result);
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _strInfo, "",
                    () => { NetManager.Instance.StartCoroutine(WarOppentMsgCoroutine(_data, 2)); });
                return;
            }
            NetManager.Instance.StartCoroutine(WarOppentMsgCoroutine(_data, 0));
        }

        private void ColiseumOppentMsg(int index)
        {
            var _data = m_ArenaDataModel.OpponentList[index];
            PlayerDataManager.Instance.ShowCharacterPopMenu(_data.Guid, _data.Name, 17, _data.Level, _data.Reincarnation,
                _data.RoleId);
        }

        private void StartInCleanUp()
        {
            if (!InspectBuildingServe(2))
            {
                return;
            }
            var e = new SatueOperateEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
            //神像清洗暂时跳过小游戏直接清洗
            //var _arg = new CleanDustArguments();
            //_arg.StatueIndex = m_StatueDataModel.SelectStatue.DataIndex;
            //var _e = new Show_UI_Event(UIConfig.CleanDust, _arg);
            //EventDispatcher.Instance.DispatchEvent(_e);
        }

        private void StartInPuzzel()
        {
            if (!InspectBuildingServe(1))
            {
                return;
            }
            //神像修复暂时跳过小游戏直接修复
            var e = new SatueOperateEvent(11);
            EventDispatcher.Instance.DispatchEvent(e);
            //var _arg = new PuzzleImageArguments();
            //_arg.StatueIndex = m_StatueDataModel.SelectStatue.DataIndex;
            //var _e = new Show_UI_Event(UIConfig.PuzzleImage, _arg);
            //EventDispatcher.Instance.DispatchEvent(_e);
        }

        private bool InspectBuildingServe(int type)
        {
            if (type < 0 || type > 2)
            {
                return false;
            }
            var _selectStaue = m_StatueDataModel.SelectStatue;
            if (_selectStaue.IsOpen == false)
            {
                //当前神像不能维护
                var e = new ShowUIHintBoard(270007);
                EventDispatcher.Instance.DispatchEvent(e);
                return false;
            }

            var _tbStatue = Table.GetStatue(_selectStaue.DataIndex);
            if (_tbStatue.NextLevelID == -1 && _tbStatue.LevelUpExp == _selectStaue.CurExp)
            {
                //已经生到最大的等级了
                var _e = new ShowUIHintBoard(270010);
                EventDispatcher.Instance.DispatchEvent(_e);
                return false;
            }

            var needItemId = _selectStaue.MaintainItemId[type];
            var needCount = _selectStaue.MaintainItemCount[type];
            var haveCount = PlayerDataManager.Instance.GetItemCount(needItemId);
            if (needCount > haveCount)
            {
                var _tbItem = Table.GetItemBase(needItemId);
                //{0}不足
                var _str = string.Format(GameUtils.GetDictionaryText(270011), _tbItem.Name);
                var _e = new ShowUIHintBoard(_str);
                EventDispatcher.Instance.DispatchEvent(_e);
                var lackCount = needCount - haveCount;
                GameUtils.ShowQuickBuy(needItemId, lackCount);
                //PlayerDataManager.Instance.ShowItemInfoGet(_itemId);
                return false;
            }

            if (m_StatueDataModel.ChallengeCount == 0)
            {
                //剩余维护次数不足
                var _tbVip = PlayerDataManager.Instance.TbVip;
                var _oldAddCount = _tbVip.StatueAddCount;
                do
                {
                    _tbVip = Table.GetVIP(_tbVip.Id + 1);
                } while (_tbVip != null && _oldAddCount >= _tbVip.StatueAddCount);

                if (_tbVip == null)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(300130));
                }
                else if (PlayerDataManager.Instance.TbVip.Id < 12)
                {
                    GameUtils.GuideToBuyVip(_tbVip.Id);
                }
                else
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001470));
                }
                return false;
            }

            if (Game.Instance.ServerTime < m_StatueDataModel.MaintainCd)
            {
                if (m_StatueDataModel.MaintainCdFlag)
                {
                    //正在冷却中
                    var _e = new ShowUIHintBoard(270009);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return false;
                }
            }
            return true;
        }

        private IEnumerator CoolDownCoroutine(int needDia)
        {
            using (new BlockingLayerHelper(0))
            {
                var _ary = new Int32Array();
                _ary.Items.Add(1);
                _ary.Items.Add(needDia);
                var _msg = NetManager.Instance.UseBuildService(m_BuildingData.AreaId, m_TbBuilding.ServiceId, _ary);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        // StatueDataModel.IsShowCd = false;
                        StudyNoticeEffigy();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....UseBuildService...ErrorCode....{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....UseBuildService...State....{0}.", _msg.State);
                }
            }
        }

        private IEnumerator WarOppentMsgCoroutine(ArenaOpponentDataModel data, int type)
        {
            using (new BlockingLayerHelper(0))
            {
                //0  正常
                //1  cd购买
                //2  次数购买
                var _rank = data.Rank - 1;
                var _guid = data.Guid;
                if (type == 1)
                {
                    var _price = 0;
                    var _tbClientConfig = Table.GetClientConfig(203);
                    int.TryParse(_tbClientConfig.Value, out _price);
                    if (_price > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Diamond)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame,new RechargeFrameArguments { Tab = 0 }));
                        yield break;
                    }
                }
                else if (type == 2)
                {
                    var _tbUpgrade = Table.GetSkillUpgrading(19999);
                    var _price = _tbUpgrade.GetSkillUpgradingValue(m_ArenaDataModel.BuyMax - m_ArenaDataModel.BuyCount);
                    if (_price > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Diamond)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 }));
                        yield break;
                    }
                }
                var _msg = NetManager.Instance.GetP1vP1FightPlayer(_rank, _guid, type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlatformHelper.Event("city", "arenaFight");
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_LadderChange)
                    {
                        //对手的名次已经改变
                        UIManager.Instance.ShowMessage(MessageBoxType.Ok, 220399);
                        NetManager.Instance.StartCoroutine(ApplyColiseumMsgCoroutine());
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Info("GetP1vP1FightPlayer error=[{0}]", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Info("GetP1vP1FightPlayer State=[{0}]", _msg.State);
                }
            }
        }

        private void FormattingRecord(ArenaRecoardDataModel recoard)
        {
            if (recoard.Type == 0)
            {
                //主动进攻
                if (recoard.NewRank == -1)
                {
                    //你挑战了{0}获得胜利，排名不变
                    var _str = GameUtils.GetDictionaryText(220446);
                    recoard.Content = string.Format(_str, recoard.Name);
                }
                else if (recoard.NewRank < recoard.OldRank)
                {
                    //你挑战了{0}获得胜利，排名上升至{1}
                    var _str = GameUtils.GetDictionaryText(220402);
                    recoard.Content = string.Format(_str, recoard.Name, recoard.NewRank);
                }
                else
                {
                    //你挑战了{0}失败了，排名不变
                    var _str = GameUtils.GetDictionaryText(220403);
                    recoard.Content = string.Format(_str, recoard.Name);
                }
            }
            else
            {
                if (recoard.NewRank == -1)
                {
                    //{0}挑战了你，你失败了，排名不变
                    var _str = GameUtils.GetDictionaryText(220449);
                    recoard.Content = string.Format(_str, recoard.Name);
                }
                else if (recoard.NewRank > recoard.OldRank)
                {
                    //{0}挑战了你，你失败了，排名下降至{1}
                    var _str = GameUtils.GetDictionaryText(220405);
                    recoard.Content = string.Format(_str, recoard.Name, recoard.NewRank);
                    PlayerDataManager.Instance.NoticeData.ArenaRankChange = true;
                }
                else
                {
                    //{0}挑战了你，你胜利了，排名不变
                    var _str = GameUtils.GetDictionaryText(220404);
                    recoard.Content = string.Format(_str, recoard.Name);
                }
            }
        }

        private int AcquireSatueOpenLv(int index, int serverId)
        {
            //for (var i = serverId%10 + 1; i < 9; i++)
            //{
            //    var tableIndex = (serverId/10)*10 + i;
            //    var varBulidingServer = Table.GetBuildingService(tableIndex);
            //    if (varBulidingServer != null)
            //    {
            //        if (varBulidingServer.Param[0] > index)
            //        {
            //            return i + 1;
            //        }
            //    }
            //}

            if (index >= 0 && index < m_listStatueOpenLevel.Count)
            {
                return m_listStatueOpenLevel[index];
            }

            return -1;
        }

        private int AcquireStatueOpenNum()
        {
            var _playerLevel = PlayerDataManager.Instance.GetLevel();
            var _count = 0;
            var _enumerator = m_listStatueOpenLevel.GetEnumerator();
            while (_enumerator.MoveNext())
            {
                if (_playerLevel >= _enumerator.Current)
                {
                    ++_count;
                }
                else
                {
                    break;
                }
            }
            return _count;
        }

        private void Initial()
        {
            if (m_bIsInit)
                return;

            m_bIsInit = true;
            InitialRankingAward();
            InItialRankinglist();
            var _count = 0;
            var _tbMaxBs = Table.GetBuildingService(60);
            if (_tbMaxBs != null)
            {
                if (_tbMaxBs.Param.Length > 0)
                {
                    _count = _tbMaxBs.Param[0];
                }
            }
            m_StatueDataModel.StatueLimitCount = _count;
            m_listStatueOpenLevel = new List<int>(m_StatueDataModel.StatueLimitCount);
            m_listStatueOpenLevel.Add(Table.GetClientConfig(930).ToInt());
            m_listStatueOpenLevel.Add(Table.GetClientConfig(931).ToInt());
            m_listStatueOpenLevel.Add(Table.GetClientConfig(932).ToInt());
            m_listStatueOpenLevel.Add(Table.GetClientConfig(933).ToInt());

            NetManager.Instance.StartCoroutine(ApplyWarRegisterCoroutine());
        }

        private void InitialRankingAward()
        {
            var _last = 1;
            var _list = new List<ArenaRankAwardDataModel>();
            Table.ForeachArenaReward(recoard =>
            {
                var _cell = new ArenaRankAwardDataModel();
                _cell.Id = recoard.Id;
                _cell.Form = _last;
                _last = _cell.Id + 1;
                _list.Add(_cell);
                return true;
            });
            m_ArenaDataModel.RankAwards = new ObservableCollection<ArenaRankAwardDataModel>(_list);
        }

        private void InItialRankinglist()
        {
            if (m_ArenaDataModel.RankList.Count != 0)
            {
                return;
            }
            var _list = new List<ArenaRankDataModel>();
            Table.ForeachHonor(act =>
            {
                if (act != null && act.Id != 0)
                {
                    var _rank = new ArenaRankDataModel();
                    var _tbNameTitle = Table.GetNameTitle(act.TitleId);
                    _rank.HonorId = act.Id;
                    var _propCount = _tbNameTitle.PropValue.Length;
                    var _propStr1 = string.Empty;
                    var _propStr2 = string.Empty;
                    _rank.Item.Id = act.TitleId;
                    for (var i = 0; i < _propCount; i++)
                    {
                        var _propId = _tbNameTitle.PropId[i];
                        if (_propId == -1)
                        {
                            continue;
                        }
                        var _value = _tbNameTitle.PropValue[i];
                        if (_propId == 5)
                        {
                            _propStr1 = GameUtils.GetDictionaryText(222001) + ":" + _value + "-";
                        }
                        else if (_propId == 6)
                        {
                            _propStr1 += _value;
                        }
                        else if (_propId == 7)
                        {
                            _propStr2 = GameUtils.GetDictionaryText(222002) + ":" + _value + "-";
                        }
                        else if (_propId == 8)
                        {
                            _propStr2 += _value;
                        }
                        else
                        {
                            var _attr = new AttributeStringDataModel();
                            var _str = ExpressionHelper.AttrName[_propId] + ":";

                            _str += GameUtils.AttributeValue(_propId, _value);
                            _attr.LabelString = _str;
                            _rank.Item.Attributes.Add(_attr);
                        }
                    }
                    if (_propStr1 != string.Empty)
                    {
                        var _attr = new AttributeStringDataModel();
                        _attr.LabelString = _propStr1;
                        _rank.Item.Attributes.Insert(0, _attr);
                    }
                    if (_propStr2 != string.Empty)
                    {
                        var _attr = new AttributeStringDataModel();
                        _attr.LabelString = _propStr2;
                        _rank.Item.Attributes.Insert(0, _attr);
                    }
                    if (act.Id > m_ArenaDataModel.MilitaryRank)
                    {
                        _rank.Item.State = 0;
                    }
                    else
                    {
                        _rank.Item.State = 1;
                    }
                    _list.Add(_rank);
                }
                return true;
            });
            m_ArenaDataModel.RankList = new ObservableCollection<ArenaRankDataModel>(_list);
        }
        private void OnTipStatu(int index)
        {
            if (index < 0 || index > 4)
            {
                return;
            }
            SettingsSatueMsg(index);
        }
        private void OperatedStatuePetAnimal()
        {
            var _list = new List<int>();
            _list.Add(m_StatueDataModel.SelectStatue.Index);
            var _isCheck = false;
            {
                // foreach(var itemDataModel in StatueDataModel.PetList)
                var _enumerator4 = (m_StatueDataModel.PetList).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _itemDataModel = _enumerator4.Current;
                    {
                        if (_itemDataModel.Checked)
                        {
                            _list.Add(_itemDataModel.PetId);
                            _isCheck = true;

                            break;
                        }
                    }
                }
            }
            if (_isCheck == false)
            {
                _list.Add(-1);
            }
            SendSceneChooseAsking(CityOperationType.ASSIGNPETINDEX, m_BuildingData.AreaId, _list);
        }
        private void RenewalColiseum()
        {
        }

        private void RenewalBuildDataModel(BuildingData buildingData)
        {
            if (buildingData == null)
            {
                return;
            }

            Initial();

            m_BuildingData = buildingData;
            m_TbBuilding = Table.GetBuilding(m_BuildingData.TypeId);
            m_TbBuildingService = Table.GetBuildingService(m_TbBuilding.ServiceId);
            PlayerDataManager.Instance.NoticeData.ArenaTotalIcon = m_TbBuildingService.TipsIndex;
            var _isShowStatue = false;
            //var varBulidingServerParam04 = mTbBuildingService.Param[0];
            var _openStatueCount = AcquireStatueOpenNum();
            for (var i = 0; i < _openStatueCount; i++)
            {
                var _dataInfo = m_StatueDataModel.StatueInfos[i];
                _dataInfo.Index = i;
                _dataInfo.Condition = "";
                _dataInfo.IsOpen = true;
                _isShowStatue = true;
                SettingStatueMsg(_dataInfo, i);
                if (i >= m_StatueDataModel.StatueLimitCount)
                {
                    _dataInfo.IsShow = false;
                }
                else
                {
                    _dataInfo.IsShow = true;
                }
            }
            m_StatueDataModel.HasStatueOpen = _isShowStatue;
            var _buildingDataExdataCount5 = buildingData.Exdata.Count;
            for (var i = _openStatueCount; i < _buildingDataExdataCount5; i++)
            {
                var _dataInfo = m_StatueDataModel.StatueInfos[i];
                _dataInfo.IsOpen = false;
                _dataInfo.Index = i;
                var _tbStatu = Table.GetStatue(i * 100);
                var _openLv = AcquireSatueOpenLv(i, m_TbBuilding.ServiceId);
                //"级开启" 
                _dataInfo.Condition = string.Format(GameUtils.GetDictionaryText(270025), _openLv);
                _dataInfo.Name = _tbStatu.Name;
                SettingStatueMsg(_dataInfo, i);
                if (i >= m_StatueDataModel.StatueLimitCount)
                {
                    _dataInfo.IsShow = false;
                }
                else
                {
                    _dataInfo.IsShow = true;
                }
            }

            if (m_StatueDataModel.SelectStatue.DataIndex == -1)
            {
                //重置成第一个
                SettingsSatueMsg(0);
            }
            UpgradeStatusNum();
        }

        private void RenewalWarTime(long time)
        {
            m_ArenaDataModel.RefreshTime = Extension.FromServerBinary(time);
            StudyNoticeColiseum();
        }

        private void RenewalStatueParticularity(StatueInfoDataModel dataInfo)
        {
            var _tbStatue = Table.GetStatue(dataInfo.DataIndex);
            if (_tbStatue == null)
            {
                return;
            }
            if (_tbStatue.Level == 0)
            {
                _tbStatue = Table.GetStatue(dataInfo.DataIndex + 1);
            }
            dataInfo.Name = _tbStatue.Name;
            dataInfo.StatuAttribute.Type = _tbStatue.PropID[0];
            dataInfo.StatuAttribute.Value = _tbStatue.propValue[0];
            dataInfo.Fuse = _tbStatue.FuseValue[0];
            if (dataInfo.ItemId != -1)
            {
                var _ret = CityPetSkill.GetBSParamByIndex(BuildingType.ArenaTemple, m_TbBuildingService, 1,
                    new List<int> { dataInfo.ItemId });
                dataInfo.Fuse += _ret * 100;

                var _rate = dataInfo.Fuse / 10000.0f;
                var _pet = CityManager.Instance.GetAllPetByFilterItemId(PetListFileterType.Employ, dataInfo.ItemId);

                if (_pet != null)
                {
                    var _data = CityManager.PetItem2DataModel(_pet);
                    var _petId = _pet.ItemId;
                    var _level = _pet.Exdata[PetItemExtDataIdx.Level];
                    var _type = _tbStatue.FuseID[0];
                    if (_type == 5)
                    {
                        var _attributeValue =
                            FightAttribute.GetPetAttribut(_petId, (eAttributeType)_tbStatue.FuseID[0], _level) * _rate;
                        _type = 105;
                        dataInfo.PetAttribute.Type = _type;
                        dataInfo.PetAttribute.Value = (int)_attributeValue;
                        if (_tbStatue.FuseID[1] == 6)
                        {
                            var _attributeValueEx =
                                FightAttribute.GetPetAttribut(_petId, (eAttributeType)_tbStatue.FuseID[1], _level) * _rate;
                            dataInfo.PetAttribute.ValueEx = (int)_attributeValueEx;
                        }
                        else
                        {
                            dataInfo.PetAttribute.ValueEx = 0;
                        }
                    }
                    else
                    {
                        dataInfo.PetAttribute.Type = _type;
                        var _attributeValue =
                            FightAttribute.GetPetAttribut(_petId, (eAttributeType)_tbStatue.FuseID[0], _level) * _rate;
                        dataInfo.PetAttribute.Value = (int)_attributeValue;
                        dataInfo.PetAttribute.ValueEx = 0;
                    }
                }
            }
            else
            {
                dataInfo.PetAttribute.Type = -1;
                dataInfo.PetAttribute.Value = 0;
                dataInfo.PetAttribute.ValueEx = 0;
            }

            var attributeType = dataInfo.StatuAttribute.Type;
            dataInfo.TotalAttribute.Type = attributeType;
            dataInfo.TotalAttribute.Value = dataInfo.PetAttribute.Value + dataInfo.StatuAttribute.Value;

            if (dataInfo.PetAttribute.ValueEx > 0)
            {
                var _ex = dataInfo.StatuAttribute.ValueEx;
                if (_ex <= 0)
                {
                    _ex = dataInfo.StatuAttribute.Value;
                }
                dataInfo.TotalAttribute.ValueEx = dataInfo.PetAttribute.ValueEx + _ex;
            }
            else
            {
                dataInfo.TotalAttribute.ValueEx = 0;
            }


            dataInfo.TotalAttributeStr = GameUtils.AttributeName(attributeType) + "+" +
                                         GameUtils.AttributeValue(attributeType, dataInfo.TotalAttribute.Value);
            if (dataInfo.TotalAttribute.ValueEx != 0)
            {
                dataInfo.TotalAttributeStr += "-" + GameUtils.AttributeValue(attributeType, dataInfo.TotalAttribute.ValueEx);
            }

            dataInfo.StatuAttributeStr = GameUtils.AttributeName(attributeType) + "+" +
                                         GameUtils.AttributeValue(attributeType, dataInfo.StatuAttribute.Value);
            if (dataInfo.PetAttribute.Value != 0)
            {
                var _petAttr = "";
                _petAttr = GameUtils.AttributeValue(attributeType, dataInfo.PetAttribute.Value);
                if (dataInfo.PetAttribute.ValueEx != 0)
                {
                    _petAttr += "-" + GameUtils.AttributeValue(attributeType, dataInfo.PetAttribute.ValueEx);
                }
                dataInfo.StatuAttributeStr += "(" + _petAttr + ")";
            }
        }

        private void RenewalStatuePurse(StatueInfoDataModel dataInfo)
        {
            if (m_TbBuildingService == null)
            {
                return;
            }
            var _tbStatue = Table.GetStatue(dataInfo.DataIndex);
            if (_tbStatue == null)
            {
                return;
            }

            var _param = m_TbBuildingService.Param[2];
            var _skillUp = Table.GetSkillUpgrading(_param);
            var _skillUpLevel = _skillUp.GetSkillUpgradingValue(_tbStatue.Level);
            var _skillUpValue = Table.GetSkillUpgrading(_skillUpLevel);
            for (var i = 0; i < 3; i++)
            {
                dataInfo.MaintainItemId[i] = _skillUpValue.GetSkillUpgradingValue(i);
            }

            _param = m_TbBuildingService.Param[3];
            _skillUp = Table.GetSkillUpgrading(_param);
            _skillUpLevel = _skillUp.GetSkillUpgradingValue(_tbStatue.Level);
            _skillUpValue = Table.GetSkillUpgrading(_skillUpLevel);
            for (var i = 0; i < 3; i++)
            {
                dataInfo.MaintainItemCount[i] = _skillUpValue.GetSkillUpgradingValue(i);
            }

            _param = m_TbBuildingService.Param[4];
            _skillUp = Table.GetSkillUpgrading(_param);
            _skillUpLevel = _skillUp.GetSkillUpgradingValue(_tbStatue.Level);
            _skillUpValue = Table.GetSkillUpgrading(_skillUpLevel);
            for (var i = 0; i < 3; i++)
            {
                dataInfo.MaintainItemExp[i] = _skillUpValue.GetSkillUpgradingValue(i);
            }
        }

        private void RenewalStatusCdTime(long time = 0)
        {
            if (time != 0)
            {
                m_StatueDataModel.MaintainCd = Extension.FromServerBinary(time);
            }
            var _flag = PlayerDataManager.Instance.GetFlag(487);
            if (m_StatueDataModel.MaintainCd < Game.Instance.ServerTime)
            {
                _flag = false;
                PlayerDataManager.Instance.SetFlag(487, _flag);
            }
            if (m_StatueDataModel.ChallengeCount == 0)
            {
                m_StatueDataModel.IsShowCd = false;
            }
            else
            {
                m_StatueDataModel.IsShowCd = _flag;
            }
            m_StatueDataModel.MaintainCdFlag = _flag;

            StudyNoticeEffigy();
        }

        private void SendSceneChooseAsking(CityOperationType opt, int buildingIdx, List<int> param = null)
        {
            NetManager.Instance.StartCoroutine(SendSceneChooseAskingCoroutine(opt, buildingIdx, param));
        }

        private IEnumerator SendSceneChooseAskingCoroutine(CityOperationType opt, int buildingIdx, List<int> param)
        {
            using (new BlockingLayerHelper(0))
            {
                var _array = new Int32Array();
                if (null != param)
                {
                    {
                        var _list7 = param;
                        var _listCount7 = _list7.Count;
                        for (var _i7 = 0; _i7 < _listCount7; ++_i7)
                        {
                            var _value = _list7[_i7];
                            {
                                _array.Items.Add(_value);
                            }
                        }
                    }
                }

                var _msg = NetManager.Instance.CityOperationRequest((int)opt, buildingIdx, _array);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        switch (opt)
                        {
                            case CityOperationType.ASSIGNPETINDEX:
                            {
                                var _info = m_StatueDataModel.StatueInfos[param[0]];
                                var _oldPetId = _info.ItemId;
                                if (_oldPetId != -1)
                                {
                                    var _pet = CityManager.Instance.GetPetById(_oldPetId);
                                    _pet.Exdata[3] = (int)PetStateType.Idle;
                                }
                                var _newPetId = param[1];
                                if (param[1] != -1)
                                {
                                    var _pet = CityManager.Instance.GetPetById(_newPetId);
                                    _pet.Exdata[3] = (int)PetStateType.Building;
                                }
                                m_StatueDataModel.StatueInfos[param[0]].ItemId = param[1];

                                RenewalStatueParticularity(_info);

                                var _index = m_StatueDataModel.SelectStatue.Index;
                                m_BuildingData.PetList[_index] = param[1];
                                PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Statue);


                                if (_oldPetId != -1)
                                {
                                    UpgradePetAnimalState(_oldPetId);
                                }
                                if (_newPetId != -1)
                                {
                                    UpgradePetAnimalState(_newPetId);
                                }
                            }
                                break;
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_BuildPetMax)
                    {
                        //该建筑的宠物已满
                        var _e = new ShowUIHintBoard(270006);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard("Error:" + _msg.ErrorCode));
                        Logger.Debug("SendBuildRequestCoroutine error=[{0}]", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("SendBuildRequestCoroutine:MessageState.Timeout");
                }

                //AnalyseNoticeStatue();
            }
        }

        private void SettingsSatueMsg(int index)
        {
            m_StatueDataModel.SelectStatue = m_StatueDataModel.StatueInfos[index];
            var _count = m_StatueDataModel.StatueInfos.Count;
            for (var i = 0; i < _count; i++)
            {
                m_StatueDataModel.StatueInfos[i].IsSelect = i == index;
            }

            //         var tbStatue = Table.GetStatue(StatueDataModel.SelectStatue.DataIndex);
            // 
            //         StatueDataModel.ExpSlider.MaxValues = new List<int> {tbStatue.LevelUpExp};
            //         if (tbStatue.LevelUpExp == 0)
            //         {
            //             StatueDataModel.ExpSlider.BeginValue = 0;
            //         }
            //         else
            //         {
            //             StatueDataModel.ExpSlider.BeginValue = StatueDataModel.SelectStatue.CurExp/(float) tbStatue.LevelUpExp;
            //         }
            //         StatueDataModel.ExpSlider.TargetValue = StatueDataModel.ExpSlider.BeginValue;
        }

        private void RenewalExpNum(StatueInfoDataModel dataModel)
        {
            //  var dataModel = StatueDataModel.StatueInfos[index];
            var _tbStatue = Table.GetStatue(dataModel.DataIndex);

            dataModel.ExpSlider.MaxValues = new List<int> { _tbStatue.LevelUpExp };
            if (_tbStatue.LevelUpExp == 0)
            {
                dataModel.ExpSlider.BeginValue = 0;
            }
            else
            {
                dataModel.ExpSlider.BeginValue = dataModel.CurExp / (float)_tbStatue.LevelUpExp;
            }
            dataModel.ExpSlider.TargetValue = dataModel.ExpSlider.BeginValue;
        }

        private void SettingStatueMsg(StatueInfoDataModel dataInfo, int index)
        {
            var _tableIndex = m_BuildingData.Exdata[index];
            var _petId = m_BuildingData.PetList[index];
            var _exp = (int)m_BuildingData.Exdata64[index];
            dataInfo.DataIndex = _tableIndex;
            RenewalStatueParticularity(dataInfo);
            RenewalStatuePurse(dataInfo);
            dataInfo.ItemId = _petId;
            dataInfo.CurExp = _exp;
        }

        private void DisplayAgoSatueMsg()
        {
            var _index = m_StatueDataModel.SelectStatue.Index;
            _index--;
            if (_index == -1)
            {
                _index = m_StatueDataModel.StatueLimitCount - 1;
            }
            SettingsSatueMsg(_index);
        }

        //--------------------------------------------------------------Arena--------
        private void DisplayHonorExchangedIndex()
        {
            var _index = -1;
            {
                // foreach(var dataModel in ArenaDataModel.RankList)
                var _enumerator14 = (m_ArenaDataModel.RankList).GetEnumerator();
                while (_enumerator14.MoveNext())
                {
                    var _dataModel = _enumerator14.Current;
                    {
                        if (_dataModel.Item.State == 1)
                        {
                            _index++;
                        }
                    }
                }
            }

            //看到中间移动一位
            _index--;

            if (_index < 0)
            {
                _index = 0;
            }
            var _e = new ArenaNotifyLogic(1, _index);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        private void DisplayFollowingSatueMsg()
        {
            var _index = m_StatueDataModel.SelectStatue.Index;
            _index++;
            if (_index == m_StatueDataModel.StatueLimitCount)
            {
                _index = 0;
            }
            SettingsSatueMsg(_index);
        }

        private void StatusCoolDown()
        {
            if (m_StatueDataModel.ChallengeCount == 0)
            {
                //剩余维护次数不足
                var _e = new ShowUIHintBoard(300130);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            if (Game.Instance.ServerTime > m_StatueDataModel.MaintainCd)
            {
                return;
            }
            var _tt = m_StatueDataModel.MaintainCd - Game.Instance.ServerTime;
            var _needDia = (int)Math.Ceiling((_tt.Minutes + 1) * float.Parse(Table.GetClientConfig(572).Value));
            if (_needDia > PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes))
            {
                var _ee = new ShowUIHintBoard(210102);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            var _str = string.Format(GameUtils.GetDictionaryText(270244), _needDia);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _str, "",
                () => { NetManager.Instance.StartCoroutine(CoolDownCoroutine(_needDia)); });
        }

        private void UpgradeHonorList()
        {
            m_ArenaDataModel.MilitaryRank = PlayerDataManager.Instance.GetExData(eExdataDefine.e250);
            m_ArenaDataModel.HonorCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.Honor);
            var controller = UIManager.Instance.GetController(UIConfig.TitleUI);
            if (null != controller)
            {
                controller.CallFromOtherClass("SendMilitary", new[] { (object)m_ArenaDataModel.MilitaryRank });
            }
            var _tbHonor = Table.GetHonor(m_ArenaDataModel.MilitaryRank);
            if (_tbHonor == null)
            {
                return;
            }
            m_ArenaDataModel.NextMilitaryRank = _tbHonor.NextRank;
            if (_tbHonor.NeedHonor == -1)
            {
                m_ArenaDataModel.HonorProgressBar = 1;
            }
            else
            {
                m_ArenaDataModel.HonorProgressBar = 1f / _tbHonor.NeedHonor
                                                    * m_ArenaDataModel.HonorCount;
            }

            if (m_ArenaDataModel.HonorProgressBar >= 1 && _tbHonor.NeedHonor != -1)
            {
                PlayerDataManager.Instance.NoticeData.ArenaMilitary = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.ArenaMilitary = false;
            }
            if (m_ArenaDataModel.MilitaryRank != 0)
            {
                {
                    // foreach(var data in ArenaDataModel.RankList)
                    var _enumerator13 = (m_ArenaDataModel.RankList).GetEnumerator();
                    while (_enumerator13.MoveNext())
                    {
                        var _data = _enumerator13.Current;
                        {
                            _data.Item.State = 1;
                            if (_data.HonorId == m_ArenaDataModel.MilitaryRank)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void UpgradePetAnimalState(int petId)
        {
            if (petId == -1)
            {
                return;
            }

            var _pet = CityManager.Instance.GetPetById(petId);

            PetItemDataModel petItemData = null;

            var _flag = -1;
            var _c = m_StatueDataModel.PetList.Count;
            for (var i = 0; i < _c; i++)
            {
                var _d = m_StatueDataModel.PetList[i];
                if (_d.PetId == petId)
                {
                    _flag = i;
                    break;
                }
            }
            if (_flag == -1)
            {
                return;
            }

            var _data = CityManager.PetItem2DataModel(_pet);
            {
                for (var i = 0; i < _data.Skill.SpecialSkills.Count; i++)
                {
                    if (_data.Skill.SpecialSkills[i].SkillId != -1)
                    {
                        if (Table.GetPetSkill(_data.Skill.SpecialSkills[i].SkillId).Param[0] == 6 &&
                            Table.GetPetSkill(_data.Skill.SpecialSkills[i].SkillId).Param[1] == 1)
                        {
                            _data.BuffIconIdlist[i].Active = true;
                            _data.BuffIconIdlist[i].BuffId = Table.GetPetSkill(_data.Skill.SpecialSkills[i].SkillId).SkillIcon;
                        }
                    }
                }

                if (_data.State == (int)PetStateType.Idle)
                {
                    _data.ShowCheck = true;
                    _data.ShowMask = false;
                }
                else
                {
                    _data.ShowCheck = false;
                    _data.ShowMask = true;
                }
                // foreach(var info in StatueDataModel.StatueInfos)
                var _enumerator10 = (m_StatueDataModel.StatueInfos).GetEnumerator();
                while (_enumerator10.MoveNext())
                {
                    var _info = _enumerator10.Current;
                    {
                        if (_info.IsSelect)
                        {
                            if (_data.ItemId == _info.ItemId)
                            {
                                _data.Checked = true;
                                _data.ShowMask = false;
                                _data.ShowCheck = true;
                            }
                            break;
                        }
                    }
                }
            }

            m_StatueDataModel.PetList[_flag] = _data;
        }

        private void UpgradeList(int rank)
        {
            if (m_ArenaDataModel.CurrentRank == rank)
            {
                return;
            }
            m_ArenaDataModel.CurrentRank = rank;

            var _isFind = false;
            Table.ForeachArenaReward(recoard =>
            {
                if (rank <= recoard.Id)
                {
                    m_ArenaDataModel.RankIndex = recoard.Id;
                    _isFind = true;
                    return false;
                }
                return true;
            });
            if (_isFind == false)
            {
                m_ArenaDataModel.RankIndex = -1;
            }
        }

        private void UpgradeStatusNum()
        {
            if (m_TbBuildingService != null)
            {
                var _tbVip = PlayerDataManager.Instance.TbVip;
                m_StatueDataModel.ChallengeCount = m_TbBuildingService.Param[5] + _tbVip.StatueAddCount -
                                                   PlayerDataManager.Instance.GetExData(400);
                if (m_StatueDataModel.ChallengeCount > 0 && m_StatueDataModel.MaintainCd >= Game.Instance.ServerTime)
                {
                    m_StatueDataModel.IsShowCd = true;
                }
                else
                {
                    m_StatueDataModel.IsShowCd = false;
                }
                StudyNoticeEffigy();
            }
        }

        private void EmployEnginnerService(int type)
        {
            if (!InspectBuildingServe(type))
            {
                return;
            }
            NetManager.Instance.StartCoroutine(EmployEnginnerServiceCoroutine(type));
        }

        private IEnumerator EmployEnginnerServiceCoroutine(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _chellageCount = m_StatueDataModel.ChallengeCount;
                var _selectStaue = m_StatueDataModel.SelectStatue;
                var _ary = new Int32Array();
                _ary.Items.Add(0);
                _ary.Items.Add(_selectStaue.Index);
                _ary.Items.Add(type);
                var _msg = NetManager.Instance.UseBuildService(m_BuildingData.AreaId, m_TbBuilding.ServiceId, _ary);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        _chellageCount--;
                        PlayerDataManager.Instance.SetExData(400, _chellageCount);
                        m_StatueDataModel.ChallengeCount = _chellageCount;
                        var _str = "";
                        switch (type)
                        {
                            case 0:
                            {
                                //膜拜成功
                                _str = GameUtils.GetDictionaryText(270026);
                            }
                                break;
                            case 1:
                            {
                                //打扫成功
                                _str = GameUtils.GetDictionaryText(270027);
                            }
                                break;
                            case 2:
                            {
                                //修复成功
                                _str = GameUtils.GetDictionaryText(270028);
                            }
                                break;
                        }
                        // EventDispatcher.Instance.DispatchEvent(new UIEvent_ArenaFlyAnim(type,
                        //     selectStaue.MaintainItemExp[type]*mTbBuildingService.Param[6]/10000));
                        var _e = new ShowUIHintBoard(_str);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        AdditionRankingExp(_selectStaue, _selectStaue.MaintainItemExp[type]);
                        RenewalExpNum(_selectStaue);
                        StudyNoticeEffigy();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....UseBuildService...ErrorCode....{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....UseBuildService...State....{0}.", _msg.State);
                }
            }
        }

        #endregion

        #region 事件

        private void OnColiseumOppentCellTipEvent(IEvent ievent)
        {
            var _e = ievent as AreanOppentCellClick;

            var _index = _e.Index;
            switch (_e.Type)
            {
                case 0:
                {
                    ColiseumOppentWar(_index);
                }
                    break;
                case 1:
                {
                    ColiseumOppentMsg(_index);
                }
                    break;
            }
        }

        private void OnColiseumWarRecordChangeEvent(IEvent ievent)
        {
            var _e = ievent as ArenaFightRecoardChange;
            var _changeOne = _e.Data;
            if (m_ArenaDataModel.RecoardList.Count > 50)
            {
                //数据太多时，需要删除一部分
                var _list = new List<ArenaRecoardDataModel>(m_ArenaDataModel.RecoardList.ToArray());
                _list.RemoveRange(0, 10);
                m_ArenaDataModel.RecoardList = new ObservableCollection<ArenaRecoardDataModel>(_list);
            }
            var _recoardData = new ArenaRecoardDataModel();
            _recoardData.Name = _changeOne.Name;
            _recoardData.NewRank = _changeOne.NewRank;
            _recoardData.OldRank = _changeOne.OldRank;
            _recoardData.Type = _changeOne.Type;
            FormattingRecord(_recoardData);
            m_ArenaDataModel.RecoardList.Insert(0, _recoardData);
            m_ArenaDataModel.RecordCount = m_ArenaDataModel.RecoardList.Count;
            if (_recoardData.NewRank != -1)
            {
                NetManager.Instance.StartCoroutine(ApplyColiseumMsgCoroutine());
            }
        }

        private void OnColiseumOperateEvent(IEvent ievent)
        {
            var _e = ievent as ArenaOperateEvent;
            switch (_e.Type)
            {
                case 0:
                {
                    NetManager.Instance.StartCoroutine(ApplyColiseumMsgCoroutine());
                }
                    break;
                case 1:
                {
                    DisplayHonorExchangedIndex();
                }
                    break;
            }
        }

        private void OnColiseumPetAnmialRankEvent(IEvent ievent)
        {
            var _e = ievent as ArenaPetListEvent;

            var _petlist = new List<PetItemDataModel>();
            if (_e.IsShow == false)
            {
                return;
            }

            m_StatueDataModel.PetList.Clear();
            PetItemDataModel checkModel = null;
            {
                var _list2 = CityManager.Instance.GetAllPetByFilter(PetListFileterType.Employ);
                var _listCount2 = _list2.Count;
                for (var _i2 = 0; _i2 < _listCount2; ++_i2)
                {
                    var _pet = _list2[_i2];
                    {
                        var _data = CityManager.PetItem2DataModel(_pet);
                        {
                            for (var i = 0; i < _data.Skill.SpecialSkills.Count; i++)
                            {
                                if (_data.Skill.SpecialSkills[i].SkillId != -1)
                                {
                                    if (Table.GetPetSkill(_data.Skill.SpecialSkills[i].SkillId).Param[0] == 6 &&
                                        Table.GetPetSkill(_data.Skill.SpecialSkills[i].SkillId).Param[1] == 1)
                                    {
                                        _data.BuffIconIdlist[i].Active = true;
                                        _data.BuffIconIdlist[i].BuffId =
                                            Table.GetPetSkill(_data.Skill.SpecialSkills[i].SkillId).SkillIcon;
                                    }
                                }
                            }

                            if (_data.State == (int)PetStateType.Idle)
                            {
                                _data.ShowCheck = true;
                                _data.ShowMask = false;
                            }
                            else
                            {
                                _data.ShowCheck = false;
                                _data.ShowMask = true;
                            }
                            // foreach(var info in StatueDataModel.StatueInfos)
                            var _enumerator10 = (m_StatueDataModel.StatueInfos).GetEnumerator();
                            while (_enumerator10.MoveNext())
                            {
                                var _info = _enumerator10.Current;
                                {
                                    if (_info.IsSelect)
                                    {
                                        if (_data.ItemId == _info.ItemId)
                                        {
                                            _data.Checked = true;
                                            _data.ShowMask = false;
                                            _data.ShowCheck = true;
                                            checkModel = _data;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        if (_data.Checked)
                        {
                            continue;
                        }
                        _petlist.Add(_data);
                    }
                }
            }
            _petlist.Sort((x, y) =>
            {
                var _xcount = 0;
                var _ycount = 0;
                {
                    // foreach(var data in x.BuffIconIdlist)
                    var _enumerator16 = (x.BuffIconIdlist).GetEnumerator();
                    while (_enumerator16.MoveNext())
                    {
                        var _data = _enumerator16.Current;
                        {
                            if (_data.Active)
                            {
                                _xcount++;
                            }
                        }
                    }
                }
                {
                    // foreach(var data in y.BuffIconIdlist)
                    var _enumerator17 = (y.BuffIconIdlist).GetEnumerator();
                    while (_enumerator17.MoveNext())
                    {
                        var _data = _enumerator17.Current;
                        {
                            if (_data.Active)
                            {
                                _ycount++;
                            }
                        }
                    }
                }
                if (_xcount > _ycount)
                {
                    return 1;
                }
                if (_xcount == _ycount)
                {
                    return 0;
                }
                return -1;
            });
            {
                var _list18 = _petlist;
                var _listCount18 = _list18.Count;
                for (var _i18 = 0; _i18 < _listCount18; ++_i18)
                {
                    var _data = _list18[_i18];
                    {
                        if (_data.ShowCheck)
                        {
                            m_StatueDataModel.PetList.Insert(0, _data);
                        }
                        else
                        {
                            m_StatueDataModel.PetList.Add(_data);
                            _data.BuffIconIdlist[0].Active = false;
                        }
                    }
                }
            }
            if (checkModel != null)
            {
                m_StatueDataModel.PetList.Insert(0, checkModel);
                checkModel.BuffIconIdlist[0].Active = false;
            }
            m_StatueDataModel.PetCount = m_StatueDataModel.PetList.Count;
        }

        private void OnCityMsgInitialEvent(IEvent ievent)
        {
            m_BuildingData = null;
            {
                // foreach(var buildingData in CityManager.Instance.BuildingDataList)
                var _enumerator12 = (CityManager.Instance.BuildingDataList).GetEnumerator();
                while (_enumerator12.MoveNext())
                {
                    var _buildingData = _enumerator12.Current;
                    {
                        var _typeId = _buildingData.TypeId;
                        var _tbBuild = Table.GetBuilding(_typeId);
                        if (_tbBuild == null)
                        {
                            continue;
                        }
                        if (_tbBuild.Type == 6)
                        {
                            m_BuildingData = _buildingData;
                            break;
                        }
                    }
                }
            }
            if (m_BuildingData != null)
            {
                RenewalBuildDataModel(m_BuildingData);
            }

            StudyNoticeColiseum();
            UpgradeStatusNum();
            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Statue);
        }

        private void OnSceneEvent(IEvent ievent)
        {
            if (State != FrameState.Open)
            {
                return;
            }
            var _e = ievent as UIEvent_CityEvent;

            if (_e == null)
            {
                return;
            }
            if (_e.IntParam == null || _e.IntParam.Count != 1)
            {
                return;
            }

            var _index = _e.IntParam[0];

            if (_index >= m_StatueDataModel.PetList.Count)
            {
                return;
            }


            m_StatueDataModel.PetList[_index].Checked = !m_StatueDataModel.PetList[_index].Checked;

            if (m_StatueDataModel.PetList[_index].Checked)
            {
                var _c = m_StatueDataModel.PetList.Count;
                for (var i = 0; i < _c; i++)
                {
                    var _d = m_StatueDataModel.PetList[i];
                    if (_d.Checked && _d.ItemId != m_StatueDataModel.PetList[_index].ItemId)
                    {
                        _d.Checked = false;
                    }
                }
            }

            switch (_e.StringParam)
            {
                case "ChoosePet":
                {
                    OperatedStatuePetAnimal();
                }
                    break;
            }
        }

        private void OnTipRankEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_OnClickRankBtn;
            {
                // foreach(var data in ArenaDataModel.RankList)
                var _enumerator11 = (m_ArenaDataModel.RankList).GetEnumerator();
                while (_enumerator11.MoveNext())
                {
                    var _data = _enumerator11.Current;
                    {
                        _data.Item.IsSelect = false;
                    }
                }
            }
            m_ArenaDataModel.RankList[_e.Idx].Item.IsSelect = true;
        }
        private void OnExMsgInitialEvent(IEvent ievent)
        {
            var _e = ievent as ExDataInitEvent;
            var _topRank = PlayerDataManager.Instance.GetExData(eExdataDefine.e93);
            if (_topRank > 1000)
            {
                m_ArenaDataModel.TopRank = -1;
            }
            else
            {
                m_ArenaDataModel.TopRank = _topRank;
            }

            var _tbVip = PlayerDataManager.Instance.TbVip;
            var _tbExdata = Table.GetExdata((int)eExdataDefine.e98);
            m_ArenaDataModel.EnterMax = _tbExdata.InitValue;
            m_ArenaDataModel.EnterCount = PlayerDataManager.Instance.GetExData(eExdataDefine.e98);
            _tbExdata = Table.GetExdata((int)eExdataDefine.e99);
            m_ArenaDataModel.BuyMax = _tbExdata.InitValue + _tbVip.PKBuyCount;
            m_ArenaDataModel.BuyCount = PlayerDataManager.Instance.GetExData(eExdataDefine.e99) + _tbVip.PKBuyCount;

            UpgradeHonorList();
            NetManager.Instance.StartCoroutine(ApplyColiseumMsgCoroutine());
        }

        private void OnExMsgInitial64Event(IEvent ievent)
        {
            var _time = PlayerDataManager.Instance.GetExData64((int)Exdata64TimeType.P1vP1CoolDown);
            RenewalWarTime(_time);

            var _time1 = PlayerDataManager.Instance.GetExData64((int)Exdata64TimeType.StatueCdTime);
            RenewalStatusCdTime(_time1);
        }

        private void OnFlagUpgradeEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            switch (_e.Index)
            {
                case 487:
                {
                    RenewalStatusCdTime();
                }
                    break;
            }
        }

        //--------------------------------------------------------------Statue--------
        private void OnSatueOperatedEvent(IEvent ievent)
        {
            var _e = ievent as SatueOperateEvent;
            switch (_e.Type)
            {
                case 1:
                {
                    //挑战成功
                    EmployEnginnerService(2);
                }
                    break;
                case 2:
                {
                    //挑战失败
                    var e1 = new ShowUIHintBoard(270005);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
                    break;
                case 3:
                {
                    PlatformHelper.Event("city", "arenaService", 1);
                    StartInPuzzel();
                }
                    break;
                case 4:
                {
                    PlatformHelper.Event("city", "arenaService", 2);
                    StartInCleanUp();
                }
                    break;
                case 11:
                {
                    //挑战成功
                    EmployEnginnerService(1);
                }
                    break;
                case 12:
                {
                    //挑战失败
                    var e1 = new ShowUIHintBoard(270005);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
                    break;
                case 13:
                {
                    //cool
                    StatusCoolDown();
                }
                    break;
                case 20:
                {
                    PlatformHelper.Event("city", "arenaService", 0);
                    EmployEnginnerService(0);
                }
                    break;
                case 30:
                {
                    RenewalStatusCdTime();
                }
                    break;
                case 41:
                {
                    DisplayAgoSatueMsg();
                }
                    break;
                case 42:
                {
                    DisplayFollowingSatueMsg();
                }
                    break;
                case 100:
                {
                    OnTipStatu(_e.Index);
                }
                    break;
            }
        }

        //-------------------------------------------------------------Event-------------------
        private void OnUpgradeBuildEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_CityUpdateBuilding;

            var _building = CityManager.Instance.GetBuildingByAreaId(_e.Idx);
            if (null == _building)
            {
                return;
            }
            var _bulidId = _building.TypeId;
            var _tbBuilding = Table.GetBuilding(_bulidId);
            if (_tbBuilding == null)
            {
                return;
            }
            if (_tbBuilding.Type != (int)BuildingType.ArenaTemple)
            {
                return;
            }
            RenewalBuildDataModel(_building);
        }

        private void OnUpgradeExMsgEvent(IEvent ievent)
        {
            var _e = ievent as ArenaExdataUpdate;
            var _hasUpdate = false;
            switch (_e.Type)
            {
                case eExdataDefine.e93:
                {
                    _hasUpdate = true;
                    if (_e.Value > 1000)
                    {
                        m_ArenaDataModel.TopRank = -1;
                    }
                    else
                    {
                        m_ArenaDataModel.TopRank = _e.Value;
                    }
                }
                    break;
                case eExdataDefine.e98:
                {
                    _hasUpdate = true;
                    var _tbExdata = Table.GetExdata((int)eExdataDefine.e98);
                    m_ArenaDataModel.EnterMax = _tbExdata.InitValue;
                    m_ArenaDataModel.EnterCount = _e.Value;
                }
                    break;
                case eExdataDefine.e99:
                {
                    _hasUpdate = true;
                    var _tbVip = PlayerDataManager.Instance.TbVip;
                    var _tbExdata = Table.GetExdata((int)eExdataDefine.e99);
                    m_ArenaDataModel.BuyMax = _tbExdata.InitValue + _tbVip.PKBuyCount;
                    m_ArenaDataModel.BuyCount = _e.Value + _tbVip.PKBuyCount;
                }
                    break;
                case eExdataDefine.e400:
                {
                    _hasUpdate = true;
                    UpgradeStatusNum();
                }
                    break;

                case eExdataDefine.e250:
                {
                    UpgradeHonorList();
                }
                    break;
            }
            if (_hasUpdate)
            {
                StudyNoticeColiseum();
            }
        }

        private void OnUpgradeExData64Event(IEvent ievent)
        {
            var _e = ievent as ExData64UpDataEvent;
            switch ((Exdata64TimeType)_e.Key)
            {
                case Exdata64TimeType.P1vP1CoolDown:
                {
                    RenewalWarTime(_e.Value);
                }
                    break;
                case Exdata64TimeType.StatueCdTime:
                {
                    RenewalStatusCdTime(_e.Value);
                }
                    break;
            }
        }

        private void OnUpgradeHonorEvent(IEvent ievent)
        {
            var _e = ievent as Resource_Change_Event;
            if (_e.Type == eResourcesType.Honor)
            {
                m_ArenaDataModel.HonorCount = _e.NewValue;
                UpgradeHonorList();
            }
        }

        private void OnVipLvChangeEvent(IEvent ievent)
        {
            var _tbVip = PlayerDataManager.Instance.TbVip;
            var _tbExdata = Table.GetExdata((int)eExdataDefine.e99);
            m_ArenaDataModel.BuyMax = _tbExdata.InitValue + _tbVip.PKBuyCount;
            m_ArenaDataModel.BuyCount = PlayerDataManager.Instance.GetExData(eExdataDefine.e99) + _tbVip.PKBuyCount;
            if (m_TbBuildingService != null)
            {
                m_StatueDataModel.ChallengeCount = m_TbBuildingService.Param[5] + _tbVip.StatueAddCount -
                                                   PlayerDataManager.Instance.GetExData(eExdataDefine.e400);
            }
            StudyNoticeEffigy();
        }
        private void OnPetLVupEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_PetLevelup;
            var _list = new List<int>();
            {
                // foreach(var data in StatueDataModel.StatueInfos)
                var _enumerator15 = (m_StatueDataModel.StatueInfos).GetEnumerator();
                while (_enumerator15.MoveNext())
                {
                    var data = _enumerator15.Current;
                    {
                        if (data.ItemId == _e.PetId)
                        {
                            _list.Add(data.Index);
                        }
                    }
                }
            }
            if (_list.Count == 0)
            {
                return;
            }
            _list.Add(_e.PetId);
            SendSceneChooseAsking(CityOperationType.ASSIGNPETINDEX, m_BuildingData.AreaId, _list);
        }

        private void OnUpgradeRankEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ApplyAdvancementRankingCoroutine());
        }
        private void OnSeePetAnimalSkillEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SeeSkills;
            var _flag = _e.Flag;
            if (_flag)
            {
                m_ArenaDataModel.ShowPetSkills = true;
                m_ArenaDataModel.PetSkills = _e.Idx;
            }
            else
            {
                m_ArenaDataModel.ShowPetSkills = false;
            }
        }

        //---------------------------Battle---------------------
        private void RefreshMultipleBattle()
        {
            Table.ForeachPVPBattle(record =>
            {
                if (record.Id == 0)
                {
                    LeftCell = m_MultyBattleModel.BattleCells[0];
                    LeftCell.Id = record.Id;
                    if (!GameUtils.CheckIsWeekLoopOk(record.WeekLoop))
                    {
                        LeftCell.IsOpen = false;
                    }
                    else
                    {
                        LeftCell.IsOpen = true;
                    }
                    ShowReward(LeftCell);
                }
                else if (record.Id == 1)
                {
                    RightCell = m_MultyBattleModel.BattleCells[1];
                    RightCell.Id = record.Id;
                    if (!GameUtils.CheckIsWeekLoopOk(record.WeekLoop))
                    {
                        RightCell.IsOpen = false;
                    }
                    else
                    {
                        RightCell.IsOpen = true;
                    }
                    ShowReward(RightCell);
                }
                else
                {
                    return false;
                }
                return true;
            }
                );
        }
        private void OnCheckGetFirstWinRewardEvent(IEvent ievent)
        {
            var e = ievent as CheckGetFirstWinRewardEvent;
            switch (e.Type)
            {
                case 0:
                {
                    if (LeftCell.HasAccept || RightCell.HasAccept)
                    {
                        //奖励已领取
                        GameUtils.ShowHintTip(100001369);
                    }
                    else if (!LeftCell.ShowAccpet && !RightCell.ShowAccpet)
                    {
                        //未达到领取条件
                        GameUtils.ShowHintTip(100001368);
                    }
                    else
                    {
                        if (LeftCell.ShowAccpet)
                        {
                            OnAdoptBattleDecide(LeftCell);
                        }
                        else if (RightCell.ShowAccpet)
                        {
                            OnAdoptBattleDecide(RightCell);
                        }
                    }
                }
                    break;
            }
        }
        private void ShowReward(MultyBattleCellData cell)
        {
            var _battleId = cell.Id;
            var _tbPvpBattle = Table.GetPVPBattle(_battleId);
            var _playerCount = 0;
            var _playerData = PlayerDataManager.Instance;
            FubenRecord tbDungeon = null;
            for (int i = _tbPvpBattle.Fuben.Length - 1; i >=0 ; i--)
            {
                var _dungeonId = _tbPvpBattle.Fuben[i];
                if (_dungeonId != -1)
                {
                    tbDungeon = Table.GetFuben(_dungeonId);
                    if (tbDungeon == null)
                    {
                        continue;
                    }
                    if (_playerData.CheckCondition(tbDungeon.EnterConditionId) == 0 || i == 0)
                    {
                        cell.DungeonId = _dungeonId;
                        var _tbFuben = Table.GetFuben(_dungeonId);
                        if (_tbFuben != null)
                        {
                            var _tbQueue = Table.GetQueue(_tbFuben.QueueParam);
                            _playerCount = _tbQueue.CountLimit;
                            cell.SceneId = _tbFuben.SceneId;
                        }
                        break;
                    }
                }
            }
            cell.PlayerCount = _playerCount;
            cell.CanAccept = PlayerDataManager.Instance.GetFlag(tbDungeon.ScriptId);
            cell.HasAccept = PlayerDataManager.Instance.GetFlag(tbDungeon.FlagId);
            RenewalQueueMsg(cell);
            RenewalStateType(cell);
            RefreshRemuneration(cell);
        }
        private void RenewalStateType(MultyBattleCellData cell)
        {
            if (cell.IsQueue)
            {
                cell.StateType = 1;
            }
            else
            {
                if (GameLogic.Instance && GameLogic.Instance.Scene)
                {
                    var _sceneId = GameLogic.Instance.Scene.SceneTypeId;
                    var _tbScene = Table.GetScene(_sceneId);
                    if (_tbScene != null)
                    {
                        if (_tbScene.FubenId == cell.DungeonId)
                        {
                            cell.StateType = 2;
                            return;
                        }
                    }
                }
                cell.StateType = 0;
            }
        }
        private void RenewalQueueMsg(MultyBattleCellData cell)
        {
            if (QueueInformation.QueueId == -1)
            {
                cell.IsQueue = false;
                return;
            }
            var _tbPvpBattle = Table.GetPVPBattle(cell.Id);
            if (_tbPvpBattle == null)
            {
                return;
            }
            for (var i = _tbPvpBattle.Fuben.Length - 1; i >= 0; i--)
            {
                var _tbDungeon = Table.GetFuben(_tbPvpBattle.Fuben[i]);
                if (_tbDungeon != null)
                {
                    if (QueueInformation.QueueId == _tbDungeon.QueueParam)
                    {
                        cell.IsQueue = true;
                        cell.StartTime = QueueInformation.StartTime;
                        cell.ExpectTime = QueueInformation.StartTime.AddSeconds(QueueInformation.ExpectScend);                    
                        return;
                    }
                }
            }
            cell.IsQueue = false;
        }

        Coroutine TeamTimteRefresh;
        private void RefreshStartTime(MultyBattleCellData cell)
        {
            if (TeamTimteRefresh != null)
            {
                NetManager.Instance.StopCoroutine(TeamTimteRefresh);
            }
            TeamTimteRefresh = NetManager.Instance.StartCoroutine(RefreshTeamTime(cell));
        }

        private IEnumerator RefreshTeamTime(MultyBattleCellData cell)
        {
            while (cell.IsQueue)
            {
                yield return new WaitForSeconds(2f);
                if (!CheckEnterCondition(cell))
                {
                    cell.IsQueue = false;
                    OnCallOffQueue(cell);
                    break;
                }
            }
        }


        private void RefreshRemuneration(MultyBattleCellData cell)
        {
            if (cell == null)
            {
                return;
            }
            var _tbFuben = Table.GetFuben(cell.DungeonId);
            if (_tbFuben == null)
            {
                return;
            }
            var _todayCount = PlayerDataManager.Instance.GetExData(_tbFuben.TodayCountExdata);
            var _todayWinCount = PlayerDataManager.Instance.GetExData(_tbFuben.ResetExdata);
            var _extraCount = PlayerDataManager.Instance.GetExData(_tbFuben.TimeExdata);

            cell.PlayedCount = _todayCount;
            var _rewards = cell.Rewards;
            //首胜奖励
            for (int i = 0; i < _tbFuben.RewardId.Count; ++i)
            {
                var _reward = _rewards[i];
                _reward.ItemId = _tbFuben.RewardId[i];
                _reward.Count = _tbFuben.RewardCount[i];
                //cell.FirstTxt = string.Format(GameUtils.GetDictionaryText(100001315), _rewards[0].Count, _rewards[1].Count, _rewards[2].Count);
                //cell.BagItemData.ItemId = _reward.ItemId;
                //if (!FirstRewardDic.ContainsKey(_reward.ItemId) && _reward.ItemId != -1)
                //{
                //    FirstRewardDic.Add(_reward.ItemId, _reward.Count);
                //}
            }
            //奖励
            #region 计算有问题
            //var _rewardIdx = 0;
            //var _extraRewards = cell.ExtraRewards;
            //var _record = Table.GetSkillUpgrading(_tbFuben.ScanReward[0]);
            //if (_record != null)
            //{
            //    var _reward = _extraRewards[_rewardIdx];
            //    _reward.ItemId = (int)eResourcesType.Honor;
            //    _reward.Count = _record.GetSkillUpgradingValue(_extraCount);
            //    if (_reward.Count > 0)
            //    {
            //        ++_rewardIdx;
            //    }
            //}
            //_record = Table.GetSkillUpgrading(_tbFuben.ScanExp);
            //if (_record != null)
            //{
            //    var _reward = _extraRewards[_rewardIdx];
            //    _reward.ItemId = (int)eResourcesType.ExpRes;
            //    _reward.Count = _record.GetSkillUpgradingValue(_extraCount);
            //    if (_reward.Count > 0)
            //    {
            //        ++_rewardIdx;
            //    }
            //}
            //_record = Table.GetSkillUpgrading(_tbFuben.ScanGold);
            //if (_record != null)
            //{
            //    var _reward = _extraRewards[_rewardIdx];
            //    _reward.ItemId = (int)eResourcesType.GoldRes;
            //    _reward.Count = _record.GetSkillUpgradingValue(_extraCount);
            //    if (_reward.Count > 0)
            //    {
            //        ++_rewardIdx;
            //    }
            //}
            //for (var _imax = _extraRewards.Count; _rewardIdx < _imax; _rewardIdx++)
            //{
            //    _extraRewards[_rewardIdx++].ItemId = -1;
            //}
            #endregion

            var _extraRewards = cell.ExtraRewards;

            //额外奖励
            var _now = Game.Instance.ServerTime;
            var _begin = Table.GetClientConfig(282).ToInt();
            var _end = Table.GetClientConfig(283).ToInt();
            var _beginTime = new DateTime(_now.Year, _now.Month, _now.Day, _begin / 100, _begin % 100, 0);
            var _endTime = new DateTime(_now.Year, _now.Month, _now.Day, _end / 100, _end % 100, 0);
            var _hasExtraReward = _now >= _beginTime && _now <= _endTime;

            _extraRewards[0].ItemId = (int)eResourcesType.Honor;
            _extraRewards[0].Count = 0;

            _extraRewards[1].ItemId = (int)eResourcesType.ExpRes;
            _extraRewards[1].Count = 0;

            _extraRewards[2].ItemId = (int)eResourcesType.GoldRes;
            _extraRewards[2].Count = 0;

            //荣誉
            var _record = Table.GetSkillUpgrading(_tbFuben.ScanReward[0]);
            if (_record != null)//正常
            {
                _extraRewards[0].Count = _record.GetSkillUpgradingValue(_todayWinCount);
            }

            _record = Table.GetSkillUpgrading(_tbFuben.ScanReward[0]);
            if (_hasExtraReward && _record != null)//额外时间段奖励
            {
                _extraRewards[0].Count += _record.GetSkillUpgradingValue(_extraCount);
            }

            //经验
            _record = Table.GetSkillUpgrading(_tbFuben.ScanExp);
            if (_record != null)
            {

                _extraRewards[1].Count = _record.GetSkillUpgradingValue(_todayCount);//正常
                if (_hasExtraReward)
                {
                    _extraRewards[1].Count += _record.GetSkillUpgradingValue(_extraCount);//额外时间段奖励
                }
            }

            //金币
            _record = Table.GetSkillUpgrading(_tbFuben.ScanGold);
            if (_record != null)
            {
                _extraRewards[2].Count = _record.GetSkillUpgradingValue(_todayCount);//正常
                if (_hasExtraReward)
                {
                    _extraRewards[2].Count += _record.GetSkillUpgradingValue(_extraCount);//额外时间段奖励
                }
            }            
        }
        private void AnalyseGiveNoticeBattle()
        {
            //var _show = false;
            //var _c = m_MultyBattleModel.BattleCells.Count;
            //for (var i = 0; i < _c; i++)
            //{
            //    var _cell = m_MultyBattleModel.BattleCells[i];
            //    if (_cell == null)
            //    {
            //        continue;
            //    }
            //    var _flag = false;
            //    var _record = Table.GetPVPBattle(_cell.Id);
            //    for (var j = 0; j < _record.Fuben.Length; j++)
            //    {
            //        var _tbFuben = Table.GetFuben(_record.Fuben[j]);
            //        if (_tbFuben != null)
            //        {
            //            var _condition = PlayerDataManager.Instance.CheckCondition(_tbFuben.EnterConditionId);
            //            if (_condition == 0)
            //            {
            //                var _exdataCount = PlayerDataManager.Instance.GetExData(_tbFuben.TodayCountExdata);
            //                if (_exdataCount == 0)
            //                {
            //                    _flag = true;
            //                    _show = true;
            //                    break;
            //                }
            //                if (!PlayerDataManager.Instance.GetFlag(_tbFuben.FlagId))
            //                {
            //                    _flag = true;
            //                    _show = true;
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //    _cell.ShowNotice = _flag;
            //}
            if (LeftCell.IsOpen && CheckEnterCondition(LeftCell))
            {
                LeftCell.ShowNotice = true;
            }
            else
            {
                LeftCell.ShowNotice = false;
            }
            if (RightCell.IsOpen && CheckEnterCondition(RightCell))
            {
                RightCell.ShowNotice = true;
            }
            else
            {
                RightCell.ShowNotice = false;
            }
            if (!LeftCell.ShowNotice && !RightCell.ShowNotice)
            {
                m_MultyBattleModel.RedPointNotice = false;
            }
            else
            {
                m_MultyBattleModel.RedPointNotice = true;
            }
        }
        private void OnRenewalQueueEvent(IEvent ievent)
        {
            if (LeftCell.IsOpen)
            {
                RenewalQueueMsg(LeftCell);
                RenewalStateType(LeftCell);
            }
            if (RightCell.IsOpen)
            {
                RenewalQueueMsg(RightCell);
                RenewalQueueMsg(RightCell);
            }
        }
        private void OnMultiFightOperateEvent(IEvent ievent)
        {
            var _e = ievent as MultyBattleOperateEvent;
            switch (_e.Type)
            {
                case 0:
                {            
                    if (!LeftCell.IsQueue)
                    {
                        OnBeginQueue(LeftCell);
                    }
                    else
                    {
                        OnCallOffQueue(LeftCell);
                    }
                }
                    break;
                case 1:
                {
                    if (!RightCell.IsQueue)
                    {
                        OnBeginQueue(RightCell);
                    }
                    else
                    {
                        OnCallOffQueue(RightCell);
                    }
                }
                    break;
            }
        }
        private void OnBeginQueue(MultyBattleCellData cell)
        {
            if (!CheckEnterCondition(cell))
            {
                GameUtils.ShowHintTip(200005012);
                return;
            }
            if (PlayerDataManager.Instance.IsInPvPScnen())
            {
                GameUtils.ShowHintTip(456);
                return;
            }

            var _tbScene = Table.GetScene(cell.DungeonId);
            if (_tbScene == null)
            {
                return;
            }
            var _level = PlayerDataManager.Instance.GetLevel();
            if (_tbScene.LevelLimit > _level)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(300102));
                return;
            }
            var _teamData = UIManager.Instance.GetController(UIConfig.TeamFrame).GetDataModel("") as TeamDataModel;

            if (_teamData != null)
            {
                if (_teamData.TeamList[0].Guid != 0 && _teamData.TeamList[0].Guid != ObjManager.Instance.MyPlayer.GetObjId())
                {
                    //只有队长才能进行此操作
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(440));
                    return;
                }

                //检查其他人的等级
            }

            var _isEnter = PlayerDataManager.Instance.CheckDungeonEnter(cell.DungeonId);
            if (!_isEnter)
            {
                return;
            }

            //如果在排其它的队
            var _tbDungeon = Table.GetFuben(cell.DungeonId);
            var _queueUpData = PlayerDataManager.Instance.PlayerDataModel.QueueUpData;
            if (_queueUpData.QueueId != -1 && _queueUpData.QueueId != _tbDungeon.QueueParam)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 41004, "", () =>
                {
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                    NetManager.Instance.StartCoroutine(QueueGetOnCoroutine(cell));
                });
                return;
            }
            NetManager.Instance.StartCoroutine(QueueGetOnCoroutine(cell));
        }
        private void OnCallOffQueue(MultyBattleCellData cell)
        {
            NetManager.Instance.StartCoroutine(QueueGetOnCancelCoroutine(cell));
        }
        private void OnAdoptBattleDecide(MultyBattleCellData cell)
        {
            NetManager.Instance.StartCoroutine(AdopttBattleDecideCoroutine(cell));
        }
        private IEnumerator AdopttBattleDecideCoroutine(MultyBattleCellData cell)
        {
            using (new BlockingLayerHelper(0))
            {
                var _dungeonId = cell.DungeonId;
                var _msg = NetManager.Instance.AcceptBattleAward(_dungeonId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        FirstRewardDic.Clear();
                        var _tbFuben = Table.GetFuben(_dungeonId);
                        for (int i = 0; i < _tbFuben.RewardId.Count; ++i)
                        {
                            if (!FirstRewardDic.ContainsKey(_tbFuben.RewardId[i]) && _tbFuben.RewardId[i] != -1)
                            {
                                FirstRewardDic.Add(_tbFuben.RewardId[i], _tbFuben.RewardCount[i]);
                            }
                        }
                        var e = new ShowItemsArguments
                        {
                            Items = FirstRewardDic
                        };
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ShowItemsFrame,e));
                        //GameUtils.UseItem(cell.BagItemData);
                        cell.HasAccept = true;
                        
                        if (_tbFuben != null)
                        {
                            PlayerDataManager.Instance.SetFlag(_tbFuben.FlagId);

                        }
                        //RefreshRemuneration();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....AcceptBattleAward.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....AcceptBattleAward.......{0}.", _msg.State);
                }
            }
        }
        private IEnumerator QueueGetOnCoroutine(MultyBattleCellData cell)
        {
            using (new BlockingLayerHelper(0))
            {
                var _tbDungeon = Table.GetFuben(cell.DungeonId);
                var _msg = NetManager.Instance.MatchingStart(_tbDungeon.QueueParam);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.InitQueneData(_msg.Response.Info);
                        cell.IsQueue = true;
                        cell.StartTime = QueueInformation.StartTime;
                        cell.ExpectTime = QueueInformation.StartTime.AddSeconds(QueueInformation.ExpectScend);
                        RefreshStartTime(cell);
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
        private IEnumerator QueueGetOnCancelCoroutine(MultyBattleCellData cell)
        {
            using (new BlockingLayerHelper(0))
            {
                var _dungeonId = cell.DungeonId;
                var _tbDungeon = Table.GetFuben(_dungeonId);
                var _msg = NetManager.Instance.MatchingCancel(_tbDungeon.QueueParam);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        QueueInformation.QueueId = -1;
                        cell.IsQueue = false;
                        cell.StateType = 0;
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_WindowShowDungeonQueue(cell.StartTime,
                            QueueInformation.QueueId));
                        EventDispatcher.Instance.DispatchEvent(new QueueCanceledEvent());
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
        private QueueUpDataModel QueueInformation
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.QueueUpData; }
        }
        private bool CheckEnterCondition(MultyBattleCellData cell)
        {
            var now = Game.Instance.ServerTime;
            var _tbFuben = Table.GetFuben(cell.DungeonId);                
              
            for (int i = 0; i < _tbFuben.OpenTime.Count; i++)
            {
                var startTime = new DateTime(now.Year, now.Month, now.Day, _tbFuben.OpenTime[i] / 100, _tbFuben.OpenTime[i] % 100, 0);
                var canEnterTime = startTime.AddMinutes(_tbFuben.CanEnterTime);
                if (now <=canEnterTime && now>=startTime)
                {
                    return true;
                }            
            }
            return false;
        }
        private void OnMarkRenwalEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            var _c = m_MultyBattleModel.BattleCells.Count;
            for (var i = 0; i < _c; i++)
            {
                var _cell = m_MultyBattleModel.BattleCells[i];
                if (_cell.FlagReceive == _e.Index)
                {
                    AnalyseGiveNoticeBattle();
                    return;
                }
            }
        }
        #endregion
    

 

  

  

   

  

   

   
    }
}
