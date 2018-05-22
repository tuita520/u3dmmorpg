#region using

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

#endregion
namespace ScriptController
{
    public class BosssHomeCtrl : IControllerBase
    {
        private BossHomeDataModel DataModel;
        private bool Inited = false;

        public BosssHomeCtrl()
        {
            DataModel = new BossHomeDataModel();

            if (!Inited)
            {
                Inited = true;

                int i = 0;
                foreach (var item in DataModel.ItemList)
                {
                    item.Id = 1;
                    item.Show = 0;
                }
                Table.ForeachBossHome((tb) =>
                {
                    if (i >= DataModel.ItemList.Count)
                    {
                        return false;
                    }
                    var model = DataModel.ItemList[i];
                    model.Id = tb.Id;
                    model.Show = 1;
                    i++;
                    return true;
                });
            }
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIBossHomeClickEvent.EVENT_TYPE, ClickRefreshData);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitEvent);
            EventDispatcher.Instance.AddEventListener(UIBossHomeOperationClickEvent.EVENT_TYPE, EnterSceneEvent);
            //EventDispatcher.Instance.AddEventListener(UIBossHomeDieRefreshEvent.EVENT_TYPE, BossHeadRefresh);
        }
        private void Init(IEvent ievent)
        {
        }
        private void OnExDataInitEvent(IEvent ievent)
        {
            //DataModel.CurrentEnergy = PlayerDataManager.Instance.GetExData(EnergyExdataIdx);
        }
        private void RefreshTabData(int idx)
        {
            if (idx < 0 || idx >= DataModel.ItemList.Count)
            {
                return;
            }
           
            ApplyActivityState();
            DataModel.CurrentSelectPageIdx = idx;
            DataModel.Info.Id = DataModel.ItemList[idx].Id;
            var tb = Table.GetBossHome(DataModel.Info.Id);
            DataModel.ModelId = tb.CharacterBaseId;
            for (int i = 0; i < tb.Item.Length; i++)
            {
                DataModel.Info.RewardId[i] = tb.Item[i];
                DataModel.Info.RewardCount[i] = tb.ItemCount[i];
            }
       
            if (tb.VipLimit == 1)
            {
                homeSceneId = 22000;
            }
            else if (tb.VipLimit == 4)
            {
                homeSceneId = 22001;
            }       
            viplimit = tb.VipLimit;
            costDiamo = tb.CostNum;
            DataModel.NeedLevel = Table.GetScene(homeSceneId).LevelLimit;
            EventDispatcher.Instance.DispatchEvent(new AcientBattleFieldCurrBossEvent(tb.CharacterBaseId));
        }

        private void ClickRefreshData(IEvent ieve)
        {
            var e = ieve as UIBossHomeClickEvent;
            RefreshTabData(e.index);
        }
        private int viplimit = 0;
        private int costDiamo = 0;
        private int homeSceneId = 0;
        private void EnterSceneEvent(IEvent iev)
        {
            //if (DataModel.ItemList[DataModel.CurrentSelectPageIdx].isGrey)
            //{
            //    Debug.LogError("Boss已死"); return;
            //}
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.LevelRes) < DataModel.NeedLevel)
            {
                var _ee = new ShowUIHintBoard(210207);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes) < costDiamo)
            {
                var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel) < viplimit)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, string.Format(GameUtils.GetDictionaryText(270247), viplimit), "",
                    () => {
                              var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                              EventDispatcher.Instance.DispatchEvent(_e);
                    });
                return;
            }
            var tbScene = GameLogic.Instance.Scene.TableScene;
            if (tbScene == null)
            {
                return;
            }
            if (tbScene.Id == homeSceneId)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270081));
                return;
            }
            //var diamond = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Diamond - costDiamo;
            //PlayerDataManager.Instance.SetRes(3, diamond);
            var scene = GameLogic.Instance.Scene;
            if (scene.TableScene.Type != (int)eSceneType.Fuben)
            {
                DataModel.ModelId = -1;
            }
