/********************************************************************************* 

                         Scorpion



  *FileName:SelectRoleFrameCtrler

  *Version:1.0

  *Date:2017-07-27
  *Description:

**********************************************************************************/


#region using

using System;
using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class SelectRoleFrameCtrler : IControllerBase
    {
        #region 构造函数
        public SelectRoleFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(UIEvent_SelectRole_Enter.EVENT_TYPE, OnButton_EnterGameEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SelectRole_Back.EVENT_TYPE, OnButton_BackEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SelectRole_Index.EVENT_TYPE, OnButton_SelectEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_ShowCreateRole.EVENT_TYPE, OnButton_ShowCreateRoleEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_GetRandomName.EVENT_TYPE, OnButton_GetRandomNameEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_CreateRoleType_Change.EVENT_TYPE, OnCreateRoleTypeChangeEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_CreateRole.EVENT_TYPE, OnButton_CreateRoleEvent);
            EventDispatcher.Instance.AddEventListener(NameChange.EVENT_TYPE, OnInPutNameEvent);
        }
        #endregion

        #region 成员变量
        private LoginDataModel DataModel;
        private bool InPutname;
        private bool mBackToLogin;
        private CharacterLoginDataModel NowSelectRole;

        private int RandomNameCount = 0;
        #endregion

        #region 事件
        //取消按钮
        private void OnButton_BackEvent(IEvent ievent)
        {
            Game.Instance.ExitToServerList();
        }
        private void OnButton_CreateRoleEvent(IEvent ievent)
        {
            if (GameSetting.Instance.ReviewState == 1)
            {
                DataModel.CreateType = UnityEngine.Random.Range(0, 3);
                var actorTable = Table.GetActor(DataModel.CreateType);
                DataModel.CreateName = StringConvert.GetRandomName(actorTable.Sex);

                DataModel.CreateName = DataModel.CreateName.Trim();
            }
            else
            {
                DataModel.CreateName = DataModel.CreateName.Trim();

                if (!GameUtils.CheckName(DataModel.CreateName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 300900);
                    return;
                }
                if (GameUtils.CheckSensitiveName(DataModel.CreateName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 200004120);
                    return;
                }
                if (GameUtils.ContainEmoji(DataModel.CreateName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 725);
                    return;
                }

                if (!GameUtils.CheckLanguageName(DataModel.CreateName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 725);
                    return;
                }
            }

            NetManager.Instance.StartCoroutine(CreateRoleCoroutine());
        }
        //确认按钮
        private void OnButton_EnterGameEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_SelectRole_Enter;
        }

        //随机一个姓名
        private void OnButton_GetRandomNameEvent(IEvent ievent)
        {
            var _actorTable = Table.GetActor(DataModel.CreateType);
            DataModel.CreateName = StringConvert.GetRandomName(_actorTable.Sex);
        }

        //选角色按钮
        private void OnButton_SelectEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_SelectRole_Index;
            var _nIndex = _ee.index;
            if (_nIndex < 0 || _nIndex >= DataModel.Characters.Count)
            {
                return;
            }
            if (DataModel.Characters[_nIndex].CharacterId == ulong.MaxValue)
            {
                return;
            }

            PlayerDataManager.Instance.mInitBaseAttr.CharacterId = DataModel.Characters[_nIndex].CharacterId;
            PlayerDataManager.Instance.mInitBaseAttr.RoleId = DataModel.Characters[_nIndex].RoleId;

            // NowSelectRole = DataModel.Characters[nIndex];
            NowSelectRole.Clone(DataModel.Characters[_nIndex]);
        }

        //创建角色按钮
        private void OnButton_ShowCreateRoleEvent(IEvent ievent)
        {
            if (string.IsNullOrEmpty(DataModel.CreateName))
            {
                OnButton_GetRandomNameEvent(null);
            }
            var _ee = ievent as UIEvent_ShowCreateRole;

            if (mBackToLogin)
            {
                if (_ee.index == 0)
                {
                    Game.Instance.ExitToServerList();
                }
            }
            else
            {
                DataModel.showCreateFrame = _ee.index;
            }
        }
        //选择创建人物的职业
        private void OnCreateRoleTypeChangeEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_CreateRoleType_Change;
            if (_ee != null)
            {
                DataModel.CreateType = _ee.index;
            }
            if (!InPutname)
            {
                OnButton_GetRandomNameEvent(null);
            }
            var _table = Table.GetActor(_ee.index);
            if (null != _table)
            {
                var _index = UnityEngine.Random.Range(0, 3);
                SoundManager.Instance.StopAllSoundEffect();
                SoundManager.Instance.PlaySoundEffect(_table.Dubbing[_index]);
            }
        }

        private void OnInPutNameEvent(IEvent ievent)
        {
            var _e = ievent as NameChange;
            InPutname = _e.Idx;
        }
        #endregion

        #region 逻辑函数
        private IEnumerator CreateRoleCoroutine()
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.CreateCharacter(PlayerDataManager.Instance.ServerId, DataModel.CreateType,
                    DataModel.CreateName);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _list = PlayerDataManager.Instance.CharacterLists;
                        _list = _msg.Response.Info;
                        // RefreshDate(msg.Response.Info, msg.Response.SelectId);
                        //新需求,创建完人物直接进入游戏
                        GameLogic.PlayFirstEnterGameCG = 1;
                        var _role = _list[_list.Count - 1];
                        var _SelectedRoleId = _role.CharacterId;

                        var _serverName = PlayerDataManager.Instance.ServerName;
                        var _serverId = PlayerDataManager.Instance.ServerId;
                        PlayerDataManager.Instance.CharacterFoundTime = Extension.FromServerBinary(_msg.Response.CharacterFoundTime);
                        var _ts = PlayerDataManager.Instance.CharacterFoundTime - DateTime.Parse("1970-1-1");
                        var _time = (int)_ts.TotalSeconds;
                        PlatformHelper.CollectionCreateRoleDataForKuaifa(_SelectedRoleId.ToString(), DataModel.CreateName,
                            _serverId.ToString(), _serverName, _time.ToString());

                        {
                            //这里实现给主角名字赋值，为了后面对话时用
                            PlayerDataManager.Instance.PlayerDataModel.CharacterBase.Name = _role.Name;
                        }
