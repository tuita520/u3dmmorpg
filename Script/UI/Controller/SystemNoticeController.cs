/********************************************************************************* 

                         Scorpion



  *FileName:SystemNoticeFrameCtrler

  *Version:1.0

  *Date:2017-07-18

  *Description:

**********************************************************************************/


#region using

using System.Collections.Generic;
using System.ComponentModel;
using EventSystem;

#endregion

namespace ScriptController
{
    public class SystemNoticeFrameCtrler : IControllerBase
    {
        #region 构造函数
        public SystemNoticeFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(SystemNoticeOperate.EVENT_TYPE, OnSystemNoticeActEvent);
        }
        #endregion

        #region 成员变量
        private bool mIsSend;
        private readonly List<string> SystemLableStringList = new List<string>();
        #endregion

        #region 逻辑函数
        private void EnhanceContentInfo(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            if (SystemLableStringList.Count > GameUtils.SystemNoticeRollingScreenLimit)
            {
                SystemLableStringList.RemoveRange(0, 5);
            }

            SystemLableStringList.Add(content);
        }

        private void ClearContentInfo()
        {
            SystemLableStringList.Clear();
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SystemNoticeFrame));
        }

        private void NotifyShowAnnouncement()
        {
            if (SystemLableStringList.Count == 0)
            {
                var _e = new SystemNoticeNotify("");
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            else
            {
                var _e = new SystemNoticeNotify(SystemLableStringList[0]);
                EventDispatcher.Instance.DispatchEvent(_e);
                SystemLableStringList.RemoveAt(0);
            }
        }
        #endregion

        #region 事件
        private void OnSystemNoticeActEvent(IEvent ievent)
        {
            var _e = ievent as SystemNoticeOperate;
            switch (_e.Type)
            {
                case 0:
                    NotifyShowAnnouncement();
                    break;
                case 1:
                    ClearContentInfo();
                    break;
            }
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            SystemLableStringList.Clear();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return null;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
            if (SystemLableStringList.Count > 0)
            {
                var _e = new Show_UI_Event(UIConfig.SystemNoticeFrame, new SystemNoticeArguments());
                EventDispatcher.Instance.DispatchEvent(_e);
            }
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
            if (mIsSend == false)
            {
                NotifyShowAnnouncement();
                mIsSend = true;
            }
        }

        public void RefreshData(UIInitArguments data)
        {
            var _arg = data as SystemNoticeArguments;
            if (_arg == null)
            {
                return;
            }
            EnhanceContentInfo(_arg.NoticeInfo);

            if (State == FrameState.Open)
            {
                mIsSend = true;
            }
            else
            {
                mIsSend = false;
            }
        }

        public FrameState State { get; set; }
        #endregion

    }
}