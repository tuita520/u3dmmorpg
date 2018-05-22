#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class EquipUIController : IControllerBase
    {
        private static BagItemDataModel mEmptyBagItem = new BagItemDataModel();
        private List<bool> EquipExcellMaxList = new List<bool>();
        private List<bool> SuperExcellentCheckBoxList = new List<bool>();
        public EquipUIController()
        {
            //mEquipPackController = UIManager.Instance.CreateControllerBase(UIConfig.EquipPack);
            mEquipPackController = UIManager.Instance.GetController(UIConfig.EquipPack);
            mSmithyFrameController = UIManager.Instance.GetController(UIConfig.SmithyUI);
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInit);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnRefrehEquipBagItemStatus);
            EventDispatcher.Instance.AddEventListener(EquipOperateClick.EVENT_TYPE, OnClickEquipOperate);
            EventDispatcher.Instance.AddEventListener(VipLevelChangedEvent.EVENT_TYPE, OnVipLevelChanged);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemChanged);
            EventDispatcher.Instance.AddEventListener(UIEvent_SpecialItemShowEvent.EVENT_TYPE, OnSpecialItemShow);
            EventDispatcher.Instance.AddEventListener(UIEvent_EquipShengJie.EVENT_TYPE, OnClickShengJie);
            EventDispatcher.Instance.AddEventListener(warrantSelectCanaclEvent.EVENT_TYPE, WarrantSelectCanacl);
            EventDispatcher.Instance.AddEventListener(AddAndSubGodBlessEvent.EVENT_TYPE, AddAndSubGodBless);
            EventDispatcher.Instance.AddEventListener(RefGodBlessMaxEvent.EVENT_TYPE, RefGodBlessMax);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChanged);
        }

        //当前选择的物品数据
        private BagItemDataModel mBagItemData;
        //装备筛选界面右侧的相关数据类型
        private IControllerBase mEquipPackController;
        private IControllerBase mSmithyFrameController;
        private int mLastType = -1;

        private Coroutine m_RefineCoroutine;

        private EquipUIDataModel DataModel { get; set; }

        private TotalCount totalCount { get; set; }

        private EquipPackDataModel EquipPackDataModel
        {
            get { return mEquipPackController.GetDataModel("") as EquipPackDataModel; }
        }

        //追加网络包逻辑
        private IEnumerator AppendEquipCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {              
                var appendData = DataModel.EquipAppendData;
                var itemData = appendData.AppendItem;
                var msg = NetManager.Instance.AppendEquip(itemData.BagId, itemData.Index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var e = new EquipUiNotifyLogic(3);
                        EventDispatcher.Instance.DispatchEvent(e);
                        //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220006));
                        itemData.Exdata[1] = msg.Response;
                        itemData.Exdata[25] += DataModel.EquipAppendData.CostItemCount;
                        RefreshAppend(itemData);

                        if (DataModel.isAutoRefine)
                        {
                            OnEquipAppendAuto(1.3f);                       
                        }
                        if (itemData.Exdata.Binding != 1)
                        {
                            itemData.Exdata.Binding = 1;
                        }
                        RefreshEquipBagStatus(itemData);              
                        var tbItemBase = Table.GetItemBase(itemData.ItemId);
                        var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
                        PlatformHelper.UMEvent("EquipJingLian", itemData.BagId.ToString(), msg.Response + "/" + tbEquip.AddAttrMaxValue);               
                 
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                    {
                        DataModel.isAutoRefine = false;
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.DiamondNotEnough)
                    {
                        DataModel.isAutoRefine = false;
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                    {
                        DataModel.isAutoRefine = false;
                        var tbAppdend = Table.GetEquipAdditional1(appendData.AppendId);
                        if (null == tbAppdend)
                        {
                            yield break;
                        }
                        if (!GameUtils.CheckEnoughItems(tbAppdend.MaterialID, appendData.CostItemCount))
                        {
                            DataModel.isAutoRefine = false;
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                            yield break;
                        }
                    }
                    else
                    {
                        DataModel.isAutoRefine = false;
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("AppendEquip..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    DataModel.isAutoRefine = false;
                    Logger.Debug("AppendEquip..................." + msg.State);
                }
            }
        }

        //检查追加的条件
        private bool CheckInheritAppend()
        {
            var inheritData = DataModel.EquipInheritData;
            var inheritItem = inheritData.InheritItem;
            var inheritedItem = inheritData.InheritedItem;
            var appendValue1 = inheritItem.Exdata[1];
            var appendValue2 = inheritedItem.Exdata[1];
            var tbEquipBase = Table.GetEquipBase(inheritData.InheritedItem.ItemId);
            if (appendValue2 == tbEquipBase.AddAttrMaxValue)
            {
                //被传承装备追加属性已经达到上限，无需传承
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270088));
                return false;
            }
            var tbToEquip = Table.GetItemBase(inheritData.InheritedItem.ItemId);
            var tbFromEquip = Table.GetItemBase(inheritData.InheritItem.ItemId);
            if (tbToEquip.Type == tbFromEquip.Type)
            {
                if (appendValue1 <= appendValue2)
                {
                    //被传承装备追加属性已经大于等于传承装备追加属性
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270087));
                    return false;
                }
            }
            if (appendValue1 > tbEquipBase.AddAttrMaxValue)
            {
                //被传承装备追加属性上限低于传承装备追加属性，是否继续
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270090, "",
                    () => { SmritiEquipConfirm(); });
                return false;
            }
            return true;
        }

        //检查强化的条件
        private bool CheckInheritEnchance()
        {
            var inheritData = DataModel.EquipInheritData;
            var inheritItem = inheritData.InheritItem;
            var inheritedItem = inheritData.InheritedItem;
            var tbItemBase = Table.GetItemBase(inheritedItem.ItemId);
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);

            if (inheritedItem.Exdata[0] == tbEquip.MaxLevel)
            {
                //装备达到最大的强化等级
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270085));
                return false;
            }
            if (inheritedItem.Exdata[0] >= inheritItem.Exdata[0])
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220009));
                return false;
            }
            if (inheritItem.Exdata[0] > tbEquip.MaxLevel)
            {
                //继承装备强化等级已经大于等于传承装备强化等级，无需传承
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270086, "", () => { SmritiEquipConfirm(); });
                return false;
            }
            return true;
        }

        //检查洗练的条件
        private bool CheckInheritExcellent()
        {
            var inheritData = DataModel.EquipInheritData;
            var inheritItem = inheritData.InheritItem;
            var inheritedItem = inheritData.InheritedItem;
            var tbItemBase = Table.GetItemBase(inheritedItem.ItemId);
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);

            var range = tbEquip.ExcellentAttrValue;
            var tbEnchant = Table.GetEquipEnchant(range);
            var maxRate = tbEquip.ExcellentValueMax;

            var isAllSamll = true;
            for (var i = 0; i < 4; i++)
            {
                if (inheritedItem.Exdata[2 + i] < inheritItem.Exdata[2 + i])
                {
                    isAllSamll = false;
                    break;
                }
            }
            if (isAllSamll)
            {
                //继承装备卓越属性已经大于等于传承装备卓越属性，无需传承
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270091));
                return false;
            }

            var isOneSamll = false;
            for (var i = 0; i < 4; i++)
            {
                if (inheritedItem.Exdata[2 + i] > inheritItem.Exdata[2 + i])
                {
                    isOneSamll = true;
                    break;
                }
            }
            if (isOneSamll)
            {
                //继承装备有洗炼属性高于传承装备，可能导致战斗力下降，是否继续？
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270092, "",
                    () => { SmritiEquipConfirm(); });
                return false;
            }

            var isLimit = false;
            for (var i = 0; i < 4; i++)
            {
                var attrid = tbEquip.ExcellentAttrId[i];
                var index = GameUtils.GetAttrIndex(attrid);
                if (index != -1 && inheritItem.Exdata[2 + i] < tbEnchant.Attr[index]*maxRate/100)
                {
                    isLimit = true;
                    break;
                }
            }
            if (isLimit)
            {
                //继承装备追加属性上限低于传承装备追加属性，是否继续？
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 220011, "",
                    () => { SmritiEquipConfirm(); });
                return false;
            }

            return true;
        }

        //强化网络包处理
        private IEnumerator EnchanceEquipCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var enchangeData = DataModel.EquipEnchanceData;
                var itemData = enchangeData.EnchanceItem;

                var msg = NetManager.Instance.EnchanceEquip(itemData.BagId, itemData.Index,
                    Convert.ToInt32(enchangeData.IsSpecialItem), Convert.ToInt32(enchangeData.IsSuccessRate), Convert.ToInt32(DataModel.EquipEnchanceData.AddAndSubGodBless));
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        if (itemData.Exdata[0] < msg.Response)
                        {
                            //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220002));
                            var e = new EquipUiNotifyLogic(1);
                            EventDispatcher.Instance.DispatchEvent(e);
                            PlatformHelper.UMEvent("EquipEnchance", itemData.BagId.ToString(), (itemData.Exdata[0] + 1) + "|1");
                        }
                        else
                        {
                            //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220003));
                            var e = new EquipUiNotifyLogic(2);
                            EventDispatcher.Instance.DispatchEvent(e);
                            PlatformHelper.UMEvent("EquipEnchance", itemData.BagId.ToString(), (itemData.Exdata[0] - 1) + "|0");
                        }
                        
                        itemData.Exdata[0] = msg.Response;
                        if (itemData.Exdata.Binding != 1)
                        {
                            itemData.Exdata.Binding = 1;
                        }
                        RefreshEnchance(itemData, false);
                        RefreshEquipBagStatus(itemData);
                        isGoOn = true;
                        NetManager.Instance.StartCoroutine(RefreshSuccessLvCoroutine());
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.DiamondNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Error("msgSendFun..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("msgSendFun..................." + msg.State);
                }
            }
        }
        private bool isGoOn = false;
        private void RefreshSuccessLv()
        {
            if (DataModel.EquipEnchanceData.IsSpecialItem)
            {
                var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
                if (enchanceId != -1)
                {
                    var tbEnchance = Table.GetEquipBlessing(enchanceId);
                    if (tbEnchance == null) return;
                    var warrantCount = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count;                  
                    if (warrantCount < int.Parse(DataModel.EquipEnchanceData.AddAndSubGodBless))
                    {
                        DataModel.EquipEnchanceData.AddAndSubGodBless = warrantCount.ToString();
                    }
                    var warrantItemCount = int.Parse(DataModel.EquipEnchanceData.AddAndSubGodBless);
                    if (warrantItemCount >= tbEnchance.WarrantItemCount)
                    {
                        warrantItemCount = tbEnchance.WarrantItemCount;
                    }
                    var success = tbEnchance.Probability / 100 + (warrantItemCount * tbEnchance.MoreChance) / 100;
                    if (success >= 100)
                    {
                        success = 100;
                    }
                    DataModel.EquipEnchanceData.SuccessProbability = string.Format("{0}%", success);
                    if (isGoOn)
                    {
                        if (warrantNum >= PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count)
                        {
                            warrantNum = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count;
                            DataModel.EquipEnchanceData.AddAndSubGodBless = warrantNum.ToString();
                        }                        
                        isGoOn = false;
                    }     
                }
            }           
        }

        private IEnumerator RefreshSuccessLvCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            RefreshSuccessLv();
        }

        private int warrantNum = 0;
        private void AddAndSubGodBless(IEvent ievent)
        {
            var _e = ievent as AddAndSubGodBlessEvent;
            var warrantCount = 0;
            var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
            var dicID = 22007;
            if (enchanceId != -1)
            {
                var tbEnchance = Table.GetEquipBlessing(enchanceId);
                if (tbEnchance == null) return;
                dicID = tbEnchance.WarrantItemId;
                warrantCount = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count;
            }

            //0 减号、1 加号
            if (_e.Type == 0)
            {
                if (warrantNum <= 0)
                {
                    warrantNum = 0;                   
                }
                else
                {
                    warrantNum -= 1;
                }
                DataModel.EquipEnchanceData.AddAndSubGodBless = warrantNum.ToString();
            }
            else if (_e.Type == 1)
            {
                if (warrantNum >= warrantCount)
                {
                    var items = new Dictionary<int, int>();
                    items[dicID] = 1;
                    GameUtils.ShowQuickBuy(items);
                    warrantNum = warrantCount;
                }
                else
                {
                    warrantNum += 1;
                }
                DataModel.EquipEnchanceData.AddAndSubGodBless = warrantNum.ToString();
            }
            RefreshSuccessLv();
        }
        private void RefGodBlessMax(IEvent ievent)
        {
            var ieve = ievent as RefGodBlessMaxEvent;
            warrantNum = ieve.Type;
            DataModel.EquipEnchanceData.AddAndSubGodBless = warrantNum.ToString();

            if (DataModel.EquipEnchanceData.IsSpecialItem)
            {
                var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
                if (enchanceId != -1)
                {
                    var tbEnchance = Table.GetEquipBlessing(enchanceId);
                    if (tbEnchance == null) return;         
                    var warrantItemCount = int.Parse(DataModel.EquipEnchanceData.AddAndSubGodBless);
                    if (warrantItemCount >= tbEnchance.WarrantItemCount)
                    {
                        warrantItemCount = tbEnchance.WarrantItemCount;
                    }
                    var success = tbEnchance.Probability / 100 + (warrantItemCount * tbEnchance.MoreChance) / 100;
                    if (success >= 100)
                    {
                        success = 100;
                    }
                    DataModel.EquipEnchanceData.SuccessProbability = string.Format("{0}%", success);
                }
            }
        }
        //洗练网络包逻辑
        private IEnumerator ExcellentResetEquipCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var itemData = DataModel.EquipExcellentRestData.ExcellentItem;
                var msg = NetManager.Instance.ResetExcellentEquip(itemData.BagId, itemData.Index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220007));
                        EventDispatcher.Instance.DispatchEvent(new ExcellentEquipEvent());
                        for (var i = 0; i < 4; i++)
                        {
                            itemData.Exdata[18 + i] = msg.Response.Items[i];
                        }
                        RefreshExcelletReset(itemData);
                        if (itemData.Exdata.Binding != 1)
                        {
                            itemData.Exdata.Binding = 1;
                        }

                        PlatformHelper.UMEvent("EquipXiLian", itemData.BagId.ToString());
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("ExcellentResetEquip..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("ExcellentResetEquip..................." + msg.State);
                }
            }
        }

        //洗练结果网络包逻辑
        private IEnumerator ExcellentResetOkCoroutine(int ret)
        {
            using (new BlockingLayerHelper(0))
            {
                var itemData = DataModel.EquipExcellentRestData.ExcellentItem;
                var msg = NetManager.Instance.ConfirmResetExcellentEquip(itemData.BagId, itemData.Index, ret);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {					
                        if (ret == 1)
                        {
                            for (var i = 0; i < 4; i++)
                            {
                                if (itemData.Exdata[18 + i] != -1)
                                {
                                    itemData.Exdata[2 + i] = itemData.Exdata[18 + i];
                                    itemData.Exdata[18 + i] = -1;
                                }
                            }                        
                            var e = new EquipUiNotifyLogic(1);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else
                        {
                            for (var i = 0; i < 4; i++)
                            {
                                itemData.Exdata[18 + i] = -1;
                            }                      
                        }
                        RefreshExcelletReset(itemData);

                        RefreshEquipBagStatus(itemData);
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.DiamondNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("ExcellentResetOK..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("ExcellentResetOK..................." + msg.State);
                }
            }
        }

        private static int GetAdditionalTable1(EquipAdditional1Record tbAdditional, int Value)
        {
            var tbskillup = Table.GetSkillUpgrading(tbAdditional.AddPropArea);
            var level = 0;
            var lValue = tbskillup.GetSkillUpgradingValue(level);
            while (lValue < Value)
            {
                level++;
                var newValue = tbskillup.GetSkillUpgradingValue(level);
                if (newValue == lValue)
                {
                    break;
                }
                lValue = newValue;
            }
            return level;
        }

        private bool IsShowAppendNotice(BagItemDataModel bagItem, int changeCount)
        {
            var itemId = bagItem.ItemId;
            if (itemId == -1)
            {
                return false;
            }

            var tbItemBase = Table.GetItemBase(itemId);
            if (tbItemBase == null)
            {
                return false;
            }
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            if (tbEquip == null)
            {
                return false;
            }

            var appendId = tbEquip.AddIndexID;
            if (appendId == -1)
            {
                return false;
            }
            var tbAppdend = Table.GetEquipAdditional1(appendId);
            if (tbAppdend == null)
            {
                return false;
            }

            var addAttrValue = bagItem.Exdata[1];
            var isMaxValue = (addAttrValue == tbEquip.AddAttrMaxValue);
            if (isMaxValue)
            {
                return false;
            }

            var addLevel = GetAdditionalTable1(tbAppdend, bagItem.Exdata[1]);
            var costItemCount = Table.GetSkillUpgrading(tbAppdend.MaterialCount).GetSkillUpgradingValue(addLevel);
            var costMoney = Table.GetSkillUpgrading(tbAppdend.Money).GetSkillUpgradingValue(addLevel);

            var money = PlayerDataManager.Instance.GetRes((int) eResourcesType.GoldRes);
            if (money <= costMoney)
            {
                return false;
            }

            var nowCount = PlayerDataManager.Instance.GetItemTotalCount(tbAppdend.MaterialID).Count;
            var beforeCount = nowCount - changeCount;

            if (beforeCount < costItemCount && nowCount >= costItemCount)
            {
                return true;
            }

            return false;
        }

        private void OnExDataInit(IEvent ievent)
        {
            var e = ievent as ExDataInitEvent;
            if (null == e)
            {
                return;
            }
            CheckRefineNotice();
        }
        private void OnBagItemChanged(IEvent ievent)
        {
            var e = ievent as UIEvent_BagItemCountChange;
            if (null == e)
            {
                return;
            }
            if (e.ItemId == 22003)
            {
                CheckRefineNotice();                
            }
            var tbAppdend = Table.GetEquipAdditional1(1);
            if (e != null && e.ItemId != tbAppdend.MaterialID)
            {
                return;
            }

            RefreshAppendNotice(e.ChangeCount);
        }
        private void OnResourceChanged(IEvent ievent)
        {
            var e = ievent as Resource_Change_Event;
            if (null == e)
            {
                return;
            }
            if (e.Type != eResourcesType.GoldRes)
            {
                return;
            }
            var MyMoney = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold;
            var enchangeData = DataModel.EquipEnchanceData;
            if (enchangeData.NeedMoney > MyMoney)
            {
                enchangeData.MoneyColor = Color.red;
            }
            else
            {
                enchangeData.MoneyColor = Color.white;
            }
        }
        private void CheckRefineNotice()
        {
            var hint = PlayerDataManager.Instance.CheckCondition(47);
            if (hint != 0)
            {
                PlayerDataManager.Instance.WeakNoticeData.Additional = false;
            }
            else
            {
                var RefineGemResCount = PlayerDataManager.Instance.GetItemTotalCount(22003);
                if (RefineGemResCount.Count >= DataModel.RefineGemNoticeCount)
                {
                    PlayerDataManager.Instance.WeakNoticeData.Additional = true;
                }
                else
                {
                    PlayerDataManager.Instance.WeakNoticeData.Additional = false;
                }
            }
        }

        private void OnSpecialItemShow(IEvent ievent)
        {
            totalCount.Count = 15;
            if (!DataModel.EquipEnchanceData.IsSpecialItem)
            {
                var enchance = DataModel.EquipEnchanceData.EnchanceId;
                if(enchance != -1)
                {
                    var pr = Table.GetEquipBlessing(enchance);
                    if (pr == null) return;
                    var successPR = pr.Probability / 100;
                    DataModel.EquipEnchanceData.SuccessProbability = string.Format("{0}%", successPR); 
                }          
                
                return;
            }
            var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
            if (enchanceId != -1)
            {
                var tbEnchance = Table.GetEquipBlessing(enchanceId);
                if (tbEnchance == null) return;
                var items = new Dictionary<int, int>();
                items[tbEnchance.WarrantItemId] = tbEnchance.WarrantItemCount;


                var warrantCount = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count;
                var warrantItemCount = int.Parse(DataModel.EquipEnchanceData.AddAndSubGodBless);
                if (warrantItemCount >= tbEnchance.WarrantItemCount)
                {
                    warrantItemCount = tbEnchance.WarrantItemCount;
                }
                var success = tbEnchance.Probability / 100 + (warrantItemCount * tbEnchance.MoreChance) / 100;
                if (success >= 100)
                {
                    success = 100;
                }
                DataModel.EquipEnchanceData.SuccessProbability = string.Format("{0}%", success);
                //装备强化使用神佑勾选时，神佑宝石大于0即可使用，不需要弹出
                if (warrantCount <= 0)
                {
                    if (!GameUtils.CheckEnoughItems(items, true))
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    }  
                }           
            }
            //DataModel.EquipEnchanceData.IsSpecialItemShow = !DataModel.EquipEnchanceData.IsSpecialItemShow; 
            //EventDispatcher.Instance.DispatchEvent(new EquipUiNotifyRefreshCoumuseList());
        }

        private void WarrantSelectCanacl(IEvent ievent)
        {
            if (DataModel.EquipEnchanceData == null) return;
            var enchanceId = DataModel.EquipEnchanceData.EnchanceId;      
            if (enchanceId != -1)
            {
                var tbEnchance = Table.GetEquipBlessing(enchanceId);
                if (tbEnchance == null) return;
                var warrantCount = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count;
                if (warrantCount <= 0)
                {
                    DataModel.EquipEnchanceData.IsSpecialItem = false;
                }
            }            
        }
        
        private bool CheckSmithyEvoItem()
        {
            var tbBase = Table.GetEquipBase(mBagItemData.ItemId);
            if (tbBase == null)
            {
                GameUtils.ShowHintTip(200002863);
                return false;
            }
            var tbUp = Table.GetEquipBase(tbBase.UpdateEquipID);
            if (tbUp == null)
            {
                GameUtils.ShowHintTip(200002863);
                return false;
            }

            var tbUplogic = Table.GetEquipUpdate(tbBase.EquipUpdateLogic);
            if (tbUplogic == null )
            {
                GameUtils.ShowHintTip(200002863);
                return false;
            }

            var neededItems = new List<ItemIdDataModel>();
            for (int i = 0, imax = tbUplogic.NeedItemID.Length; i < imax; i++)
            {
                if (tbUplogic.NeedItemID[i] == -1)
                {
                    break;
                }

                var needItem = new ItemIdDataModel();
                needItem.ItemId = tbUplogic.NeedItemID[i];
                needItem.Count = tbUplogic.NeedItemCount[i];
                neededItems.Add(needItem);
            }
            for (int i = 0, imax = tbUplogic.NeedResID.Length; i < imax; i++)
            {
                if (tbUplogic.NeedResID[i] == -1)
                {
                    continue;
                }
                var needItem = new ItemIdDataModel();
                needItem.ItemId = tbUplogic.NeedResID[i];
                needItem.Count = tbUplogic.NeedResCount[i];
                neededItems.Add(needItem);
//             if (needItem.ItemId == 10) //魔尘
//             {
//                 evoData.needMochengCount = needItem.Count;
//                 break;
//             }
            }
            for (var i = 0; i < neededItems.Count; i++)
            {
                if (neededItems[i].Count > PlayerDataManager.Instance.GetItemCount(neededItems[i].ItemId))
                {
                    GameUtils.ShowHintTip(300600);
                    return false;
                }
            }
            return true;
        }

        private void OnClickShengJie(IEvent ievent)
        {
            if (!CheckSmithyEvoItem())
            {
                return;
            }

            var evoItems = new List<BagItemDataModel>();

            //主装备
            evoItems.Add(mBagItemData);

            //材料装备
            var tbBase = Table.GetEquipBase(mBagItemData.ItemId);
            if (tbBase == null)
            {
                ResetShengJie();
                return;
            }
            var tbUplogic = Table.GetEquipUpdate(tbBase.EquipUpdateLogic);
            if (tbUplogic == null)
            {
                ResetShengJie();
                return;
            }

            int num = 0;
            var _bagItem = PlayerDataManager.Instance.GetBagItemByItemIdjingJie((int)eBagType.Equip, tbBase.EquipUpdateLogic);
            for (int i = 0, imax = tbUplogic.NeedEquipCount - 1; i < imax; i++)
            {
                if (_bagItem.Count <= num)
                {
                    GameUtils.ShowHintTip(300600);
                    return;
                }
                if (_bagItem[num] == null)
                {
                    continue;
                }

                var needItem = new BagItemDataModel();
                needItem.ItemId = mBagItemData.ItemId;
                needItem.Index = _bagItem[num].Index;
                evoItems.Add(needItem);
                num ++;
            }

            var param = new Int32Array();
            param.Items.Add(3);
            {
                foreach (var evoItem in evoItems)
                {
                    if (evoItem.ItemId != -1)
                    {
                        param.Items.Add(evoItem.BagId);
                        param.Items.Add(evoItem.Index);
                    }
                }
            }

            UseBuildService(param, AdvanceEquipManual);
        }

        private void UseBuildService(Int32Array param, Action onOk = null)
        {
            NetManager.Instance.StartCoroutine(UseBuildServiceCoroutine(param, onOk));
        }

        private void AdvanceEquipManual()
        {
            BagItemDataModel majorEquip;
            if (mBagItemData.BagId == (int)eBagType.Equip)
            {
                majorEquip = PlayerDataManager.Instance.GetItem(mBagItemData.BagId, mBagItemData.Index);
            }
            else
            {
                var equipType = PlayerDataManager.Instance.BagIdToEquipType[mBagItemData.BagId];
                majorEquip = PlayerDataManager.Instance.GetEquipData((eEquipType)equipType);
            }
            if (majorEquip != null && majorEquip.ItemId != -1)
            {
                var evoData = DataModel.EquipShengJieData.ShengJieResultItem;
//             evoData.EvoedEquip.Clone(evoData.EvolvedItem);
//             evoData.IsShowEvoedEquip = true;
                majorEquip.Clone(evoData);
            }

            //ClearEvoData();
        }


        private IEnumerator UseBuildServiceCoroutine(Int32Array param, Action onOk)
        {
            using (new BlockingLayerHelper(0))
            {
                var tbBuilding = Table.GetBuilding(80);

                var msg = NetManager.Instance.UseBuildService(8, tbBuilding.ServiceId, param);
                yield return msg.SendAndWaitUntilDone();

                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.GainItemHintUI));
                        if (param.Items[0] == 3) //装备进阶功能功能
                        {
                            var e = new EquipUiNotifyLogic(1);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        if (onOk != null)
                        {
                            onOk();
                        }
                    }
                    else
                    {
                        var e = new EquipUiNotifyLogic(2);
                        EventDispatcher.Instance.DispatchEvent(e);
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
        }

        private void OnChangeEquipCell(IEvent ievent)
        {
            //if (mLastType == 5)
            //{
            //    return;
            //}
        
            var e = ievent as EquipCellSelect;
            RefreshItemData(e.ItemData, e.Index);

            if (DataModel.EquipEnchanceData != null)
            {
                var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
                if (enchanceId != -1)
                {
                    var tbEnchance = Table.GetEquipBlessing(enchanceId);
                    if (tbEnchance == null) return;
                    DataModel.EquipEnchanceData.AddAndSubGodBless = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count.ToString();
                    warrantNum = int.Parse(DataModel.EquipEnchanceData.AddAndSubGodBless);
                }
            }
        }

        //--------------------------------------------------------------------Append------------------
        private void OnClickEquipAppend()
        {
            var playerData = PlayerDataManager.Instance.PlayerDataModel;
            var appendData = DataModel.EquipAppendData;
            var tbAppdend = Table.GetEquipAdditional1(appendData.AppendId);
            if (tbAppdend == null)
            {
                Logger.Error("OnClickEquipAppend  error :appendData.AppendId = {0}, ", appendData.AppendId);
                return;
            }
            if (appendData.IsMaxValue == 1)
            {
                DataModel.isAutoRefine = false;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220001));
                return;
            }

            if (appendData.CostMoney > playerData.Bags.Resources.Gold)
            {
                DataModel.isAutoRefine = false;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.GoldRes);
                return;
            }
            //if (appendData.CostItemCount > PlayerDataManager.Instance.GetItemCount(tbAppdend.MaterialID))
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
            //    PlayerDataManager.Instance.ShowItemInfoGet(tbAppdend.MaterialID);
            //    return;
            //}

            if (!GameUtils.CheckEnoughItems(tbAppdend.MaterialID, appendData.CostItemCount))
            {
                DataModel.isAutoRefine = false;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                return;
            }

            var tbItem = Table.GetItemBase(appendData.AppendItem.ItemId);
            if (tbItem == null)
            {
                DataModel.isAutoRefine = false;
                return;
            }
            if (tbItem.CanTrade == 1 && appendData.AppendItem.Exdata.Binding != 1)
            {
                DataModel.isAutoRefine = false;
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 210117, "",
                    () => { NetManager.Instance.StartCoroutine(AppendEquipCoroutine()); });
          
                return;
            }
            NetManager.Instance.StartCoroutine(AppendEquipCoroutine());      
        }

        private bool OnClickEquipAppendBoolAuto()
        {

            var playerData = PlayerDataManager.Instance.PlayerDataModel;
            var appendData = DataModel.EquipAppendData;
            var tbAppdend = Table.GetEquipAdditional1(appendData.AppendId);
            if (tbAppdend == null)
            {
                DataModel.isAutoRefine = false;
                Logger.Error("OnClickEquipAppend  error :appendData.AppendId = {0}, ", appendData.AppendId);
                return false;
            }
            if (appendData.IsMaxValue == 1)
            {
                DataModel.isAutoRefine = false;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220001));
                return false;
            }

            if (appendData.CostMoney > playerData.Bags.Resources.Gold)
            {
                DataModel.isAutoRefine = false;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.GoldRes);
                return false;
            }       
            if (!GameUtils.CheckEnoughItems(tbAppdend.MaterialID, appendData.CostItemCount))
            {
                DataModel.isAutoRefine = false;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                return false;
            }

            var tbItem = Table.GetItemBase(appendData.AppendItem.ItemId);
            if (tbItem == null)
            {
                DataModel.isAutoRefine = false;
                return false;
            }
            if (tbItem.CanTrade == 1 && appendData.AppendItem.Exdata.Binding != 1)
            {
                DataModel.isAutoRefine = false;
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 210117, "",
                    () => { NetManager.Instance.StartCoroutine(AppendEquipCoroutine()); });
                return false;
            }
        
            return true;      
        }
        //--------------------------------------------------------------------Enchance------------------
        private void OnClickEquipEnchance()
        {
            var playerData = PlayerDataManager.Instance.PlayerDataModel;
            var enchangeData = DataModel.EquipEnchanceData;
            var itemData = enchangeData.EnchanceItem;
            if (itemData.ItemId == -1)
            {
                //请选择物品
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270084));
                return;
            }
            var tbItem = Table.GetItemBase(itemData.ItemId);
            if (tbItem == null)
            {
                return;
            }
            if (enchangeData.IsMaxLevel == 1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220000));
                return;
            }

            var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
            if (enchanceId == -1)
            {
                enchanceId = itemData.Exdata[0];
            }
            var tbEnchance = Table.GetEquipBlessing(enchanceId);
            if (tbEnchance == null)
            {
                return;
            }

            var items = new Dictionary<int, int>();
            if (enchangeData.IsSpecialItem)
            {
                //现在是点击神佑宝石，有几个消耗几个
                var warrantCount = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count;
                if (warrantCount >= tbEnchance.WarrantItemCount || warrantCount <= 0)
                {
                    warrantCount = tbEnchance.WarrantItemCount;
                }
                items[tbEnchance.WarrantItemId] = warrantCount;//tbEnchance.WarrantItemCount;
                //if (tbEnchance.WarrantItemCount > PlayerDataManager.Instance.GetItemCount(tbEnchance.WarrantItemId))
                //{
                //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                //    PlayerDataManager.Instance.ShowItemInfoGet(tbEnchance.WarrantItemId);
                //    return;
                //}
            }

            if (tbEnchance.NeedMoney > playerData.Bags.Resources.Gold)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.GoldRes);
                return;
            }

            for (var i = 0; i < 3; i++)
            {
                if (tbEnchance.NeedItemId[i] != -1)
                {
                    items[tbEnchance.NeedItemId[i]] = tbEnchance.NeedItemCount[i];
                }
            }   
          
            if (!GameUtils.CheckEnoughItems(items, true))
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                //PlayerDataManager.Instance.ShowItemInfoGet(tbEnchance.NeedItemId[i]);
                return;
            }

            if (tbItem.CanTrade == 1 && itemData.Exdata.Binding != 1)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 210117, "",
                    () => { NetManager.Instance.StartCoroutine(EnchanceEquipCoroutine()); });
                return;
            }
            NetManager.Instance.StartCoroutine(EnchanceEquipCoroutine());
        }

        //--------------------------------------------------------------------Inherit------------------
        private void OnClickEquipInherit()
        {
            var inheritData = DataModel.EquipInheritData;
            if (inheritData.InheritItem.ItemId == -1
                || inheritData.InheritedItem.ItemId == -1)
            {
                return;
            }

            var tbFromEquip = Table.GetItemBase(inheritData.InheritItem.ItemId);
            var tbToEquip = Table.GetItemBase(inheritData.InheritedItem.ItemId);
            if (tbFromEquip == null || tbToEquip == null)
            {
                return;
            }

            if (!GameUtils.CheckInheritType(tbFromEquip, tbToEquip))
            {
                return;
            }

            if (inheritData.IsDiamond)
            {
                if (inheritData.CostDiamond > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Diamond)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.DiamondRes);
                    return;
                }
            }
            if (inheritData.IsGold)
            {
                if (inheritData.CostGold > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.GoldRes);
                    return;
                }
            }
            if (inheritData.IsEnchance && !CheckInheritEnchance())
            {
                return;
            }
            if (inheritData.IsAdd && !CheckInheritAppend())
            {
                return;
            }
            if (inheritData.IsExcellent && !CheckInheritExcellent())
            {
                return;
            }
            SmritiEquipConfirm();
        }

        private void OnClickEquipInheritedItem()
        {
            RefreshInheritedItem(mEmptyBagItem);
        }

        private void OnClickEquipInheritItem()
        {
            RefreshInheritItem(mEmptyBagItem);
        }

        private void OnClickEquipOperate(IEvent ievent)
        {
            var e = ievent as EquipOperateClick;
            switch (e.OperateType)
            {
                case 0:
                {
                    OnClickEquipEnchance();
                }
                    break;
                case 1:
                {
                    OnClickEquipAppendGo();
                }
                    break;
                case 20:
                {
                    OnClickExcellentReset();
                }
                    break;
                case 21:
                {
                    OnClickExcellentResetAffirm(1);
                }
                    break;
                case 22:
                {
                    OnClickExcellentResetAffirm(0);
                }
                    break;
                case 3:
                {
                    OnClickSuperExcellent();
                }
                    break;
                case 4:
                {
                    OnClickEquipInherit();
                }
                    break;
                case 5:
                    {
                        OnClickSuperExcellentOperate(1);
                    }
                    break;
                case 6:
                    {
                        OnClickSuperExcellentOperate(0);
                    }
                    break;
                case 41:
                {
                    OnClickEquipInheritItem();
                }
                    break;
                case 42:
                {
                    OnClickEquipInheritedItem();
                }
                    break;
                case 43:
                {
                    OnClickTips(true);
                }
                    break;
                case 44:
                {
                    OnClickTips(false);
                }
                    break;
                case 100:
                {
                    var count = DataModel.OperateTypes.Count;
                    for (var i = 0; i < count; i++)
                    {
                        DataModel.OperateTypes[i] = false;
                    }
                    if (e.Index >= 0 && e.Index < count)
                    {
                        DataModel.OperateTypes[e.Index] = true;
                    }
                    RefreshItemData(mBagItemData, -1, true);
                    DataModel.isAutoRefine = false;
                    var ee = new PlaySpriteAnimationEvent();
                    EventDispatcher.Instance.DispatchEvent(ee);
                }
                    break;
                case 45:
                {
                    OnClickEquipAppendAuto();
                
                }
                    break;
            }
        }

        //装备精炼
        private void OnClickEquipAppendGo()
        {
            if (DataModel.isAutoRefine)
            {
                DataModel.isAutoRefine = false;
            }
            OnClickEquipAppend();
        }

        //装备一键精炼
        private void OnClickEquipAppendAuto()
        {
            if (DataModel.isAutoRefine)
            {
            
                DataModel.isAutoRefine = false;
                return;
            }
            DataModel.isAutoRefine = true;
            OnEquipAppendAuto(0f);
        }

        private void OnEquipAppendAuto(float delay)
        {
            if (!OnClickEquipAppendBoolAuto())
            {
                DataModel.isAutoRefine = false;           
                return;
            }

            if (m_RefineCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(m_RefineCoroutine);
            }
       
            m_RefineCoroutine = NetManager.Instance.StartCoroutine(OnEquipAppendAutoCorourtine(delay));              
        }

        private IEnumerator OnEquipAppendAutoCorourtine(float delay)
        {
            yield return new WaitForSeconds(delay);      
            if (!DataModel.isAutoRefine)
            {
                yield break;
            }
            else
            {
                NetManager.Instance.StartCoroutine(AppendEquipCoroutine());
            }        
        }
        //--------------------------------------------------------------------Excellent------------------
        private void OnClickExcellentReset()
        {
            var playerData = PlayerDataManager.Instance.PlayerDataModel;
            var excellentReset = DataModel.EquipExcellentRestData;
            if (!excellentReset.IsSuccint)
            {
                GameUtils.ShowHintTip(280001);
                return;
            }
            var itemData = excellentReset.ExcellentItem;
            if (itemData.ItemId == -1)
            {
                //请选择物品
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270084));
                return;
            }
            var tbItemBase = Table.GetItemBase(itemData.ItemId);
            if (tbItemBase == null)
            {
                return;
            }
            excellentReset.EquipId = tbItemBase.Exdata[0];
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            if (tbEquip == null)
            {
                return;
            }
            var tbEquipExcellent = Table.GetEquipExcellent(tbEquip.Ladder);
            if (tbEquipExcellent == null)
            {
                return;
            }
            if (excellentReset.AttrInfos[0].Type == -1)
            {
                GameUtils.ShowHintTip(100000493);
                return;
            }
            EquipExcellMaxList.Clear();
            foreach (var item in DataModel.EquipExcellentRestData.AttrInfos)
            {
                if (99 == item.Type || 98 == item.Type)
                {
                    //等级属性
                    if (item.Value == item.MinValue)
                    {
                        EquipExcellMaxList.Add(true);
                    }
                    else
                    {
                        EquipExcellMaxList.Add(false);
                    }                
                }
                else
                {
                    if (item.Value == item.MaxValue)
                    {
                        EquipExcellMaxList.Add(true);
                    }
                    else
                    {
                        EquipExcellMaxList.Add(false);
                    }
                }
            }
            if (EquipExcellMaxList.Count >0)
            {
                if (!EquipExcellMaxList.Contains(false))
                {
                    var EquipUpdateId = Table.GetEquipBase(itemData.ItemId).EquipUpdateLogic;
                    if (-1 == EquipUpdateId)
                    {
                        GameUtils.ShowHintTip(GameUtils.GetDictionaryText(100002295));
                        return;
                    }
                    else
                    {
                        GameUtils.ShowHintTip(GameUtils.GetDictionaryText(100002287));
                        return;
                    }            
                }
            }
            if (tbEquipExcellent.GreenMoney > playerData.Bags.Resources.Gold)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.GoldRes);
                return;
            }
            //if (tbEquipExcellent.GreenItemCount > PlayerDataManager.Instance.GetItemCount(tbEquipExcellent.GreenItemId))
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
            //    PlayerDataManager.Instance.ShowItemInfoGet(tbEquipExcellent.GreenItemId);
            //    return;
            //}

            if (!GameUtils.CheckEnoughItems(tbEquipExcellent.GreenItemId, tbEquipExcellent.GreenItemCount))
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                return;
            }

            if (tbItemBase.CanTrade == 1 && itemData.Exdata.Binding != 1)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 210117, "",
                    () => { NetManager.Instance.StartCoroutine(ExcellentResetEquipCoroutine()); });
                return;
            }
            NetManager.Instance.StartCoroutine(ExcellentResetEquipCoroutine());
        }

        //洗练结构的操作，0：取消，1：确定
        private void OnClickExcellentResetAffirm(int ret)
        {
            NetManager.Instance.StartCoroutine(ExcellentResetOkCoroutine(ret));
        }

        //--------------------------------------------------------------------Super------------------
        private void OnClickSuperExcellent()
        {
            var playerData = PlayerDataManager.Instance.PlayerDataModel;
            var excellentData = DataModel.EquipSuperExcellentData;

            var tbEquip = Table.GetEquipBase(excellentData.EquipId);
            if (tbEquip == null)
            {
                return;
            }
            var tbItem = Table.GetItemBase(excellentData.ExcellentItem.ItemId);
            if (tbItem == null)
            {
                return;
            }
            if (excellentData.AttributeInfos[0].Type == -1)
            {
                GameUtils.ShowHintTip(100000495);
                return;
            }

            var ladder = tbEquip.Ladder;
            var tbExcellent = Table.GetEquipExcellent(ladder);
            if (tbExcellent == null)
            {
                return;
            }
            if (excellentData.LockMoney > playerData.Bags.Resources.Gold)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.GoldRes);
                return;
            }
            //if (excellentData.LockItemCount > PlayerDataManager.Instance.GetItemCount(tbExcellent.LockId))
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
            //    PlayerDataManager.Instance.ShowItemInfoGet(tbExcellent.LockId);
            //    return;
            //}
            //if (tbExcellent.ItemCount > PlayerDataManager.Instance.GetItemCount(tbExcellent.ItemId))
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
            //    PlayerDataManager.Instance.ShowItemInfoGet(tbExcellent.ItemId);
            //    return;
            //}
            var items = new Dictionary<int, int>();
            if (-1 != excellentData.LockItemId)
            {
                if (!GameUtils.CheckEnoughItems(excellentData.LockItemId, excellentData.LockItemCount))
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    return;
                }
            }
            if (-1 != excellentData.CostItem)
            {
                if (!GameUtils.CheckEnoughItems(excellentData.CostItem, excellentData.CostItemCount))
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    return;
                }
            }
            //items[excellentData.LockId] = excellentData.LockItemCount;
            //items[tbExcellent.ItemId] = tbExcellent.ItemCount;
            //if (!GameUtils.CheckEnoughItems(items))
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
            //    return;
            //}

            var lockList = new List<int>();
            for (var i = 0; i < 6; i++)
            {
                lockList.Add(excellentData.LockList[i] != true ? 0 : 1);
            }
            if (tbItem.CanTrade == 1 && excellentData.ExcellentItem.Exdata.Binding != 1)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 210117, "",
                    () => { NetManager.Instance.StartCoroutine(SuperExcellentEquipCoroutine(lockList)); });
                return;
            }
            NetManager.Instance.StartCoroutine(SuperExcellentEquipCoroutine(lockList));
        }
        private void OnClickSuperExcellentOperate(int ok)
        {
            NetManager.Instance.StartCoroutine(SuperExcellentResultCoroutine(ok));
        }
        //随灵结果网络包逻辑
        private IEnumerator SuperExcellentResultCoroutine(int ok)
        {
            using (new BlockingLayerHelper(0))
            {
                var itemData = DataModel.EquipSuperExcellentData.ExcellentItem;
                var msg = NetManager.Instance.SaveSuperExcellentEquip(itemData.BagId, itemData.Index, ok);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (ok == 1)
                        {
                            for (var i = 0; i < 6; i++)
                            {
                                if (itemData.Exdata[35 + i] != -1 && itemData.Exdata[41 + i] > 0)
                                {
                                    itemData.Exdata[6 + i] = itemData.Exdata[35 + i];
                                    itemData.Exdata[12 + i] = itemData.Exdata[41 + i];
                                }
                                itemData.Exdata[35 + i] = -1;
                            }
                            var e = new EquipUiNotifyLogic(1);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else
                        {
                            for (var i = 0; i < 6; i++)
                            {
                                itemData.Exdata[35 + i] = -1;
                                itemData.Exdata[41 + i] = 0;
                            }
                        }
                        RefreshSuperExcellet(itemData);
                        RefreshEquipBagStatus(itemData);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unknow)
                    {
                        //取消
                        for (var i = 0; i < 6; i++)
                        {
                            itemData.Exdata[35 + i] = -1;
                            itemData.Exdata[41 + i] = 0;
                        }
                        RefreshSuperExcellet(itemData);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.DiamondNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("SuperExcellentOK..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("SuperExcellentOK..................." + msg.State);
                }
            }
        }

        private void OnClickTips(bool isOpen)
        {
            for (var i = 0; i < DataModel.OperateTypes.Count; i++)
            {
                if (DataModel.OperateTypes[i])
                {
                    DataModel.Tips[i] = isOpen;
                    break;
                }
            }
        }

        //监听是否选择提高成功率
        private void OnEnchanceChange(object sender, PropertyChangedEventArgs e)
        {
            if (DataModel.OperateTypes[0])
            {
                if (e.PropertyName == "IsSuccessRate")
                {
                    //通过反向绑定变量IsSuccessRate，如果选择则替换成对应的强化id
                    if (DataModel.EquipEnchanceData.IsSuccessRate)
                    {
                        var tbEnchance = Table.GetEquipBlessing(DataModel.EquipEnchanceData.NowLevel);
                        DataModel.EquipEnchanceData.EnchanceId = tbEnchance.SpecialId;
                    }
                    else
                    {
                        DataModel.EquipEnchanceData.EnchanceId = DataModel.EquipEnchanceData.NowLevel;
                    }
                }
            }
        }

        private void OnEquipCellSwap(IEvent ievent)
        {
            //if (mLastType == 5)
            //{
            //    return;
            //}
            var inheritData = DataModel.EquipInheritData;
            var inheritItem = inheritData.InheritItem;
            var inheritedItem = inheritData.InheritedItem;
            RefreshInheritItem(inheritedItem);
            RefreshInheritedItem(inheritItem);
        }

        //监听传承的内容
        private void OnInheritChange(object sender, PropertyChangedEventArgs e)
        {
            if (DataModel.OperateTypes[4] &&
                (e.PropertyName == "IsEnchance"
                 || e.PropertyName == "IsAdd"
                 || e.PropertyName == "IsExcellent"))
            {
                RefreshInheritCost();
            }
        }

        private void OnRefrehEquipBagItemStatus(IEvent ievent)
        {
            var e = ievent as UIEvent_BagChange;
            if (e.HasType(eBagType.Equip))
            {
                if (State == FrameState.Open)
                {
                    PlayerDataManager.Instance.RefreshEquipBagStatus();
                    RefreshNewShengJie(mBagItemData);
                }
            }
        }

        //监听属性锁定数量的变化，影响金钱消耗
        private void OnSuperExcellentChange(object sender, PropertyChangedEventArgs e)
        {
            if (DataModel.OperateTypes[3])
            {
                RefreshSuperExcelletCost();
            }
        }

        private void OnVipLevelChanged(IEvent ievent)
        {
            var tbVip = PlayerDataManager.Instance.TbVip;
            DataModel.EquipEnchanceData.EnhanceVipAdd = tbVip.EnhanceRatio;
        }

        //根据传入的物品，生成显示数据
        private void RefreshAppend(object data)
        {
            var itemData = data as BagItemDataModel;
            var appendData = DataModel.EquipAppendData;
            appendData.AppendItem = itemData;
            var playerBags = PlayerDataManager.Instance.PlayerDataModel.Bags;
            var itemId = itemData.ItemId;
            if (itemId == -1)
            {
                ResetAppend();
                return;
            }
            var tbItemBase = Table.GetItemBase(itemId);
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            appendData.AttributeData.Type = tbEquip.AddAttrId;
            appendData.AttributeData.Value = itemData.Exdata[1];
            appendData.MaxAppendValue = tbEquip.AddAttrMaxValue;
            appendData.EquipId = tbEquip.Id;
            appendData.AppendId = tbEquip.AddIndexID;
            if (appendData.AppendId == -1)
            {
                appendData.IsMaxValue = 1;
                return;
            }
            var tbAppdend = Table.GetEquipAdditional1(appendData.AppendId);
            if (tbAppdend == null)
            {
                return;
            }
            appendData.IsMaxValue = appendData.AttributeData.Value == tbEquip.AddAttrMaxValue ? 1 : 0;
            var addLevel = GetAdditionalTable1(tbAppdend, itemData.Exdata[1]);

            appendData.CostItemCount = Table.GetSkillUpgrading(tbAppdend.MaterialCount).GetSkillUpgradingValue(addLevel);
            appendData.CostMoney = Table.GetSkillUpgrading(tbAppdend.Money).GetSkillUpgradingValue(addLevel);
            if (appendData.IsMaxValue == 1)
            {
                appendData.AttributeData.Change = 0;
                appendData.AttributeData.ChangeEx = 0;
            }
            else
            {
                var minUp = Table.GetSkillUpgrading(tbAppdend.MinSection).GetSkillUpgradingValue(addLevel);
                var maxUp = Table.GetSkillUpgrading(tbAppdend.MaxSection).GetSkillUpgradingValue(addLevel);

                var minValue = minUp + appendData.AttributeData.Value;
                if (minValue > tbEquip.AddAttrMaxValue)
                {
                    minValue = tbEquip.AddAttrMaxValue;
                    appendData.AttributeData.Change = minValue;
                    appendData.AttributeData.ChangeEx = 0;
                }
                else
                {
                    appendData.AttributeData.Change = minValue;

                    var maxValue = appendData.AttributeData.Value + maxUp;
                    if (maxValue > tbEquip.AddAttrMaxValue)
                    {
                        maxValue = tbEquip.AddAttrMaxValue;
                    }
                    appendData.AttributeData.ChangeEx = maxValue;
                }          
            }              
        }

        private void RefreshAppendNotice(int changeCount)
        {
            var bag = PlayerDataManager.Instance.GetBag((int) eBagType.Equip);
            var count = bag.Items.Count;
            for (var i = 0; i < count; i++)
            {
                var item = bag.Items[i];
                if (IsShowAppendNotice(item, changeCount))
                {
                    PlayerDataManager.Instance.WeakNoticeData.NurturanceTotal = true;
                    PlayerDataManager.Instance.WeakNoticeData.AppendNotice = true;
                    break;
                }
            }
        }

        //通过传进的物品数据，生成显示的数据
        private void RefreshEnchance(object data, bool reset = true)
        {
            var itemData = data as BagItemDataModel;
            var enchangeData = DataModel.EquipEnchanceData;
            enchangeData.EnchanceItem = itemData;
            var playerBags = PlayerDataManager.Instance.PlayerDataModel.Bags;
            if (itemData == null)
            {
                ResetEnchance();
                return;
            }
            var itemId = itemData.ItemId;
            if (itemId == -1)
            {
                ResetEnchance();
                return;
            }
            var tbItemBase = Table.GetItemBase(itemId);
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            enchangeData.EquipId = tbEquip.Id;
            enchangeData.NowLevel = itemData.Exdata[0];
            enchangeData.IsMaxLevel = enchangeData.NowLevel == tbEquip.MaxLevel ? 1 : 0;
            if (reset || enchangeData.IsMaxLevel == 1)
            {
                enchangeData.IsSpecialItem = false;
            
                enchangeData.IsSuccessRate = false;
                DataModel.EquipEnchanceData.IsSpecialItemShow = false;
            }
            var tbEnchance = Table.GetEquipBlessing(enchangeData.NowLevel);
            //显示神佑宝石
            DataModel.EquipEnchanceData.IsSpecialItemShow = tbEnchance.WarrantItemId != -1;

            if (DataModel.EquipEnchanceData.IsSpecialItemShow)
            {
                DataModel.EquipEnchanceData.AddAndSubGodBless = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count.ToString();                
            }
            enchangeData.NeedMoney = tbEnchance.NeedMoney;
            var MyMoney = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold;
            if (enchangeData.NeedMoney > MyMoney)
            {
                enchangeData.MoneyColor = Color.red;
            }
            else
            {
                enchangeData.MoneyColor = Color.white;
            }
            enchangeData.EnchanceId = enchangeData.IsSuccessRate ? tbEnchance.SpecialId : enchangeData.NowLevel;
            enchangeData.NextLevel = enchangeData.NowLevel + 1;
            for (var i = 0; i < 4; i++)
            {
                var nAttrId = tbEquip.BaseAttr[i];
                enchangeData.Attributes[i].Type = nAttrId;
                if (nAttrId != -1)
                {
                    var v = GameUtils.GetBaseAttr(tbEquip, enchangeData.NowLevel, i, nAttrId);
                    var nv = 0;
                    if (enchangeData.IsMaxLevel != 1)
                    {
                        nv = GameUtils.GetBaseAttr(tbEquip, enchangeData.NextLevel, i, nAttrId);
                    }
                    GameUtils.SetAttribute(enchangeData.Attributes, i, nAttrId, v, nv);
                }
            }

            if (!DataModel.EquipEnchanceData.IsSpecialItem)
            {
                var enchance = DataModel.EquipEnchanceData.EnchanceId;
                if (enchance != -1 && enchance != null)
                {
                    var pr = Table.GetEquipBlessing(enchance);
                    if (pr == null) return;
                    var successPR = pr.Probability / 100;
                    DataModel.EquipEnchanceData.SuccessProbability = string.Format("{0}%", successPR);
                }
            }        

        }

        private void RefreshEquipBagStatus(BagItemDataModel bagItemData, BagItemDataModel bagItemData2 = null)
        {
            PlayerDataManager.Instance.GetBagItemFightPoint(bagItemData);
            if (bagItemData2 != null)
            {
                PlayerDataManager.Instance.GetBagItemFightPoint(bagItemData2);
            }
            PlayerDataManager.Instance.RefreshEquipBagStatus();
        }

        //根据传入的物品，生成显示数据
        private void RefreshExcelletReset(object data)
        {
            var itemData = data as BagItemDataModel;
            var excellentResetData = DataModel.EquipExcellentRestData;
            var itemId = itemData.ItemId;
            if (itemId == -1)
            {
                excellentResetData.EquipId = -1;
                return;
            }
            var tbItemBase = Table.GetItemBase(itemId);

            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            if (tbEquip == null)
            {
                return;
            }
            excellentResetData.IsSuccint = tbEquip.IsSuccint == 1;
            if (!excellentResetData.IsSuccint)
            {
                GameUtils.ShowHintTip(280001);
                excellentResetData.ExcellentItem.ItemId = 14;
                excellentResetData.ExcellentItem.Exdata.Enchance = 0;
                excellentResetData.EquipId = -1;

                for (var i = 0; i < 4; i++)
                {
                    excellentResetData.AttrInfos[i].Reset();
                    excellentResetData.AttrColors[i] = MColor.grey;
                }
                return;
            }
            {
                excellentResetData.ExcellentItem.BagId = itemData.BagId;
                excellentResetData.ExcellentItem.Count = itemData.Count;
            
                for (int i = 0; i < itemData.Exdata.Count; i++)
                {
                    excellentResetData.ExcellentItem.Exdata[i] = itemData.Exdata[i];
                }
                excellentResetData.ExcellentItem.Index = itemData.Index;
                excellentResetData.ExcellentItem.ItemId = itemData.ItemId;
                excellentResetData.ExcellentItem.TotalCount = itemData.TotalCount;
                excellentResetData.ExcellentItem.IsGrey = itemData.IsGrey;
            }
            excellentResetData.EquipId = tbItemBase.Exdata[0];
            var range = tbEquip.ExcellentAttrValue;
            var tbEnchant = Table.GetEquipEnchant(range);
            if (tbEnchant == null)
            {
                return;
            }
            var minRate = tbEquip.ExcellentValueMin;
            var maxRate = tbEquip.ExcellentValueMax;
            excellentResetData.HasChange = 0;
            var attrCount = 4;
            switch (tbItemBase.Quality)
            {
                case 0: //白
                {
                    attrCount = 0;
                }
                    break;
                case 1: //绿
                {
                    attrCount = 2;
                }
                    break;
                case 2: //蓝
                {
                    attrCount = 3;
                }
                    break;
            }
            for (var i = 0; i < 4; i++)
            {
                var attrid = tbEquip.ExcellentAttrId[i];
                var index = GameUtils.GetAttrIndex(attrid);
                if (index != -1 && i < attrCount)
                {
                    excellentResetData.AttrInfos[i].Type = attrid;                
                    excellentResetData.AttrInfos[i].Value = itemData.Exdata[2 + i];                
                    excellentResetData.AttrInfos[i].MinValue = tbEnchant.Attr[index]*minRate/100;
                    excellentResetData.AttrInfos[i].MaxValue = tbEnchant.Attr[index]*maxRate/100;
                    if (itemData.Exdata[18 + i] != -1)
                    {
                        excellentResetData.HasChange = 1;
                        excellentResetData.AttrInfos[i].Change = itemData.Exdata[18 + i] - itemData.Exdata[2 + i];
                    }
                    else
                    {
                        excellentResetData.AttrInfos[i].Change = 0;
                    }

                    if (excellentResetData.AttrInfos[i].Change == 0
                        && excellentResetData.AttrInfos[i].Value == 0
                        || (excellentResetData.AttrInfos[i].Change + excellentResetData.AttrInfos[i].Value == 0))
                    {
                        //excellentResetData.AttrInfos[i].Reset();
                        excellentResetData.AttrColors[i] = MColor.grey;
                    }
                    else
                    {
                        excellentResetData.AttrColors[i] = MColor.green;
                    }
                }
                else
                {
                    excellentResetData.AttrInfos[i].Reset();
                    excellentResetData.AttrColors[i] = MColor.grey;
                }
            }
        }

        private void RefreshInherit(object data, int index, bool isFromToggle = false)
        {
            var itemData = data as BagItemDataModel;
            var inheritData = DataModel.EquipInheritData;

            if (index == -1)
            {
                if (inheritData.InheritedItem.ItemId != -1 && inheritData.InheritItem.ItemId != -1)
                {
                    RefreshInheritItem(itemData);
                }
                else
                {
                    if (inheritData.InheritedItem.ItemId == -1)
                    {
                        if (isFromToggle)
                        {
                            RefreshInheritedItem(mEmptyBagItem);
                        }
                        else
                        {
                            RefreshInheritedItem(itemData); 
                        }
                    }
                    else
                    {
                        if (inheritData.InheritItem.ItemId == -1)
                        {
                            if (isFromToggle)
                            {
                                RefreshInheritItem(mEmptyBagItem);
                            }
                            else
                            {
                                RefreshInheritItem(itemData);
                            }
                        }
                    }
                }
            }
            else
            {
                if (index == 0)
                {
                    RefreshInheritItem(itemData);
                }
                else
                {
                    RefreshInheritedItem(itemData);
                }
            }
        }

//     private void RefreshEvolvedItem()
//     {
//         var evolvedItem = DataModel.EquipShengJieData.ShengJieResultItem;
//         var mainEquip = DataModel.EquipShengJieData.ShengJieItem;
//         if (!mainEquip.IsGrey)
//         {
//             evolvedItem.Clone(mainEquip);
//             evolvedItem.ItemId = evolvedItem.ItemId;
//         }
//         var maxEnhance = 0;
//         var maxAppend = 0;
//         foreach (var item in evoData.EvolveItems)
//         {
//             if (item.IsGrey)
//             {
//                 continue;
//             }
//             if (maxEnhance < item.Exdata.Enchance)
//             {
//                 maxEnhance = item.Exdata.Enchance;
//             }
//             if (maxAppend < item.Exdata.Append)
//             {
//                 maxAppend = item.Exdata.Append;
//             }
//         }
//         if (evolvedItem.Exdata.Enchance != maxEnhance)
//         {
//             evolvedItem.Exdata.Enchance = maxEnhance;
//         }
//         if (evolvedItem.Exdata.Append != maxAppend)
//         {
//             evolvedItem.Exdata.Append = maxAppend;
//         }
//     }

        private void RefreshNewShengJie(object data)
        {
            if (!DataModel.OperateTypes[5])
            {
                return;
            }

            var itemData = data as BagItemDataModel;
            if (itemData == null)
            {
                ResetShengJie();
                return;
            }
            var itemId = itemData.ItemId;
            if (itemId == -1)
            {
                ResetShengJie();
                return;
            }

            var enchangeData = DataModel.EquipShengJieData;
            var tbItemBase = Table.GetItemBase(itemId);
            var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
            enchangeData.ShengJieItem = itemData;
            enchangeData.EquipId = tbEquip.Id;
            enchangeData.NowLevel = itemData.Exdata[0];
            enchangeData.IsMaxLevel = enchangeData.NowLevel == tbEquip.MaxLevel ? 1 : 0;
            enchangeData.ShengJieId = tbEquip.EquipUpdateLogic;

            var tbEquipEvo = Table.GetEquipUpdate(tbEquip.EquipUpdateLogic);
            if (tbEquipEvo == null)
            {
                GameUtils.ShowHintTip(200002863);
                ResetShengJie();
                return;
            }

            var tbUpEquip = Table.GetEquipBase(tbEquip.UpdateEquipID);
            if (tbUpEquip == null)
            {
                GameUtils.ShowHintTip(200002863);
                ResetShengJie();
                return;
            }

            enchangeData.NeedRes = tbEquipEvo.NeedResCount[0];
            enchangeData.Items.Clear();
            for (int i = 0; i < tbEquipEvo.NeedItemID.Length; ++i)
            {
                if (tbEquipEvo.NeedItemID == null || tbEquipEvo.NeedItemCount == null)
                {
                    continue;
                }
                if (tbEquipEvo.NeedItemID[i] == 0 || tbEquipEvo.NeedItemID[i] == -1 || tbEquipEvo.NeedItemCount[i] <= 0)
                {
                    continue;
                }
                var tmp = new ItemEquipJinJieCellDataModel();
                tmp.ItemId = tbEquipEvo.NeedItemID[i];
                tmp.Count = tbEquipEvo.NeedItemCount[i];
                tmp.TotalCount = PlayerDataManager.Instance.GetItemTotalCount(tmp.ItemId).Count;
                enchangeData.Items.Add(tmp);
            }

            var needItem = new ItemEquipJinJieCellDataModel();
            needItem.ItemId = mBagItemData.ItemId;
            needItem.Count = tbEquipEvo.NeedEquipCount - 1;
            var _bagItem = PlayerDataManager.Instance.GetBagItemByItemIdjingJie((int)eBagType.Equip, tbEquip.EquipUpdateLogic);
            needItem.TotalCount = _bagItem.Count;//PlayerDataManager.Instance.GetItemTotalCount(needItem.ItemId).Count;
            enchangeData.Items.Add(needItem);
            enchangeData.ItemCount = enchangeData.Items.Count;

            var resultEquip = new BagItemDataModel();
            resultEquip.Clone(mBagItemData);
            resultEquip.ItemId = tbUpEquip.Id;
            resultEquip.Count = 1;

            // 复制
            //RefreshEvolvedItem();
            PlayerDataManager.Instance.RefreshEquipBagStatus(resultEquip);
            enchangeData.ShengJieResultItem = resultEquip;

            for (var i = 0; i < 4; i++)
            {
                var nAttrId = tbEquip.BaseAttr[i];
                enchangeData.Attributes[i].Type = nAttrId;
                if (nAttrId != -1)
                {
                    var v = GameUtils.GetBaseAttr(tbEquip, enchangeData.NowLevel, i, nAttrId);
                    var nv = GameUtils.GetBaseAttr(tbUpEquip, enchangeData.NowLevel, i, nAttrId);
                    GameUtils.SetAttribute(enchangeData.Attributes, i, nAttrId, v, nv);
                }
            }
        }

        private void RefreshShengJie()
        {
            if (mSmithyFrameController != null)
            {
                mSmithyFrameController.RefreshData(new SmithyFrameArguments
                {
                    BuildingData = null
                });
            }

            //mEquipPackController.RefreshData(new EquipPackArguments { RefreshForEvoEquip = true });
        }

        //重新计算花费
        private void RefreshInheritCost()
        {
            var inheritData = DataModel.EquipInheritData;
            var itemdData = inheritData.InheritItem;
            if (itemdData.ItemId == -1)
            {
                return;
            }
            if (inheritData.IsEnchance)
            {
                var enchanceLv = itemdData.Exdata[0];
                var tbEnchanve = Table.GetEquipBlessing(enchanceLv);
                inheritData.CostGold = tbEnchanve.SmritiMoney;
                inheritData.CostDiamond = tbEnchanve.SmritiGold;
                return;
            }
            if (inheritData.IsAdd)
            {
                var tbEquip = Table.GetEquipBase(itemdData.ItemId);
                if (tbEquip == null)
                {
                    return;
                }
                var tbAdditional = Table.GetEquipAdditional1(tbEquip.AddIndexID);
                if (tbAdditional == null)
                {
                    return;
                }
                var AddLevel = GetAdditionalTable1(tbAdditional, itemdData.Exdata[1]);
                //金钱检查
                inheritData.CostGold = Table.GetSkillUpgrading(tbAdditional.SmritiMoney).GetSkillUpgradingValue(AddLevel);
                inheritData.CostDiamond =
                    Table.GetSkillUpgrading(tbAdditional.SmritiDiamond).GetSkillUpgradingValue(AddLevel);
                return;
            }
            if (inheritData.IsExcellent)
            {
                var tbItemBase = Table.GetItemBase(itemdData.ItemId);
                var tbEquip = Table.GetEquipBase(tbItemBase.Exdata[0]);
                var tbExcellent = Table.GetEquipExcellent(tbEquip.Ladder);
                inheritData.CostGold = tbExcellent.SmritiMoney;
                inheritData.CostDiamond = tbExcellent.SmritiGold;
            }
        }

        //设置继承目的物品
        private void RefreshInheritedItem(BagItemDataModel bagItem)
        {
            var inheritData = DataModel.EquipInheritData;
            if (bagItem.ItemId != -1 && inheritData.InheritItem == bagItem)
            {
                mEquipPackController.CallFromOtherClass("RefreshForEquipInherit",
                    new object[2] {inheritData.InheritItem, inheritData.InheritedItem});
            }
            else
            {
                inheritData.InheritedItem = bagItem;
                //mBagItemData = bagItem;
                SetItemData(bagItem);
                mEquipPackController.CallFromOtherClass("RefreshForEquipInherit",
                    new object[2] {inheritData.InheritItem, inheritData.InheritedItem});
            }
        }

        //设置继承来源物品
        private void RefreshInheritItem(BagItemDataModel bagItem)
        {
            var inheritData = DataModel.EquipInheritData;
            if (inheritData.InheritedItem.ItemId != -1)
            {
                SetItemData(inheritData.InheritedItem);
//            mBagItemData = inheritData.InheritedItem;
            }
            if (bagItem.ItemId != -1 && inheritData.InheritItem == bagItem)
            {
                mEquipPackController.CallFromOtherClass("RefreshForEquipInherit",
                    new object[2] {inheritData.InheritItem, inheritData.InheritedItem});
                return;
            }
            inheritData.InheritItem = bagItem;
            mEquipPackController.CallFromOtherClass("RefreshForEquipInherit",
                new object[2] {inheritData.InheritItem, inheritData.InheritedItem});
            if (bagItem.ItemId == -1)
            {
                ResetInheritData();
            }
            else
            {
                RefreshInheritCost();
            }
        }

        private void SetItemData(BagItemDataModel data)
        {
            if (null != mBagItemData)
            {
                mBagItemData.IsChoose = false;
            }
            mBagItemData = data;
            if (data != null)
            {
                mBagItemData.IsChoose = true;
            }
        }
        private void RefreshItemData(BagItemDataModel data, int index = -1, bool isFromToggle = false)
        {
            if (data != null)
            {
                SetItemData(data);
//            mBagItemData = data;
            }

            for (var i = 0; i < 7; i++)
            {
                if (DataModel.OperateTypes[i])
                {
                    if (i != 4 && mLastType == 4)
                    {
                        mEquipPackController.CallFromOtherClass("Refresh", null);
                    }
                    //if (i != 5 && 5 == mLastType)
                    //{
                    //    mEquipPackController.CallFromOtherClass("Refresh", null);
                    //}

                    if (mBagItemData == null || mBagItemData.ItemId == -1)
                    {
                        if (EquipPackDataModel != null)
                        {
                            if (EquipPackDataModel.EquipItems.Count > 0)
                            {
                                SetItemData(EquipPackDataModel.EquipItems[0].BagItemData);
//                            mBagItemData = EquipPackDataModel.EquipItems[0].BagItemData;
                            }
                            else
                            {
                                if (EquipPackDataModel.PackItems.Count > 0)
                                {
                                    SetItemData(EquipPackDataModel.EquipItems[0].BagItemData);
                                    //mBagItemData = EquipPackDataModel.PackItems[0].BagItemData;
                                }
                            }
                        }
                        if (mBagItemData == null)
                        {
                            SetItemData(mEmptyBagItem);
//                        mBagItemData = mEmptyBagItem;
                        }
                        mEquipPackController.RefreshData(new EquipPackArguments {DataModel = mBagItemData});
                    }
                    switch (i)
                    {
                        case 0:
                        {
                            RefreshEnchance(mBagItemData);
                        }
                            break;
                        case 1:
                        {
                            RefreshAppend(mBagItemData);
                        }
                            break;
                        case 2:
                        {
                            RefreshExcelletReset(mBagItemData);
                        }
                            break;
                        case 3:
                        {
                            ResetSuperExcellet();
                            RefreshSuperExcellet(mBagItemData);
                        }
                            break;
                        case 4:
                        {
                            if (isFromToggle)
                            {
                                RefreshInherit(mBagItemData, index, isFromToggle);
                            }
                            else
                            {
                                RefreshInherit(mBagItemData, index);
                            }
                        }
                            break;
                        //case 5:
                        //{
                        //    RefreshShengJie();
                        //}
                        //break;
                        case 5:
                        {
                            RefreshNewShengJie(mBagItemData);
                        }
                            break;
                    }
                    if (i != 4)
                    {
                        if (DataModel.EquipInheritData.InheritItem.ItemId != -1)
                        {
                            DataModel.EquipInheritData.InheritItem = mEmptyBagItem;
                        }
                        if (DataModel.EquipInheritData.InheritedItem.ItemId != -1)
                        {
                            DataModel.EquipInheritData.InheritedItem = mEmptyBagItem;
                        }
                        mEquipPackController.CallFromOtherClass("RefreshSelectFlag", new object[1] {mBagItemData});
                    }
                    mLastType = i;
                }
            }
        }

        //根据传入的物品，生成显示数据
        private void RefreshSuperExcellet(object data)
        {
            var itemData = data as BagItemDataModel;
            if (itemData == null)
            {
                return;
            }
            var excellentData = DataModel.EquipSuperExcellentData;
            for (var i = 0; i < 6; i++)
            {
                excellentData.AttributeInfos[i].Type = -1;
            }
            excellentData.SuperExcellentChange =0;
            excellentData.ExcellentItem = itemData;
            var itemId = itemData.ItemId;
            if (itemId == -1)
            {
                excellentData.EquipId = -1;
                return;
            }
            var tbItemBase = Table.GetItemBase(itemId);
            if (tbItemBase == null)
            {
                return;
            }
            excellentData.EquipId = tbItemBase.Exdata[0];
            var tbEquip = Table.GetEquipBase(excellentData.EquipId);
            if (tbEquip == null)
            {
                return;
            }
            excellentData.CanSuperExcellent = -1;
           
            var range = tbEquip.NewRandomAttrValue;
            if(tbEquip.RandomAttrCount>=0 && range>=0){
                 excellentData.CanSuperExcellent = 1;
            }
            if (excellentData.CanSuperExcellent!=1)
            {
                GameUtils.ShowHintTip(100000495);
            }
            var tbEnchant = Table.GetEquipEnchant(range);
            if (tbEnchant == null)
            {
                return;
            }

            for (var i = 0; i < 6; i++)
            {
                var rAttrId = itemData.Exdata[35 + i];
                var rAttrValue = itemData.Exdata[41 + i];
                if (rAttrId != -1 && rAttrValue>0)
                {
                    excellentData.SuperExcellentChange = 1;
                    break;
                }
            }

            var minRate = tbEquip.NewRandomValueMin;
            var maxRate = tbEquip.NewRandomValueMax;


            if (excellentData.SuperExcellentChange == 1)
            {
                List<int> orgin = new List<int>();
                for (var i = 0; i < 6; i++)//检查一样属性
                {
                    var AttrId = itemData.Exdata[6 + i];
                    //var AttrValue = itemData.Exdata[12 + i];
                    if (AttrId != -1)
                    {
                        orgin.Add(AttrId);
                        int containAttrIndex = -1;
                        for (var j = 0; j < 6; j++)
                        {
                            var rAttrId = itemData.Exdata[35 + j];
                            var rAttrValue = itemData.Exdata[41 + j];
                            if (AttrId == rAttrId)
                            {
                                containAttrIndex = j;
                                break;
                            }
                        }
                        if (containAttrIndex>=0)
                        {
                             excellentData.AttributeInfos[i].Type = AttrId;
                             excellentData.AttributeInfos[i].Value = itemData.Exdata[12 + i];
                             excellentData.AttributeInfos[i].Change = (itemData.Exdata[41 + containAttrIndex] - itemData.Exdata[12 + i]);

                             var index = GameUtils.GetAttrIndex(AttrId);//新旧属性肯定是一样的
                             excellentData.AttributeInfos[i].MinValue = tbEnchant.Attr[index] * minRate / 100;
                             excellentData.AttributeInfos[i].MaxValue = tbEnchant.Attr[index] * maxRate / 100;

                        }
                    }
                }
                for (var i = 0; i < 6; i++)
                {
                    var rAttrId = itemData.Exdata[35 + i];
                    var rAttrValue = itemData.Exdata[41 + i];
                    if (rAttrId != -1 && !orgin.Contains(rAttrId))
                    {
                        int containAttrIndex = -1;
                        for (var j = 0; j < 6; j++)
                        {
                            if (excellentData.AttributeInfos[j].Type == -1)
                            {
                                containAttrIndex = j;
                                break;
                            }
                        }
                        if (containAttrIndex >= 0)
                        {
                            excellentData.AttributeInfos[containAttrIndex].Type = rAttrId;
                            excellentData.AttributeInfos[containAttrIndex].Value = itemData.Exdata[41 + i];
                            excellentData.AttributeInfos[containAttrIndex].Change = 0;

                            var index = GameUtils.GetAttrIndex(rAttrId);//新旧属性肯定是一样的
                            excellentData.AttributeInfos[containAttrIndex].MinValue = tbEnchant.Attr[index] * minRate / 100;
                            excellentData.AttributeInfos[containAttrIndex].MaxValue = tbEnchant.Attr[index] * maxRate / 100;
                        }

                    }
                }
                for (var i = 0; i < 6; i++)
                {
                    if (excellentData.AttributeInfos[i].Type == -1)
                    {
                        excellentData.ShowList[i] = false;
                        excellentData.AttributeInfos[i].Reset();
                    }
                }
            }
            else
            {
               
               
                for (var i = 0; i < 6; i++)
                {

                    var AttrId = itemData.Exdata[6 + i];
                    var AttrValue = itemData.Exdata[12 + i];
                    excellentData.AttributeInfos[i].Change = 0;
                   

                    if (AttrId != -1)
                    {
                        excellentData.AttributeInfos[i].Type = AttrId;
                        var index = GameUtils.GetAttrIndex(AttrId);//新旧属性肯定是一样的
                        excellentData.AttributeInfos[i].Value = itemData.Exdata[12 + i];

                        excellentData.AttributeInfos[i].MinValue = tbEnchant.Attr[index] * minRate / 100;
                        excellentData.AttributeInfos[i].MaxValue = tbEnchant.Attr[index] * maxRate / 100;
                    }
                    else
                    {
                        excellentData.ShowList[i] = false;
                        excellentData.AttributeInfos[i].Reset();

                    }
                }
            }

          
            RefreshSuperExcelletCost();
        }

        private void RefreshSuperExcelletCost()
        {
            var excellentData = DataModel.EquipSuperExcellentData;
            var lockNum = 0;
            var attrNum = 0;
            for (var i = 0; i < 6; i++)
            {
                if (excellentData.AttributeInfos[i].Type != -1)
                {
                    attrNum++;
                }
                if (excellentData.LockList[i])
                {
                    lockNum++;
                }
            }

            if (lockNum == attrNum - 1)
            {
                for (var i = 0; i < 6; i++)
                {
                    if (!excellentData.LockList[i])
                    {
                        excellentData.ShowList[i] = false;
                    }
                    else
                    {
                        excellentData.ShowList[i] = true;
                    }
                }
            }
            else
            {
                for (var i = 0; i < 6; i++)
                {
                    if (excellentData.AttributeInfos[i].Type != -1)
                    {
                        excellentData.ShowList[i] = true;
                    }
                    else
                    {
                        excellentData.ShowList[i] = false;
                    }
                }
            }
            SuperExcellentCheckBoxList.Clear();
            var tbEquip = Table.GetEquipBase(excellentData.EquipId);
            var ladder = tbEquip.Ladder;
            var tbExcellent = Table.GetEquipExcellent(ladder);
            excellentData.CostItem = tbExcellent.ItemId;
            excellentData.CostItemCount = tbExcellent.ItemCount;
            for (int i = 0; i < excellentData.LockList.Count; i++)
            {
                SuperExcellentCheckBoxList.Add(excellentData.LockList[i]);
            }
            if (SuperExcellentCheckBoxList.Contains(true))
            {
                excellentData.LockItemId = tbExcellent.LockId;
                excellentData.LockItemCount = lockNum > 0 ? tbExcellent.LockCount[lockNum - 1] : 0;
            }
            else
            {
                excellentData.LockItemId = -1;
            }
            excellentData.LockMoney = tbExcellent.Money[lockNum];
            excellentData.TotalLock = lockNum;
        }

        //重置追加显示
        private void ResetAppend()
        {
            var appendData = DataModel.EquipAppendData;
            appendData.AppendId = -1;
            appendData.AttributeData.Reset();
            appendData.MaxAppendValue = 0;
            appendData.IsMaxValue = 0;
            appendData.CostMoney = 0;
            appendData.CostItemCount = 0;
        }

        //重置界面显示
        private void ResetEnchance()
        {
            var enchangeData = DataModel.EquipEnchanceData;
            enchangeData.EnchanceId = -1;
            enchangeData.NextLevel = 0;
            enchangeData.IsMaxLevel = 0;

            for (var i = 0; i < 4; i++)
            {
                enchangeData.Attributes[i].Reset();
            }
        }

        //重置界面显示
        private void ResetShengJie()
        {
            var enchangeData = DataModel.EquipShengJieData;
            var resultEq = new BagItemDataModel();
            resultEq.ItemId = -1;
            resultEq.Count = 1;
            resultEq.Status = (int)eBagItemType.UnLock;
            enchangeData.ShengJieResultItem = resultEq;
            enchangeData.NowLevel = 0;

            enchangeData.NeedRes = 0;
            enchangeData.Items.Clear();
            enchangeData.ItemCount = 0;

            for (var i = 0; i < 4; i++)
            {
                enchangeData.Attributes[i].Reset();
            }
        }

        private void ResetInheritData()
        {
            var inheritData = DataModel.EquipInheritData;
            inheritData.CostGold = 0;
            inheritData.CostDiamond = 0;
        }

        private void ResetSuperExcellet()
        {
            var excellentData = DataModel.EquipSuperExcellentData;
            for (var i = 0; i < 6; i++)
            {
                excellentData.LockList[i] = false;
                excellentData.ShowList[i] = true;
            }
        }

        //继承网络包逻辑
        private void SmritiEquipConfirm()
        {
            var inheritData = DataModel.EquipInheritData;
            var tbItem = Table.GetItemBase(inheritData.InheritedItem.ItemId);
            if (tbItem == null)
            {
                return;
            }
            if (tbItem.CanTrade == 1 && inheritData.InheritedItem.Exdata.Binding != 1)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 210117, "",
                    () => { NetManager.Instance.StartCoroutine(SmritiEquipCoroutine()); });
                return;
            }
            NetManager.Instance.StartCoroutine(SmritiEquipCoroutine());
        }

        private IEnumerator SmritiEquipCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var inheritData = DataModel.EquipInheritData;
                var smritiType = 0;
                if (inheritData.IsAdd)
                {
                    smritiType = 1;
                }
                else if (inheritData.IsExcellent)
                {
                    smritiType = 2;
                }
                var costType = 0;
                costType = inheritData.IsGold ? 0 : 1;

                var inheritItem = inheritData.InheritItem;
                var inheritedItem = inheritData.InheritedItem;

                var tbFromEquip = Table.GetEquipBase(inheritItem.ItemId);
                var tbToEquip = Table.GetEquipBase(inheritedItem.ItemId);
                if (tbFromEquip == null || tbToEquip == null)
                {
                    yield break;
                }
                var msg = NetManager.Instance.SmritiEquip(smritiType, costType, inheritItem.BagId, inheritItem.Index,
                    inheritedItem.BagId, inheritedItem.Index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var e = new EquipUiNotifyLogic(1);
                        EventDispatcher.Instance.DispatchEvent(e);
                        //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220008));
                        if (inheritedItem.Exdata.Binding != 1)
                        {
                            inheritedItem.Exdata.Binding = 1;
                        }
                        if (inheritData.IsEnchance)
                        {
                            if (inheritItem.Exdata[0] > tbToEquip.MaxLevel)
                            {
                                inheritedItem.Exdata[0] = tbToEquip.MaxLevel;
                            }
                            else
                            {
                                inheritedItem.Exdata[0] = inheritItem.Exdata[0];
                            }
                            inheritItem.Exdata[0] = 0;
                        }
                        else if (inheritData.IsAdd)
                        {
                            //if (appendValue1 > tbToEquip.AddAttrMaxValue)
                            //{
                            //    appendValue1 = tbToEquip.AddAttrMaxValue;
                            //}
                            inheritedItem.Exdata[1] = msg.Response;
                            inheritItem.Exdata[1] = 0;
                        }
                        else if (inheritData.IsExcellent)
                        {
                            var range = tbToEquip.ExcellentAttrValue;
                            var tbEnchant = Table.GetEquipEnchant(range);
                            if (tbEnchant == null)
                            {
                                yield break;
                            }
                            var maxRate = tbToEquip.ExcellentValueMax;

                            for (var i = 0; i < 4; i++)
                            {
                                var attrid = tbToEquip.ExcellentAttrId[i];
                                var index = GameUtils.GetAttrIndex(attrid);
                                if (index != -1)
                                {
                                    if (attrid != tbFromEquip.ExcellentAttrId[i])
                                    {
                                        if (inheritedItem.Exdata[2 + i] < tbEnchant.Attr[index]*maxRate/100)
                                        {
                                            inheritedItem.Exdata[2 + i] = tbEnchant.Attr[index]*maxRate/100;
                                        }
                                        else
                                        {
                                            inheritedItem.Exdata[2 + i] = inheritItem.Exdata[2 + i];
                                        }
                                    }
                                }
                                inheritItem.Exdata[2 + i] = 0;
                            }
                        }

                        RefreshEquipBagStatus(inheritItem, inheritedItem);
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.DiamondNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.Error_EquipNoAdditionalNoSmrit)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270262));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("SmritiEquip..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("SmritiEquip..................." + msg.State);
                }
            }
        }

        //随灵网络包逻辑
        private IEnumerator SuperExcellentEquipCoroutine(List<int> lockList)
        {
            using (new BlockingLayerHelper(0))
            {
                var excellentData = DataModel.EquipSuperExcellentData;
                var itemData = excellentData.ExcellentItem;
                var array = new Int32Array();
                array.Items.AddRange(lockList);
                var msg = NetManager.Instance.SuperExcellentEquip(itemData.BagId, itemData.Index, array);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        for (var i = 0; i < 6; i++)
                        {
                            //if (msg.Response.AttrId.Count > i + 1&& msg.Response.AttrValue.Count > i + 1)
                            //{
                            //    itemData.Exdata[35 + i] = msg.Response.AttrId[i];
                            //    itemData.Exdata[41 + i] = msg.Response.AttrValue[i];
                            //}
                            itemData.Exdata[35 + i] = msg.Response.AttrId[i];
                            itemData.Exdata[41 + i] = msg.Response.AttrValue[i];
                        }
                        RefreshSuperExcellet(itemData);
                        if (itemData.Exdata.Binding != -1)
                        {
                            itemData.Exdata.Binding = 1;
                        }

                        PlatformHelper.UMEvent("EquipSuiLing", itemData.BagId.ToString());
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("SuperExcellentEquip..................." + msg.ErrorCode);
                    }
                }
                else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                }
                else if (msg.ErrorCode == (int) ErrorCodes.DiamondNotEnough)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                }
                else if (msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                }
                else
                {
                    Logger.Debug("SuperExcellentEquip..................." + msg.State);
                }
            }
        }

        public void CleanUp()
        {
            if (DataModel != null)
            {
                DataModel.EquipEnchanceData.PropertyChanged -= OnEnchanceChange;
                DataModel.EquipSuperExcellentData.LockList.PropertyChanged -= OnSuperExcellentChange;
                DataModel.EquipInheritData.PropertyChanged -= OnInheritChange;
            }

            DataModel = new EquipUIDataModel();           
            DataModel.OperateTypes[0] = true;

            totalCount = new TotalCount();

            DataModel.EquipEnchanceData.PropertyChanged += OnEnchanceChange;
            DataModel.EquipSuperExcellentData.LockList.PropertyChanged += OnSuperExcellentChange;
            DataModel.EquipInheritData.PropertyChanged += OnInheritChange;
            DataModel.RefineGemNoticeCount = int.Parse(Table.GetClientConfig(1220).Value);
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
            EventDispatcher.Instance.AddEventListener(EquipCellSelect.EVENT_TYPE, OnChangeEquipCell);
            EventDispatcher.Instance.AddEventListener(EquipCellSwap.EVENT_TYPE, OnEquipCellSwap);

            if (mEquipPackController != null)
            {
                mEquipPackController.OnShow();
            }

            if (mSmithyFrameController != null)
            {
                mSmithyFrameController.OnShow();
            }
            var ee = new PlaySpriteAnimationEvent();
            EventDispatcher.Instance.DispatchEvent(ee);
            if (!DataModel.isAutoRefine)
            {
                DataModel.IsShowEffect = false;
                var e = new EquipUiNotifyLogic(4);
                EventDispatcher.Instance.DispatchEvent(e);
            }
            else
            {
                DataModel.IsShowEffect = true;
            }
        }

        public void Close()
        {
            EventDispatcher.Instance.RemoveEventListener(EquipCellSelect.EVENT_TYPE, OnChangeEquipCell);
            EventDispatcher.Instance.RemoveEventListener(EquipCellSwap.EVENT_TYPE, OnEquipCellSwap);
            //DataModel.ToggleSelect = -1;
            //mLastType = -1;
            DataModel.isAutoRefine = false;
            if (mSmithyFrameController != null)
            {
                mSmithyFrameController.Close();
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "EquipPack")
            {
                return EquipPackDataModel;
            }
            return DataModel;
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as EquipUIArguments;
            //mBagItemData = null;
            SetItemData(null);
            var openId = 0;
            BagItemDataModel itemData = null;
            if (args != null)
            {
                if (args.Tab < 0 || args.Tab >= 6)
                {
                    openId = 0;
                }
                else
                {
                    openId = args.Tab;
                }
                if (args.Data != null)
                {
                    if (args.ResourceType == 0) //背包打开
                    {
                        itemData = PlayerDataManager.Instance.GetItem(args.Data.BagId, args.Data.Index);
                    }
                    else if (args.ResourceType == 1) //身上打开
                    {
                        itemData = args.Data;
                    }
                }
            }

            for (var i = 0; i < 7; i++)
            {
                DataModel.OperateTypes[i] = false;
            }

            for (var i = 0; i < 7; i++)
            {
                if (i == openId)
                {
                    DataModel.OperateTypes[i] = true;
                    break;
                }
            }
            for (var i = 0; i < DataModel.Tips.Count; i++)
            {
                DataModel.Tips[i] = false;
            }          
           
            mEquipPackController.CallFromOtherClass("Refresh", null);
            DataModel.ToggleSelect = openId;
            RefreshItemData(itemData);
            mLastType = DataModel.ToggleSelect;
            PlayerDataManager.Instance.RefreshEquipBagStatus();
            if (DataModel.EquipEnchanceData != null)
            {
                var enchanceId = DataModel.EquipEnchanceData.EnchanceId;
                if (enchanceId != -1)
                {
                    var tbEnchance = Table.GetEquipBlessing(enchanceId);
                    if (tbEnchance == null) return;
                    DataModel.EquipEnchanceData.AddAndSubGodBless = PlayerDataManager.Instance.GetItemTotalCount(tbEnchance.WarrantItemId).Count.ToString();
                    warrantNum = int.Parse(DataModel.EquipEnchanceData.AddAndSubGodBless);
                }
            }
        }

        public FrameState State { get; set; }
    }
}