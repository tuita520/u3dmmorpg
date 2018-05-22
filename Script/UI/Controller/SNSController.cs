#region using

using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class SNSController : IControllerBase
    {

        private SNSDataModel DataModel;
    
        public SNSController()
        {
            EventDispatcher.Instance.AddEventListener(ChatMainPrivateChar.EVENT_TYPE, OnprivChatEvent);
            EventDispatcher.Instance.AddEventListener(SNSTabEvent.EVENT_TYPE, OnClickTab);
            CleanUp();
        }
        public void CleanUp()
        {
            DataModel = new SNSDataModel();
        }
        public void RefreshData(UIInitArguments data)
        {
            //好友界面请求服务器好友数据的时间间隔，防止频繁打开造成服务器的压力
            int seconds = (System.DateTime.Now - PlayerDataManager.Instance.TeamInviteClickFubenTime).Seconds;
            if (seconds >= 5)
            {
                EventDispatcher.Instance.DispatchEvent(new FriendRefresh_Event());
            }
            PlayerDataManager.Instance.TeamInviteClickFubenTime = System.DateTime.Now;

            if (data == null)
                return;
            DataModel.tab = data.Tab;
            var _arg = data as FriendArguments;
            if(_arg != null)
            {
                if (_arg.Type == 1)
                {
                    //切换Tab
                    EventDispatcher.Instance.DispatchEvent(new FriendTabUpdateEvent(0));
                    //添加联系人
                    if (_arg.Data != null)
                    {
                        EventDispatcher.Instance.DispatchEvent(new AddRelationEvent(_arg.Data));
                    }
                }
                else if (_arg.Type == -1)
                {//初始化好友
                    EventDispatcher.Instance.DispatchEvent(new FriendTabUpdateEvent(-1));
                }
            }

            var mailCtrl = UIManager.Instance.GetController(UIConfig.MailUI);
            if(mailCtrl != null)
            {
                mailCtrl.RefreshData(new UIInitArguments());
            }
        }
        #region 基类
        public INotifyPropertyChanged GetDataModel(string name)
        {

            return DataModel;
        }

        public void Close()
        {

        }

        public void OnShow()
        {
            EventDispatcher.Instance.DispatchEvent(new MailUIRefreshEvent());
            var friendType = PlayerDataManager.Instance.NoticeData.FriendTabType;
            if (friendType == 0 || friendType == 1)
            {
                EventDispatcher.Instance.DispatchEvent(new FriendBtnEvent(friendType));
            }
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
        public FrameState State { get; set; }
        #endregion 基类 

        private void OnClickTab(IEvent ievent)
        {
            SNSTabEvent e = ievent as SNSTabEvent;
            if (e != null)
            {
                DataModel.tab = e.index;
            }
        }
        private void OnprivChatEvent(IEvent ievent)
        {//添加联系人
            var _e = ievent as ChatMainPrivateChar;
            var _arg = new FriendArguments();
            _arg.Type = 1;
            _arg.Data = _e.Data;
            _arg.Tab = 0;
            if (State == FrameState.Open)
            {
                RefreshData(_arg);
            }
            else
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SNSFrameUI, _arg));
            }
        }
    }
}
