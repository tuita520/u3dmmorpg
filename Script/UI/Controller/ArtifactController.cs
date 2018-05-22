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
using UnityEngine;

#endregion


namespace ScriptController
{
    public class ArtifactController : IControllerBase
    {
        private bool isInitDict = false;
        private Dictionary<int, List<EuqipInfo>> equipRecordDict = new Dictionary<int, List<EuqipInfo>>();
        private List<EuqipInfo> equipList;
        private int lastSelectIndex = -1;
        private Coroutine mPressTriger;

        public ArtifactController()
        {
            CleanUp();
        
            EventDispatcher.Instance.AddEventListener(ArtifactOpEvent.EVENT_TYPE, OnEvent_Operate);
            EventDispatcher.Instance.AddEventListener(ArtifactSelectEvent.EVENT_TYPE, OnEvent_SelectModel);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChange);
            EventDispatcher.Instance.AddEventListener(Enter_Scene_Event.EVENT_TYPE, OnEnterScene);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemCountChange);
        }

        private ArtifactDataModel DataModel;
        private EquipInfoDataModel InfoDatamodel;
        private bool mIsInit = true;
        private string StrDic230004;
        private string StrDic230006;
        private string StrDic230025;
        private string StrDic230032;
        private string StrDic230033;
        private string StrDic230034;

        public void CleanUp()
        {
            DataModel = new ArtifactDataModel();
            InfoDatamodel = new EquipInfoDataModel();
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
            if (isRefresh)
            {
                //NetManager.Instance.StartCoroutine(UpdateEquioNumContie());
                UpdateEquioNum();
            }

            if (num != -1)
            {
                EventDispatcher.Instance.DispatchEvent(new UpdateMaYaUIModelEvent(num));
            }
            isRefresh = true;
        }

        public void Tick()
        {
        }

        private void InitEquipConfig()
        {
            if (isInitDict)
            {
                return;
            }
            isInitDict = true;

            equipRecordDict.Clear();

            Table.ForeachEquipBase(record =>
            {
                if (record.ShowEquip != 1)
                    return true;

                if (record.Occupation == -1)
                    return true;

                List<EuqipInfo> equipList;
                if (!equipRecordDict.TryGetValue(record.Occupation, out equipList))
                {
                    equipRecordDict[record.Occupation] = new List<EuqipInfo>();
                    equipList = equipRecordDict[record.Occupation];
                }

                var equipInfo = new EuqipInfo();
                equipInfo.Record = record;
                equipList.Add(equipInfo);

                return true;
            });

            var enumerator = equipRecordDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var equipList = enumerator.Current.Value;
                if (equipList != null)
                {
                    equipList.Sort((l, r) =>
                    {
                        if (l.Record.Ladder > r.Record.Ladder)
                            return 1;
                        else if (l.Record.Ladder == r.Record.Ladder)
                            return 0;
                        else
                            return -1;
                    });

                    var enumerator1 = equipList.GetEnumerator();
                    var minLadder = -1;
                    while (enumerator1.MoveNext())
                    {
                        if (enumerator1.Current == null)
                            continue;
                        if (enumerator1.Current.Record.Ladder == minLadder || minLadder == -1)
                        {
                            minLadder = enumerator1.Current.Record.Ladder;
                            enumerator1.Current.CanBuy = true;

                            var tbItem = Table.GetItemBase(enumerator1.Current.Record.Id);
                            if (tbItem == null)
                                continue;
                            var itemComposeRecord = Table.GetItemCompose(tbItem.ComposeID);
                            if (itemComposeRecord != null)
                            {
                                // 潜规则第一个了。。
                                enumerator1.Current.BuyCost = itemComposeRecord.NeedCount[0];
                                enumerator1.Current.BuyNeedItemId = itemComposeRecord.NeedId[0];
                                DataModel.NeedItemId = enumerator1.Current.BuyNeedItemId;
                            }
                        }
                        else
                        {
                            enumerator1.Current.CanBuy = false;
                        }
                    }
                }
            }
        }

        private int index = -1;
        private bool isRefresh = false;
        private bool isHav = false;
        private int num = -1;
        private List<int> list = new List<int>();
        public void RefreshData(UIInitArguments data)
        {
            isRefresh = false;
            isHav = true;
            InitEquipConfig();

            var roleId = PlayerDataManager.Instance.GetRoleId();
            if (!equipRecordDict.TryGetValue(roleId, out equipList))
            {
                return;
            }
            index = -1;
       
            DataModel.Career = roleId;
            //if (DataModel.WeaponItems.Count == 0)
            //{
            DataModel.WeaponItems.Clear();
            DataModel.Models.Clear();
            var enumerator = equipList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                    continue;

                var itemDm = new ItemIdDataModel();
                itemDm.ItemId = enumerator.Current.Record.Id;
                itemDm.Count = 0;
               
                var dm = new EquipModelDataModel();
                dm.EquipId = enumerator.Current.Record.Id;
                dm.Select = false;
                var weaponMainId = PlayerDataManager.Instance.GetEquipData(eEquipType.WeaponMain).ItemId;
                             
                if (isHav)
                {
                    list.Clear();
                    for (int i = 0; i < equipList.Count; i++)
                    {
                        var equipCount1 = PlayerDataManager.Instance.GetItemCount(equipList[i].Record.Id);
                        if (equipCount1 > 0 || weaponMainId == equipList[i].Record.Id)
                        {
                            num = i;
                            list.Add(equipList[i].Record.Id);
                        }                      
                    }
                    list.Sort();
                    isHav = false;
                    if (list.Count <= 0)
                    {
                        DataModel.WeaponItems.Add(itemDm);
                        DataModel.Models.Add(dm);
                        break;
                    }                  
                }
                
                if (index > num)
                {
                    break;
                }
                index++;
                DataModel.WeaponItems.Add(itemDm);
                DataModel.Models.Add(dm);              
            }
            //}    
        }

        private void OnResourceChange(IEvent ievent)
        {
            var e = ievent as Resource_Change_Event;
            if (e == null)
                return;

            if (e.Type == eResourcesType.AchievementScore)
            {
                DataModel.AchievementPoint = e.NewValue;
            }
        }

        private void OnEnterScene(IEvent ievent)
        {
            RefreshRedPoint();
        }

        private void RefreshRedPoint()
        {
            InitEquipConfig();

            List<EuqipInfo> currentEquipList;
            var roleId = PlayerDataManager.Instance.GetRoleId();
            if (!equipRecordDict.TryGetValue(roleId, out currentEquipList))
            {
                return;
            }
            PlayerDataManager.Instance.NoticeData.MayaNotice = false;
            var enumerator = currentEquipList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var equipInfo = enumerator.Current;
                if (equipInfo != null && equipInfo.CanBuy)
                {
                    var haveItem = PlayerDataManager.Instance.GetItemCount(equipInfo.BuyNeedItemId);
                    if (haveItem >= equipInfo.BuyCost)
                    {
                        PlayerDataManager.Instance.NoticeData.MayaNotice = true;
                        break;
                    }
                }
            }
        }

        private void OnBagItemCountChange(IEvent ievent)
        {
            var e = ievent as UIEvent_BagItemCountChange;
            if (e == null)
                return;

            if (e.ItemId == DataModel.NeedItemId)
            {
                DataModel.HaveItemNum += e.ChangeCount;
                RefreshRedPoint();
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "EquipInfo")
            {
                return InfoDatamodel;
            }
            return DataModel;  
        }

        public void Close()
        {
            EventDispatcher.Instance.DispatchEvent(new MaYaWuQiDestoryModel_Event());
        }

        private void BuyItem(int itemId, int buyCount)
        {
            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null || tbItem.StoreID < 0)
            {
                return;
            }

            var tbStore = Table.GetStore(tbItem.StoreID);
            if (tbStore == null)
            {
                return;
            }

            var cost = tbStore.NeedValue * buyCount;
            if (PlayerDataManager.Instance.GetRes(tbStore.NeedType) < cost)
            {
                var tbItemCost = Table.GetItemBase(tbStore.NeedType);
                //{0}不足！
                var str = GameUtils.GetDictionaryText(701);
                str = string.Format(str, tbItemCost.Name);
                GameUtils.ShowHintTip(str);
                PlayerDataManager.Instance.ShowItemInfoGet(tbStore.NeedType);

                if ((int)eResourcesType.GoldRes == tbStore.NeedType)
                {
                    //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ExchangeUI));
                    var e = new Show_UI_Event(UIConfig.WishingUI, new WishingArguments { Tab = 1});
                    EventDispatcher.Instance.DispatchEvent(e);
                }
                return;
            }

            NetManager.Instance.StartCoroutine(StoreBuyCoroutine(tbItem.StoreID, buyCount));
        }

        private IEnumerator StoreBuyCoroutine(int index, int count = 1)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.StoreBuy(index, count, -1);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var tbStore = Table.GetStore(index);
                        //购买成功
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(431));

                        if (tbStore == null)
                        {
                            yield break;
                        }

                        PlatformHelper.UMEvent("BuyItem", tbStore.Name.ToString(), count);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var e = new ShowUIHintBoard(430);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Error("StoreBuy....StoreId= {0}...ErrorCode...{1}", index, msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("StoreBuy............State..." + msg.State);
                }
            }
        }


        private void ViewSkill(int equipId)
        {
            var buffList = Table.GetBuffGroup(1000).BuffID;
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.BufferListUI, new UIInitArguments()
            //{
            //    Args = buffList
            //}));
            DataModel.BufferListData.BuffList.Clear();
            var enumorator = buffList.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var cell = new BuffInfoCell();
                cell.BuffId = enumorator.Current;
                cell.BuffLevel = 1;
                DataModel.BufferListData.BuffList.Add(cell);
            }
        }
        private void GotoAdvance(int equipId)
        {
        
        }

        private void BuyNeedItem(int itemId)
        {
            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
                return;

            var tbStore = Table.GetStore(tbItem.StoreID);
            if (tbStore == null)
                return;

            var haveItem = PlayerDataManager.GetInstance().GetItemCount(tbStore.NeedType);
            if (haveItem < tbStore.NeedValue)
            {
                var notice = string.Format(GameUtils.GetDictionaryText(100002179), tbStore.NeedValue);
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(notice));            
            }
            else
            {
                DataModel.ShowBuyBox = true;
                DataModel.MaxCount = haveItem / tbStore.NeedValue;
                DataModel.BuyCount = Math.Min(DataModel.MaxCount, 1);
                EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(1));
            }
        }

        private void OnEvent_Operate(IEvent ievent)
        {
            var e = ievent as ArtifactOpEvent;
            if (e == null)
                return;

            switch (e.Idx)
            {
                case 0:
                {
                    if (DataModel.CanBuy == false)
                        return;

                    if (DataModel.HaveItemNum >= DataModel.NeedItemNum)
                    { // 合成
                        //var tbItem = Table.GetItemBase(DataModel.NeedItemId);
                        //var notice = string.Format(GameUtils.GetDictionaryText(100002118), tbItem.Name);
                        //UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, notice, "", () =>
                        //{
                        //    BuyItem(DataModel.SelectEquipId, 1);
                        //});
                        GameUtils.GotoMaterialUI(DataModel.NeedItemId);
                    }
                    else
                    { // 购买碎片
                        BuyNeedItem(DataModel.NeedItemId);
                    }
                }
                    break;
                case 1:
                {
                    if (DataModel.ShowBufferList)
                    {
                        DataModel.ShowBufferList = false;
                        EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));
                    }
                    else
                    {
                        DataModel.ShowBufferList = true;
                        ViewSkill(DataModel.SelectEquipId);
                        EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(1));                    
                    }
                }
                    break;
                case 2:
                {
                    GotoAdvance(DataModel.SelectEquipId);
                }
                    break;
                case 3:
                {
                    DataModel.ShowBufferList = false;
                    EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));
                }
                    break;
                case 4:
                {
                    var have = false;
                    PlayerDataManager.Instance.ForeachEquip2(bagItem =>
                    {
                        if (bagItem.ItemId != -1)
                        {
                            var tbRecord = Table.GetEquipBase(bagItem.ItemId);
                            if (tbRecord != null && tbRecord.ShowEquip == 1)
                            {
                                have = true;
                                return false;
                            }
                        }
                        return true;
                    });

                    if (!have)
                    {
                        var __enumerator6 = (PlayerDataManager.Instance.GetBag((int)eBagType.Equip).Items).GetEnumerator();
                        while (__enumerator6.MoveNext())
                        {
                            var bagData = __enumerator6.Current;
                            if (bagData != null && bagData.ItemId != -1)
                            {
                                var tbRecord = Table.GetEquipBase(bagData.ItemId);
                                if (tbRecord != null && tbRecord.ShowEquip == 1)
                                {
                                    have = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (have)
                    {
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MyArtifactUI));
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002121));
                    }
                }
                    break;
                case 5:
                {
                    DataModel.ShowTips = true;
                    EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(1));
                }
                    break;
                case 6:
                {
                    DataModel.ShowTips = false;
                    EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));
                }
                    break;
                case 7:
                {
                    if (DataModel.ShowBufferList)
                    {
                        DataModel.ShowBufferList = false;
                        EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));                    
                    }
                    if (DataModel.ShowTips)
                    {
                        DataModel.ShowTips = false;
                        EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));                    
                    }
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ArtifactUi));
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipSkillTipUI));
                }
                    break;
                case 9:
                {
                    DataModel.ShowBuyBox = false;
                    EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));
                }
                    break;
                case 12:
                {
                    BuyItem(DataModel.NeedItemId, DataModel.BuyCount);
                    DataModel.ShowBuyBox = false;
                    EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));
                }
                    break;
                case 13:
                {
                    OnClickBuyInfoMax();
                }
                    break;
                case 14:
                {
                    OnClickBuyInfoAdd();
                }
                    break;
                case 15:
                {
                    OnClickBuyInfoDel();
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
        private void OnClickBuyInfoMax()
        {
            DataModel.BuyCount = DataModel.MaxCount;
        }
        private void OnClickBuyInfoAdd()
        {
            if (DataModel.BuyCount < DataModel.MaxCount)
            {
                DataModel.BuyCount++;
            }
        }
        private void OnClickBuyInfoDel()
        {
            if (DataModel.BuyCount > 1)
            {
                DataModel.BuyCount--;
            }
        }
        private bool CheckPressCount(bool isAdd)
        {
            if (isAdd)
            {
                if (DataModel.BuyCount < DataModel.MaxCount)
                {
                    DataModel.BuyCount++;
                    return true;
                }
            }
            else
            {
                if (DataModel.BuyCount > 1)
                {
                    DataModel.BuyCount--;
                    return true;
                }
            }
            return false;
        }
        private IEnumerator ButtonOnPressCoroutine(bool isAdd)
        {
            var pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(pressCd);
                if (CheckPressCount(isAdd) == false)
                {
                    NetManager.Instance.StopCoroutine(mPressTriger);
                    mPressTriger = null;
                    yield break;
                }
                if (pressCd > 0.01)
                {
                    pressCd = pressCd * 0.8f;
                }
            }
            yield break;
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
        private void SelectModel(int index)
        {
            if (index >= 0 && index < equipList.Count)
            {
                DataModel.SelectEquipId = equipList[index].Record.Id;
                DataModel.MaYaNameShow = index;
                EquipInFo(DataModel.SelectEquipId);
                DataModel.CanBuy = equipList[index].CanBuy;

                DataModel.NeedItemId = equipList[index].BuyNeedItemId;
                DataModel.HaveItemNum = PlayerDataManager.Instance.GetItemCount(DataModel.NeedItemId);
                DataModel.NeedItemNum = equipList[index].BuyCost;

                if (lastSelectIndex >= 0 && lastSelectIndex < DataModel.Models.Count && index != lastSelectIndex)
                {
                    DataModel.Models[lastSelectIndex].Select = false;
                }

                if (index >= 0 && index < DataModel.Models.Count)
                {
                    lastSelectIndex = index;
                    DataModel.Models[index].Select = true;
                }
            }        
        }

        private void OnEvent_SelectModel(IEvent ievent)
        {
            var e = ievent as ArtifactSelectEvent;
            if (e == null || e.ListItem == null)
                return;

            var index = e.ListItem.Index;
            SelectModel(index);

            //var modelDataModel = e.ListItem.Item as EquipModelDataModel;
        }

        public FrameState State { get; set; }

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

        private void EquipInFo(int itemId)
        {
            if (mIsInit)
            {
                InitStr();
            }

            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
            {
                return;
            }
            InfoDatamodel.BuffId = -1;
            var equipId = tbItem.Exdata[0];
            var tbEquip = Table.GetEquipBase(equipId);
            if (tbEquip != null)
            {
                InfoDatamodel.BuffLevel = tbEquip.AddBuffSkillLevel;

                if (tbEquip.BuffGroupId >= 0)
                {
                    var tbBuffGroup = Table.GetBuffGroup(tbEquip.BuffGroupId);
                    if (tbBuffGroup != null && tbBuffGroup.BuffID.Count == 1)
                    {
                        InfoDatamodel.BuffId = tbBuffGroup.BuffID[0];
                    }
                    else
                    {
                        InfoDatamodel.BuffId = 8999;
                    }
                }
            }

            if (tbEquip.FIghtNumDesc == -1)
            {
                InfoDatamodel.FightNum = "?????";
            }
            else
            {
                InfoDatamodel.FightNum = tbEquip.FIghtNumDesc.ToString();
            }

            if (tbEquip == null)
            {
                return;
            }
            InfoDatamodel.ItemId = itemId;
            InfoDatamodel.EquipId = equipId;
            InfoDatamodel.EnchanceLevel = 0;


            InfoDatamodel.CanUseLevel = PlayerDataManager.Instance.GetLevel() < tbItem.UseLevel ? 1 : 0;
            //职业符合不？
            if (tbEquip.Occupation != -1)
            {
                InfoDatamodel.CanRole = PlayerDataManager.Instance.GetRoleId() == tbEquip.Occupation ? 0 : 1;
            }
            var strDic = GameUtils.GetDictionaryText(230004);

            InfoDatamodel.PhaseDesc = string.Format(strDic, GameUtils.NumEntoCh(tbEquip.Ladder));

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
                        InfoDatamodel.NeedAttr[i] = string.Format("[FF0000]{0}[-]", needStr);
                    }
                    else
                    {
                        InfoDatamodel.NeedAttr[i] = string.Format("[00FF00]{0}[-]", needStr);
                    }
                }
                else
                {
                    InfoDatamodel.NeedAttr[i] = "";
                }
            }

            var enchanceLevel = InfoDatamodel.EnchanceLevel;

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
                    GameUtils.SetAttribute(InfoDatamodel.BaseAttr, i, nAttrId, baseValue, changeValue);
                }
                else
                {
                    InfoDatamodel.BaseAttr[i].Reset();
                }
            }
            for (var i = 0; i < 4; i++)
            {
                var attrData = InfoDatamodel.BaseAttr[i];
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
                            //rDic = "{0}+:{1}[00ff00](+{2})[-]-{3}[00ff00](+{4})[-]";
                            strDic = StrDic230034;
                            InfoDatamodel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrChange, attrValueEx,
                                attrChangeEx);
                        }
                        else
                        {
                            var attrValueEx = GameUtils.AttributeValue(nAttrId, attrData.ValueEx);
                            //strDic = "{0}+:{1}-{2}";
                            strDic = StrDic230033;
                            InfoDatamodel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrValueEx);
                        }
                    }
                    else
                    {
                        if (attrData.Change != 0 || attrData.ChangeEx != 0)
                        {
                            var attrChange = GameUtils.AttributeValue(nAttrId, attrData.Change);
                            //strDic = "{0}+:{1}[00ff00](+{2})[-]";
                            strDic = StrDic230032;
                            InfoDatamodel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrChange);
                        }
                        else
                        {
                            //strDic = "{0}+:{1}";
                            strDic = StrDic230025;
                            InfoDatamodel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue);
                        }
                    }
                }
                else
                {
                    InfoDatamodel.BaseAttrStr[i] = "";
                }
            }

            strDic = StrDic230025;
            //strDic = "{0}+:{1}";
            for (var i = 0; i != 2; ++i)
            {
                var nAttrId = tbEquip.BaseFixedAttrId[i];
                if (nAttrId != -1)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    var attrValue = GameUtils.AttributeValue(nAttrId, tbEquip.BaseFixedAttrValue[i]);
                    InfoDatamodel.AddAttrStr[i] = string.Format(strDic, attrName, attrValue);
                }
                else
                {
                    InfoDatamodel.AddAttrStr[i] = "";
                }
            }

            //灵魂、卓越、字符串显示
            InfoDatamodel.StrExcellent = "";
            InfoDatamodel.StrSoul = "";
            var min = 0;
            var minbool = false;
            var max = 0;

            if (tbEquip.RandomAttrCount != -1)
            {
                var tbEquipRalate = Table.GetEquipRelate(tbEquip.RandomAttrCount);
                if (tbEquipRalate == null)
                {
                    return;
                }
                min = 0;
                minbool = false;
                max = 0;
                for (var i = 0; i < tbEquipRalate.AttrCount.Length; i++)
                {
                    if (tbEquipRalate.AttrCount[i] > 0)
                    {
                        max = i;
                        if (!minbool)
                        {
                            minbool = true;
                            min = i;
                        }
                    }
                }
                if (min != 0)
                {
                    if (tbEquip.LingHunDescId == -1)
                    {
                        if (min == max)
                        {
                            InfoDatamodel.StrSoul = min + GameUtils.GetDictionaryText(300839); //"条随机属性";
                        }
                        else
                        {
                            InfoDatamodel.StrSoul = string.Format("{0}-{1}" + GameUtils.GetDictionaryText(300839), min, max);
                        }
                        InfoDatamodel.SouleHeight = 16;
                    }
                    else
                    {
                        //InfoDatamodel.StrSoul = GameUtils.GetDictionaryText(tbEquip.LingHunDescId); //"条随机属性";
                        //string[] subts = InfoDatamodel.StrExcellent.Split('\n');                   
                        //InfoDatamodel.SouleHeight = (subts.Length) * 16;

                        var i = 0;
                        var str = GameUtils.GetDictionaryText(tbEquip.LingHunDescId); //"条随机属性";
                        string[] subts = str.Split('\n');
                        foreach (var item in subts)
                        {
                            InfoDatamodel.MaYaAttr[i] = item;
                            i++;
                        }
                    }
                }
            }
        }

        private void UpdateEquioNum()
        {
            var weaponMainId = PlayerDataManager.Instance.GetEquipData(eEquipType.WeaponMain).ItemId;
            for (int i = 0; i < equipList.Count; i++)
            {
                var equipCount1 = PlayerDataManager.Instance.GetItemCount(equipList[i].Record.Id);
                if (equipCount1 > 0 || weaponMainId == equipList[i].Record.Id)
                {
                    if (list.Count == 0)
                    {
                        num = 0;
                        list.Add(equipList[0].Record.Id);
                        var itemDm = new ItemIdDataModel();
                        itemDm.ItemId = equipList[1].Record.Id;
                        itemDm.Count = 0;

                        var dm = new EquipModelDataModel();
                        dm.EquipId = equipList[1].Record.Id;
                        dm.Select = false;

                        DataModel.WeaponItems.Add(itemDm);
                        DataModel.Models.Add(dm);
                    }
                    else if (equipList[i].Record.Id > list[list.Count - 1])
                    {
                        if (i < 2)
                        {
                            list.Add(equipList[i].Record.Id);
                            var itemDm = new ItemIdDataModel();
                            itemDm.ItemId = equipList[i+1].Record.Id;
                            itemDm.Count = 0;

                            var dm = new EquipModelDataModel();
                            dm.EquipId = equipList[i+1].Record.Id;
                            dm.Select = false;

                            DataModel.WeaponItems.Add(itemDm);
                            DataModel.Models.Add(dm);
                        }                    
                    }
                }
            }
        }
    }
}