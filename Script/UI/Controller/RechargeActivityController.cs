/********************************************************************************* 

                         Scorpion



  *FileName:PayMonmentFrameCtrler

  *Version:1.0

  *Date:2017-06-15

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
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class PayMonmentFrameCtrler : IControllerBase
    {

        #region 构造函数
        public PayMonmentFrameCtrler()
        {
            platfrom = "android";
#if UNITY_ANDROID
        platfrom = "android";
#elif UNITY_IOS
        platfrom = "ios";
#endif
            EventDispatcher.Instance.AddEventListener(RechageActivityRewardOperation.EVENT_TYPE, PaymentWorkEvent);
            EventDispatcher.Instance.AddEventListener(RechageActivityMenuItemClick.EVENT_TYPE, CarteHitEvent);
            EventDispatcher.Instance.AddEventListener(RechageActivityOperation.EVENT_TYPE, OnWorkEvent);
            EventDispatcher.Instance.AddEventListener(RechageActivityInvestmentOperation.EVENT_TYPE, IntrojectionWorkEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, InitializeExDatumEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, RenovateExdatumEvent);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, RenovateSignEvent);
            EventDispatcher.Instance.AddEventListener(RechageActivityInitTables.EVENT_TYPE, InitializeFormsEvent);
            CleanUp();
        }
        #endregion

        #region 成员变量
        private List<int> tableExdata = new List<int>();
        private List<int> tableFlagTrue = new List<int>();
        private List<int> tableFlagFalse = new List<int>();

        private readonly string platfrom;
        private int _menuSelectIndex;

        private readonly Dictionary<int, RechargeActivityMenuItemDataModel> _mExtraIdToMenuItem =
            new Dictionary<int, RechargeActivityMenuItemDataModel>();

        private readonly Dictionary<int, List<int>> _mInvestmentDic = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, List<int>> _mReChargeDic = new Dictionary<int, List<int>>();
        private RechargeActivityMenuItemDataModel _mSelectedMenuItem;
        private RechargeActivityDataModel DataModel;
        private string MainLabelStr = string.Empty;
        private DateTime NearTime;
        private readonly int RandomSecondesToApplyTables = 100;
        private object RefleshTrigger;
        private bool TableUpdated;
        private object Trigger;
        #endregion

        #region 事件
        private void InitializeExDatumEvent(IEvent ievent)
        {
            InitializeForms();
        }

        private void InitializeFormsEvent(IEvent ievent)
        {
            var _count = MyRandom.Random(RandomSecondesToApplyTables);
            NetManager.Instance.StartCoroutine(AwaitToSecondsToDemandFormsCoroutine(_count));
        }

        private void IntrojectionWorkEvent(IEvent ievent)
        {
            var _e = ievent as RechageActivityInvestmentOperation;
            var _item = DataModel.Investment.MainLists[_e.Index];
            if (_item.GetState != 1)
            {
                return;
            }
            GetPayment((int)eReChargeRewardType.Investment, _item.Id, _e.Index);
        }

        private void CarteHitEvent(IEvent ievent)
        {
            var _e = ievent as RechageActivityMenuItemClick;
            DataModel.CurrSelectType = _e.Index;
            CarteProvisionHit(_e.Index);
        }

        private void OnWorkEvent(IEvent ievent)
        {
            var _e = ievent as RechageActivityOperation;
            switch (_e.Type)
            {
                case 0:
                {
                    var _tbNotice = GainPayActivityAnnouncement(DataModel.Notice.Id);
                    GameUtils.GotoUiTab(_tbNotice.GotoUiId, _tbNotice.GotoUiTab);
                }
                    break;
                case 1:
                {
                    PurchaseIntrojection();
                }
                    break;
                case 2:
                {
                    var _tbNotice = GainPayActivityAnnouncement(DataModel.FirstRecharge.Id);
                    GameUtils.GotoUiTab(_tbNotice.GotoUiId, _tbNotice.GotoUiTab);
                }
                    break;
            }
        }

        private void PaymentWorkEvent(IEvent ievent)
        {
            var _e = ievent as RechageActivityRewardOperation;
            var _item = DataModel.Recharge.MainLists[_e.Index];
            if (_item.GetState != 1)
            {
                return;
            }
            GetPayment((int)eReChargeRewardType.Recharge, _item.Id, _e.Index);
        }

        private void RenovateSignEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            if (_e == null)
            {
                return;
            }
            CostAchieved(_e.Index);

            if (tableFlagTrue.Contains(_e.Index) || tableFlagFalse.Contains(_e.Index))
            {
                RefurbishCarteDatum();
            }
        }

        private void RenovateExdatumEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            if (_e.Key == (int)eExdataDefine.e69)
            {
                var _str = string.Empty;
                if (_e.Value < 1)
                {
                    //首充
                    _str = GameUtils.GetDictionaryText(100000587);
                }
                else
                {
                    _str = GameUtils.GetDictionaryText(100001000);
                }
                if (_str != MainLabelStr)
                {
                    EventDispatcher.Instance.DispatchEvent(new FirstRechargeTextSet_Event(_str));
                }
                MainLabelStr = _str;
                return;
            }
            RefurbishAnnouncementByExdatum(_e.Key);
            if (tableExdata.Contains(_e.Key))
            {
                RefurbishCarteDatum();
                CarteProvisionHit(_menuSelectIndex);
            }

        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new RechargeActivityDataModel();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void RefreshData(UIInitArguments data)
        {
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnShow()
        {
            RefurbishCarteDatum();
            if (DataModel.MenuLists.Count > 0)
            {
                CarteProvisionHit(_menuSelectIndex);
            }
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

        #region 逻辑函数
        private IEnumerator DemandPayFormsCoroutine()
        {
            var _msg = NetManager.Instance.ApplyRechargeTables(0);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply && _msg.ErrorCode == (int)ErrorCodes.OK)
            {
                Game.Instance.RechargeActivityData = _msg.Response;
                _RechargeActiveTable = _msg.Response.RechargeActiveTable;
                _RechargeActiveNoticeTable = _msg.Response.RechargeActiveNoticeTable;
                _RechargeActiveCumulativeRewardTable = _msg.Response.RechargeActiveCumulativeRewardTable;
                _RechargeActiveInvestmentRewardTable = _msg.Response.RechargeActiveInvestmentRewardTable;
                _RechargeActiveCumulativeTable = _msg.Response.RechargeActiveCumulativeTable;
                _RechargeActiveInvestmentTable = _msg.Response.RechargeActiveInvestmentTable;
                InitializeDatum();
                RefurbishCarteDatum();
            }
        }

        private void PurchaseIntrojection()
        {
            var _tbActivity = GainPayActivity(_mSelectedMenuItem.ActivityId);
            var _tbRecharge = GainPayActivityAccumulated(_tbActivity.SonType);
            var _conditionDic = PlayerDataManager.Instance.CheckCondition(_tbRecharge.ConditionId);
            if (_conditionDic != 0)
            {
                var _e = new ShowUIHintBoard(_conditionDic);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            var _id = DataModel.Investment.Id;
            //道具不足
            //         if (PlayerDataManager.Instance.GetRes(tbRecharge.NeedItemId) < tbRecharge.NeedItemCount)
            //         {
            //             var str = string.Format(GameUtils.GetDictionaryText(270011), Table.GetItemBase(tbRecharge.NeedItemId).Name);
            //             var ee = new ShowUIHintBoard(str);
            //             EventDispatcher.Instance.DispatchEvent(ee);
            //             return;
            //         }
            NetManager.Instance.StartCoroutine(PurchaseIntrojectionCoroutine(_id));
        }

        private IEnumerator PurchaseIntrojectionCoroutine(int id)
        {
            using (new BlockingLayerHelper(0))
            {
                var _tbRecharge = GainPayActivityAccumulated(id);
                if (_tbRecharge == null || _tbRecharge.ChargeID == null)
                {
                    yield break;
                }

                string[] str = _tbRecharge.ChargeID.Split(',');
                if (str.Length < 2)
                {
                    yield break;
                }

                var _tableid = 1;
                if (platfrom == "android")
                {
                    var _aa = str[1];
                    _tableid = int.Parse(_aa);
                }
                else if (platfrom == "ios")
                {
                    var _aa = str[0];
                    _tableid = int.Parse(_aa);
                }
                else
                {
                    yield break;
                }

                var _ee = new OnTouZiBtnClick_Event(_tableid);
                EventDispatcher.Instance.DispatchEvent(_ee);
                //             var msg = NetManager.Instance.Investment(id);
                //             yield return msg.SendAndWaitUntilDone();
                //             if (msg.State == MessageState.Reply)
                //             {
                //                 if (msg.ErrorCode == (int) ErrorCodes.OK)
                //                 {
                //                     DataModel.Investment.CanBuy = 0;
                //                     var tbRecharge = GetRechargeActiveCumulative(id);
                //                     var extraId = tbRecharge.ExtraId;
                //                     var instance = PlayerDataManager.Instance;
                //                     if (tbRecharge.ResetCount != -1)
                //                     {
                //                         var tbExdata = Table.GetExdata(extraId);
                //                         if (tbExdata != null)
                //                         {
                //                             var randomValue = MyRandom.Random(tbExdata.RefreshValue[0], tbExdata.RefreshValue[1]);
                //                             instance.SetExData(extraId, randomValue);
                //                         }
                //                     }
                //                     instance.SetFlag(tbRecharge.FlagTrueId);
                //                     var flagList = tbRecharge.FlagFalseId;
                //                     for (var i = 0; i < flagList.Count; i++)
                //                     {
                //                         instance.SetFlag(flagList[i], false);
                //                     }
                //                     MenuItemClick(_menuSelectIndex);
                //                     RefleshNoticeByExdata(extraId);
                //                     var e = new ShowUIHintBoard(431);
                //                     EventDispatcher.Instance.DispatchEvent(e);
                //                 }
                //                 else
                //                 {
                //                     UIManager.Instance.ShowNetError(msg.ErrorCode);
                //                 }
                //             }
            }
        }

        private void GetPayment(int type, int id, int index)
        {
            NetManager.Instance.StartCoroutine(GetPaymentCoroutine(type, id, index));
        }

        private IEnumerator GetPaymentCoroutine(int type, int id, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.GainReward(type, id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _mType = (eReChargeRewardType)type;
                        var _instance = PlayerDataManager.Instance;
                        var _flagId = 0;
                        var _exdataId = 0;
                        switch (_mType)
                        {
                            case eReChargeRewardType.Recharge:
                            {
                                var _item = DataModel.Recharge.MainLists[index];
                                var _tbRecharge = GainPayActivityIntrojectionPayment(id);
                                _flagId = _tbRecharge.Flag;
                                _instance.SetFlag(_flagId, true);
                                _exdataId = GainPayActivityIntrojection(_tbRecharge.Type).ExtraId;
                                _item.GetState = 2;
                            }
                                break;
                            case eReChargeRewardType.Investment:
                            {
                                var _item = DataModel.Investment.MainLists[index];
                                var _tbRecharge = GainPayActivityAccumulatedPayment(id);
                                _flagId = _tbRecharge.Flag;
                                _instance.SetFlag(_flagId, true);
                                _exdataId = GainPayActivityAccumulated(_tbRecharge.Type).ExtraId;
                                _item.GetState = 2;
                            }
                                break;
                        }
                        RefurbishAnnouncementByExdatum(_exdataId);
                        RefurbishCarteDatum();
                        CarteProvisionHit(_menuSelectIndex);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void InitializeStudy()
        {
            _mExtraIdToMenuItem.Clear();
            var _menuList = DataModel.MenuLists;
            var _instance = PlayerDataManager.Instance;
            foreach (var item in _menuList)
            {
                var _tbRecharge = GainPayActivity(item.ActivityId);
                if (_tbRecharge == null)
                {
                    continue;
                }
                var _type = _tbRecharge.Type;
                switch (_type)
                {
                    case (int)eReChargeRewardType.Investment:
                    {
                        var _tbCumulative = GainPayActivityAccumulated(_tbRecharge.SonType);
                        var _rewardList = _mInvestmentDic[_tbCumulative.Id];
                        var _count = _rewardList.Count;
                        var _flag = false;
                        for (var i = 0; i < _count; i++)
                        {
                            var _tbReward = GainPayActivityAccumulatedPayment(_rewardList[i]);
                            if (_instance.GetFlag(_tbReward.Flag))
                            {
                                continue;
                            }
                            if (_instance.CheckCondition(_tbReward.ConditionId) == 0)
                            {
                                _flag = true;
                                break;
                            }
                        }
                        item.NoticeFlag = _flag;
                        if (_tbCumulative.ExtraId >= 0 && _tbCumulative.ActivityId >= 0)
                        {
                            _mExtraIdToMenuItem[_tbCumulative.ExtraId] = item;
                        }
                    }
                        break;
                    case (int)eReChargeRewardType.Recharge:
                    {
                        var _tbInvestment = GainPayActivityIntrojection(_tbRecharge.SonType);
                        var _rewardList = _mReChargeDic[_tbInvestment.Id];
                        var _count = _rewardList.Count;
                        var _flag = false;
                        for (var i = 0; i < _count; i++)
                        {
                            var _tbReward = GainPayActivityIntrojectionPayment(_rewardList[i]);
                            if (_instance.GetFlag(_tbReward.Flag))
                            {
                                continue;
                            }
                            if (_instance.CheckCondition(_tbReward.ConditionId) == 0)
                            {
                                _flag = true;
                                break;
                            }
                        }
                        item.NoticeFlag = _flag;
                        if (_tbInvestment.ExtraId >= 0 && _tbInvestment.ActivityId >= 0)
                        {
                            _mExtraIdToMenuItem[_tbInvestment.ExtraId] = item;
                        }
                    }
                        break;
                    case (int)eReChargeRewardType.FirstRecharge:
                    {
                        item.NoticeFlag = _instance.GetExData(eExdataDefine.e69) < 1;
                    }
                        break;
                }
            }
            RefurbishAnnouncementSign();
        }

        private void InitializeDatum()
        {
            _mReChargeDic.Clear();
            _mInvestmentDic.Clear();
            ForeachPayActivityIntrojectionPayment(table =>
            {
                if (!_mReChargeDic.ContainsKey(table.Type))
                {
                    _mReChargeDic[table.Type] = new List<int>();
                }
                _mReChargeDic[table.Type].Add(table.Id);
                return true;
            });
            ForeachPayActivityAccumulatedPayment(table =>
            {
                if (!_mInvestmentDic.ContainsKey(table.Type))
                {
                    _mInvestmentDic[table.Type] = new List<int>();
                }
                _mInvestmentDic[table.Type].Add(table.Id);
                return true;
            });

            tableExdata.Clear();
            tableFlagTrue.Clear();
            tableFlagFalse.Clear();

            if (_RechargeActiveCumulativeTable != null && _RechargeActiveCumulativeTable.Records != null)
            {
                foreach (var tempData in _RechargeActiveCumulativeTable.Records.Values)
                {
                    tableExdata.Add(tempData.ExtraId);
                    tableFlagTrue.Add(tempData.FlagTrueId);
                    tableFlagFalse.AddRange(tempData.FlagFalseId);
                }
            }
        }

        private void InitializeForms()
        {
            NetManager.Instance.StartCoroutine(DemandPayFormsCoroutine());
            var _mTime = Game.Instance.ServerTime.Date.AddDays(1);
            if (Trigger != null)
            {
                TimeManager.Instance.DeleteTrigger(Trigger);
                Trigger = null;
            }
            Trigger = TimeManager.Instance.CreateTrigger(_mTime, () =>
            {
                NetManager.Instance.StartCoroutine(DemandPayFormsCoroutine());
                TableUpdated = false;
            }, (int)TimeSpan.FromDays(1).TotalMilliseconds);
        }

        private void RefurbishFirstFootet(RechargeActiveEntry tbActivity)
        {
            var _notice = DataModel.FirstRecharge;
            var _sonType = tbActivity.SonType;
            //
            if (_sonType >= 1 && _sonType <= 3)
            {
                var _roleId = ObjManager.Instance.MyPlayer.RoleId;
                _sonType = _roleId + 1;
            }
            _notice.Id = _sonType;
            var _tbNotice = GainPayActivityAnnouncement(_sonType);
            _notice.IsShowBtn = (_tbNotice.IsBtnShow == 1);
            _notice.TitleStr = tbActivity.LabelText;
            _tbNotice.Desc = _tbNotice.Desc.Replace(@"\", "");
            _tbNotice.Desc = _tbNotice.Desc.Replace("n", "\n");

            _notice.MainStr = _tbNotice.Desc;
            _notice.Desc = _tbNotice.Desc;
            _notice.BtnText = _tbNotice.BtnText;

            for (var i = 0; i < _tbNotice.ItemId.Count; i++)
            {
                _notice.ItemId[i] = _tbNotice.ItemId[i];
                _notice.ItemCount[i] = _tbNotice.ItemCount[i];
            }
            if (PlayerDataManager.Instance.CheckCondition(_tbNotice.ConditionId) == 0)
            {
                _notice.GetState = 1;
            }
            else
            {
                _notice.GetState = 0;
            }
        }

        private void RefurbishIntrojectionFootet(RechargeActiveEntry tbActivity)
        {
            var _investment = DataModel.Investment;
            var _rechargeId = tbActivity.SonType;
            if (!_mInvestmentDic.ContainsKey(_rechargeId))
            {
                return;
            }
            //"不限时"
            var _varStr = GameUtils.GetDictionaryText(270285);
            //yyyy年MM月dd日hh:mm:ss
            var _varStr2 = GameUtils.GetDictionaryText(270286);
            var _tbRecharge = GainPayActivityAccumulated(_rechargeId);
            var _values = _mInvestmentDic[_rechargeId];
            var _count = _values.Count;
            var _instance = PlayerDataManager.Instance;
            var _totalDiaCount = 0;
            var _list = new List<RechargeActivityInvestmentItemDataModel>();
            for (var i = 0; i < _count; i++)
            {
                var _item = new RechargeActivityInvestmentItemDataModel();
                var _tbReward = GainPayActivityAccumulatedPayment(_values[i]);
                _item.Id = _tbReward.Id;
                _totalDiaCount += _tbReward.ItemCount;
                if (_instance.GetFlag(_tbReward.Flag))
                {
                    _item.GetState = 2;
                }
                else
                {
                    if (_instance.CheckCondition(_tbReward.ConditionId) == 0)
                    {
                        _item.GetState = 1;
                    }
                    else
                    {
                        _item.GetState = 0;
                    }
                }
                _item.Desc1 = _tbReward.Desc1;
                _item.Desc2 = _tbReward.Desc2;
                _item.ItemId = _tbReward.ItemId;
                _item.ItemCount = _tbReward.ItemCount;
                _list.Add(_item);
            }
            var _startTimeStr = string.Empty;
            var _endTimeStr = string.Empty;
            if (tbActivity.OpenRule == (int)eRechargeActivityOpenRule.Last)
            {
                _investment.DuringTime = _varStr;
            }
            else if (tbActivity.OpenRule == (int)eRechargeActivityOpenRule.NewServerAuto)
            {
                _startTimeStr = _instance.OpenTime.AddHours(tbActivity.StartTime.ToInt()).ToString(_varStr2);
                _endTimeStr = _instance.OpenTime.AddHours(tbActivity.EndTime.ToInt()).ToString(_varStr2);
                _investment.DuringTime = _startTimeStr + "-" + _endTimeStr;
            }
            else if (tbActivity.OpenRule == (int)eRechargeActivityOpenRule.LimitTime)
            {
                _startTimeStr = Convert.ToDateTime(tbActivity.StartTime).ToString(_varStr2);
                _endTimeStr = Convert.ToDateTime(tbActivity.EndTime).ToString(_varStr2);
                _investment.DuringTime = _startTimeStr + "-" + _endTimeStr;
            }

            var _strs = _tbRecharge.BuyConditionText.Split('|');

            //DataModel.Recharge.TotalDiamond = instance.GetExData(tbRecharge.ExtraId);
            _investment.Id = _tbRecharge.Id;
            _investment.Multiple = _strs.Length > 1 ? int.Parse(_strs[1]) : 0;
            var itemName = Table.GetItemBase(_tbRecharge.NeedItemId).Name;
            _investment.GetStr = "";//totalDiaCount + "倍" + itemName;
            _investment.NeedStr = _tbRecharge.NeedItemCount + itemName;
            _investment.MainLists = new ObservableCollection<RechargeActivityInvestmentItemDataModel>(_list);
            _investment.CanBuy = _instance.GetFlag(_tbRecharge.FlagTrueId) ? 0 : 1;
            _investment.TypeStr = _tbRecharge.TypeStr;
            _investment.BuyConditionText = _strs[0];
            _investment.BgIconId = _tbRecharge.BgIconId;
        }

        private void RefurbishCarteDatum()
        {
            var _list = new List<RechargeActivityMenuItemDataModel>();
            var _instance = PlayerDataManager.Instance;
            //"不限时"
            var _varStr = GameUtils.GetDictionaryText(270285);
            var _first = true;
            NearTime = Game.Instance.ServerTime;
            ForeachPayActivity(table =>
            {
                if (!table.ServerIds.Contains(-1) && !table.ServerIds.Contains(PlayerDataManager.Instance.ServerId))
                {
                    return true;
                }

                var _serverTime = Game.Instance.ServerTime;
                if (table.OpenRule == (int)eRechargeActivityOpenRule.Last)
                {
                    var _item = new RechargeActivityMenuItemDataModel();
                    _item.ActivityId = table.Id;
                    _item.OverTime = Game.Instance.ServerTime.AddSeconds(-2);
                    _item.TimeLimitStr = _varStr;
                    var _tb = GainPayActivity(table.Id);
                    _item.Icon = _tb.Icon;
                    _item.LabelText = _tb.LabelText;
                    _list.Add(_item);
                }
                else if (table.OpenRule == (int)eRechargeActivityOpenRule.NewServerAuto)
                {
                    var _tb = GainPayActivity(table.Id);
                    var _overTime =
                        _instance.OpenTime.AddHours(_tb.EndTime.ToInt());
                    var _startTime = _instance.OpenTime.AddHours(_tb.StartTime.ToInt());

                    if (table.Type == 2) //是投资活动 而且买了 就延长7天
                    {
                        var _sonId = table.SonType;
                        var _tbTouZi = GainPayActivityAccumulated(_sonId);
                        if (_tbTouZi != null)
                        {
                            var _flag = _tbTouZi.FlagTrueId;
                            if (PlayerDataManager.Instance.GetFlag(_flag))
                            {
                                _overTime = _overTime.AddDays(7);
                            }
                        }
                    }

                    if (_overTime < Game.Instance.ServerTime)
                    {
                        return true;
                    }
                    if (_startTime > Game.Instance.ServerTime)
                    {
                        if (_first)
                        {
                            NearTime = _startTime;
                        }
                        else
                        {
                            if (_startTime < NearTime)
                            {
                                NearTime = _startTime;
                            }
                        }
                        _first = false;
                    }
                    else
                    {
                        var _item = new RechargeActivityMenuItemDataModel();
                        _item.ActivityId = table.Id;
                        _item.OverTime = _overTime;
                        _item.TimeLimitStr = string.Empty;
                        _item.Icon = _tb.Icon;
                        _item.LabelText = _tb.LabelText;
                        _list.Add(_item);
                        if (_first)
                        {
                            NearTime = _overTime;
                        }
                        else
                        {
                            if (_overTime < NearTime)
                            {
                                NearTime = _overTime;
                            }
                        }
                        _first = false;
                    }
                }
                else if (table.OpenRule == (int)eRechargeActivityOpenRule.LimitTime)
                {
                    var _startTime = Convert.ToDateTime(table.StartTime);
                    var _overTime = Convert.ToDateTime(table.EndTime);

                    if (table.Type == 2) //是投资活动 而且买了 就延长7天
                    {
                        var _sonId = table.SonType;
                        var _tbTouZi = GainPayActivityAccumulated(_sonId);
                        if (_tbTouZi != null)
                        {
                            var _flag = _tbTouZi.FlagTrueId;
                            if (PlayerDataManager.Instance.GetFlag(_flag))
                            {
                                _overTime = _overTime.AddDays(7);
                            }
                        }
                    }

                    if (_serverTime > _startTime && _serverTime < _overTime)
                    {
                        var _item = new RechargeActivityMenuItemDataModel();
                        _item.ActivityId = table.Id;
                        _item.OverTime = _overTime;
                        _item.TimeLimitStr = string.Empty;
                        var _tb = GainPayActivity(table.Id);
                        _item.Icon = _tb.Icon;
                        _item.LabelText = _tb.LabelText;
                        _list.Add(_item);
                        if (_first)
                        {
                            NearTime = _overTime;
                        }
                        else
                        {
                            if (_overTime < NearTime)
                            {
                                NearTime = _overTime;
                            }
                        }
                        _first = false;
                    }
                }
                return true;
            });
            if (!_first) //活动结束刷新menu
            {
                if (RefleshTrigger != null)
                {
                    TimeManager.Instance.DeleteTrigger(RefleshTrigger);
                    RefleshTrigger = null;
                }
                RefleshTrigger = TimeManager.Instance.CreateTrigger(NearTime, () =>
                {
                    RefurbishCarteDatum();
                    CarteProvisionHit(_menuSelectIndex);
                });
            }

            DataModel.MenuLists = new ObservableCollection<RechargeActivityMenuItemDataModel>(_list);
            if (DataModel.MenuLists.Count > 0)
            {
                DataModel.IsShowTouZiBtn = 1;
            }
            else
            {
                DataModel.IsShowTouZiBtn = 0;
            }
            InitializeStudy();
        }

        private void RefurbishAnnouncementByExdatum(int exdataId)
        {
            if (!_mExtraIdToMenuItem.ContainsKey(exdataId))
            {
                return;
            }
            var _instance = PlayerDataManager.Instance;
            var _item = _mExtraIdToMenuItem[exdataId];
            var _tbRecharge = GainPayActivity(_item.ActivityId);
            var _type = _tbRecharge.Type;
            switch (_type)
            {
                case (int)eReChargeRewardType.Investment:
                {
                    var _tbCumulative = GainPayActivityAccumulated(_tbRecharge.SonType);
                    var _rewardList = _mInvestmentDic[_tbCumulative.Id];
                    var _count = _rewardList.Count;
                    var _flag = false;
                    for (var i = 0; i < _count; i++)
                    {
                        var _tbReward = GainPayActivityAccumulatedPayment(_rewardList[i]);
                        if (_instance.GetFlag(_tbReward.Flag))
                        {
                            continue;
                        }
                        if (_instance.CheckCondition(_tbReward.ConditionId) == 0)
                        {
                            _flag = true;
                            break;
                        }
                    }
                    _item.NoticeFlag = _flag;
                }
                    break;
                case (int)eReChargeRewardType.Recharge:
                {
                    var _tbInvestment = GainPayActivityIntrojection(_tbRecharge.SonType);
                    var _rewardList = _mReChargeDic[_tbInvestment.Id];
                    var _count = _rewardList.Count;
                    var _flag = false;
                    for (var i = 0; i < _count; i++)
                    {
                        var _tbReward = GainPayActivityIntrojectionPayment(_rewardList[i]);
                        if (_instance.GetFlag(_tbReward.Flag))
                        {
                            continue;
                        }
                        if (_instance.CheckCondition(_tbReward.ConditionId) == 0)
                        {
                            _flag = true;
                            break;
                        }
                    }
                    _item.NoticeFlag = _flag;
                }
                    break;
            }
            RefurbishAnnouncementSign();
        }

        private void RefurbishAnnouncementSign()
        {
            var _oldFlag = PlayerDataManager.Instance.NoticeData.RechageActivity;
            var _menuList = DataModel.MenuLists;
            var _flag = false;
            foreach (var item in _menuList)
            {
                if (item.NoticeFlag)
                {
                    _flag = true;
                    break;
                }
            }
            //var payCountTotal = PlayerDataManager.Instance.GetExData(eExdataDefine.e69);
            //flag = flag || payCountTotal < 1;
            PlayerDataManager.Instance.NoticeData.RechageActivity = _flag;
        }

        private void RefurbishAnnouncementFootet(RechargeActiveEntry tbActivity)
        {
            var _notice = DataModel.Notice;
            var _sonType = tbActivity.SonType;
            var _tbNotice = GainPayActivityAnnouncement(_sonType);
            _notice.IsShowBtn = (_tbNotice.IsBtnShow == 1);
            _notice.TitleStr = tbActivity.LabelText;
            _tbNotice.Desc = _tbNotice.Desc.Replace(@"\", "");
            _tbNotice.Desc = _tbNotice.Desc.Replace("n", "\n");
            _notice.MainStr = _tbNotice.Desc;
            _notice.Desc = _tbNotice.Desc;
            _notice.BtnText = _tbNotice.BtnText;

            for (var i = 0; i < _tbNotice.ItemId.Count; i++)
            {
                _notice.ItemId[i] = _tbNotice.ItemId[i];
                _notice.ItemCount[i] = _tbNotice.ItemCount[i];
            }

            if (PlayerDataManager.Instance.CheckCondition(_tbNotice.ConditionId) == 0)
            {
                _notice.GetState = 1;
            }
            else
            {
                _notice.GetState = 0;
            }
        }

        private void RefurbishPayFootet(RechargeActiveEntry tbActivity)
        {
            var _recharge = DataModel.Recharge;
            var _rechargeId = tbActivity.SonType;
            if (!_mReChargeDic.ContainsKey(_rechargeId))
            {
                return;
            }
            //"不限时"
            var _varStr = GameUtils.GetDictionaryText(270285);
            //yyyy年MM月dd日hh:mm:ss
            var _varStr2 = GameUtils.GetDictionaryText(270286);
            var _tbRecharge = GainPayActivityIntrojection(_rechargeId);
            var _values = _mReChargeDic[_rechargeId];
            var _count = _values.Count;
            var _btnText = _tbRecharge.BtnText;
            var _instance = PlayerDataManager.Instance;
            var _list = new List<RechargeActivityRewardItemDataModel>();
            var _day = 0;
            for (var i = 0; i < _count; i++)
            {
                var _item = new RechargeActivityRewardItemDataModel();
                var _tbReward = GainPayActivityIntrojectionPayment(_values[i]);
                _item.Id = _tbReward.Id;
                if (_instance.GetFlag(_tbReward.Flag))
                {
                    _item.GetState = 2;
                }
                else
                {
                    if (_instance.CheckCondition(_tbReward.ConditionId) == 0)
                    {
                        _item.GetState = 1;
                    }
                    else
                    {
                        _item.GetState = 0;
                    }
                }
                var _index = _tbRecharge.ConditionText.IndexOf("{#day}");
                if (_index == -1)
                {
                    _item.ConditionText = string.Format(_tbRecharge.ConditionText, _tbReward.DiaNeedCount);
                }
                else
                {
                    _day++;
                    _item.ConditionText = _tbRecharge.ConditionText.Replace("{#day}", _day.ToString());
                }

                _item.BtnText = _btnText;
                for (var ii = 0; ii < _tbReward.ItemId.Count; ii++)
                {
                    _item.ItemId[ii] = _tbReward.ItemId[ii];
                    _item.ItemCount[ii] = _tbReward.ItemCount[ii];
                }
                _list.Add(_item);
            }
            var _startTimeStr = string.Empty;
            var _endTimeStr = string.Empty;

            if (tbActivity.OpenRule == (int)eRechargeActivityOpenRule.Last)
            {
                _recharge.DuringTime = _varStr;
            }
            else if (tbActivity.OpenRule == (int)eRechargeActivityOpenRule.NewServerAuto)
            {
                _startTimeStr = _instance.OpenTime.AddHours(tbActivity.StartTime.ToInt()).ToString(_varStr2);
                _endTimeStr = _instance.OpenTime.AddHours(tbActivity.EndTime.ToInt()).ToString(_varStr2);
                _recharge.DuringTime = _startTimeStr + "-" + _endTimeStr;
            }
            else if (tbActivity.OpenRule == (int)eRechargeActivityOpenRule.LimitTime)
            {
                _startTimeStr = Convert.ToDateTime(tbActivity.StartTime).ToString(_varStr2);
                _endTimeStr = Convert.ToDateTime(tbActivity.EndTime).ToString(_varStr2);
                _recharge.DuringTime = _startTimeStr + "-" + _endTimeStr;
            }


            _recharge.Id = _tbRecharge.Id;
            _recharge.TotalDiamond = _instance.GetExData(_tbRecharge.ExtraId);
            _recharge.TotalDiamondStr = string.Format(_tbRecharge.Tips, _recharge.TotalDiamond);
            _recharge.Type = _tbRecharge.Type;
            _recharge.Tips = _tbRecharge.Tips;
            _recharge.ConditionText = _tbRecharge.ConditionText;
            _recharge.BtnText = _tbRecharge.BtnText;
            _recharge.BgIconId = _tbRecharge.BgIconId;
            _recharge.MainLists = new ObservableCollection<RechargeActivityRewardItemDataModel>(_list);
        }

        private void CostAchieved(int index)
        {
            if (_RechargeActiveCumulativeTable == null || _RechargeActiveCumulativeTable.Records == null)
            {
                return;
            }
            var _has = false;
            foreach (var tempData in _RechargeActiveCumulativeTable.Records.Values)
            {
                if (tempData.FlagTrueId == index)
                {
                    _has = true;
                }
            }
            if (_has)
            {
                CarteProvisionHit(_menuSelectIndex);
                var _e = new ShowUIHintBoard(431);
                EventDispatcher.Instance.DispatchEvent(_e);
            }

        }

        private IEnumerator AwaitToSecondsToDemandFormsCoroutine(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            InitializeForms();
        }

        private void CarteProvisionHit(int index)
        {
            if (DataModel.MenuLists == null)
            {
                return;
            }

            if (index >= DataModel.MenuLists.Count)
            {
                return;
            }

            _menuSelectIndex = index;
            if (_mSelectedMenuItem != null)
            {
                _mSelectedMenuItem.Selected = 0;
            }
            _mSelectedMenuItem = DataModel.MenuLists[index];
            _mSelectedMenuItem.Selected = 1;
            var _tbActivity = GainPayActivity(_mSelectedMenuItem.ActivityId);
            var _type = (eReChargeRewardType)_tbActivity.Type;
            switch (_type)
            {
                case eReChargeRewardType.Notice:
                {
                    RefurbishAnnouncementFootet(_tbActivity);
                }
                    break;
                case eReChargeRewardType.Recharge:
                {
                    RefurbishPayFootet(_tbActivity);
                }
                    break;
                case eReChargeRewardType.Investment:
                {
                    RefurbishIntrojectionFootet(_tbActivity);
                }
                    break;
                case eReChargeRewardType.FirstRecharge:
                {
                    RefurbishFirstFootet(_tbActivity);
                }
                    break;
                case eReChargeRewardType.DaoHang:
                {
                }
                    break;
            }
            DataModel.SelectType = _tbActivity.Type;
        }
        #endregion

        #region 数据转换

        private RechargeActiveTable _RechargeActiveTable;
        private RechargeActiveNoticeTable _RechargeActiveNoticeTable;
        private RechargeActiveCumulativeRewardTable _RechargeActiveCumulativeRewardTable;
        private RechargeActiveInvestmentRewardTable _RechargeActiveInvestmentRewardTable;
        private RechargeActiveCumulativeTable _RechargeActiveCumulativeTable;
        private RechargeActiveInvestmentTable _RechargeActiveInvestmentTable;

        private void ForeachPayActivity(Func<RechargeActiveEntry, bool> act)
        {
            if (act == null)
            {
                Logger.Error("Foreach RechargeActive act is null");
                return;
            }

            if (_RechargeActiveTable == null)
            {
                return;
            }

            if (_RechargeActiveTable.Records == null)
            {
                return;
            }

            foreach (var tempRecord in _RechargeActiveTable.Records)
            {
                try
                {
                    if (!act(tempRecord.Value))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private RechargeActiveEntry GainPayActivity(int nId)
        {
            RechargeActiveEntry tbRechargeActive;
            if (!_RechargeActiveTable.Records.TryGetValue(nId, out tbRechargeActive))
            {
                Logger.Info("RechargeActive[{0}] not find by Table", nId);
                return null;
            }
            return tbRechargeActive;
        }

        private void ForeachPayActivityAnnouncement(Func<RechargeActiveNoticeEntry, bool> act)
        {
            if (act == null)
            {
                Logger.Error("Foreach RechargeActiveNotice act is null");
                return;
            }
            foreach (var tempRecord in _RechargeActiveNoticeTable.Records)
            {
                try
                {
                    if (!act(tempRecord.Value))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private RechargeActiveNoticeEntry GainPayActivityAnnouncement(int nId)
        {
            RechargeActiveNoticeEntry tbRechargeActiveNotice;
            if (!_RechargeActiveNoticeTable.Records.TryGetValue(nId, out tbRechargeActiveNotice))
            {
                Logger.Info("RechargeActiveNotice[{0}] not find by Table", nId);
                return null;
            }
            return tbRechargeActiveNotice;
        }

        private void ForeachPayActivityIntrojection(Func<RechargeActiveInvestmentEntry, bool> act)
        {
            if (act == null)
            {
                Logger.Error("Foreach RechargeActiveInvestment act is null");
                return;
            }
            foreach (var tempRecord in _RechargeActiveInvestmentTable.Records)
            {
                try
                {
                    if (!act(tempRecord.Value))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private RechargeActiveInvestmentEntry GainPayActivityIntrojection(int nId)
        {
            RechargeActiveInvestmentEntry tbRechargeActiveInvestment;
            if (!_RechargeActiveInvestmentTable.Records.TryGetValue(nId, out tbRechargeActiveInvestment))
            {
                Logger.Info("RechargeActiveInvestment[{0}] not find by Table", nId);
                return null;
            }
            return tbRechargeActiveInvestment;
        }

        private void ForeachPayActivityIntrojectionPayment(Func<RechargeActiveInvestmentRewardEntry, bool> act)
        {
            if (act == null)
            {
                Logger.Error("Foreach RechargeActiveInvestmentReward act is null");
                return;
            }
            foreach (var tempRecord in _RechargeActiveInvestmentRewardTable.Records)
            {
                try
                {
                    if (!act(tempRecord.Value))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private RechargeActiveInvestmentRewardEntry GainPayActivityIntrojectionPayment(int nId)
        {
            RechargeActiveInvestmentRewardEntry tbRechargeActiveInvestmentReward;
            if (!_RechargeActiveInvestmentRewardTable.Records.TryGetValue(nId, out tbRechargeActiveInvestmentReward))
            {
                Logger.Info("RechargeActiveInvestmentReward[{0}] not find by Table", nId);
                return null;
            }
            return tbRechargeActiveInvestmentReward;
        }

        private void ForeachPayActivityAccumulated(Func<RechargeActiveCumulativeEntry, bool> act)
        {
            if (act == null)
            {
                Logger.Error("Foreach RechargeActiveCumulative act is null");
                return;
            }
            foreach (var tempRecord in _RechargeActiveCumulativeTable.Records)
            {
                try
                {
                    if (!act(tempRecord.Value))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private RechargeActiveCumulativeEntry GainPayActivityAccumulated(int nId)
        {
            RechargeActiveCumulativeEntry tbRechargeActiveCumulative;
            if (!_RechargeActiveCumulativeTable.Records.TryGetValue(nId, out tbRechargeActiveCumulative))
            {
                Logger.Info("RechargeActiveCumulative[{0}] not find by Table", nId);
                return null;
            }
            return tbRechargeActiveCumulative;
        }

        private void ForeachPayActivityAccumulatedPayment(Func<RechargeActiveCumulativeRewardEntry, bool> act)
        {
            if (act == null)
            {
                Logger.Error("Foreach RechargeActiveCumulativeReward act is null");
                return;
            }
            foreach (var tempRecord in _RechargeActiveCumulativeRewardTable.Records)
            {
                try
                {
                    if (!act(tempRecord.Value))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private RechargeActiveCumulativeRewardEntry GainPayActivityAccumulatedPayment(int nId)
        {
            RechargeActiveCumulativeRewardEntry tbRechargeActiveCumulativeReward;
            if (!_RechargeActiveCumulativeRewardTable.Records.TryGetValue(nId, out tbRechargeActiveCumulativeReward))
            {
                Logger.Info("RechargeActiveCumulativeReward[{0}] not find by Table", nId);
                return null;
            }
            return tbRechargeActiveCumulativeReward;
        }

        #endregion
    }
}