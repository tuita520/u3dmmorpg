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

namespace ScriptController
{
    public class NewOfflineExpController : IControllerBase
    {
        private NewOffLineExpDataModel DataModel;

        public NewOfflineExpController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(InitUI_Event.EVENT_TYPE, OnInitUI);
            EventDispatcher.Instance.AddEventListener(OnOfflineExpCloses_Event.EVENT_TYPE, OnOfflineExpCloses);
            EventDispatcher.Instance.AddEventListener(ChangeOfflineTypeEvent.EVENT_TYPE, OnOfflineExpChange);

        }
        public FrameState State { get; set; }
        public void CleanUp()
        {
            DataModel = new NewOffLineExpDataModel();
        }
        private void OnOfflineExpChange(IEvent ievent)
        {
            ChangeOfflineTypeEvent e = ievent as ChangeOfflineTypeEvent;
            if (e == null)
                return;
            DataModel.Multi = e.Type;
        }
        private void OnOfflineExpCloses(IEvent ievent)
        {

            if (DataModel.Multi == 2)
            {
                //var cost = int.Parse(Table.GetClientConfig(589).Value);
                var cost=DataModel.Cost;
                if (cost > PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes))
                {
                    var ec = new Close_UI_Event(UIConfig.NewOfflineExpFrame);
                    EventDispatcher.Instance.DispatchEvent(ec);

                    var _ee = new ShowUIHintBoard(210102);
                    EventDispatcher.Instance.DispatchEvent(_ee);

                    var _e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0 });
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }                
            }
            NetManager.Instance.StartCoroutine(GetOfflineExp());
            var e = new Close_UI_Event(UIConfig.NewOfflineExpFrame);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        private void OnInitUI(IEvent ievent)
        {
            var e = ievent as InitUI_Event;
            if (e == null)
            {
                return;
            }
            NetManager.Instance.StartCoroutine(ApplyOfflineData(e.isShowRewardFrame));

        }
        private IEnumerator GetOfflineExp()
        {
            var _msg = NetManager.Instance.CSApplyOfflineExpData(DataModel.Multi);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply && _msg.ErrorCode != (int)ErrorCodes.OK)
            {
                UIManager.Instance.ShowNetError(_msg.ErrorCode);
                yield break;
            }
            DataModel.IsShow = 0;
            yield break;
        }
        private IEnumerator ApplyOfflineData(bool ishowRewardFrame)
        {
            var _msg = NetManager.Instance.CSApplyOfflineExpData(0);
            yield return _msg.SendAndWaitUntilDone();
            if (_msg.State == MessageState.Reply && _msg.ErrorCode == (int)ErrorCodes.OK)
            {
              
                var ts = new TimeSpan(0, 0, (int)_msg.Response.OfflineTime);
                DataModel.OfflineTime = GameUtils.GetTimeDiffString(ts);
                ts = new TimeSpan(0,0,(int)_msg.Response.RewardTime);
                DataModel.RewardTime = GameUtils.GetTimeDiffString(ts);

                DataModel.OfflineTimeRounding = GameUtils.GetTimeRounddingPeerFiveMinute(ts);
                DataModel.Cost = int.Parse(Table.GetClientConfig(1403).Value) * DataModel.OfflineTimeRounding;

                DataModel.OfflineExp = (int)_msg.Response.AddExp;

                var leftTime = _msg.Response.LeftTime;
                TimeSpan left = new TimeSpan(0, 0, leftTime);
                DataModel.LeftExpTime = GameUtils.GetTimeDiffString(left);

                var resultList = new Dictionary<int, int>();
                resultList.Clear();
                foreach (var value in _msg.Response.Items)
                {
                    if (resultList.ContainsKey(value.itemid))
                    {
                        resultList[value.itemid] += value.count;
                    }
                    else
                    {
                        resultList.Add(value.itemid, value.count);
                    }
                }
                if (_msg.Response.AddMoney > 0)
                {
                    if (resultList.ContainsKey(2))
                    {
                        resultList[2] += (int)_msg.Response.AddMoney;
                    }
                    else
                    {
                        resultList.Add(2, (int)_msg.Response.AddMoney);
                    }
                }

                DataModel.Items.Clear();
                foreach (var data in resultList)
                {
                    var tmpItem = new ItemIdDataModel();
                    tmpItem.ItemId = data.Key;
                    tmpItem.Count = data.Value;
                    DataModel.Items.Add(tmpItem); 
                }

                //showUI
                if (_msg.Response.AddExp > 0 || DataModel.Items.Count > 0 || _msg.Response.AddMoney > 0)
                {
                    DataModel.IsShow = 1;
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.NewOfflineExpFrame, new UIInitArguments()));
                }
                else
                {
                   
                    if (ishowRewardFrame)
                    {
                        UIManager.Instance.ShowUI(UIConfig.RewardFrame, new UIRewardFrameArguments
                        {
                            Tab = 2
                        }); 
                    }
                }
            }
            yield break;
        }

        public void RefreshData(UIInitArguments data)
        {
//         var leftTime = PlayerDataManager.Instance.GetExData((int) eExdataDefine.e742);
//         TimeSpan ts = new TimeSpan(0, 0, leftTime);
//         DataModel.LeftExpTime = GameUtils.GetTimeDiffString(ts);
            DataModel.RoleId  = PlayerDataManager.Instance.mInitBaseAttr.RoleId;
       
        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
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

        }
    }
}
