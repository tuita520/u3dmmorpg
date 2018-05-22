/********************************************************************************* 

                         Scorpion



  *FileName:ServerListFrameCtrler

  *Version:1.0

  *Date:2017-07-12

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    public class ServerListFrameCtrler : IControllerBase
    {
        #region 构造函数
        public ServerListFrameCtrler()
        {
        
            CleanUp();
            EventDispatcher.Instance.AddEventListener(Event_ServerListCellIndex.EVENT_TYPE, OnServerListCellIndexEvent);
            EventDispatcher.Instance.AddEventListener(Event_ServerGroupListCellIndex.EVENT_TYPE, OnServerGroupListCellIndexEvent);
            EventDispatcher.Instance.AddEventListener(Event_ServerListButton.EVENT_TYPE, OnServerListButtonEvent);
            EventDispatcher.Instance.AddEventListener(Event_ServerPlayerCellClick.EVENT_TYPE, OnPlayerCellClick);
        }


        #endregion

        #region 成员变量
        private static int refreshMark = 1;
        private ServerListDataModel mServerListDataModel;
        private Coroutine refreshCoroutine = null;
        private Dictionary<int, ServerInfoData> serverCacheDictionary;
        private bool refreshPlayerList = true;
        private string selectRoleName = string.Empty;
        #endregion

        #region 逻辑函数
        private bool IsServerOpen()
        {
            var _serverState = (ServerStateType)mServerListDataModel.SelectedServer.State;
            switch (_serverState)
            {
                case ServerStateType.Prepare:
                case ServerStateType.Repair:
                    return false;
                case ServerStateType.Fine:
                case ServerStateType.Busy:
                case ServerStateType.Crowded:
                case ServerStateType.Full:
                    return true;
            }
            return false;
        }
        private IEnumerator PlayerChooseServerIdCoroutine()
        {
            if (!IsServerOpen())
            {
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 300832);
                yield break;
            }

            //using (new BlockingLayerHelper(1))
            var _block = new BlockingLayerHelper(1);
            {
                var _msg = NetManager.Instance.PlayerSelectServerId(mServerListDataModel.SelectedServer.ServerId);
                yield return _msg.SendAndWaitUntilDone();

                Logger.Debug(_msg.State.ToString());
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.ServerId = _msg.Request.ServerId;
                        PlayerDataManager.Instance.LastLoginServerId = _msg.Request.ServerId;
                        PlayerDataManager.Instance.CharacterLists = _msg.Response.Info;

                        if (PlayerDataManager.Instance.CharacterLists.Count <= 0 && GameSetting.Instance.ReviewState == 1)
                        {
                            var e = new UIEvent_CreateRole();
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else
                        {
                            PlayerDataManager.Instance.SelectedRoleIndex = _msg.Response.SelectId;
                            if (!string.IsNullOrEmpty(selectRoleName))
                            {
                                for (int i = 0; i < _msg.Response.Info.Count; i++)
                                {
                                    var player = _msg.Response.Info[i];
                                    if (player.Name.Equals(selectRoleName))
                                    {
                                        PlayerDataManager.Instance.SelectedRoleIndex = player.CharacterId;
                                    }
                                }
                            }

                            PlayerDataManager.Instance.ServerName = mServerListDataModel.SelectedServer.ServerName;

                            if (GameSetting.Instance.ReviewState == 1)
                            {
                                SoundManager.Instance.StopBGM(0.5f);
                                NetManager.Instance.CallEnterGame(PlayerDataManager.Instance.SelectedRoleIndex);
                            }
                            else
                            {
                                ResourceManager.PrepareScene(Resource.GetScenePath("SelectCharacter"), www =>
                                {
                                    ResourceManager.Instance.StartCoroutine(ResourceManager.LoadSceneImpl("SelectCharacter", www,
                                        () =>
                                        {
                                            /*
                                        UIManager.Instance.ShowUI(UIConfig.SelectRoleUI,
                                            new SelectRoleArguments
                                            {
                                                CharacterSimpleInfos = PlayerDataManager.Instance.CharacterLists,
                                                SelectId = msg.Response.SelectId,
                                                ServerName = mServerListDataModel.SelectedServer.ServerName
                                            });*/
                                            _block.Dispose();
                                        }));
                                });
                            }
                        }
                    }
                    else
                    {
                        GameUtils.ShowLoginTimeOutTip();
                        _block.Dispose();
                    }
                }
                else
                {
                    _block.Dispose();
                }
            }
        }

        private IEnumerator RefurbishServerListCoroutine(int seconds, int mark)
        {
            yield return new WaitForSeconds(seconds + 3);
            if (mark != refreshMark)
            {
                yield break;
            }
            if (State == FrameState.Open)
            {
                const int placeHolder = 0;
                var _serverListMsg = NetManager.Instance.GetServerList(placeHolder);
                yield return _serverListMsg.SendAndWaitUntilDone();

                if (_serverListMsg.State == MessageState.Reply)
                {
                    if (_serverListMsg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        RefreshData(new ServerListArguments
                        {
                            Data = _serverListMsg.Response
                        });
                    }
                }
            }
        }
        #endregion

        #region 事件
        private void OnServerGroupListCellIndexEvent(IEvent ievent)
        {
            mServerListDataModel.ShowPlayerList = false;
            var _ee = ievent as Event_ServerGroupListCellIndex;
            if (_ee.Index < 0 || _ee.Index >= mServerListDataModel.ServerList.Count)
            {
                return;
            }
            mServerListDataModel.SelectGroupData = mServerListDataModel.ServerList[_ee.Index];
            var _c = mServerListDataModel.ServerList.Count;
            for (var i = 0; i < _c; i++)
            {
                mServerListDataModel.ServerList[i].IsSelected = false;
            }
            mServerListDataModel.SelectGroupData.IsSelected = true;
        }

        private void OnPlayerCellClick(IEvent iEvent)
        {
            var ievent = iEvent as Event_ServerPlayerCellClick;
            if (null != ievent)
            {
                var dataModel = ievent.dataModel as PlayerInfoData;
                if (dataModel != null && dataModel.ServerInfo != null)
                {
                    mServerListDataModel.SelectedServer.IsSelected = false;
                    mServerListDataModel.SelectedServer = dataModel.ServerInfo;
                    mServerListDataModel.SelectedServer.IsSelected = true;
                    mServerListDataModel.ServerViewShow = false;

                    for (int i = 0; i < mServerListDataModel.PlayerList.Count; i++)
                    {
                        mServerListDataModel.PlayerList[i].IsSelectedRole = false;
                    }
                    dataModel.IsSelectedRole = true;
                    selectRoleName = dataModel.PlayerName;
                }
            }
        }


        private void OnServerListButtonEvent(IEvent ievent)
        {
            var _ee = ievent as Event_ServerListButton;
            switch (_ee.ButtonType)
            {
                case 0:
                {
                    NetManager.Instance.StartCoroutine(PlayerChooseServerIdCoroutine());
                }
                    break;
                case 1:
                {
                    mServerListDataModel.SelectedServer = mServerListDataModel.LastServer;
                }
                    break;
                //公告按钮
                case 2:
                {
                    mServerListDataModel.AnnouncementShow = false;
                    mServerListDataModel.AnnouncementShow = true;
                }
                    break;
                //服务器列表开关按钮
                case 3:
                {
                    ShowServerList();
                }
                    break;
                case 4:
                {
                    mServerListDataModel.ShowPlayerList = true;
                    var _c = mServerListDataModel.ServerList.Count;
                    for (var i = 0; i < _c; i++)
                    {
                        mServerListDataModel.ServerList[i].IsSelected = false;
                    }
                }
                    break;
            }
        }

        private void OnServerListCellIndexEvent(IEvent ievent)
        {
            var _ee = ievent as Event_ServerListCellIndex;
            if (_ee.Index < 0 || _ee.Index >= mServerListDataModel.SelectGroupData.ServerGroup.Count)
            {
                return;
            }

            mServerListDataModel.SelectedServer.IsSelected = false;
            mServerListDataModel.SelectedServer = mServerListDataModel.SelectGroupData.ServerGroup[_ee.Index];
            mServerListDataModel.SelectedServer.IsSelected = true;
            mServerListDataModel.ServerViewShow = false;
        }

        private void ShowServerList()
        {
            mServerListDataModel.ServerViewShow = !mServerListDataModel.ServerViewShow;

            if (mServerListDataModel.ServerViewShow)
            {
                mServerListDataModel.AnnouncementShow = false;
            }

            if (refreshPlayerList)
            {
                NetManager.Instance.StartCoroutine(GetAllPlayerInfo());
                refreshPlayerList = false;
            }
        }

        private IEnumerator GetAllPlayerInfo()
        {
            var msg = NetManager.Instance.GetAllCharactersLoginInfo(-1);
            yield return msg.SendAndWaitUntilDone();

            if (msg.State == MessageState.Reply)
            {
                mServerListDataModel.PlayerList.Clear();
                var filterList = FilterPlayer(msg.Response.CharacterInfos);
                foreach (var info in filterList)
                {
                    var playerInfo = new PlayerInfoData { PlayerName = info.Name, PlayerType = info.TypeId };

                    playerInfo.FaceId = GameUtils.GetRebornCircleIconId(info.TypeId, info.RebornTimes);
                    var tableId = info.RebornTimes <= 0 ? 1053 : 1038;
                    var dicDesc = GameUtils.GetDictionaryText(tableId);
                    if (!string.IsNullOrEmpty(dicDesc))
                    {
                        if (info.RebornTimes <= 0)
                        {
                            playerInfo.LevelAndReborn = string.Format(dicDesc, info.Level);
                        }
                        else
                        {
                            playerInfo.LevelAndReborn = string.Format(dicDesc, info.RebornTimes, info.Level);
                        }
                    }
                    ServerInfoData serverInfo;
                    if (serverCacheDictionary.TryGetValue(info.ServerId, out serverInfo))
                    {
                        playerInfo.ServerInfo = serverInfo;
                        mServerListDataModel.PlayerList.Add(playerInfo);
                    }
                }
                if (mServerListDataModel.PlayerList.Count >= 1)
                {
                    mServerListDataModel.PlayerList[0].IsSelectedRole = true;
                }
            }
        }

        private IEnumerable<CharacterLoginInfo> FilterPlayer(List<CharacterLoginInfo> list)
        {
//         var dictionary = new Dictionary<int, List<CharacterLoginInfo>>();
//         foreach (var info in list)
//         {
//             if (dictionary.ContainsKey(info.ServerId))
//             {
//                 dictionary[info.ServerId].Add(info);
//             }
//             else
//             {
//                 var l = new List<CharacterLoginInfo> {info};
//                 dictionary.Add(info.ServerId, l);
//             }
//         }
//         var result = dictionary.Select(oneServer => oneServer.Value.OrderByDescending(level => level.Level))
//             .Select(enumerator => enumerator.First());
//         return result.OrderByDescending(m => m.ServerId);

            return list.OrderByDescending(player => player.LastTime);

        }




        #endregion


        #region 固有函数
        public void CleanUp()
        {
            mServerListDataModel = new ServerListDataModel();
            serverCacheDictionary = new Dictionary<int, ServerInfoData>();
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
            LoginLogic.instance.InvisibleLoginFrame();
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as ServerListArguments;
            if (_args == null || _args.Data == null)
            {
                return;
            }

            var _netdata = _args.Data;
            var _plData = _netdata.PlayerData;

            //下次刷新时间
            var _waitSec = _netdata.WaitSec;
            if (_waitSec > 0)
            {
                refreshMark++;
                NetManager.Instance.StartCoroutine(RefurbishServerListCoroutine(_waitSec, refreshMark));
            }

            //所有服务器数据
            mServerListDataModel.ServerList.Clear();
            serverCacheDictionary.Clear();
            var _dataModel = mServerListDataModel;
            var _responseData = _netdata.Data;
            PlayerDataManager.Instance.ServerNames.Clear();

            var _index = 1;
            var _minIndex = 0;
            ServerGroupData groupdata = null;
            Uint64Array characterList;
            {
                var _list1 = _responseData;
                var _listCount1 = _list1.Count;
                for (var __i1 = 0; __i1 < _listCount1; ++__i1)
                {
                    var _state = _list1[__i1];
                    {
                        if (_index % 10 == 1)
                        {
                            groupdata = new ServerGroupData();
                            _dataModel.ServerList.Add(groupdata);
                            _minIndex = _index;
                        }
                        var _serverInfo = new ServerInfoData();
                        _serverInfo.ServerName = _state.Name;
                        _serverInfo.ServerId = _state.ServerId;
                        PlayerDataManager.Instance.ServerNames.Add(_serverInfo.ServerId, _serverInfo.ServerName);
                        _serverInfo.State = _state.State;
                        _serverInfo.isNew = (_state.IsNew != 0);
                        _serverInfo.MieShiIconStata = _state.actiResult;
                        if (_plData.TryGetValue(_state.ServerId, out characterList))
                        {
                            _serverInfo.CharacterCount = characterList.Items.Count;
                            if (_serverInfo.CharacterCount != 0)
                            {
                                groupdata.RedPromptShow = 1;
                            }
                        }
                        if (groupdata != null)
                        {
                            groupdata.ServerGroup.Add(_serverInfo);
                            //{0}-{1}区
                            groupdata.GroupName = string.Format(GameUtils.GetDictionaryText(270117), _minIndex, _index);
                            serverCacheDictionary.Add(_state.ServerId, _serverInfo);
                        }

                        if (_serverInfo.ServerId == PlayerDataManager.Instance.LastLoginServerId)
                        {
                            _dataModel.SelectedServer = _serverInfo;
                            _dataModel.SelectedServer.IsSelected = true;
                            _dataModel.LastServer = _serverInfo;
                            _dataModel.SelectGroupData = groupdata;
                            _dataModel.SelectGroupData.IsSelected = true;
                        }

                        _index++;
                    }
                }
            }
#if !UNITY_EDITOR
#if UNITY_ANDROID || UNITY_IOS
            if (1 == Table.GetClientConfig(1154).ToInt())
            {//初始化主播系统
                string _id = "";
                string _key = "";
                if (_args.Data.Config.TryGetValue(1213, out _id) && _args.Data.Config.TryGetValue(1214, out _key))
                {
       	            GVoiceManager.Instance.Init(_id,_key);
                }
                else
                {//没能取到服务器端信息,走本地

    	            GVoiceManager.Instance.Init();
                }
            }
#endif
#endif

        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return mServerListDataModel;
        }

        public FrameState State { get; set; }
        #endregion
    
    }
}