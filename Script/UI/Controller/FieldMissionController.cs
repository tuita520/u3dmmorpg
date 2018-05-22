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

namespace ScriptController
{
    public class FieldMissionController : IControllerBase
    {
        private FieldMissionDataModel DataModel;  
        public FieldMissionController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(FieldActivityEvent.EVENT_TYPE, OnFieldActivity);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpdata);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagDataUpdate);
            EventDispatcher.Instance.AddEventListener(ActiveTaskInfoEvent.EVENT_TYPE, OnRecvTaskEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnApplyActiveInfo);
            
        }
  
        #region 固有函数

        public void CleanUp()
        {
            DataModel = new FieldMissionDataModel();
            var strReward = Table.GetClientConfig(1217).Value;
            var rewards = strReward.Split('|');
            foreach (var v in rewards)
            {
                var id = int.Parse(v);
                ItemIdDataModel item = new ItemIdDataModel();
                item.ItemId = id;
                item.Count = 1;
                DataModel.Reward.Add(item);
            }

        }

        public void RefreshData(UIInitArguments data)
        {
      
        }
        #endregion
        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
            NetManager.Instance.StartCoroutine(ApplyActiveInfo());
        }

        private void OnApplyActiveInfo(IEvent e)
        {
            NetManager.Instance.StartCoroutine(ApplyActiveInfo());
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        public void Close()
        {
        }

        public void Tick()
        {
        }

        private void OnExDataUpdata(IEvent ievent)
        {
            ExDataUpDataEvent e = ievent as ExDataUpDataEvent;
            if (e == null)
                return;
            bool bCheck = false;
            for (int i = 0; i < DataModel.PlayerData.Count; i++)
            {
                if (DataModel.PlayerData[i].ExIdx == e.Key && DataModel.PlayerData[i].Stat == 0)
                {
                    DataModel.PlayerData[i].Count = e.Value;
                    if (DataModel.PlayerData[i].Count == DataModel.PlayerData[i].MaxCount)
                    {
                        DataModel.PlayerData[i].Stat = 1;
                        bCheck = true;
                    }
                }
            }
            if (bCheck)
                CheckNotic();
        }

       
        private void OnFlagDataUpdate(IEvent ievent)
        {
            FlagUpdateEvent e = ievent as FlagUpdateEvent;
            if (e == null)
                return;
            for (int i = 0; i < DataModel.PlayerData.Count; i++)
            {
                if (DataModel.PlayerData[i].Flag == e.Index && e.Value == true)
                {
                    DataModel.PlayerData[i].Stat = 2;
                    CheckNotic();
                    return;
                }
            }
            for (int i = 0; i < DataModel.AllianceData.Count; i++)
            {
                if (DataModel.AllianceData[i].Flag == e.Index && e.Value == true)
                {
                    DataModel.AllianceData[i].Stat = 2;
                    CheckNotic();
                    return;
                }
            }
        }

        private void OnFieldActivity(IEvent ievent)
        {
            FieldActivityEvent e = ievent as FieldActivityEvent;
            if (e == null)
                return;
            if(e.Param1<10)
                DataModel.SelectUI = e.Param1;
            switch (e.Param1)
            {
                case 1:
                {//争夺目标
                
                    DataModel.MissionTab = e.Param2;
                }
                    break;
                case 3:
                    DataModel.RankTab = e.Param2;
                    break;
                
                case 10:
                {
                    NetManager.Instance.StartCoroutine(ApplyGetRewardCorount(e.Param2));
                }
                    break;
            }
        }

        private IEnumerator ApplyGetRewardCorount(int id)
        {
            var msg = NetManager.Instance.ApplyFieldActivityReward(id);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State != MessageState.Reply)
            {
                yield break;
            }
            if (msg.ErrorCode != (int)ErrorCodes.OK)
            {
                UIManager.Instance.ShowNetError(msg.ErrorCode);
                yield break;
            }
            for (int i = 0; i < DataModel.PlayerData.Count; i++)
            {
                if (DataModel.PlayerData[i].Id == id)
                {
                    DataModel.PlayerData[i].Stat = 2;
                }
            }
            for (int i = 0; i < DataModel.AllianceData.Count; i++)
            {
                if (DataModel.AllianceData[i].Id == id)
                {
                    DataModel.AllianceData[i].Stat = 2;
                }
            }
            CheckNotic();
        }

        private void OnRecvTaskEvent(IEvent ievent)
        {
            ActiveTaskInfoEvent e = ievent as ActiveTaskInfoEvent;
            if (e == null)
                return;
            OnRecvTask(e.info);
        }
        private void CheckNotic()
        {
            DataModel.NoticPlayer = false;
            DataModel.NoticAlliance = false;
            for (int i = 0; i < DataModel.PlayerData.Count; i++)
            {
                if (DataModel.PlayerData[i].Stat == 1)
                {
                    DataModel.NoticPlayer = true;
                    break ;
                }
            }

            for (int i = 0; i < DataModel.AllianceData.Count; i++)
            {
                if (DataModel.AllianceData[i].Stat == 1)
                {
                    DataModel.NoticAlliance = true;
                    break;
                }
            }
        }
        private void OnRecvTask(DBActiveTask msg)
        {
            DataModel.AllianceData.Clear();
            DataModel.PlayerData.Clear();
            DataModel.AllianceRankData.Clear();
            DataModel.PlayerRankData.Clear();
            DataModel.Idx = -1;
            DataModel.AllianceIdx = -1;
            {
                DataModel.myRank.Name = PlayerDataManager.Instance.GetName();
                DataModel.myRank.Level = PlayerDataManager.Instance.GetLevel();
                DataModel.myRank.Fight = PlayerDataManager.Instance.PlayerDataModel.Attributes.FightValue;
                DataModel.myRank.Score = PlayerDataManager.Instance.GetExData(949);
                DataModel.myRank.Role = PlayerDataManager.Instance.GetRoleId();
                DataModel.myRank.Idx = GameUtils.GetDictionaryText(100000640);
            }
            {
                DataModel.myAllianceRank.Idx = GameUtils.GetDictionaryText(100000640);

                //DataModel.myAllianceRank.AllianceId = m.AllianceId;
                //DataModel.myAllianceRank.Fight = m.Fight;
                //DataModel.myAllianceRank.Level = m.Level;

                //DataModel.myAllianceRank.Flags = m.Flags;
                //DataModel.myAllianceRank.Name = m.Name;
                //DataModel.myAllianceRank.Score = m.Score;
            }




            foreach (var id in msg.pTaskIDs)
            {//玩家任务
                var tb = Table.GetObjectTable(id);
                if (tb == null)
                    continue;
                FieldMissionBaseDataModel tmp = new FieldMissionBaseDataModel();
                tmp.Id = id;
                tmp.ExIdx = tb.ExData;
                tmp.Count = PlayerDataManager.Instance.GetExData(tb.ExData);
                tmp.MaxCount = tb.NeedCount;
                tmp.Flag = tb.IsGet;
                if (PlayerDataManager.Instance.GetFlag(tb.IsGet) == true)
                {//已领取
                    tmp.Stat = 2;
                }
                else
                {
                    tmp.Stat = tmp.Count >= tmp.MaxCount ? 1 : 0;
                }
                for (int i = 0; i < tb.Reward.Length && i < tb.RewardNum.Length && i < tmp.Rewards.Count; i++)
                {
                    tmp.Rewards[i].ItemId = tb.Reward[i];
                    tmp.Rewards[i].Count = tb.RewardNum[i];

                }



                DataModel.PlayerData.Add(tmp);
            }
            foreach (var id in msg.aTaskIDs)
            {//战盟任务
                var tb = Table.GetObjectTable(id);
                if (tb == null)
                    continue;
                FieldMissionBaseDataModel tmp = new FieldMissionBaseDataModel();
                tmp.Id = id;
                tmp.ExIdx = tb.ExData;
                tmp.Flag = tb.IsGet;
                tmp.MaxCount = tb.NeedCount;

                for (int i = 0; i < tb.Reward.Length && i < tb.RewardNum.Length && i < tmp.Rewards.Count; i++)
                {
                    tmp.Rewards[i].ItemId = tb.Reward[i];
                    tmp.Rewards[i].Count = tb.RewardNum[i];
                }

                DataModel.AllianceData.Add(tmp);
            }
            List<FieldMissionAllianceRankDataModel> l = new List<FieldMissionAllianceRankDataModel>();
            foreach (var v in msg.AllianceTaskList)
            {//战盟任务信息
                FieldMissionAllianceRankDataModel tmp = new FieldMissionAllianceRankDataModel();
                tmp.AllianceId = v.Key;
                tmp.Name = v.Value.Name;
                tmp.Score = v.Value.Score;
                tmp.Fight = v.Value.Fight;
                tmp.Flags = v.Value.Flags;
                tmp.Level = v.Value.Level;
                if (tmp.AllianceId == PlayerDataManager.Instance.GetExData(eExdataDefine.e282))
                {
                    var info = v.Value;
                    for (int i = 0; i < DataModel.AllianceData.Count; i++)
                    {
                        var m = DataModel.AllianceData[i];
                        if (info.TaskList.ContainsKey(m.Id))
                        {
                            m.Count = info.TaskList[m.Id].Count;
                            if (PlayerDataManager.Instance.GetFlag(m.Flag) == true)
                            {//已领取
                                m.Stat = 2;
                            }
                            else
                            {
                                m.Stat = info.TaskList[m.Id].Count >= info.TaskList[m.Id].Need ? 1 : 0;
                            }
                        }
                    }
                }
                l.Add(tmp);
            }
            l.Sort((a, b) =>
            {
                if (b.Flags > a.Flags)
                    return 1;
                if (b.Flags < a.Flags)
                    return -1;
                return b.Score - a.Score;
            });
            for (int i = 0; i < l.Count; i++)
            {
                var m = l[i];
                m.Idx = (i + 1).ToString();
                if (m.AllianceId == PlayerDataManager.Instance.GetExData(eExdataDefine.e282))
                {
                    DataModel.AllianceIdx = i;
                    DataModel.myAllianceRank.AllianceId = m.AllianceId;
                    DataModel.myAllianceRank.Fight = m.Fight;
                    DataModel.myAllianceRank.Level = m.Level;
                    DataModel.myAllianceRank.Idx = m.Idx;
                    DataModel.myAllianceRank.Flags = m.Flags;
                    DataModel.myAllianceRank.Name = m.Name;
                    DataModel.myAllianceRank.Score = m.Score;
                }
                DataModel.AllianceRankData.Add(m);
            }

            for (int i = 0; i < msg.PlayerTaskList.Count; i++)
            {//玩家积分排行
                var v = msg.PlayerTaskList[i];
                FieldMissionPlayerRankDataModel data = new FieldMissionPlayerRankDataModel();
                data.Id = v.guid;
                data.Role = v.role;
                data.Name = v.name;
                data.Score = v.score;
                data.Idx = (i + 1).ToString();
                data.Level = v.level;
                data.Fight = v.fight;
                if (data.Id == PlayerDataManager.Instance.CharacterGuid)
                {
                    DataModel.Idx = i + 1;
                    DataModel.myRank.Idx = (i + 1).ToString();
                }
                DataModel.PlayerRankData.Add(data);
            }
            CheckNotic();
        }
        private IEnumerator ApplyActiveInfo()
        {
            var msg = NetManager.Instance.ClientApplyActiveInfo(PlayerDataManager.Instance.ServerId, PlayerDataManager.Instance.GetExData(eExdataDefine.e282));
            yield return msg.SendAndWaitUntilDone();
            if (msg.State != MessageState.Reply)
            {
                yield break;
            }
            if (msg.ErrorCode != (int)ErrorCodes.OK)
            {
                UIManager.Instance.ShowNetError(msg.ErrorCode);
                yield break;
            }
            OnRecvTask(msg.Response);
        }
    }
}
