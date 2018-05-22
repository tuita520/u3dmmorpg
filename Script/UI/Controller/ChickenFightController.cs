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
using Shared;

namespace ScriptController
{
    public class ChickenFightController : IControllerBase
    {

        private QueueUpDataModel QueueUpData
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.QueueUpData; }
        }
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

        private FubenRecord fuben = null;
        private ChickenFightDataModel DataModel;
        private FieldMissionPlayerRankDataModel MissionDataModel;

        public ChickenFightController()
        {
            CleanUp();
            fuben = Table.GetFuben(30000);
            EventDispatcher.Instance.AddEventListener(ChickenFightChoosePageEvent.EVENT_TYPE, OnChoosePageEvent);
        }

        #region 事件
        private void OnChoosePageEvent(IEvent ievent)
        {
            var e = ievent as ChickenFightChoosePageEvent;
            if (e == null)
            {
                return;
            }
            switch (e.PageId)
            {
                case 0://开始排队
                    OnBeginQueue();
                    break;
                case 1://停止排队
                    NetManager.Instance.StartCoroutine(MatchingCancelCoroutine(fuben.QueueParam));
                    break;
                case 2://规则
                    DataModel.ViewState = 1;

                    break;
                case 3://规则关闭
                    DataModel.ViewState = 0;
                    break;
                case 4://排行
                    NetManager.Instance.StartCoroutine(ApplyRankCoroutine());
                    DataModel.ViewState = 2;
                    break;
                case 5://关闭排行
                    DataModel.ViewState = 0;
                    break;
                case 6://去争夺

                    break;
            }
        }
    
        private IEnumerator ApplyRankCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ApplyChickenRankData(ObjManager.Instance.MyPlayer.GetObjId());
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DataModel.PlayerRankList.Clear();
                        if (msg.Response.RankList != null)
                        {
                            for (int i = 0; i < msg.Response.RankList.Count; i++)
                            {
                                FieldMissionPlayerRankDataModel item = new FieldMissionPlayerRankDataModel();
                                item.Id = msg.Response.RankList[i].Guid;
                                item.Idx = msg.Response.RankList[i].Rank.ToString();
                                item.Fight = msg.Response.RankList[i].FightValue;
                                item.Score = msg.Response.RankList[i].Score;
                                item.Role = msg.Response.RankList[i].Profession;
                                item.Level = msg.Response.RankList[i].Level;
                                item.Name = msg.Response.RankList[i].Name;
                                DataModel.PlayerRankList.Add(item);
                            }
                        }
                        if (msg.Response.MyRank != null)
                        {
                            MissionDataModel.Id = msg.Response.MyRank.Guid;
                            MissionDataModel.Idx = msg.Response.MyRank.Rank.ToString();
                            MissionDataModel.Fight = msg.Response.MyRank.FightValue;
                            MissionDataModel.Score = msg.Response.MyRank.Score;
                            MissionDataModel.Role = msg.Response.MyRank.Profession;
                            MissionDataModel.Level = msg.Response.MyRank.Level;
                            MissionDataModel.Name = msg.Response.MyRank.Name;
                        }
                        else
                        {
                         //   MissionDataModel.Id = msg.Response.MyRank.Guid;
                         //   MissionDataModel.Idx = msg.Response.MyRank.Rank.ToString();
                            MissionDataModel.Fight = PlayerDataManager.Instance.PlayerDataModel.Attributes.FightValue;
                            MissionDataModel.Score = 0;
                            MissionDataModel.Role = PlayerDataManager.Instance.PlayerDataModel.CharacterBase.RoleId;
                            MissionDataModel.Level = PlayerDataManager.Instance.GetLevel();
                            MissionDataModel.Name = PlayerDataManager.Instance.GetName();
                        }
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....ApplyRankCoroutine.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....ApplyRankCoroutine.......{0}.", msg.State);
                }
            }
        }
        private string GetLadderName(int ladder, int pro)
        {
            string defaultName = "";
            var tabTrans = Table.GetTransmigration(ladder);
            //var tabActor = Table.GetActor(pro);
            if (null != tabTrans)
            {
                switch (pro)
                {
                    case 0: // 剑士
                        defaultName = tabTrans.zsRebornName;
                        break;
                    case 1: // 法师
                        defaultName = tabTrans.fsRebornName;
                        break;
                    case 2: // 弓箭手
                        defaultName = tabTrans.gsRebornName;
                        break;
                    //case 3: // 游侠
                    //    defaultName = tabTrans.gsRebornName;
                    //    break;
                }
            }

            return defaultName;
        }
        private void OnBeginQueue()
        {
            
            if (!CheckEnterCondition())
            {
                GameUtils.ShowHintTip(200005012);
                return;
            }
            if (PlayerDataManager.Instance.IsInPvPScnen())
            {
                GameUtils.ShowHintTip(456);
                return;
            }
            var _tbScene = Table.GetScene(fuben.SceneId);
            if (_tbScene == null)
                return;
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
            }
            var _isEnter = PlayerDataManager.Instance.CheckDungeonEnter(fuben.Id);
            if (!_isEnter)
                return;
            //如果在排其它的队
            var _queueUpData = PlayerDataManager.Instance.PlayerDataModel.QueueUpData;
            if (_queueUpData.QueueId != -1 && _queueUpData.QueueId != fuben.QueueParam)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 41004, "", () =>
                {
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_CloseDungeonQueue(1));
                    NetManager.Instance.StartCoroutine(MatchingStartCoroutine(fuben.QueueParam));
                });
                return;
            }
            NetManager.Instance.StartCoroutine(MatchingStartCoroutine(fuben.QueueParam));
        }
        private bool CheckEnterCondition()
        {
            return true;
            var now = Game.Instance.ServerTime;
            for (int i = 0; i < fuben.OpenTime.Count; i++)
            {
                var startTime = new DateTime(now.Year, now.Month, now.Day, fuben.OpenTime[i] / 100, fuben.OpenTime[i] % 100, 0);
                var canEnterTime = startTime.AddMinutes(fuben.CanEnterTime);
                if (now <= canEnterTime && now >= startTime)
                {
                    return true;
                }
            }
            return false;
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
                        DataModel.QueueState = 1;
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

        private IEnumerator MatchingCancelCoroutine(int queueId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.MatchingCancel(queueId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DataModel.QueueState = 0;
                        QueueUpData.QueueId = -1;
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_WindowShowDungeonQueue(Game.Instance.ServerTime,-1));
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
                    var content = string.Format(GameUtils.GetDictionaryText(dicId), names);
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(content));
                }
                return true;
            }
            return false;
        }
        #endregion
        #region 固有函数
        public void CleanUp()
        {
            DataModel = new ChickenFightDataModel();
            MissionDataModel = new FieldMissionPlayerRankDataModel();
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
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }
        public void RefreshData(UIInitArguments data)
        {
            DataModel.ViewState = 0;
            DataModel.QueueState = 0;

            FubenRecord fuben = Table.GetFuben(30000);
            SceneRecord scene = Table.GetScene(fuben.SceneId);

            var _queueUpData = PlayerDataManager.Instance.PlayerDataModel.QueueUpData;
            if (_queueUpData.QueueId != -1 && _queueUpData.QueueId == fuben.QueueParam)
            {
                DataModel.QueueState = 1;
            }
           
            if (DataModel.Reward.Count <= 0)
            {
           
                DataModel.NeedLevel = scene.LevelLimit;
                for (int i = 0; i < fuben.RewardId.Count && i < fuben.RewardCount.Count; i++)
                {
                    if (fuben.RewardId[i] > 0 && fuben.RewardCount[i] > 0)
                    {
                        ItemIdDataModel model = new ItemIdDataModel();
                        model.ItemId = fuben.RewardId[i];
                        model.Count = fuben.RewardCount[i];
                        DataModel.Reward.Add(model);
                        
                    }
                
                }
            }
         

            if (DataModel.TotalRewardRank == null)
            {
                DataModel.TotalRewardRank =
                    new System.Collections.ObjectModel.ObservableCollection<GongxianJianliItem>();

                for (int i = 0; ; i++)
                {
                    CheckenFinalRewardRecord dcrr = Table.GetCheckenFinalReward(i + 1);
                    if (dcrr == null)
                    {
                        break;
                    }
                    string[] rank = dcrr.Rank.Split('|');
                    GongxianJianliItem jiangliItem = new GongxianJianliItem();
                    if (rank.Length > 0)
                    {
                        if (int.Parse(rank[0]) <= 3)
                        {
                            jiangliItem.NubIcon = dcrr.RankIcon;
                        }
                        else
                        {
                            jiangliItem.Numb = string.Format("{0} - {1}", dcrr.Rank[0], dcrr.Rank[dcrr.Rank.Length - 1]);
                        }

                        for (int j = 0; j < dcrr.RankItemCount.Length; j++)
                        {
                            if (dcrr.RankItemID[j] > 0)
                            {
                                GongxianJianliItem.JiangliItem item = new GongxianJianliItem.JiangliItem();
                                item.IconId = dcrr.RankItemID[j];
                                ItemBaseRecord dbd = Table.GetItemBase(dcrr.RankItemID[j]);
                                item.Icon = dbd.Icon;
                                item.count = dcrr.RankItemCount[j].ToString();
                                jiangliItem.Rewards.Add(item);
                            }
                        }
                    }
                    DataModel.TotalRewardRank.Add(jiangliItem);
                }
            }
                
          
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "Chicken")
                return DataModel;
            else
                return MissionDataModel;
        }

        public FrameState State { get; set; }
        #endregion
    }
}
