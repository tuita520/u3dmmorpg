/********************************************************************************* 

                         Scorpion



  *FileName:PayFrameCtrler

  *Version:1.0

  *Date:2017-06-17

  *Description:

**********************************************************************************/


#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using LitJson;
using Platfrom.TypeSDKHelper;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class PayFrameCtrler : IControllerBase
    {

        #region 构造函数
        public PayFrameCtrler()
        {
            platfrom = "android";
#if UNITY_ANDROID
        platfrom = "android";
#elif UNITY_IOS
        platfrom = "ios";
#endif
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_RechargeFrame_OnClick.EVENT_TYPE, OnConfigurationHitedEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDatumInitializeEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, ApplyKaiFuTeHui);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagUpdateEvent);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagInitEvent);
            EventDispatcher.Instance.AddEventListener(ExData64InitEvent.EVENT_TYPE, OnExDatumInitializeEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDatumRenovateEvent);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnRenovateVipExpEvent);
            EventDispatcher.Instance.AddEventListener(RechargeSuccessEvent.EVENT_TYPE, OnPayAchievedEvent);
            EventDispatcher.Instance.AddEventListener(OnTouZiBtnClick_Event.EVENT_TYPE, OnInvestBtnHitEvent);
            EventDispatcher.Instance.AddEventListener(GetVipRewardEvent.EVENT_TYPE, OnGetVipReward);
            EventDispatcher.Instance.AddEventListener(GotoRechargeEvent.EVENT_TYPE, OnClickRechage);
            EventDispatcher.Instance.AddEventListener(ShowTipsEvent.EVENT_TYPE,OnShowTipsEvent);

            EventDispatcher.Instance.AddEventListener(ChangeDayEvent.EVENT_TYPE, OnChangeDayEvent);

        }
        #endregion

        #region 成员变量
        private RechargeDataModel DataModel;
        private int maxVipLevel;
        private readonly HashSet<int> mExdataKey = new HashSet<int>();
        private readonly string platfrom;

        private KaiFuTeHuiData responseKaiFuTeHuiData;

        private string lastOrderId = String.Empty;
        #endregion

        #region 事件
        private void OnExDatumInitializeEvent(IEvent ievent)
        {
            RefurbishFristPay();
            RefurbishPayProvision();
            RefurbishSlogan();
        }
        private void OnChangeDayEvent(IEvent iEvent)
        {
            NetManager.Instance.StartCoroutine(ChangeDay());
        }

        private IEnumerator ChangeDay()
        {
             yield return new WaitForSeconds(0.2f);
             NetManager.Instance.StartCoroutine(ApplyTeHuiDataCoroutine());
        }
        private void ApplyKaiFuTeHui(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ApplyTeHuiDataCoroutine());
        }
    
        private IEnumerator ApplyTeHuiDataCoroutine()
        {
            var _msg = NetManager.Instance.ApplyKaiFuTeHuiData(0);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply && _msg.ErrorCode == (int)ErrorCodes.OK)
            {
                responseKaiFuTeHuiData = _msg.Response;
                RefreshKaiFuTeHui();
            }
        }

        private void OnShowTipsEvent(IEvent ievent)
        {
            var e = ievent as ShowTipsEvent;
            switch (e.Type)
            {
                case 0:
                {
                    DataModel.ShowTips = true;
                    if (PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel) == 0)
                    {
                        DataModel.VipInfoIndex = 1;
                    }
                    else
                    {
                        DataModel.VipInfoIndex = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);
                    }
                    ShowLeftOrRightArrow();
                    SwitchVipInformation();
                }
                    break;
                case 1:
                {
                    DataModel.ShowTips = false;
                } 
                    break;
            }
        }
        private void OnFlagUpdateEvent(IEvent ievent)
        {
            var e = ievent as FlagUpdateEvent;
            if (e == null)
            {
                return;
            }
            if (e.Index == 2506)
            {
                RefurbishVipInformation();
                return;
            }
        }

        private void OnFlagInitEvent(IEvent ievent)
        {
            RefurbishVipInformation();
        }

        private void OnExDatumRenovateEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            if (_e == null)
            {
                return;
            }

            if (mExdataKey.Contains(_e.Key))
            {
                RefurbishPayProvision();
                RefurbishSlogan();
            }

            if (_e.Key == (int) eExdataDefine.e69)
            {
                RefurbishSlogan();
            }

            if (responseKaiFuTeHuiData != null && _e.Key == responseKaiFuTeHuiData.ExData)
            {
                DataModel.KaiFuTeHui.HasBuy = _e.Value <= 0;
            }
        }
        private void OnConfigurationHitedEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_RechargeFrame_OnClick;

            switch (_e.index)
            {
                case 0: // 充值物品点击
                {
                    if (_e.exData != null)
                    {
                        if ((int)_e.exData == -1)
                        {
                            if (platfrom.Equals("ios"))
                            {
                                OnPayProvisionHit(40);
                            }

                            if (platfrom.Equals("android"))
                            {
                                OnPayProvisionHit(41);
                            }
                        }
                        else
                        {
                            OnPayProvisionHit((int)_e.exData);
                        }
                    }
                    else
                    {
                        Logger.Error("recharge item tableid = null!!");
                    }
                }
                    break;
                case 1: // banner购买
                {
                    PurchaseSloganProvision();
                }
                    break;
                case 2: //随身药店
                {
                    OnMedicineShop();
                }
                    break;
                case 3: //随身仓库
                {
                    OnCommodityResidence();
                }
                    break;
                case 4: //快捷修理
                {
                    OnCoreRestore();
                }

                    break;
                case 5: //vip信息翻页 -1前 1 后
                {
                    if (_e.exData != null)
                    {
                        VipInformationFootetScroll((int)_e.exData);
                    }
                }

                    break;
                case 6: //购买开服特惠
                {
                    BuyKaiFuTeHui();
                }

                    break;
            }
        }

        private void BuyKaiFuTeHui()
        {
            if (responseKaiFuTeHuiData == null)
            {
                return;
            }

            if (responseKaiFuTeHuiData.Id == 0)
            {
                return;
            }

            // 检查次数是否足够
            if (PlayerDataManager.Instance.GetExData(responseKaiFuTeHuiData.ExData) <= 0)
            {
                GameUtils.ShowNetErrorHint((int)ErrorCodes.Error_BuyTeHui_Failed_No_Times);
                return;
            }

            // 检查钱够不够
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes) < responseKaiFuTeHuiData.NowPrice)
            {
                //GameUtils.ShowNetErrorHint((int)ErrorCodes.DiamondNotEnough);
                var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            NetManager.Instance.StartCoroutine(BuyTeHuiItemCoroutine());
        }

        private IEnumerator BuyTeHuiItemCoroutine()
        {
            var _msg = NetManager.Instance.BuyKaiFuTeHuiItem(responseKaiFuTeHuiData.Id);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply && _msg.ErrorCode == (int)ErrorCodes.OK)
            {
                //购买成功
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(431));
            }
            else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
            {
                var _e = new ShowUIHintBoard(430);
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            else
            {
                UIManager.Instance.ShowNetError(_msg.ErrorCode);
            }
        }

        private void OnInvestBtnHitEvent(IEvent ievent)
        {
            var _e = ievent as OnTouZiBtnClick_Event;
            if (_e == null)
            {
                return;
            }

            var _tableid = _e.TableId;
            NetManager.Instance.StartCoroutine(PurchaseWaresCoroutine(_tableid));
        }

        private void OnPayAchievedEvent(IEvent ievent)
        {
            var _e = ievent as RechargeSuccessEvent;
            var _table = Table.GetRecharge(_e.RechargeId);
            if (_table != null)
            {
                var _str = GameUtils.GetDictionaryText(300833);
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, _str);

                PlatformHelper.CollectionPayDataForKuaifa(_table.Price.ToString(), lastOrderId, _table.Desc);
            }
        }

        private void OnRenovateVipExpEvent(IEvent ievent)
        {
            var _e = ievent as Resource_Change_Event;
            if (_e.Type == eResourcesType.VipExpRes)
            {
                RefurbishVipInformation();
            }
            else if (_e.Type == eResourcesType.VipLevel)
            {
                RefurbishVipInformation();
            }
        }
        #endregion

        #region 逻辑函数
        private void PurchaseSloganProvision()
        {
            if (DataModel.BannerItem != null)
            {
                OnPayProvisionHit(DataModel.BannerItem.TableId);
            }
        }

        private IEnumerator PurchaseWaresCoroutine(int tableid)
        {

            var tbConfig = Table.GetClientConfig(1212);
            var id = tbConfig.Value.ToInt();
            if (id != -1)
            {
                var message = GameUtils.GetDictionaryText(id);
                if (!string.IsNullOrEmpty(message))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, message);
                    yield break;
                }

            }

            using (new BlockingLayerHelper(0))
            {
                var _outMessage = new ApplyOrderMessage();
                var _channel = GameSetting.Channel;
                _outMessage.Channel = string.Format("{0}.{1}", platfrom, _channel);
                _outMessage.GoodId = tableid;
                _outMessage.ExtInfo = "XXXX";
#if USETYPESDK
        var platdata = U3DTypeSDK.Instance.GetPlatformData();
        var channelid = platdata.GetData(U3DTypeAttName.CHANNEL_ID);
        var cpid = platdata.GetData(U3DTypeAttName.CP_ID);
        _outMessage.ExtInfo = string.Format("{0}.{1}.{2}", cpid, channelid, TypeSDKHelper.Instance.userIdforPay);
#endif
                var _msg = NetManager.Instance.ApplyOrderSerial(_outMessage);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {

                        var _table = Table.GetRecharge(tableid);
                        var _roleid = PlayerDataManager.Instance.GetGuid().ToString();
                        var _roleName = PlayerDataManager.Instance.GetName();
                        var _goodsName = _table.Name;
                        var _oid = _msg.Response.OrderId;
                        lastOrderId = _oid;
                        var _level = PlayerDataManager.Instance.GetRes((int) eResourcesType.LevelRes).ToString();
                        var _serverId = PlayerDataManager.Instance.ServerId.ToString();
                        var _serverName = PlayerDataManager.Instance.ServerName;
                        var _haveDiamond = PlayerDataManager.Instance.GetRes((int) eResourcesType.DiamondRes).ToString();

                        var _sb = new System.Text.StringBuilder();
                        var _writer = new JsonWriter(_sb);
                        _writer.WriteObjectStart();
                        _writer.WritePropertyName("roleID");
                        _writer.Write(_roleid);
                        _writer.WritePropertyName("roleName");
                        _writer.Write(_roleName);
                        _writer.WritePropertyName("goodsName");
                        _writer.Write(_goodsName);
                        _writer.WritePropertyName("goodsPrice");
                        _writer.Write(_table.Price.ToString());
                        _writer.WritePropertyName("oid");
                        _writer.Write(_oid);
                        _writer.WritePropertyName("roleLevel");
                        _writer.Write(_level);
                        _writer.WritePropertyName("serverId");
                        _writer.Write(_serverId);
                        _writer.WritePropertyName("serverName");
                        _writer.Write(_serverName);
                        _writer.WritePropertyName("tableId");
                        _writer.Write(tableid);
                        _writer.WritePropertyName("goodsDesc");
                        _writer.Write(_table.Name);
                        _writer.WritePropertyName("diamond");
                        _writer.Write(_haveDiamond);
                        _writer.WritePropertyName("applePid");
                        _writer.Write(_table.GoodsId);

                        //支持多包支付，把平台，渠道，大版本号透传给支付服务器
                        var sb2 = new System.Text.StringBuilder();
                        var writer2 = new JsonWriter(sb2);
                        writer2.WritePropertyName("cporder");
                        writer2.Write(_oid);
                        writer2.WritePropertyName("platform");
                        writer2.Write(UpdateHelper.Platform);
                        writer2.WritePropertyName("gameversion");
                        writer2.Write(UpdateHelper.LocalGameVersion);
                        writer2.WritePropertyName("channel");
                        writer2.Write(UpdateHelper.Channel);

                        _writer.WritePropertyName("cpPrivateInfo");
                        _writer.Write(sb2.ToString());

                        _writer.WriteObjectEnd();

                        
                        PlatformHelper.MakePayWithGoodInfo(_sb.ToString());
                    }
                }
            }
        }

        private RechargeItemDataModel ProduceProvisionFromForm(RechargeRecord table)
        {
            if (table.Visible != 0 && table.Platfrom.Equals(platfrom) && table.Type != 3)
            {
                var _item = new RechargeItemDataModel();
                _item.TableId = table.Id;
                _item.ItemId = table.ItemId;
                _item.GoodName = table.Name;
                _item.GoodPrice = table.Price;
                _item.GoodType = table.Type;
                _item.GoodUnit = GainGoodPart();
                _item.PurchaseTimes = PlayerDataManager.Instance.GetExData(table.ExdataId) + 1;
                if (_item.GoodType == 0) // 月卡
                {

                    var _timespan = GainMonthSolitaireLastTime();
                    if (_timespan.TotalSeconds > 0)
                    {
                        var _desc = string.Format(table.Desc, (int)_timespan.TotalDays);
                        _item.GoodDesc = _desc;
                        _item.Recommendation = false;
                    }
                    else
                    {
                        _item.GoodDesc = table.ExDesc;
                        _item.Recommendation = true;
                    }
                }
                else if (_item.GoodType == 1) // 普通充值
                {
                    var _lastTimes = table.ExTimes - _item.PurchaseTimes;
                    if (_lastTimes == 0 || table.ExTimes < 0)
                    {
                        var exPercent = ((float)table.ExDiamond / table.Diamond).ToString("0%");
                        _item.GoodDesc = string.Format(table.ExDesc, table.ExDiamond, exPercent);
                        _item.Recommendation = true;
                    }
                    else
                    {
                        var normalPercent = ((float) table.NormalDiamond/table.Diamond).ToString("0%");
                        _item.GoodDesc = string.Format(table.ExDesc, table.NormalDiamond, normalPercent);
                        _item.Recommendation = false;
                    }
                }
                return _item;
            }
            return null;
        }

        private string GainGoodPart()
        {
            var _unit = "元";

            //example
            if (GameSetting.Channel.Equals("91netdragon"))
            {
                _unit = "豆";
            }
            return _unit;
        }

        private TimeSpan GainMonthSolitaireLastTime()
        {
            var _expirationDate = /*DateTime.Now.AddHours(1).ToBinary();*/
                PlayerDataManager.Instance.GetExData64((int)Exdata64TimeType.MonthCardExpirationDate);
            var _expirationTime = Extension.FromServerBinary(_expirationDate);
            return _expirationTime - Game.Instance.ServerTime;
        }

        private void OnMedicineShop()
        {
            var _arg = new StoreArguments { Tab = 0 };
            var _e = new Show_UI_Event(UIConfig.StoreUI, _arg);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        private void OnCoreRestore()
        {
            GameUtils.OnQuickRepair();
        }

        private void OnPayProvisionHit(int tableid)
        {
            NetManager.Instance.StartCoroutine(PurchaseWaresCoroutine(tableid));
        }

        private void OnCommodityResidence()
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.DepotUI));
        }

        private void RefurbishSlogan()
        {
            DataModel.ShowFristChargeBanner = false;
            DataModel.ShowMonthBanner = false;
            DataModel.ShowItemBanner = false;

            var _payCountTotal = PlayerDataManager.Instance.GetExData(eExdataDefine.e69);
            //         if (payCountTotal < 1)
            //         {
            //             DataModel.ShowFristChargeBanner = true;
            //         }
            //         else
            {
                if (GainMonthSolitaireLastTime().TotalSeconds < 0)
                {
                    DataModel.ShowMonthBanner = true;
                    DataModel.BannerItem = DataModel.RechargeItems.First(item => item.GoodType == 0);
                }
                else
                {
                    DataModel.ShowItemBanner = true;
                    var _c = DataModel.RechargeItems.Count;
                    DataModel.BannerItem = DataModel.RechargeItems[_c - 1];
                    for (var i = 0; i < _c; i++)
                    {
                        var _item = DataModel.RechargeItems[i];
                        if (_item.GoodType == 0)
                        {
                            continue;
                        }
                        var _record = Table.GetRecharge(_item.TableId);
                        var _exdata = PlayerDataManager.Instance.GetExData(_record.ExdataId);
                        if (_record.ExTimes > _exdata)
                        {
                            DataModel.BannerItem = _item;
                            break;
                        }
                    }
                }
            }
        }

        //刷新首充物品
        private void RefurbishFristPay()
        {
            //首充奖励
            DataModel.FristChargeReward.Clear();
            var _tbId = 590 + PlayerDataManager.Instance.GetRoleId();
            var _configTable = Table.GetClientConfig(_tbId);
            if (_configTable != null)
            {
                var _skillUpGradeTable = Table.GetSkillUpgrading(_configTable.Value.ToInt());
                if (_skillUpGradeTable != null)
                {
                    var _itemIds = _skillUpGradeTable.Values;
                    var _c = _itemIds.Count;
                    for (var i = 0; i < _c; i++)
                    {
                        var _id = _itemIds[i];
                        var _item = new ItemIdDataModel();
                        _item.ItemId = _id;
                        _item.Count = 1;
                        DataModel.FristChargeReward.Add(_item);
                    }
                }
            }
        }

        //刷新充值商品
        private void RefurbishPayProvision()
        {
            DataModel.RechargeItems.Clear();
            //充值物品
            Table.ForeachRecharge(table =>
            {
                if (table.Type == 4)//过滤终生卡显示
                    return true;
                var _item = ProduceProvisionFromForm(table);
                if (null != _item)
                {
                    DataModel.RechargeItems.Add(_item);
                }
                return true;
            });
        }

        //刷新vip特权功能开放
        private void RefurbishVipAction()
        {
            if (DataModel.VipLevel < 1)
            {
                return;
            }
            var _table = Table.GetVIP(DataModel.VipLevel);
            DataModel.RepairShow = _table.Repair;
            DataModel.StoreShow = _table.Depot;
        }

        //刷新vip信息
        private void RefurbishVipInformation()
        {
            //vip相关
            DataModel.VipExp = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipExpRes);
            DataModel.VipLevel = PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel);

            if (DataModel.VipLevel != maxVipLevel)
            {
                var _nextLevel = DataModel.VipLevel < maxVipLevel ? DataModel.VipLevel + 1 : maxVipLevel;
                var _tbVip = Table.GetVIP(_nextLevel);
                DataModel.VipExpString = string.Format("{0}/{1}", DataModel.VipExp, _tbVip.NeedVipExp);
                DataModel.VipProgressValue = DataModel.VipExp / (float)_tbVip.NeedVipExp;
                var _levelupExp = _tbVip.NeedVipExp - DataModel.VipExp;
                DataModel.NeedVipExpToLevelUp = _levelupExp.ToString();
                DataModel.IsHideMaxLabel = 0;
            }
            else
            {
                DataModel.VipExpString = "MAX";
                DataModel.VipProgressValue = 1;
                DataModel.NeedVipExpToLevelUp = "0";
                DataModel.IsHideMaxLabel = 1;
            }
            DataModel.VipReward.Clear();
            RefurbishVipAction();
            int vipReward = 0;
            for (int lv = DataModel.VipLevel; lv <= DataModel.VipLevel+1; lv++)
            {
                if(lv == 0 || lv > maxVipLevel)
                    continue;
                VipRewardDataModel vip = new VipRewardDataModel();
                Dictionary<int,int> reward = VIPRewardManager.GetInstance().GetVipReward(lv);
                vip.VipLevel = lv;
                if (lv != DataModel.VipLevel)
                    vip.Status = -1;
                else
                {
                    vip.Status = PlayerDataManager.Instance.GetFlag(2506)?0:1;
                    vipReward = vip.Status;
                }
                foreach (var v in reward)
                {
                    ItemIdDataModel item =     new ItemIdDataModel();
                    item.ItemId = v.Key;
                    item.Count = v.Value;
                    vip.VipReward.Add(item);
                }
                DataModel.VipReward.Add(vip);
            }
            PlayerDataManager.Instance.NoticeData.VipReward = vipReward;
        }

        //刷新vip功能信息页面
        private void SwitchVipInformation()
        {
            if (DataModel.VipInfoIndex == 0)
            {
                var _nextLevel = DataModel.VipLevel < maxVipLevel ? DataModel.VipLevel + 1 : maxVipLevel;
                DataModel.VipInfoIndex = _nextLevel;
            }
            ShowLeftOrRightArrow();
            var _tbVip = Table.GetVIP(DataModel.VipInfoIndex);
            DataModel.VipInfo = _tbVip.Desc.Replace("\\n", "\n");
            var _netLabel = DataModel.VipInfo.Replace("\n", "");
            //计算行数
            var _count = DataModel.VipInfo.Length - _netLabel.Length;
            DataModel.VipInfoLineCount = _count + 1;

            DataModel.VipItemId = new ItemIdDataModel
            {
                Count = 1,
                ItemId = _tbVip.PackItemParam[0]
            };

            DataModel.VipBuffId = new ItemIdDataModel
            {
                Count = 1,
                ItemId = _tbVip.PackItemParam[1]
            };

            DataModel.VipTitleid = new ItemIdDataModel
            {
                Count = 1,
                ItemId = _tbVip.PackItemParam[2]
            };

            DataModel.NeedDiamond = _tbVip.NeedVipExp;
        }
        private void ShowLeftOrRightArrow()
        {
            if (DataModel.VipInfoIndex == 1)
            {
                DataModel.ShowLeftArrow = false;
            }
            else if (DataModel.VipInfoIndex == 12)
            {
                DataModel.ShowRightArrow = false;
            }
            else
            {
                DataModel.ShowLeftArrow = true;
                DataModel.ShowRightArrow = true;
            }
        }
        private void VipInformationFootetScroll(int direction)
        {
            if (direction > 0)
            {
                if (DataModel.VipInfoIndex < maxVipLevel)
                {
                    DataModel.VipInfoIndex++;
                }
            }
            else
            {
                if (DataModel.VipInfoIndex > 1)
                {
                    DataModel.VipInfoIndex--;
                }
            }

            SwitchVipInformation();
        }
        #endregion


        #region 固有函数
        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name.Equals("RechargeDataModel"))
            {
                return DataModel;
            }

            return null;
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as RechargeFrameArguments;

            if (args != null && args.Tab != -1)
            {
                DataModel.TableSelect = args.Tab;
            }
            else
            {
                DataModel.TableSelect = 0;
            }


            RefurbishVipInformation();
            SwitchVipInformation();
            NetManager.Instance.StartCoroutine(ApplyTeHuiDataCoroutine());
            
        }

        private void RefreshKaiFuTeHui()
        {
            if (responseKaiFuTeHuiData == null)
            {
                return;
            }
            DataModel.KaiFuTeHui.TitleIcon = responseKaiFuTeHuiData.IconId;
            BagItemDataModel item = null;
            if( DataModel.KaiFuTeHui.SellItem == null)
                item = new BagItemDataModel();
            else
                item = DataModel.KaiFuTeHui.SellItem;
            item.ItemId = responseKaiFuTeHuiData.ItemId;
            DataModel.KaiFuTeHui.SellItem = item;
            DataModel.KaiFuTeHui.SellNum = responseKaiFuTeHuiData.ItemCount;
            DataModel.KaiFuTeHui.OldPrice = responseKaiFuTeHuiData.OldPrice;
            DataModel.KaiFuTeHui.NowPrce = responseKaiFuTeHuiData.NowPrice;
            DataModel.KaiFuTeHui.HasBuy = PlayerDataManager.Instance.GetExData((int)responseKaiFuTeHuiData.ExData) <= 0;
        }

        public void Tick()
        {
        }

        public void Close()
        {
        }

        public void OnShow()
        {
        }
        private void OnGetVipReward(IEvent ievent)
        {
            GameLogic.Instance.StartCoroutine(ActivationRewardCoroutine((int) eActivationRewardType.DailyVipGift, 2000));
        }
        private IEnumerator ActivationRewardCoroutine(int type, int id)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ActivationReward(type, id);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State != MessageState.Reply)
                {
                    Logger.Debug("[ClaimRewardCoroutine] msg.State != MessageState.Reply");
                    yield break;
                }

                if (msg.ErrorCode != (int) ErrorCodes.OK)
                {
                    Logger.Debug("[ClaimRewardCoroutine] ErrorCodes=[{0}]", msg.ErrorCode);
                    if (msg.ErrorCode == (int) ErrorCodes.Error_ItemNoInBag_All)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000006));
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip((ErrorCodes) msg.ErrorCode));
                    }
                    yield break;
                }
                else
                {
                    foreach (var v in DataModel.VipReward)
                    {
                        if (v.VipLevel == DataModel.VipLevel)
                        {
                            v.Status = 0;
                            PlayerDataManager.Instance.SetFlag(2506,true);
                            break;
                        }
                    }
                }
            }
        }
        private void OnClickRechage(IEvent e)
        {
            GameUtils.GotoUiTab(79, 0);
        }

        public void CleanUp()
        {
            responseKaiFuTeHuiData = null;
            DataModel = new RechargeDataModel();
            maxVipLevel = 0;

            mExdataKey.Clear();
            Table.ForeachVIP(table =>
            {
                maxVipLevel++;
                return true;
            });

            Table.ForeachRecharge(table =>
            {
                mExdataKey.Add(table.ExdataId);
                return true;
            });

            maxVipLevel -= 2; // 去掉首0和尾max
        }

        public FrameState State { get; set; }
        #endregion


    }
}