#if UNITY_EDITOR
                        var _skip = true;
#else
					bool _skip = true;
					//bool skip = list.Count>1;
#endif
                        //播放创建角色时的CG
                        /*
                    if (0 == DataModel.CreateType)
                    {
                        PlayCG.Instance.PlayCGFile("Video/jianshi.txt",
                            () => { NetManager.Instance.CallEnterGame(SelectedRoleId); }, skip, false);
                    }
                    else if (1 == DataModel.CreateType)
                    {
                        PlayCG.Instance.PlayCGFile("Video/fashi.txt",
                            () => { NetManager.Instance.CallEnterGame(SelectedRoleId); }, skip, false);
                    }
                    else if (2 == DataModel.CreateType)
                    {
                        PlayCG.Instance.PlayCGFile("Video/gongshou.txt",
                            () => { NetManager.Instance.CallEnterGame(SelectedRoleId); }, skip, false);
                    }
                    else
                    {
                        NetManager.Instance.CallEnterGame(SelectedRoleId);
                    }*/
                        NetManager.Instance.CallEnterGame(_SelectedRoleId);
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.Error_NAME_IN_USE)
                        {
                            if (GameSetting.Instance.ReviewState == 1)
                            {
                                // 防止无限递归
                                if (RandomNameCount > 100)
                                {
                                    yield break;
                                }

                                RandomNameCount++;
                                DataModel.CreateType = UnityEngine.Random.Range(0, 3);
                                var actorTable = Table.GetActor(DataModel.CreateType);
                                DataModel.CreateName = StringConvert.GetRandomName(actorTable.Sex);
                                DataModel.CreateName = DataModel.CreateName.Trim();

                                NetManager.Instance.StartCoroutine(CreateRoleCoroutine());
                            }
                            else
                            {
                                var _dicId = _msg.ErrorCode + 200000000;
                                var _tbDic = Table.GetDictionary(_dicId);
                                var _info = "";
                                _info = _tbDic.Desc[GameUtils.LanguageIndex];
                                UIManager.Instance.ShowMessage(MessageBoxType.Ok, _info, ""); 
                            }
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    Logger.Debug("CreateCharacter.................." + _msg.State);
                }
            }
        }
        private void RefurbishRoleDataModel(int index, CharacterSimpleInfo info)
        {
            var _dataModel = DataModel.Characters[index];
            if (info != null)
            {
                _dataModel.CharacterId = info.CharacterId;
                _dataModel.Level = info.Level;
                _dataModel.Name = info.Name;
                _dataModel.RoleId = info.RoleId;
                // dataModel.Type = info.Type;
                _dataModel.Type = info.RoleId;
                _dataModel.Reborn = info.Ladder;
                _dataModel.FaceId = GameUtils.GetRebornCircleIconId(info.RoleId, info.Ladder);

                var tbDict = Table.GetDictionary(info.Ladder <= 0 ? 1053 : 1038);
                if (tbDict != null && tbDict.Desc != null && tbDict.Desc.Length > GameUtils.LanguageIndex)
                {
                    var dicDesc = tbDict.Desc[GameUtils.LanguageIndex];
                    if (dicDesc != null)
                    {
                        if (info.Ladder <= 0)
                        {
                            _dataModel.LevelDesc = String.Format(dicDesc, info.Level);
                        }
                        else
                        {
                            _dataModel.LevelDesc = String.Format(dicDesc, info.Ladder, info.Level);
                        }
                    }
                }
            }
            else
            {
                //预备给删除功能
                var _newCharacterData = new CharacterLoginDataModel();
                _dataModel.CharacterId = _newCharacterData.CharacterId;
                _dataModel.Level = _newCharacterData.Level;
                _dataModel.Name = _newCharacterData.Name;
                _dataModel.RoleId = _newCharacterData.RoleId;
                _dataModel.Type = _newCharacterData.Type;
                _dataModel.Reborn = _newCharacterData.Reborn;
                var tbDict = Table.GetDictionary(1053);
                if (tbDict != null && tbDict.Desc != null && tbDict.Desc.Length > GameUtils.LanguageIndex)
                {
                    var dicDesc = tbDict.Desc[GameUtils.LanguageIndex];
                    if (dicDesc != null)
                    {
                        _dataModel.LevelDesc = String.Format(dicDesc, _newCharacterData.Reborn, _newCharacterData.Level);
                    } 
                }
            }
            _dataModel.showCreateButton = _dataModel.CharacterId == 0 ? 0 : 1;
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            if (DataModel == null)
            {
                DataModel = new LoginDataModel();
                var _DataModelCharactersCount0 = DataModel.Characters.Count;
                for (var i = 0; i < _DataModelCharactersCount0; i++)
                {
                    DataModel.Characters[i] = new CharacterLoginDataModel();
                }
                NowSelectRole = new CharacterLoginDataModel();
            }
        }
        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name.Equals("GetCreateShow"))
            {
                return DataModel.showCreateFrame;
            }
            return null;
        }

        public void OnShow()
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name.Equals("selectRole"))
            {
                return NowSelectRole;
            }
            return DataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }
        public void RefreshData(UIInitArguments data)
        {
            var _args = data as SelectRoleArguments;
            if (null == _args)
            {
                return;
            }

            DataModel.ServerName = _args.ServerName;

            var _selectId = _args.SelectId;
            var _characterSimpleInfos = _args.CharacterSimpleInfos;

            var _selectCount = 0;
            var _DataModelCharactersCount1 = DataModel.Characters.Count;
            for (var i = 0; i < _DataModelCharactersCount1; i++)
            {
                var _info = _characterSimpleInfos.Count > i ? _characterSimpleInfos[i] : null;
                RefurbishRoleDataModel(i, _info);
                if (_info != null && _selectId == _info.CharacterId)
                {
                    _selectCount = i;
                }
            }

            DataModel.CharacterCount = _characterSimpleInfos.Count;
            //创建人物后选中刚创建的新角色
            DataModel.SelectIndex = _selectCount;
            DataModel.showCreateFrame = 0;
            DataModel.CreateName = "";
            if (_args.Type == SelectRoleArguments.OptType.SelectMyRole)
            {
                var _e = new UIEvent_SelectRole_Index(_selectCount);
                OnButton_SelectEvent(_e);
            }


            //空号上来先创建人物
            if (_characterSimpleInfos.Count == 0)
            {
                DataModel.showCreateFrame = 1;
                mBackToLogin = true;
            }
            else
            {
                DataModel.showCreateFrame = 0;
            }
        }

        public FrameState State { get; set; }
        #endregion
 
    }
}