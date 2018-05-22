/********************************************************************************* 

                         Scorpion



  *FileName:SevenPaymentFrameCtrler

  *Version:1.0

  *Date:2017-06-27

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
using DataTable;
using EventSystem;
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class SevenPaymentFrameCtrler : IControllerBase
    {
        #region 成员变量
        private SevenRewardDataModel DataModel;

        private enum SevenRewardState
        {
            NotCanGet = 0, //不可领取
            CanGet = 1, //可领取未领取
            HasGot = 2 //已经领取
        }
        #endregion

        #region 构造函数
        public SevenPaymentFrameCtrler()
        {
            EventDispatcher.Instance.AddEventListener(UIEvent_SevenRewardInit.EVENT_TYPE, OnInitializeDatumEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SevenRewardItemClick.EVENT_TYPE, OnProvisionCellHitEvent);
            CleanUp();
        }
        #endregion
        //public SevenRewardController DataModel;
    

        #region 事件
        private void OnInitializeDatumEvent(IEvent ievent)
        {
            var _list = new List<SevenRewardItemDataModel>();
            var _loginDay = PlayerDataManager.Instance.GetExData(eExdataDefine.e94);
            var _flagData = PlayerDataManager.Instance.FlagData;
            Table.ForeachGift(table =>
            {
                if (table.Type == (int)eRewardType.SevenDayReward)
                {
                    var _canGet = false;
                    var _item = new SevenRewardItemDataModel();

                    _item.Day = table.Param[0];
                    _canGet = (_item.Day <= _loginDay);
                    for (var i = 0; i < 3; i++)
                    {
                        _item.Rewards[i].ItemId = table.Param[i * 2 + 1];
                        _item.Rewards[i].Count = table.Param[i * 2 + 2];
                    }
                    _item.Rewards[3].ItemId = table.Param[7];
                    int cou = table.Param[8];
                    if (cou <= 0)
                        cou = 1;
                    _item.Rewards[3].Count = cou;

                    if (_canGet)
                    {
                        if (_flagData.GetFlag(table.Flag) == 1)
                        {
                            _item.State = (int)SevenRewardState.HasGot;
                        }
                        else
                        {
                            _item.State = (int)SevenRewardState.CanGet;
                        }
                    }
                    else
                    {
                        _item.State = (int)SevenRewardState.NotCanGet;
                    }
                    _item.TableId = table.Id;
                    _list.Add(_item);
                }
                return true;
            });
            DataModel.Lists = new ObservableCollection<SevenRewardItemDataModel>(_list);
            StudyAnnouncement();
        }

        private void OnProvisionCellHitEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SevenRewardItemClick;
            var _selectItem = DataModel.Lists[_e.Index];
            NetManager.Instance.StartCoroutine(DesirePaymentCoroutine((int)eActivationRewardType.TableGift,
                _selectItem.TableId, _e.Index));
        }
        #endregion

        #region 逻辑函数
        private void StudyAnnouncement()
        {
            var _count = DataModel.Lists.Count;
            var _isOk = false;
            for (var i = 0; i < _count; i++)
            {
                var _item = DataModel.Lists[i];
                if (_item.State == (int)SevenRewardState.CanGet)
                {
                    _isOk = true;
                }
            }
            PlayerDataManager.Instance.NoticeData.SevenDay = _isOk;
        }

        private IEnumerator DesirePaymentCoroutine(int type, int id, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ActivationReward(type, id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DataModel.Lists[index].State = (int)SevenRewardState.HasGot;
                        StudyAnnouncement();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(".....MatchingCancel.......{0}.", _msg.ErrorCode);
                    }
                }
            }
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new SevenRewardDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            var logInNumDays = PlayerDataManager.Instance.GetExData(eExdataDefine.e94);
            if (logInNumDays < 6)
            {
                DataModel.ShowOnePicture = logInNumDays;
            }
            else
            {
                DataModel.ShowOnePicture = 6;
            }
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

        public void OnShow()
        { 
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

    }
}