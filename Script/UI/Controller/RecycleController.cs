/********************************************************************************* 

                         Scorpion



  *FileName:RebirthFrameCtrler

  *Version:1.0

  *Date:2017-06-19

  *Description:

**********************************************************************************/
#region using

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
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class RebirthFrameCtrler : IControllerBase
    {

        #region 构造函数
        public RebirthFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_RecycleBtn.EVENT_TYPE, OnBtnRebirthEvent); //回收、出售按钮
            EventDispatcher.Instance.AddEventListener(UIEvent_RecycleGetOK.EVENT_TYPE, OnBtnGainOKEvent); //回收
            EventDispatcher.Instance.AddEventListener(UIEvent_RecycleGetCancel.EVENT_TYPE, OnBtnGainAbolishEvent); //关闭回收获得页面
            EventDispatcher.Instance.AddEventListener(UIEvent_RecycleItemSelect.EVENT_TYPE, OnProvisionChooseEvent); //背包物品点击事件
            EventDispatcher.Instance.AddEventListener(UIEvent_RecycleArrange.EVENT_TYPE, OnBtnPermutationEvent); //整理
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnRefurbishEquipBagProvisionStatusEvent); //刷新背包状态
        }
        #endregion

        #region 成员变量
        private IControllerBase BackPack;
        private int RecycleType; // 0 出售，1回收
        private RecycleDataModal RecycleData { get; set; }
        #endregion

        #region 事件
        //整理背包
        private void OnBtnPermutationEvent(IEvent itevent)
        {
            InitializeRebirthBags();
            RefurbishBags();
            var _e = itevent as UIEvent_RecycleArrange;
            var _ee = new PackArrangeEventUi(_e.TabPage);
            EventDispatcher.Instance.DispatchEvent(_ee);
        }

        //关闭回收获得页面
        private void OnBtnGainAbolishEvent(IEvent ivent)
        {
            RecycleData.UIGetShow = 0;
        }

        //回收或出售确认并发包
        private void OnBtnGainOKEvent(IEvent ivent)
        {
            var _TempEquipList = new Int32Array();
            if (RecycleData.RecycleItem.Count <= 0)
            {
                return;
            }
            var _RecycleDataRecycleItemCount2 = RecycleData.RecycleItem.Count;
            for (var i = 0; i < _RecycleDataRecycleItemCount2; i++)
            {
                if (RecycleData.RecycleItem[i].ItemId != -1)
                {
                    _TempEquipList.Items.Add(RecycleData.RecycleItem[i].Index);
                }
            }
            //回收物品列表是否大于0
            NetManager.Instance.StartCoroutine(RebirthCoroutine(_TempEquipList));
        }

        //背包物品点击事件
        private void OnProvisionChooseEvent(IEvent ivent)
        {
            var _e = ivent as UIEvent_RecycleItemSelect;
            var _varItem = _e.Item;
            //varItem = PlayerDataManager.Instance.GetItem(e.BagID, e.Index);

            if (_e.type == 0)
            {
                if (_varItem.IsChoose)
                {
                    _varItem.IsChoose = false;
                    EnhanceRebirthProvision();
                    return;
                }
                if (!EquipmentQualityEstimate(_varItem))
                {
                    _varItem.IsChoose = true;
                    EnhanceRebirthProvision();
                    return;
                }
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, GameUtils.GetDictionaryText(230200), "", () =>
                {
                    _varItem.IsChoose = true;
                    EnhanceRebirthProvision();
                },
                    () => { _varItem.IsChoose = false; });
            }
            else
            {
                _varItem.IsChoose = false;
                EnhanceRebirthProvision();
            }
        }

        //刷新背包物品状态
        private void OnRefurbishEquipBagProvisionStatusEvent(IEvent ievent)
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

        //回收
        private void OnBtnRebirthEvent(IEvent ivent)
        {
            var _e = ivent as UIEvent_RecycleBtn;
            RecycleType = _e.Type;
            if (RecycleType == 0)
            {
                RecycleData.SellOrRecycleStr = GameUtils.GetDictionaryText(270238);
            }
            else
            {
                RecycleData.SellOrRecycleStr = GameUtils.GetDictionaryText(270239);
            }
            var _sellAllMoney = 0;
            var _MoneyCount = 0;
            var _MagicCount = 0;
            RecycleData.RecycleGetItem.Clear();
            var _getList = new List<BagItemDataModel>();
            if (RecycleData.RecycleItem[0].ItemId == -1)
            {
                return;
            }
            RecycleData.UIGetShow = 1;
            {
                // foreach(var item in RecycleData.RecycleItem)
                var _enumerator2 = (RecycleData.RecycleItem).GetEnumerator();
                while (_enumerator2.MoveNext())
                {
                    var _item = _enumerator2.Current;
                    {
                        if (_item.ItemId == -1)
                        {
                            continue;
                        }
                        var _tbItemBase = Table.GetItemBase(_item.ItemId);
                        if (_tbItemBase.CallBackType == -1)
                        {
                            continue;
                        }
                        if (_tbItemBase.CallBackType == 2) //金币
                        {
                            if (_tbItemBase.Sell > 0)
                            {
                                _MoneyCount += _tbItemBase.Sell;
                            }
                        }
                        else if (_tbItemBase.CallBackType == 10) //魔尘
                        {
                            if (_tbItemBase.CallBackPrice > 0)
                            {
                                _MagicCount += _tbItemBase.CallBackPrice;
                            }
                        }

                        if (_tbItemBase.Sell > 0)
                        {
                            _sellAllMoney += _tbItemBase.Sell;
                        }
                        //装备强化道具返回
                        if (_item.Exdata.Enchance > 0 && _item.Exdata.Enchance < 15)
                        {
                            var _tbBlessing = Table.GetEquipBlessing(_item.Exdata.Enchance);
                            var _tbBlessingCallBackItemLength0 = _tbBlessing.CallBackItem.Length;
                            for (var i = 0; i < _tbBlessingCallBackItemLength0; i++)
                            {
                                if (_tbBlessing.CallBackItem[i] == -1)
                                {
                                    continue;
                                }
                                var _isExist = false;
                                {
                                    // foreach(var getItem in RecycleData.RecycleGetItem)
                                    var _enumerator7 = (_getList).GetEnumerator();
                                    while (_enumerator7.MoveNext())
                                    {
                                        var _getItem = _enumerator7.Current;
                                        {
                                            if (_getItem.ItemId == _tbBlessing.CallBackItem[i])
                                            {
                                                _getItem.Count += _tbBlessing.CallBackCount[i];
                                                _isExist = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (!_isExist)
                                {
                                    var _bagitem = new BagItemDataModel();
                                    _bagitem.ItemId = _tbBlessing.CallBackItem[i];
                                    _bagitem.Count = _tbBlessing.CallBackCount[i];
                                    _getList.Add(_bagitem);
                                    //RecycleData.RecycleGetItem.Add(bagitem);
                                }
                            }
                        }


                        var _tbEquipBase = Table.GetEquipBase(_tbItemBase.Exdata[0]);
                        //装备追加道具返回
                        if (_item.Exdata.Append > _tbEquipBase.AddAttrUpMaxValue)
                        {
                            var _tbEquipAdd = Table.GetEquipAdditional1(_tbEquipBase.AddIndexID);
                            var _tbSkillUpdate = Table.GetSkillUpgrading(_tbEquipAdd.AddPropArea);

                            var _countIndex = 0;
                            var _tbSkillUpdateValuesCount1 = _tbSkillUpdate.Values.Count;
                            for (var i = 0; i < _tbSkillUpdateValuesCount1; i++)
                            {
                                if (_tbSkillUpdate.Values[i] > _item.Exdata.Append)
                                {
                                    _countIndex = i;
                                    break;
                                }
                            }
                            var _tbSkillUpdate2 = Table.GetSkillUpgrading(_tbEquipAdd.CallBackCount);

                            var _isExist = false;
                            {
                                // foreach(var getItem in RecycleData.RecycleGetItem)
                                var _enumerator8 = (_getList).GetEnumerator();
                                while (_enumerator8.MoveNext())
                                {
                                    var _getItem = _enumerator8.Current;
                                    {
                                        if (_getItem.ItemId == _tbEquipAdd.CallBackItem)
                                        {
                                            _getItem.Count += _tbSkillUpdate2.Values[_countIndex];
                                            _isExist = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!_isExist)
                            {
                                var _bagitem = new BagItemDataModel();
                                _bagitem.ItemId = _tbEquipAdd.CallBackItem;
                                _bagitem.Count = _tbSkillUpdate2.Values[_countIndex];
                                _getList.Add(_bagitem);
                                //RecycleData.RecycleGetItem.Add(bagitem);
                            }
                        }
                    }
                }
            }

            if (RecycleType == 0) //全部出售
            {
                if (_sellAllMoney > 0)
                {
                    var _varitem = new BagItemDataModel();
                    _varitem.ItemId = 2;
                    _varitem.Count = _sellAllMoney;
                    _getList.Insert(0, _varitem);
                    //RecycleData.RecycleGetItem.Insert(0, varitem);
                }
            }
            else //全部回收
            {
                if (_MoneyCount > 0)
                {
                    var _varitem = new BagItemDataModel();
                    _varitem.ItemId = 2;
                    _varitem.Count = _MoneyCount;
                    _getList.Insert(0, _varitem);
                    //RecycleData.RecycleGetItem.Insert(0, varitem);
                    //RecycleData.UIMoneyShow = 1;
                }
                //else
                //{
                //    RecycleData.UIMoneyShow = 0;
                //}
                if (_MagicCount > 0)
                {
                    var _varitem = new BagItemDataModel();
                    _varitem.ItemId = 10;
                    _varitem.Count = _MagicCount;
                    _getList.Insert(0, _varitem);
                    //RecycleData.RecycleGetItem.Insert(0, varitem);
                }
            }
            RecycleData.RecycleGetItem = new ObservableCollection<BagItemDataModel>(_getList);
            //else
            //{
            //    RecycleData.UIMagicShow = 0;
            //}
            ////回收物品居中显示
            //int gridcount = 0;
            //for (int i = 0; i < RecycleData.RecycleGetItem.Count; i++)
            //{
            //    if (RecycleData.RecycleGetItem[i].ItemId != -1)
            //    {
            //        gridcount++;
            //    }
            //}
            //gridcount = (gridcount+1)/2 -1;
            //gridcount = gridcount < 0 ? 0 : gridcount;
            var _ee = new UIEvent_RecycleSetGridCenter();
            //ee.index = gridcount;
            EventDispatcher.Instance.DispatchEvent(_ee);
        }
        #endregion


        #region 逻辑函数
        //添加物品到回收列表中
        private void EnhanceRebirthProvision()
        {
            RecycleData.RecycleItem.Clear();
            var _list = new List<BagItemDataModel>();

            var _baseData = PlayerDataManager.Instance.GetBag((int)eBagType.Equip);
            var _count = 0;
            {
                // foreach(var item in baseData.Items)
                var _enumerator4 = (_baseData.Items).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _item = _enumerator4.Current;
                    {
                        if (_item.IsChoose)
                        {
                            _list.Add(_item);
                            _count++;
                        }
                    }
                }
            }
            //默认回收背包有格子
            var _tbBaseType = Table.GetBagBase((int)eBagType.Equip);
            for (var i = _count; i < _tbBaseType.MaxCapacity; i++)
            {
                var _bgItem = new BagItemDataModel();
                _bgItem.ItemId = -1;
                _list.Add(_bgItem);
            }

            RecycleData.RecycleItem = new ObservableCollection<BagItemDataModel>(_list);
        }

        //物品品质判断，>=紫色提示或追加值大于最大成长值
        private bool EquipmentQualityEstimate(BagItemDataModel BaseItem)
        {
            var _tbBaseItem = Table.GetItemBase(BaseItem.ItemId);
            if (_tbBaseItem.Quality > 3)
            {
                return true;
            }
            if (BaseItem.Exdata.Enchance > 6)
            {
                return true;
            }
            //        var tbEquipItem = Table.GetEquipBase(BaseItem.ItemId);
            //         if (BaseItem.Exdata.Append > tbEquipItem.AddAttrUpMaxValue)
            //         {
            //             return true;
            //         }
            return false;
        }

        //初始化背包
        private void InitializeRebirthBags()
        {
            //默认回收背包有格子
            var _tbBaseType = Table.GetBagBase((int)eBagType.Equip);
            if (RecycleData.RecycleItem.Count != _tbBaseType.MaxCapacity)
            {
                RecycleData.RecycleItem.Clear();
                var _list = new List<BagItemDataModel>();

                for (var i = 0; i < _tbBaseType.MaxCapacity; i++)
                {
                    var _bagItem = new BagItemDataModel();
                    _bagItem.ItemId = -1;
                    _list.Add(_bagItem);
                    //RecycleData.RecycleItem.Add(bagItem);
                }
                RecycleData.RecycleItem = new ObservableCollection<BagItemDataModel>(_list);
            }
        }

        //checkbox变化调用
        private void OnSwitchExchange(object sender, PropertyChangedEventArgs e)
        {
            var _index = 0;
            if (!int.TryParse(e.PropertyName, out _index))
            {
                return;
            }
            SetColorChoose(_index);
        }

        //回收包
        private IEnumerator RebirthCoroutine(Int32Array TempEquipList)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.RecoveryEquip(RecycleType, TempEquipList);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //var _list = new Int32Array();
                        //_list.Items.Add(2800);
                        //PlayerDataManager.Instance.SetFlagNet(_list);

                        if (RecycleType == 0)
                        {
                            RecycleData.IsSellEffect = true;
                            RecycleData.IsSellEffect = false;

                            for (int i = 0; i < TempEquipList.Items.Count; i++)
                            {
                                var _item = PlayerDataManager.Instance.GetItem((int)eBagType.Equip, TempEquipList.Items[i]);
                                if (_item != null)
                                {
                                    PlatformHelper.UMEvent("EquipSell", _item.ItemId.ToString());
                                }
                            }
                        }
                        else
                        {
                            RecycleData.IsRecycleEffect = true;
                            RecycleData.IsRecycleEffect = false;


                            for (int i = 0; i < TempEquipList.Items.Count; i++)
                            {
                                var _item = PlayerDataManager.Instance.GetItem((int)eBagType.Equip, TempEquipList.Items[i]);

                                if (_item != null)
                                {
                                    PlatformHelper.UMEvent("EquipRecycle", _item.ItemId.ToString());
                                }
                            }
                        }


                        var _baseData = PlayerDataManager.Instance.GetBag((int)eBagType.Equip);
                        var _isFull = _baseData.Size == _baseData.Capacity;
                        for (var i = 0; i < TempEquipList.Items.Count; i++)
                        {
                            var _index = TempEquipList.Items[i];
                            if (_index < _baseData.Items.Count)
                            {
                                _baseData.Items[_index].ItemId = -1;
                                _baseData.Items[_index].IsChoose = false;
                            }
                        }
                        InitializeRebirthBags();
                        RefurbishBags();
                        _baseData.Size -= TempEquipList.Items.Count;
                        if (_isFull && TempEquipList.Items.Count > 0)
                        {
                            var _e = new EquipBagNotFullChange();
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        RecycleData.UIGetShow = 0;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        private void RefurbishBags()
        {
            var _baseData = PlayerDataManager.Instance.GetBag((int)eBagType.Equip);
            RecycleData.BagItems.Clear();
            var _list = new List<BagItemDataModel>();
            for (var i = 0; i < _baseData.Items.Count; i++)
            {
                var _item = _baseData.Items[i];
                if (_item.ItemId != -1)
                {
                    var _tbItem = Table.GetItemBase(_item.ItemId);
                    if (_tbItem != null)
                    {
                        if (_tbItem.CallBackType <= 0 || _tbItem.CallBackPrice <= 0)
                        { // 不可回收
                            continue;
                        }
                    }
                    _list.Add(_item);
                }
            }
            var _tbBaseType = Table.GetBagBase((int)eBagType.Equip);
            for (var i = _list.Count; i < _tbBaseType.MaxCapacity; i++)
            {
                var _bagItem = new BagItemDataModel();
                _bagItem.ItemId = -1;
                _list.Add(_bagItem);
                //RecycleData.RecycleItem.Add(bagItem);
            }
            RecycleData.BagItems = new ObservableCollection<BagItemDataModel>(_list);
        }

        private void SetColorChoose(int index)
        {
            var _baseData = PlayerDataManager.Instance.GetBag((int)eBagType.Equip);

            var _count = 0;
            {
                // foreach(var item in baseData.Items)
                var _enumerator1 = (_baseData.Items).GetEnumerator();
                while (_enumerator1.MoveNext())
                {
                    var _item = _enumerator1.Current;
                    {
                        if (_item.ItemId == -1)
                        {
                            _count++;
                            continue;
                        }
                        var _tbItemBase = Table.GetItemBase(_item.ItemId);
                        if (index == _tbItemBase.Quality)
                        {
                            if (RecycleData.ColorSelect[index])
                            {
                                _item.IsChoose = true;
                            }
                            else
                            {
                                _item.IsChoose = false;
                            }
                        }
                        _count++;
                    }
                }
            }
            EnhanceRebirthProvision();
        }
        #endregion
   

        #region 固有函数
        public void CleanUp()
        {
            if (RecycleData != null)
            {
                RecycleData.ColorSelect.PropertyChanged -= OnSwitchExchange;
            }


            BackPack = UIManager.Instance.GetController(UIConfig.BackPackUI);
            RecycleData = new RecycleDataModal();


            RecycleData.ColorSelect.PropertyChanged += OnSwitchExchange;
        }

        public void RefreshData(UIInitArguments data)
        {
            var _arg2 = new BackPackArguments();
            _arg2.Tab = 0;
            //BackPack.RefreshData(arg2);
            BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Recycle });
            var _RecycleDataColorSelectCount3 = RecycleData.ColorSelect.Count;
            for (var i = 0; i < _RecycleDataColorSelectCount3; i++)
            {
                RecycleData.ColorSelect[i] = false;
            }
            InitializeRebirthBags();
            RecycleData.ColorSelect[0] = true;
            SetColorChoose(0);
            RecycleData.ColorSelect[1] = true;
            SetColorChoose(1);
            RefurbishBags();
            var _args = data as RecycleArguments;
            if (_args == null)
            {
                return;
            }

            if (_args.ItemDataModel != null)
            {
                var _item = _args.ItemDataModel;
                var _varItem = PlayerDataManager.Instance.GetItem(_item.BagId, _item.Index);
                _varItem.IsChoose = true;
                EnhanceRebirthProvision();
            }
            RecycleData.UIGetShow = 0;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return RecycleData;
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

        public void OnShow()
        {
            BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Recycle });
        }

        public FrameState State { get; set; }
        #endregion
    
    }
}