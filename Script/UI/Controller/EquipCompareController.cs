#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientDataModel.Equip;
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
    public class EquipCompareController : IControllerBase
    {
        public EquipCompareController()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Close.EVENT_TYPE, Button_Close);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Use.EVENT_TYPE, Button_Use);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Reclaim.EVENT_TYPE, Button_Reclaim);
            EventDispatcher.Instance.AddEventListener(EquipAutoRecycleEvent.EVENT_TYPE, AutoRecycleEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompareBtnClick.EVENT_TYPE, OnBtnClick);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Share.EVENT_TYPE, OnBtnClickShare);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Donate.EVENT_TYPE, OnBtnClickDonate);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_TakeOut.EVENT_TYPE, OnBtnClickTakeOut);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Remove.EVENT_TYPE, OnBtnClickRemove);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Sell.EVENT_TYPE, OnBtnClickSell);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipCompare_Input.EVENT_TYPE, OnBtnClickInput);
            EventDispatcher.Instance.AddEventListener(EquipMentCompare_LevelUpEvent.EVENT_TYPE, OnBtnLevelUpClick);
        }

        private EquipCompareDataModel DataModel;
        private readonly int EquipConditionId = 31; //装备强化界面开启条件
        private int mCharacterLevel;
        private bool mIsInit = true;
        private BagItemDataModel nowItem;
        private int ResType; //确定是从背包或者装备打开
        private string StrDic230004;
        private string StrDic230006;
        private string StrDic230025;
        private string StrDic230032;
        private string StrDic230033;
        private string StrDic230034;
        //关闭按钮
        private void Button_Close(IEvent ievent)
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipComPareUI));
        }

        //recycle
        private void Button_Reclaim(IEvent ievent)
        {
            var tbItemBase = Table.GetItemBase(nowItem.ItemId);
            if (null == tbItemBase)
            {
                return;
            }
            else if (-1 == tbItemBase.CallBackType)
            {
                GameUtils.ShowHintTip(100003314);
                return;
            }
            if (EquipQualityJudge(nowItem))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, GameUtils.GetDictionaryText(230200), "", () =>
                {
                    NetManager.Instance.StartCoroutine(RecycleCoroutine(nowItem.Index));
                    //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RecycleUI,
                    //    new RecycleArguments {ItemDataModel = nowItem}));
                }, () =>
                {
                    var e = new UIEvent_EquipCompare_Close();
                    EventDispatcher.Instance.DispatchEvent(e);
                }
                    );
            }
            else
            {
                NetManager.Instance.StartCoroutine(RecycleCoroutine(nowItem.Index));
                //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RecycleUI,
                //    new RecycleArguments {ItemDataModel = nowItem}));
            }
            // UIEvent_SelectRole_Index ee = ievent as UIEvent_SelectRole_Index;
        }

        private void AutoRecycleEvent(IEvent ievent)
        {
            var e = ievent as EquipAutoRecycleEvent;
            if (null == e)
                return;
            NetManager.Instance.StartCoroutine(RecycleCoroutine(e.BagIndex));
        }

        //使用
        private void Button_Use(IEvent ievent)
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipComPareUI));

            var e = ievent as UIEvent_EquipCompare_Use;

            ReplaceEquip(nowItem, e.Index);
        }

        //judge quality
        private bool EquipQualityJudge(BagItemDataModel BaseItem)
        {
            var tbBaseItem = Table.GetItemBase(BaseItem.ItemId);
            if (tbBaseItem.Quality > 2)
            {
                return true;
            }
            if (BaseItem.Exdata.Enchance > 0)
            {
                return true;
            }
            var tbEquipItem = Table.GetEquipBase(BaseItem.ItemId);
            if (BaseItem.Exdata.Append > tbEquipItem.AddAttrUpMaxValue)
            {
                return true;
            }
            return false;
        }

        private void InitStr()
        {
            StrDic230004 = GameUtils.GetDictionaryText(230004);
            StrDic230006 = GameUtils.GetDictionaryText(230006);
            StrDic230034 = GameUtils.GetDictionaryText(230034);
            StrDic230033 = GameUtils.GetDictionaryText(230033);
            StrDic230032 = GameUtils.GetDictionaryText(230032);
            StrDic230025 = GameUtils.GetDictionaryText(230025);
            mIsInit = false;
        }

        private void OnBtnClick(IEvent ievent)
        {
            var e = ievent as UIEvent_EquipCompareBtnClick;
            if (e.BtnType == 3)
            {
                var errorId = PlayerDataManager.Instance.CheckCondition(EquipConditionId);
                if (errorId != 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(errorId));
                    return;
                }
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipComPareUI));
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.EquipUI,
                    new EquipUIArguments {Data = nowItem, ResourceType = ResType}));
            }
        }

        //升阶
        private void OnBtnLevelUpClick(IEvent iEvent)
        {
            var e = iEvent as UIEvent_EquipCompareBtnClick;
            if (nowItem != null)
            {
                var tbItemBase = Table.GetItemBase(nowItem.ItemId);
                if (tbItemBase.Type == 10001 )
                {
                    var arg = new StoreArguments { Tab = 105 };
                    var ee = new Show_UI_Event(UIConfig.StoreEquip, arg);
                    EventDispatcher.Instance.DispatchEvent(ee);                
                }
                else if (tbItemBase.Type == 10006)
                {
                    ServiceManager.DoServeice(42, 1uL, 12);
                }
            }
        }

        private void OnBtnClickInput(IEvent ievent)
        {
            var e = new EquipCellSelect(nowItem);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        //sell
        private void OnBtnClickSell(IEvent ievent)
        {
            var tbItem = Table.GetItemBase(nowItem.ItemId);
            if (tbItem == null)
            {
                return;
            }

            if (tbItem.Sell <= 0)
            {
                var str = GameUtils.GetDictionaryText(270115);
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(str));
                return;
            }

            if (EquipQualityJudge(nowItem))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, GameUtils.GetDictionaryText(230199), "",
                    () => { NetManager.Instance.StartCoroutine(SellNewItemCoroutine(nowItem.Index)); });
            }
            else
            {
                NetManager.Instance.StartCoroutine(SellNewItemCoroutine(nowItem.Index));
            }
        }

        private void OnBtnClickShare(IEvent ievent)
        {
            var arg = new ChatMainArguments {Type = 0, ItemDataModel = nowItem};
            var e = new Show_UI_Event(UIConfig.ChatMainFrame, arg);
            EventDispatcher.Instance.DispatchEvent(e);
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipComPareUI));
        }

        private void OnBtnClickDonate(IEvent ievent)
        {
            var tbItem = Table.GetItemBase(nowItem.ItemId);
            if (tbItem == null)
            {
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new BattleUnionDepot_Donate(nowItem.ItemId, nowItem.Index));
        }

        private void OnBtnClickTakeOut(IEvent ievent)
        {
            var tbItem = Table.GetItemBase(nowItem.ItemId);
            if (tbItem == null)
            {
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new BattleUnionDepot_TakeOut(nowItem.ItemId, nowItem.Index));
        }

        private void OnBtnClickRemove(IEvent ievent)
        {
            var tbItem = Table.GetItemBase(nowItem.ItemId);
            if (tbItem == null)
            {
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new BattleUnionDepot_Remove(nowItem.ItemId, nowItem.Index));
        }

        private IEnumerator RecycleCoroutine(int bagIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var TempEquipList = new Int32Array();
                TempEquipList.Items.Add(bagIndex);
                var msg = NetManager.Instance.RecoveryEquip(1, TempEquipList);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var e = new UIEvent_EquipCompare_Close();
                        EventDispatcher.Instance.DispatchEvent(e);
                        var item = PlayerDataManager.Instance.GetItem((int)eBagType.Equip, bagIndex);
                        if (TempEquipList.Items.Count > 0)
                        {
                            if (item != null)
                            {                            
                                PlatformHelper.UMEvent("EquipRecycle", item.ItemId.ToString());
                            }
                        }
                        if (GameUtils.CheckIsQuickUsing(item))
                        {
                            //快速使用中的装备被回收直接关闭快速使用按钮
                            EventDispatcher.Instance.DispatchEvent(new UseItemEvent(item));
                        }
                        //EventDispatcher.Instance.DispatchEvent(new PackArrangeEventUi((int)eBagType.Equip));//应策划需求回收完成自动整理背包
                    }
                    else
                    {
                        if (msg.ErrorCode == (int)ErrorCodes.Error_NotCallBack)
                        {
                            GameUtils.ShowHintTip(100003314);
                        }
                        //UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
        }

        private void RefreshEquiped(BagItemDataModel itemData)
        {
            var tbItemBase = Table.GetItemBase(itemData.ItemId);
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);

            var attributes = PlayerDataManager.Instance.GetEquipAttributeFix(nowItem, mCharacterLevel);

            DataModel.look.FightPoint = PlayerDataManager.Instance.GetAttrFightPoint(attributes, mCharacterLevel);
            DataModel.EquipAttrChanges[0].CanReplace = true;
            var index = 0;
            if (GameUtils.IsCanEquip(tbEquip, (int) eBagType.Equip07))
            {
                index = RefreshEquipedDataModel(index, attributes, eEquipType.RingL);
                index = RefreshEquipedDataModel(index, attributes, eEquipType.RingR);
            }
            else if (tbItemBase.Type == 10099
                     && PlayerDataManager.Instance.GetEquipData(eEquipType.WeaponScend).ItemId != -1
                     && PlayerDataManager.Instance.GetEquipData(eEquipType.WeaponMain).ItemId != -1)
            {
                index = RefreshEquipedDataModel(index, attributes, eEquipType.WeaponScend, true);
                index = RefreshEquipedDataModel(index, attributes, eEquipType.WeaponMain, true);
            }
            else
            {
                for (var i = 7; i <= 18; i++)
                {
                    if (i != (int) eBagType.Equip07 && GameUtils.IsCanEquip(tbEquip, i))
                    {
                        var equipType = PlayerDataManager.Instance.BagIdToEquipType[i];
                        index = RefreshEquipedDataModel(index, attributes, (eEquipType) equipType);
                    }
                }
            }

            for (var i = index; i < 2; i++)
            {
                DataModel.EquipAttrChanges[i].IsShow = 0;
            }

            if (index != 0)
            {
                DataModel.HasEquiped = 1;
            }
        }

        private int RefreshEquipedDataModel(int index,
            Dictionary<int, int> attributes,
            eEquipType equipType,
            bool isBothHands = false)
        {
            var equipData = PlayerDataManager.Instance.PlayerDataModel.EquipList[(int) equipType];
            var equipAttr = DataModel.EquipAttrChanges[index];
            if (equipData.ItemId == -1)
            {
                equipAttr.IsShow = 0;
                return index;
            }
            var tbItemBase = Table.GetItemBase(equipData.ItemId);
            equipAttr.IsShow = 1;

            var canEquip = PlayerDataManager.Instance.CheckItemEquip(nowItem.ItemId);

            var color = GameUtils.GetTableColorString(tbItemBase.Quality);
            var level = 0;//equipData.Exdata[0];
            var levelStr = "";
            if (level > 0)
            {
                levelStr = string.Format("+{0}", level);
            }
            if (equipType == eEquipType.RingL)
            {
                equipAttr.EquipName = string.Format(GameUtils.GetDictionaryText(230001), color, tbItemBase.Name, levelStr);
            }
            else if (equipType == eEquipType.RingR)
            {
                equipAttr.EquipName = string.Format(GameUtils.GetDictionaryText(230002), color, tbItemBase.Name, levelStr);
            }
            else
            {
                equipAttr.EquipName = string.Format(GameUtils.GetDictionaryText(230000), color, tbItemBase.Name, levelStr);
            }
            equipAttr.AttrChanges.Clear();
            equipAttr.IsSame = 0;

            List<AttributeBaseDataModel> newOrderAttrChg = null;
            var fightPoint = 0;
            if (!(isBothHands && equipType == eEquipType.WeaponScend))
            {
                newOrderAttrChg = StatisticEquipAttribute(attributes, equipData);
                if (PlayerDataManager.Instance.CheckItemEquip(equipData.ItemId) == eEquipLimit.OK ||
                    canEquip == eEquipLimit.Attribute)
                {
                    fightPoint = PlayerDataManager.Instance.GetBagItemFightPoint(equipData, mCharacterLevel,false);
                }
            }

            if (isBothHands && equipType == eEquipType.WeaponScend)
            {
                DataModel.EquipAttrChanges[0].CanReplace = false;
            }


            if (isBothHands && equipType == eEquipType.WeaponMain)
            {
                var equipData12 = PlayerDataManager.Instance.PlayerDataModel.EquipList[(int) eEquipType.WeaponScend];
                newOrderAttrChg = StatisticEquipAttribute(attributes, equipData, equipData12);
                if (PlayerDataManager.Instance.CheckItemEquip(equipData.ItemId) == eEquipLimit.OK ||
                    canEquip == eEquipLimit.Attribute)
                {
                    fightPoint = PlayerDataManager.Instance.GetBagItemFightPoint(equipData, mCharacterLevel, false);
                }
                if (PlayerDataManager.Instance.CheckItemEquip(equipData12.ItemId) == eEquipLimit.OK ||
                    canEquip == eEquipLimit.Attribute)
                {
                    fightPoint += PlayerDataManager.Instance.GetBagItemFightPoint(equipData12, mCharacterLevel, false);
                }
            }

            if (newOrderAttrChg != null)
            {
                equipAttr.AttrChanges = new ObservableCollection<AttributeBaseDataModel>(newOrderAttrChg);
                equipAttr.IsSame = equipAttr.AttrChanges.Count == 0 ? 1 : 0;
            }

            var lookPoint = DataModel.look.FightPoint;

            equipAttr.IsUp = fightPoint > lookPoint ? 1 : 0;
            equipAttr.FightPoint = Math.Abs(fightPoint - lookPoint);
            //套装
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            equipAttr.SuitId = tbEquip.TieId;
            var indexList = new List<int>();
            if (tbEquip.TieId != -1)
            {
                var nNowTieCount = 0;
                PlayerDataManager.Instance.ForeachEquip(item =>
                {
                    var ItemId = item.ItemId;
                    if (ItemId == -1)
                    {
                        return;
                    }
                    var tbTieItem = Table.GetItemBase(ItemId);
                    if (tbTieItem == null)
                    {
                        return;
                    }
                    var tbTieEquip = Table.GetEquipBase(tbTieItem.Exdata[0]);
                    if (tbTieEquip == null)
                    {
                        return;
                    }
                    if (tbEquip.TieId == tbTieEquip.TieId)
                    {
                        if (!indexList.Contains(tbTieEquip.TieIndex))
                        {
                            nNowTieCount++;
                            indexList.Add(tbTieEquip.TieIndex);
                        }
                    }
                });

                equipAttr.SuitCount = nNowTieCount;
            }
            index++;
            return index;
        }

        private void RefreshItemDataModel()
        {
            var itemData = DataModel.ItemData;
            var tbItem = Table.GetItemBase(itemData.ItemId);
            DataModel.EquipId = tbItem.Exdata[0];
            var tbEquip = Table.GetEquipBase(DataModel.EquipId);
            if (tbEquip == null)
            {
                return;
            }

            var strDic = GameUtils.GetDictionaryText(230004);

            DataModel.PhaseDesc = string.Format(strDic, GameUtils.NumEntoCh(tbEquip.Ladder));

            strDic = GameUtils.GetDictionaryText(230006);
            for (var i = 0; i != 2; ++i)
            {
                var attrId = tbEquip.NeedAttrId[i];
                if (attrId != -1)
                {
                    var attrValue = tbEquip.NeedAttrValue[i];
                    var selfAttrValue = PlayerDataManager.Instance.GetAttribute(attrId);
                    var needStr = string.Format(strDic, GameUtils.AttributeName(attrId), selfAttrValue, attrValue);

                    if (selfAttrValue < attrValue)
                    {
                        DataModel.NeedAttr[i] = string.Format("[FC3737]{0}[-]", needStr);
                    }
                    else
                    {
                        DataModel.NeedAttr[i] = string.Format("[ADFF00]{0}[-]", needStr);
                    }
                }
                else
                {
                    DataModel.NeedAttr[i] = "";
                }
            }

            var enchanceLevel = itemData.Exdata[0];
            for (var i = 0; i < 4; i++)
            {
                var nAttrId = tbEquip.BaseAttr[i];
                if (nAttrId != -1)
                {
                    var baseValue = tbEquip.BaseValue[i];
                    var changeValue = 0;
                    if (enchanceLevel > 0)
                    {
                        changeValue = GameUtils.GetBaseAttr(tbEquip, enchanceLevel, i, nAttrId) - baseValue;
                    }
                    GameUtils.SetAttribute(DataModel.BaseAttr, i, nAttrId, baseValue, changeValue);
                }
                else
                {
                    DataModel.BaseAttr[i].Reset();
                }
            }


            for (var i = 0; i < 4; i++)
            {
                var attrData = DataModel.BaseAttr[i];
                var nAttrId = attrData.Type;
                if (nAttrId != -1)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    var attrValue = GameUtils.AttributeValue(nAttrId, attrData.Value);

                    if (attrData.ValueEx != 0)
                    {
                        if (attrData.Change != 0 || attrData.ChangeEx != 0)
                        {
                            var attrValueEx = GameUtils.AttributeValue(nAttrId, attrData.ValueEx);
                            var attrChange = GameUtils.AttributeValue(nAttrId, attrData.Change);
                            var attrChangeEx = GameUtils.AttributeValue(nAttrId, attrData.ChangeEx);
                            strDic = StrDic230034;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrChange, attrValueEx,
                                attrChangeEx);
                        }
                        else
                        {
                            var attrValueEx = GameUtils.AttributeValue(nAttrId, attrData.ValueEx);
                            strDic = StrDic230033;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrValueEx);
                        }
                    }
                    else
                    {
                        if (attrData.Change != 0 || attrData.ChangeEx != 0)
                        {
                            var attrChange = GameUtils.AttributeValue(nAttrId, attrData.Change);
                            strDic = StrDic230032;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrChange);
                        }
                        else
                        {
                            strDic = StrDic230025;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue);
                        }
                    }
                }
                else
                {
                    DataModel.BaseAttrStr[i] = "";
                }
            }


            strDic = StrDic230025;
            for (var i = 0; i != 2; ++i)
            {
                var nAttrId = tbEquip.BaseFixedAttrId[i];
                if (nAttrId != -1 && itemData.Exdata.Count >= 35)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    var v = itemData.Exdata[33 + i];
                    var attrValue = GameUtils.AttributeValue(nAttrId, v);
                    DataModel.AddAttrStr[i] = string.Format(strDic, attrName, attrValue);
                }
                else
                {
                    DataModel.AddAttrStr[i] = "";
                }
            }          

            strDic = StrDic230025;

            for (var i = 0; i < 4; i++)
            {
                var nAttrId = tbEquip.ExcellentAttrId[i];
                var nAttrValue = itemData.Exdata[i + 2];
                if (nAttrId != -1 && nAttrValue != 0)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    var attrValue = GameUtils.AttributeValue(nAttrId, nAttrValue);
                    DataModel.ExcellentAttrStr[i] = string.Format(strDic, attrName, attrValue);
                }
                else
                {
                    DataModel.ExcellentAttrStr[i] = "";
                }
            }

            for (var i = 0; i < 6; i++)
            {
                var nAttrId = itemData.Exdata[i + 6];
                var nAttrValue = itemData.Exdata[i + 12];
                if (nAttrId != -1 && nAttrValue != 0)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    //var attrValue = GameUtils.AttributeValue(nAttrId, nAttrValue*100);
                    var attrValue = GameUtils.AttributeValue(nAttrId, nAttrValue);
                    DataModel.SupperAttrStr[i] = string.Format(strDic, attrName, attrValue);
                }
                else
                {
                    DataModel.SupperAttrStr[i] = "";
                }
            }
        }

        private void GetBeReplacedEquip(BagItemDataModel bagItem, ref int bagId, ref int bagIndex)
        {
            var itemId = bagItem.ItemId;
            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
            {
                return;
            }

            var equipId = tbItem.Exdata[0];
            var tbEquip = Table.GetEquipBase(equipId);
            if (tbEquip == null)
            {
                return;
            }

            var ret = PlayerDataManager.Instance.CheckItemEquip(tbItem, tbEquip);
            if (ret == eEquipLimit.Occupation)
            {
                var e1 = new ShowUIHintBoard(210106);
                EventDispatcher.Instance.DispatchEvent(e1);
                return;
            }
            if (ret == eEquipLimit.Reborn)
            {
                if (1 == tbEquip.NeedRebornLevel)
                {
                    var e1 = new ShowUIHintBoard(100003302);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
                else if (2 == tbEquip.NeedRebornLevel)
                {
                    var e1 = new ShowUIHintBoard(100003303);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
                else if (3 == tbEquip.NeedRebornLevel)
                {
                    var e1 = new ShowUIHintBoard(100003304);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
                else if (4 == tbEquip.NeedRebornLevel)
                {
                    var e1 = new ShowUIHintBoard(100003305);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
                return;
            }
            if (ret == eEquipLimit.Attribute)
            {
                var e1 = new ShowUIHintBoard(210107);
                EventDispatcher.Instance.DispatchEvent(e1);
                return;
            }
            if (ret == eEquipLimit.Level)
            {
                var e1 = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(220307), tbItem.UseLevel));
                EventDispatcher.Instance.DispatchEvent(e1);
                return;
            }

            if (GameUtils.IsCanEquip(tbEquip, (int)eBagType.Equip07))
            {
                bagId = (int)eBagType.Equip07;
                if (bagIndex == -1)
                {
                    if (PlayerDataManager.Instance.GetEquipData(eEquipType.RingL).ItemId == -1)
                    {
                        bagIndex = 0;
                    }
                    else if (PlayerDataManager.Instance.GetEquipData(eEquipType.RingR).ItemId == -1)
                    {
                        bagIndex = 1;
                    }
                    else
                    {
                        var playerData = PlayerDataManager.Instance;
                        var equipedEquips = playerData.FindEquipedEquips(itemId);
                        bagIndex = playerData.FindWorstEquipIndex(equipedEquips);
                    }
                }
            }
            else if (GameUtils.IsCanEquip(tbEquip, (int)eBagType.Equip11) &&
                     GameUtils.IsCanEquip(tbEquip, (int)eBagType.Equip12))
            {
                if (bagIndex == -1)
                {
                    var LeftEquip = PlayerDataManager.Instance.GetEquipData(eEquipType.WeaponMain).ItemId;
                    if (LeftEquip == -1)
                    {
                        bagIndex = 0;
                    }
                    else
                    {
                        if (Table.GetItemBase(LeftEquip).Type == 10099)
                        {
                            bagIndex = 0;
                        }
                        else if (PlayerDataManager.Instance.GetEquipData(eEquipType.WeaponScend).ItemId == -1)
                        {
                            bagIndex = 1;
                        }
                        else
                        {
                            bagIndex = 0;
                        }
                    }
                }
                bagId = (int)eBagType.Equip11 + bagIndex;
                bagIndex = 0;
            }
            else
            {
                bagIndex = 0;
                for (var i = 7; i <= 18; i++)
                {
                    if (GameUtils.IsCanEquip(tbEquip, i) && i != (int)eBagType.Equip07)
                    {
                        bagId = i;
                        break;
                    }
                }
            }        
        }

        private void ReplaceEquip(BagItemDataModel bagItem, int bagIndex = -1)
        {
            if (bagItem == null || GameUtils.IsTrialEnd(bagItem))
            {
                var e = new ShowUIHintBoard(GameUtils.GetDictionaryText(10224));
                EventDispatcher.Instance.DispatchEvent(e);
                return;
            }
            var bagId = -1;
            GetBeReplacedEquip(bagItem, ref bagId, ref bagIndex);

            EventDispatcher.Instance.DispatchEvent(new EquipChangeEvent(bagItem));

            NetManager.Instance.StartCoroutine(ReplaceEquipCoroutine(bagItem, bagId, bagIndex));
        }
        private static IEnumerator ReplaceEquipCoroutine(BagItemDataModel BagData, int nPart, int nIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ReplaceEquip(BagData.Index, nPart, nIndex);
                yield return msg.SendAndWaitUntilDone();

                //Logger.Error("ReplaceEquipCoroutine............State={0},ErrorCode={1}", msg.State, msg.ErrorCode);
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode != (int)ErrorCodes.OK)
                    {
                        if (msg.ErrorCode == (int)ErrorCodes.Error_LevelNoEnough)
                        {
                            //装备等级不符
                            UIManager.Instance.ShowMessage(MessageBoxType.Ok, 270051);
                        }
                        else if (msg.ErrorCode == (int)ErrorCodes.Error_RebornLevelNotEnough)
                        {
                            //转生等级不符
                            var tbEquipBase = Table.GetEquipBase(BagData.ItemId);
                            if (null != tbEquipBase)
                            {
                                if (1 == tbEquipBase.NeedRebornLevel)
                                {
                                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100003302);
                                }
                                else if (2 == tbEquipBase.NeedRebornLevel)
                                {
                                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100003303);
                                }
                                else if (3 == tbEquipBase.NeedRebornLevel)
                                {
                                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100003304);
                                }
                                else if (4 == tbEquipBase.NeedRebornLevel)
                                {
                                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100003305);
                                }
                            }
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    Logger.Error("ReplaceEquipCoroutine............State..." + msg.State);
                }
            }
        }

        private void ResetItemDataModel(BagItemDataModel ItemDataModel, EquipBaseDataModel EquipDataModel)
        {
            EquipDataModel.ItemId = ItemDataModel.ItemId;
        
            //读表
            var tbItem = Table.GetItemBase(ItemDataModel.ItemId);
            if (tbItem == null)
            {
                return;
            }
            var tbEquip = Table.GetEquipBase(tbItem.Exdata[0]);
            if (tbEquip == null)
            {
                return;
            }

            DataModel.BuffLevel = 1;
            if (ItemDataModel.Exdata.Count > (int)EquipExdataDefine.SkillLevel)
            {
                DataModel.BuffLevel = ItemDataModel.Exdata[(int)EquipExdataDefine.SkillLevel];
            }

            EquipDataModel.BuffId = -1;
            if (ItemDataModel.Exdata.Count > (int)EquipExdataDefine.BuffId)
            {
                EquipDataModel.BuffId = ItemDataModel.Exdata[(int)EquipExdataDefine.BuffId];
            }
            else
            {
                EquipDataModel.BuffId = tbEquip.BuffGroupId;
            }
            EquipDataModel.EquipId = tbEquip.Id;
            if (PlayerDataManager.Instance.GetLevel() < tbItem.UseLevel)
            {
                EquipDataModel.CanUseLevel = 1;
            }
            else
            {
                EquipDataModel.CanUseLevel = 0;
            }
            EquipDataModel.EnchanceLevel = ItemDataModel.Exdata[0];

            var dur = ItemDataModel.Exdata.Durability;
            if (dur < 0)
            {
                dur = 0;
            }
            EquipDataModel.Durability = dur;

            if (EquipDataModel.Durability == 0)
            {
//红
                EquipDataModel.DurabilityColor = new Color(252.0f/255.0f, 55.0f/255.0f, 55.0f/255.0f);
            }
            else if (tbEquip.Durability >= EquipDataModel.Durability*10)
            {
//黄
                EquipDataModel.DurabilityColor = new Color(255.0f/255.0f, 220.0f/255.0f, 122.0f/255.0f);
            }
            else
            {
//绿
                EquipDataModel.DurabilityColor = new Color(173/255.0f, 255/255.0f, 0);
            }

            //职业符合不？
            if (tbEquip.Occupation != -1)
            {
                if (PlayerDataManager.Instance.GetRoleId() == tbEquip.Occupation && 
                    PlayerDataManager.Instance.PlayerDataModel.Attributes.Resurrection >= tbEquip.NeedRebornLevel)
                {
                    EquipDataModel.CanRole = 0;
                }
                else
                {
                    EquipDataModel.CanRole = 1;
                }
            }

            if (ItemDataModel.Exdata.LeftTime >= 0)
            {
                var deltaTime = ItemDataModel.Exdata.TrialEndTimeSeconds - Game.Instance.ServerTime.GetTimeStampSeconds();
                if (deltaTime < 0)
                {
                    DataModel.LeftTime = 0;
                }
                else
                {
                    DataModel.LeftTime = (int)Math.Ceiling(deltaTime / 60.0f);
                }
            }
            else
            {
                DataModel.LeftTime = -1;
            }

            //套装相关
            for (var i = 0; i < 10; i++)
            {
                EquipDataModel.TieCount[i] = 0;
            }
            EquipDataModel.TieId = tbEquip.TieId;
            var nNowTieCount = 0;
            for (var i = 0; i != 4; ++i)
            {
                EquipDataModel.TieAttrCount[i] = 0;
            }
            if (tbEquip.TieId == -1)
            {
                return;
            }
            var tbTie = Table.GetEquipTie(tbEquip.TieId);
            if (tbTie == null)
            {
                return;
            }

            PlayerDataManager.Instance.ForeachEquip(item =>
            {
                var ItemId = item.ItemId;
                if (ItemId == -1)
                {
                    return;
                }
                var tbTieItem = Table.GetItemBase(ItemId);
                if (tbTieItem == null)
                {
                    return;
                }
                var tbTieEquip = Table.GetEquipBase(tbTieItem.Exdata[0]);
                if (tbTieEquip == null)
                {
                    return;
                }
                if (tbEquip.TieId == tbTieEquip.TieId)
                {
                    EquipDataModel.TieCount[tbTieEquip.TieIndex] = 1;
                }
            });

            for (var i = 0; i < EquipDataModel.TieCount.Count; i++)
            {
                if (EquipDataModel.TieCount[i] == 1)
                {
                    nNowTieCount++;
                }
            }

            EquipDataModel.TieNowCount = nNowTieCount;
            for (var i = 0; i != 4; ++i)
            {
                if (nNowTieCount >= tbTie.NeedCount[i])
                {
                    EquipDataModel.TieAttrCount[i] = 1;
                }
            }
        }

        //sell coroutine
        private IEnumerator SellNewItemCoroutine(int bagIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var TempEquipList = new Int32Array();
                TempEquipList.Items.Add(bagIndex);
                var msg = NetManager.Instance.RecoveryEquip(0, TempEquipList);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipComPareUI));
                        if (TempEquipList.Items.Count > 0)
                        {
                            var item = PlayerDataManager.Instance.GetItem((int)eBagType.Equip, bagIndex);
                            if (item != null)
                            {
                                PlatformHelper.UMEvent("EquipSell", item.ItemId.ToString());
                            }
                        }
                    }
                    else
                    {
                        Logger.Error("Recycle OK Error!............ErrorCode..." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("Recycle OK Error!............State..." + msg.State);
                }
            }
        }

        private List<AttributeBaseDataModel> StatisticEquipAttribute(Dictionary<int, int> attributes,
            BagItemDataModel equipData,
            BagItemDataModel equipData1 = null)
        {
            var itemAttr = PlayerDataManager.Instance.GetEquipAttributeFix(equipData, mCharacterLevel,false);
            if (equipData1 != null)
            {
                var itemAttr1 = PlayerDataManager.Instance.GetEquipAttributeFix(equipData1, mCharacterLevel);
                {
                    // foreach(var i in itemAttr1)
                    var __enumerator1 = (itemAttr1).GetEnumerator();
                    while (__enumerator1.MoveNext())
                    {
                        var i = __enumerator1.Current;
                        {
                            itemAttr.modifyValue(i.Key, i.Value);
                        }
                    }
                }
            }
            var roleId = PlayerDataManager.Instance.GetRoleId();

            var newAttrChg = new ObservableCollection<AttributeBaseDataModel>();
            {
                // foreach(var i in attributes)
                var __enumerator2 = (attributes).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var i = __enumerator2.Current;
                    {
                        if ((roleId == 0 || roleId == 2) && (i.Key == 7 || i.Key == 8))
                        {
                            continue;
                        }
                        if (roleId == 1 && (i.Key == 5 || i.Key == 6))
                        {
                            continue;
                        }
                        var find = false;
                        {
                            // foreach(var j in itemAttr)
                            var __enumerator6 = (itemAttr).GetEnumerator();
                            while (__enumerator6.MoveNext())
                            {
                                var j = __enumerator6.Current;
                                {
                                    if (i.Key == j.Key)
                                    {
                                        find = true;
                                        if (j.Value - i.Value != 0)
                                        {
                                            var info = new AttributeBaseDataModel();
                                            info.Type = j.Key;
                                            info.Value = GameUtils.EquipAttrValueRefEx(j.Key, i.Value - j.Value);
                                            //info.Value = i.Value - j.Value;
                                            newAttrChg.Add(info);
                                        }
                                    }
                                }
                            }
                        }
                        if (!find)
                        {
                            var info = new AttributeBaseDataModel();
                            info.Type = i.Key;
                            info.Value = GameUtils.EquipAttrValueRefEx(i.Key, i.Value);
                            newAttrChg.Add(info);
                        }
                    }
                }
            }
            {
                // foreach(var i in itemAttr)
                var __enumerator3 = (itemAttr).GetEnumerator();
                while (__enumerator3.MoveNext())
                {
                    var i = __enumerator3.Current;
                    {
                        if ((roleId == 0 || roleId == 2) && (i.Key == 7 || i.Key == 8))
                        {
                            continue;
                        }
                        if (roleId == 1 && (i.Key == 5 || i.Key == 6))
                        {
                            continue;
                        }
                        var find = false;
                        {
                            // foreach(var j in attributes)
                            var __enumerator7 = (attributes).GetEnumerator();
                            while (__enumerator7.MoveNext())
                            {
                                var j = __enumerator7.Current;
                                {
                                    if (i.Key == j.Key)
                                    {
                                        find = true;
                                    }
                                }
                            }
                        }
                        if (!find)
                        {
                            var info = new AttributeBaseDataModel();
                            info.Type = i.Key;
                            info.Value = GameUtils.EquipAttrValueRefEx(i.Key, -i.Value);
                            //info.Value = -i.Value;
                            newAttrChg.Add(info);
                        }
                    }
                }
            }

            var newOrderAttrChg = newAttrChg.Where(info => info.Value > 0).ToList();
            newOrderAttrChg.AddRange(newAttrChg.Where(info => info.Value < 0));

            return newOrderAttrChg;
        }

        public void CleanUp()
        {
            DataModel = new EquipCompareDataModel();
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if(name == "ReplaceEquip")
            {
                if (param.Length >= 2)
                {
                    ReplaceEquip(param[0] as BagItemDataModel, (int)param[1]);
                }
                else
                {
                    ReplaceEquip(param[0] as BagItemDataModel);
                }
            }
            return null;
        }

        public void OnShow()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as EquipCompareArguments;
            if (args == null)
            {
                return;
            }

            nowItem = args.Data;
            if (nowItem == null)
            {
                return;
            }
            ResType = args.ResourceType;
            if (mIsInit)
            {
                InitStr();
            }
            mCharacterLevel = args.CharLevel != -1 ? args.CharLevel : PlayerDataManager.Instance.GetLevel();

            DataModel.ItemData = nowItem;     
           
            RefreshItemDataModel();
            ResetItemDataModel(nowItem, DataModel.look);
            DataModel.look.FightPoint = PlayerDataManager.Instance.GetBagItemFightPoint(nowItem, mCharacterLevel);

            var tbItem = Table.GetItemBase(nowItem.ItemId);
            if (tbItem == null)
            {
                return;
            }

            DataModel.ShowSellLable = true;
            if (tbItem.Sell <= 0 && tbItem.CallBackPrice <= 0)
            {
                DataModel.ShowSellLable = false;
            }

            var tbEquip = Table.GetEquipBase(tbItem.Exdata[0]);
            if (tbEquip == null)
            {
                return;
            }
            //获得装备点
            for (var i = 0; i != 2; ++i)
            {
                DataModel.CompareBagId[i] = -1;
                DataModel.CompareBagIndex[i] = -1;
            }
            var indexCompare = 0;

            DataModel.ShowType = (int) args.ShowType;
            DataModel.IsShowLevelUp = false;
            if (DataModel.ShowType == (int)eEquipBtnShow.EquipPack)
            {
                var tbItemBase = Table.GetItemBase(nowItem.ItemId);
                //if (tbItemBase.Type == 10001 || tbItemBase.Type == 10006)
                //{
                //    DataModel.IsShowLevelUp = true;
                //}
            }

            if (DataModel.ShowType == (int) eEquipBtnShow.BagPack
                || DataModel.ShowType == (int) eEquipBtnShow.OperateBag
                || DataModel.ShowType == (int)eEquipBtnShow.Donate
                || DataModel.ShowType == (int)eEquipBtnShow.TakeOut
                )
            {
                RefreshEquiped(nowItem);
            }
            if (DataModel.look.CanRole == 0)
            {
                DataModel.CareerDesc = "[ADFF00]" + GameUtils.GetDictionaryText(230005) + "[-]";
            }
            else
            {
                DataModel.CareerDesc = "[FC3737]" + GameUtils.GetDictionaryText(230005) + "[-]";
            }
            if (PlayerDataManager.Instance.NoticeData.GongGaoMaYaWeapon)
            {
                EquipInFo(nowItem.ItemId);
                PlayerDataManager.Instance.NoticeData.GongGaoMaYaWeapon = false;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void Close()
        {
        }

        public FrameState State { get; set; }
       
        private void EquipInFo(int itemId)
        {
            var equipData = Table.GetEquipBase(itemId);
            if (equipData == null) return;
            var buff = Table.GetBuffGroup(equipData.BuffGroupId);
            if (buff == null) return;
            if (buff.BuffID.Count == 1)
            {
                DataModel.look.BuffId = buff.BuffID[0];
            }
            else
            {
                DataModel.look.BuffId = 8999;
            }
            DataModel.BuffLevel = equipData.AddBuffSkillLevel;
            //if (equipData.ZhuoYueDescId != -1)
            //{
            //    var i = 0;
            //    var str = GameUtils.GetDictionaryText(equipData.ZhuoYueDescId);
            //    string[] subs = str.Split('\n');
            //    foreach (var item in subs)
            //    {
            //        DataModel.ExcellentAttrStr[i] = item;
            //        i++;
            //    }
            //}

            var strDic = StrDic230025;
            var itemData = DataModel.ItemData;
            for (var i = 0; i < 4; i++)
            {
                //因为实际玩家没武器数据，所以直接找到属性值赋值
                var arrID = 0;
                if (i == 0)
                {
                    arrID = 4;
                }
                else if (i == 1)
                {
                    arrID = 0;
                }
                else if (i == 2)
                {
                    arrID = 2;
                }
                else if (i == 3)
                {
                    arrID = 13;
                }

                var nAttrId = equipData.ExcellentAttrId[i];
                var num = Table.GetEquipEnchant(equipData.ExcellentAttrValue);
                if (num == null) return;
                double excell = (double)equipData.ExcellentValueMin / (double)equipData.ExcellentValueMax;
                var excellNum = Math.Round(excell, 2);
                var attrValue = num.Attr[arrID] * excellNum;
                
                if (nAttrId != -1 && attrValue != 0)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                   
                    DataModel.ExcellentAttrStr[i] = string.Format(strDic, attrName, attrValue);
                }
                else
                {
                    DataModel.ExcellentAttrStr[i] = "";
                }
            }

            if (equipData.LingHunDescId != -1)
            {
                var i = 0;
                var str = GameUtils.GetDictionaryText(equipData.LingHunDescId);
                string[] subs = str.Split('\n');
                foreach (var item in subs)
                {
                    DataModel.SupperAttrStr[i] = item;
                    i++;
                }
            }
            DataModel.LeftTime = -1;
            DataModel.look.Durability = equipData.Durability;
            DataModel.look.DurabilityColor = new Color(173 / 255.0f, 255 / 255.0f, 0);
        }
    }
}