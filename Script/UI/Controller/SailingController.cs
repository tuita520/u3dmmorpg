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
using UnityEngine;

#endregion

namespace ScriptController
{
    public class SailingController : IControllerBase
    {
        private static List<Vector2> staticPos = new List<Vector2>(); //港口坐标

        public SailingController()
        {
            #region 勋章初始化

            MedalType = new Dictionary<int, string>();
            MedalType.Add(0, GameUtils.GetDictionaryText(230201));
            MedalType.Add(1, GameUtils.GetDictionaryText(230202));
            MedalType.Add(2, GameUtils.GetDictionaryText(230203));
            MedalType.Add(3, GameUtils.GetDictionaryText(230204));
            MedalType.Add(4, GameUtils.GetDictionaryText(230205));
            MedalType.Add(5, GameUtils.GetDictionaryText(230206));
            MedalType.Add(6, GameUtils.GetDictionaryText(230207));
            MedalType.Add(7, GameUtils.GetDictionaryText(230208));
            MedalType.Add(8, GameUtils.GetDictionaryText(230209));
            MedalType.Add(9, GameUtils.GetDictionaryText(230210));
            MedalType.Add(10, GameUtils.GetDictionaryText(230211));
            MedalType.Add(11, GameUtils.GetDictionaryText(230212));
            MedalType.Add(12, GameUtils.GetDictionaryText(230213));
            MedalType.Add(13, GameUtils.GetDictionaryText(230214));

            #endregion

            for (var i = 0; i < 12; i += 2)
            {
                var tbClientx = Table.GetClientConfig(550 + i);
                var tbClienty = Table.GetClientConfig(551 + i);
                var x = float.Parse(tbClientx.Value);
                var y = float.Parse(tbClienty.Value);
                staticPos.Add(new Vector2(x, y));
            }

            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingReturnBtn.EVENT_TYPE, OnClickReturnBtn); //返回按钮
            EventDispatcher.Instance.AddEventListener(UIEvent_Sail_ExdataUpdate.EVENT_TYPE, OnExdataUpdate); //返回按钮
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingPackItemUI.EVENT_TYPE, OnClickPackItem); //点击背包物品
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingPickAll.EVENT_TYPE, PickAll); //捡起所有的掉落船饰        
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingPickOne.EVENT_TYPE, PickOne); //捡起单个掉落船饰
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingPutOnClick.EVENT_TYPE, PutOnClick); //穿上船饰
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingLightPoint.EVENT_TYPE, LightPointClick); //炼金一次
            EventDispatcher.Instance.AddEventListener(UIEvent_CityUpdateBuilding.EVENT_TYPE, UpdateBuilding); //更新建筑数据
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingOperation.EVENT_TYPE, SailingOperation); //勇士港操作事件
            EventDispatcher.Instance.AddEventListener(CityDataInitEvent.EVENT_TYPE, OnCityDataInit); // 初始化农场数据
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingLightPointAccess.EVENT_TYPE, LightPointAccessClick); // 直达
            EventDispatcher.Instance.AddEventListener(CityWeakNoticeRefreshEvent.EVENT_TYPE, CityWeakNoticeRefresh);
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingCheckRedPoint.EVENT_TYPE, CheckRedPoint);
            EventDispatcher.Instance.AddEventListener(ShowPreviewUIEvent.EVENT_TYPE, OnShowPreviewUIEvent);
            EventDispatcher.Instance.AddEventListener(MedalCellClcikEvent.EVENT_TYPE, OnClickDedalCellEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SailingCheckType.EVENT_TYPE, UIEvent_SailingCheckTypeCallBack);
            EventDispatcher.Instance.AddEventListener(SailingShowMessageBoxEvent.EVENT_TYPE, OnSailingShowMessageBoxEvent);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChangedEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInit);
        }


        private List<int> BaglevelUpList = new List<int>();
        private BuildingData BraveHarborBuild;
        private bool CanSailing;
        private int LeftScanCount = 0;
        private Coroutine mAutoShipCoroutine;
        private Dictionary<int, string> MedalType; //勋章类型
        private int mSelectIndex;
        private int mSelectType = (int)SelectType.BagSelect; //背包选择或者装备选择，为升级显示用
        private object NoticeTriggerr;
        private int ScanCount;
        private double ScanSpeed = 0.06; //设置船速系数
        private string ScanTips270249;
        private string ScanTips270250;
        private MedalItemDataModel SelectedItem; //设置被操作的勋章
        private List<int> TempBagLevelUplist = new List<int>();
        private int EatAllInitExp;
        private int OldValue;
        private int NewValue;
        //选择类型，升级用
        private enum SelectType
        {
            BagSelect, //背包选择
            EquipSelect //装备选择
        }

        private int ActionCount { get; set; } //船所走的步数
        private int ActionIndex { get; set; } //船所在的航线index
        private int DataState { get; set; } //船航行的状态：无-1，0正常行驶，购买1，扫荡10
        private bool IsScan { get; set; } //是否在扫荡
        private SailingDataModel SailingData { get; set; }
        //升级物品list创建
        private void AddBagSelectList()
        {
            SailingData.TempData.TotalExp = 0;
            var exp = 0;
            BaglevelUpList.Clear();
            TempBagLevelUplist.Clear();

            var tempitem = SailingData.TempData.TempMedalItem;
            if (tempitem.BaseItemId == -1)
            {
                return;
            }
            var MedalTable = Table.GetMedal(tempitem.BaseItemId);

            exp = tempitem.nExp + GetTolalExp(MedalTable.LevelUpExp, tempitem.nLevel);
            var totalExp = GetTolalExp(MedalTable.LevelUpExp, MedalTable.MaxLevel);

            var bagCount = SailingData.ShipEquip.BagItem.Count;
            for (var i = 0; i < bagCount; i++)
            {
                var item = SailingData.ShipEquip.BagItem[i];
                if (item.IsShowCheck == 1)
                {
                    if (item.BaseItemId == -1)
                    {
                        continue;
                    }
                    var ItemBaseTable2 = Table.GetItemBase(item.BaseItemId);
                    var MedalTable2 = Table.GetMedal(ItemBaseTable2.Exdata[0]);

                    exp += MedalTable2.InitExp;
                    exp += item.nExp;
                    exp += GetTolalExp(MedalTable2.LevelUpExp, item.nLevel);
                    BaglevelUpList.Add(i);
                    if (exp > totalExp)
                    {
                        //满经验
                    }
                }
            }
            SailingData.TempData.TotalExp = exp;
        }

        //吞噬临时包裹的list创建
        private void AddTempBagSelectList(int BestIndex)
        {
            SailingData.TempData.TotalExp = 0;
            var Exp = 0;
            TempBagLevelUplist.Clear();

            var tempitem = SailingData.ShipEquip.DropItem[BestIndex];
            var MedalTable = Table.GetMedal(tempitem.BaseItemId);

            Exp += tempitem.nExp;
            Exp += GetTolalExp(MedalTable.LevelUpExp, tempitem.nLevel);

            var totalExp = GetTolalExp(MedalTable.LevelUpExp, MedalTable.MaxLevel + 1);

            var dropCount = SailingData.ShipEquip.DropItem.Count;
            for (var i = 0; i < dropCount; i++)
            {
                var item = SailingData.ShipEquip.DropItem[i];
                if (item.BaseItemId == -1 || i == BestIndex)
                {
                    continue;
                }
                var MedalTable2 = Table.GetMedal(item.BaseItemId);
                Exp += MedalTable2.InitExp;
                Exp += item.nExp;
                Exp += GetTolalExp(MedalTable2.LevelUpExp, item.nLevel);
                TempBagLevelUplist.Add(i);
                if (SailingData.TempData.TotalExp > totalExp)
                {
                    //满经验
                    SailingData.TempData.TotalExp = Exp;
                    return;
                }
            }
            SailingData.TempData.TotalExp = Exp;
        }

        private void AnalyseNotice()
        {
            //var diff = -1;
            //CanSailing = false;
            //var isNotice = false;
            //{
            //    var __list6 = BraveHarborBuild.Exdata;
            //    var __listCount6 = __list6.Count;
            //    var index = 0;
            //    for (var __i6 = 0; __i6 < __listCount6; ++__i6)
            //    {
            //        var i = __list6[__i6];
            //        {
            //            if (index > 4)
            //            {
            //                break;
            //            }
            //            if (i == 2 || i == 3 || i == 12)
            //            {
            //                var overTime = BraveHarborBuild.Exdata64[index];
            //                if (overTime == 0)
            //                {
            //                    index++;
            //                    continue;
            //                }
            //                var okTime =
            //                    (int) (Extension.FromServerBinary(overTime) - Game.Instance.ServerTime).TotalSeconds;
            //                if (okTime <= 0)
            //                {
            //                    isNotice = true;
            //                    break;
            //                }
            //                if (okTime < diff || diff == -1)
            //                {
            //                    diff = okTime;
            //                }
            //            }
            //            if (i == 10)
            //            {
            //                CanSailing = true;
            //            }
            //            index++;
            //        }
            //    }
            //}
            //var tbBuilding = Table.GetBuilding(BraveHarborBuild.TypeId);
            //if (tbBuilding != null)
            //{
            //    var tbBuildingService = Table.GetBuildingService(tbBuilding.ServiceId);
            //    PlayerDataManager.Instance.NoticeData.SailingIco = tbBuildingService.TipsIndex;
            //}
            //PlayerDataManager.Instance.NoticeData.SailingNotice = isNotice;
            //EventDispatcher.Instance.DispatchEvent(new CityBulidingNoticeRefresh(BraveHarborBuild));
            //if (isNotice)
            //{
            //    return;
            //}
            //if (NoticeTriggerr != null)
            //{
            //    TimeManager.Instance.DeleteTrigger(NoticeTriggerr);
            //    NoticeTriggerr = null;
            //}

            //NoticeTriggerr = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1 + diff), () =>
            //{
            //    if (NoticeTriggerr != null)
            //    {
            //        AnalyseNotice();
            //        EventDispatcher.Instance.DispatchEvent(new CityBulidingNoticeRefresh(BraveHarborBuild));
            //        TimeManager.Instance.DeleteTrigger(NoticeTriggerr);
            //        NoticeTriggerr = null;
            //    }
            //});
        }

        //扫荡七海
        private void AutoShipClick(int isAuto)
        {
            if (SailingData.IsAuto == isAuto)
                return;
            SailingData.IsAuto = isAuto;
            if (isAuto == 1)
                NetManager.Instance.StartCoroutine(LightPointClickCoroutine());
        }

        //自动扫荡Coroutine
        private IEnumerator AutoShipClickCoroutine(int count)
        {
            //---temp
            //var instance = PlayerDataManager.Instance;
            //;
            //var scanDistance = PlayerDataManager.Instance.GetExData(eExdataDefine.e71);
            ////int mScanCount = 0;
            //for (var i = 0; i < count; i++)
            //{
            //    for (var index = 4; index >= 0; --index)
            //    {
            //        var oldValue = BraveHarborBuild.Exdata[index];
            //        //不可点
            //        if (oldValue == 0)
            //        {
            //            continue;
            //        }
            //        //正在航行
            //        var needmoney = 0;
            //        var tbSailing = Table.GetSailing(index);
            //        if (scanDistance >= instance.TbVip.SailScanCount)
            //        {
            //            IsScan = false;
            //            SetScanName(IsScan);
            //            mAutoShipCoroutine = null;
            //        }
            //        if (oldValue%10 == 2)
            //        {
            //            //判断钱是否够
            //            needmoney = GetBuyMoney(index, tbSailing.distanceParam);
            //            var resCount = PlayerDataManager.Instance.GetRes(tbSailing.ConsumeType);
            //            if (needmoney < 0 || resCount < needmoney)
            //            {
            //                var ee = new ShowUIHintBoard(270226);
            //                EventDispatcher.Instance.DispatchEvent(ee);
            //                PlayerDataManager.Instance.ShowItemInfoGet(tbSailing.ConsumeType);
            //                IsScan = false;
            //                SetScanName(IsScan);
            //                mAutoShipCoroutine = null;
            //                yield break;
            //            }

            //            SailingData.Ship[index].Times = "";
            //            var prob = GetShipPercent(index);
            //            if (prob > 0 && prob < 1)
            //            {
            //                DataState = 1;
            //                ActionIndex = index;
            //                ActionCount = 0;
            //                while (DataState == 1)
            //                {
            //                    if (!IsScan)
            //                    {
            //                        StopShipScan(index);
            //                        mAutoShipCoroutine = null;
            //                        yield break;
            //                    }
            //                    yield return new WaitForSeconds(0.05f);
            //                }
            //            }
            //        }
            //        else
            //        //可航行
            //        {
            //            //判断钱是否够
            //            if (tbSailing == null)
            //            {
            //                mAutoShipCoroutine = null;
            //                yield break;
            //            }
            //            needmoney = tbSailing.Distance/tbSailing.distanceParam;
            //            var resCount = PlayerDataManager.Instance.GetRes(tbSailing.ConsumeType);
            //            if (needmoney < 0 || resCount < needmoney)
            //            {
            //                var ee = new ShowUIHintBoard(270226);
            //                EventDispatcher.Instance.DispatchEvent(ee);
            //                PlayerDataManager.Instance.ShowItemInfoGet(tbSailing.ConsumeType);
            //                IsScan = false;
            //                SetScanName(IsScan);
            //                mAutoShipCoroutine = null;
            //                yield break;
            //            }

            //            SailingData.States[index] = 2;
            //            SailingData.Ship[index].IsShowShip = true;
            //            SailingData.Ship[index].Times = " ";
            //            DataState = 10;
            //            ActionIndex = index;
            //            ActionCount = 0;
            //            BraveHarborBuild.Exdata[index] = 0;
            //            SetScanName(IsScan);
            //            while (DataState == 10)
            //            {
            //                if (!IsScan)
            //                {
            //                    StopShipScan(index);
            //                    mAutoShipCoroutine = null;
            //                    yield break;
            //                }
            //                yield return new WaitForSeconds(0.05f);
            //            }
            //        }

            //        //网络包
            //        var tbBuild = Table.GetBuilding(BraveHarborBuild.TypeId);
            //        var array = new Int32Array();
            //        array.Items.Add(3);
            //        //扫荡第一次，发送1修改服务器exdata + 1
            //        //if (mScanCount == 0)
            //        //{
            //        //    array.Items.Add(1);
            //        //}
            //        //else
            //        //{   
            //        //    array.Items.Add(0);
            //        //}
            //        using (new BlockingLayerHelper(0))
            //        {
            //            var msg = NetManager.Instance.UseBuildService(BraveHarborBuild.AreaId, tbBuild.ServiceId, array);
            //            yield return msg.SendAndWaitUntilDone();
            //            if (msg.State == MessageState.Reply)
            //            {
            //                if (msg.ErrorCode == (int) ErrorCodes.OK)
            //                {
            //                    // mScanCount++;
            //                    var result = msg.Response.Data32[0];
            //                    var resCount = PlayerDataManager.Instance.GetRes(tbSailing.ConsumeType);
            //                    PlayerDataManager.Instance.PlayerDataModel.Bags.Resources[tbSailing.ConsumeType] =
            //                        resCount - needmoney;
            //                    scanDistance += tbSailing.Distance*10;
            //                    if (result > 0)
            //                    {
            //                        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingFlyAnim(index,
            //                            tbSailing.SuccessGetExp));
            //                        BraveHarborBuild.Exdata[result] = BraveHarborBuild.Exdata[result]%10 + 10;
            //                    }
            //                    else
            //                    {
            //                        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingFlyAnim(index,
            //                            tbSailing.FailedGetExp));
            //                    }
            //                    if (index == 0)
            //                    {
            //                        BraveHarborBuild.Exdata[index] = 10;
            //                    }
            //                    else
            //                    {
            //                        BraveHarborBuild.Exdata[index] = BraveHarborBuild.Exdata[index]/10*10;
            //                    }
            //                    if (index != 4)
            //                    {
            //                        SailingData.Ship[index + 1].Posion = new Vector3(staticPos[index + 1].x,
            //                            staticPos[index + 1].y, 0);
            //                    }

            //                    RefeshBuildData(BraveHarborBuild);
            //                    if (!IsScan)
            //                    {
            //                        StopShipScan(index);
            //                        mAutoShipCoroutine = null;
            //                        yield break;
            //                    }
            //                }
            //                else if (msg.ErrorCode == (int) ErrorCodes.MoneyNotEnough)
            //                {
            //                    var ee = new ShowUIHintBoard(210102);
            //                    EventDispatcher.Instance.DispatchEvent(ee);
            //                    StopShipScan(index);
            //                    mAutoShipCoroutine = null;
            //                    yield break;
            //                }
            //                else
            //                {
            //                    UIManager.Instance.ShowNetError(msg.ErrorCode);
            //                    StopShipScan(index);
            //                    mAutoShipCoroutine = null;
            //                    yield break;
            //                }
            //            }
            //            else
            //            {
            //                var e = new ShowUIHintBoard(220821);
            //                EventDispatcher.Instance.DispatchEvent(e);
            //                StopShipScan(index);
            //                mAutoShipCoroutine = null;
            //                yield break;
            //            }
            //            break;
            //        }
            //    }
            //}
            //IsScan = false;
            //SetScanName(IsScan);
            //mAutoShipCoroutine = null;
            yield break;
        }

        private void SetShipSpeedOrGet(int nIndex, int nStat)
        {
            //--temp
            //if (nIndex < SailingData.Ship.Count)
            //{
            //    SailingData.Ship[nIndex].SpeedOrGet = nStat;
            //    CheckRedPoint(new UIEvent_SailingCheckRedPoint(0));
            //}
        }
        //购买到港
        private IEnumerator BuyShipCoroutine(int nIndex, int money)
        {
            //--temp
            //动画
            //var prob = GetShipPercent(nIndex);
            //SailingData.Ship[nIndex].Times = "";
            //SetShipSpeedOrGet(nIndex, 0);
            //// SailingData.AddSpeed[nIndex] = "";
            //if (prob > 0 && prob < 1)
            //{
            //    DataState = 1;
            //    ActionIndex = nIndex;
            //    ActionCount = 0;
            //    while (DataState == 1)
            //    {
            //        yield return new WaitForSeconds(0.05f);
            //    }
            //}

            ////网络包
            //var tbBuild = Table.GetBuilding(BraveHarborBuild.TypeId);
            //var array = new Int32Array();
            //array.Items.Add(2);
            //array.Items.Add(nIndex);
            //array.Items.Add(money);
            //using (new BlockingLayerHelper(0))
            //{
            //    var msg = NetManager.Instance.UseBuildService(BraveHarborBuild.AreaId, tbBuild.ServiceId, array);
            //    yield return msg.SendAndWaitUntilDone();
            //    if (msg.State == MessageState.Reply)
            //    {
            //        if (msg.ErrorCode == (int) ErrorCodes.OK)
            //        {
            //            var result = msg.Response.Data32[0];
            //            var tbSailing = Table.GetSailing(nIndex);
            //            if (result > 0)
            //            {
            //                BraveHarborBuild.Exdata[result] = BraveHarborBuild.Exdata[result]%10 + 10;
            //                EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingFlyAnim(nIndex,
            //                    tbSailing.SuccessGetExp));
            //                //成功
            //                var ee = new ShowUIHintBoard(230217);
            //                EventDispatcher.Instance.DispatchEvent(ee);
            //            }
            //            else
            //            {
            //                //失败
            //                var ee = new ShowUIHintBoard(230218);
            //                EventDispatcher.Instance.DispatchEvent(ee);
            //                EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingFlyAnim(nIndex, tbSailing.FailedGetExp));
            //            }
            //            if (nIndex == 0)
            //            {
            //                BraveHarborBuild.Exdata[nIndex] = 10;
            //            }
            //            else
            //            {
            //                BraveHarborBuild.Exdata[nIndex] = BraveHarborBuild.Exdata[nIndex]/10*10;
            //            }
            //            RefeshBuildData(BraveHarborBuild);
            //        }
            //        else
            //        {
            //            UIManager.Instance.ShowNetError(msg.ErrorCode);
            //        }
            //    }
            //    else
            //    {
            //        var e = new ShowUIHintBoard(220821);
            //        EventDispatcher.Instance.DispatchEvent(e);
            //    }
            //}
            //DataState = 0;
            yield break;
        }

        //计算总船坞属性加成
        private void CalculateItemProp()
        {
            var PropDict = new Dictionary<int, int>();
            var myValue = 0;
            {
                // foreach(var item in SailingData.ShipEquip.EquipItem)
                var __enumerator3 = (SailingData.ShipEquip.EquipItem).GetEnumerator();
                while (__enumerator3.MoveNext())
                {
                    var item = __enumerator3.Current;
                    {
                        if (item.BaseItemId != -1)
                        {
                            var tbMedal = Table.GetMedal(item.BaseItemId);
                            var tbMedalAddPropIDLength12 = tbMedal.AddPropID.Length;
                            for (var i = 0; i < tbMedalAddPropIDLength12; i++)
                            {
                                if (tbMedal.PropValue[i] != -1)
                                {
                                    var tbProp = Table.GetSkillUpgrading(tbMedal.PropValue[i]);
                                    myValue = tbProp.GetSkillUpgradingValue(item.nLevel);
                                    if (PropDict.ContainsKey(tbMedal.AddPropID[i]))
                                    {
                                        PropDict[tbMedal.AddPropID[i]] += myValue;
                                    }
                                    else
                                    {
                                        PropDict.Add(tbMedal.AddPropID[i], myValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //int attrCount =  SailingData.TempData.AttributesAll.Count;
            //for (int i = 0; i < attrCount; i++)
            //{
            //    SailingData.TempData.AttributesAll[i].Type = -1;
            //}
            SailingData.TempData.AttributesAll.Clear();
            var attrList = new List<AttributeChangeDataModel>();
            if (PropDict.Count != 0)
            {
                var i = 0;
                {
                    // foreach(var kvp in PropDict)
                    var __enumerator4 = (PropDict).GetEnumerator();
                    while (__enumerator4.MoveNext())
                    {
                        var kvp = __enumerator4.Current;
                        {
                            var item = new AttributeChangeDataModel();
                            item.Type = kvp.Key;
                            item.Value = kvp.Value;
                            attrList.Add(item);
                        }
                    }
                }
            }
            SailingData.TempData.AttributesAll = new ObservableCollection<AttributeChangeDataModel>(attrList);
        }

        private void CityWeakNoticeRefresh(IEvent ievent)
        {
            if (BraveHarborBuild == null)
            {
                return;
            }
            PlayerDataManager.Instance.WeakNoticeData.Sailing = CanSailing;
            EventDispatcher.Instance.DispatchEvent(new CityBulidingWeakNoticeRefresh(BraveHarborBuild));
        }

        //关闭tick
        private void CloseTick()
        {
            if (IsScan)
            {
                IsScan = false;
                SetScanName(IsScan);
            }
            if (mAutoShipCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(mAutoShipCoroutine);
                mAutoShipCoroutine = null;
            }
            // BraveHarborBuild = null;
        }

        private void CloseWoodTips()
        {
            SailingData.IsShowWoodTip = false;
        }

        private void UIEvent_SailingCheckTypeCallBack(IEvent iEvent)
        {
            if (iEvent != null)
            {
                var ie = iEvent as UIEvent_SailingCheckType;
                if (ie != null)
                {
                    if (ie.CheckBox.Count == 0) return;
                    int tempInitExp = 0;
                    for (int i = 0; i < ie.CheckBox.Count; i++)
                    {
                        if (ie.CheckBox[i])
                        {
                            switch (i)
                            {
                                case 0://符文经验                          
                                    tempInitExp += GetBagInitExpByColor(13);
                                    break;
                                case 1://绿色
                                    tempInitExp += GetBagInitExpByColor(1);
                                    break;
                                case 2://蓝色
                                    tempInitExp += GetBagInitExpByColor(2);
                                    break;
                                case 3://紫色
                                    tempInitExp += GetBagInitExpByColor(3);
                                    break;
                            }
                        }
                    }
                    EatAllInitExp = tempInitExp;
                }
            }
        }

        private int GetBagExpByLevel(MedalItemDataModel data)
        {
            int Ext = 0;
            var tbMedal = Table.GetMedal(data.BaseItemId);
            for (int i = 1; i < data.nLevel; i++)
            {
                var needExp = Table.GetSkillUpgrading(tbMedal.LevelUpExp).GetSkillUpgradingValue(i);
                Ext += needExp;
            }                          
            return Ext;
        }

        private int GetBagInitExpByColor(int color)
        {
            int InitExt = 0;
            var enumerator = SailingData.ShipEquip.BagItem.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var curr = enumerator.Current;
                if(curr.BaseItemId == -1)continue;            
                var tbMedal = Table.GetMedal(curr.BaseItemId);
                if (tbMedal == null)
                {
                    Logger.Error("GetMedal is Null");
                    return InitExt;
                }
                if (tbMedal.MedalType == color && tbMedal.Quality == 0)//经验符文
                {
                    InitExt += tbMedal.InitExp;             
                }
                else if (tbMedal.Quality == color)
                {
                    if (curr.nLevel > 1)
                    {
                        InitExt += GetBagExpByLevel(curr);  
                    }
                    InitExt += tbMedal.InitExp;    
                }
            }
            return InitExt;
        }


        private void EatAll(int flag)
        {
            bool bShow = false;
            if (SailingData.ShipEquip.BagItem.Count == 0)
                return;

            if ((1 << 3 & flag) > 0)
            {
                foreach (var item in SailingData.ShipEquip.BagItem)
                {
                    var tb = Table.GetItemBase(item.BaseItemId);
                    if (tb != null)
                    {
                        if (tb.Color == 3)
                        {
                            bShow = true;
                            break;
                        }
                    }
                }
            }


            if (bShow)
            {
                var str = string.Format(GameUtils.GetDictionaryText(230216), EatAllInitExp);
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "",
                    () =>
                    {
                        NetManager.Instance.StartCoroutine(SplitMedal((int)eBagType.MedalBag, -1, flag));
                    });
            }
            else
            {
                NetManager.Instance.StartCoroutine(SplitMedal((int)eBagType.MedalBag, -1, flag));

            }


            //var TempItemCount = args[0];
            //var QualityCount = args[1]; ///品质>=紫色的个数

            //AddTempBagSelectList(BestIndex);
            //if (TempBagLevelUplist.Count > 0)
            //{
            //    var tempCount = TempBagLevelUplist.Count;
            //    for (var i = 0; i < tempCount; i++)
            //    {
            //        tempBagList.Items.Add(TempBagLevelUplist[i]);
            //    }
            //    if (QualityCount > 1)
            //    {
            //        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 230216, "",
            //            () =>
            //            {
            //                NetManager.Instance.StartCoroutine(LevelUpCoroutine((int)eBagType.MedalTemp, BestIndex,
            //                    tempBagList, bagList));
            //            });
            //    }
            //    else
            //    {
            //        NetManager.Instance.StartCoroutine(LevelUpCoroutine((int)eBagType.MedalTemp, BestIndex, tempBagList,
            //            bagList));
            //    }
            //}
        }

        private IEnumerator SplitMedal(int bagId, int idx, int flag)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.SplitMedal(bagId, idx, flag);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (SailingData.TempData.TempMedalItem.BagId == bagId)
                        {
                            var tbMedal = Table.GetMedal(SailingData.TempData.TempMedalItem.BaseItemId);
                            if (tbMedal != null)
                            {
                                if (SailingData.TempData.TempMedalItem.Index == idx || (0 < (flag & (1 << tbMedal.Quality))))
                                {
                                    SetListSelectBg();
                                    SailingData.TempData.TempMedalItem.BaseItemId = -1;
                                    SailingData.TempData.TempMedalItem.ItemName = string.Empty;
                                }
                            }

                        }
                    }
                    else
                    {
                        if (msg.ErrorCode == (int)ErrorCodes.Error_Runr_Not_Resolve)
                        {
                            var e = new ShowUIHintBoard(200009200);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
            yield break;
        }

        //寻找最好品质的临时背包物品
        private int FindBestMedal(int[] TempItemCount_QualityCount) //ref int TempItemCount,ref int QualityCount
        {
            var tempQuality = -1;
            var index = -1;
            if (SailingData.ShipEquip.DropItem.Count != 0)
            {
                {
                    // foreach(var item in SailingData.ShipEquip.DropItem)
                    var __enumerator1 = (SailingData.ShipEquip.DropItem).GetEnumerator();
                    while (__enumerator1.MoveNext())
                    {
                        var item = __enumerator1.Current;
                        {
                            if (item.BaseItemId != -1)
                            {
                                TempItemCount_QualityCount[0]++;
                                var tbMedal = Table.GetMedal(item.BaseItemId);
                                if (tbMedal.Quality > tempQuality)
                                {
                                    tempQuality = tbMedal.Quality;
                                    index = item.Index;
                                }
                                if (tbMedal.Quality > 2)
                                {
                                    TempItemCount_QualityCount[1]++;
                                }
                            }
                        }
                    }
                }
            }

            return index;
        }


        //计算经验总和
        private int GetTolalExp(int index, int level)
        {
            var exp = 0;
            var tbProp = Table.GetSkillUpgrading(index);
            if (tbProp == null)
            {
                return 0;
            }
            for (var i = 1; i < level; ++i)
            {
                exp += tbProp.GetSkillUpgradingValue(i);
            }
            return exp;
        }

        //-----------------------------------主界面显示功能-----------------------

        //初始化界面数据
        private void InitData()
        {
            if (SelectedItem != null)
            {
                SelectedItem.Selected = 0;
            }
            var tbBuilding = Table.GetBuilding(BraveHarborBuild.TypeId);
            var tbBuildingServer = Table.GetBuildingService(tbBuilding.ServiceId);

            var SailingDataShipEquipEquipItemCount22 = SailingData.ShipEquip.EquipItem.Count;
            int lvel = PlayerDataManager.Instance.GetLevel();
            for (var i = 0; i < SailingDataShipEquipEquipItemCount22; i++)
            {
                SailingData.ShipEquip.EquipItem[i].PlayEffect = false;
                var limit = Table.GetClientConfig(1106 + i).ToInt();
                if (lvel >= limit)
                {
                    SailingData.ShipEquip.EquipItem[i].IsLock = 0;
                    continue;
                }
                SailingData.ShipEquip.EquipItem[i].IsLock = 1;
            }
            /*
        for (var i = 0; i < SailingDataShipEquipEquipItemCount22; i++)
        {
            if (i < tbBuildingServer.Param[2])
            {
                SailingData.ShipEquip.EquipItem[i].IsLock = 0;
                continue;
            }
            SailingData.ShipEquip.EquipItem[i].IsLock = 1;
        }
         * */
            {
                // foreach(var item in SailingData.ShipEquip.BagItem)
                var __enumerator21 = (SailingData.ShipEquip.BagItem).GetEnumerator();
                while (__enumerator21.MoveNext())
                {
                    var item = __enumerator21.Current;
                    {
                        item.IsShowCheck = 0;
                    }
                }
            }
            {
                // foreach(var item in SailingData.ShipEquip.DropItem)
                var __enumerator22 = (SailingData.ShipEquip.DropItem).GetEnumerator();
                while (__enumerator22.MoveNext())
                {
                    var item = __enumerator22.Current;
                    {
                        item.IsShowCheck = 0;
                    }
                }
            }

            var SailingDataUIColorSelectCount23 = SailingData.UI.ColorSelect.Count;
            for (var i = 0; i < SailingDataUIColorSelectCount23; i++)
            {
                SailingData.UI.ColorSelect[i] = false;
            }

            SetScanTips();
        }

        //初始化背包
        private void InitMedalBag(BagBaseData bagBase)
        {
            var bagID = bagBase.BagId;
            switch (bagID)
            {
                case 6:
                {
                    var list = new List<MedalItemDataModel>();
                    SailingData.ShipEquip.BagItem.Clear();
                    var tbBaseBase = Table.GetBagBase(bagID);
                    var tbBaseBaseMaxCapacity20 = tbBaseBase.MaxCapacity;
                    for (var i = 0; i < tbBaseBaseMaxCapacity20; i++)
                    {
                        var Medalitem = new MedalItemDataModel();
                        Medalitem.Index = i;
                        list.Add(Medalitem);
                        //SailingData.ShipEquip.BagItem.Add(Medalitem);
                    }
                    {
                        var __list10 = bagBase.Items;
                        var __listCount10 = __list10.Count;
                        for (var __i10 = 0; __i10 < __listCount10; ++__i10)
                        {
                            var item = __list10[__i10];
                            {
                                var Medalitem = list[item.Index];
                                //MedalItemDataModel Medalitem = SailingData.ShipEquip.BagItem[item.Index];
                                Medalitem.BagId = (int)eBagType.MedalBag;
                                Medalitem.BaseItemId = item.ItemId;
                                if (Medalitem.BaseItemId > 0)
                                {
                                    Medalitem.nLevel = item.Exdata[0];
                                    Medalitem.nExp = item.Exdata[1];
                                    Medalitem.nNeedExp = item.Exdata[2];
                                }

                            }
                        }
                    }
                    SailingData.ShipEquip.BagItem = new ObservableCollection<MedalItemDataModel>(list);
                }
                    break;
                case 19:
                {
                    var __list11 = bagBase.Items;
                    var __listCount11 = __list11.Count;
                    for (var __i11 = 0; __i11 < __listCount11; ++__i11)
                    {
                        var item = __list11[__i11];
                        {
                            var Medalitem = SailingData.ShipEquip.EquipItem[item.Index];
                            Medalitem.BaseItemId = item.ItemId;
                            Medalitem.BagId = (int)eBagType.MedalUsed;
                            if (Medalitem.BaseItemId > 0)
                            {
                                Medalitem.nLevel = item.Exdata[0];
                                Medalitem.nExp = item.Exdata[1];
                                Medalitem.nNeedExp = item.Exdata[2];
                            }

                        }
                    }
                }
                    break;
                case 20:
                {
                    var tbBaseBase = Table.GetBagBase(bagID);
                    var tbBaseBaseMaxCapacity21 = tbBaseBase.MaxCapacity;
                    var list = new List<MedalItemDataModel>();
                    for (var i = 0; i < tbBaseBaseMaxCapacity21; i++)
                    {
                        var Medalitem = new MedalItemDataModel();
                        Medalitem.Index = i;
                        list.Add(Medalitem);
                        //SailingData.ShipEquip.DropItem.Add(Medalitem);
                    }
                    {
                        var __list12 = bagBase.Items;
                        var __listCount12 = __list12.Count;
                        for (var __i12 = 0; __i12 < __listCount12; ++__i12)
                        {
                            var item = __list12[__i12];
                            {
                                var Medalitem = list[item.Index];
                                //MedalItemDataModel Medalitem = SailingData.ShipEquip.DropItem[item.Index];
                                Medalitem.BagId = (int)eBagType.MedalTemp;
                                Medalitem.BaseItemId = item.ItemId;
                                if (Medalitem.BaseItemId > 0)
                                {
                                    Medalitem.nLevel = item.Exdata[0];
                                    Medalitem.nExp = item.Exdata[1];
                                    Medalitem.nNeedExp = item.Exdata[2];
                                }

                            }
                        }
                    }
                    SailingData.ShipEquip.DropItem = new ObservableCollection<MedalItemDataModel>(list);
                }
                    break;
                //default:
                //    {
                //       break;
                //    }
            }
        }

        //升级返回按钮
        private void LevelBackClick()
        {
            SailingData.IsShowAttr = 1;
        }

        //升级界面显示
        private void LevelUIShow()
        {
        }

        //升级确定按钮
        private void LevelUpClick()
        {
            var tb = Table.GetMedal(SailingData.TempData.TempMedalItem.BaseItemId);
            if (tb != null)
            {
                if (tb.MaxLevel <= SailingData.TempData.TempMedalItem.nLevel)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220809));
                }
                else
                {
                    int curLevel = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e621);//无尽幻境历史最高
                    if (curLevel <= SailingData.TempData.TempMedalItem.nLevel)
                    {
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, string.Format(GameUtils.GetDictionaryText(100003020), curLevel), "", () =>
                        {
                            var ee = new Show_UI_Event(UIConfig.ClimbingTowerUI);
                            EventDispatcher.Instance.DispatchEvent(ee);
                        }, null, false, false, GameUtils.GetDictionaryText(100000298));
                    }
                    else
                    {
                        if (SailingData.TempData.TempMedalItem.nNeedExp > PlayerDataManager.Instance.GetRes((int)eResourcesType.HomeExp))
                        {
                            PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.HomeExp);
                            return;
                        }

                        NetManager.Instance.StartCoroutine(LevelUpCoroutine(SailingData.TempData.TempMedalItem.BagId,
                            SailingData.TempData.TempMedalItem.Index));
                    }
               
                }

            }

        }

        private IEnumerator LevelUpCoroutine(int bagID,
            int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.EnchanceMedal(bagID, index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_SailingPlayAnimation());
                        LevelupTempItem();
                        IsUpMaxLevel((int)eBagType.MedalUsed);
                    }
                    else
                    {
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

        private void IsUpMaxLevel(int bagId)
        {
            var tb = Table.GetMedal(SailingData.TempData.TempMedalItem.BaseItemId);
            if (tb != null)
            {
                if (tb.CanEquipment == 0) return;
                SailingData.TakeoffBtnPosition = new Vector3(228f, 0f, 0f);
                //SailingData.UpLevelMaxDesPosition0 = new Vector3(-128.2f, -48f, 0f);
                //SailingData.UpLevelMaxDesPosition1 = new Vector3(-128.2f, -80f, 0f);
                //SailingData.UpLevelMaxLevelPosition = new Vector3(-118.9f, -78.6f, 0f);

                bool isMax = SailingData.TempData.TempMedalItem.nLevel >= tb.MaxLevel;
                {
                    SailingData.IsShowItemDesLabel = !isMax;
                    SailingData.IsShowUpBtn = !isMax;
                    SailingData.IsMaxLevel = !isMax;
                    SailingData.IsMaxLevelShowLabel = isMax;
                    if (isMax)
                    {
                        SailingData.TakeoffBtnPosition = new Vector3(114f, 0f, 0f);
                        //SailingData.UpLevelMaxDesPosition0 = new Vector3(-35.3f, -48f, 0f);
                        //SailingData.UpLevelMaxDesPosition1 = new Vector3(-35.3f, -80f, 0f);
                        //SailingData.UpLevelMaxLevelPosition = new Vector3(-27f, -78.6f, 0f);
                    }
                }

                if (bagId == (int)eBagType.MedalBag)
                {
                    SailingData.IsShowUpBtn = false;
                    SailingData.TakeoffBtnPosition = new Vector3(5000f, 0f, 0f);
                }
                else if (bagId == (int)eBagType.MedalUsed)
                {
                    if (isMax)
                    {
                        SailingData.IsShowUpBtn = !isMax;
                        SailingData.TakeoffBtnPosition = new Vector3(114f, 0f, 0f);
                    }
                    else
                    {
                        SailingData.IsShowUpBtn = true;
                        SailingData.TakeoffBtnPosition = new Vector3(228f, 0f, 0f);
                    }
                }
                else if (bagId == 0)
                {
                    SailingData.IsShowUpBtn = false;
                }
                else if (bagId == 1)
                {
                    SailingData.IsShowUpBtn = !isMax;
                }
                SailingData.IsMaxLevel = !isMax;
                SailingData.IsMaxLevelShowLabel = isMax;
            }
        }


        private void OnShowPreviewUIEvent(IEvent ievent)
        {
            var e = ievent as ShowPreviewUIEvent;
            switch (e.Type)
            {
                case 0:
                {
                    SailingData.ShowPriview = true;
                    SailingData.ButtonSelect = 0;
                    ShowPreviewMedal();
                }
                    break;
                case 1:
                {
                    SailingData.ButtonSelect = -1;
                    SailingData.ShowPriview = false;
                }
                    break;
            }
        }
        private void ShowPreviewMedal()
        {
            var PreviewMedal = Table.GetClientConfig(257).Value;
            var MedalList = new List<MedalItemDataModel>();
            var MedalItem = PreviewMedal.Split('|');
            for (int i = 0; i < MedalItem.Length; i++)
            {
                var MedalData = new MedalItemDataModel();
                var medal = int.Parse(MedalItem[i]);
                var tbItemBase = Table.GetItemBase(medal);
                MedalData.BaseItemId = tbItemBase.Id;
                MedalData.Index = i;
                MedalData.BagId = (int)eBagType.MedalBag;
                MedalList.Add(MedalData);
            }
            SailingData.ShipEquip.PreviewItem = new ObservableCollection<MedalItemDataModel>(MedalList);
        }
        private void OnClickDedalCellEvent(IEvent ievent)
        {
            var e = ievent as MedalCellClcikEvent;
            if (e.Index >= 0)
            {
                if (SelectedItem != null)
                {
                    SelectedItem.Selected = 0;
                }
                if (e.BagId == (int)eBagType.MedalBag)
                {
                    mSelectType = (int)SelectType.BagSelect;

                    SailingData.ShipEquip.PreviewItem[e.Index].Selected = 1;
                    SelectedItem = SailingData.ShipEquip.PreviewItem[e.Index];
                }
                var tempdata = new MedalInfoDataModel();
                tempdata.ItemData = SelectedItem;
                if (tempdata.ItemData.BaseItemId == -1)
                {
                    return;
                }
                var tbMedal = Table.GetMedal(tempdata.ItemData.BaseItemId);
                if (null == tbMedal)
                {
                    return;
                }
                var ExpValue = Table.GetSkillUpgrading(tbMedal.LevelUpExp).Values;
                tempdata.ItemData.BagId = e.BagId;
                tempdata.ItemData.Index = e.Index;
                tempdata.ItemData.MaxLevel = tbMedal.MaxLevel;
                if (tempdata.ItemData.MaxLevel-1 >= ExpValue.Count)
                {
                    return;
                }
                var exp = ExpValue[tempdata.ItemData.MaxLevel-1];
                tempdata.ItemData.MaxExp = string.Format("{0}{1}{2}", exp, "/", exp);
                tempdata.PutOnOrOff = e.PutOnOrOff;
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MedalInfoUI,
                    new MedalInfoArguments { MedalInfoData = tempdata }));
                mSelectIndex = e.Index;
            }
        }
        private void LightPointAccessClick(IEvent ievent)
        {//专家炼金
            var e = ievent as UIEvent_SailingLightPointAccess;
            var index = e.Index;
            NetManager.Instance.StartCoroutine(LightPointClickAccessCoroutine(index));
        }
        //开始出航
        private void LightPointClick(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(LightPointClickCoroutine());

        }

        private void AutoAlchemy()
        {
            if (SailingData.IsAuto == 0)
                return;

            NetManager.Instance.StartCoroutine(LightPointClickCoroutine());
        }

        private void SetIndex(int idx)
        {
            if (idx >= 0 && idx < SailingData.Ship.Count)
            {
                SailingData.Index = idx;
                SailingData.EffectIndex = SailingData.Index;
                SailingData.CurItem.CopyFrom(SailingData.Ship[idx]);
            }
        }
        // 出海
        private IEnumerator LightPointClickCoroutine()
        {

            if (SailingData.CurItem.CostAlchemy > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Alchemy)
            {
                PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.Alchemy);
                SailingData.IsAuto = 0;
                yield break;
            }



            using (new BlockingLayerHelper(0))
            {
                var tbBuild = Table.GetBuilding(BraveHarborBuild.TypeId);
                var array = new Int32Array();
                array.Items.Add(0);
                var msg = NetManager.Instance.UseBuildService(BraveHarborBuild.AreaId, tbBuild.ServiceId, array);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SetIndex(msg.Response.Data32[0]);
                        yield return null;
                    }
                    else
                    {
                        if (msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                        {
                            SailingData.IsAuto = 0;
                            EventDispatcher.Instance.DispatchEvent(new AutoRecycleMedalEvent());
                            yield break;
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(msg.ErrorCode); 
                        }
                    }

                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                    yield break;
                }
            }
            if (SailingData.IsAuto > 0)
            {
                yield return new WaitForSeconds(0.5f);
                AutoAlchemy();
            }
        }

        //直达
        private IEnumerator LightPointClickAccessCoroutine(int nIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var tbBuild = Table.GetBuilding(BraveHarborBuild.TypeId);
                var array = new Int32Array();
                array.Items.Add(4);
                array.Items.Add(nIndex);
                var msg = NetManager.Instance.UseBuildService(BraveHarborBuild.AreaId, tbBuild.ServiceId, array);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {

                    }
                    else
                    {
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

        //领取结果



        //整理背包
        private void OnArrange()
        {
            if (SailingData.ShipEquip.BagItem.Count > 1)
            {
                NetManager.Instance.StartCoroutine(OnArrangeCoroutine((int)eBagType.MedalBag));
            }
        }

        private IEnumerator OnArrangeCoroutine(int nBagId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.SortBag(nBagId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var bag = msg.Response;
                        InitMedalBag(bag);
                    }
                    else
                    {
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

        private void OnCityDataInit(IEvent ievent)
        {
            BraveHarborBuild = null;
            {
                var __enumerator6 = (CityManager.Instance.BuildingDataList).GetEnumerator();
                while (__enumerator6.MoveNext())
                {
                    var buildingData = __enumerator6.Current;
                    {
                        var typeId = buildingData.TypeId;
                        var tbBuild = Table.GetBuilding(typeId);
                        if (tbBuild == null)
                        {
                            continue;
                        }
                        if (tbBuild.Type == 12)
                        {
                            BraveHarborBuild = buildingData;
                            SetIndex(buildingData.Exdata[0]);
                            break;
                        }
                    }
                }
            }
            if (BraveHarborBuild != null)
            {
                AnalyseNotice();
            }
        }

        //点击背包物品事件
        private void OnClickPackItem(IEvent ievent)
        {
            var e = ievent as UIEvent_SailingPackItemUI;
            if (e.Index >= 0)
            {
                if (SelectedItem != null)
                {
                    SelectedItem.Selected = 0;
                    SailingData.TempData.TempMedalItem.ItemName = string.Empty;
                }
                if (e.BagId == (int)eBagType.MedalBag)
                {
                    mSelectType = (int)SelectType.BagSelect;
                    SetListSelectBg();
                    MedalItemDataModel item = SailingData.ShipEquip.BagItem[e.Index];
                    if (item != null)
                    {
                        SetTempMedalItem(item);
                        item.Selected = 1;
                        SelectedItem = item;
                        var ItemBaseTable = Table.GetItemBase(SelectedItem.BaseItemId);
                        if (ItemBaseTable != null)
                        {
                            var tbColor = Table.GetColorBase(ItemBaseTable.Quality);
                            SailingData.TempData.TempMedalItem.ItemName = ItemBaseTable.Name;
                            SailingData.TempData.TempMedalItem.ItemNameColor = new Color(tbColor.Red / 255f, tbColor.Green / 255f,
                                tbColor.Blue / 255f);

                            IsShowBtnByParam(Table.GetMedal(ItemBaseTable.Id).CanEquipment);
                        }
                    }
                }
                else if (e.BagId == (int)eBagType.MedalUsed)
                {
                    mSelectType = (int)SelectType.EquipSelect;
                    SetListSelectBg();
                    SetTempMedalItem(SailingData.ShipEquip.EquipItem[e.Index]);
                    SailingData.ShipEquip.EquipItem[e.Index].Selected = 1;
                    SelectedItem = SailingData.ShipEquip.EquipItem[e.Index];
                    var ItemBaseTable = Table.GetItemBase(SelectedItem.BaseItemId);
                    if (ItemBaseTable != null)
                    {
                        var tbColor = Table.GetColorBase(ItemBaseTable.Quality);
                        SailingData.TempData.TempMedalItem.ItemName = ItemBaseTable.Name;
                        SailingData.TempData.TempMedalItem.ItemNameColor = new Color(tbColor.Red / 255f, tbColor.Green / 255f,
                            tbColor.Blue / 255f);
                    }
                    SailingData.IsShowDecomposeBtn = false;
                    SailingData.IsShowUpBtn = true;
                }
                else
                {
                    SailingData.TempData.TempMedalItem.BaseItemId = -1;
                    return;
                }
                var tempdata = new MedalInfoDataModel();
                tempdata.ItemData = SailingData.TempData.TempMedalItem;
                if (tempdata.ItemData.BaseItemId == -1)
                {
                    return;
                }
                tempdata.ItemData.BagId = e.BagId;
                tempdata.ItemData.Index = e.Index;
                tempdata.PutOnOrOff = e.PutOnOrOff;
                var tbMedal = Table.GetMedal(tempdata.ItemData.BaseItemId);
                if (tbMedal.CanEquipment == 1)
                {
                    tempdata.IsShowButton = 1;
                }
                else
                {
                    tempdata.IsShowButton = 0;
                }
                //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MedalInfoUI,
                //    new MedalInfoArguments { MedalInfoData = tempdata }));
                mSelectIndex = e.Index;

                IsUpMaxLevel(e.BagId);
            }


        }

        private void IsShowBtnByParam(int canEquipment)
        {
            SailingData.IsShowUpBtn = true;
            SailingData.IsShowDecomposeBtn = true;
            SailingData.IsShowEquipBtn = true;
            SailingData.DecomposeBtnPosition = new Vector3(114f, 0, 0);
            SailingData.IsShowItemDesLabel = true;
            SailingData.IsShowUpBtn = canEquipment == 0;
            if (canEquipment != 0)
            {
                SailingData.DecomposeBtnPosition = Vector3.zero;
            }
            else
            {
                SailingData.IsShowUpBtn = false;
                SailingData.IsShowEquipBtn = false;
                SailingData.IsShowItemDesLabel = false;
            }
        }

        //------------------------------------背包界面----------------------------------------------
        //返回按钮//船坞---海港切换
        private void OnClickReturnBtn(IEvent ievent)
        {
            SailingData.IsShowAttr = 0;
        }

        private void OnExdataUpdate(IEvent ievent)
        {
            SetScanTips();
        }

        //升级界面checkbox响应
        private void OnToggleChange(object sender, PropertyChangedEventArgs e)
        {
            var index = 0;
            if (!int.TryParse(e.PropertyName, out index))
            {
                return;
            }
            RefleshUI();
        }

        //-----------------------------------出航界面-------------------------------
        //捡起所有的掉落船饰
        private void PickAll(IEvent ievent)
        {
            var e = ievent as UIEvent_SailingPickAll;
            var count = 0;
            {
                // foreach(var item in SailingData.ShipEquip.DropItem)
                var __enumerator7 = (SailingData.ShipEquip.DropItem).GetEnumerator();
                while (__enumerator7.MoveNext())
                {
                    var item = __enumerator7.Current;
                    {
                        if (item.BaseItemId != -1)
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
            //临时掉落背包装备个数是否为0
            if (count != 0)
            {
                NetManager.Instance.StartCoroutine(PickUpMemedalCoroutine(-1, e.Flag,e.IsAuto));
            }
        }

        //捡起单个掉落船饰
        private void PickOne(IEvent ievent)
        {
            var e = ievent as UIEvent_SailingPickOne;

            var count = 0;
            var tempData = SailingData.ShipEquip.DropItem[e.Index];
            if (tempData.BaseItemId == -1)
            {
                return;
            }
            var ItemBaseTable = Table.GetItemBase(tempData.BaseItemId);
            if (ItemBaseTable == null)
            {
                return;
            }
            var MedalTable = Table.GetMedal(ItemBaseTable.Exdata[0]);
            if (MedalTable == null)
            {
                return;
            }
            //废弃零件无法收进背包
            if (MedalTable.Quality == 0)
            {
                var ee = new ShowUIHintBoard(220811);
                EventDispatcher.Instance.DispatchEvent(ee);
                return;
            }

            var SailingDataShipEquipBagItemCount18 = SailingData.ShipEquip.BagItem.Count;
            for (var i = 0; i < SailingDataShipEquipBagItemCount18; i++)
            {
                if (SailingData.ShipEquip.BagItem[i].BaseItemId == -1)
                {
                    count++;
                    break;
                }
            }
            //背包已满
            if (count == 0)
            {
                var ee = new ShowUIHintBoard(220812);
                EventDispatcher.Instance.DispatchEvent(ee);
            }
            NetManager.Instance.StartCoroutine(PickUpMemedalCoroutine(e.Index, 0));
        }

        private IEnumerator PickUpMemedalCoroutine(int index, int flag,bool IsAuto = false)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.PickUpMedal(index, flag);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (IsAuto)
                        {
                            yield return new WaitForSeconds(0.2f);
                            SailingData.IsAuto = 1;
                            NetManager.Instance.StartCoroutine(LightPointClickCoroutine());                            
                        }
                    }
                    else
                    {
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

        //穿上船饰
        private void PutOnClick(IEvent ievent)
        {
            var count = 0;
            var e = ievent as UIEvent_SailingPutOnClick;
            if (e.PutOnOrOff == 1)
            {
                var IsSameType = false;
                var varItem = SailingData.TempData.TempMedalItem;
                var MedalTable = Table.GetMedal(varItem.BaseItemId);
                var medalType = MedalTable.MedalType;
                {
                    // foreach(var item in SailingData.ShipEquip.EquipItem)
                    var __enumerator5 = (SailingData.ShipEquip.EquipItem).GetEnumerator();
                    while (__enumerator5.MoveNext())
                    {
                        var item = __enumerator5.Current;
                        {
                            if (item.BaseItemId == -1)
                            {
                                if (item.IsLock != 1)
                                {
                                    count++;
                                }
                                continue;
                            }
                            var MedalTable2 = Table.GetMedal(item.BaseItemId);
                            if (MedalTable2.MedalType == medalType)
                            {
                                IsSameType = true;
                            }
                        }
                    }
                }
                ////你的装备零件栏已满
                if (!IsSameType && count == 0)
                {
                    var ee = new ShowUIHintBoard(220802);
                    EventDispatcher.Instance.DispatchEvent(ee);
                    return;
                }
                SailingData.IsShowDecomposeBtn = false;
                SailingData.IsShowUpBtn = true;
                NetManager.Instance.StartCoroutine(PutOnMemedalCoroutine((int)eBagType.MedalBag,
                    SailingData.TempData.TempMedalItem.Index));
            }
            else
            {
                var SailingDataShipEquipBagItemCount14 = SailingData.ShipEquip.BagItem.Count;
                for (var i = 0; i < SailingDataShipEquipBagItemCount14; i++)
                {
                    if (SailingData.ShipEquip.BagItem[i].BaseItemId == -1)
                    {
                        count++;
                        break;
                    }
                }
                //背包已满，请先吞噬掉一些零件
                if (count == 0)
                {
                    var ee = new ShowUIHintBoard(220804);
                    EventDispatcher.Instance.DispatchEvent(ee);
                    return;
                }
                SailingData.IsShowDecomposeBtn = true;
                SailingData.IsShowUpBtn = false;
                SailingData.DecomposeBtnPosition = Vector3.zero;
                NetManager.Instance.StartCoroutine(PutOnMemedalCoroutine((int)eBagType.MedalUsed,
                    SailingData.TempData.TempMedalItem.Index));
            }

            IsUpMaxLevel(e.PutOnOrOff);
        }

        private IEnumerator PutOnMemedalCoroutine(int bagID, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.EquipMedal(bagID, index);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (msg.Response >= 0 && msg.Response < SailingData.ShipEquip.EquipItem.Count)
                        {
                            var item = SailingData.ShipEquip.EquipItem[msg.Response];
                            item.PlayEffect = false;
                            if (bagID == (int)eBagType.MedalBag)
                            {
                                item.PlayEffect = true;
                            }
                        }
                        SailingData.TempData.TempMedalItem.Index = msg.Response;
                        SailingData.TempData.TempMedalItem.BagId = bagID == (int)eBagType.MedalUsed ? (int)eBagType.MedalBag : (int)eBagType.MedalUsed;

                        if (SelectedItem != null)
                        {
                            SelectedItem.Selected = 0;
                            SetSelectIsShow(bagID, msg.Response);
                        }
                    }
                    else
                    {
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

        private void SetSelectIsShow(int type, int index)
        {
            var count = 0;
            if (type == (int)eBagType.MedalUsed)
            {
                count = SailingData.ShipEquip.BagItem.Count;
                if (index >= count) return;
                SetListSelectBg();
                SailingData.ShipEquip.BagItem[index].Selected = 1;
            }
            else if (type == (int)eBagType.MedalBag)
            {
                count = SailingData.ShipEquip.EquipItem.Count;
                if (index >= count) return;
                SetListSelectBg();
                SailingData.ShipEquip.EquipItem[index].Selected = 1;
            }
        }

        private void SetListSelectBg()
        {
            for (var i = 0; i < SailingData.ShipEquip.BagItem.Count; i++)
            {
                if (SailingData.ShipEquip.BagItem[i].BaseItemId == -1) continue;
                SailingData.ShipEquip.BagItem[i].Selected = 0;
            }
            for (var i = 0; i < SailingData.ShipEquip.EquipItem.Count; i++)
            {
                if (SailingData.ShipEquip.EquipItem[i].BaseItemId == -1) continue;
                SailingData.ShipEquip.EquipItem[i].Selected = 0;
            }
        }

        // 刷新勇士港building
        private void RefeshBuildData(BuildingData buildingData)
        {
            //        var tbBuild = Table.GetBuilding(buildingData.TypeId);
            //        if (tbBuild == null)
            //        {
            //            return;
            //        }
            //        if (tbBuild.Type != (int) BuildingType.BraveHarbor)
            //        {
            //            return;
            //        }
            //        var index = 0;
            //        {
            //            var __list9 = buildingData.Exdata;
            //            var __listCount9 = __list9.Count;
            //            for (var __i9 = 0; __i9 < __listCount9; ++__i9)
            //            {
            //                var i = __list9[__i9];
            //                {
            //                    if (index > 4)
            //                    {
            //                        break;
            //                    }
            //                    SailingData.States[index] = i;
            //                    SailingData.Ship[index].DistancePerCent = 0f;
            //                    //SailingData.AddSpeed[index] = "";
            //                    if (i == 2 || i == 12 || i == 3)
            //                    {
            //                        // SailingData.AddSpeed[index] = GameUtils.GetDictionaryText(220819);
            //                        if (i == 3)
            //                        {
            //                            //SailingData.Ship[index].SpeedOrGet = 2;
            //                            SetShipSpeedOrGet(index, 2);
            //                            SailingData.Ship[index].IsShowShip = false;
            //                        }
            //                        else
            //                        {
            //                            //SailingData.Ship[index].SpeedOrGet = 1;
            //                            SetShipSpeedOrGet(index, 1);
            //                            SailingData.Ship[index].IsShowShip = true;
            //                        }
            //                        UpdataShip(index);
            //                    }
            //                    else
            //                    {
            //                        //if (i == 0)
            //                        //{
            ////                        SailingData.Ship[index].SpeedOrGet = 0;
            //                        SetShipSpeedOrGet(index, 0);
            //                        // }
            //                        SailingData.Ship[index].IsShowShip = false;
            //                        SailingData.Ship[index].Times = " ";
            //                        //if (index == 4)
            //                        //{
            //                        //    SailingData.States[5] = 0;
            //                        //}
            //                    }
            //                    index++;
            //                }
            //            }
            //        }
            //        AnalyseNotice();
            //        RefleshAccessBtn();
        }
        //刷新属性增加显示
        private void RefleshLevelUpAttr()
        {
            var tempData = SailingData.TempData.TempMedalItem;
            if (tempData.BaseItemId != -1)
            {
                var ItemBaseTable = Table.GetItemBase(tempData.BaseItemId);
                var MedalTable = Table.GetMedal(ItemBaseTable.Exdata[0]);

                var MedalTableAddPropIDLength11 = MedalTable.AddPropID.Length;
                for (var i = 0; i < MedalTableAddPropIDLength11; i++)
                {
                    if (MedalTable.AddPropID[i] == -1)
                    {
                        SailingData.TempData.Attributes[i].Type = -1;
                    }
                    else
                    {
                        var tbProp = Table.GetSkillUpgrading(MedalTable.PropValue[i]);
                        SailingData.TempData.Attributes[i].Type = MedalTable.AddPropID[i];
                        SailingData.TempData.Attributes[i].Value = tbProp.GetSkillUpgradingValue(tempData.nLevel);
                        SailingData.TempData.Attributes[i].Change = tbProp.GetSkillUpgradingValue(tempData.NextLevel);
                    }
                }
            }
        }

        //levelup页面刷新
        private void RefleshUI()
        {
            var tempData = SailingData.TempData.TempMedalItem;
            //刷新经验界面
            AddBagSelectList();
            var UICurrentExp = 0;
            var UICurrentExpMax = 0;
            if (tempData.BaseItemId != -1)
            {
                var MedalTable = Table.GetMedal(tempData.BaseItemId);
                var tbProp = Table.GetSkillUpgrading(MedalTable.LevelUpExp);
                var templevel = tempData.nLevel;
                var MedalTableMaxLevel10 = MedalTable.MaxLevel;
                for (var i = templevel; i <= MedalTableMaxLevel10; i++)
                {
                    var levelexp = GetTolalExp(MedalTable.LevelUpExp, templevel);
                    if (levelexp < SailingData.TempData.TotalExp)
                    {
                        templevel++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (templevel > MedalTable.MaxLevel + 1)
                {
                    SailingData.UI.NextLevel = MedalTable.MaxLevel;
                    UICurrentExpMax = tbProp.GetSkillUpgradingValue(MedalTable.MaxLevel);
                    UICurrentExp = UICurrentExpMax;
                }
                else
                {
                    var temp = templevel - 1;
                    temp = (temp < tempData.nLevel)
                        ? tempData.nLevel
                        : temp;

                    //SailingData.UI.NextLevel = templevel - 1;
                    SailingData.UI.NextLevel = tempData.nLevel + 1;
                    SailingData.UI.NextLevel = (SailingData.UI.NextLevel < tempData.nLevel)
                        ? tempData.nLevel
                        : SailingData.UI.NextLevel;

                    UICurrentExp = SailingData.TempData.TotalExp -
                                   GetTolalExp(MedalTable.LevelUpExp, templevel - 1);
                    UICurrentExpMax = tbProp.GetSkillUpgradingValue(temp);
                    UICurrentExp = UICurrentExp > UICurrentExpMax ? UICurrentExpMax : UICurrentExp;
                }


                float tempfloat = 0;
                if (UICurrentExpMax != 0)
                {
                    tempfloat = UICurrentExp / (float)UICurrentExpMax;
                }
                SailingData.UI.UICurrentExp = UICurrentExp;
                SailingData.UI.UICurrentExpMax = UICurrentExpMax;
                SailingData.UI.currentExp = tempfloat;
            }
            RefleshLevelUpAttr();
        }

        //勇士港操作事件
        private void SailingOperation(IEvent ievent)
        {
            var e = ievent as UIEvent_SailingOperation;
            switch (e.Type)
            {
                case 0:
                {
                    OnArrange();
                }
                    break;
                case 1:
                {
                    EatAll(e.Param);
                }
                    break;
                case 2:
                {
                    LevelUIShow();
                }
                    break;
                case 3:
                {
                    LevelUpClick();
                }
                    break;
                case 4:
                {
                    AutoShipClick(e.Param);
                }
                    break;
                case 5:
                {
                    LevelBackClick();
                }
                    break;
                case 6:
                {
                    CloseTick();
                }
                    break;
                case 7:
                {
                    WoodTipsOk();
                }
                    break;
                case 8:
                {
                    CloseWoodTips();
                }
                    break;
                case 9:
                {
                    SplitMedalSingle();
                }
                    break;
            }
        }

        private void SplitMedalSingle()
        {
            if (SailingData.TempData.TempMedalItem != null && SailingData.TempData.TempMedalItem.BaseItemId > 0)
            {
                var tb = Table.GetItemBase(SailingData.TempData.TempMedalItem.BaseItemId);
                if (19 == SailingData.TempData.TempMedalItem.BagId)
                {
                    //玩家装备的符文
                    GameUtils.ShowHintTip(224704);
                    return;
                }
                else if (tb.Quality > 2)
                {
                    var tbMedal = Table.GetMedal(SailingData.TempData.TempMedalItem.BaseItemId);
                    if (tbMedal == null)
                    {
                        Logger.Error("GetMedal is Null ");
                        return;
                    }
                    var exp = 0;
                    if (SailingData.TempData.TempMedalItem.nLevel > 1)
                    {
                        exp = GetBagExpByLevel(SailingData.TempData.TempMedalItem);
                    }
                    exp += tbMedal.InitExp;
                    var str = string.Format(GameUtils.GetDictionaryText(230216), exp);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "",
                        () =>
                        {
                            NetManager.Instance.StartCoroutine(SplitMedal(SailingData.TempData.TempMedalItem.BagId, SailingData.TempData.TempMedalItem.Index, 0));
                        });
                }
                else
                {
                    NetManager.Instance.StartCoroutine(SplitMedal(SailingData.TempData.TempMedalItem.BagId, SailingData.TempData.TempMedalItem.Index, 0));
                }
            }
        }
        //---------------------------------升级界面----------------------------------------

        //设置扫荡和取消扫荡标签
        private void SetScanName(bool IsScan)
        {
            if (IsScan)
            {
                SailingData.ScanName = GameUtils.GetDictionaryText(220823);
            }
            else
            {
                SailingData.ScanName = GameUtils.GetDictionaryText(220822);
            }
        }

        private void SetScanTips()
        {
            //var vipLevel= PlayerDataManager.Instance.GetRes((int) eResourcesType.VipLevel);
            var count = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e71);
            var scanCount = PlayerDataManager.Instance.TbVip.SailScanCount;
            if (scanCount <= 0)
            {
                SailingData.ScanTips = ScanTips270250;
            }
            else
            {
                var countShow = scanCount - count;
                countShow = countShow <= 0 ? 0 : countShow;
                SailingData.ScanTips = string.Format(ScanTips270249, countShow);
            }
        }

        //点击设置勾选框   
        private void SetShowCheck(int bagId, int index)
        {
            if (bagId == (int)eBagType.MedalBag)
            {
                SailingData.ShipEquip.BagItem[index].IsShowCheck =
                    (SailingData.ShipEquip.BagItem[index].IsShowCheck == 1) ? 0 : 1;
            }
            else if (bagId == (int)eBagType.MedalTemp)
            {
                SailingData.ShipEquip.DropItem[index].IsShowCheck =
                    (SailingData.ShipEquip.DropItem[index].IsShowCheck == 1) ? 0 : 1;
            }
            RefleshUI();
        }

        //设置要升级的物品
        private void SetTempMedalItem(MedalItemDataModel item)
        {
            //    SailingData.TempData.TempMedalItem = new MedalItemDataModel(item);
            SailingData.TempData.TempMedalItem.CopyFrom(item);

            var tb = Table.GetMedal(item.BaseItemId);
            if (tb != null)
            {

                SailingData.TempData.TempMedalItem.NextLevel = SailingData.TempData.TempMedalItem.nLevel >= tb.MaxLevel
                    ? tb.MaxLevel
                    : SailingData.TempData.TempMedalItem.nLevel + 1;
                RefleshLevelUpAttr();
            }
        }

        //终止扫荡后status重置
        private void StopShipScan(int index)
        {
            //DataState = -1;
            //IsScan = false;
            //BraveHarborBuild.Exdata[index] = 10;
            //ResetShipPosition();
            //RefeshBuildData(BraveHarborBuild);
            //SetScanName(IsScan);
        }

        //更新建筑数据
        private void UpdateBuilding(IEvent ievent)
        {
            if (BraveHarborBuild == null)
            {
                return;
            }
            var e = ievent as UIEvent_CityUpdateBuilding;
            if (BraveHarborBuild.AreaId == e.Idx)
            {
                var buildingData = CityManager.Instance.GetBuildingByAreaId(e.Idx);
                RefeshBuildData(buildingData);
            }
        }

        private void LevelupTempItem()
        {
            if (SailingData.TempData.TempMedalItem.BaseItemId <= 0)
            {
                return;
            }
            SailingData.TempData.TempMedalItem.nLevel++;
            var tbMedal = Table.GetMedal(SailingData.TempData.TempMedalItem.BaseItemId);
            if (tbMedal == null)
                return;

            var needExp = Table.GetSkillUpgrading(tbMedal.LevelUpExp).GetSkillUpgradingValue(SailingData.TempData.TempMedalItem.nLevel);                            
            SailingData.TempData.TempMedalItem.nNeedExp = needExp;
            SailingData.TempData.TempMedalItem.NextLevel = SailingData.TempData.TempMedalItem.nLevel >= tbMedal.MaxLevel
                ? tbMedal.MaxLevel
                : SailingData.TempData.TempMedalItem.nLevel + 1;
            RefleshLevelUpAttr();
        }
        //更新背包
        private void UpdateMedalBag(int bagid, ItemsChangeData bag)
        {
            if (bagid == (int)eBagType.MedalBag)
            {
                {
                    // foreach(var baseData in bag.ItemsChange)
                    var __enumerator13 = (bag.ItemsChange).GetEnumerator();
                    while (__enumerator13.MoveNext())
                    {
                        var baseData = __enumerator13.Current;
                        {
                            var index = baseData.Key;
                            var bagitem = SailingData.ShipEquip.BagItem[index];
                            bagitem.BaseItemId = baseData.Value.ItemId;
                            bagitem.Index = baseData.Value.Index;
                            bagitem.BagId = (int)eBagType.MedalBag;
                            if (bagitem.BaseItemId > 0)
                            {
                                bagitem.nLevel = baseData.Value.Exdata[0];
                                bagitem.nExp = baseData.Value.Exdata[1];
                                bagitem.nNeedExp = baseData.Value.Exdata[2];
                            }
                        }
                    }
                }
            }
            else if (bagid == (int)eBagType.MedalUsed)
            {
                {
                    // foreach(var baseData in bag.ItemsChange)
                    var __enumerator16 = (bag.ItemsChange).GetEnumerator();
                    while (__enumerator16.MoveNext())
                    {
                        var baseData = __enumerator16.Current;
                        {
                            var index = baseData.Key;
                            var equipitem = SailingData.ShipEquip.EquipItem[index];
                            equipitem.BaseItemId = baseData.Value.ItemId;
                            equipitem.BagId = (int)eBagType.MedalUsed;
                            equipitem.BaseItemId = baseData.Value.ItemId;
                            equipitem.Index = baseData.Value.Index;
                            if (equipitem.BaseItemId != -1)
                            {
                                equipitem.nLevel = baseData.Value.Exdata[0];
                                equipitem.nExp = baseData.Value.Exdata[1];
                                equipitem.nNeedExp = baseData.Value.Exdata[2];
                            }
                        }
                    }
                }
                CalculateItemProp();
            }
            else if (bagid == (int)eBagType.MedalTemp)
            {
                // foreach(var baseData in bag.ItemsChange)
                var __enumerator19 = (bag.ItemsChange).GetEnumerator();
                while (__enumerator19.MoveNext())
                {
                    var baseData = __enumerator19.Current;
                    {
                        var index = baseData.Key;
                        var dropitem = SailingData.ShipEquip.DropItem[index];
                        dropitem.BaseItemId = baseData.Value.ItemId;
                        dropitem.BagId = (int)eBagType.MedalTemp;
                        dropitem.BaseItemId = baseData.Value.ItemId;
                        dropitem.Index = baseData.Value.Index;

                        if (baseData.Value.ItemId > 0)
                        {
                            dropitem.nLevel = baseData.Value.Exdata[0];
                            dropitem.nExp = baseData.Value.Exdata[1];
                            dropitem.nNeedExp = baseData.Value.Exdata[2];
                        }
                    }
                }
            }
        }

        private void WoodTipsOk()
        {
            IsScan = !IsScan;
            SetScanName(IsScan);
            SailingData.IsShowWoodTip = false;
            if (mAutoShipCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(mAutoShipCoroutine);
                mAutoShipCoroutine = null;
            }
            mAutoShipCoroutine = NetManager.Instance.StartCoroutine(AutoShipClickCoroutine(ScanCount));
        }

        //-----------------------------------接口---------------------------------------
        public INotifyPropertyChanged GetDataModel(string name)
        {
            return SailingData;
        }

        public void CleanUp()
        {
            if (SailingData != null)
            {
                SailingData.UI.ColorSelect.PropertyChanged -= OnToggleChange;
            }
            SailingData = new SailingDataModel();
            SailingData.UI.ColorSelect.PropertyChanged += OnToggleChange;
            for (var i = 0; i < SailingData.Ship.Count; i++)
            {
                SailingData.Ship[i] = new SailingShipDataModel();
            }
            ScanTips270250 = GameUtils.GetDictionaryText(270250);
            ScanTips270249 = GameUtils.GetDictionaryText(270249);
            SailingData.AlchemyTipCount = int.Parse(Table.GetClientConfig(1219).Value);
            var disStr = GameUtils.GetDictionaryText(300848);
            Table.ForeachSailing(tb =>
            {
                SailingData.Ship[tb.Id].CostAlchemy = tb.CostCount;
                SailingData.Ship[tb.Id].CostDiamond = tb.ItemCount;
                return true;
            });
            SailingData.IsShowFastReachMessageBox = false;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "InitMedalBag")
            {
                InitMedalBag(param[0] as BagBaseData);
            }
            else if (name == "UpdateMedalBag")
            {
                UpdateMedalBag((int)param[0], param[1] as ItemsChangeData);
            }

            return null;
        }

        public void OnShow()
        {
            SailingData.EffectIndex = -1;
        }

        public void Close()
        {
            PlayerDataManager.Instance.WeakNoticeData.Sailing = false;
            EventDispatcher.Instance.DispatchEvent(new CityBulidingWeakNoticeRefresh(BraveHarborBuild));

            if (SailingData.TempData != null)
            {
                if (SailingData.TempData.TempMedalItem != null)
                {
                    if (SailingData.TempData.TempMedalItem.BaseItemId != -1)
                    {
                        if (SailingData.TempData.TempMedalItem.BagId == (int)eBagType.MedalUsed)
                        {
                            SailingData.IsShowEquipBtn = false;
                        }
                    }
                }
            }
        }


        public void RefreshData(UIInitArguments data)
        {
            var args = data as SailingArguments;
            DataState = 0;
            if (args == null)
            {
                return;
            }
            if (args.Tab > 1)
            {
                return;
            }
            BraveHarborBuild = args.BuildingData;
            RefeshBuildData(BraveHarborBuild);
            SetScanName(IsScan);
            InitData();
            CalculateItemProp();
            CheckRedPoint(new UIEvent_SailingCheckRedPoint(-1));
            PlayerDataManager.Instance.NoticeData.AlchemyTip = false;
            var ResCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.Alchemy);
            if (ResCount > SailingData.CurItem.CostAlchemy)
            {
                SailingData.CanAlchemy = true;
            }
            else
            {
                SailingData.CanAlchemy = false;
            }

        }

        public FrameState State { get; set; }
        public void Tick()
        {
        }
        private void CheckRedPoint(IEvent ievent)
        {
            //UIEvent_SailingCheckRedPoint e = ievent as UIEvent_SailingCheckRedPoint;
            //if (e.Type == 0 || e.Type == -1)
            //{
            //    SailingData.nCompleteNode = 0;
            //    foreach (var ship in SailingData.Ship)
            //    {
            //        if (ship.SpeedOrGet == 2)
            //        {
            //            SailingData.nCompleteNode++;
            //        }
            //    }
            //}
            //if (e.Type == 1 || e.Type == -1)
            //{
            //    SailingData.nCanLevelup = 0;
            //    foreach (var item in SailingData.ShipEquip.EquipItem)
            //    {
            //        if(item.nNeedExp>0&&item.nNeedExp<=PlayerDataManager.Instance.GetRes((int)eResourcesType.HomeExp))
            //        {
            //            SailingData.nCanLevelup ++;
            //            break;
            //        }
            //    }
            //}
            //PlayerDataManager.Instance.NoticeData.SailingNotice = SailingData.nRed > 0;
        }

        private void OnSailingShowMessageBoxEvent(IEvent iEvent)
        {
            var ie = iEvent as SailingShowMessageBoxEvent;
            if (ie == null) return;
            switch (ie._Type)
            {
                case 0://打开
                    if (PlayerDataManager.Instance.IsCheckSailingTip) return;
                    break;
                case 1://ok                    
                    PlayerDataManager.Instance.IsCheckSailingTip = SailingData.CheckShowFastReachMessageBox;
                    break;
                case 2://cancle
                    SailingData.CheckShowFastReachMessageBox = false;
                    break;
            }
            NetManager.Instance.StartCoroutine(DelayShowMessageBox(ie));
        }

        private void OnResourceChangedEvent(IEvent ievent)
        {
            var e = ievent as Resource_Change_Event;
            if (e.Type != eResourcesType.Alchemy)
            {
                return;
            }
            OldValue = e.OldValue;
            NewValue = e.NewValue;
            if (NewValue >= SailingData.AlchemyTipCount)
            {
                PlayerDataManager.Instance.NoticeData.AlchemyTip = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.AlchemyTip = false;
            }
            if (NewValue >= SailingData.CurItem.CostAlchemy)
            {
                SailingData.CanAlchemy = true;
            }
            else
            {
                SailingData.CanAlchemy = false;
            }
        }
        private void OnExDataInit(IEvent ievent)
        {
            CheckAlchemyTip();
        }
        private void CheckAlchemyTip()
        {
            var ResCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.Alchemy);
            if (ResCount >= SailingData.AlchemyTipCount)
            {
                PlayerDataManager.Instance.NoticeData.AlchemyTip = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.AlchemyTip = false;
            }
        }

        private IEnumerator DelayShowMessageBox(SailingShowMessageBoxEvent ie)
        {
            yield return new WaitForSeconds(0.12f);
            SailingData.IsShowFastReachMessageBox = ie.IsShow;
            SailingData.FastReachMsgDesc = ie.StrDes;
        }
    }
}