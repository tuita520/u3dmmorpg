#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;

#endregion

namespace ScriptController
{
    public class ClimbingTowerController : IControllerBase
    {
        private object mTrigger;
        private List<ClimbingTowerSweepReward> temp;     
        public ClimbingTowerController()
        {
            temp = new List<ClimbingTowerSweepReward>();
            EventDispatcher.Instance.AddEventListener(TowerFloorClickEvent.EVENT_TYPE, OnClickCell);
            EventDispatcher.Instance.AddEventListener(TowerBtnClickEvent.EVENT_TYPE, OnTowerEvent);
            EventDispatcher.Instance.AddEventListener(TowerRefreshEvent.EVENT_TYPE, OnTowerRefresh);

            CleanUp();
        }

        private ClimbingTowerDataModel DataModel;
        public void CleanUp()
        {
            DataModel = new ClimbingTowerDataModel();
        }



        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }
        public void OnShow()
        {
            //temp.Clear();
            InitLeft();
            InitReward(DataModel.NextFloor);
        }
        public void Close()
        {
        }

        public void Tick()
        {
        }
        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }

        public void RefreshData(UIInitArguments data)
        {
        
      
        }

        private void ResetNextFloor()
        {
            DataModel.NextFloor = DataModel.CurFloor;
            var tb = Table.GetClimbingTower(DataModel.CurFloor+1);
            if (tb != null)
            {
                DataModel.NextFloor = DataModel.CurFloor + 1;
            }
        }
        private void InitLeft()
        {
            int max = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e621);
            int cur = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e623);
            int sweep = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e622);
            var tbTower = Table.GetClimbingTower(cur);
            if (tbTower != null)
            {
                DataModel.CanSweep = tbTower.SweepFloor>0 && sweep>0;
            }
            else
            {
                DataModel.CanSweep = false;
            }
            DataModel.CurFloor = cur;
            ResetNextFloor();
            DataModel.MaxFloor = max;
            DataModel.SelectIdx = DataModel.NextFloor;
            DataModel.SweepTimes = sweep;
        
            int _maxId = cur ;
            for (int id = cur + 1; id <= cur + 2 + 1; id++)
            {
                var tb = Table.GetClimbingTower(id);
                if (tb == null)
                    break;
                _maxId = id;
            }
            int _minId = _maxId - 4;

            if (_minId < 1)
            {
                _minId = 1;
                _maxId = _minId + 4;
            }
        

            for (int id =_minId,i=0; id <= _maxId; id++,i++)
            {
                var tb = Table.GetClimbingTower(id);
                if (tb == null)
                    continue;
                DataModel.FloorList[i].nIndex = id ;
                DataModel.FloorList[i].bSelect = DataModel.NextFloor == id;
                DataModel.FloorList[i].bSweep = max > (id - 1);
                if (DataModel.NextFloor == id)
                {
                    DataModel.FloorList[i].strName= string.Format(GameUtils.GetDictionaryText(100001242), id);
                }
                else if (DataModel.NextFloor > id)
                {
                    DataModel.FloorList[i].strName = string.Format(GameUtils.GetDictionaryText(100001241), id);
                }
                else
                {
                    DataModel.FloorList[i].strName= string.Format(GameUtils.GetDictionaryText(100001233), id);
                }
            }
        }
        private void OnClickCell(IEvent ievent)
        {
            TowerFloorClickEvent e = ievent as TowerFloorClickEvent;
            if (e == null)
                return;
            if (e.nIndex >= DataModel.FloorList.Count)
                return;
            for (int i = 0; i < DataModel.FloorList.Count; i++)
            {

                if (i == e.nIndex)
                {
                    DataModel.FloorList[i].bSelect = true;
                    DataModel.SelectIdx = DataModel.FloorList[i].nIndex;
                }
                else
                    DataModel.FloorList[i].bSelect = false;
            }
            InitReward(DataModel.FloorList[e.nIndex].nIndex);
        }

        private void InitReward(int id)
        {
            int max = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e621);
            var tbTower = Table.GetClimbingTower(id);
            if (tbTower == null)
                return;
            var tbFuben = Table.GetFuben(tbTower.FubenId);
            if (tbFuben == null)
                return;

            DataModel.bFirstReward = max >= id ;

            DataModel.FightPoint = tbFuben.FightPoint;
            DataModel.AwardItems.Clear();
            for (int i = 0; i < tbTower.RewardList.Count && i < tbTower.NumList.Count; i++)
            {
                BagItemDataModel item = new BagItemDataModel();
                item.ItemId = tbTower.RewardList[i];
                item.Count = tbTower.NumList[i];
                DataModel.AwardItems.Add(item);
            }
        
            DataModel.OnceRewards.Clear();
            for (int i = 0; i < tbTower.OnceRewardList.Count && i < tbTower.OnceNumList.Count; i++)
            {
                BagItemDataModel item = new BagItemDataModel();
                item.ItemId = tbTower.OnceRewardList[i];
                item.Count = tbTower.OnceNumList[i];
                DataModel.OnceRewards.Add(item);
            }

            EventDispatcher.Instance.DispatchEvent(new TowerRefreshBoss_Event(tbTower.Boss));
        }

        private void OnTowerRefresh(IEvent ievent)
        {
            OnShow();
        }
        private void OnTowerEvent(IEvent ievent)
        {
            TowerBtnClickEvent e = ievent as TowerBtnClickEvent;
            if (e == null)
                return;
            switch (e.nType)
            {
                case 0:
                {//进入
                    var tbDynamic = Table.GetDynamicActivity(10);
                    if (tbDynamic != null)
                    {
                        if (!GameUtils.CheckIsWeekLoopOk(tbDynamic.WeekLoop))
                        {
                            GameUtils.ShowHintTip(45001);
                            return;
                        }
                    }

                    if (DataModel.NextFloor != DataModel.CurFloor)
                    {
                        var tb = Table.GetClimbingTower(DataModel.NextFloor);
                        if (tb != null)
                        {
                            GameUtils.EnterFuben(tb.FubenId);
                            // ObjManager.Instance.MyPlayer.EnterAutoCombat();
                        }
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(60003));
                    }
                
                }
                    break;
                case 1:
                {//扫荡
                
                    int max = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e621);
                    int cur = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e623);
                    int sweep = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e622);
                    var tb = Table.GetClimbingTower(cur);
                    if (tb == null || tb.SweepFloor <= 0)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(60002));
                    }
                    else if (sweep <= 0)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(224701));
                    }
                    else
                    {
                        string str = string.Format(GameUtils.GetDictionaryText(224703),tb.SweepFloor);
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, null, () => { NetManager.Instance.StartCoroutine(OnSweepTower()); });                    
                    }
                }
                    break;
                case 2:
                {//扫荡返回
                    DataModel.Sweeping = false;
                    OnShow();
                }
                    break;
                case 3:
                {//购买扫荡
                   int times = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e624);
                    int curFloor = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e623);
                    var tbTower = Table.GetClimbingTower(curFloor);
                    if (tbTower == null)
                        return;
                    int sweepFloor = tbTower.SweepFloor;
                    if (sweepFloor <= 0)
                        return;
                    var tbVip = Table.GetVIP(PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel));
                      if (tbVip != null)
                     {
                         if (times >= tbVip.TowerSweepTimes)
                         {
                             EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(60001));
                             return;
                         }
                     }
                    var tb = Table.GetSkillUpgrading(140000);
                    int cost = tb.GetSkillUpgradingValue(sweepFloor - 1);
                    string str = string.Format(GameUtils.GetDictionaryText(60000), cost, tbVip.TowerSweepTimes - times, tbVip.TowerSweepTimes);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "", () =>
                    {
                        if (PlayerDataManager.Instance.GetRes((int) eResourcesType.DiamondRes) < cost)
                        {
                            GameUtils.ShowNetErrorHint((int)ErrorCodes.DiamondNotEnough);
                        }
                        else
                            NetManager.Instance.StartCoroutine(OnBuySweepTimes());
                    });
                }
                    break;
                case 4:
                {//刷新
                    OnShow();
                }
                    break;
            }
        }

        private IEnumerator OnBuySweepTimes()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TowerBuySweepTimes(0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode != (int) ErrorCodes.OK)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....OnBuySweepTimes.......{0}.", msg.ErrorCode);
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(224702));
                    }
                }
                else
                {
                    Logger.Warn(".....OnBuySweepTimes.......{0}.", msg.State);
                }
            }
            yield break;
        }
        private void RewardCache()
        {
            if (mTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(mTrigger);
            }
            if (temp.Count > 0)
            {
                DataModel.SweepLog.Add(temp[0]);
                temp.RemoveAt(0);
            }
            if (temp.Count > 0)
            {
                mTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1), RewardCache);
            }
        
        
        }
        private IEnumerator OnSweepTower()
        {
            DataModel.Sweeping = true;
            DataModel.SweepLog.Clear();
            temp.Clear();
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TowerSweep(0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        for (int i = 0; i < msg.Response.RewardList.Count; i++)
                        {

                            ClimbingTowerSweepReward list = new ClimbingTowerSweepReward();

                            for (int j = 0;
                                j < msg.Response.RewardList[i].IDList.Count && j < msg.Response.RewardList[i].NumList.Count;
                                j++)
                            {
                                ItemIdDataModel item = new ItemIdDataModel();
                                item.Count = msg.Response.RewardList[i].NumList[j];
                                item.ItemId = msg.Response.RewardList[i].IDList[j];
                                list.AwardItems.Add(item);                            
                            }
                            list.strFloor = string.Format(GameUtils.GetDictionaryText(100001233),
                                msg.Response.RewardList[i].Floor);
                            // DataModel.SweepLog.Add(list);
                            temp.Add(list);
                        
                        }
                        if (mTrigger != null)
                        {
                            TimeManager.Instance.DeleteTrigger(mTrigger);
                        }
                        mTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1), RewardCache);
                    
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....OnSweepTower.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Warn(".....OnSweepTower.......{0}.", msg.State);
                }
            }
        }
    }
}