//            NetManager.Instance.StartCoroutine(ApplyBossHomeCostCoroutine());
            GameUtils.EnterFuben(homeSceneId);
        }
        object trigger = null;
        private int time;
        private int inIndex;
        private bool isGrey = false;

        //private void GreyCoolDown(int index)
        //{
        //    inIndex = index-1;
        //    DataModel.ItemList[inIndex].isGrey = true;
        //    DataModel.ItemList[inIndex].TimeDown = coolTime.ToString ();
        //    time =int.Parse(DataModel.ItemList[inIndex].TimeDown);
        //    GameUtils.GetTimeDiffString(time, true);
        //    trigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1), RefreshState, 1000);
        //}

        //private void RefreshState()
        //{
        //    time--;
        //    if (time <= 0)
        //    {
        //        TimeManager.Instance.DeleteTrigger(trigger);
        //        DataModel.ItemList[inIndex].TimeDown = "0";
        //        DataModel.ItemList[inIndex].isGrey = false;

        //    }
        //    DataModel.ItemList[inIndex].TimeDown = time.ToString();
        //}

        //private void BossHeadRefresh(IEvent iev)
        //{
        //    var DieBossId = iev as UIBossHomeDieRefreshEvent;

        //    Table.ForeachBossHome(tb =>
        //    {
        //        if (tb.CharacterBaseId == DieBossId.BossID)
        //        {
        //            GreyCoolDown(tb.Id);
        //            return false;
        //        }
        //        return true;
        //    });
        //}

        private void ApplyActivityState()
        {
            NetManager.Instance.StartCoroutine(ApplyBossHomeCoroutine());
        }

        private IEnumerator ApplyBossHomeCoroutine()
        {
            var msg = NetManager.Instance.ApplyBossHome(PlayerDataManager.Instance.ServerId);
            yield return msg.SendAndWaitUntilDone();

            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    var data = msg.Response.Data;

                    foreach (var item in DataModel.ItemList)
                    {
                        int stat = 0;
                        if (data.TryGetValue(item.Id,out stat))
                        {
                            item.isGrey = stat != 0;
                            item.NoDie = stat == 0;
                        }
                    }
                }
            }
        }
        //private IEnumerator UpDateTime()
        //{
        //    while (true)
        //    {
        //        yield return new WaitForSeconds(1.0f);
        //        TimeTick();
        //    }
        //}
        //private string timeForm;
        //private void TimeTick()
        //{
        //    for (int i = 1; i <= BossDieTime.Count; i++)
        //    {
        //        if (BossDieTime[i] != -1)
        //        {
        //            var t = Table.GetBossHome(i);
        //            var npsRefresh = Table.GetNpcBase(t.CharacterBaseId);
        //            var span = npsRefresh.ReviveTime/1000 - ((int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds - BossDieTime[i]);
        //            var min = span / 60;
        //            var sec = span % 60;
        //            if (min > 0)
        //            {
        //                timeForm = string.Format("{0}分{1}秒", span / 60, span % 60);
        //            }
        //            else
        //            {
        //                timeForm = string.Format("{0}秒",span % 60);
        //            }
               
        //            DataModel.ItemList[i-1].TimeDown = timeForm;
        //            if (span <= 0)
        //            {
        //                DataModel.ItemList[i - 1].isGrey = false;
        //                DataModel.ItemList[i - 1].NoDie = true;
        //            }
        //        }
        //    }
        //}
        #region 固有函数
        public void CleanUp()
        {
            DataModel.CurrentSelectPageIdx = 0;
            DataModel.ModelId = -1;
        }

        public void RefreshData(UIInitArguments data)
        {       
            RefreshTabData(0);
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void Close()
        {
            DataModel.CurrentSelectPageIdx = 0;
            DataModel.ModelId = -1;
        }

        public void OnShow()
        {
            //RefreshTabData(0);
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
        #endregion
    }
}
