/********************************************************************************* 

                         Scorpion



  *FileName:CorePurchaseFrameCtrler

  *Version:1.0

  *Date:2017-06-19

  *Description:

**********************************************************************************/
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
using Shared;

#endregion

namespace ScriptController
{
    public class CorePurchaseFrameCtrler : IControllerBase
    {
        #region 构造函数
        public CorePurchaseFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(QuickBuyOperaEvent.EVENT_TYPE, OnCorePurchaseOperaEvent);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResExchangeEvent);
        }
        #endregion

        #region 成员变量
        private QuickBuyDataModel DataModel;
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new QuickBuyDataModel();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
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

        public void OnShow()
        {
        }

        private bool IsQuickBuyGift = true;
        public void RefreshData(UIInitArguments ievent)
        {
            var _e = ievent as QuickBuyArguments;
            if (_e == null)
            {
                return;
            }

            if (_e.Items.Count == 1)
            {
                var enumerator1 = _e.Items.GetEnumerator();
                if (enumerator1.MoveNext())
                {
                    var itemId = enumerator1.Current.Key;
                    var item = Table.GetItemBase(itemId);
                    if (item == null)
                    {
                        return;
                    }

                    var tbStore = Table.GetStore(item.StoreID);
                    if (tbStore == null)
                    {
                        return;
                    }
                    IsQuickBuyGift = GameUtils.IsQuickBuyGift(tbStore.ItemId);
                    if (IsQuickBuyGift)
                    { // 礼包
                        RefurbishPresentPurchase(itemId, enumerator1.Current.Value);
                    }
                    else
                    { // 一个物品
                        RefurbishOnePurchase(itemId, enumerator1.Current.Value);
                    }
                }
            }
            else if (_e.Items.Count > 1)
            { // 多个
                RefurbishMultyPurchase(_e.Items);
            }
        }
        public FrameState State { get; set; }
        #endregion

        #region 逻辑函数
        private int GetImageByWaresForm(int type)
        {
            switch (type)
            {
                case 0:
                    return 60000;
                case 1:
                    return 600001;
                case 2:
                    return 600003;
                case 3:
                    return 600002;
            }
            return -1;
        }

        private void RefurbishOnePurchase(int itemId, int itemCount)
        {
            DataModel.Type = 0;

            var _item = Table.GetItemBase(itemId);
            if (_item == null || _item.StoreID == -1)
            {
                return;
            }

            var _tbStore = Table.GetStore(_item.StoreID);
            if (_tbStore == null)
            {
                return;
            }

            DataModel.OneBuy.StoreId = _item.StoreID;
            var _maxCount = PlayerDataManager.Instance.GetMaxBuyCount(itemId);
            if (_maxCount == -1)
            {
                _maxCount = 99;
            }

            DataModel.OneBuy.MaxBuyCount = _maxCount;

            DataModel.OneBuy.Item.ItemId = _item.Id;
            DataModel.OneBuy.Item.HaveCount = _tbStore.ItemCount;
            DataModel.OneBuy.Item.RtIconId = GetImageByWaresForm(_tbStore.GoodsType);
            DataModel.IsShowDiscount = DataModel.OneBuy.Item.RtIconId;

            DataModel.Currency = _tbStore.NeedType;
            OnePurchaseSetFigure(itemCount);
        }

        private void RefurbishMultyPurchase(Dictionary<int, int> items)
        {
            DataModel.Type = 1;

            DataModel.OriginalPrice = 0;
            DataModel.DiscountPrice = 0;
            DataModel.MultyBuy.ItemList.Clear();
            var _enumerator = items.GetEnumerator();
            while (_enumerator.MoveNext())
            {
                var _itemData = new ItemBuyDataModel();
                _itemData.ItemId = _enumerator.Current.Key;
                _itemData.Count = _enumerator.Current.Value;
                DataModel.GiftBuy.Item.Count = _enumerator.Current.Value;
                var _item = Table.GetItemBase(_itemData.ItemId);
                if (_item == null || _item.StoreID == -1)
                {
                    continue;
                }

                var _tbStore = Table.GetStore(_item.StoreID);
                if (_tbStore != null)
                {
                    _itemData.HaveCount = _tbStore.ItemCount;
                    _itemData.RtIconId = GetImageByWaresForm(_tbStore.GoodsType);
                    var oldValue = _tbStore.Old;
                    if (oldValue == -1)
                    {
                        oldValue = _tbStore.NeedValue;//策划需求：商品原价为-1，则原价==实际价格
                    }
                    DataModel.OriginalPrice += _itemData.Count * oldValue;
                    //DataModel.OriginalPrice += _itemData.Count * _tbStore.Old;
                    DataModel.DiscountPrice += _itemData.Count * _tbStore.NeedValue;
                    DataModel.Currency = _tbStore.NeedType;
                    DataModel.IsShowDiscount = _itemData.RtIconId;
                }
                DataModel.MultyBuy.ItemList.Add(_itemData);
            }

            RenovateSpend();
        }

        private void RefurbishPresentPurchase(int itemId, int itemCount)
        {
            DataModel.Type = 2;

            var _item = Table.GetItemBase(itemId);
            if (_item == null || _item.StoreID == -1)
            {
                return;
            }

            var _tbStore = Table.GetStore(_item.StoreID);
            if (_tbStore == null)
            {
                return;
            }

            DataModel.GiftBuy.NeedItemId = itemId;

            DataModel.GiftBuy.Item.ItemId = _tbStore.ItemId;
            DataModel.GiftBuy.Item.Count = itemCount;
            DataModel.GiftBuy.Item.HaveCount = _tbStore.ItemCount;
            DataModel.GiftBuy.Item.RtIconId = GetImageByWaresForm(_tbStore.GoodsType);
            DataModel.IsShowDiscount = DataModel.GiftBuy.Item.RtIconId;

            var oldValue = _tbStore.Old;
            if (oldValue == -1)
            {
                oldValue = _tbStore.NeedValue;//策划需求：商品原价为-1，则原价==实际价格
            }
            DataModel.OriginalPrice = itemCount * oldValue;
            //DataModel.OriginalPrice = itemCount * _tbStore.Old;
            DataModel.DiscountPrice = itemCount * _tbStore.NeedValue;

            DataModel.Currency = _tbStore.NeedType;

            RenovateSpend();
        }

        private void OnePurchaseSetFigure(int count)
        {
            if (count < 1)
                count = 1;

            if (count > DataModel.OneBuy.MaxBuyCount)
                count = DataModel.OneBuy.MaxBuyCount;

            var _tbStore = Table.GetStore(DataModel.OneBuy.StoreId);
            if (_tbStore != null)
            {
                DataModel.OneBuy.BuyCount = count;
                DataModel.OneBuy.Item.Count = count;
                DataModel.OriginalPrice = count * _tbStore.Old;
                DataModel.DiscountPrice = count * _tbStore.NeedValue;
            }

            RenovateSpend();
        }

        private void RenovateSpend()
        {
            var _have = PlayerDataManager.Instance.GetRes(DataModel.Currency);
            if (_have >= DataModel.OriginalPrice)
            {
                DataModel.OriginalColor = MColor.white;
            }
            else
            {
                DataModel.OriginalColor = MColor.red;
            }

            if (_have >= DataModel.DiscountPrice)
            {
                DataModel.DiscountlColor = MColor.white;
            }
            else
            {
                DataModel.DiscountlColor = MColor.red;
            }

            if (DataModel.OriginalPrice != 0)
            {
                var _dis = Math.Floor(10 * (double)DataModel.DiscountPrice / DataModel.OriginalPrice);
                DataModel.Discount = string.Format(GameUtils.GetDictionaryText(100001165), (int)_dis);
            }
        }

        private void PurchaseShopProvision(int storeId, int count)
        {
            if ((IsQuickBuyGift && DataModel.GiftBuy.Item.Count <= 0) || (!IsQuickBuyGift && DataModel.OneBuy.MaxBuyCount <= 0))
            {
                 GameUtils.ShowHintTip(200002651);
                 return;
            }
            var _tbStore = Table.GetStore(storeId);
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
            var _cost = _tbStore.NeedValue * count;
            if (PlayerDataManager.Instance.GetRes(_tbStore.NeedType) < _cost)
            {
                var _tbItemCost = Table.GetItemBase(_tbStore.NeedType);
                //{0}不足！
                var _str = GameUtils.GetDictionaryText(701);
                _str = string.Format(_str, _tbItemCost.Name);
                GameUtils.ShowHintTip(_str);
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame,
                    new RechargeFrameArguments { Tab = 0 }));
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.QuickBuyUi));
                return;
            }
            if (_tbStore.NeedItem == -1)
            {
                NetManager.Instance.StartCoroutine(ShopPurchaseCoroutine(storeId, count));
            }
        }

        private IEnumerator ShopPurchaseCoroutine(int index, int count = 1)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreBuy(index, count, (int)NpcService.NsShop);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (DataModel.Type != 2)
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.QuickBuyUi));

                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _tbStore = Table.GetStore(index);
                        //购买成功
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(431));
                        PlayerDataManager.Instance.NoticeData.RefGoldBlessNum = true;
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
                            }
                        }

                        PlatformHelper.UMEvent("BuyItem", _tbStore.Name, count);                 
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var _e = new ShowUIHintBoard(430);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("QuickBuy StoreBuy....StoreId= {0}...ErrorCode...{1}", index, _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("QuickBuy StoreBuy............State..." + _msg.State);
                }
            }
        }
        #endregion

        #region 事件
        private void OnCorePurchaseOperaEvent(IEvent ievent)
        {
            var _e = ievent as QuickBuyOperaEvent;
            if (_e == null)
            {
                return;
            }

            switch (_e.Type)
            {
                case 0:
                { // 减少数量
                    OnePurchaseSetFigure(DataModel.OneBuy.BuyCount - 1);
                }
                    break;

                case 3:
                {
                    OnePurchaseSetFigure(DataModel.OneBuy.BuyCount + 1);
                }
                    break;

                case 11:
                {
                    PurchaseShopProvision(DataModel.OneBuy.StoreId, DataModel.OneBuy.BuyCount);
                }
                    break;
                case 12:
                {
                    var _have = PlayerDataManager.Instance.GetRes(DataModel.Currency);
                    if (_have < DataModel.DiscountPrice)
                    {
                        var _tbItemCost = Table.GetItemBase(DataModel.Currency);
                        var _str = GameUtils.GetDictionaryText(701);
                        _str = string.Format(_str, _tbItemCost.Name);
                        GameUtils.ShowHintTip(_str);
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame,
                            new RechargeFrameArguments { Tab = 0 }));
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.QuickBuyUi));
                        return;
                    }

                    var _enumerator = DataModel.MultyBuy.ItemList.GetEnumerator();
                    while (_enumerator.MoveNext())
                    {
                        var _buyItem = _enumerator.Current;
                        if (_buyItem != null)
                        {
                            var _tbItem = Table.GetItemBase(_buyItem.ItemId);
                            if (_tbItem == null)
                            {
                                continue;
                            }
                            PurchaseShopProvision(_tbItem.StoreID, _buyItem.Count);
                        }
                    }
                }
                    break;
                case 13:
                {
                    var _item = Table.GetItemBase(DataModel.GiftBuy.NeedItemId);
                    if (_item == null || _item.StoreID == -1)
                    {
                        return;
                    }

                    PurchaseShopProvision(_item.StoreID, DataModel.GiftBuy.Item.Count);
                }
                    break;
                case 21:
                {
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame,
                        new RechargeFrameArguments { Tab = 2 }));
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.QuickBuyUi));
                }
                    break;
            }
        }

        private void OnResExchangeEvent(IEvent ievent)
        {
            if (State != FrameState.Open)
            {
                return;
            }

            var _e = ievent as Resource_Change_Event;
            if (_e != null && (int)_e.Type == DataModel.Currency)
            {
                RenovateSpend();
            }
        }
        #endregion
    }
}
