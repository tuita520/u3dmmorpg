/********************************************************************************* 

                         Scorpion



  *FileName:TradeFrameCtrler

  *Version:1.0

  *Date:2017-07-012

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using GameUI;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class TradeFrameCtrler : IControllerBase
    {

        #region 枚举

        private enum eSortType
        {
            PriceUp = 0,
            PriceDown = 1,
            TimeUp = 2,
            TimeDown = 3,
            None = 4
        }

        private enum eEquipPage
        {
            SubPage = 0,
            DetailPage = 1
        }

        private enum eSellType
        {
            DiamondRes = 11,
            Other16 = 10
        }

        #endregion

        #region 成员变量

        private bool m_bVisible;
        private BuildingDataModel m_BuildingDataModel;
        private TradingDataModel m_DataModel;
        private int m_iExchangeItemIndex;
        private int m_iMySellIndex;
        private ulong m_ulSellerId;
        private float m_fTimeinterval;
        private Coroutine m_ButtonPress { get; set; }
        private float SinglePrice { get; set; }
        public FrameState State { get; set; }

        private int m_iTotalPage = 1; //总的页数
        private readonly int m_iMaxPageCount = 10; //一页最大显示条数
        private int m_iOptSelectIndex; // 单选按钮index 
        private eSortType m_SortType = eSortType.None; //排列方式

        private ExchangeEquipMenuDataModel m_SelectedMenuItem; //选择菜单index
        private ExchangeEquipItemDataModel m_SelectedEquipItem;
        private bool m_bNeedApplyHistory = true;
        private int m_iCanApplyHistoryCount = 2;


        private readonly Dictionary<int, ExchangeEquipMenuDataModel> m_dMotherList =
            new Dictionary<int, ExchangeEquipMenuDataModel>(); //主menu

        private readonly Dictionary<int, List<ExchangeEquipMenuDataModel>> m_dSonList =
            new Dictionary<int, List<ExchangeEquipMenuDataModel>>(); //子menu

        private readonly Dictionary<int, List<ExchangeEquipItemDataModel>> m_dEquipLists =
            new Dictionary<int, List<ExchangeEquipItemDataModel>>(); //装备拍卖行总的物品


        #endregion

        #region 构造函数

        public TradeFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_TradingFrameButton.EVENT_TYPE, OnButtonEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_TradingBagItemClick.EVENT_TYPE, OnBagPropClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_OnTradingItemSelled.EVENT_TYPE, OnPropSellEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_TradingCoolDownChanged.EVENT_TYPE, OnCDChangEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_OnTradingEquipOperation.EVENT_TYPE, OnEquipmentOperateEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_TradingEquipTabPage.EVENT_TYPE, OnRefleshTradeEquipmentTabPageEvent);
        }

        #endregion

        #region 固有函数

        # region base class

        public void CleanUp()
        {
            if (m_DataModel != null)
            {
                m_DataModel.SellSelectingItem.PropertyChanged -= OnRatioChange;
                m_DataModel.SelectedExchangeItem.PropertyChanged -= OnInterchangeRatioChange;
                m_DataModel.MyTradingItems.PropertyChanged -= MineStackChanged;
            }

            m_DataModel = new TradingDataModel();
            m_DataModel.SellSelectingItem.PropertyChanged += OnRatioChange;
            m_DataModel.SelectedExchangeItem.PropertyChanged += OnInterchangeRatioChange;

            m_DataModel.ExchangeItems.Clear();
            Table.ForeachTrade(table =>
            {
                var exchangeItem = new ExchangeItemDataModel();
                exchangeItem.ExchangeId = table.Id;
                exchangeItem.BagItem.ItemId = table.ItemID;
                exchangeItem.Price = table.Price;
                exchangeItem.SellCount = 1;
                exchangeItem.SellPrice = table.Price;
                exchangeItem.SellGroupRate = table.Count;
                exchangeItem.SellGroupCountMax = 0;
                exchangeItem.SellGroupCount = 0;
                m_DataModel.ExchangeItems.Add(exchangeItem);
                return true;
            });

            //装备交易行功能

            InitionMenusList();
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as TradingArguments;

            if (_args != null && _args.Tab != -1)
            {
                PlayerDataManager.Instance.PlayerDataModel.SkillData.TabSelectIndex = _args.Tab;
            }
            else
            {
                PlayerDataManager.Instance.PlayerDataModel.SkillData.TabSelectIndex = 0;
            }

            m_DataModel.SellInfoShow = false;
            m_DataModel.OtherSellInfoShow = false;
            m_DataModel.ExchangeSellInfoShow = false;
            m_DataModel.SellSelectingItem.Clone(new TradingItemDataModel());
            BuildingData _buildData = null;
            if (_args != null && _args.BuildingData != null)
            {
                _buildData = _args.BuildingData;
            }
            else
            {
                if (CityManager.Instance == null || CityManager.Instance.BuildingDataList == null)
                {
                    return;
                }
                {
                    // foreach(var buildingData in CityManager.Instance.BuildingDataList)
                    var _enumerator1 = (CityManager.Instance.BuildingDataList).GetEnumerator();
                    while (_enumerator1.MoveNext())
                    {
                        var _buildingData = _enumerator1.Current;
                        {
                            var _tbBuild = Table.GetBuilding(_buildingData.TypeId);
                            if (_tbBuild.Type == 9)
                            {
                                _buildData = _buildingData;
                                break;
                            }
                        }
                    }
                }
            }
            if (_buildData == null)
            {
                return;
            }
            var _tbBuilding = Table.GetBuilding(_buildData.TypeId);
            if (null == _tbBuilding)
            {
                return;
            }
            var _tbBuildingService = Table.GetBuildingService(_tbBuilding.ServiceId);
            var _count = _tbBuildingService.Param[0];
            var _itemCount = m_DataModel.MyTradingItems.Count;
            for (var i = _itemCount; i < _count; i++)
            {
                m_DataModel.MyTradingItems.Add(CreationPropFromIdOrDataModel(-1));
            }


            RefreshExchangeItems();
            m_DataModel.EquipData.ItemTypeOpt[0] = true;
            OnTradeTagClick(0);
            MenusChoose(0);
            SetSoldChooseType(0);
            AppliedSoldHistoryRecord();
        }

        private void RefreshExchangeItems()
        {
            var _c = m_DataModel.ExchangeItems.Count;
            for (var i = 0; i < _c; i++)
            {
                var _exchangeItem = m_DataModel.ExchangeItems[i];
                var _count = PlayerDataManager.Instance.GetItemTotalCount(_exchangeItem.BagItem.ItemId).Count;
                _exchangeItem.BagItem.Count = _count;
                _exchangeItem.SellGroupCountMax = _count / _exchangeItem.SellGroupRate;
                _exchangeItem.SellGroupCount = _count >= _exchangeItem.SellGroupRate ? 1 : 0;
                _exchangeItem.SellCount = _exchangeItem.SellGroupRate;
                _exchangeItem.SellPrice = _exchangeItem.SellCount * _exchangeItem.Price;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "TradingDataModel")
            {
                return m_DataModel;
            }
            return null;
        }

        public void Close()
        {
            m_bVisible = false;
        }

        public void Tick()
        {
            if (!m_bVisible)
            {
                return;
            }

            m_fTimeinterval += Time.deltaTime;
            if (m_fTimeinterval < 1)
            {
                return;
            }
            m_fTimeinterval -= 1;


            //每个物品吆喝时长
            var _count = m_DataModel.MyTradingItems.Count;
            for (var i = 0; i < _count; i++)
            {
                var _item = m_DataModel.MyTradingItems[i];
                if (_item.PeddleDateTime > Game.Instance.ServerTime)
                {
                    _item.PeddleTime = GameUtils.GetTimeDiffString(_item.PeddleDateTime);
                }
            }

            //叫卖cd
            m_DataModel.PeddlingTime =
                GameUtils.GetTimeDiffString(DateTime.FromBinary(m_DataModel.PeddlingCd) - Game.Instance.ServerTime);
            var _freeTime = Extension.FromServerBinary(m_DataModel.BroadCastNextFreeTime);
            m_DataModel.BroadCastTimeString = GameUtils.GetTimeDiffString(_freeTime - Game.Instance.ServerTime);

            if (_freeTime < Game.Instance.ServerTime && m_DataModel.BaseLayerShow)
            {
                RefreshAnotherGamePlayerCD();
                m_DataModel.BroadCastNextFreeTime = DateTime.MaxValue.ToBinary();
            }
        }

        public void OnChangeScene(int sceneId)
        {
        }

        private void Init(SelfStoreList list)
        {
            m_DataModel.MyTradingItems.Clear();
            var _c = list.Items.Count;
            for (var i = 0; i < _c; i++)
            {
                var _item = CreationPropFromNetMsg(list.Items[i]);

                m_DataModel.MyTradingItems.Add(_item);
            }
            RefreshMineStackNum();
            m_DataModel.PeddlingCd = Extension.FromServerBinary(list.NextFreeTime).ToBinary();
            RefreshMessage();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name.Equals("Init"))
            {
                CleanUp();
                Init(param[0] as SelfStoreList);
            }
            return null;
        }

        public void OnShow()
        {
            m_bVisible = true;
        }

        #endregion

        #endregion

        #region 逻辑函数

        private void SetupSellingType(TradingItemDataModel data)
        {
            if (data.TradeType == (int)eSellType.DiamondRes)
            {
                data.SellType = (int)eResourcesType.DiamondRes;
            }
            else
            {
                data.SellType = (int)eResourcesType.Other16;
            }
        }

        private TradingItemDataModel CreationPropFromNetMsg(SelfStoreOne data)
        {
            var _itemBase = data.ItemData;
            if (_itemBase == null)
            {
                return new TradingItemDataModel();
            }

            var _bagItem = new BagItemDataModel();
            _bagItem.ItemId = _itemBase.ItemId;
            _bagItem.Count = _itemBase.Count;
            _bagItem.Exdata.InstallData(_itemBase.Exdata);
            var _tradingItem = CreationPropFromIdOrDataModel(_bagItem);
            _tradingItem.SellCount = data.ItemData.Count;
            _tradingItem.SellPrice = data.NeedCount;
            _tradingItem.SellType = data.ItemType;
            _tradingItem.TradeType = data.ItemType;
            var _overTime = Extension.FromServerBinary(data.BroadcastOverTime);
            if (_overTime < Game.Instance.ServerTime)
            {
                _tradingItem.PeddleTime = string.Empty;
                _tradingItem.IsPeddling = false;
                _tradingItem.PeddleDateTime = DateTime.MinValue;
            }
            else
            {
                var _diffTime = _overTime.Subtract(Game.Instance.ServerTime);
                _tradingItem.PeddleTime = GameUtils.GetTimeDiffString(_diffTime);
                _tradingItem.IsPeddling = true;
                _tradingItem.PeddleDateTime = _overTime;
            }

            _tradingItem.TradingItemId = data.Id;
            _tradingItem.State = GetPropState(data.State);
            SetupSellingType(_tradingItem);
            return _tradingItem;
        }

        private TradingItemDataModel CreationPropFromNetMsg(OtherStoreOne data)
        {
            var _itemBase = data.ItemData;
            if (_itemBase == null)
            {
                return new TradingItemDataModel();
            }

            var _bagItem = new BagItemDataModel();
            _bagItem.ItemId = _itemBase.ItemId;
            _bagItem.Count = _itemBase.Count;
            _bagItem.Exdata.InstallData(_itemBase.Exdata);
            var _tradingItem = CreationPropFromIdOrDataModel(_bagItem);
            _tradingItem.SellCount = data.ItemData.Count;
            _tradingItem.SellPrice = data.NeedCount;
            _tradingItem.TradingItemId = data.Id;
            _tradingItem.SellType = data.NeedType;
            _tradingItem.TradeType = data.NeedType;
            _tradingItem.ManagerId = data.ManagerId;
            _tradingItem.State = GetPropState(data.State);
            SetupSellingType(_tradingItem);
            return _tradingItem;
        }

        private int GetPropState(int type)
        {
            switch (type)
            {
                case (int)StoreItemType.Buyed:
                {
                    return 2;
                }
                    break;
                case (int)StoreItemType.Normal:
                {
                    return 1;
                }
                    break;
                case (int)StoreItemType.Free:
                {
                    return 0;
                }
                    break;
            }
            return 0;
        }

        private TradingItemDataModel CreationPropFromIdOrDataModel(int id)
        {
            var _item = new BagItemDataModel();
            _item.ItemId = id;
            _item.Count = 0;
            return CreationPropFromIdOrDataModel(_item);
        }

        private TradingItemDataModel CreationPropFromIdOrDataModel(BagItemDataModel bagDataModel)
        {
            var _dataModel = new TradingItemDataModel();

            if (bagDataModel.ItemId == -1)
            {
                return _dataModel;
            }

            _dataModel.BagItem.Clone(bagDataModel);
            var _tbItem = Table.GetItemBase(bagDataModel.ItemId);
            _dataModel.NeedLevel = _tbItem.LevelLimit;
            _dataModel.SellCount = 1;
            _dataModel.MinSinglePrice = _tbItem.TradeMin;
            _dataModel.MaxSinglePrice = _tbItem.TradeMax;
            _dataModel.MaxSellCount = _tbItem.TradeMaxCount < _dataModel.BagItem.Count
                ? _tbItem.TradeMaxCount
                : _dataModel.BagItem.Count;
            if (_dataModel.MaxSellCount == 0)
            {
                _dataModel.SliderRate = 0;
            }
            else
            {
                _dataModel.SliderCanMove = _dataModel.MaxSellCount > 1;
                _dataModel.SliderRate = 1 / (float)_dataModel.MaxSellCount;
            }
            var _timeNow = Game.Instance.ServerTime;
            var _timeCd = DateTime.FromBinary(m_DataModel.PeddlingCd);
            _dataModel.IsPeddling = _timeNow > _timeCd;

            _dataModel.PeddleTime = string.Empty;
            _dataModel.State = 1;
            _dataModel.SellPrice = _dataModel.SellCount * _dataModel.MinSinglePrice;
            SinglePrice = _dataModel.MinSinglePrice;

            return _dataModel;
        }

        private void RefleshSellingSelectProp()
        {
            m_DataModel.SellSelectingItem.Clone(CreationPropFromIdOrDataModel(m_DataModel.SellSelectingItem.BagItem));
            //选择性刷新拍卖行数据
            RefleshSoldPropType(m_DataModel.SellSelectingItem);
        }

        private OtherPlayerTradingDataModel CreationOtherGamePlayerFromNetMsg(StoreBroadcastOne one)
        {
            var _player = new OtherPlayerTradingDataModel();
            _player.PlayerId = one.SellCharacterId;
            _player.PlayerName = one.SellCharacterName;
            _player.PeddingItem.SellPrice = one.NeedCount;

            var _itemBase = one.ItemData;
            if (_itemBase == null)
            {
                return _player;
            }
            var _bagItem = _player.PeddingItem.BagItem;
            _bagItem.ItemId = _itemBase.ItemId;
            _bagItem.Count = _itemBase.Count;
            _bagItem.Exdata.InstallData(_itemBase.Exdata);
            _player.PeddingItem.SellCount = _bagItem.Count;
            return _player;
        }
        #region 界面数据

        private void OpenExchangedMsg(int index)
        {
            var _exchange = m_DataModel.ExchangeItems[index];
            if (null == _exchange)
            {
                return;
            }

            m_DataModel.SelectedExchangeItem.Clone(_exchange);
            m_DataModel.ExchangeSellInfoShow = true;
            m_iExchangeItemIndex = index;
        }

        private void OpenPadNum()
        {
            var _item = m_DataModel.SellSelectingItem;
            var _minvalue = _item.MinSinglePrice * _item.SellCount;
            var _maxValue = _item.SellCount * _item.MaxSinglePrice;

            NumPadLogic.ShowNumberPad(_minvalue, _maxValue, value =>
            {
                if (value == -1)
                {
                    return;
                }

                var _selectItem = m_DataModel.SellSelectingItem;
                if (value >= _selectItem.MinSinglePrice && value <= _selectItem.MaxSinglePrice * _selectItem.SellCount)
                {
                    _selectItem.SellPrice = value;
                    SinglePrice = _selectItem.SellPrice / (float)_selectItem.SellCount;
                }
            });
        }

        private void SoldValueChanged(int value)
        {
            var _item = m_DataModel.SellSelectingItem;
            if (value >= _item.MinSinglePrice && value <= _item.MaxSinglePrice * _item.SellCount)
            {
                _item.SellPrice = value;
                SinglePrice = _item.SellPrice / (float)_item.SellCount;
            }
        }

        //刷新显示
        private void RefleshSoldPropType(TradingItemDataModel item)
        {
            if (m_DataModel.SellSelectingItem == null)
            {
                return;
            }

            if (m_DataModel.EquipTabPage != 0 && m_DataModel.SellTypeList[0])
            {
                //var item = mDataModel.SellSelectingItem;
                item.SellType = (int)eResourcesType.DiamondRes;
                var _sellPrice = int.Parse(Table.GetClientConfig(610).Value);
                item.SellPrice = _sellPrice;
                item.PeddleDateTime = Game.Instance.ServerTime.AddHours(Table.GetClientConfig(611).Value.ToInt());
                item.PeddleTime = GameUtils.GetTimeDiffString(item.PeddleDateTime);
                item.MinSinglePrice = _sellPrice;
                item.MaxSinglePrice = 999999;
            }
            else
            {
                item.SellType = (int)eResourcesType.Other16;
                var _tbItem = Table.GetItemBase(item.BagItem.ItemId);
                if (_tbItem == null)
                {
                    return;
                }
                if (BitFlag.GetLow(_tbItem.CanTrade, 1))
                {
                    item.PeddleDateTime = Game.Instance.ServerTime.AddHours(Table.GetClientConfig(611).Value.ToInt());
                    item.PeddleTime = GameUtils.GetTimeDiffString(item.PeddleDateTime);
                }
            }
        }

        private void OnRatioChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SliderRate")
            {
                var _dataModel = m_DataModel.SellSelectingItem;
                if (_dataModel.BagItem.ItemId == -1)
                {
                    _dataModel.SellCount = 0;
                    _dataModel.SellPrice = 0;
                    return;
                }
                if (m_DataModel.EquipTabPage == 0)
                {
                    _dataModel.SellCount = (int)(Mathf.Round(_dataModel.SliderRate * (_dataModel.MaxSellCount - 1)) + 1);
                    _dataModel.SellPrice = (int)(_dataModel.SellCount * SinglePrice);
                }
            }
        }

        private void OnInterchangeRatioChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SliderRate")
            {
                var _dataModel = m_DataModel.SelectedExchangeItem;
                _dataModel.SellGroupCount = (int)(Mathf.Round(_dataModel.SliderRate * (_dataModel.SellGroupCountMax - 1)) + 1);
                _dataModel.SellCount = _dataModel.SellGroupCount * _dataModel.SellGroupRate;
                _dataModel.SellPrice = (_dataModel.SellCount * _dataModel.Price);
            }
        }

        private void MineStackChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshMineStackNum();
        }

        private void RefreshMineStackNum()
        {
            var _current = 0;
            var _c = m_DataModel.MyTradingItems.Count;
            for (var i = 0; i < _c; i++)
            {
                var _item = m_DataModel.MyTradingItems[i];
                if (_item.State == 0)
                {
                    _current++;
                }
            }
            m_DataModel.MyStackCount = string.Format("{0}/{1}", _c - _current, _c);
            EventDispatcher.Instance.DispatchEvent(new UIEvent_CityTradingStack(_c - _current, _c));
        }

        private bool OnAdditionNum()
        {
            var _dataModel = m_DataModel.SellSelectingItem;
            if (SinglePrice < _dataModel.MaxSinglePrice)
            {
                _dataModel.SellPrice++;
                SinglePrice = _dataModel.SellPrice / (float)_dataModel.SellCount;
                return true;
            }
            return false;
        }

        private bool OnSubtractNum()
        {
            var _dataModel = m_DataModel.SellSelectingItem;
            if (SinglePrice > _dataModel.MinSinglePrice)
            {
                _dataModel.SellPrice--;
                SinglePrice = _dataModel.SellPrice / (float)_dataModel.SellCount;
                return true;
            }
            return false;
        }

        private IEnumerator OnAdditionPushCoroutine()
        {
            var _pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(_pressCd);
                if (OnAdditionNum() == false)
                {
                    NetManager.Instance.StopCoroutine(m_ButtonPress);
                    m_ButtonPress = null;
                    yield break;
                }
                if (_pressCd > 0.0001)
                {
                    _pressCd = _pressCd * 0.8f;
                }
            }
        }

        private IEnumerator OnSubtractPushCoroutine()
        {
            var pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(pressCd);
                if (OnSubtractNum() == false)
                {
                    NetManager.Instance.StopCoroutine(m_ButtonPress);
                    m_ButtonPress = null;
                    yield break;
                }
                if (pressCd > 0.0001)
                {
                    pressCd = pressCd * 0.8f;
                }
            }
        }

        private void RefreshMessage()
        {
            if (null != m_DataModel)
            {
                var _count = m_DataModel.MyTradingItems.Count;
                var _noticeState = false;
                for (var i = 0; i < _count; i++)
                {
                    var _item = m_DataModel.MyTradingItems[i];
                    if (_item.State == 2)
                    {
                        _noticeState = true;
                        break;
                    }
                }
                EventDispatcher.Instance.DispatchEvent(new UIEvent_CityRefreshTradingNotice(_noticeState));
                PlayerDataManager.Instance.NoticeData.MyTradingItemSelled = _noticeState;
            }
        }

        #endregion

        #region 网络数据

        private void OnButtonAbolish(int index)
        {
            if (index >= m_DataModel.MyTradingItems.Count)
            {
                Logger.Error("StoreOperationCancel index error!! index =" + index);
            }
            else
            {
                NetManager.Instance.StartCoroutine(ShopOperationAbolishCoroutine(index));
            }
        }

        private IEnumerator ShopOperationAbolishCoroutine(int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _item = m_DataModel.MyTradingItems[index];
                var _msg = NetManager.Instance.StoreOperationCancel(_item.TradingItemId);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        _item.Clone(new TradingItemDataModel());
                        RefreshMineStackNum();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void ShopOperationAddition()
        {
            var _tradItem = m_DataModel.SellSelectingItem;
            if (_tradItem.BagItem.ItemId == -1)
            {
                return;
            }
            var _type = 0;
            if (_tradItem.IsPeddling)
            {
                var _timeNow = Game.Instance.ServerTime;
                var _timeCd = DateTime.FromBinary(m_DataModel.PeddlingCd);
                if (_timeNow > _timeCd)
                {
                    _type = 1;
                    NetManager.Instance.StartCoroutine(OnShopOperationAdditionCoroutine(_type));
                }
                else
                {
                    var _cast = Table.GetClientConfig(302);
                    //叫卖冷却剩余{0},是否花费{1}钻石购买冷却!
                    var _message = string.Format(GameUtils.GetDictionaryText(270120),
                        GameUtils.GetTimeDiffString(_timeCd - _timeNow), _cast.Value);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _message, "",
                        () =>
                        {
                            var _diamond = PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes);
                            if (_diamond < int.Parse(_cast.Value))
                            {
                                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(300401));
                                return;
                            }

                            _type = 2;
                            NetManager.Instance.StartCoroutine(OnShopOperationAdditionCoroutine(_type));
                        },
                        () => { });
                }
            }
            else
            {
                NetManager.Instance.StartCoroutine(OnShopOperationAdditionCoroutine(_type));
            }
        }

        private IEnumerator OnShopOperationAdditionCoroutine(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _tradItem = m_DataModel.SellSelectingItem;
                var _item = _tradItem.BagItem;

                var _msg = NetManager.Instance.StoreOperationAdd(type, _item.BagId, _item.Index, _tradItem.SellCount,
                    _tradItem.SellPrice, m_iMySellIndex);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlatformHelper.Event("city", "TradingAdd");
                        //上架成功
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270121));
                        m_DataModel.SellSelectingItem.TradingItemId = _msg.Response;
                        m_DataModel.MyTradingItems[m_iMySellIndex].Clone(m_DataModel.SellSelectingItem);
                        m_DataModel.SellSelectingItem.Clone(new TradingItemDataModel());
                        m_DataModel.SellInfoShow = false;
                        if (type != 0)
                        {
                            var _timecd = Game.Instance.ServerTime + m_DataModel.PeddingCdMax;
                            m_DataModel.PeddlingCd = _timecd.ToBinary();

                            m_DataModel.MyTradingItems[m_iMySellIndex].PeddleTime =
                                GameUtils.GetTimeDiffString(m_DataModel.PeddlingLastTimeMax);
                            m_DataModel.MyTradingItems[m_iMySellIndex].PeddleDateTime =
                                Game.Instance.ServerTime + m_DataModel.PeddlingLastTimeMax;
                        }
                        RefreshMineStackNum();
                        if (type == 1 || type == 2)
                        {
                            //飞经验
                            var _exp = int.Parse(Table.GetClientConfig(303).Value);
                            EventDispatcher.Instance.DispatchEvent(new UIEvent_TradingFlyAnim(_exp));
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void RefreshAnotherGamePlayerMoney()
        {
            var _cast = Table.GetClientConfig(305);
            //是否花费{0}钻石刷新?
            var _message = string.Format(GameUtils.GetDictionaryText(270122), _cast.Value);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _message, "",
                () =>
                {
                    var _diamond = PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes);
                    if (_diamond < int.Parse(_cast.Value))
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(300401));
                        return;
                    }
                    NetManager.Instance.StartCoroutine(RefreshOtherPlayerMessageCoroutine(1));
                },
                () => { });
        }

        private void RefreshAnotherGamePlayerCD()
        {
            var _freeTIme = Extension.FromServerBinary(m_DataModel.BroadCastNextFreeTime);
            var _nowTime = Game.Instance.ServerTime;

            if (_freeTIme < _nowTime)
            {
                NetManager.Instance.StartCoroutine(RefreshOtherPlayerMessageCoroutine(0));
            }
        }

        private IEnumerator RefreshOtherPlayerMessageCoroutine(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreOperationBroadcast(type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_DataModel.OtherPlayers.Clear();
                        var _list = _msg.Response.Items;
                        var _otherPlayer = new OtherPlayerTradingDataModel();
                        var _count = _list.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _onePlayer = CreationOtherGamePlayerFromNetMsg(_list[i]);
                            m_DataModel.OtherPlayers.Add(_onePlayer);
                        }
                        m_DataModel.BroadCastNextFreeTime = _msg.Response.CacheOverTime;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void AcquireOtherPlayerMsg(int index)
        {
            var _player = m_DataModel.OtherPlayers[index];
            m_DataModel.SelectionOtherPlayer.PlayerId = _player.PlayerId;
            m_DataModel.SelectionOtherPlayer.PlayerName = _player.PlayerName;
            m_DataModel.SelectionOtherPlayer.PeddingItem.Clone(_player.PeddingItem);

            NetManager.Instance.StartCoroutine(ShopOperationLookCoroutine(_player.PlayerId));
        }

        private IEnumerator ShopOperationLookCoroutine(ulong playerId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreOperationLook(playerId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    m_DataModel.OtherPlayerItems.Clear();
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _list = _msg.Response;
                        m_ulSellerId = _list.SellCharacterId;
                        var _count = _list.Items.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _one = _list.Items[i];
                            var _item = CreationPropFromNetMsg(_one);
                            m_DataModel.OtherPlayerItems.Add(_item);
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }


        private void BuyOtherGamePlayerProp(int index)
        {
            var _res = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources;
            var _item = m_DataModel.OtherPlayerItems[index];
            if (_item.TradeType == (int)eSellType.DiamondRes || _item.TradeType == (int)eSellType.Other16)
            {
                if (_item.SellType == (int)eResourcesType.DiamondRes && _res.Diamond < _item.SellPrice)
                {
                    //您的货币不足，购买失败
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(429));
                    return;
                }
                if (_item.SellType == (int)eResourcesType.Other16 && _res.MuCurrency < _item.SellPrice)
                {
                    //您的货币不足，购买失败
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(429));
                    return;
                }
                NetManager.Instance.StartCoroutine(BuyPropAuctionCoroutine(m_DataModel.SelectionOtherPlayer.PlayerId,
                    _item.TradingItemId, _item.ManagerId, () =>
                    {
                        PlatformHelper.Event("city", "TradingBuy");
                        _item.State = GetPropState((int)StoreItemType.Buyed);
                        m_bNeedApplyHistory = true;
                        m_iCanApplyHistoryCount = 2;
                    }));
            }
            else
            {
                if (_res.MuCurrency < _item.SellPrice)
                {
                    //您的货币不足，购买失败
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(429));
                    return;
                }
                NetManager.Instance.StartCoroutine(ShopOperationPurchaseCoroutine(index));
            }
        }

        private IEnumerator ShopOperationPurchaseCoroutine(int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _item = m_DataModel.OtherPlayerItems[index];
                var _msg = NetManager.Instance.StoreOperationBuy(m_DataModel.SelectionOtherPlayer.PlayerId, _item.TradingItemId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlatformHelper.Event("city", "TradingBuy");
                        var _res = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources;
                        var _muGold = _res.MuCurrency - _item.SellPrice;
                        PlayerDataManager.Instance.SetRes(16, _muGold);
                        _item.State = GetPropState((int)StoreItemType.Buyed);
                    }
                    else
                    {
                        //您购买的道具已被其它玩家买走
                        UIManager.Instance.ShowMessage(MessageBoxType.Ok, GameUtils.GetDictionaryText(270123), "",
                            () =>
                            {
                                NetManager.Instance.StartCoroutine(
                                    ShopOperationLookCoroutine(m_DataModel.SelectionOtherPlayer.PlayerId));
                            });
                    }
                }
            }
        }

        private void ShopOperationReap(int index)
        {
            NetManager.Instance.StartCoroutine(ShopOperationReapCoroutine(index));
        }

        private IEnumerator ShopOperationReapCoroutine(int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _item = m_DataModel.MyTradingItems[index];
                var _msg = NetManager.Instance.StoreOperationHarvest(_item.TradingItemId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //var res = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources;
                        //res.MuCurrency = res.MuCurrency + item.SellPrice;
                        var _emptyItem = CreationPropFromIdOrDataModel(-1);
                        _item.Clone(_emptyItem);
                        RefreshMessage();
                        RefreshMineStackNum();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void OperationInterchange()
        {
            NetManager.Instance.StartCoroutine(ShopOperationInterchangeCoroutine());
        }

        private IEnumerator ShopOperationInterchangeCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _item = m_DataModel.SelectedExchangeItem;
                var _msg = NetManager.Instance.SSStoreOperationExchange(_item.ExchangeId, _item.SellCount);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _res = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources;
                        var _mugold = _res.MuCurrency + _item.SellPrice;
                        PlayerDataManager.Instance.SetRes(16, _mugold);
                        _item.BagItem.Count -= _item.SellCount;
                        var _count = _item.BagItem.Count;
                        _item.SliderRate = 0;
                        _item.SellGroupCountMax = _count / _item.SellGroupRate;
                        _item.SellGroupCount = _count >= _item.SellGroupRate ? 1 : 0;
                        _item.SellCount = _item.SellGroupRate;
                        _item.SellPrice = (_item.SellCount * _item.Price);
                        m_DataModel.ExchangeSellInfoShow = false;
                        m_DataModel.ExchangeItems[m_iExchangeItemIndex].Clone(_item);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        #endregion

        #region //装备交易行功能

        private void InitionMenusList()
        {
            m_dMotherList.Clear();
            m_dSonList.Clear();
            var _list = new List<ExchangeEquipMenuDataModel>();
            var _index1 = 0;
            Table.ForeachAuctionType1(table =>
            {
                var _item = new ExchangeEquipMenuDataModel();
                _item.Type = 1;
                _item.Id = table.Id;
                _item.TypeName = table.Type;
                _item.Selected = false;
                _item.Index = _index1;
                _index1++;
                //list.Add(item);
                m_dMotherList.Add(table.Id, _item);
                _list.Add(_item);
                var _sonList = new List<ExchangeEquipMenuDataModel>();
                var _count = table.SonList.Count;
                for (var i = 0; i < _count; i++)
                {
                    var _index2 = 0;
                    var _sonItem = new ExchangeEquipMenuDataModel();
                    _sonItem.Type = 0;
                    var _tbAuction2 = Table.GetAuctionType2(table.SonList[i]);
                    if (_tbAuction2 == null)
                    {
                        return true;
                    }
                    _sonItem.Id = _tbAuction2.Id;
                    _sonItem.TypeName = _tbAuction2.Name;
                    _sonItem.Selected = false;
                    _sonItem.Index = _index2;
                    _index2++;
                    _sonList.Add(_sonItem);
                    //
                }
                m_dSonList.Add(table.Id, _sonList);
                return true;
            });
            m_DataModel.EquipData.MenuDatas = new ObservableCollection<ExchangeEquipMenuDataModel>(_list);
        }

        private void MenusChoose(int index)
        {
            var _menu = m_DataModel.EquipData.MenuDatas;

            if (_menu[index].Type == 1)
            {
                var _list = new List<ExchangeEquipMenuDataModel>();
                if (_menu[index].Selected)
                {
                    _menu[index].Selected = false;
                    for (var i = 0; i < m_dMotherList.Count; i++)
                    {
                        _list.Add(m_dMotherList[i]);
                    }
                    m_DataModel.EquipData.MenuDatas = new ObservableCollection<ExchangeEquipMenuDataModel>(_list);
                }
                else
                {
                    if (m_SelectedMenuItem != null)
                    {
                        m_SelectedMenuItem.Selected = false;
                    }

                    var _sonIndex = _menu[index].Index;
                    for (var i = 0; i < m_dMotherList.Count; i++)
                    {
                        m_dMotherList[i].Selected = false;
                        _list.Add(m_dMotherList[i]);
                        if (i != _sonIndex)
                        {
                            continue;
                        }
                        m_dMotherList[i].Selected = true;
                        var _sonCount = m_dSonList[_sonIndex].Count;
                        for (var j = 0; j < _sonCount; j++)
                        {
                            if (j == 0)
                            {
                                m_dSonList[i][j].Selected = true;
                                m_SelectedMenuItem = m_dSonList[i][j];
                            }
                            else
                            {
                                m_dSonList[i][j].Selected = false;
                            }
                            _list.Add(m_dSonList[i][j]);
                        }
                    }
                    m_DataModel.EquipData.MenuDatas = new ObservableCollection<ExchangeEquipMenuDataModel>(_list);

                    var _tbAuction = Table.GetAuctionType1(_menu[index].Id);
                    RenewalSon(_tbAuction.SonList[0]);
                }
            }
            else
            {
                if (m_SelectedMenuItem != null)
                {
                    m_SelectedMenuItem.Selected = false;
                }
                _menu[index].Selected = true;
                m_SelectedMenuItem = _menu[index];
                RenewalSon(_menu[index].Id);
            }
        }

        private void SetEquipedMsg(List<AuctionItemOne> items)
        {
            m_dEquipLists.Clear();
            var _list = new List<ExchangeEquipItemDataModel>();
            var _list2 = new List<ExchangeEquipItemDataModel>();
            foreach (var item in items)
            {
                var _dataOne = new ExchangeEquipItemDataModel();
                _dataOne.MangerId = item.ManagerId;
                _dataOne.Guid = item.ItemGuid;
                _dataOne.Item.ItemId = item.ItemData.ItemId;
                _dataOne.Item.Exdata.InstallData(item.ItemData.Exdata);
                _dataOne.SellCharacterId = item.SellCharacterId;
                if (item.NeedType == (int)eSellType.DiamondRes)
                {
                    _dataOne.Type = (int)eResourcesType.DiamondRes;
                }
                else if (item.NeedType == (int)eSellType.Other16)
                {
                    _dataOne.Type = (int)eResourcesType.Other16;
                }
                _dataOne.Price = item.NeedCount;
                _dataOne.SellName = item.SellCharacterName;
                _dataOne.Time = Extension.FromServerBinary(item.OverTime);
                _dataOne.TimeStr = GameUtils.GetTimeDiffString(_dataOne.Time);
                if (item.NeedType == (int)eSellType.DiamondRes)
                {
                    _list.Add(_dataOne);
                }
                else if (item.NeedType == (int)eSellType.Other16)
                {
                    _list2.Add(_dataOne);
                }
            }
            m_dEquipLists.Add((int)eSellType.DiamondRes, _list);
            m_dEquipLists.Add((int)eSellType.Other16, _list2);
        }

        private void RenewalSon(int auction1Id)
        {
            var _classList = new List<ExchangeEquipClassItemDataModel>();
            var _tbAution2 = Table.GetAuctionType2(auction1Id);
            var _count = _tbAution2.SonList.Count;
            for (var i = 0; i < _count; i++)
            {
                var _tbAution3 = Table.GetAuctionType3(_tbAution2.SonList[i]);
                var _item = new ExchangeEquipClassItemDataModel();
                _item.Id = _tbAution3.Id;
                _classList.Add(_item);
            }
            m_DataModel.EquipData.ClassList = new ObservableCollection<ExchangeEquipClassItemDataModel>(_classList);

            m_DataModel.EquipData.ShowWitchPage = (int)eEquipPage.SubPage;
        }

        private void BuyPropAuctionAbolish()
        {
            m_DataModel.EquipData.IsShowBuyPage = false;
        }

        private void EquipPropChoosed(int index)
        {
            m_SelectedEquipItem = m_DataModel.EquipData.Items[index];
            m_DataModel.EquipData.BuyItem = m_DataModel.EquipData.BuyItem.Clone(m_SelectedEquipItem);
            m_DataModel.EquipData.IsShowBuyPage = true;
        }

        private void SetSoldChooseType(int index)
        {
            for (var i = 0; i < m_DataModel.SellTypeList.Count; i++)
            {
                m_DataModel.SellTypeList[i] = false;
            }
            if (index == 0)
            {
                m_DataModel.SellTypeList[0] = true;
            }
            else
            {
                m_DataModel.SellTypeList[1] = true;
            }
            RefleshSellingSelectProp();
        }

        private void OnClickedTime()
        {
            if (m_SortType == eSortType.TimeUp)
            {
                m_SortType = eSortType.TimeDown;
            }
            else
            {
                m_SortType = eSortType.TimeUp;
            }
            ReplacementPage();
        }

        private void OnClickValue()
        {
            if (m_SortType == eSortType.PriceUp)
            {
                m_SortType = eSortType.PriceDown;
            }
            else
            {
                m_SortType = eSortType.PriceUp;
            }
            ReplacementPage();
        }


        private void ReplacementPage()
        {
            var mEquipData = m_DataModel.EquipData;
            mEquipData.PageIndex = 1;
            RenewalEquipedList();
        }

        private void ButtonPageBelow()
        {
            var _mEquipData = m_DataModel.EquipData;
            if (_mEquipData.PageIndex == 1)
            {
                return;
            }
            _mEquipData.PageIndex--;
            RenewalEquipedList();
        }

        private void ButtonPageUpon()
        {
            var _mEquipData = m_DataModel.EquipData;
            if (_mEquipData.PageIndex >= m_iTotalPage)
            {
                return;
            }
            _mEquipData.PageIndex++;
            RenewalEquipedList();
        }

        private void SetPageButtonStatus()
        {
            var _mEquipData = m_DataModel.EquipData;
            if (m_iTotalPage <= 1 || _mEquipData.PageIndex <= 1)
            {
                _mEquipData.IsShowPageFront = false;
            }
            else
            {
                _mEquipData.IsShowPageFront = true;
            }
            if (_mEquipData.PageIndex >= m_iTotalPage)
            {
                _mEquipData.IsShowPageBack = false;
            }
            else
            {
                _mEquipData.IsShowPageBack = true;
            }
        }

        private void RenewalEquipedList()
        {
            var _equipList = m_dEquipLists[m_iOptSelectIndex];
            var _mEquipData = m_DataModel.EquipData;
            if (_equipList == null || _equipList.Count == 0)
            {
                _mEquipData.Items = new ObservableCollection<ExchangeEquipItemDataModel>();
                return;
            }
            _mEquipData.PageIndex = Math.Max(_mEquipData.PageIndex, 1);
            m_iTotalPage = (_equipList.Count - 1) / m_iMaxPageCount + 1;

            var _startIndex = (_mEquipData.PageIndex - 1) * m_iMaxPageCount;
            if (_startIndex >= _equipList.Count)
            {
                Logger.Error("Trading RefleshEquipList error");
            }
            var _items = new List<ExchangeEquipItemDataModel>();
            var _addCount = 0;
            for (var i = _startIndex; i < _equipList.Count && _addCount < m_iMaxPageCount; i++)
            {
                _items.Add(_equipList[i]);
                _addCount++;
            }
            SetPageButtonStatus();
            SetLabeledStatus();
            ResettingSort(m_SortType, _items);
            _mEquipData.Items = new ObservableCollection<ExchangeEquipItemDataModel>(_items);
        }


        private void ResettingSort(eSortType type, List<ExchangeEquipItemDataModel> Lists)
        {
            switch (type)
            {
                case eSortType.PriceUp:
                {
                    Lists.Sort((a, b) =>
                    {
                        if (a.Price > b.Price)
                        {
                            return 1;
                        }
                        if (a.Price == b.Price)
                        {
                            if (a.Time < b.Time)
                            {
                                return -1;
                            }
                            return 1;
                        }
                        return -1;
                    });
                }
                    break;

                case eSortType.PriceDown:
                {
                    Lists.Sort((a, b) =>
                    {
                        if (a.Price < b.Price)
                        {
                            return 1;
                        }
                        if (a.Price == b.Price)
                        {
                            if (a.Time < b.Time)
                            {
                                return -1;
                            }
                            return 1;
                        }
                        return -1;
                    });
                }
                    break;
                case eSortType.TimeUp:
                {
                    Lists.Sort((a, b) =>
                    {
                        if (a.Time < b.Time)
                        {
                            return -1;
                        }
                        if (a.Time == b.Time)
                        {
                            if (a.Price < b.Price)
                            {
                                return -1;
                            }
                            return 1;
                        }
                        return 1;
                    });
                }
                    break;
                case eSortType.TimeDown:
                {
                    Lists.Sort((a, b) =>
                    {
                        if (a.Time < b.Time)
                        {
                            return 1;
                        }
                        if (a.Time == b.Time)
                        {
                            if (a.Price < b.Price)
                            {
                                return -1;
                            }
                            return 1;
                        }
                        return -1;
                    });
                }
                    break;
            }
        }

        private void SetLabeledStatus()
        {
            var _equipData = m_DataModel.EquipData;
            //equipData.PriceLabel = "价格";
            //equipData.TimeLabel = "时间";
            switch (m_SortType)
            {
                case eSortType.PriceUp:
                    _equipData.ArrowDirection = 0;
                    //equipData.PriceLabel += "↓";
                    break;
                case eSortType.PriceDown:
                    _equipData.ArrowDirection = 1;
                    //equipData.PriceLabel += "↑";
                    break;
                case eSortType.TimeUp:
                    _equipData.ArrowDirection = 2;
                    //equipData.TimeLabel += "↑";
                    break;
                case eSortType.TimeDown:
                    _equipData.ArrowDirection = 3;
                    //equipData.TimeLabel += "↓";
                    break;
                case eSortType.None:
                    break;
            }
        }

        private void SetChooseType(int index)
        {
            for (var i = 0; i < m_DataModel.EquipData.ItemTypeOpt.Count; i++)
            {
                m_DataModel.EquipData.ItemTypeOpt[i] = false;
            }
            if (index == 0)
            {
                m_iOptSelectIndex = (int)eSellType.DiamondRes;
                m_DataModel.EquipData.ItemTypeOpt[0] = true;
            }
            else
            {
                m_iOptSelectIndex = (int)eSellType.Other16;
                m_DataModel.EquipData.ItemTypeOpt[1] = true;
            }
        }

        private void SetChooseIndex()
        {
            var _index = 0;
            var _opt = m_DataModel.EquipData.ItemTypeOpt;
            for (var i = 0; i < _opt.Count; i++)
            {
                if (_opt[i])
                {
                    _index = i;
                    break;
                }
            }
            if (_index == 0)
            {
                m_iOptSelectIndex = (int)eSellType.DiamondRes;
            }
            else
            {
                m_iOptSelectIndex = (int)eSellType.Other16;
            }
        }

        private void OnTradeTagClick(int index)
        {
            m_DataModel.EquipTabPage = index;
            if (index == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new PackTradingSellPage(index, 1));
            }
            else if (index == 1)
            {
                EventDispatcher.Instance.DispatchEvent(new PackTradingSellPage(index, 0));
            }
        }




        private void OnPropAuction()
        {
            NetManager.Instance.StartCoroutine(OnPropAuctionCoroutine(() => { PlatformHelper.Event("city", "TradingAdd"); }));
        }

        private IEnumerator OnPropAuctionCoroutine(Action act)
        {
            using (new BlockingLayerHelper(0))
            {
                var _tradItem = m_DataModel.SellSelectingItem;
                var _item = _tradItem.BagItem;
                if (_item.ItemId == -1)
                {
                    yield break;
                }
                var _type = 10;
                if (m_DataModel.SellTypeList[0])
                {
                    _type = (int)eSellType.DiamondRes;
                }
                else if (m_DataModel.SellTypeList[1])
                {
                    _type = (int)eSellType.DiamondRes;
                }
                var _msg = NetManager.Instance.OnItemAuction(_type, _item.BagId, _item.Index, _tradItem.SellCount,
                    _tradItem.SellPrice, m_iMySellIndex);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_DataModel.SellSelectingItem.TradingItemId = _msg.Response;
                        //上架成功
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270121));
                        m_DataModel.MyTradingItems[m_iMySellIndex].Clone(m_DataModel.SellSelectingItem);
                        m_DataModel.MyTradingItems[m_iMySellIndex].TradingItemId = _msg.Response;
                        m_DataModel.SellSelectingItem.Clone(new TradingItemDataModel());
                        m_DataModel.SellInfoShow = false;

                        //if (type != 0)
                        //{
                        //    var timecd = Game.Instance.ServerTime + mDataModel.PeddingCdMax;
                        //    mDataModel.PeddlingCd = timecd.ToBinary();

                        //    mDataModel.MyTradingItems[mMySellIndex].PeddleTime =
                        //        GameUtils.GetTimeDiffString(mDataModel.PeddlingLastTimeMax);
                        //    mDataModel.MyTradingItems[mMySellIndex].PeddleDateTime =
                        //        Game.Instance.ServerTime + mDataModel.PeddlingLastTimeMax;
                        //}
                        RefreshMineStackNum();
                        act();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void BuyPropAuction()
        {
            var _playerId = PlayerDataManager.Instance.Guid;
            if (_playerId == m_SelectedEquipItem.SellCharacterId)
            {
                GameUtils.ShowHintTip(300881);
                return;
            }
            NetManager.Instance.StartCoroutine(BuyPropAuctionCoroutine(m_SelectedEquipItem.SellCharacterId,
                m_SelectedEquipItem.Guid, m_SelectedEquipItem.MangerId, () =>
                {
                    foreach (var item in m_dEquipLists[m_iOptSelectIndex])
                    {
                        if (item.Guid == m_SelectedEquipItem.Guid)
                        {
                            m_dEquipLists[m_iOptSelectIndex].Remove(item);
                            break;
                        }
                    }
                    m_DataModel.EquipData.Items.Remove(m_SelectedEquipItem);
                    m_DataModel.EquipData.IsShowBuyPage = false;
                }));
        }

        private IEnumerator BuyPropAuctionCoroutine(ulong sellCharacterId,
            long guid,
            long mangerId,
            Action act)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BuyItemAuction(sellCharacterId, guid, mangerId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        act();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void SonChoose(int index)
        {
            var _id = m_DataModel.EquipData.ClassList[index].Id;
            NetManager.Instance.StartCoroutine(CSChoosePropAuctionCoroution(_id, () =>
            {
                m_DataModel.EquipData.ShowWitchPage = (int)eEquipPage.DetailPage;
                SetChooseIndex();
                m_SortType = eSortType.PriceUp;
                ReplacementPage();
            }));
        }


        private IEnumerator CSChoosePropAuctionCoroution(int type, Action act)
        {
            using (new BlockingLayerHelper(0))
            {
                var _instance = PlayerDataManager.Instance;
                var _msg = NetManager.Instance.CSSelectItemAuction(_instance.ServerId, type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (_msg.Response.Items.Count == 0)
                        {
                            GameUtils.ShowHintTip(300880);
                            yield break;
                        }
                        SetEquipedMsg(_msg.Response.Items);

                        act();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void AppliedSoldHistoryRecord()
        {
            if (m_bNeedApplyHistory && m_iCanApplyHistoryCount > 0)
            {
                NetManager.Instance.StartCoroutine(AppliedSoldHistoryRecordCoroutine(() =>
                {
                    m_iCanApplyHistoryCount = 0;
                    m_bNeedApplyHistory = false;
                }));
            }
        }

        private IEnumerator AppliedSoldHistoryRecordCoroutine(Action act)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplySellHistory(0);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SetHistoryRecordInfo(_msg.Response.items);
                        act();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                    m_iCanApplyHistoryCount--;
                    AppliedSoldHistoryRecord();
                }
            }
        }

        private void SetHistoryRecordInfo(List<SellHistoryOne> items)
        {
            if (items == null || items.Count == 0)
            {
                m_DataModel.EquipData.HistoryEmptyTip = true;
                return;
            }
            m_DataModel.EquipData.HistoryEmptyTip = false;
            m_DataModel.HistoryList.Clear();
            var _myGuid = PlayerDataManager.Instance.Guid;
            var _list = new List<ExchangeEquipHistoryItemDataModel>();
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var _value = items[i];
                var _item = new ExchangeEquipHistoryItemDataModel();
                _item.Item.ItemId = _value.ItemData.ItemId;
                _item.Item.Exdata.InstallData(_value.ItemData.Exdata);
                _item.Item.Count = _value.ItemData.Count;
                _item.Name = _value.buyCharacterName;
                //if (value.buyCharacterId == myGuid)
                //{
                _item.Type = _value.type;
                // }
                //else
                //{
                //    item.Type = 1;
                //}
                _item.SaleType = _value.resType;
                _item.SaleCount = _value.resCount;
                _item.Time = Extension.FromServerBinary(_value.sellTime).ToString("yyyy/MM/dd HH:mm:ss");
                _list.Add(_item);
            }
            m_DataModel.HistoryList = new ObservableCollection<ExchangeEquipHistoryItemDataModel>(_list);
        }

        #endregion
        #endregion

        #region 事件函数

        private void OnCDChangEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_TradingCoolDownChanged;
            m_DataModel.PeddingCdMax = _e.CD;
            m_DataModel.PeddlingTime = _e.CD.TotalHours.ToString("f0");

            m_DataModel.PeddlingLastTimeMax = _e.LastTime;
            m_DataModel.PeddlingLastTimeString = GameUtils.GetTimeDiffString(m_DataModel.PeddlingLastTimeMax);
        }

        private void OnButtonEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_TradingFrameButton;

            switch (_e.ButtonIndex)
            {
                //添加按钮
                case 0:
                {
                    m_iMySellIndex = _e.Data;
                    m_DataModel.SellInfoShow = true;

                    //引导潜规则
                    if (!PlayerDataManager.Instance.GetFlag(534))
                    {
                        var _list = new Int32Array();
                        _list.Items.Add(534);

                        var _list1 = new Int32Array();
                        _list1.Items.Add(533);
                        PlayerDataManager.Instance.SetFlagNet(_list, _list1);
                    }
                }
                    break;
                //关闭卖出界面按钮
                case 1:
                {
                    m_DataModel.SellInfoShow = false;
                }
                    break;
                //add
                case 2:
                {
                    OnAdditionNum();
                }
                    break;
                //sub
                case 3:
                {
                    OnSubtractNum();
                }
                    break;
                //addPress
                case 4:
                {
                    m_ButtonPress = NetManager.Instance.StartCoroutine(OnAdditionPushCoroutine());
                }
                    break;
                //addRelease
                case 5:
                {
                    if (m_ButtonPress != null)
                    {
                        NetManager.Instance.StopCoroutine(m_ButtonPress);
                        m_ButtonPress = null;
                    }
                }
                    break;
                //subPress
                case 6:
                {
                    m_ButtonPress = NetManager.Instance.StartCoroutine(OnSubtractPushCoroutine());
                }
                    break;
                //subRelease
                case 7:
                {
                    if (null != m_ButtonPress)
                    {
                        NetManager.Instance.StopCoroutine(m_ButtonPress);
                        m_ButtonPress = null;
                    }
                }
                    break;
                //上架新商品
                case 8:
                {
                    ShopOperationAddition();
                }
                    break;
                //下架商品
                case 9:
                {
                    OnButtonAbolish(_e.Data);
                }
                    break;
                //刷新别人的摊位
                case 10:
                {
                    RefreshAnotherGamePlayerMoney();
                }
                    break;
                //获取别人摊位详细信息
                case 11:
                {
                    m_DataModel.OtherSellInfoShow = true;
                    AcquireOtherPlayerMsg(_e.Data);
                }
                    break;
                //关闭别人摊位详细信息
                case 12:
                {
                    m_DataModel.OtherSellInfoShow = false;
                }
                    break;
                //购买别人的商品
                case 13:
                {
                    BuyOtherGamePlayerProp(_e.Data);
                }
                    break;
                //收获卖出物品
                case 14:
                {
                    ShopOperationReap(_e.Data);
                }
                    break;
                //去逛街toggle
                case 15:
                {
                    RefreshAnotherGamePlayerCD();
                }
                    break;
                //打开数字键盘
                case 16:
                {
                    OpenPadNum();
                }
                    break;
                //兑换物品详情打开
                case 17:
                {
                    OpenExchangedMsg(_e.Data);
                }
                    break;
                //兑换物品
                case 18:
                {
                    OperationInterchange();
                }
                    break;
                //关闭兑换界面
                case 19:
                {
                    m_DataModel.ExchangeSellInfoShow = false;
                }
                    break;
                //兑换界面切换刷新物品数量
                case 20:
                {
                    RefreshExchangeItems();
                }
                    break;
            }
        }

        private void OnBagPropClickEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_TradingBagItemClick;
            var _bagItem = _e.BagItem;
            m_DataModel.SellSelectingItem.Clone(CreationPropFromIdOrDataModel(_bagItem));
            //选择性刷新拍卖行数据
            RefleshSoldPropType(m_DataModel.SellSelectingItem);
        }
        private void OnPropSellEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_OnTradingItemSelled;
            m_bNeedApplyHistory = true; //打开历史重新请求
            m_iCanApplyHistoryCount = 2;
            if (m_DataModel != null)
            {
                var _count = m_DataModel.MyTradingItems.Count;
                var _bFind = false;
                for (var i = 0; i < _count; i++)
                {
                    var _item = m_DataModel.MyTradingItems[i];
                    if (_item.TradingItemId == _e.itemId)
                    {
                        _bFind = true;
                        _item.State = GetPropState((int)StoreItemType.Buyed);
                        var _name = Table.GetItemBase(_item.BagItem.ItemId).Name;
                        EventDispatcher.Instance.DispatchEvent(
                            new ShowUIHintBoard(_name + GameUtils.GetDictionaryText(270119)));
                        break;
                    }
                }

                if (!_bFind)
                {
                    Logger.Error("Cant Find Trading Item id ={0}", _e.itemId);
                }
                RefreshMessage();
            }
        }
        private void OnEquipmentOperateEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_OnTradingEquipOperation;
            switch (_e.Type)
            {
                case 0:
                {
                    ButtonPageUpon();
                }
                    break;
                case 1:
                {
                    ButtonPageBelow();
                }
                    break;
                case 2:
                {
                    AppliedSoldHistoryRecord();
                }
                    break;
                case 3:
                {
                    OnClickValue();
                }
                    break;
                case 4:
                {
                    OnClickedTime();
                }
                    break;
                case 5:
                {
                    MenusChoose(_e.Value);
                }
                    break;
                case 6:
                {
                    SonChoose(_e.Value);
                }
                    break;
                case 7:
                {
                    EquipPropChoosed(_e.Value);
                }
                    break;
                case 8:
                {
                    SetChooseType(_e.Value);
                    if (m_DataModel.EquipData.ShowWitchPage == 1)
                    {
                        m_SortType = eSortType.PriceUp;
                        ReplacementPage();
                    }
                }
                    break;
                case 9:
                {
                    SetSoldChooseType(_e.Value);
                }
                    break;
                case 10:
                {
                    BuyPropAuction();
                }
                    break;
                case 11:
                {
                    OnTradeTagClick(_e.Value);
                }
                    break;
                case 12:
                {
                    OnPropAuction();
                }
                    break;
                case 13:
                {
                    BuyPropAuctionAbolish();
                }
                    break;
            }
        }
        private void OnRefleshTradeEquipmentTabPageEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_TradingEquipTabPage;
            m_DataModel.EquipTabPage = _e.Page;
        }

        #endregion
    }
}