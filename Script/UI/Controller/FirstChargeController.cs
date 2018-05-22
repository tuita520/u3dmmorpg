/********************************************************************************* 

                         Scorpion




  *FileName:FirstChargeController

  *Version:1.0

  *Date:2017-06-12

  *Description:

**********************************************************************************/

#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using EventSystem;
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class StartResponsibleFrameCtrler : IControllerBase
    {

        #region 静态变量

        #endregion

        #region 成员变量
        private FirstChargeDataModel m_DataModel;
        private int m_isCharged1 = 0;
        private int m_isCharged2 = 0;
        private int m_isCharged3 = 0;
        #endregion

        #region 构造函数
        public StartResponsibleFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(FirstChargeBtnClick_Event.EVENT_TYPE, OnStartResponBtnClickEvent);
            EventDispatcher.Instance.AddEventListener(FirstChargeCloseBtnClick_Event.EVENT_TYPE, OnBeginResponCloseBtnClickEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExchangeDataUpDataEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnApplyBeginResponItemEvent);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagRisingDataEvent);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagInitializationDataEvent);
            EventDispatcher.Instance.AddEventListener(FirstChargeToggleSuccess_Event.EVENT_TYPE, OnSwitchClickEvent);

        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_DataModel = new FirstChargeDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            SetBackGroundColor();
            RefreshWithOutItems();
        }

        private void SetBackGroundColor()
        {
            switch (m_DataModel.ToggleSelect)
            {
                case 0:
                    m_DataModel.BackColor = GameUtils.GetTableColor(26); 
                    break;
                case 1:
                    m_DataModel.BackColor = GameUtils.GetTableColor(23);
                    break;
                case 2:
                    m_DataModel.BackColor = GameUtils.GetTableColor(22);
                    break;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
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
            if (PlayerDataManager.Instance.FirstChargeData != null && PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList != null &&
                PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList.Count > m_DataModel.ToggleSelect)
            {
                var _temp = "";
                if (PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath.Count > 0)
                {
                    if (0 == PlayerDataManager.Instance.GetRoleId())
                    {
                        _temp = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath[0];
                        var _ex = new FirstChargeModelDisplay_Event(_temp, m_DataModel.ToggleSelect);
                        EventDispatcher.Instance.DispatchEvent(_ex);
                    }
                    if (1 == PlayerDataManager.Instance.GetRoleId())
                    {
                        _temp = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath[1];
                        var _ex = new FirstChargeModelDisplay_Event(_temp, m_DataModel.ToggleSelect);
                        EventDispatcher.Instance.DispatchEvent(_ex);
                    }
                    if (2 == PlayerDataManager.Instance.GetRoleId())
                    {
                        _temp = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath[2];
                        var _ex = new FirstChargeModelDisplay_Event(_temp, m_DataModel.ToggleSelect);
                        EventDispatcher.Instance.DispatchEvent(_ex);
                    }
                }
            }
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private void OnApplyBeginResponItemEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ApplyStartResponItemCoroutine());
        }

        private void OnSwitchClickEvent(IEvent ievent)
        {
            var _e = ievent as FirstChargeToggleSuccess_Event;
            if (_e != null)
            {
                m_DataModel.ToggleSelect = _e.index;
            }
            else
            {
                m_DataModel.ToggleSelect = 0;
            }

            InitButtonState();
            if (State == FrameState.Open)
            {
                if (PlayerDataManager.Instance.FirstChargeData != null && PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList != null &&
                    PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList.Count > m_DataModel.ToggleSelect)
                {
                    var _temp = "";
                    if (PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath.Count > 0)
                    {
                        if (0 == PlayerDataManager.Instance.GetRoleId())
                        {
                            _temp = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath[0];
                            var _ex = new FirstChargeModelDisplay_Event(_temp, m_DataModel.ToggleSelect);
                            EventDispatcher.Instance.DispatchEvent(_ex);
                        }
                        if (1 == PlayerDataManager.Instance.GetRoleId())
                        {
                            _temp = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath[1];
                            var _ex = new FirstChargeModelDisplay_Event(_temp, m_DataModel.ToggleSelect);
                            EventDispatcher.Instance.DispatchEvent(_ex);
                        }
                        if (2 == PlayerDataManager.Instance.GetRoleId())
                        {
                            _temp = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].modelPath[2];
                            var _ex = new FirstChargeModelDisplay_Event(_temp, m_DataModel.ToggleSelect);
                            EventDispatcher.Instance.DispatchEvent(_ex);
                        }
                    }
                }
            }
        }

        private void OnStartResponBtnClickEvent(IEvent ievent)
        {
            //FirstChargeControlBtnClick_Event e = ievent as FirstChargeControlBtnClick_Event;
            var _flag = -1;
            if (PlayerDataManager.Instance.FirstChargeData != null && PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList.Count > m_DataModel.ToggleSelect)
            {
                _flag = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[m_DataModel.ToggleSelect].flag;
            }
            else
            {
                return;
            }

            if (m_DataModel.ToggleSelect == 0)
            {
                if (m_isCharged1 == 0) // 未充值过
                {
                    var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                else if (m_isCharged1 == 1) // 充值过了
                {
                    if (PlayerDataManager.Instance.GetFlag(_flag))// 领取过了
                    {
                        var _e = new ShowUIHintBoard(100001115);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else// 没领取过
                    {
                        // 领取逻辑
                        NetManager.Instance.StartCoroutine(ApplyGainStartResponItem(0));
                    }
                }
            }
            else if (m_DataModel.ToggleSelect == 1)
            {
                if (m_isCharged2 == 0) // 未充值过
                {
                    var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                else if (m_isCharged2 == 1) // 充值过了
                {
                    if (PlayerDataManager.Instance.GetFlag(_flag))// 领取过了
                    {
                        var _e = new ShowUIHintBoard(100001115);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else// 没领取过
                    {
                        // 领取逻辑
                        NetManager.Instance.StartCoroutine(ApplyGainStartResponItem(1));
                    }
                }
            }
            else if (m_DataModel.ToggleSelect == 2)
            {
                if (m_isCharged3 == 0) // 未充值过
                {
                    var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                else if (m_isCharged3 == 1) // 充值过了
                {
                    if (PlayerDataManager.Instance.GetFlag(_flag))// 领取过了
                    {
                        var _e = new ShowUIHintBoard(100001115);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else// 没领取过
                    {
                        // 领取逻辑
                        NetManager.Instance.StartCoroutine(ApplyGainStartResponItem(2));
                    }
                }
            }
        }

        private void OnBeginResponCloseBtnClickEvent(IEvent ievent)
        {
            var _e = new Close_UI_Event(UIConfig.FirstChargeFrame);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        private void OnExchangeDataUpDataEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;

            if (_e.Key == (int)eExdataDefine.e652)
            {
                InitButtonState();
                FirstChargeUpdate();
                FirstChargeNotice();
            }
            else if (_e.Key == (int)eExdataDefine.e69)
            {
                FirstChargeUpdate();
                FirstChargeNotice();
            }
        }

        private void OnFlagInitializationDataEvent(IEvent ievent)
        {
            RenewMainUIstartResponBtn();
        }

        private void OnFlagRisingDataEvent(IEvent ievent)
        {
            var e = ievent as FlagUpdateEvent;
            if (State == FrameState.Open)
            {
                InitButtonState();
            }
            if (PlayerDataManager.Instance.FirstChargeData != null)
            {
                for (int i = 0; i < PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList.Count; i++)
                {
                    var _flag = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList[i].flag;
                    if (e.Index == _flag)
                    {
                        RenewMainUIstartResponBtn();
                        FirstChargeNotice();
                    }
                }
            }
            else
            {
                return;
            }
        }

        #endregion






        private IEnumerator ApplyStartResponItemCoroutine()
        {
            var _msg = NetManager.Instance.ApplyFirstChargeItem(0);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply && _msg.ErrorCode == (int) ErrorCodes.OK)
            {
                PlayerDataManager.Instance.FirstChargeData = _msg.Response;
                RenewMainUIstartResponBtn();
                InitButtonState();
                RenewItems();
                FirstChargeNotice();
            }
        }

        private IEnumerator ApplyGainStartResponItem(int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyGetFirstChargeItem(index);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply && _msg.ErrorCode == (int) ErrorCodes.OK)
                {
                    if (_msg.Response == 0) // 失敗
                    {
                    }
                    else if (_msg.Response == 1) // 成功
                    {
                        var _tempDic = new Dictionary<int, int>();
                        if (index == 0 && m_DataModel != null)
                        {
                            foreach (var _data in m_DataModel.RechargeItemsTab1)
                            {
                                _tempDic.Add(_data.ItemId, _data.Count);
                            }
                        }
                        else if (index == 1 && m_DataModel != null)
                        {
                            foreach (var _data in m_DataModel.RechargeItemsTab2)
                            {
                                _tempDic.Add(_data.ItemId, _data.Count);
                            }
                        }
                        else if (index == 2 && m_DataModel != null)
                        {
                            foreach (var _data in m_DataModel.RechargeItemsTab3)
                            {
                                _tempDic.Add(_data.ItemId, _data.Count);
                            }
                        }
                    
                        var _e = new ShowItemsArguments
                        {
                            Items = _tempDic
                        };
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ShowItemsFrame, _e));

                        var _iEvent = new FirstChargeGetItemSuccess_Event();
                        EventDispatcher.Instance.DispatchEvent(_iEvent);
                    }
                }
                else
                {
                    GameUtils.ShowNetErrorHint(_msg.ErrorCode);
                }
            }
        }

  
        private void InitButtonState()
        {
            m_isCharged1 = 0;
            m_DataModel.WanChengState1 = 0;
            m_DataModel.btnState1 = 1;

            m_isCharged2 = 0;
            m_DataModel.WanChengState2 = 0;
            m_DataModel.btnState2 = 1;

            m_isCharged3 = 0;
            m_DataModel.WanChengState3 = 0;
            m_DataModel.btnState3 = 1;
            if (PlayerDataManager.Instance.FirstChargeData == null || PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList == null)
            {
                return;
            }
            var _index = 0;
            foreach (var _data in PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList)
            {
                _index++;
                if (_index == 1)
                {
                    if (PlayerDataManager.Instance.GetFlag(_data.flag)) // 领取过
                    {
                        m_DataModel.WanChengState1 = 1;
                        m_DataModel.btnState1 = 0;
                        var iEvent = new FirstChargeCloseBtnClick_Event();
                        EventDispatcher.Instance.DispatchEvent(iEvent);
                    }

                    if (PlayerDataManager.Instance.GetExData((int)eExdataDefine.e652) >= _data.diamond)
                    {
                        m_isCharged1 = 1;
                    }
                    else
                    {
                        m_isCharged1 = 0;
                    }
                }
                if (_index == 2)
                {
                    if (PlayerDataManager.Instance.GetFlag(_data.flag)) // 领取过
                    {
                        m_DataModel.WanChengState2 = 1;
                        m_DataModel.btnState2 = 0;
                        var iEvent = new FirstChargeCloseBtnClick_Event();
                        EventDispatcher.Instance.DispatchEvent(iEvent);
                    }

                    if (PlayerDataManager.Instance.GetExData((int)eExdataDefine.e652) >= _data.diamond)
                    {
                        m_isCharged2 = 1;
                    }
                    else
                    {
                        m_isCharged2 = 0;
                    }
                }
                if (_index == 3)
                {
                    if (PlayerDataManager.Instance.GetFlag(_data.flag)) // 领取过
                    {
                        m_DataModel.WanChengState3 = 1;
                        m_DataModel.btnState3 = 0;
                        var iEvent = new FirstChargeCloseBtnClick_Event();
                        EventDispatcher.Instance.DispatchEvent(iEvent);
                    }

                    if (PlayerDataManager.Instance.GetExData((int)eExdataDefine.e652) >= _data.diamond)
                    {
                        m_isCharged3 = 1;
                    }
                    else
                    {
                        m_isCharged3 = 0;
                    }
                }
            }
        }

        private void RenewItems()
        {
            var _chargeDataList = PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList;
            int index = 0;
            foreach (var _dataList in _chargeDataList)
            {
                index++;
                if (index == 1)
                {
                    m_DataModel.RechargeItemsTab1.Clear();
                    foreach (var _item in _dataList.items)
                    {
                        var _temp = new ItemIdDataModel();
                        _temp.ItemId = _item.itemid;
                        _temp.Count = _item.count;
                        m_DataModel.RechargeItemsTab1.Add(_temp);
                    }
                    m_isCharged1 = _dataList.isCharged;
                }

                if (index == 2)
                {
                    m_DataModel.RechargeItemsTab2.Clear();
                    foreach (var _item in _dataList.items)
                    {
                        var _temp = new ItemIdDataModel();
                        _temp.ItemId = _item.itemid;
                        _temp.Count = _item.count;
                        m_DataModel.RechargeItemsTab2.Add(_temp);
                    }
                    m_isCharged2 = _dataList.isCharged;
                }

                if (index == 3)
                {
                    m_DataModel.RechargeItemsTab3.Clear();
                    foreach (var _item in _dataList.items)
                    {
                        var _temp = new ItemIdDataModel();
                        _temp.ItemId = _item.itemid;
                        _temp.Count = _item.count;
                        m_DataModel.RechargeItemsTab3.Add(_temp);
                    }
                    m_isCharged3 = _dataList.isCharged;
                }
            }
        }

        private void RenewMainUIstartResponBtn()
        {
            // 主界面首冲图标是否显示
            if (PlayerDataManager.Instance.FirstChargeData == null || PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList == null)
            {
                return;
            }

            var _isAllGet = true;

            foreach (var _data in PlayerDataManager.Instance.FirstChargeData.FirstChagreItemList)
            {
                if (!PlayerDataManager.Instance.GetFlag(_data.flag))
                {
                    _isAllGet = false;
                }
            }

            if (_isAllGet)
            {
                m_DataModel.isShowMainUIFirstChagre = 0;
            }
            else
            {
                m_DataModel.isShowMainUIFirstChagre = 1;
            }
        }

        private void RefreshWithOutItems()
        {
            // 剑士100001111 魔法师100001112 弓箭手100001113
            var _str = "";
            if (0 == PlayerDataManager.Instance.GetRoleId())
            {
                _str = GameUtils.GetDictionaryText(100001111);
            }
            else if (1 == PlayerDataManager.Instance.GetRoleId())
            {
                _str = GameUtils.GetDictionaryText(100001112);
            }
            else if (2 == PlayerDataManager.Instance.GetRoleId())
            {
                _str = GameUtils.GetDictionaryText(100001113);
            }
            m_DataModel.MainStr = _str;

            InitButtonState();
        }
        private void FirstChargeUpdate()
        {
            var result = GameUtils.FirstChargeJudge();
            if (result.Contains(1))
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.FirstChargeFrame));
            }
        }
        private void FirstChargeNotice()
        {
            var notice = PlayerDataManager.Instance.NoticeData;
            var result = GameUtils.FirstChargeJudge();
            int Index = 0;
            if (result[0] != 3)
            {
                Index = 0;
            }
            else if (result[1] != 3)
            {
                Index = 1;
            }
            else if (result[2] != 3)
            {
                Index = 2;
            }
            else
            {
                Index = -1;
            }
            m_DataModel.ToggleSelect = Index;
            if (result.Contains(1))
            {
            
                notice.FirstChargeFlag = true;
            }
            else
            {
                notice.FirstChargeFlag = false;
            }
        }
  
    }
}