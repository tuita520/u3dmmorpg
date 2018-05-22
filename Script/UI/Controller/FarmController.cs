/********************************************************************************* 

                         Scorpion




  *FileName:FarmController

  *Version:1.0

  *Date:2017-06-12

  *Description:

**********************************************************************************/

#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    public class PeasantFrameCtrler : IControllerBase
    {

        #region 静态变量
        private static FarmOrderData s_EmptyData = new FarmOrderData();
        #endregion

        #region 成员变量
        private FarmArguments m_farmArguments;
        private BuildingData BuildingData;
        private FarmDataModel m_DataModel;
        private Dictionary<int, int> m_DicNum_Level;
        private Dictionary<int, int> m_dicOrdIdToCount = new Dictionary<int, int>(); //农场订单物品
        //public int SeedPage = 1;  //种子菜单页面pageindex
        private int m_harvestCount;
        private readonly List<PetSkillRecord> m_petSkill = new List<PetSkillRecord>();
        private int m_selectIndex = -1;
        private BuildingRecord m_tbBuilding;
        private BuildingServiceRecord m_tbBuildingService;
        //----------------------------------------------------------------------------------Notice-----------------------------
        private object m_noticeFarmTrigger;
        private int m_orderMaxCount;
        private Coroutine m_refreshCoroutine;
        #endregion

        #region 构造函数
        public PeasantFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(CityDataInitEvent.EVENT_TYPE, OnCityInfoInit); // 初始化农场数据
            EventDispatcher.Instance.AddEventListener(FarmOperateEvent.EVENT_TYPE, OnFarmHandleEvent); //农场操作函数
            EventDispatcher.Instance.AddEventListener(FarmLandCellClick.EVENT_TYPE, LandCellClick); //农场土地消息处理
            EventDispatcher.Instance.AddEventListener(FarmMenuDragEvent.EVENT_TYPE, OnFarmInterfaceDragEvent); //设置被拖拽的物体的index
            EventDispatcher.Instance.AddEventListener(FarmMenuClickEvent.EVENT_TYPE, OnFarmInterfaceClickEvent); //菜单物体被点击
            EventDispatcher.Instance.AddEventListener(FarmOrderListClick.EVENT_TYPE, OnFarmCommandListClickEvent); // 订单list点击事件

            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExchangeDataInit); //初始化ExData
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnUpdateExchangeData); //同步ExData
            EventDispatcher.Instance.AddEventListener(VipLevelChangedEvent.EVENT_TYPE, OnVipLevelAlteredEvent); //vip level改变了

            EventDispatcher.Instance.AddEventListener(CityWeakNoticeRefreshEvent.EVENT_TYPE, OnWeakNoticeRenewEvent);
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_harvestCount = 0;
            m_DicNum_Level = new Dictionary<int, int>();

            Table.ForeachBuilding(tbBuilding =>
            {
                if (tbBuilding.Type == (int)BuildingType.Farm)
                {
                    var _tableService = Table.GetBuildingService(tbBuilding.ServiceId);
                    if (null == _tableService)
                    {
                        return true;
                    }
                    var _max = _tableService.Param[4];
                    if (_max > m_orderMaxCount)
                    {
                        m_orderMaxCount = _max;
                    }
                    if (m_DicNum_Level.ContainsKey(_max))
                    {
                        m_DicNum_Level[_max] = Math.Min(m_DicNum_Level[_max], tbBuilding.Level);
                    }
                    else
                    {
                        m_DicNum_Level.Add(_max, tbBuilding.Level);
                    }
                }

                return true;
            });


            if (m_refreshCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(m_refreshCoroutine);
                m_refreshCoroutine = null;
            }
            m_DataModel = new FarmDataModel();
            for (var i = 0; i < m_DataModel.LandMenuData.ItemList.Count; i++)
            {
                m_DataModel.LandMenuData.ItemList[i] = new FarmLandMenuCell();
            }
            m_DataModel.LandMenuData.SeedPage = 1;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "GetBuildLevel")
            {
                if (m_tbBuilding != null)
                {
                    return m_tbBuilding.Level;
                }
                return 0;
            }
            return null;
        }

        public void OnShow()
        {
            if (m_farmArguments == null)
            {
                return;
            }
            RenewBuildData(m_farmArguments.BuildingData);
            RenewPetSkillData();
            NetManager.Instance.StartCoroutine(CityRenewTaskCoroutine(-1));
        }

        public void Close()
        {
            m_harvestCount = 0;
            PlayerDataManager.Instance.WeakNoticeData.Farm = false;
            EventDispatcher.Instance.DispatchEvent(new CityBulidingWeakNoticeRefresh(BuildingData));
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as FarmArguments;
            if (_args == null)
            {
                return;
            }
            m_farmArguments = _args;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public FrameState State { get; set; }
        #endregion



        #region 事件
        //菜单物体被点击
        private void OnFarmInterfaceClickEvent(IEvent ievent)
        {
            var _e = ievent as FarmMenuClickEvent;
            var _menu = m_DataModel.LandMenuData;
            var _index = _e.Index + _menu.ItemList.Count * (_menu.SeedPage - 1);
            if (_index >= _menu.MenuList.Count)
            {
                Logger.Error("OnFarmMenuClick  index >= menu.MenuList.Count");
                return;
            }
            var _menuIndex = _index;
            var _landIndex = _menu.Index;
            var _menuData = _menu.MenuList[_index];
            var _land = m_DataModel.CropLand[m_DataModel.LandMenuData.Index];
            _menu.PlantId = _land.Type;
            if (_menu.State == (int)MenuState.Growing
                && _land.State == (int)LandState.Growing
                && _menuData.Id == -1
                && _menuIndex == 0)
            {
                //是否确定要铲除该作物？拆除后将不会收获任何作物
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270093, "",
                    () => { FarmOperationContract(_landIndex, OperateType.Wipeout); });
            }
            _menu.Index = -1;
        }

        //设置被拖拽的物体的index
        private void OnFarmInterfaceDragEvent(IEvent ievent)
        {
            var _e = ievent as FarmMenuDragEvent;
            var _menu = m_DataModel.LandMenuData;
            var _index = _e.Index + _menu.ItemList.Count * (_menu.SeedPage - 1);
            if (_index >= _menu.MenuList.Count)
            {
                Logger.Error("OnFarmMenuClick  index >= menu.MenuList.Count");
                return;
            }
            m_DataModel.LandMenuData.DragIndex = _index;
        }

        //农场操作函数
        private void OnFarmHandleEvent(IEvent ievent)
        {
            var _e = ievent as FarmOperateEvent;
            switch (_e.Type)
            {
                //             case 0:
                //             case 1:
                //             case 2:
                //             case 3:
                //             case 4:
                //             case 5:
                //                 {
                //                     BtnBuyAddOrReduce(e.Type);
                //                 }
                //                 break;
                //             case 13:
                //                 {
                //                     StoreConfrimOk();
                //                 }
                //                 break;
                //             case 14:
                //                 {
                //                     StoreConfrimCancel();
                //                 }
                //                 break;
                case 15:
                {
                    ShutLandInterface();
                }
                    break;
                case 16:
                {
                    var _e2 = new CityBulidingNoticeRefresh(BuildingData);
                    EventDispatcher.Instance.DispatchEvent(_e2);
                }
                    break;
                case 17:
                {
                    HandinOrderClick();
                }
                    break;
                case 18:
                {
                    DeleteOrderClick();
                }
                    break;
                case 19:
                {
                    RenewOrderMinTimeClick();
                }
                    break;
                case 20:
                {
                    NextPageMenu();
                }
                    break;
                //case 21:
                //    {
                //        MenuPageDown();
                //    }
                //    break;
                case 22:
                {
                    m_selectIndex = -1;
                    RenewSelectedOrder();
                    //SetOrderCanDeliver();
                }
                    break;
            }
        }

        //----------------------------------------------------------------------------------Store--------------------------
        // 订单list点击事件
        private void OnFarmCommandListClickEvent(IEvent ievent)
        {
            var _e = ievent as FarmOrderListClick;
            var _index = _e.Index;

            var _orderData = m_DataModel.Orders[_index];
            if (_orderData.State == (int)CityMissionState.Wait)
            {
                RenewOrderClick(_index);
                return;
            }
            SetChooseOrderData(_index);
        }

        //初始化ExData
        private void OnUpdateExchangeData(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            if (_e.Key == 325)
            {
                var _tbVip = PlayerDataManager.Instance.TbVip;
                m_DataModel.RemainCount = _e.Value + _tbVip.FarmAddRefleshCount;
            }
        }

        //vip level改变了
        private void OnVipLevelAlteredEvent(IEvent ievent)
        {
            var _tbVip = PlayerDataManager.Instance.TbVip;
            m_DataModel.RemainCount = PlayerDataManager.Instance.GetExData(325) + _tbVip.FarmAddRefleshCount;
        }

        private void OnWeakNoticeRenewEvent(IEvent ievent)
        {
            if (BuildingData == null)
            {
                return;
            }
            PlayerDataManager.Instance.WeakNoticeData.Farm = GetWeakMessageState();
            EventDispatcher.Instance.DispatchEvent(new CityBulidingWeakNoticeRefresh(BuildingData));
        }

        //农场土地处理 点击、 被拖拽到它上面
        private void LandCellClick(IEvent ievent)
        {
            var _e = ievent as FarmLandCellClick;
            if (_e.IsDraging)
            {
                LandElementDrag(_e.Index);
            }
            else
            {
                LandCellClick(_e.Index);
            }
        }

        //同步ExData
        private void OnExchangeDataInit(IEvent ievent)
        {
            var _tbVip = PlayerDataManager.Instance.TbVip;
            m_DataModel.RemainCount = PlayerDataManager.Instance.GetExData(325) + _tbVip.FarmAddRefleshCount;
        }
        // 初始化农场数据
        private void OnCityInfoInit(IEvent ievent)
        {
            //var miss = CityManager.Instance.BuildingMissionList;
            // RefreshMission(miss);


            BuildingData = null;
            {
                // foreach(var buildingData in CityManager.Instance.BuildingDataList)
                var __enumerator6 = (CityManager.Instance.BuildingDataList).GetEnumerator();
                while (__enumerator6.MoveNext())
                {
                    var _buildingData = __enumerator6.Current;
                    {
                        var _typeId = _buildingData.TypeId;
                        var _tbBuild = Table.GetBuilding(_typeId);
                        if (_tbBuild == null)
                        {
                            continue;
                        }
                        if (_tbBuild.Type == 2)
                        {
                            BuildingData = _buildingData;
                            break;
                        }
                    }
                }
            }
            if (BuildingData != null)
            {
                RenewBuildData(BuildingData);
            }
        }
        #endregion




  
        //查找最近时间设置，刷新订单
        private void FindMissionTimeSetting()
        {
            var _minTime = Game.Instance.ServerTime;
            var _bFind = false;
            var _count = m_DataModel.Orders.Count;
            for (var i = 0; i < _count; i++)
            {
                var _orderData = m_DataModel.Orders[i];
                if (_orderData.State != (int) CityMissionState.Wait)
                {
                    continue;
                }
                var _freshTime = _orderData.RefresTime;
                if (_bFind == false)
                {
                    _bFind = true;
                    _minTime = _freshTime;
                }
                else
                {
                    if (_minTime > _freshTime)
                    {
                        _minTime = _freshTime;
                    }
                }
            }

            if (m_refreshCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(m_refreshCoroutine);
                m_refreshCoroutine = null;
            }

            if (_bFind)
            {
                m_DataModel.MinRefreshTime = _minTime;
                m_refreshCoroutine = NetManager.Instance.StartCoroutine(RenewMission());
            }
        }

        //刷新农场标志
        private void RenewFarmMark()
        {
            if (m_noticeFarmTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(m_noticeFarmTrigger);
                m_noticeFarmTrigger = null;
            }
            var _isMature = false;
            var _minSec = -1;
            var _count = m_DataModel.CropLand.Count;
            for (var i = 0; i < _count; i++)
            {
                var _land = m_DataModel.CropLand[i];
                if (_land == null)
                {
                    continue;
                }
                if (_land.Type == -1)
                {
                    continue;
                }

                if (_land.State == (int) LandState.Mature)
                {
                    _isMature = true;
                    var _tbItem = Table.GetItemBase(_land.Type);
                    if (_tbItem != null)
                    {
                        PlayerDataManager.Instance.NoticeData.FarmTotalIcon = _tbItem.Icon;
                    }
                    break;
                }
                if (_land.State == (int) LandState.Growing)
                {
                    var _dif = (int) (_land.MatureTime - Game.Instance.ServerTime).TotalSeconds;
                    if (_dif <= 0)
                    {
                        var _tbItem = Table.GetItemBase(_land.Type);
                        if (_tbItem != null)
                        {
                            PlayerDataManager.Instance.NoticeData.FarmTotalIcon = _tbItem.Icon;
                        }
                        _isMature = true;
                        break;
                    }
                    if (_minSec == -1 || _minSec > _dif)
                    {
                        _minSec = _dif;
                    }
                }
            }

            PlayerDataManager.Instance.NoticeData.FarmTotal = _isMature;
            if (_isMature == false)
            {
                if (_minSec != -1)
                {
                    //等待scends刷新标志
                    m_noticeFarmTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(_minSec + 1),
                        () => { RenewFarmMark(); });
                    //NetManager.Instance.StartCoroutine(AnalyseNoticeFarmCoroutine(minSec));
                }
            }
        }

        //申请建筑数据
        private void ApplyForBuildData()
        {
            NetManager.Instance.StartCoroutine(ApplyForBuildDataCoroutine());
        }

        //申请建筑数据
        private IEnumerator ApplyForBuildDataCoroutine()
        {
            if (BuildingData == null)
            {
                yield break;
            }
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyCityBuildingData(BuildingData.AreaId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var _index = 0;
                        foreach (var _data in CityManager.Instance.BuildingDataList)
                        {
                            if (_data.AreaId == BuildingData.AreaId)
                            {
                                break;
                            }
                            _index++;
                        }
                        CityManager.Instance.BuildingDataList[_index] = _msg.Response;
                        RenewBuildData(_msg.Response);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("ApplyCityBuildingData............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ApplyCityBuildingData............State..." + _msg.State);
                }
            }
        }

        //0type 0=提交 1=放弃 2 花费去刷新
        private IEnumerator CityTaskCoroutine(int index, int type, int cost)
        {
            using (new BlockingLayerHelper(0))
            {
                var _orderData = m_DataModel.SelectOrder;
                var _msg = NetManager.Instance.CityMissionOperation(type, index, cost);

                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        switch (type)
                        {
                            case 0:
                            {
                                PlatformHelper.Event("city", "farmOrderComplete");
                                EventDispatcher.Instance.DispatchEvent(new FarmOrderFlyAnim());
                                PlayerDataManager.Instance.BagItemCountChange(_msg.Response);
                                var _keys = m_dicOrdIdToCount.Keys.ToList();
                                foreach (var i in _keys)
                                {
                                    m_dicOrdIdToCount[i] = PlayerDataManager.Instance.GetItemCount(i);
                                }
                                //   var responseCount= msg.Response.BagsChange.
                                //foreach (var ii in msg.Response.BagsChange)
                                //{
                                //    var value = ii.Value;
                                //    foreach (var ss in value.ItemsChange)
                                //    {
                                //        var value2 = ss.Value;
                                //        if (DicOrdIdToCount.ContainsKey(value2.ItemId))
                                //        {
                                //            DicOrdIdToCount[value2.ItemId] = value2.Count;
                                //        }
                                //    }
                                //}
                                SetOrderWaitingTime(index);
                                RenewSelectedOrder();
                                CanShowSubmit();
                            }
                                break;
                            case 1:
                            {
                                //不在用这个放弃
                            }
                                break;
                            case 2:
                            {
//购买成功刷新一次
                                NetManager.Instance.StartCoroutine(CityRenewTaskCoroutine(-1));
                            }
                                break;
                        }
                    }
                    else if (_msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                    {
                        GameUtils.ShowNetErrorHint(_msg.ErrorCode);
                    }
                    else if (_msg.ErrorCode == (int) ErrorCodes.Error_DataOverflow
                             || _msg.ErrorCode == (int) ErrorCodes.Unknow
                             || _msg.ErrorCode == (int) ErrorCodes.Error_CityMissionNotFind
                             || _msg.ErrorCode == (int) ErrorCodes.Error_CityMissionState
                             || _msg.ErrorCode == (int) ErrorCodes.Error_CityMissionTime)
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        NetManager.Instance.StartCoroutine(CityRenewTaskCoroutine(-1));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("CityMissionOperation............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("CityMissionOperation............State..." + _msg.State);
                }
            }
        }

        //请求、刷新订单
        private IEnumerator CityRenewTaskCoroutine(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.CityRefreshMission(type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var _missions = _msg.Response.CityMissions;
                        m_selectIndex = -1;
                        RenewOrderFormList(_missions);
                        FindMissionTimeSetting();
                        RenewSelectedOrder();
                        CanShowSubmit();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("CityRefreshMissionCoroutine............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("CityRefreshMissionCoroutine............State..." + _msg.State);
                }
            }
        }

        private void ShutLandInterface()
        {
            m_DataModel.LandMenuData.Index = -1;
        }

        //删除订单
        private IEnumerator DeleteTaskCoroutine(int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.DropCityMission(index);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        ResetOneOrder(_msg.Response, index);
                        RenewSelectedOrder();
                    }
                    else if (_msg.ErrorCode == (int) ErrorCodes.Error_CityMissionFreeCd)
                    {
                        SetOrderWaitingTime(index);
                        RenewSelectedOrder();
                    }
                    else if (_msg.ErrorCode == (int) ErrorCodes.Error_DataOverflow
                             || _msg.ErrorCode == (int) ErrorCodes.Error_CityMissionState
                             || _msg.ErrorCode == (int) ErrorCodes.Error_BuildNotFind)
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        NetManager.Instance.StartCoroutine(CityRenewTaskCoroutine(-1));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("DropCityMission............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("DropCityMission............State..." + _msg.State);
                }
            }
        }

        private int RepairPlantNeedTime(PlantRecord tbPlant)
        {
            var _TimeRef = 0;

            foreach (var _skill in m_petSkill)
            {
                if (tbPlant.PlantType == _skill.Param[0] && _skill.EffectId == 2)
                {
                    if (0 != _skill.Param[1])
                    {
                        _TimeRef += _skill.Param[1];
                    }
                }
            }
            return _TimeRef;
        }

        private bool GetWeakMessageState()
        {
            //空地
            var _c = m_DataModel.CropLand.Count;
            var _ret = false;
            for (var i = 0; i < _c; i++)
            {
                var _land = m_DataModel.CropLand[i];
                if (_land.State == (int) LandState.Blank)
                {
                    _ret = true;
                    break;
                }
            }

            if (_ret == false)
            {
                return false;
            }

            //种子
            var _bag = PlayerDataManager.Instance.GetBag((int) eBagType.FarmDepot);
            if (_bag == null)
            {
                return false;
            }

            var _c2 = _bag.Items.Count;
            for (var i = 0; i < _c2; i++)
            {
                var _item = _bag.Items[i];
                if (_item.ItemId < 1)
                {
                    continue;
                }
                var _table = Table.GetItemBase(_item.ItemId);
                if (_table == null)
                {
                    continue;
                }
                if (_table.Type == 90000)
                {
                    return true;
                }
            }

            return false;
        }

        //若干秒后设置作物为成熟期
        private IEnumerator SetCropsRipe(int scends, FarmCropDataModel data)
        {
            yield return new WaitForSeconds(scends);
            data.State = (int) LandState.Mature;
        }

        //菜单翻页
        private void NextPageMenu()
        {
            if (m_DataModel.LandMenuData.TotalPage >= m_DataModel.LandMenuData.SeedPage + 1)
            {
                m_DataModel.LandMenuData.SeedPage++;
            }
            else if (m_DataModel.LandMenuData.TotalPage < m_DataModel.LandMenuData.SeedPage + 1)
            {
                m_DataModel.LandMenuData.SeedPage = 1;
            }
            SetMenuShowObject(m_DataModel.LandMenuData.MenuList);
        }

        //服务器返回数据赋值到订单数据
        private void ServerDataToOrder(BuildMissionOne missionOne, FarmOrderData orderData)
        {
            var _missionId = missionOne.MissionId;
            orderData.OrderId = _missionId;
            //orderData.Index = index;
            var _itemCount = missionOne.ItemIdList.Count;
            if (_itemCount > 6)
            {
                Logger.Error("物品太多了 配置有问题~~");
                _itemCount = 6;
            }
            for (var i = 0; i < _itemCount; i++)
            {
                var _id = missionOne.ItemIdList[i];
                var _count = missionOne.ItemCountList[i];
                orderData.OrderItems[i].ItemId = _id;
                orderData.OrderItems[i].Count = _count;
                m_dicOrdIdToCount[_id] = PlayerDataManager.Instance.GetItemCount(_id);
            }
            for (var i = _itemCount; i < 6; i++)
            {
                orderData.OrderItems[i].ItemId = -1;
                orderData.OrderItems[i].Count = 0;
            }
            orderData.Gold = missionOne.GiveMoney;
            orderData.Exp = missionOne.GiveExp;
            orderData.ItemId = missionOne.GiveItem;
            orderData.State = missionOne.State;
            orderData.RefresTime = Extension.FromServerBinary(missionOne.RefreshTime);
        }

  

        //删除订单
        private void DeleteOrderClick()
        {
            if (m_DataModel.RemainCount > 0)
            {
                //今日订单免费刷新次数还有N次，是否刷新这个订单？
                var _str = string.Format(GameUtils.GetDictionaryText(270241), m_DataModel.RemainCount);
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _str, "",
                    () => { NetManager.Instance.StartCoroutine(DeleteTaskCoroutine(m_selectIndex)); });
            }
            else
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270242, "", () =>
                {
                    //今日订单免费刷新次数已经用完，是否删除这个订单？
                    NetManager.Instance.StartCoroutine(DeleteTaskCoroutine(m_selectIndex));
                });
            }
        }

        //刷新订单
        private void RenewOrderClick(int index)
        {
            var _orderData = m_DataModel.Orders[index];
            if (_orderData.State != (int) CityMissionState.Wait)
            {
                return;
            }
            var _dif = _orderData.RefresTime - Game.Instance.ServerTime;
            var _totalCost = (int) Math.Ceiling((float) _dif.TotalSeconds/(60.0f*5))*GameUtils.OrderRefreshCost;

            if (_totalCost <= 0)
            {
                NetManager.Instance.StartCoroutine(CityTaskCoroutine(index, 2, 0));
            }
            else
            {
                //是否花费{0}钻石立刻获得订单？
                var _dicStr = GameUtils.GetDictionaryText(270098);
                var _str = string.Format(_dicStr, _totalCost);
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _str, "", () =>
                {
                    if (_totalCost > PlayerDataManager.Instance.GetRes((int) eResourcesType.DiamondRes))
                    {
                        var _e = new ShowUIHintBoard(210102);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        return;
                    }
                    NetManager.Instance.StartCoroutine(CityTaskCoroutine(index, 2, _totalCost));
                });
            }
        }

        private void RenewOrderMinTimeClick()
        {
            var _count = m_DataModel.Orders.Count;
            var _index = -1;
            for (var i = 0; i < _count; i++)
            {
                var _orderData = m_DataModel.Orders[i];
                if (_orderData.State != (int) CityMissionState.Wait)
                {
//有不在等待中的
                    SetChooseOrderData(i);
                    break;
                }
                if (_orderData.RefresTime == m_DataModel.MinRefreshTime)
                {
                    _index = i;
                }
            }
            if (_index == -1)
            {
                _index = 0;
            }

            RenewOrderClick(_index);
        }

        //订单提交
        private void HandinOrderClick()
        {
            if (m_selectIndex < 0 || m_selectIndex >= m_DataModel.Orders.Count)
            {
                return;
            }
            var _order = m_DataModel.Orders[m_selectIndex];
            if (_order.State == (int) CityMissionState.Wait)
            {
                return;
            }
            for (var i = 0; i < _order.OrderItems.Count; i++)
            {
                var _itemId = _order.OrderItems[i].ItemId;
                if (_itemId == -1)
                {
                    continue;
                }
                var _itemCount = _order.OrderItems[i].Count;
                var _count = PlayerDataManager.Instance.GetItemCount(_itemId);
                if (_count < _itemCount)
                {
                    //材料不足
                    var _e = new ShowUIHintBoard(210101);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
            }
            NetManager.Instance.StartCoroutine(CityTaskCoroutine(m_selectIndex, 0, 0));
        }

   


  
        //土地点击事件
        private void LandCellClick(int index)
        {
            var _menu = m_DataModel.LandMenuData;
            _menu.SeedPage = 1;
            if (_menu.Index == index)
            {
                return;
            }
            var _landData = m_DataModel.CropLand[index];

            var _state = (LandState) _landData.State;
            switch (_state)
            {
                case LandState.Lock:
                    break;
                case LandState.Blank:
                {
                    DisplaySeedMenu(index);
                }
                    break;
                case LandState.Growing:
                {
                    DisplayGrowthMenu(index);
                }
                    break;
                case LandState.Mature:
                {
                    ShowHarvestMenu(index);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //菜单物体拖拽处理
        private void LandElementDrag(int index)
        {
            var _landData = m_DataModel.CropLand[index];
            var _state = (MenuState) m_DataModel.LandMenuData.State;
            var _menuData = m_DataModel.LandMenuData.MenuList[m_DataModel.LandMenuData.DragIndex];
            switch (_state)
            {
                case MenuState.Invalid:
                    break;
                case MenuState.Seed:
                {
                    if (_landData.State != (int) LandState.Blank)
                    {
                        return;
                    }
                    if (_menuData.Data == 0)
                    {
                        return;
                    }

                    FarmOperationContract(index, OperateType.Seed, _menuData.Id);
                }
                    break;
                case MenuState.Growing:
                {
                    if (_landData.State != (int) LandState.Growing)
                    {
                        return;
                    }
                    if (_menuData.Data == 0)
                    {
                        return;
                    }
                    FarmOperationContract(index, OperateType.Speedup, _menuData.Id);
                }
                    break;
                case MenuState.Mature:
                {
                    if (_landData.State != (int) LandState.Mature)
                    {
                        return;
                    }
                    FarmOperationContract(index, OperateType.Mature);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void RenewBuildData(BuildingData buildingData)
        {
            if (buildingData == null)
            {
                return;
            }
            BuildingData = buildingData;
            var _buildingDataExdataCount0 = buildingData.Exdata.Count;
            for (var i = 0; i < _buildingDataExdataCount0; i++)
            {
                m_DataModel.CropLand[i].Type = buildingData.Exdata[i];
                if (m_DataModel.CropLand[i].Type != -1)
                {
                    m_DataModel.CropLand[i].MatureTime = Extension.FromServerBinary(buildingData.Exdata64[i]);
                    if (m_DataModel.CropLand[i].MatureTime < Game.Instance.ServerTime)
                    {
                        m_DataModel.CropLand[i].Index = i;
                        m_DataModel.CropLand[i].State = (int) LandState.Mature;
                    }
                    else
                    {
                        m_DataModel.CropLand[i].State = (int) LandState.Growing;
                        m_DataModel.CropLand[i].Index = i;
                        if (m_DataModel.CropLand[i].MatureTimer != null)
                        {
                            NetManager.Instance.StopCoroutine(m_DataModel.CropLand[i].MatureTimer);
                        }
                        var _scends = (int) (m_DataModel.CropLand[i].MatureTime - Game.Instance.ServerTime).TotalSeconds;
                        m_DataModel.CropLand[i].MatureTimer =
                            NetManager.Instance.StartCoroutine(SetCropsRipe(_scends, m_DataModel.CropLand[i]));
                    }
                }
                else
                {
                    m_DataModel.CropLand[i].Index = i;
                    m_DataModel.CropLand[i].Type = -1;
                    m_DataModel.CropLand[i].State = (int) LandState.Blank;
                }
            }

            m_tbBuilding = Table.GetBuilding(buildingData.TypeId);
            if (m_tbBuilding == null)
            {
                return;
            }
            m_tbBuildingService = Table.GetBuildingService(m_tbBuilding.ServiceId);
            if (m_tbBuildingService == null)
            {
                return;
            }
            var _depot = PlayerDataManager.Instance.GetBag((int) eBagType.FarmDepot);

            if (_depot == null)
            {
                return;
            }

            if (buildingData.Exdata.Count < m_tbBuildingService.Param[0])
            {
                for (var i = buildingData.Exdata.Count; i < m_tbBuildingService.Param[0]; i++)
                {
                    m_DataModel.CropLand[i].Type = -1;
                    m_DataModel.CropLand[i].Index = i;
                    m_DataModel.CropLand[i].State = (int) LandState.Blank;
                }
            }

            for (var i = m_tbBuildingService.Param[0]; i < 15; i++)
            {
                m_DataModel.CropLand[i].Type = -1;
                m_DataModel.CropLand[i].Index = i;
                m_DataModel.CropLand[i].State = (int) LandState.Lock;
            }


            var _index = 0;
            _depot.Capacity = m_tbBuildingService.Param[2];
            {
                // foreach(var item in depot.Items)
                var __enumerator2 = (_depot.Items).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var item = __enumerator2.Current;
                    {
                        if (_index < m_tbBuildingService.Param[2])
                        {
                            item.Status = (int) eBagItemType.UnLock;
                        }
                        else
                        {
                            item.Status = (int) eBagItemType.Lock;
                        }
                        _index++;
                    }
                }
            }

            RenewFarmMark();
        }

        //刷新订单列表
        private void RenewOrderFormList(List<BuildMissionOne> missionList)
        {
            m_dicOrdIdToCount.Clear();
            if (m_refreshCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(m_refreshCoroutine);
                m_refreshCoroutine = null;
            }
            var _list = new List<FarmOrderData>();
            var _index = 0;
            {
                var __list7 = missionList;
                var __listCount7 = __list7.Count;
                for (var __i7 = 0; __i7 < __listCount7; ++__i7)
                {
                    var _missionOne = __list7[__i7];
                    {
                        var _orderData = new FarmOrderData();
                        ServerDataToOrder(_missionOne, _orderData);
                        //orderData.IsSelect = false;
                        _index++;
                        _list.Add(_orderData);
                    }
                }
            }
            for (var i = _list.Count; i < m_orderMaxCount; i++)
            {
                if (i >= 0)
                {
                    var _orderData = new FarmOrderData();
                    _orderData.State = (int) CityMissionState.Lock;
                    _list.Add(_orderData);
                }
            }
            m_DataModel.Orders = new ObservableCollection<FarmOrderData>(_list);
            var _name = GameUtils.GetDictionaryText(270237);
            for (var i = 0; i < m_DataModel.Orders.Count; i++)
            {
                var _level = -1;
                {
                    // foreach(var VARIABLE in dicNum_Level)
                    var __enumerator2 = (m_DicNum_Level).GetEnumerator();
                    while (__enumerator2.MoveNext())
                    {
                        var _VARIABLE = __enumerator2.Current;
                        {
                            if (i < _VARIABLE.Key)
                            {
                                if (-1 == _level)
                                {
                                    _level = _VARIABLE.Value;
                                }
                                else
                                {
                                    _level = Math.Min(_level, _VARIABLE.Value);
                                }
                            }
                        }
                    }
                }
                m_DataModel.Orders[i].OpenName = string.Format(_name, _level);
            }
        }

        private IEnumerator RenewMission()
        {
            yield return new WaitForSeconds((int) (m_DataModel.MinRefreshTime - Game.Instance.ServerTime).TotalSeconds);
            NetManager.Instance.StartCoroutine(CityRenewTaskCoroutine(0));
        }

        private void RenewPetSkillData()
        {
            m_petSkill.Clear();
            if (BuildingData == null)
            {
                return;
            }
            foreach (var _pet in BuildingData.PetList)
            {
                if (_pet == -1)
                {
                    continue;
                }
                var _petData = CityManager.Instance.GetPetById(_pet);
                if (_petData == null)
                {
                    continue;
                }

                var _level = _petData.Exdata[PetItemExtDataIdx.Level];

                var _tbPet = Table.GetPet(_pet);
                for (var _idx = 0; _idx < _tbPet.Speciality.Length; _idx++)
                {
                    var _skillId = _petData.Exdata[PetItemExtDataIdx.SpecialSkill_Begin + _idx];
                    if (-1 == _skillId)
                    {
                        continue;
                    }

                    if (_level < _tbPet.Speciality[_idx])
                    {
                        continue;
                    }
                    var _tablePetSkill = Table.GetPetSkill(_skillId);
                    if (null == _tablePetSkill)
                    {
                        continue;
                    }
                    m_petSkill.Add(_tablePetSkill);
                }
            }
        }

        //刷新选择的订单
        private void RenewSelectedOrder()
        {
            if (m_selectIndex != -1)
            {
                var _orderData = m_DataModel.Orders[m_selectIndex];
                if (_orderData.State == (int) CityMissionState.Wait)
                {
                    ResetChooseOrderData();
                }
            }
            var _count = m_DataModel.Orders.Count;

            for (var i = 0; i < _count; i++)
            {
                var _orderData = m_DataModel.Orders[i];
                if (_orderData.State == (int) CityMissionState.Normal)
                {
                    SetChooseOrderData(i);
                    break;
                }
            }
            if (m_selectIndex == -1)
            {
                m_DataModel.IsAllWaite = true;
            }
            else
            {
                m_DataModel.IsAllWaite = false;
            }
        }

        //重置单个订单
        private void ResetOneOrder(BuildMissionOne mission, int index)
        {
            if (index < 0 || index >= m_DataModel.Orders.Count)
            {
                return;
            }
            var _orderData = m_DataModel.Orders[index];
            ServerDataToOrder(mission, _orderData);
            CanShowSubmit();
        }

        //重置被选中的订单数据
        private void ResetChooseOrderData()
        {
            m_selectIndex = -1;
            m_DataModel.SelectOrder.IsSelect = false;
            m_DataModel.SelectOrder = s_EmptyData;
        }

        //设置农场菜单显示的物体
        private void SetMenuShowObject(ObservableCollection<FarmLandMenuCell> list)
        {
            var _t = 0;
            var _menu = m_DataModel.LandMenuData;
            var _itemCount = _menu.ItemList.Count;
            for (var j = 0; j < _itemCount; j++)
            {
                var _item = _menu.ItemList[j];
                _item.IsEnable = false;
            }

            if (list.Count%_itemCount != 0)
            {
                _menu.TotalPage = list.Count/_itemCount + 1;
            }
            else
            {
                _menu.TotalPage = list.Count/_itemCount;
            }

            //if ((menu.SeedPage - 1) * itemCount > list.Count)
            //{
            //    menu.SeedPage = 1;
            //}
            for (var j = (_menu.SeedPage - 1)*_itemCount; j < list.Count; j++)
            {
                if (_t < _menu.ItemList.Count)
                {
                    var _item = new FarmLandMenuCell(list[j]);
                    _menu.ItemList[_t] = _item;
                    _t++;
                }
                else
                {
                    break;
                }
            }
            if (list.Count > _menu.ItemList.Count)
            {
                _menu.IsShowUpPage = 1;
            }
            else
            {
                _menu.IsShowUpPage = 0;
            }
        }

        //是否显示可提交
        private void CanShowSubmit()
        {
            var _deliverCount = 0;
            for (var i = 0; i < m_DataModel.Orders.Count; i++)
            {
                var _orderItems = m_DataModel.Orders[i].OrderItems;
                var _orderCount = _orderItems.Count;
                if (m_DataModel.Orders[i].State == (int) CityMissionState.Wait ||
                    m_DataModel.Orders[i].State == (int) CityMissionState.Lock) //未开启
                {
                    continue;
                }
                var _canDeliver = true;
                for (var j = 0; j < _orderCount; j++)
                {
                    var _item = _orderItems[j];
                    if (_item.ItemId != -1)
                    {
                        if (m_dicOrdIdToCount[_item.ItemId] >= _item.Count)
                        {
                        }
                        else
                        {
                            _canDeliver = false;
                            break;
                        }
                    }
                }
                m_DataModel.Orders[i].IsCanDeliver = _canDeliver;
                if (_canDeliver)
                {
                    _deliverCount++;
                }
            }
            m_DataModel.DeliverCount = _deliverCount;
        }

        //设置订单等待时间
        private void SetOrderWaitingTime(int index)
        {
            var _orderData = m_DataModel.Orders[index];
            _orderData.State = (int) CityMissionState.Wait;
            if (m_tbBuildingService != null)
            {
                _orderData.RefresTime = Game.Instance.ServerTime.AddMinutes(m_tbBuildingService.Param[3]);
            }
            ResetChooseOrderData();
            FindMissionTimeSetting();
        }

        //设置被选的订单数据
        private void SetChooseOrderData(int index)
        {
            if (index < 0 || index >= m_DataModel.Orders.Count)
            {
                return;
            }
            if (m_selectIndex == index)
            {
                return;
            }
            if (m_DataModel.Orders[index].State == (int) CityMissionState.Lock)
            {
                return;
            }
            var _orderData = m_DataModel.Orders[index];
            m_selectIndex = index;
            m_DataModel.SelectOrder.IsSelect = false;
            m_DataModel.SelectOrder = _orderData;
            m_DataModel.SelectOrder.IsSelect = true;
        }

        //显示成长菜单
        private void DisplayGrowthMenu(int index)
        {
            var _menu = m_DataModel.LandMenuData;
            var _landData = m_DataModel.CropLand[index];
            _menu.State = (int) MenuState.Growing;
            var _list = new List<FarmLandMenuCell>();
            var _cell = new FarmLandMenuCell();
            _cell.Index = 0;
            _cell.Id = -1;
            var _tbPlant = Table.GetPlant(_landData.Type);
            if (_tbPlant == null)
            {
                return;
            }
            if (_tbPlant.CanRemove == 1)
            {
                //铲除
                _cell.Name = GameUtils.GetDictionaryText(270095);
                _cell.IconId = 1002019;
                _list.Add(_cell);
            }

            var _ii = 1;
            for (var i = 0; i < 4; i++)
            {
                var _menuCell = new FarmLandMenuCell();
                var _itemId = 91200 + i;
                var _count = PlayerDataManager.Instance.GetItemCount(_itemId);
                if (_count == 0)
                {
                    continue;
                }
                var _tbItem = Table.GetItemBase(_itemId);
                _menuCell.Name = _tbItem.Name;
                _menuCell.Index = _ii;
                _menuCell.Id = _itemId;
                _menuCell.IconId = _tbItem.Icon;
                _menuCell.Data = _count;
                _menuCell.ItemId = _itemId;
                _ii++;
                _list.Add(_menuCell);
            }
            if (_list.Count == 0)
            {
                _menu.IsShowMenuScroll = false;
            }
            else
            {
                _menu.IsShowMenuScroll = true;
            }
            _menu.MenuList = new ObservableCollection<FarmLandMenuCell>(_list);
            _menu.Index = index;

            //FarmMenuCountRefresh ee = new FarmMenuCountRefresh(list.Count);
            //EventDispatcher.Instance.DispatchEvent(ee);

            var _land = m_DataModel.CropLand[_menu.Index];
            _menu.PlantId = _land.Type;
            var _sec = (int) (_land.MatureTime - Game.Instance.ServerTime).TotalSeconds;
            var _e = new FarmMatureRefresh(_sec, _tbPlant.MatureCycle);
            EventDispatcher.Instance.DispatchEvent(_e);
            SetMenuShowObject(_menu.MenuList);
        }

        //显示收获菜单
        private void ShowHarvestMenu(int index)
        {
            var _menu = m_DataModel.LandMenuData;
            _menu.State = (int) MenuState.Mature;
            var _list = new List<FarmLandMenuCell>();
            var _cell = new FarmLandMenuCell();
            _cell.Index = 0;
            _cell.Id = 0;
            //收获
            _cell.Name = GameUtils.GetDictionaryText(270094);
            _cell.IconId = 1002020;

            _list.Add(_cell);

            _menu.MenuList = new ObservableCollection<FarmLandMenuCell>(_list);
            _menu.Index = index;
            var _land = m_DataModel.CropLand[index];
            _menu.PlantId = _land.Type;
            _menu.IsShowMenuScroll = true;
            SetMenuShowObject(_menu.MenuList);
            //FarmMenuCountRefresh ee = new FarmMenuCountRefresh(list.Count);
            //EventDispatcher.Instance.DispatchEvent(ee);
        }

        //显示种子菜单
        private void DisplaySeedMenu(int index)
        {
            var _menu = m_DataModel.LandMenuData;
            _menu.State = (int) MenuState.Seed;
            var _i = 0;
            var _list = new List<FarmLandMenuCell>();
            var _ss = m_tbBuildingService.Param[1];
            Table.ForeachPlant(recoard =>
            {
                var _itemId = recoard.PlantItemID;
                var _count = PlayerDataManager.Instance.GetItemCount(_itemId);
                if (_count == 0)
                {
                    return true;
                }
                var _tbItem = Table.GetItemBase(_itemId);
                if (_tbItem == null)
                {
                    return true;
                }
                var _cell = new FarmLandMenuCell();
                _cell.Name = recoard.PlantName;
                _cell.Index = _i;
                _cell.Id = recoard.Id;
                _cell.ItemId = _itemId;
                _cell.IconId = _tbItem.Icon;
                _cell.Data = _count;
                if (recoard.PlantLevel > m_tbBuildingService.Param[1])
                {
                    return true;
                    //cell.IsEnable = false;
                }

                _i++;
                _list.Add(_cell);
                return true;
            });
            if (_list.Count == 0)
            {
                //请前往作物商店购买种子
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(270096));
                return;
            }
            _menu.IsShowMenuScroll = true;
            _menu.MenuList = new ObservableCollection<FarmLandMenuCell>(_list);
            _menu.Index = index;
            SetMenuShowObject(_menu.MenuList);
            //FarmMenuCountRefresh ee = new FarmMenuCountRefresh(list.Count);
            //EventDispatcher.Instance.DispatchEvent(ee);
        }

        //农场操作发包
        private void FarmOperationContract(int index, OperateType type, int dataEx = -1)
        {
            switch (type)
            {
                case OperateType.Seed:
                    break;
                case OperateType.Mature:
                    break;
                case OperateType.Speedup:
                    break;
                case OperateType.Wipeout:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            NetManager.Instance.StartCoroutine(FarmOperationContractCoroutine(index, type, dataEx));
        }

        //农场操作发包
        private IEnumerator FarmOperationContractCoroutine(int index, OperateType type, int dataEx)
        {
            using (new BlockingLayerHelper(0))
            {
                var _param = new Int32Array();
                _param.Items.Add((int) type);
                _param.Items.Add(index);

                switch (type)
                {
                    case OperateType.Seed:
                    {
                        _param.Items.Add(dataEx);
                    }
                        break;
                    case OperateType.Mature:
                        break;
                    case OperateType.Speedup:
                    {
                        _param.Items.Add(dataEx);
                    }
                        break;
                    case OperateType.Wipeout:
                        break;
                    default:
                        break;
                }

                var _msg = NetManager.Instance.UseBuildService(BuildingData.AreaId, m_tbBuilding.ServiceId, _param);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var _landData = m_DataModel.CropLand[index];
                        var _data = _msg.Response.Data32;
                        switch (type)
                        {
                            case OperateType.Seed:
                            {
                                EventDispatcher.Instance.DispatchEvent(new FarmCellTipEvent(OperateType.Seed, index, dataEx,
                                    -1));

                                {
                                    // foreach(var menu in DataModel.LandMenuData.MenuList)
                                    var __enumerator3 = (m_DataModel.LandMenuData.MenuList).GetEnumerator();
                                    while (__enumerator3.MoveNext())
                                    {
                                        var _menu = __enumerator3.Current;
                                        {
                                            if (_menu.Id == dataEx)
                                            {
                                                _menu.Data--;
                                                EventDispatcher.Instance.DispatchEvent(new FramDragRefreshCount(_menu.Data,
                                                    _menu.Index));
                                                break;
                                            }
                                        }
                                    }
                                }

                                _landData.Type = dataEx;
                                var _tbPlant = Table.GetPlant(dataEx);
                                _landData.State = (int) LandState.Growing;
                                if (_landData.MatureTimer != null)
                                {
                                    NetManager.Instance.StopCoroutine(_landData.MatureTimer);
                                }
                                var _fix = RepairPlantNeedTime(_tbPlant);
                                var _scends = _tbPlant.MatureCycle*60;
                                if (_fix != 0)
                                {
                                    _scends = _scends*(_fix + 10000)/10000;
                                }
                                _landData.MatureTime = Game.Instance.ServerTime.AddSeconds(_scends);
                                _landData.MatureTimer = NetManager.Instance.StartCoroutine(SetCropsRipe(_scends, _landData));
                                BuildingData.Exdata[index] = _landData.Type;
                                BuildingData.Exdata64[index] = _landData.MatureTime.ToServerBinary();
                                m_DataModel.LandMenuData.Index = -1;
                                if (m_dicOrdIdToCount.ContainsKey(dataEx))
                                {
                                    m_dicOrdIdToCount[dataEx] -= 1;
                                }
                                CanShowSubmit();
                            }
                                break;
                            case OperateType.Mature:
                            {
                                for (int i = 0, imax = _data.Count; i < imax; i += 2)
                                {
                                    var _itemId = m_DataModel.CropLand[index].Type;
                                    PlatformHelper.Event("City", "FarmMature", _itemId);
                                    var _tbPlant = Table.GetPlant(_itemId);
                                    var _ee = new FarmCellTipEvent(OperateType.Mature, index, _data[i], _data[i + 1]);
                                    _ee.Exp = _tbPlant.GetHomeExp;
                                    EventDispatcher.Instance.DispatchEvent(_ee);
                                    if (m_dicOrdIdToCount.ContainsKey(_data[i]))
                                    {
                                        m_dicOrdIdToCount[_data[i]] += _data[i + 1];
                                    }
                                }
                                _landData.State = (int) LandState.Blank;
                                _landData.Type = -1;
                                if (_landData.MatureTimer != null)
                                {
                                    NetManager.Instance.StopCoroutine(_landData.MatureTimer);
                                    _landData.MatureTimer = null;
                                }
                                m_DataModel.LandMenuData.Index = -1;
                                BuildingData.Exdata[index] = -1;
                                BuildingData.Exdata64[index] = Game.Instance.ServerTime.ToServerBinary();
                                m_harvestCount++;
                                if (6 == m_harvestCount)
                                {
                                    if (PlayerDataManager.Instance.GetFlag(523) &&
                                        !PlayerDataManager.Instance.GetFlag(524))
                                    {
                                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ShowPlantDemo());
                                    }
                                }
                                CanShowSubmit();
                            }
                                break;
                            case OperateType.Speedup:
                            {
                                EventDispatcher.Instance.DispatchEvent(new FarmCellTipEvent(OperateType.Speedup, index,
                                    dataEx,
                                    -1));
                                {
                                    // foreach(var menu in DataModel.LandMenuData.MenuList)
                                    var __enumerator4 = (m_DataModel.LandMenuData.MenuList).GetEnumerator();
                                    while (__enumerator4.MoveNext())
                                    {
                                        var _menu = __enumerator4.Current;
                                        {
                                            if (_menu.Id == dataEx)
                                            {
                                                _menu.Data--;
                                                var _e = new FramDragRefreshCount(_menu.Data, _menu.Index);
                                                EventDispatcher.Instance.DispatchEvent(_e);
                                                break;
                                            }
                                        }
                                    }
                                }

                                PlatformHelper.Event("City", "FarmSpeedUp", dataEx);
                                var _tbItem = Table.GetItemBase(dataEx);
                                if (_tbItem.Exdata[0] <= 0)
                                {
                                    _landData.MatureTime = Game.Instance.ServerTime;
                                }
                                else
                                {
                                    _landData.MatureTime = _landData.MatureTime.AddMinutes(-_tbItem.Exdata[0]);
                                }
                                if (_landData.MatureTimer != null)
                                {
                                    NetManager.Instance.StopCoroutine(_landData.MatureTimer);
                                }

                                var _scends = (int) (_landData.MatureTime - Game.Instance.ServerTime).TotalSeconds;

                                if (_scends <= 0)
                                {
                                    _landData.State = (int) LandState.Mature;
                                }
                                else
                                {
                                    _landData.MatureTimer =
                                        NetManager.Instance.StartCoroutine(SetCropsRipe(_scends, _landData));
                                }
                                if (index == m_DataModel.LandMenuData.Index)
                                {
                                    var _tbPlant = Table.GetPlant(_landData.Type);
                                    var _e = new FarmMatureRefresh(_scends, _tbPlant.MatureCycle);
                                    EventDispatcher.Instance.DispatchEvent(_e);
                                }
                                BuildingData.Exdata64[index] = _landData.MatureTime.ToServerBinary();
                                m_DataModel.LandMenuData.Index = -1;
                            }
                                break;
                            case OperateType.Wipeout:
                            {
                                _landData.Type = -1;
                                if (_landData.MatureTimer != null)
                                {
                                    NetManager.Instance.StopCoroutine(_landData.MatureTimer);
                                    _landData.MatureTimer = null;
                                }
                                _landData.State = (int) LandState.Blank;
                                var _e = new FarmMatureRefresh(0, 0);
                                EventDispatcher.Instance.DispatchEvent(_e);
                                BuildingData.Exdata[index] = -1;
                            }
                                break;
                            default:
                                break;
                        }

                        RenewFarmMark();
                    }
                    else if (_msg.ErrorCode == (int) ErrorCodes.Error_AlreadyHaveSeed
                             || _msg.ErrorCode == (int) ErrorCodes.Error_NeedFarmLevelMore
                             || _msg.ErrorCode == (int) ErrorCodes.Error_NotFindSeed
                             || _msg.ErrorCode == (int) ErrorCodes.Error_SeedTimeNotOver
                             || _msg.ErrorCode == (int) ErrorCodes.Error_NotFindSeed)
                    {
                        ApplyForBuildData();
                    }
                    else if (_msg.ErrorCode == (int) ErrorCodes.ParamError
                             || _msg.ErrorCode == (int) ErrorCodes.Error_DataOverflow
                             || _msg.ErrorCode == (int) ErrorCodes.Error_ItemID
                             || _msg.ErrorCode == (int) ErrorCodes.ItemNotEnough
                             || _msg.ErrorCode == (int) ErrorCodes.Error_ItemNot91000)
                    {
                        Logger.Error("UseBuildService............ErrorCode..." + _msg.ErrorCode);
                    }
                    else
                    {
                        Logger.Error("UseBuildService............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("UseBuildService............State..." + _msg.State);
                }
            }
        }


    }
}