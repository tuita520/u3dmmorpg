/********************************************************************************* 

                         Scorpion



  *FileName:StoreFrameCtrler

  *Version:1.0

  *Date:2017-07-13

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
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class StoreFrameCtrler : IControllerBase
    {
        #region 构造函数
        public StoreFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(StoreCellClick.EVENT_TYPE, OnShopCellClickEvent);
            EventDispatcher.Instance.AddEventListener(StoreOperaEvent.EVENT_TYPE, OnStoreOperaEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnRefurbishEquipBagItemStatusEvent);
            EventDispatcher.Instance.AddEventListener(StoreCacheTriggerEvent.EVENT_TYPE, OnStoreCacheTriggerEvent);
            EventDispatcher.Instance.AddEventListener(UpdateFuBenStoreStore_Event.EVENT_TYPE, OnUpdateFuBenStoreLimitItemsEvent);
            EventDispatcher.Instance.AddEventListener(LoadSceneOverEvent.EVENT_TYPE, OnClearFuBenItemsEvent);
            EventDispatcher.Instance.AddEventListener(StoreUIRefreshEvent.EVENT_TYPE, OnStoreUIRefreshEvent);
            EventDispatcher.Instance.AddEventListener(Event_RefreshFuctionOnState.EVENT_TYPE, OnMainUIRefreshEvent);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChanged);
        }

        #endregion

        #region 成员变量
        private IControllerBase BackPack;
        private StoreDataModel DataModel;
        private Coroutine mPressTriger;
        private int RankCondition;
        private Dictionary<int, List<StoreCellData>> mStoreCache = new Dictionary<int, List<StoreCellData>>();
        // <server表param[0] ,数据> 

        private object mStoreCacheObject;
        private readonly Dictionary<int, int> mVipModifyCache = new Dictionary<int, int>();
        private int mServiceType = -1;
        #endregion

        #region 静态变量
        private static readonly BagItemDataModel emptyBagItemData = new BagItemDataModel();
        #endregion

        #region 逻辑函数
        private IEnumerator ApplyShopsCoroutine(int type, int serviceType = -1)
        {
            using (new BlockingLayerHelper(0))
            {
                DataModel.ItemList.Clear();
                if (mStoreCache.ContainsKey(type) && type != 1106)//珍宝商人数据动态刷新，暂不加入缓存
                {
                    SetShopCellDatum(type);
                    yield break;
                }
                var _msg = NetManager.Instance.ApplyStores(type, serviceType);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SetShopData(type, _msg.Response.items);
                    }
                    else
                    {
                        Logger.Error("ApplyStores............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ApplyStores............State..." + _msg.State);
                }
            }
        }

        private void SetShopData(int type, List<StoneItem> shopItems)
        {
            var _itemList = new List<StoreCellData>();
            {
                var _list1 = shopItems;
                var _listCount1 = _list1.Count;
                for (var __i1 = 0; __i1 < _listCount1; ++__i1)
                {
                    var _item = _list1[__i1].itemid;
                    {
                        var _cell = new StoreCellData
                        {
                            StoreIndex = _item
                        };
                        var _tbStore = Table.GetStore(_cell.StoreIndex);
                        if (_tbStore.BuyCondition == -1)
                        {
                            _cell.BuyLimit = "";
                        }
                        else
                        {
                            var _tbCondition = Table.GetConditionTable(_tbStore.BuyCondition);
                            var VipLimit = _tbCondition.ExdataMin[0];
                            _cell.BuyLimit = string.Format(GameUtils.GetDictionaryText(100001383), VipLimit);
                            var VIPLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
                            if (VIPLevel >= VipLimit)
                            {
                                _cell.LimitColor = GameUtils.GetTableColor(24);
                            }
                            else
                            {
                                _cell.LimitColor = GameUtils.GetTableColor(25);
                            }
                        }
                        var _limit = _tbStore.DayCount;
                        if (_limit == -1)
                        {
                            _limit = _tbStore.WeekCount;
                        }
                        if (_limit == -1)
                        {
                            _limit = _tbStore.MonthCount;
                        }
                        if (type == 1106)//珍宝商人特殊处理
                        {
                            _limit = _list1[__i1].itemcount;
                            _cell.ExData = _limit;
                            _cell.Limit = _limit;
                        }
                        else
                        {
                            _cell.ExData = _limit;
                            if (_limit == -1)
                            {
                                _cell.Limit = -1;
                            }
                            else
                            {
                                _cell.Limit = PlayerDataManager.Instance.GetExData(_limit) +
                                              VipAlterCount(_cell.StoreIndex);
                            }  
                        }

                        //// 只要发限购数量了 就覆盖掉扩展计数的数量  不走扩展计数了
                        //if (_list1[__i1].itemcount >= 0)
                        //{
                        //    _cell.Limit = _list1[__i1].itemcount;
                        //    _cell.ExData = -1;
                        //}

                        _cell.ItemId = _tbStore.ItemId;
                        _itemList.Add(_cell);
                    }
                }
            }
            if (mStoreCache.ContainsKey(type))
            {
                mStoreCache[type] = _itemList;
            }
            else
            {
                mStoreCache.Add(type, _itemList);  
            }
            SetShopCellDatum(type);
        }

        private IEnumerator ButtonOnPressCoroutine(bool isAdd)
        {
            var _pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(_pressCd);
                if (CheckPressCount(isAdd) == false)
                {
                    NetManager.Instance.StopCoroutine(mPressTriger);
                    mPressTriger = null;
                    yield break;
                }
                if (_pressCd > 0.01)
                {
                    _pressCd = _pressCd * 0.8f;
                }
            }
            yield break;
        }

        private void PurchaseEquipItem()
        {
            NetManager.Instance.StartCoroutine(ShopPurchaseEquipCoroutine(DataModel.SelectId, DataModel.ReplaceEquip.BagId,
                DataModel.ReplaceEquip.Index));
        }

        private bool CheckPressCount(bool isAdd)
        {
            if (isAdd)
            {
                if (DataModel.SelectCount < DataModel.MaxCount)
                {
                    DataModel.SelectCount++;
                    return true;
                }
            }
            else
            {
                if (DataModel.SelectCount > 1)
                {
                    DataModel.SelectCount--;
                    return true;
                }
            }
            return false;
        }

        private StoreCellData GainCellData(int tableIndex)
        {
            {
                // foreach(var cellData in DataModel.ItemList)
                if (!mStoreCache.ContainsKey(DataModel.StoreType))
                {
                    return null;
                }
                var _item = mStoreCache[DataModel.StoreType];
                var _enumerator2 = (_item).GetEnumerator();
                while (_enumerator2.MoveNext())
                {
                    var _cellData = _enumerator2.Current;
                    {
                        if (_cellData.StoreIndex == tableIndex)
                        {
                            return _cellData;
                        }
                    }
                }
            }
            return null;
        }

        private void IsCanUse(StoreRecord tbStore, StoreCellData item)
        {
            if (item.Limit == 0)
            {
                item.CanUse = false;
                return;
            }
            if (tbStore.ItemId < 0)
            {
                item.CanUse = false;
                return;
            }
            var _tbItemBase = Table.GetItemBase(tbStore.ItemId);
            item.CanUse = PlayerDataManager.Instance.ItemOrEquipCanUse(_tbItemBase);
        }

        private void OnClickPurchaseInfoAdd()
        {
            if (DataModel.SelectCount < DataModel.MaxCount)
            {
                DataModel.SelectCount++;
            }
        }

        private void OnClickPurchaseInfoPurchase()
        {
            var _index = DataModel.SelectId;
            var _count = DataModel.SelectCount;
            var _tbStore = Table.GetStore(_index);
            if (_tbStore == null)
            {
                return;
            }
            var _roleType = PlayerDataManager.Instance.GetRoleId();
            if (BitFlag.GetLow(_tbStore.SeeCharacterID, _roleType) == false)
            {
                return;
            }

            if (_tbStore.DisplayCondition != -1)
            {
                var _retCond = PlayerDataManager.Instance.CheckCondition(_tbStore.DisplayCondition);
                if (_retCond != 0)
                {
                    GameUtils.ShowHintTip(_retCond);
                    return;
                }
            }
            var _cost = _tbStore.NeedValue * _count;
            if (PlayerDataManager.Instance.GetRes(_tbStore.NeedType) < _cost)
            {
                var _tbItemCost = Table.GetItemBase(_tbStore.NeedType);
                //{0}不足！
                var _str = GameUtils.GetDictionaryText(701);
                _str = string.Format(_str, _tbItemCost.Name);
                GameUtils.ShowHintTip(_str);
                PlayerDataManager.Instance.ShowItemInfoGet(_tbStore.NeedType);

                if ((int)eResourcesType.GoldRes == _tbStore.NeedType)
                {
                    var e = new Show_UI_Event(UIConfig.WishingUI, new WishingArguments { Tab = 1 });
                    EventDispatcher.Instance.DispatchEvent(e);
                    //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ExchangeUI));
                }
                if ((int)eResourcesType.DiamondRes == _tbStore.NeedType)
                {
                    var _ee = new Close_UI_Event(UIConfig.RechargeFrame);
                    EventDispatcher.Instance.DispatchEvent(_ee);

                    var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                    EventDispatcher.Instance.DispatchEvent(_e);
                }

                return;
            }
            if (_tbStore.BuyCondition != -1)
            {
                var _tbCondition = Table.GetConditionTable(_tbStore.BuyCondition);
                var VipLimit = _tbCondition.ExdataMin[0];
                var VIPLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
                if (VIPLevel < VipLimit)
                {
                    var str = string.Format(GameUtils.GetDictionaryText(1722),VipLimit);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,str,"", () => { VipLimitOperation(); });
                    return;
                }
                if (-1 != RankCondition)
                {
                    GameUtils.ShowHintTip(RankCondition);
                    return;
                }
            }
            if (-1 != _tbStore.ItemId)
            {
                var tbEquip = Table.GetEquipBase(_tbStore.ItemId);
                if (null != tbEquip)
                {
                    var reborn = PlayerDataManager.Instance.GetExData(eExdataDefine.e51);
                    if (reborn < tbEquip.NeedRebornLevel)
                    {
                        GameUtils.ShowHintTip(100001396);
                        return;
                    }                    
                }
            }
            if (_tbStore.NeedItem != -1)
            {
                if (DataModel.ReplaceEquip.ItemId == -1)
                {
                    var _tbItemCost = Table.GetItemBase(_tbStore.NeedItem);
                    //{0}不足！
                    var _str = GameUtils.GetDictionaryText(701);
                    _str = string.Format(_str, _tbItemCost.Name);
                    GameUtils.ShowHintTip(_str);
                    return;
                }

                var _find = false;
                PlayerDataManager.Instance.ForeachEquip(equip =>
                {
                    if (equip.ItemId != _tbStore.NeedItem)
                    {
                        return;
                    }
                    if (equip.Index != DataModel.ReplaceEquip.Index)
                    {
                        return;
                    }
                    _find = true;
                });
                if (_find == false)
                {
                    return;
                }
                var _equipOld = Table.GetEquipBase(DataModel.ReplaceEquip.ItemId);
                var _equipNew = Table.GetEquipBase(_tbStore.ItemId);
                var _itemNew = Table.GetItemBase(_tbStore.ItemId);
                if (_equipOld == null || _equipNew == null || _itemNew == null)
                {
                    return;
                }
                if (_equipOld.Part != _equipNew.Part)
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                        210115,
                        "",
                        () => { PurchaseEquipItem(); });
                    return;
                }
                var _result = PlayerDataManager.Instance.CheckItemEquip(_itemNew, _equipNew);
                if (_result != eEquipLimit.OK)
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                        210116,
                        "",
                        () => { PurchaseEquipItem(); });
                    return;
                }
                PurchaseEquipItem();
            }
            else
            {
                NetManager.Instance.StartCoroutine(ShopPurchaseCoroutine(DataModel.SelectId, DataModel.SelectCount));
            }
        }
        private void VipLimitOperation()
        {
            var _ee = new Close_UI_Event(UIConfig.RechargeFrame);
            EventDispatcher.Instance.DispatchEvent(_ee);

            var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
            EventDispatcher.Instance.DispatchEvent(_e);
        }
        private void OnClickPurchaseInfoClose()
        {
            DataModel.SelectId = -1;
            //         DataModel.RoleId = -1;
            //         DataModel.MaxCount = -1;
            //         DataModel.SelectCount = -1;
        }

        private void OnClickPurchaseInfoDel()
        {
            if (DataModel.SelectCount > 1)
            {
                DataModel.SelectCount--;
            }
        }

        private void OnClickPurchaseInfoMax()
        {
            DataModel.SelectCount = DataModel.MaxCount;
        }

        private void OnClickPressCount(bool isAdd, bool isPress)
        {
            if (isPress)
            {
                if (mPressTriger != null)
                {
                    NetManager.Instance.StopCoroutine(mPressTriger);
                }
                mPressTriger = NetManager.Instance.StartCoroutine(ButtonOnPressCoroutine(isAdd));
            }
            else
            {
                if (mPressTriger != null)
                {
                    NetManager.Instance.StopCoroutine(mPressTriger);
                    mPressTriger = null;
                }
            }
        }

        private void OnClickReplaced()
        {
            if (DataModel.ReplaceList.Count < 2)
            {
                return;
            }
            DataModel.ReplaceIndex++;
            DataModel.ReplaceIndex = DataModel.ReplaceIndex % DataModel.ReplaceList.Count;
            DataModel.ReplaceEquip = DataModel.ReplaceList[DataModel.ReplaceIndex];

            var _bagType = DataModel.ReplaceEquip.BagId;
            var _bagIndex = DataModel.ReplaceEquip.Index;
            switch ((eBagType)_bagType)
            {
                case eBagType.Equip07:
                {
                    if (_bagIndex == 0)
                    {
                        DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271002); //"(左手)";
                    }
                    else
                    {
                        DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271003); //"(右手)";
                    }
                }
                    break;
                case eBagType.Equip11:
                {
                    DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271004); //"(主手)";
                }
                    break;
                case eBagType.Equip12:
                {
                    DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271005); //"(副手)";
                }
                    break;
            }
        }

        private void OnClickShowSelectedIcon()
        {
            var _storeId = DataModel.SelectId;
            var _tbStore = Table.GetStore(_storeId);
            if (_tbStore == null)
            {
                return;
            }
            var _itemId = _tbStore.ItemId;
            var _tbItem = Table.GetItemBase(_itemId);
            if (_tbItem == null)
            {
                return;
            }
            if (_tbStore.NeedItem == -1)
            {
                GameUtils.ShowItemIdTip(_itemId);
                return;
            }

            var _bagItemData = new BagItemDataModel();
            _bagItemData.ItemId = _itemId;

            if (DataModel.ReplaceEquip != null)
            {
                if (DataModel.ReplaceEquip.ItemId != -1)
                {
                    _bagItemData.Exdata.InstallData(DataModel.ReplaceEquip.Exdata);
                }
            }


            if (_bagItemData.Exdata.Count > 0)
            {
                GameUtils.ShowItemDataTip(_bagItemData);
            }
            else
            {
                GameUtils.ShowItemIdTip(_itemId, 1);
            }
        }
        private void SetShopCellDatum(int type)
        {
            var _list = mStoreCache[type];
            var _cellList = new List<StoreCellData>();
            foreach (var cellData in _list)
            {
                var _tbStore = Table.GetStore(cellData.StoreIndex);
                cellData.nSort = _tbStore.Order;

                if (_tbStore.BuyCondition == -1)
                {
                    cellData.BuyLimit = "";
                }
                else
                {
                    var _tbCondition = Table.GetConditionTable(_tbStore.BuyCondition);
                    var VipLimit = _tbCondition.ExdataMin[0];
                    cellData.BuyLimit = string.Format(GameUtils.GetDictionaryText(100001383), VipLimit);
                    var VIPLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
                    if (VIPLevel >= VipLimit)
                    {
                        cellData.LimitColor = GameUtils.GetTableColor(24);
                    }
                    else
                    {
                        cellData.LimitColor = GameUtils.GetTableColor(25);
                    }
                }

                var _limit = _tbStore.DayCount;
                if (_limit == -1)
                {
                    _limit = _tbStore.WeekCount;
                }
                if (_limit == -1)
                {
                    _limit = _tbStore.MonthCount;
                }
                cellData.ExData = _limit;
                if (_limit == -1)
                {
                    cellData.Limit = -1;
                }
                else
                {
                    //钻石商店要刷新vip影响的数量
                    if (DataModel.StoreType == 15 || DataModel.StoreType == 16 || DataModel.ShowType == 18)
                    {
                        cellData.Limit = PlayerDataManager.Instance.GetExData(_limit) + VipAlterCount(cellData.StoreIndex);
                    }
                    else
                    {
                        cellData.Limit = PlayerDataManager.Instance.GetExData(_limit);
                    }
                }

                if (PlayerDataManager.Instance.CheckCondition(_tbStore.DisplayCondition) != 0)
                {
                    continue;
                }
                IsCanUse(_tbStore, cellData);
                _cellList.Add(cellData);
            }
            if (DataModel.ShowType == 18)//VIP商城
            {
                _cellList.Sort(SortVipCellData);
            }
            else
            {
                _cellList.Sort((a, b) => { return a.nSort - b.nSort; });
            }
            DataModel.ItemList = new ObservableCollection<StoreCellData>(_cellList);
        }

        private static int SortVipCellData(StoreCellData a, StoreCellData b)
        {
            var v1 = 0;
            var v2 = 0;
            var curVipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);

            {//数据A
                var _tbStoreA = Table.GetStore(a.StoreIndex);
                var _tbConditionA = Table.GetConditionTable(_tbStoreA.BuyCondition);
                var VipLimitA = _tbConditionA.ExdataMin[0];
                if (VipLimitA == curVipLevel)
                {
                    v1 += 1000000;
                }
                else if (VipLimitA == curVipLevel + 1)
                {
                    v1 += 100000;
                }
                else if (VipLimitA < curVipLevel)
                {
                    v1 += 10000;
                }
            }

            {//数据B
                var _tbStoreB = Table.GetStore(b.StoreIndex);
                var _tbConditionB = Table.GetConditionTable(_tbStoreB.BuyCondition);
                var VipLimitB = _tbConditionB.ExdataMin[0];
                if (VipLimitB == curVipLevel)
                {
                    v2 += 1000000;
                }
                else if (VipLimitB == curVipLevel + 1)
                {
                    v2 += 100000;
                }
                else if (VipLimitB < curVipLevel)
                {
                    v2 += 10000;
                }
            }

            if (v1 > v2)
            {
                return -1;
            }
            if (v1 < v2)
            {
                return 1;
            }
            return a.nSort - b.nSort;
        }

        private void OnResourceChanged(IEvent ievent)
        {
            var e = ievent as Resource_Change_Event;
            var _resNum = PlayerDataManager.Instance.GetRes(DataModel.ResType);
            if (_resNum != -1)
            {
                DataModel.ResNum = _resNum;
            }
        }        
        /// <summary>
        /// 刷新数值
        /// _type:商店类型ID   needValue：需要花费的金钱
        /// </summary>
        private void RefreshNum(int _type, int needValue)
        {
            var _tbStoreType = Table.GetStoreType(_type);
            if (_tbStoreType != null)
            {
                DataModel.ResType = _tbStoreType.ResType;
                DataModel.ResNum = 0;
                if (_tbStoreType.ResType != -1)
                {
                    if (_tbStoreType.ResType < (int)eResourcesType.CountRes && _tbStoreType.ResType > (int)eResourcesType.InvalidRes)
                    {
                        var _resNum = PlayerDataManager.Instance.GetRes(_tbStoreType.ResType) - needValue;
                        if (_resNum != -1)
                        {
                            DataModel.ResNum = _resNum;
                        }
                    }
                }
            }
        }

        private IEnumerator ShopPurchaseCoroutine(int index, int count = 1)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreBuy(index, count, mServiceType);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    OnClickPurchaseInfoClose();
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _tbStore = Table.GetStore(index);
                        //购买成功
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(431));
                        if (_tbStore.Type != -1)
                        {
                            //RefreshNum(_tbStore.Type, _tbStore.NeedValue);
                        }
                        var _cellData = GainCellData(index);
                        if (_cellData.ExData != -1)
                        {
                            _cellData.Limit -= count;
                            if (_cellData.Limit <= 0)
                            {
                                _cellData.CanUse = false;
                            }

                            //钻石商城和绑钻商城通用相同扩展数据，同步刷新限购次数。
                            if (DataModel.StoreType == 15 || DataModel.StoreType == 16)
                            {
                                var _type = DataModel.StoreType == 15 ? 16 : 15;
                                if (mStoreCache.ContainsKey(_type))
                                {
                                    foreach (var item in mStoreCache[_type])
                                    {
                                        if (item.ItemId == _tbStore.ItemId)
                                        {
                                            item.Limit = _cellData.Limit;
                                            item.CanUse = _cellData.CanUse;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (_tbStore == null)
                        {
                            yield break;
                        }
                        var _flagId = _tbStore.BugSign;
                        if (_flagId != -1)
                        {
                            var _flag = PlayerDataManager.Instance.GetFlag(_flagId);
                            if (_flag == false)
                            {
                                PlayerDataManager.Instance.SetFlag(_flagId, true);
                                UpdateShopList();
                            }
                        }

                        if (DataModel.StoreType == 15)
                        {
                            PlatformHelper.UMEvent("DiamondShopExpend", _tbStore.Name.ToString(), _tbStore.NeedValue * count);
                        }

                        PlatformHelper.UMEvent("BuyItem", _tbStore.Name.ToString(), count);                         
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var _e = new ShowUIHintBoard(430);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_TreasureStoreItemCountNotEnough)
                    {
                        var _e = new ShowUIHintBoard(100003016);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("StoreBuy....StoreId= {0}...ErrorCode...{1}", index, _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("StoreBuy............State..." + _msg.State);
                }
            }
        }
        private IEnumerator ShopPurchaseEquipCoroutine(int index, int bagId, int bagIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreBuyEquip(index, bagId, bagIndex, mServiceType);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    OnClickPurchaseInfoClose();
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //购买成功
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(431));
                        var _cellData = GainCellData(index);
                        if (_cellData.ExData != -1)
                        {
                            _cellData.Limit -= 1;
                            if (_cellData.Limit <= 0)
                            {
                                _cellData.CanUse = false;
                            }
                        }
                        var _tbStore = Table.GetStore(index);
                        if (_tbStore == null)
                        {
                            yield break;
                        }
                        if (_msg.Response == 0)
                        {
                            DataModel.ReplaceEquip.ItemId = _tbStore.ItemId;
                            //检查属性变化
                            PlayerDataManager.Instance.RefreshEquipBagStatus(DataModel.ReplaceEquip);
                        }
                        var _flagId = _tbStore.BugSign;
                        if (_flagId != -1)
                        {
                            var _flag = PlayerDataManager.Instance.GetFlag(_flagId);
                            if (_flag == false)
                            {
                                PlayerDataManager.Instance.SetFlag(_flagId, true);
                                UpdateShopList();
                            }
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var _e = new ShowUIHintBoard(430);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("StoreBuy............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("StoreBuy............State..." + _msg.State);
                }
            }
        }
        private void UpdateShopList()
        {
            SetShopCellDatum(DataModel.StoreType);
        }
        private int VipAlterCount(int StoreId)
        {
            var _ret = 0;
            if (DataModel.StoreType != 15 && DataModel.StoreType != 16 && DataModel.StoreType != 18)
            {
                return _ret;
            }

            if (mVipModifyCache.ContainsKey(StoreId))
            {
                _ret = mVipModifyCache[StoreId];
            }

            return _ret;
        }
        #endregion

        #region 事件
        private void OnRefurbishEquipBagItemStatusEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_BagChange;
            if (_e.HasType(eBagType.Equip))
            {
                if (State == FrameState.Open)
                {
                    PlayerDataManager.Instance.RefreshEquipBagStatus();
                }
            }
        }

        private void OnShopCellClickEvent(IEvent ievent)
        {
            var _e = ievent as StoreCellClick;
            var _cellData = _e.CellData;
            var _tbStore = Table.GetStore(_cellData.StoreIndex);
            if (_tbStore == null)
            {
                return;
            }
            var _tbItem = Table.GetItemBase(_tbStore.ItemId);
            if (_tbItem == null)
            {
                return;
            }

            //钻石商店会卖资源，所以特殊处理
            if (DataModel.StoreType != 15 && DataModel.StoreType != 16 && DataModel.StoreType != 1106)
            {
                // 对于普通商店来说 ExData==-1 那么一定 limit == -1   所以替换掉没影响
                // 对于神秘商店来说一定是ExData==-1  如果limit == -1 就是不限购显示最大叠加数 如果limit ！= -1 超过最大叠加数 显示最大叠加数 不超过显示limit 
                // if (cellData.ExData == -1)
                if (_cellData.Limit == -1)
                {
                    DataModel.MaxCount = _tbItem.MaxCount;
                }
                else
                {
                    DataModel.MaxCount = _cellData.Limit > _tbItem.MaxCount ? _tbItem.MaxCount : _cellData.Limit;
                }
            }
            else
            {
                if (_cellData.ExData == -1)
                {
                    DataModel.MaxCount = 99;
                }
                else
                {
                    DataModel.MaxCount = _cellData.Limit > 99 ? 99 : _cellData.Limit;
                }
            }


            if (DataModel.MaxCount == 0)
            {
                //已达到限购数量
                var _e1 = new ShowUIHintBoard(270118);
                EventDispatcher.Instance.DispatchEvent(_e1);
                return;
            }
            DataModel.SelectId = _cellData.StoreIndex;
            DataModel.HasReplace = false;//每次都重置下

            var tbStore = Table.GetStore(DataModel.SelectId);
            if (null != tbStore)
            {
                var tbCondition = Table.GetConditionTable(tbStore.BuyCondition);
                if (null != tbCondition)
                {
                    var flagid = tbCondition.TrueFlag[0];
                    NameTitleRecord tbNameTitle = null;
                    var nametitleid = 0;
                    Table.ForeachNameTitle(tb =>
                    {
                        if (tb.TitleType == 1)
                        {
                            if (PlayerDataManager.Instance.GetFlag(tb.FlagId))
                            {
                                tbNameTitle = tb;
                            }
                            if (flagid == tb.FlagId)
                            {
                                nametitleid = tb.Id;
                            }
                        }
                        return true;
                    });
                    if (null == tbNameTitle)
                    {
                        tbNameTitle = Table.GetNameTitle(2000);
                    }
                    if (null != tbNameTitle)
                    {
                        if (tbNameTitle.Id < nametitleid)
                        {
                            DataModel.DescColor = GameUtils.GetTableColor(5);
                            RankCondition = tbCondition.FlagTrueDict;
                        }
                        else
                        {
                            DataModel.DescColor = GameUtils.GetTableColor(14);
                            RankCondition = -1;
                        }
                        DataModel.BuyConditionDesc = GameUtils.GetDictionaryText(tbCondition.FlagTrueDict);
                        DataModel.IsShowRank = true;
                    }
                }
                else
                {
                    DataModel.IsShowRank = false;
                }
            }
            DataModel.SelectCount = 1;
            if (DataModel.StoreType >= 100)
            {
                //装备兑换
                if (_tbStore.NeedItem == -1)
                {
                    DataModel.ReplaceList.Clear();
                    DataModel.ReplaceEquip = emptyBagItemData;
                }
                else
                {
                    var _tbEquip = Table.GetEquipBase(_tbStore.NeedItem);
                    if (_tbEquip == null)
                    {
                        return;
                    }
                    
                    var _list = new List<BagItemDataModel>();
                    PlayerDataManager.Instance.ForeachEquip(equip =>
                    {
                        if (equip.ItemId == _tbStore.NeedItem)
                        {
                            _list.Add(equip);
                        }
                    });
                    DataModel.ReplaceFalg = "";
                    if (_list.Count > 0)
                    {
                        DataModel.ReplaceList = new ObservableCollection<BagItemDataModel>(_list);
                        DataModel.ReplaceEquip = DataModel.ReplaceList[0];
                        if (_list.Count == 2)
                        {
                            DataModel.HasReplace = true;
                            var _bagType = DataModel.ReplaceEquip.BagId;
                            var _bagIndex = DataModel.ReplaceEquip.Index;
                            switch ((eBagType)_bagType)
                            {
                                case eBagType.Equip07:
                                {
                                    if (_bagIndex == 0)
                                    {
                                        DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271002); //"(左手)";
                                    }
                                    else
                                    {
                                        DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271003); //"(右手)";  
                                    }
                                }
                                    break;
                                case eBagType.Equip11:
                                {
                                    DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271004); //"(主手)";
                                }
                                    break;
                                case eBagType.Equip12:
                                {
                                    DataModel.ReplaceFalg = GameUtils.GetDictionaryText(271005); //"(副手)";
                                }
                                    break;
                            }
                        }
                        else
                        {
                            DataModel.HasReplace = false;
                        }
                    }
                    else
                    {
                        DataModel.HasReplace = false;
                        DataModel.ReplaceList.Clear();
                        DataModel.ReplaceEquip = emptyBagItemData;
                    }
                    DataModel.ReplaceIndex = 0;
                }

                if (_tbItem.Type == 2100)
                {
                    DataModel.RoleId = _tbItem.Exdata[1];
                }
                else
                {
                    DataModel.RoleId = -1;
                }
            }
            else
            {
                //普通商店
                //if (tbItem.MaxCount == 1)
                //{
                //    OnClickBuyInfoBuy();
                //}
                //else
                //{
                if (_tbItem.Type == 2100)
                {
                    DataModel.RoleId = _tbItem.Exdata[1];
                }
                else
                {
                    DataModel.RoleId = -1;
                }
                //}
            }
        }
        private void OnStoreOperaEvent(IEvent ievent)
        {
            var _e = ievent as StoreOperaEvent;
            switch (_e.Type)
            {
                case 9:
                {
                    OnClickShowSelectedIcon();
                }
                    break;
                case 10:
                {
                    OnClickReplaced();
                }
                    break;
                case 11:
                {
                    OnClickPurchaseInfoClose();
                }
                    break;
                case 12:
                {
                    OnClickPurchaseInfoPurchase();
                }
                    break;
                case 13:
                {
                    OnClickPurchaseInfoMax();
                }
                    break;
                case 14:
                {
                    OnClickPurchaseInfoAdd();
                }
                    break;
                case 15:
                {
                    OnClickPurchaseInfoDel();
                }
                    break;
                case 16:
                {
                    OnClickPressCount(true, true);
                }
                    break;
                case 17:
                {
                    OnClickPressCount(false, true);
                }
                    break;
                case 18:
                {
                    OnClickPressCount(true, false);
                }
                    break;
                case 19:
                {
                    OnClickPressCount(false, false);
                }
                    break;
            }
        }
        //商店trigger
        private void OnStoreCacheTriggerEvent(IEvent ievent)
        {
            var _mTime = Game.Instance.ServerTime.Date.AddDays(1);
            if (mStoreCacheObject != null)
            {
                TimeManager.Instance.DeleteTrigger(mStoreCacheObject);
                mStoreCacheObject = null;
            }
            mStoreCacheObject = TimeManager.Instance.CreateTrigger(_mTime, () => { mStoreCache.Clear(); },
                (int)TimeSpan.FromDays(1).TotalMilliseconds);
        }
        private void OnUpdateFuBenStoreLimitItemsEvent(IEvent ievent)
        {
            var _e = ievent as UpdateFuBenStoreStore_Event;
            if (_e == null) return;

            var _storeType = _e.mStoreType;
            if (!mStoreCache.ContainsKey(_storeType)) return;

            if (_e.Items == null) return;

            foreach (var item in _e.Items.items)
            {
                var _cellData = GainCellData(item.itemid);
                if (_cellData != null)
                {
                    _cellData.Limit = item.itemcount;
                }
                SetShopCellDatum(_storeType);
            }
        }

        private void OnClearFuBenItemsEvent(IEvent ievent)
        {
            var _e = ievent as LoadSceneOverEvent;
            if (_e == null) return;

            if (mServiceType != -1)
            {
                Table.ForeachService(tableService =>
                {
                    if (tableService.Type == mServiceType)
                    {
                        if (tableService.Param.Count() > 0 && mStoreCache.ContainsKey(tableService.Param[0]))
                        {
                            mStoreCache[tableService.Param[0]].Clear();
                            mStoreCache.Remove(tableService.Param[0]);
                        }
                    }

                    return true;
                });
                mServiceType = -1;
            }
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new StoreDataModel();
            BackPack = UIManager.Instance.GetController(UIConfig.BackPackUI);
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            throw new NotImplementedException(name);
        }

        public void OnShow()
        {
            BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Character });
            if (PlayerDataManager.Instance.isTaskWildShop)
            {
                DataModel.IsNPC = false;
            }
            else
            {
                DataModel.IsNPC = true;
            }
        }

        public void Close()
        {
            //DataModel.IsNPC = false;                          
        }

        public void Tick()
        {
        }
        private Stack<UIInitArguments> OpenTabStack = new Stack<UIInitArguments>();
        private void OnStoreUIRefreshEvent(IEvent iEvent)
        {
            if (OpenTabStack.Count > 0)
            {
                var ar = OpenTabStack.Peek();
                RefreshData(ar);
            }
        }
        private void OnMainUIRefreshEvent(IEvent iEvent)
        {
            var e = iEvent as Event_RefreshFuctionOnState;
            if (null == e || OpenTabStack.Count <= 0)
            {
                return;
            }
            OpenTabStack.Clear();                
        }
        

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as StoreArguments;
            if (_args == null)
            {
                return;
            }
            mServiceType = -1;
            BackPack.RefreshData(null);
            BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Character });
            DataModel.SelectId = -1;
            DataModel.SelectCount = 1;
            DataModel.MaxCount = -1;
            var _type = _args.Tab;
            DataModel.StoreType = _type;
            if (_type != 15 && _type != 16 && _type != 18)
            {
                //队列中不加入商城中的type
                OpenTabStack.Push(_args);                
            }
            var _tbStoreType = Table.GetStoreType(_type);
            if (_tbStoreType != null)
            {
                DataModel.ResType = _tbStoreType.ResType;
                DataModel.ResNum = 0;
                if (_tbStoreType.ResType != -1)
                {
                    if (_tbStoreType.ResType < (int)eResourcesType.CountRes && _tbStoreType.ResType > (int)eResourcesType.InvalidRes)
                    {
                        var _resNum = PlayerDataManager.Instance.GetRes(_tbStoreType.ResType);
                        if (_resNum != -1)
                        {
                            DataModel.ResNum = _resNum;
                        }
                    }
                }
            }

            if (DataModel.StoreType > 100)
            {
                DataModel.ReplaceEquip = emptyBagItemData;
            }
            DataModel.ReplaceList.Clear(); 
            DataModel.ShowType = _args.Tab;

            if (_type == 15 || _type == 16 || _type == 18)
            {
                mVipModifyCache.Clear();
                var _table = Table.GetVIP(PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel));
                for (var i = 0; i < _table.BuyItemId.Length; i++)
                {
                    var _id = _table.BuyItemId[i];
                    if (_id != -1)
                    {
                        mVipModifyCache.Add(_id, _table.BuyItemCount[i]);
                    }
                }
            }
            if (_args.Args != null && _args.Args.Count > 0)
            {
                mServiceType = _args.Args[0];
                NetManager.Instance.StartCoroutine(ApplyShopsCoroutine(_type, _args.Args[0]));
            }
            else
            {
                NetManager.Instance.StartCoroutine(ApplyShopsCoroutine(_type));
            }
            // 	    if (2==type)
            // 	    {
            // 			if (PlayerDataManager.Instance.GetFlag(522))
            // 		    {
            // 				var list = new Int32Array();
            // 				list.Items.Add(523);
            // 
            // 				var list1 = new Int32Array();
            // 				list1.Items.Add(522);
            // 				PlayerDataManager.Instance.SetFlagNet(list, list1);
            // 		    }
            // 			
            // 	    }
        }



        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

    }
}