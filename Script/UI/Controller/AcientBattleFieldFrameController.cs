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
    public class AcientBattleFieldFrameCtrler : IControllerBase
    {
        private AcientBattleFieldDataModel DataModel;
        private bool Inited = false;
        private const int ScenenId = 21000;
        private const int EnergyExdataIdx = 632;
        private Dictionary<int, int> BossDieTime = new Dictionary<int, int>();
        private string timeForm;

        public AcientBattleFieldFrameCtrler()
        {
            DataModel = new AcientBattleFieldDataModel();

            if (!Inited)
            {
                Inited = true;

                int i = 0;
                foreach (var item in DataModel.ItemList)
                {
                    item.Id = 1;
                    item.Show = 0;
                }
                Table.ForeachAcientBattleField((tb) =>
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

                var tbScene = Table.GetScene(ScenenId);
                DataModel.NeedLevel = tbScene.LevelLimit;
                DataModel.CostEnergy = Table.GetClientConfig(940).ToInt();
            }
            CleanUp();

            EventDispatcher.Instance.AddEventListener(UIAcientBattleFieldMenuItemClickEvent.EVENT_TYPE, OnClickPageBtn);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpDataEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitEvent);
            EventDispatcher.Instance.AddEventListener(UIAcientBattleFieldOperationClickEvent.EVENT_TYPE, OnClickEnterEvent);
        }
        private void ApplyActivityState()
        {
            NetManager.Instance.StartCoroutine(ApplyAcientBattleCoroutine());
        }

        private IEnumerator ApplyAcientBattleCoroutine()
        {
            var msg = NetManager.Instance.ApplyAcientBattle(PlayerDataManager.Instance.ServerId);
            yield return msg.SendAndWaitUntilDone();

            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    var data = msg.Response.Data;
                    BossDieTime = data;
                    NetManager.Instance.StartCoroutine(UpDateTime());
                    int i = 0;
                    foreach (var item in BossDieTime)
                    {
                        if (item.Value != -1)
                        {
                            DataModel.ItemList[i].isGrey = true;
                            DataModel.ItemList[i].NoDie = false;
                        }
                        else
                        {
                            DataModel.ItemList[i].isGrey = false;
                            DataModel.ItemList[i].NoDie = true;
                        }
                        i++;
                    }
                }
            }
        }
        private IEnumerator UpDateTime()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                TimeTick();
            }
        }
        private void TimeTick()
        {
            int i = 0;
            foreach (var item in BossDieTime)
            {
                if (item.Value != -1)
                {
                    var t = Table.GetAcientBattleField(item.Key);
                    var npsRefresh = Table.GetNpcBase(t.CharacterBaseId);
                    var span = npsRefresh.ReviveTime / 1000 - ((int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds - item.Value);
                    var min = span / 60;
                    var sec = span % 60;
                    if (min > 0)
                    {

                        timeForm = string.Format(Table.GetDictionary(210404).Desc[GameUtils.LanguageIndex], span / 60, span % 60);
                    }
                    else
                    {
                        timeForm = string.Format(Table.GetDictionary(210405).Desc[GameUtils.LanguageIndex], span % 60);
                    }

                    DataModel.ItemList[i].TimeDown = timeForm;
                    if (span <= 0)
                    {
                        DataModel.ItemList[i].isGrey = false;
                        DataModel.ItemList[i].NoDie = true;
                    }
                }
                i++;
            }
          
        }
        public void CleanUp()
        {
            DataModel.CurrentSelectPageIdx = 0;
            DataModel.ModelId = -1;
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
            DataModel.CurrentSelectPageIdx = 0;
            DataModel.ModelId = -1;
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            ChooseActivityMenu(0);
            ApplyActivityState();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }

        private void OnClickPageBtn(IEvent ievent)
        {
            var e = ievent as UIAcientBattleFieldMenuItemClickEvent;
            ChooseActivityMenu(e.Idx);
        }

        private void OnExDataInitEvent(IEvent ievent)
        {
            DataModel.CurrentEnergy = PlayerDataManager.Instance.GetExData(EnergyExdataIdx);
        }

        private void OnClickEnterEvent(IEvent ievent)
        {
            var e = ievent as UIAcientBattleFieldOperationClickEvent;

            var costDiamond = Table.GetClientConfig(941).ToInt();
            var count = Table.GetClientConfig(942).ToInt();

            if (0 == e.OptType)
            {
                var tbDynamicActivity = Table.GetDynamicActivity(16);
                if (tbDynamicActivity != null)
                {
                    if (!GameUtils.CheckIsWeekLoopOk(tbDynamicActivity.WeekLoop))
                    {
                        GameUtils.ShowHintTip(45001);
                        return;
                    }
                }
		    
                if (PlayerDataManager.Instance.GetLevel() < DataModel.NeedLevel)
                {
                    var tbCondition = Table.GetConditionTable(tbDynamicActivity.OpenCondition);
                    if (tbCondition != null)
                    {
                        GameUtils.ShowHintTip(tbCondition.ItemDict[0]);
                        return;
                    }
                }
		    
                if (DataModel.CostEnergy == 0 && DataModel.CurrentEnergy == 0)
                {
                    ShowMessageBox(costDiamond, count);
                    return;
                }
		    
                if ( DataModel.CurrentEnergy < DataModel.CostEnergy)
                {
                    ShowMessageBox(costDiamond, count);
                    return;
                }
		    	  
                var tbScene = GameLogic.Instance.Scene.TableScene;
                if (tbScene.Id == ScenenId)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270081));
                    return;
                }
                GameUtils.EnterFuben(ScenenId);
            }
            else if (1 == e.OptType)
            {
                var str = string.Format(GameUtils.GetDictionaryText(210124), costDiamond, count, GameUtils.GetDictionaryText(221004));
                var vipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
                var canBuyTimes = Table.GetVIP(vipLevel).PetIslandBuyTimes;
                var buyTimes = PlayerDataManager.Instance.GetExData(633);

                var str1 = string.Format(GameUtils.GetDictionaryText(300000135), vipLevel, buyTimes, canBuyTimes);

                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str + "\n" + str1, "",
                    () =>
                    {
                        if (buyTimes >= canBuyTimes)
                        {
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220939)));
                            return;
                        }
                        if (PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes) >= costDiamond)
                        {
                            NetManager.Instance.StartCoroutine(BuyTimesByDiamond());
                        }
                        else
                        {
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame));
                        }
                    });
            }

        }

        private void ShowMessageBox(int costDiamond ,int count)
        {
            var str = string.Format(GameUtils.GetDictionaryText(210124), costDiamond, count, GameUtils.GetDictionaryText(221004));
            var vipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
            var canBuyTimes = Table.GetVIP(vipLevel).PetIslandBuyTimes;
            var buyTimes = PlayerDataManager.Instance.GetExData(633);

            var str1 = string.Format(GameUtils.GetDictionaryText(300000135), vipLevel, buyTimes, canBuyTimes);

            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str + "\n" + str1, "",
                () =>
                {
                    if (buyTimes >= canBuyTimes)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220939)));
                        return;
                    }
                    if (PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes) >= costDiamond)
                    {
                        NetManager.Instance.StartCoroutine(BuyTimesByDiamond());
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame));
                    }
                });
        }

        private IEnumerator BuyTimesByDiamond()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.BuyEnergyByType(1);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {

                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.DiamondNotEnough)
                    {
                        var e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                        EventDispatcher.Instance.DispatchEvent(e);

                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error(".....BuyTimesByDiamond.......{0}.", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....PetIsLandBuyTili.......{0}.", msg.State);
                }
            }
        }
        private void OnExDataUpDataEvent(IEvent ievent)
        {
            var e = ievent as ExDataUpDataEvent;
            var idx = e.Key;
            if (idx == EnergyExdataIdx)
            {
                DataModel.CurrentEnergy = PlayerDataManager.Instance.GetExData(EnergyExdataIdx);


            }
		
        }

        private void ChooseActivityMenu(int idx)
        {
            if (idx < 0 || idx >= DataModel.ItemList.Count)
            {
                return;
            }

            DataModel.CurrentSelectPageIdx = idx;
            DataModel.Info.Id = DataModel.ItemList[idx].Id;
            var tb = Table.GetAcientBattleField(DataModel.Info.Id);
            //var tbChar = Table.GetCharacterBase(tb.CharacterBaseId);
            //DataModel.ModelId = tbChar.CharModelID;
            DataModel.ModelId = tb.CharacterBaseId;
            for(int i=0; i<tb.Item.Length ; i++)
            {
                //DataModel.Info.Rewards[i].ItemId = tb.Item[i];
                //DataModel.Info.Rewards[i].Count = tb.ItemCount[i];
                DataModel.Info.RewardId[i]= tb.Item[i];
                DataModel.Info.RewardCount[i]= tb.ItemCount[i];
            }
            EventDispatcher.Instance.DispatchEvent(new AcientBattleFieldCurrBossEvent(tb.CharacterBaseId));
		
		
        }

    }
}
