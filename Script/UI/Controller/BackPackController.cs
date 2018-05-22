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
using UnityEngine;

#endregion

namespace ScriptController
{
    public class BackPackController : IControllerBase
    {
        public BackPackController()
        {
            EventDispatcher.Instance.AddEventListener(PackItemClickEvent.EVENT_TYPE, OnClickPackItem);
            EventDispatcher.Instance.AddEventListener(PackArrangeEventUi.EVENT_TYPE, OnArrange);
            EventDispatcher.Instance.AddEventListener(PackCapacityEventUi.EVENT_TYPE, OnCapacity);
            EventDispatcher.Instance.AddEventListener(PackUnlockEvent.EVENT_TYPE, OnPackUnlock);
            EventDispatcher.Instance.AddEventListener(PackUnlockOperate.EVENT_TYPE, OnPackUnlockOperate);
            EventDispatcher.Instance.AddEventListener(SetBagFreeIconEvent.EVENT_TYPE, SetBagFreeIcon);
            EventDispatcher.Instance.AddEventListener(ShowPackPageEvent.EVENT_TYPE, OnShowPackPageEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_PickSettingChanged.EVENT_TYPE, AutoAutoRecycleEquip);
            CleanUp();
        }

        private BackPackDataModel DataModel;
        //增加免费包裹效果
        private Dictionary<int, object> FreeTirgger = new Dictionary<int, object>();
        private readonly Dictionary<int, int> indexDict = new Dictionary<int, int>();
        private BagsDataModel mData;
        private float mIntervalTime;
        private float mSortCD;
        private bool bSort = false;
        private int mUnLockBagId;
        private int mKeyCost;
        private int mDiamondLack;
        private int mUnLockindex;
        private int mUnLockKeyNeed;
        private int mUnLockDiamondNeed;

        public enum BackPackType
        {
            Character = 0,
            Depot,
            Recycle,
            DepotFarm,
            Chest,
        }

        private BackPackType PackType { get; set; }

        private void BuyBagCapacity(int bagId, int index)
        {
            //var tbBag = Table.GetBagBase(bagId);
            //var bagData = PlayerDataManager.Instance.GetBag(bagId);
            //var start = bagData.Capacity;

            //start -= tbBag.InitCapacity;
            //var totalCost = 0;
            //var totalTime = (int) (bagData.UnlockTime - Game.Instance.ServerTime).TotalSeconds;
            //var refresh = true;
            //if (totalTime < 0)
            //{
            //    totalTime = 0;
            //    refresh = false;
            //}
            //var end = index - tbBag.InitCapacity;

            //for (var i = start + 1; i <= end; i++)
            //{
            //    var priceType = i/tbBag.ChangeBagCount;
            //    var tbUpgrade = Table.GetSkillUpgrading(tbBag.Expression);
            //    var result = tbUpgrade.GetSkillUpgradingValue(priceType);
            //    totalTime += result*tbBag.TimeMult*60;
            //}

            //var bili = tbBag.TimeMult*60;
            //totalCost = totalTime/bili + (totalTime%bili > 0 ? 1 : 0);
            //mUnLockBagId = bagId;
            //mUnLockCost = totalCost;
            //mUnLockindex = index;
            //var vartime = Game.Instance.ServerTime.AddSeconds(totalTime);
            //var ee = new PackUnlockUIEvent(end - start + 1, totalCost, vartime, refresh);
            //EventDispatcher.Instance.DispatchEvent(ee);
        }

        private void BuyBagCapacityByPaid(int bagId, int index)
        {
            var tbBag = Table.GetBagBase(bagId);
            var bagData = PlayerDataManager.Instance.GetBag(bagId);
            var start = bagData.Capacity;
            start -= tbBag.InitCapacity;
            var diamondCost = 0;
            var keyCost = 0;
            var end = index - tbBag.InitCapacity;
            for (var i = start; i <= end; i++)
            {
                var priceType = i / tbBag.ChangeBagCount;
                var tbUpgrade = Table.GetSkillUpgrading(tbBag.Expression);
                var result = tbUpgrade.GetSkillUpgradingValue(priceType);
                keyCost += result;
            }
            var keyCount = PlayerDataManager.Instance.GetItemCount(GameUtils.OpenBagKeyId);
            if (keyCost > keyCount)
            {
                diamondCost = (keyCost - keyCount) * tbBag.TimeMult;
                bagData.IsLackKey = true;
            }
            else
            {
                bagData.IsLackKey = false;
            }
            mUnLockBagId = bagId;
            mKeyCost = keyCost;
            mDiamondLack = diamondCost;
            mUnLockindex = index;

            #region UpdateDataModel
            bagData.UnlockCount = end - start + 1;
            bagData.TotalKeyCount = keyCount;
            bagData.NeedKeyCount = keyCost;
            bagData.LackDiamondCount = diamondCost;
            #endregion

            var ee = new PackUnlockUIEvent(bagId);
            EventDispatcher.Instance.DispatchEvent(ee);
        }

