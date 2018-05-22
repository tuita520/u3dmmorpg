
#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
    public class ShiZhuangController : IControllerBase
    {
        private ShiZhuangDataModel DataModel;
        private const int EquipStoreTypeId = 1300;
        private const int WingStoreTypeId = 1301;
        private const int WeaponStoreTypeId = 1302;
        private bool IsInitEquipStateFlag = false;
        private object _refreshTimer;
        Dictionary<int, BagItemDataModel> limitFashions = new Dictionary<int, BagItemDataModel>();

        private enum eShiZhuangBagType
        {
            Equip = 26,
            Wing = 27,
            Weapon = 28,
        }

        #region ----------------------------------------------基类接口----------------------------------------------
        public ShiZhuangController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ShiZhuangSelectTypeEvent.EVENT_TYPE, SetSelectType);
            EventDispatcher.Instance.AddEventListener(ShiZhuangChangeTabEvent.EVENT_TYPE, ChangeShowTab);
            EventDispatcher.Instance.AddEventListener(ShiZhuangOperaEvent.EVENT_TYPE, ShiZhuangOpera);
            EventDispatcher.Instance.AddEventListener(ShiZhuangStoreCellClick.EVENT_TYPE, StoreCellClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnBagChangeEvent);
            EventDispatcher.Instance.AddEventListener(BagDataInitEvent.EVENT_TYPE, OnBagDataInit);
            EventDispatcher.Instance.AddEventListener(ShiZhuangItemUseEvent.EVENT_TYPE, ShiZhuangItemUse);
            EventDispatcher.Instance.AddEventListener(SetEquipModelStateEvent.EVENT_TYPE, SetEquipModelState);
        }

        public FrameState State { get; set; }

        public void CleanUp()
        {
            DataModel = new ShiZhuangDataModel();
            DataModel.Type = 1;
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
            var _args = data as ShiZhuangUIArguments;
            if (_args != null)
            {
                var _type = _args.Tab;
                DataModel.Type = _type;
            }
            else
            {
                DataModel.Type = 1;
                DataModel.ShowTab = 0;
            }
            RefreshBagAndStoreInfo();
            InitEquipState();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "UpdatePartData")
            {
                UpdatePartData(param[0] as ItemsChangeData);
            }
            if (name == "QuickUseFashion")
            {
                QuickUseShiZhuang(param[0] as BagItemDataModel);
            }
            return null;
        }

        #endregion ----------------------------------------------基类接口----------------------------------------------

        #region ----------------------------------------------回调函数----------------------------------------------

        private void SetEquipModelState(IEvent iEvent)
        {
            var e = iEvent as SetEquipModelStateEvent;
            if (null == e)
                return;
        }

        private void SetSelectType(IEvent iEvent)
        {
            var e = iEvent as ShiZhuangSelectTypeEvent;
            if (null == e)
                return;
            var type = e.Type;
            DataModel.Type = type;
            RefreshBagAndStoreInfo();
        }

        private void OnBagDataInit(IEvent ievent)
        {
            UpdataFashionInfo();
        }

        private void OnBagChangeEvent(IEvent iEvent)
        {
            var e = iEvent as UIEvent_BagChange;
            if (null == e)
                return;
            if (e.HasType(eBagType.EquipShiZhuangBag) ||
                e.HasType(eBagType.WingShiZhuangBag) ||
                e.HasType(eBagType.WeaponShiZhuangBag))
            {
                RefreshBagAndStoreInfo();
            }
        }

        private void ShiZhuangItemUse(IEvent iEvent)
        {
            var e = iEvent as ShiZhuangItemUseEvent;
            if (null == e)
                return;
            var bagId = 0;
            var part = 0;
            var bagItem = e.ItemData;
            GetUseInfo(bagItem, ref bagId, ref part);
            NetManager.Instance.StartCoroutine(UseShiZhuangCoroutine(bagId, bagItem.Index, part));
        }

        private void QuickUseShiZhuang(BagItemDataModel bagItemData)
        {
            var bagId = bagItemData.BagId;
            var index = bagItemData.Index;
            var part = 0;
            if (bagId == (int)eBagType.EquipShiZhuangBag)
            {
                part = (int)eBagType.EquipShiZhuang;
            }
            NetManager.Instance.StartCoroutine(UseShiZhuangCoroutine(bagId, index, part, true));
        }

        private void ShiZhuangOpera(IEvent iEvent)
        {
            var e = iEvent as ShiZhuangOperaEvent;
            if (null == e)
                return;
            var type = e.Type;
            switch (type)
            {
                case 0://关闭确认购买界面
                    {
                        DataModel.IsShowConfirmInfo = false;
                    }
                    break;
                case 1://确认购买
                    {
                        FashionStoreBuy();
                    }
                    break;
                case 2://隐藏时装装备
                    {
                        ChangeEquipState();
                    }
                    break;
                case 3://隐藏时装武器
                    {
                        ChangeWeaponState();
                    }
                    break;
                case 4://隐藏时装翅膀和普通翅膀
                    {
                        ChangeWingState();
                    }
                    break;
            }
        }

        private void StoreCellClickEvent(IEvent iEvent)
        {
            var _e = iEvent as ShiZhuangStoreCellClick;
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

            if (_cellData.ExData == -1)
            {
                DataModel.MaxCount = 99;
            }
            else
            {
                DataModel.MaxCount = _cellData.Limit > 99 ? 99 : _cellData.Limit;
            }

            if (DataModel.MaxCount == 0)
            {
                var _e1 = new ShowUIHintBoard(270118);//已达到限购数量
                EventDispatcher.Instance.DispatchEvent(_e1);
                return;
            }
            DataModel.RoleId = _tbItem.OccupationLimit;
            DataModel.SelectId = _cellData.StoreIndex;
            DataModel.SelectCount = 1;
            DataModel.IsShowConfirmInfo = true;
        }

        /// <summary>
        /// 0:背包 1:商城
        /// </summary>
        private void ChangeShowTab(IEvent iEvent)
        {
            var e = iEvent as ShiZhuangChangeTabEvent;
            if (null == e)
                return;
            var tab = e.Tab;
            DataModel.ShowTab = tab;
            RefreshBagAndStoreInfo();
        }
        #endregion ----------------------------------------------回调函数----------------------------------------------

        #region ----------------------------------------------逻辑函数-------------------------------------------------

        private void FashionStoreBuy()
        {
            var index = DataModel.SelectId;
            var count = DataModel.SelectCount;
            var tbStore = Table.GetStore(index);
            if (tbStore == null)
                return;
            var cost = tbStore.NeedValue * count;
            if (PlayerDataManager.Instance.GetRes(tbStore.NeedType) < cost)
            {
                var _tbItemCost = Table.GetItemBase(tbStore.NeedType);
                var str = GameUtils.GetDictionaryText(701);
                str = string.Format(str, _tbItemCost.Name);
                GameUtils.ShowHintTip(str);
                PlayerDataManager.Instance.ShowItemInfoGet(tbStore.NeedType);
                DataModel.IsShowConfirmInfo = false;
                return;
            }
            NetManager.Instance.StartCoroutine(ShopPurchaseCoroutine(DataModel.SelectId, DataModel.SelectCount));
        }

        private void InitEquipState()
        {
            if (!IsInitEquipStateFlag)
            {
                var equipList = PlayerDataManager.Instance.PlayerDataModel.EquipList;
                if (equipList[10].ItemId == -1)
                    DataModel.IsHideEquip = false;
                else
                    DataModel.IsHideEquip = equipList[10].Exdata.ModelState == 0 ? true : false;

                InitWingState();

                if (equipList[12].ItemId == -1)
                    DataModel.IsHideWeapon = false;
                else
                    DataModel.IsHideWeapon = equipList[12].Exdata.ModelState == 0 ? true : false;

                IsInitEquipStateFlag = true;
            }
        }

        private void InitWingState()
        {
            //if (equipList[11].ItemId == -1)
            //    DataModel.IsHideWing = false;
            //else
            //    DataModel.IsHideWing = equipList[11].Exdata.ModelState == 0 ? true : false;
            //if (!DataModel.IsHideWing)
            //{
                var wingCon = UIManager.Instance.GetController(UIConfig.WingUI);
                if (wingCon != null)
                {
                    var wingDataModel = wingCon.GetDataModel("") as WingDataModel;
                    if (wingDataModel.ItemData.ItemId == -1)
                    {
                        DataModel.IsHideWing = false;
                        return;
                    }
                    if (wingDataModel.ItemData.ExtraData[11] == 0)
                    {
                        DataModel.IsHideWing = true;
                    }
                    else
                    {
                        DataModel.IsHideWing = false;
                    }
                }
            //}
        }

        private IEnumerator RefreshFashionInfoCoroutine(int placeholder = 0)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.RefreshFashionInfo(placeholder);
                yield return msg.SendAndWaitUntilDone();
            }
        }

        private void ChangeEquipState()
        {
            var state = !DataModel.IsHideEquip;
            var parts = new Int32Array();
            parts.Items.Add((int)eBagType.EquipShiZhuang);
            NetManager.Instance.StartCoroutine(SetEquipModelStateCoroutine(parts, state));
        }

        private void ChangeWingState()
        {
            var state = !DataModel.IsHideWing;
            var parts = new Int32Array();
            parts.Items.Add((int)eBagType.Wing);
            NetManager.Instance.StartCoroutine(SetEquipModelStateCoroutine(parts, state));
        }

        private void ChangeWeaponState()
        {
        }

        private void RefreshLimitFashions()
        {
            //Debug.LogWarning(Game.Instance.ServerTime.ToString("yyyy/MM/dd HH:mm:ss"));
            var isNeedRefresh = false;
            var bags = PlayerDataManager.Instance.PlayerDataModel.Bags.Bags;
            for (int i = (int)eBagType.EquipShiZhuangBag; i < (int)eBagType.WeaponShiZhuangBag; i++)
            {
                var bag = bags[i];
                if (null != bag)
                {
                    foreach (var item in bag.Items)
                    {
                        if (item.ItemId == -1)
                            continue;
                        var limitTime = item.Exdata[32];
                        if (limitTime == -1)
                            continue;
                        if (!limitFashions.ContainsKey(item.ItemId))
                        {
                            limitFashions.Add(item.ItemId, item);
                        }
                    }
                }
            }

            var equipList = PlayerDataManager.Instance.PlayerDataModel.EquipList;

            for (int i = 10; i < 13; i++)
            {
                var equip = equipList[i];
                if (equip.ItemId == -1)
                    continue;
                var limitTime = equip.Exdata[32];
                if (limitTime == -1)
                    continue;
                if (!limitFashions.ContainsKey(equip.ItemId))
                {
                    limitFashions.Add(equip.ItemId, equip);
                }
            }

            foreach (var limitFashion in limitFashions)
            {
                var nowDate = DataTimeExtension.GetTimeStampSeconds(Game.Instance.ServerTime);
                var limitDate = limitFashion.Value.Exdata[32];
                if (nowDate >= limitDate)
                {
                    if (null != _refreshTimer)
                    {
                        TimeManager.Instance.DeleteTrigger(_refreshTimer);
                        _refreshTimer = null;
                    }
                    //Debug.LogError("通知服务器删除到期时装: " + limitFashion.Key);
                    NetManager.Instance.StartCoroutine(RefreshFashionInfoCoroutine());
                    break;
                }
            }
        }

        private void UpdataFashionInfo()
        {
            return;//暂时关闭，下一版本加入
            if (!IsNeedCheckFashion())
                return;
            limitFashions.Clear();
            if (null != _refreshTimer)
            {
                TimeManager.Instance.DeleteTrigger(_refreshTimer);
                _refreshTimer = null;
            }
            _refreshTimer = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime, RefreshLimitFashions, 1000);
        }

        private void UpdatePartData(ItemsChangeData changeData)
        {
            UpdataFashionInfo();
        }

        private bool IsNeedCheckFashion()
        {
            var bags = PlayerDataManager.Instance.PlayerDataModel.Bags.Bags;
            for (int i = (int)eBagType.EquipShiZhuangBag; i < (int)eBagType.WeaponShiZhuangBag; i++)
            {
                var bag = bags[i];
                if (null != bag)
                {
                    foreach (var item in bag.Items)
                    {
                        if (item.ItemId == -1)
                            continue;
                        return true;
                    }
                }
            }

            var equipList = PlayerDataManager.Instance.PlayerDataModel.EquipList;

            for (int i = 10; i < 13; i++)
            {
                var equip = equipList[i];
                if (equip.ItemId == -1)
                    continue;
                return true;
            }

            return false;
        }

        private void RefreshBagAndStoreInfo()
        {
            switch (DataModel.Type)
            {
                case 0://翅膀
                    {
                        DataModel.BagNameStr = GameUtils.GetDictionaryText(290105);
                        DataModel.StoreNameStr = GameUtils.GetDictionaryText(290106);
                        NetManager.Instance.StartCoroutine(ApplyShiZhuangShopCoroutine(WingStoreTypeId));
                    }
                    break;
                case 1://装备
                    {
                        DataModel.BagNameStr = GameUtils.GetDictionaryText(290103);
                        DataModel.StoreNameStr = GameUtils.GetDictionaryText(290104);
                        NetManager.Instance.StartCoroutine(ApplyShiZhuangShopCoroutine(EquipStoreTypeId));
                    }
                    break;
                case 2://武器
                    {
                        DataModel.BagNameStr = GameUtils.GetDictionaryText(290107);
                        DataModel.StoreNameStr = GameUtils.GetDictionaryText(290108);
                        NetManager.Instance.StartCoroutine(ApplyShiZhuangShopCoroutine(WeaponStoreTypeId));
                    }
                    break;
            }
            RefreshBagInfo();
        }

        private void RefreshBagInfo()
        {
            var bagId = 0;
            switch (DataModel.Type)
            {
                case 0://翅膀
                    {
                        bagId = (int)eShiZhuangBagType.Wing;
                    }
                    break;
                case 1://装备
                    {
                        bagId = (int)eShiZhuangBagType.Equip;
                    }
                    break;
                case 2://武器
                    {
                        bagId = (int)eShiZhuangBagType.Weapon;
                    }
                    break;
            }
            var bagData = PlayerDataManager.Instance.PlayerDataModel.Bags.Bags[bagId];
            if (null != bagData)
            {
                if (bagData.Size <= 0)
                {
                    DataModel.IsShowRecommend = true;
                }
                else
                {
                    DataModel.IsShowRecommend = false;
                    GetBagData(bagData);
                }
            }
        }

        private void GetBagData(BagBaseDataModel bagData)
        {
            DataModel.BagItemsList.Clear();
            foreach (var item in bagData.Items)
            {
                if (item.ItemId != -1)
                {
                    DataModel.BagItemsList.Add(item);
                }
            }
        }

        private IEnumerator ApplyShiZhuangShopCoroutine(int type, int serviceType = -1)
        {
            using (new BlockingLayerHelper(0))
            {
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
                        Logger.Error("ApplyShiZhuangShop............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ApplyShiZhuangShop............State..." + _msg.State);
                }
            }
        }

        private void SetShopData(int type, List<StoneItem> shopItems)
        {
            var recommendItemList = new List<ShiZhuangStoreCellData>();
            var itemList = new List<ShiZhuangStoreCellData>();
            {
                var list = shopItems;
                var listCount = list.Count;
                for (var i = 0; i < listCount; i++)
                {
                    var item = list[i].itemid;
                    {
                        var tbStore = Table.GetStore(item);
                        if (null == tbStore)
                            continue;
                        var cell = new ShiZhuangStoreCellData { StoreIndex = item };
                        cell.ItemId = tbStore.ItemId;
                        cell.ExData = tbStore.DayCount;//策划指定读取此列获取扩展计数
                        cell.State = PlayerDataManager.Instance.GetExData(cell.ExData);
                        cell.Limit = PlayerDataManager.Instance.GetExData(cell.ExData);
                        cell.SortOrder = tbStore.Order;
                        itemList.Add(cell);
                        if (tbStore.GoodsType == 0)
                        {
                            if (cell.State == 1)//已经获得的不加入推荐列表
                            {
                                recommendItemList.Add(cell);//推荐列表
                            }
                        }
                    }
                }
                itemList.Sort((a, b) => { return a.SortOrder - b.SortOrder; });
                recommendItemList.Sort((a, b) => { return a.SortOrder - b.SortOrder; });
            }
            DataModel.StoreList = new ObservableCollection<ShiZhuangStoreCellData>(itemList);
            DataModel.RecommendList = new ObservableCollection<ShiZhuangStoreCellData>(recommendItemList);
        }

        private IEnumerator ShopPurchaseCoroutine(int index, int count = 1, int serviceType = -1)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreBuy(index, count, serviceType);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShiZhuangOperaEvent(0));
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(431));//购买成功
                        var _tbStore = Table.GetStore(index);
                        if (_tbStore == null)
                        {
                            yield break;
                        }
                        RefreshBagAndStoreInfo();
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var _e = new ShowUIHintBoard(430);
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

        private IEnumerator SetEquipModelStateCoroutine(Int32Array parts, bool state)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ChangeEquipState(parts, state);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode != (int)ErrorCodes.OK)
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                    else
                    {
                        if (parts.Items.Contains((int)eBagType.EquipShiZhuang))
                        {
                            DataModel.IsHideEquip = state;
                        }
                        if (parts.Items.Contains((int)eBagType.WingShiZhuang) || parts.Items.Contains((int)eBagType.Wing))
                        {
                            DataModel.IsHideWing = state;
                        }
                        if (parts.Items.Contains((int)eBagType.WeaponShiZhuang))
                        {
                            DataModel.IsHideWeapon = state;
                        }
                    }
                }
                else
                {
                    Logger.Error("SetEquipModelStateCoroutine............State..." + msg.State);
                }
            }
        }

        private IEnumerator UseShiZhuangCoroutine(int BagId, int BagItemIndex, int Part, bool isQuickUse = false)
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_HintCloseEvent());//关闭快捷使用界面
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.UseShiZhuang(BagId, BagItemIndex, Part);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode != (int)ErrorCodes.OK)
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                    else
                    {
                    }
                }
                else
                {
                    Logger.Error("UseShiZhuangCoroutine............State..." + msg.State);
                }
            }
        }

        private static void GetUseInfo(BagItemDataModel bagItem, ref int bagId, ref int part)
        {
            var itemId = bagItem.ItemId;
            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
                return;

            var equipId = tbItem.Exdata[0];
            var tbEquip = Table.GetEquipBase(equipId);
            if (tbEquip == null)
                return;

            if (GameUtils.IsShiZhuangCanEquip(tbEquip, (int)eBagType.WingShiZhuang))
            {
                part = (int)eBagType.WingShiZhuang;
                bagId = (int)eBagType.WingShiZhuangBag;
            }

            if (GameUtils.IsShiZhuangCanEquip(tbEquip, (int)eBagType.EquipShiZhuang))
            {
                part = (int)eBagType.EquipShiZhuang;
                bagId = (int)eBagType.EquipShiZhuangBag;
            }

            if (GameUtils.IsShiZhuangCanEquip(tbEquip, (int)eBagType.WeaponShiZhuang))
            {
                part = (int)eBagType.WeaponShiZhuang;
                bagId = (int)eBagType.WeaponShiZhuangBag;
            }
        }
        #endregion ----------------------------------------------逻辑函数----------------------------------------------
    }
}