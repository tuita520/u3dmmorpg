/********************************************************************************* 

                         Scorpion




  *FileName:OperationlistController

  *Version:1.0

  *Date:2017-06-28

  *Description:

**********************************************************************************/
#region using

using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class OperaListFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量

        private OperationListData Data;
        private OperationListDataModel DataModel;
        #endregion

        #region 构造函数
        public OperaListFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_OperationList_Button.EVENT_TYPE, OnBtnsEvent); //下拉菜单点击事件
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new OperationListDataModel();
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

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as OperationlistArguments;
            if (_args == null)
            {
                return;
            }

            Data = _args.Data;
            if (Data == null)
            {
                return;
            }
            var _noticeData = PlayerDataManager.Instance.NoticeData;
            var _listRecord = Table.GetOperationList(Data.TableId);
            if (_listRecord == null)
                return;
            DataModel.Speek = _listRecord.Speek;
            DataModel.Challenge = _listRecord.Challenge;
            DataModel.Attribute = _listRecord.Attribute;

            //这个之前是有好友开启条件的，但是现在开始游戏就有好友界面，所以把判断取消
            //if (_noticeData.FriendOpenFlag)
            {
                var _fiend = UIManager.Instance.GetController(UIConfig.FriendUI);

                if (_listRecord.AddFriend == 1)
                {
                    var _ret = (bool)_fiend.CallFromOtherClass("IsInFriendListId", new object[] { Data.CharacterId });
                    DataModel.AddFriend = _ret == false ? 1 : 0;
                }
                else
                {
                    DataModel.AddFriend = 0;
                }

                if (_listRecord.AddEnemy == 1 && Data.ChannelType != 10)
                {
                    var _ret = (bool)_fiend.CallFromOtherClass("IsInEnemyListId", new object[] { Data.CharacterId });
                    DataModel.AddEnemy = _ret == false ? 1 : 0;
                }
                else
                {
                    DataModel.AddEnemy = 0;
                }

                if (_listRecord.AddShield == 1)
                {
                    var _ret = (bool)_fiend.CallFromOtherClass("IsInBalckListId", new object[] { Data.CharacterId });
                    DataModel.AddShield = _ret == false ? 1 : 0;
                }
                else
                {
                    DataModel.AddShield = 0;
                }

                if (_listRecord.DelFriend == 1)
                {
                    var _ret = (bool)_fiend.CallFromOtherClass("IsInFriendListId", new object[] { Data.CharacterId });
                    DataModel.DelFriend = _ret ? 1 : 0;
                }
                else
                {
                    DataModel.DelFriend = 0;
                }

                if (_listRecord.DelEnemy == 1)
                {
                    var _ret = (bool)_fiend.CallFromOtherClass("IsInEnemyListId", new object[] { Data.CharacterId });
                    DataModel.DelEnemy = _ret ? 1 : 0;
                }
                else
                {
                    DataModel.DelEnemy = 0;
                }

                if (_listRecord.DelShield == 1)
                {
                    var _ret = (bool)_fiend.CallFromOtherClass("IsInBalckListId", new object[] { Data.CharacterId });
                    DataModel.DelShield = _ret ? 1 : 0;
                }
                else
                {
                    DataModel.DelShield = 0;
                }
            }
            //else
            //{
            //    DataModel.AddFriend = 0;
            //    DataModel.AddEnemy = 0;
            //    DataModel.AddShield = 0;
            //    DataModel.DelFriend = 0;
            //    DataModel.DelEnemy = 0;
            //    DataModel.DelShield = 0;
            //}


            if (_noticeData.TeamOpenFlag)
            {
                var _team = UIManager.Instance.GetController(UIConfig.TeamFrame);

                if (_listRecord.InviteTeam == 1)
                {
                    var _ret = (bool)_team.CallFromOtherClass("IsInTeam", new object[] { Data.CharacterId });
                    DataModel.InviteTeam = _ret == false ? 1 : 0;
                }
                else
                {
                    DataModel.InviteTeam = 0;
                }

                var _hasTeam = (bool)_team.CallFromOtherClass("HasTeam", null);
                if (_listRecord.ApplyTeam == 1 && _hasTeam == false)
                {
                    DataModel.ApplyTeam = 1;
                }
                else
                {
                    DataModel.ApplyTeam = 0;
                }

                if (_listRecord.UpLeader == 1 && _hasTeam)
                {
                    DataModel.UpLeader = 1;
                }
                else
                {
                    DataModel.UpLeader = 0;
                }

                if (_listRecord.KickTeam == 1 && _hasTeam)
                {
                    DataModel.KickTeam = 1;
                }
                else
                {
                    DataModel.KickTeam = 0;
                }

                if (_listRecord.LeaveTeam == 1 && _hasTeam)
                {
                    DataModel.LeaveTeam = 1;
                }
                else
                {
                    DataModel.LeaveTeam = 0;
                }
            }
            else
            {
                DataModel.InviteTeam = 0;
                DataModel.ApplyTeam = 0;
                DataModel.UpLeader = 0;
                DataModel.KickTeam = 0;
                DataModel.LeaveTeam = 0;
            }

            if (_noticeData.UnionOpenFlag)
            {
                var _union = UIManager.Instance.GetController(UIConfig.BattleUnionUI);
                var _hasUnion = (bool)_union.CallFromOtherClass("HasUnion", null);

                if (_listRecord.JoinUnion == 1 && _hasUnion && Data.ChannelType != 10)
                {
                    DataModel.JoinUnion = 1;
                }
                else
                {
                    DataModel.JoinUnion = 0;
                }

                DataModel.UpToChief = _listRecord.UpChief;
                DataModel.UpAccess = _listRecord.UpAccess;
                DataModel.DownAccess = _listRecord.DownAccess;
                DataModel.KickUnion = _listRecord.QuitUnion;
            }
            else
            {
                DataModel.JoinUnion = 0;
                DataModel.UpToChief = 0;
                DataModel.UpAccess = 0;
                DataModel.DownAccess = 0;
                DataModel.KickUnion = 0;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件

        //弹出的下拉菜单按钮相应事件
        private void OnBtnsEvent(IEvent ievent)
        {
            var _e2 = new Close_UI_Event(UIConfig.OperationList);
            EventDispatcher.Instance.DispatchEvent(_e2);
            var _ee = ievent as UIEvent_OperationList_Button;

            if (GameUtils.CharacterIdIsRobot(Data.CharacterId) && _ee.Index != 1)
            {
                //玩家不在线
                GameUtils.ShowHintTip(200000003);
                return;
            }

            switch (_ee.Index)
            {
                //发起聊天
                case 0:
                {
                    var _e = new ChatMainPrivateChar(Data);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //查看属性
                case 1:
                {
                    PlayerDataManager.Instance.ShowPlayerInfo(Data.CharacterId);
                }
                    break;
                //加为好友
                case 2:
                {
                    var _e = new FriendOperationEvent(1, 1, Data.CharacterName, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //加为仇人
                case 3:
                {
                    var _e = new FriendOperationEvent(2, 1, Data.CharacterName, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //屏蔽
                case 4:
                {
                    var _e = new FriendOperationEvent(3, 1, Data.CharacterName, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //删除好友
                case 5:
                {
                    var _e = new FriendOperationEvent(1, 0, Data.CharacterName, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //解除仇人
                case 6:
                {
                    var _e = new FriendOperationEvent(2, 0, Data.CharacterName, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //取消屏蔽
                case 7:
                {
                    var _e = new FriendOperationEvent(3, 0, Data.CharacterName, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //邀请组队
                case 8:
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_TeamInvitePlayer(Data.CharacterId));
                }
                    break;
                //申请进队
                case 9:
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_TeamApplyOtherTeam(Data.CharacterId));
                }
                    break;
                //提升队长
                case 10:
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_TeamSwapLeader(Data.CharacterId));
                }
                    break;
                //请出队伍
                case 11:
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_TeamKickPlayer(Data.CharacterId));
                }
                    break;
                //离开队伍
                case 12:
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_TeamLeaveTeam());
                }
                    break;
                //13 邀请入盟，14提升领袖，15提升权限，16降低权限，17请出战盟
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                {
                    var _e = new UIEvent_UnionCommunication(_ee.Index, Data.CharacterId);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                    break;
                //决斗邀请
                case 18:
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_InviteChallenge(Data.CharacterId));
                }
                    break;
            }
        }
        #endregion



 
    }
}