        private void BuySpaceBag()
        {
            var keyCount = PlayerDataManager.Instance.GetItemCount(GameUtils.OpenBagKeyId);
            var diamondCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes);
            if (mKeyCost > keyCount)
            {
                if (mDiamondLack > diamondCount)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(305));
                    PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.DiamondRes);
                    return;
                }
            }
            NetManager.Instance.StartCoroutine(BuySpaceBagCoroutine());
        }

        private IEnumerator BuySpaceBagCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.BuySpaceBag(mUnLockBagId, mUnLockindex, mKeyCost);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(304));
                        var bagData = PlayerDataManager.Instance.GetBag(mUnLockBagId);

                        var isFull = bagData.Size == bagData.Capacity;

                        var count = 0;
                        for (var i = 0; i <= mUnLockindex; i++)
                        {
                            if (bagData.Items[i].Status == (int) eBagItemType.Lock ||
                                bagData.Items[i].Status == (int) eBagItemType.FreeLock)
                            {
                                bagData.Items[i].Status = (int) eBagItemType.UnLock;
                                count++;
                            }
                        }


                        var tbBagBase = Table.GetBagBase(mUnLockBagId);
                        var star = mUnLockindex + 1 - tbBagBase.InitCapacity;

                        if (star >= bagData.Items.Count)
                        {
                            yield break;
                        }
                        star = star/tbBagBase.ChangeBagCount;
                        var tbUpgrade = Table.GetSkillUpgrading(tbBagBase.Expression);
                        var result = tbUpgrade.GetSkillUpgradingValue(star);
                        result *= tbBagBase.TimeMult*60;
                        bagData.UnlockTime = Game.Instance.ServerTime.AddSeconds(result);

                        if (mUnLockBagId == 0)
                        {
                            if (isFull)
                            {
                                var playerData = PlayerDataManager.Instance.PlayerDataModel;
                                playerData.Bags.IsEquipFull = false;
                                var e = new EquipBagNotFullChange();
                                EventDispatcher.Instance.DispatchEvent(e);
                            }
                        }
                        SetFreeIcon();
                        bagData.Capacity += count;
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.DiamondNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(305));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Error("BuySpaceBag Error!............ErrorCode..." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("BuySpaceBag Error!............State..." + msg.State);
                }
            }
        }

        private void ClickItem(int nBagId, int nIndex)
        {
            var item = PlayerDataManager.Instance.GetItem(nBagId, nIndex);
            if (item == null)
            {
                return;
            }
            GameUtils.ShowItemDataTip(item, eEquipBtnShow.BagPack);
        }

        private void DepotPutIn(BagItemDataModel bagItem)
        {
            var bagBase = PlayerDataManager.Instance.GetBag((int) eBagType.Depot);

            NetManager.Instance.StartCoroutine(DepotPutInCoroutine(bagItem.BagId, bagItem.Index));
        }

        private IEnumerator DepotPutInCoroutine(int bagId, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.DepotPutIn(bagId, index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        //存入仓库
                        var e = new ShowUIHintBoard(270050);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var e = new ShowUIHintBoard(531);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Error("DepotPutIn Error!............ErrorCode..." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("DepotPutIn Error!............State..." + msg.State);
                }
            }
        }

        private void DepotTakeOut(BagItemDataModel bagItem)
        {
            var tbItem = Table.GetItemBase(bagItem.ItemId);
            var bagBase = PlayerDataManager.Instance.GetBag(tbItem.InitInBag);
            NetManager.Instance.StartCoroutine(DepotTakeOutCoroutine(bagItem.Index));
        }

        private IEnumerator DepotTakeOutCoroutine(int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.DepotTakeOut(index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        //从仓库取出
                        var e = new ShowUIHintBoard(270048);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.Error_ItemNoInBag_All)
                    {
                        //目标背包已满，无法再放入物品
                        var e = new ShowUIHintBoard(270049);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Error("DepotTakeOut Error!............ErrorCode..." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("DepotTakeOut Error!............State..." + msg.State);
                }
            }
        }
        List<int> sortBagList = new List<int>();

        private void OnArrange(IEvent ievent)
        {
            var e = ievent as PackArrangeEventUi;
            if (sortBagList.FindIndex(o => o == e.PackId) < 0)
            {
                sortBagList.Add(e.PackId);
                bSort = true;
            }
        }



        private IEnumerator OnArrangeCoroutine(int nBagId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.SortBag(nBagId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var bag = msg.Response;
                        PlayerDataManager.Instance.InitBagData(bag);
                        var bagType = (eBagType) bag.BagId;
                        if (bagType == eBagType.Equip
                            || bagType == eBagType.Depot)
                        {
                            PlayerDataManager.Instance.RefreshEquipBagStatus(bagType);
                        }
                        SetFreeIcon();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("OnArrangeCoroutine............State..." + msg.State);
                }
            }
        }

        private void OnCapacity(IEvent ievent)
        {
            var e = ievent as PackCapacityEventUi;
            var bagType = e.BagType;
            var bag = PlayerDataManager.Instance.GetBag(bagType);
            if (bag.MaxCapacity == bag.Capacity)
            {
                return;
            }
            BuyBagCapacityByPaid(bagType, bag.Capacity);
        }

        private void OnClickPackItem(IEvent ievent)
        {
            var ee = ievent as PackItemClickEvent;
            if (PackType == BackPackType.Character)
            {
                ClickItem(ee.BagId, ee.Index);
            }
            if (PackType == BackPackType.DepotFarm)
            {
                ClickItem(ee.BagId, ee.Index);
            }
            else if (PackType == BackPackType.Depot)
            {
                var item = PlayerDataManager.Instance.GetItem(ee.BagId, ee.Index);
                if (item == null)
                {
                    return;
                }
                if (item.BagId == (int)eBagType.Depot)
                {
                    DepotTakeOut(item);
                }
                else
                {
                    DepotPutIn(item);
                }
            }
            else if (PackType == BackPackType.Recycle)
            {
                var item = PlayerDataManager.Instance.GetItem(ee.BagId, ee.Index);
                if (ee.BagId != (int)eBagType.Equip)
                {
                    ClickItem(ee.BagId, ee.Index);
                }
                //else
                //{
                //    item.IsChoose = !item.IsChoose;
                //}

                //Todo
            }
            else if (PackType == BackPackType.Chest)
            {
                var tbItem = Table.GetItemBase(ee.TableId);
                //  var item = PlayerDataManager.Instance.GetItem(nBagId, nIndex);
                if (tbItem == null)
                {
                    return;
                }
                //var item = new BagItemDataModel();
                //item.ItemId = ee.TableId;
                GameUtils.ShowItemIdTip(ee.TableId);
            }
        }

        private void OnPackUnlock(IEvent ievent)
        {
            var e = ievent as PackUnlockEvent;
            var bagItem = e.DataModel;
            var bagId = bagItem.BagId;
            if (bagId == (int) eBagType.FarmDepot)
            {
                var e1 = new ShowUIHintBoard(300300);
                EventDispatcher.Instance.DispatchEvent(e1);
                return;
            }
            var index = bagItem.Index;
            //BuyBagCapacity(bagId, index);
            BuyBagCapacityByPaid(bagId, index);
        }

        private void OnPackUnlockOperate(IEvent ievent)
        {
            var e = ievent as PackUnlockOperate;
            if (e.Type == 0)
            {
                mUnLockBagId = -1;
                mUnLockindex = 0;
                mKeyCost = 0;
                mDiamondLack = 0;
            }
            else
            {
                BuySpaceBag();
            }
        }

        private void OnShowPackPageEvent(IEvent ievent)
        {
            var e = ievent as ShowPackPageEvent;
            DataModel.ShowType = e.PackPage;
        }

        

        private void AutoAutoRecycleEquip(IEvent ievent)
        {
            var control = UIManager.Instance.GetController(UIConfig.SettingUI);
            if (null == control)
            {
                return;
            }
            var equipBag = PlayerDataManager.Instance.GetBag(0);
            var itemId = 0;
            var bagIndex = 0;
            for (int i = 0; i < equipBag.Items.Count; i++)
            {
                itemId = equipBag.Items[i].ItemId;
                bagIndex = equipBag.Items[i].Index;
                if (itemId != -1)
                {
                    if ((bool)control.CallFromOtherClass("CanAutoRecyleItem", new object[] { itemId }))
                    {
                        EventDispatcher.Instance.DispatchEvent(new EquipAutoRecycleEvent(bagIndex));
                    }
                }
            }
        }

        private void SetBagFreeIcon(IEvent ievent)
        {
            SetFreeIcon();
        }

        private void SetFreeIcon()
        {
            return;//关闭计时免费开启
            var instance = PlayerDataManager.Instance;
            var indexequip = instance.GetLockIndex((int) eBagType.Equip);
            var indexbag = instance.GetLockIndex((int) eBagType.BaseItem);
            var indexDepot = instance.GetLockIndex((int) eBagType.Depot);
            indexDict.Clear();
            indexDict.Add((int) eBagType.Equip, indexequip);
            indexDict.Add((int) eBagType.BaseItem, indexbag);
            indexDict.Add((int) eBagType.Depot, indexDepot);

            var noticeData = instance.NoticeData;

            {
                // foreach(var item in indexDict)
                var __enumerator1 = (indexDict).GetEnumerator();
                while (__enumerator1.MoveNext())
                {
                    var item = __enumerator1.Current;
                    {
                        var index = item.Key;
                        var value = item.Value;
                        if (index != -1)
                        {
                            var tbBagBase = Table.GetBagBase(index);
                            if (tbBagBase == null)
                            {
                                continue;
                            }
                            var bagData = instance.GetBag(index);
                            if (bagData == null)
                            {
                                continue;
                            }

                            var totalTime = (int) (bagData.UnlockTime - Game.Instance.ServerTime).TotalSeconds;
                            var isFull = bagData.Capacity == bagData.MaxCapacity;
                            if (totalTime < 0 && !isFull)
                            {
                                totalTime = 0;
                                bagData.FreeUnlock = true;
                            }
                            else
                            {
                                bagData.FreeUnlock = false;
                            }

                            if (!isFull)
                            {
                                switch (index)
                                {
                                    case (int) eBagType.Equip:
                                    {
                                        noticeData.EquipBagFree = bagData.FreeUnlock;
                                    }
                                        break;
                                    case (int) eBagType.BaseItem:
                                    {
                                        noticeData.ItemBagFree = bagData.FreeUnlock;
                                    }
                                        break;
                                    case (int) eBagType.Depot:
                                    {
                                        noticeData.DepotBagFree = bagData.FreeUnlock;
                                    }
                                        break;
                                }
                            }

                            var freeTime = Game.Instance.ServerTime.AddSeconds(totalTime);
                            if (FreeTirgger.ContainsKey(index))
                            {
                                TimeManager.Instance.DeleteTrigger(FreeTirgger[index]);
                                FreeTirgger.Remove(index);
                            }

                            FreeTirgger[index] = TimeManager.Instance.CreateTrigger(freeTime, () =>
                            {
                                object tr;
                                if (FreeTirgger.TryGetValue(index, out tr))
                                {
                                    TimeManager.Instance.DeleteTrigger(tr);
                                    FreeTirgger.Remove(index);
                                }
                                var mIndex = indexDict[index];
                                if (mIndex >= 0 && mIndex < bagData.MaxCapacity)
                                {
                                    if (bagData.Items[mIndex].Status == (int) eBagItemType.Lock)
                                    {
                                        bagData.Items[mIndex].Status = (int) eBagItemType.FreeLock;

                                        bagData.FreeUnlock = !isFull;
                                        if (!isFull)
                                        {
                                            switch (index)
                                            {
                                                case (int) eBagType.Equip:
                                                {
                                                    noticeData.EquipBagFree = true;
                                                }
                                                    break;
                                                case (int) eBagType.BaseItem:
                                                {
                                                    noticeData.ItemBagFree = true;
                                                }
                                                    break;
                                                case (int) eBagType.Depot:
                                                {
                                                    noticeData.DepotBagFree = true;
                                                }
                                                    break;
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        public void CleanUp()
        {
            mData = PlayerDataManager.Instance.PlayerDataModel.Bags;

            DataModel = new BackPackDataModel();
            DataModel.ShowType = 1;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "SetPackType")
            {
                PackType = (BackPackType) param[0];
            }
            else if (name == "GetPackType")
            {
                return PackType;
            }

            return null;
        }

        public void OnShow()
        {
        }

        public void Close()
        {
            //DataModel.ShowType = -1;
        }

        private void SortTick()
        {
            mSortCD += Time.deltaTime;
            if (mSortCD < 0.3f)
                return;
            mSortCD -= 0.3f;
            if (bSort == false)
                return;
            bSort = false;
            foreach (var v in sortBagList)
            {
                NetManager.Instance.StartCoroutine(OnArrangeCoroutine(v));                
            }
            sortBagList.Clear();
        }
        public void Tick()
        {
            SortTick();
            mIntervalTime += Time.deltaTime;
            if (mIntervalTime < 0.1f)
            {
                return;
            }
            mIntervalTime -= 0.1f;
            for (var i = 1; i < 3; i++)
            {
                var skillItem = mData.ItemWithSkillList[i];
                if (skillItem.LastTime <= 0)
                {
                    continue;
                }

                skillItem.LastTime -= 0.1f;
                if (skillItem.LastTime <= 0)
                {
                    skillItem.LastTime = 0;
                    skillItem.ProgressValue = 0;
                }
                else
                {
                    skillItem.ProgressValue = skillItem.LastTime/skillItem.MaxTime;
                }
            }
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as BackPackArguments;
            PlayerDataManager.Instance.RefreshEquipBagStatus();
            if (args != null && args.Tab != -1)
            {
                DataModel.ShowType = args.Tab;
            }
            else
            {
//默认打开道具背包
                DataModel.ShowType = 1;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        private FrameState m_state;
        public FrameState State
        {
            get { return m_state; }
            set { m_state = value; }
        }
    }
}