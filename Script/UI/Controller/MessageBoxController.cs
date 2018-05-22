
/********************************************************************************* 

                         Scorpion




  *FileName:MessageBoxController

  *Version:1.0

  *Date:2017-06-28

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using EventSystem;
using Shared;

#endregion

namespace ScriptController
{
    public class MsgBoxFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        private MessageBoxDataModel DataModel;
        private readonly Queue<object[]> Datas = new Queue<object[]>();
        private bool m_keepOpen;
        private int countDownTime = 10;
        object trigger = null;
        private int btnId;
        private bool isOver = false;
        #endregion

        #region 构造函数
        public MsgBoxFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(MessageBoxClick.EVENT_TYPE, OnMsgBosClickEvent);
            EventDispatcher.Instance.AddEventListener(MessageBoxAutoChooseEvent.EVENT_TYPE, TimeShow);
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new MessageBoxDataModel();
        }

        public void OnChangeScene(int sceneId)
        {
            if (m_keepOpen)
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MessageBox));
            }
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "RefrehMessge")
            {
                var _refreshMessage = true;
                var _keepOpen = (bool)param[6];
                if (_keepOpen)
                {
                    if (m_keepOpen)
                    {
                        Datas.Enqueue(param);
                        _refreshMessage = false;
                    }
                    else
                    {
                        m_keepOpen = true;
                    }
                }
                else if (m_keepOpen)
                {
                    return null;
                }

                if (_refreshMessage)//210001  210000
                {
                    if (param.Length < 8)
                        RenewMsg((MessageBoxType)param[0], param[1] as string, param[2] as string, param[3] as Action,
                       param[4] as Action, (bool)param[5]);
                    else
                        RenewMsg((MessageBoxType)param[0], param[1] as string, param[2] as string, param[3] as Action,
                        param[4] as Action, (bool)param[5], param[7] as string, param[8] as string);
                }
            }

            return null;
        }

        public void OnShow()
        {
        }

        public void Close()
        {
            PlayerDataManager.Instance.NoticeData.DugeonNotMessage = false;
            PlayerDataManager.Instance.isInTeamInvite = false;
            if (m_keepOpen)
            {
                if (Datas.Count > 0)
                {
                    var _param = Datas.Dequeue();
                    if (_param.Length < 8)
                        RenewMsg((MessageBoxType)_param[0], _param[1] as string, _param[2] as string, _param[3] as Action,
                      _param[4] as Action, (bool)_param[5]);
                    else
                        RenewMsg((MessageBoxType)_param[0], _param[1] as string, _param[2] as string, _param[3] as Action,
                        _param[4] as Action, (bool)_param[5], _param[7] as string, _param[8] as string);
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MessageBox));
                }
                else
                {
                    m_keepOpen = false;
                }
            }
            isOver = true;
            DataModel.IsChancelBtnTime = false;
            DataModel.IsOKBtnTime = false;
            if (trigger != null)
            {
                TimeManager.Instance.DeleteTrigger(trigger);
            }
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private void OnMsgBosClickEvent(IEvent ievent)
        {

            var _e = ievent as MessageBoxClick;
            if (_e.Type == 0)
            {
                if (DataModel.CacelAction != null)
                {
                    DataModel.CacelAction();
                }
            }
            else if (_e.Type == 1)
            {
                if (DataModel.OkAction != null)
                {
                    DataModel.OkAction();
                }
            }
        }

        private void TimeShow(IEvent ieve)
        {        
            isOver = false;
            var _e = ieve as MessageBoxAutoChooseEvent;
            if (!_e.BtnOk)
            {
                DataModel.IsChancelBtnTime = true;
                DataModel.IsOKBtnTime = false;
                btnId = 0;
            }
            else
            {
                DataModel.IsOKBtnTime = true;
                DataModel.IsChancelBtnTime = false;
                btnId = 1;
            }
            if (trigger != null)
            {
                TimeManager.Instance.DeleteTrigger(trigger);
            }
        
            // to set time update
            {
                countDownTime = _e.CountDown;
                DataModel.CountDown = string.Format("({0})", countDownTime.ToString() + "s"); ;
                trigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(1), RefreshTime, 1000);
            }
        }     

        void RefreshTime()
        {
            countDownTime--;      

            if (countDownTime < 0)
            {           
                TimeManager.Instance.DeleteTrigger(trigger);

                if (isOver)
                {               
                    DataModel.IsChancelBtnTime = false;
                    DataModel.IsOKBtnTime = false;
                    return;
                }

                var e = new MessageBoxClick(btnId);
                EventDispatcher.Instance.DispatchEvent(e);

                var e1 = new Close_UI_Event(UIConfig.MessageBox);
                EventDispatcher.Instance.DispatchEvent(e1);

                DataModel.IsChancelBtnTime = false;
                DataModel.IsOKBtnTime = false;

                return;
            }

            DataModel.CountDown = string.Format("({0})", countDownTime.ToString() + "s");
        }

        #endregion






        private void RenewMsg(MessageBoxType boxType,
            string info,
            string title = "",
            Action okAction = null,
            Action cancelAction = null,
            bool isSystemInfo = false,
            string okStr = "",
            string cancleStr = ""
            )
        {
            DataModel.BoxType = (int)boxType;
            DataModel.Info = info;
            if (string.IsNullOrEmpty(title))
            {
                title = GameUtils.GetDictionaryText(200099);
            }
            DataModel.Title = title;
            if (string.IsNullOrEmpty(okStr))
            {
                okStr = GameUtils.GetDictionaryText(210000);
            }
            if (string.IsNullOrEmpty(cancleStr))
            {
                cancleStr = GameUtils.GetDictionaryText(210001);
            }
            DataModel.OKStr = okStr;
            DataModel.CancleStr = cancleStr;
            DataModel.OKStr1 = okStr;
            DataModel.CancleStr1 = cancleStr;
            DataModel.OkAction = okAction;
            DataModel.CacelAction = cancelAction;
            if (isSystemInfo)
            {
                DataModel.Depth = 100000;
            }
            else
            {
                DataModel.Depth = 10000;
            }
        }


    }
}