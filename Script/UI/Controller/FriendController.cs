/********************************************************************************* 

                         Scorpion




  *FileName:FriendController

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
using System.IO;
using System.Text;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using LZ4s;
using ProtoBuf;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#if !UNITY_EDITOR
using UnityEngine;
#endif

#endregion

namespace ScriptController
{
    public class PartnerFrameCtrler : IControllerBase
    {
        #region 静态变量
        private static readonly ContactInfoDataModel s_emptyContactInfo = new ContactInfoDataModel();

        private enum eTab
        {
            Temp = 0,
            Friend = 1,
            Enemy = 2,
            Black = 3,
            Find = 4
        }
        #endregion

        #region 成员变量
        private FriendDataModel m_DataModel;
        private string m_inputStr = string.Empty;
        private string m_inputStr2 = string.Empty;
        private int m_asyncState = -1;
        private readonly List<DateTime> m_cdTimeList = new List<DateTime>(4);
        private string m_characterChatDirectory = "";
        private Dictionary<string, string> m_dicItemLink = new Dictionary<string, string>();
        private int m_loadSeekPostion;
        private Dictionary<ulong, List<ChatMessageData>> m_saveListCaches;
        private object m_saveTImerTrigger;
        private Dictionary<ulong, List<ChatMessageData>> m_unWriteListCaches;
        private readonly int m_pageChatCount = 10;
        private ulong lastGuid { get; set; }
        #endregion

        #region 构造函数
        public PartnerFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ChatVoiceContent.EVENT_TYPE, OncontVoiceChatEvent);
            EventDispatcher.Instance.AddEventListener(FriendSeekBtnClick.EVENT_TYPE, OnFindClickCharEvent);
            EventDispatcher.Instance.AddEventListener(FriendClickShowInfo.EVENT_TYPE, OnClickInfoEvent);
            EventDispatcher.Instance.AddEventListener(FriendOperationEvent.EVENT_TYPE, OnPartnerOperaEvent);
            EventDispatcher.Instance.AddEventListener(FriendClickTabEvent.EVENT_TYPE, OnPartnerClicTabEvent);
            EventDispatcher.Instance.AddEventListener(FriendAddSyncEvent.EVENT_TYPE, OnPartnerAyncAddEvent);
            EventDispatcher.Instance.AddEventListener(FriendDelSyncEvent.EVENT_TYPE, OnPartnerAyncDeleteEvent);
            EventDispatcher.Instance.AddEventListener(FriendUpdateSyncEvent.EVENT_TYPE, OnPartnerAyncRefreshEvent);
            //EventDispatcher.Instance.AddEventListener(ChatMainPrivateChar.EVENT_TYPE, OnprivChatEvent);
            EventDispatcher.Instance.AddEventListener(FriendClickType.EVENT_TYPE, OnPartnerClicType);
            EventDispatcher.Instance.AddEventListener(WhisperChatMessage.EVENT_TYPE, OnLittleChatMessageEvent);
            EventDispatcher.Instance.AddEventListener(FriendContactCell.EVENT_TYPE, OnFriendContactCell);
            EventDispatcher.Instance.AddEventListener(FriendContactClickAddFriend.EVENT_TYPE, OnPartnerContactAddFriendEvent);
            EventDispatcher.Instance.AddEventListener(AddFaceNode.EVENT_TYPE, OnAddNodeEvent);
            EventDispatcher.Instance.AddEventListener(ChatShareItemEvent.EVENT_TYPE, OnshowTargetChatEvent);
            EventDispatcher.Instance.AddEventListener(ChatMainSendVoiceData.EVENT_TYPE, OnTransmitVoiceChatEvent);

            EventDispatcher.Instance.AddEventListener(FriendReceive.EVENT_TYPE, OnPartnerRecEvent);

            EventDispatcher.Instance.AddEventListener(FriendBtnEvent.EVENT_TYPE, OnClickFriendBtn);
            EventDispatcher.Instance.AddEventListener(AddRelationEvent.EVENT_TYPE, OnAddRelation);
            EventDispatcher.Instance.AddEventListener(FriendRefresh_Event.EVENT_TYPE, NewApplyPlayerheadMsgEvent);
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            s_emptyContactInfo.CharacterId = 0;
            s_emptyContactInfo.Name = "";
            s_emptyContactInfo.IsSelect = false;
            m_DataModel = new FriendDataModel();
            m_cdTimeList.Clear();

            for (var _i = 0; _i < 2; _i++)
            {
                //seek fast
                m_cdTimeList.Add(Game.Instance.ServerTime);
            }
            m_unWriteListCaches = new Dictionary<ulong, List<ChatMessageData>>();
            m_inputStr = GameUtils.GetDictionaryText(100001058);
            m_DataModel.InputChat = m_inputStr;
            m_inputStr2 = GameUtils.GetDictionaryText(240612);
            m_DataModel.InputSeek = m_inputStr2;
            UpdateListByType();
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "ApplyAllInfo")
            {
                ApplyAllMsg();
            }
            else if (name == "IsInFriendListId")
            {
                var _id = (ulong)param[0];
                return IsBelongPartnerListId(_id);
            }
            else if (name == "IsInEnemyListId")
            {
                var _id = (ulong)param[0];
                return IsBelongEnemyListId(_id);
            }
            else if (name == "IsInBalckListId")
            {
                var _id = (ulong)param[0];
                return IsBelongBalckListId(_id);
            }
            else if (name == "IsInBalckListName")
            {
                var _str = (string)param[0];
                return IsBelongBalckListName(_str);
            }
            else if (name == "GetFiendInfo")
            {
                var _type = (int)param[0];
                var _guid = (ulong)param[1];
                return GetPartnerMsg(_type, _guid);
            }
            else if (name == "GetContactInfoNumber")
            {
                return GetContactInfoNumber();
            }
            else if (name == "GetFriendsInfoNumber")
            {
                return GetFriendsInfoNumber();
            }
            else if (name == "GetFriendsInfo")
            {
                return GetFriendsInfo();
            }
            else if(name == "GetContactInfo")
            {
                return GetContactInfo();
            }
            return null;
        }

        public void OnShow()
        {
        }

        public void Close()
        {
            var _e = new Close_UI_Event(UIConfig.ChatItemList);
            EventDispatcher.Instance.DispatchEvent(_e);

            var _e1 = new Close_UI_Event(UIConfig.FaceList);
            EventDispatcher.Instance.DispatchEvent(_e1);

            PlayerDataManager.Instance.CloseCharacterPopMenu();

            WriteChacheRecords();
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            for (var i = 0; i < m_DataModel.IsSelectTab.Count; i++)
            {
                m_DataModel.IsSelectTab[i] = false;
            }
            m_DataModel.EmptyTips[3] = true;
            m_DataModel.SeekInfos.Clear();
            var _arg = data as FriendArguments;

            if (_arg == null || _arg.Type == 0)
            {
                m_DataModel.IsSelectTab[0] = true;
                m_DataModel.SelectToggle = 0;
            }
            else if (_arg.Type == 1)
            {//添加联系人
                var _d = _arg.Data;
                SetRelationChatMsg(_d.CharacterId, _d.CharacterName, _d.Ladder, _d.Level, _d.RoleId);
                UpdateListByType((int)eTab.Temp);
            }
            else if (_arg.Type == 2)
            {
                RefreshRelationOrder();
                RefreshRelationCellNextIndex();
                m_DataModel.SelectToggle = 0;
                m_DataModel.IsSelectTab[0] = true;
            }
            else
            {
                m_DataModel.SelectToggle = 0;
                m_DataModel.IsSelectTab[0] = true;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private void OnTransmitVoiceChatEvent(IEvent ievent)
        {
            var _selfGuid = PlayerDataManager.Instance.GetGuid();
            var _e = ievent as ChatMainSendVoiceData;

            if (_e.IsWhisper == false)
            {
                return;
            }
            if (_e.VoiceData.Length < 1)
            {
                Logger.Debug("voiceData.Length < 1");
                return;
            }

            if (_e.VoiceLength < 0.5f)
            {
                //时间太短
                Logger.Debug("record time < 0.5s");
                return;
            }

            var _speakTime = (int)Math.Ceiling(_e.VoiceLength);


            var _charData = m_DataModel.SelectContact;
            var _chatContent = "/" + _charData.Name + " " + _speakTime;
            var _content = new ChatMessageContent
            {
                Content = _chatContent,
                SoundData = _e.VoiceData
            };

            NetManager.Instance.StartCoroutine(SendChatMessageEnumerator((int)eChatChannel.Whisper, _content, _selfGuid,
                _charData.Name));
        }

        private ContactInfoDataModel GetLastChatRelation()
        {
            foreach (var v in m_DataModel.ContactInfos)
            {
                if (v.UnreadCount > 0)
                    return v;
            }
            if (m_DataModel.ContactInfos.Count > 0)
            {
                return m_DataModel.ContactInfos[0];
            }
            return new ContactInfoDataModel();
        }
        private void OnLittleChatMessageEvent(IEvent ievent)
        {
            var _e = ievent as WhisperChatMessage;
            var _message = new FriendMessageDataModel();
            _message.MessageData = _e.DataModel;
            var _msgInfo = _e.DataModel;
            var _type = 0;
            var _level = 0;
            var _ladder = 0;
            if (m_DataModel.SelectContact.CharacterId == _msgInfo.CharId)
            {
                var _info = _message.MessageData as ChatMessageData;
                if (_info == null)
                {
                    return;
                }
                _type = m_DataModel.SelectContact.Type;
                _level = m_DataModel.SelectContact.Level;
                _ladder = m_DataModel.SelectContact.Ladder;
                if (m_DataModel.ChatMessages.Count > 0)
                {
                    var _last = m_DataModel.ChatMessages[m_DataModel.ChatMessages.Count - 1].MessageData as ChatMessageData;
                    if (_last == null)
                    {
                        return;
                    }
                    var _dif = DateTime.FromBinary(_info.Times) - DateTime.FromBinary(_last.Times);
                    if (_dif.TotalMinutes > 5)
                    {
                        _info.ShowTime = 1;
                    }
                }
                else
                {
                    _info.ShowTime = 1;
                }
                if (_info.Type == (int)eChatChannel.MyWhisper)
                {
                    _info.RoleId = PlayerDataManager.Instance.GetRoleId();
                }
                else if (_info.Type == (int)eChatChannel.Whisper)
                {
                    _info.RoleId = m_DataModel.SelectContact.Type;
                }
                m_DataModel.ChatMessages.Add(_message);
                if (m_DataModel.SelectContact.CharacterId != _msgInfo.CharId)
                {
                    m_DataModel.SelectContact.CharacterId = _msgInfo.CharId;
                }

                m_DataModel.SelectContact.NextIndex = 0;
                AddRelationCellNextIndex(m_DataModel.SelectContact.NextIndex);

                if (State == FrameState.Close)
                {
                    m_DataModel.SelectContact.UnreadCount++;

                    RefreshUnreadNum(_msgInfo.CharId);

                    RefreshNoticeUnreadNum();
                }
                //             if (DataModel.SelectContact.Index != 0)
                //             {
                //                 DataModel.ContactInfos.Remove(DataModel.SelectContact);
                //                 InsetContactInfo(DataModel.SelectContact);
                //             }
            }
            else
            {
                var _info = GetRelationMsgData(_msgInfo.CharId);
                if (_info != null)
                {
                    _info.UnreadCount++;
                    RefreshUnreadNum(_info.CharacterId);

                    RefreshNoticeUnreadNum();
                    _msgInfo.RoleId = _info.Type;
                    _info.NextIndex = 0;
                    AddRelationCellNextIndex(_info.Index);
                    RefreshRelationOrder(true);

                    for (int i = 0; i < m_DataModel.CurInfos.Count; i++)
                    {
                        if (m_DataModel.CurInfos[i].Guid == _info.CharacterId)
                        {
                            var info = m_DataModel.CurInfos[i];                            
                            m_DataModel.CurInfos.Remove(m_DataModel.CurInfos[i]);
                            m_DataModel.CurInfos.Insert(0, info);
                        }
                    }
                }
                else
                {
                    //创建联系人
                    AddRelationCellMsg(_msgInfo.CharId, _msgInfo.Name, _message);
                    return;
                }
            }
            AddChattingRecords(_msgInfo.CharId, _message);
        }

        private ObservableCollection<FriendInfoDataModel> GetFriendsInfo()
        {
            return m_DataModel.FriendInfos;
        }
        private ObservableCollection<ContactInfoDataModel> GetContactInfo()
        {
            return m_DataModel.ContactInfos;
        }

        private int GetFriendsInfoNumber()
        {
            return m_DataModel.FriendInfos.Count;
        }
        private int GetContactInfoNumber()
        {
            return m_DataModel.ContactInfos.Count;
        }
        private void RefreshUnreadNum(ulong  msgCharId)
        {
            for (int i = 0; i < m_DataModel.CurInfos.Count; i++)
            {
                if (m_DataModel.CurInfos[i].Guid == msgCharId)
                {
                    m_DataModel.CurInfos[i].UnreadNum++;
                }
            }

            for (int i = 0; i < m_DataModel.FriendInfos.Count; i++)
            {
                if (m_DataModel.FriendInfos[i].Guid == msgCharId)
                {
                    m_DataModel.FriendInfos[i].UnreadNum++;
                }
            }
        }

        private void OnClickInfoEvent(IEvent ievent)
        {
            var _e = ievent as FriendClickShowInfo;

            var _tabid = 0;
            var _DataModelIsSelectTabCount0 = m_DataModel.IsSelectTab.Count;
            //for (var i = 0; i < _DataModelIsSelectTabCount0; i++)
            //{
            //    if (m_DataModel.IsSelectTab[i])
            //    {
            //        if (i == 0)
            //        {
            //            _tabid = 7;
            //            break;
            //        }
            //        if (i == 1)
            //        {
            //            _tabid = 8;
            //            break;
            //        }
            //        if (i == 2)
            //        {
            //            _tabid = 9;
            //            break;
            //        }
            //        if (i == 3)
            //        {
            //            _tabid = 10;
            //            break;
            //        }
            //    }
            //}
            if (_tabid == 0)
            {
                switch (_e.Data.FromType)
                {
                    case (int)eTab.Temp:
                        _tabid = 10;
                        break;
                    case (int)eTab.Friend:
                        _tabid = 7;
                        break;
                    case (int)eTab.Black:
                        _tabid = 9;
                        break;
                    case (int)eTab.Enemy:
                        _tabid = 8;
                        break;

                }
            }
            var _d = _e.Data;
            PlayerDataManager.Instance.ShowCharacterPopMenu(_d.Guid, _d.Name, _tabid, _d.Level, _d.Ladder, _d.TypeId);
        }

        private void OnFindClickCharEvent(IEvent ievent)
        {
            var _e = ievent as FriendSeekBtnClick;
            if (_e == null)
            {
                return;
            }
            m_DataModel.SeekInfos.Clear();
            m_DataModel.EmptyTips[3] = true;
            var _t = _e.Type;
            if (_t == 0)
            {
                if ((Game.Instance.ServerTime - m_cdTimeList[1]).TotalSeconds < 3)
                {
                    //请勿频繁查询
                    GameUtils.ShowHintTip(220217);
                    return;
                }
                m_cdTimeList[1] = Game.Instance.ServerTime;
                if (!FindPartner())
                {
                    return;
                }
            }
            else
            {
                if ((Game.Instance.ServerTime - m_cdTimeList[0]).TotalSeconds < 3)
                {
                    //请勿频繁征友
                    GameUtils.ShowHintTip(220216);
                    return;
                }
                m_cdTimeList[0] = Game.Instance.ServerTime;
                QuickFindPartner();
            }

            for (var i = 0; i < m_DataModel.IsSelectTab.Count; i++)
            {
                m_DataModel.IsSelectTab[i] = false;
            }
            m_DataModel.IsSelectTab[4] = true;
            m_DataModel.SelectToggle = 4;
        }

        private void OnPartnerAyncAddEvent(IEvent ievent)
        {
            var _e = ievent as FriendAddSyncEvent;
            AyncEnemyNotic(_e.Type);
            AddPartnerMsg(_e.Type, _e.Data);
        }

        private void AyncEnemyNotic(int enemyID)
        {
            var controllerBase = UIManager.Instance.GetController(UIConfig.SNSFrameUI);
            if (controllerBase == null)
            {
                return;
            }
            if (enemyID == 2)
            {
                if (controllerBase.State != FrameState.Open)
                {
                    PlayerDataManager.Instance.NoticeData.NewEnemy = 1;
                }
                PlayerDataManager.Instance.NoticeData.FriendNewEnemy = 1;
            }
        }

        private void OnPartnerAyncDeleteEvent(IEvent ievent)
        {
            var e = ievent as FriendDelSyncEvent;
            DeletePartnerMsg(e.Type, e.CharacterId);
        }

        private void OnPartnerAyncRefreshEvent(IEvent ievent)
        {
            var e = ievent as FriendUpdateSyncEvent;
            var _infos = e.Data.Characters;
            var _fiendChange = false;
            var _enemyChange = false;
            var _blackChange = false;
            {
                // foreach(var info in infos)
                var __enumerator4 = (_infos).GetEnumerator();
                while (__enumerator4.MoveNext())
                {
                    var _info = __enumerator4.Current;
                    {
                        foreach (var _friendInfo in m_DataModel.FriendInfos)
                        {
                            if (_friendInfo.Guid == _info.Id)
                            {
                                GetPartnerMsgDataModel(_info, _friendInfo);
                                _fiendChange = true;
                                break;
                            }
                        }
                        foreach (var _enemyInfo in m_DataModel.EnemyInfos)
                        {
                            if (_enemyInfo.Guid == _info.Id)
                            {
                                GetPartnerMsgDataModel(_info, _enemyInfo);
                                _enemyChange = true;
                                break;
                            }
                        }
                        foreach (var _blackInfo in m_DataModel.BlackInfos)
                        {
                            if (_blackInfo.Guid == _info.Id)
                            {
                                GetPartnerMsgDataModel(_info, _blackInfo);
                                _blackChange = true;
                                break;
                            }
                        }
                        UpdateRelation(_info);
                    }
                }
            }
            if (_fiendChange)
            {
                var _list = new List<FriendInfoDataModel>(m_DataModel.FriendInfos);
                _list.Sort();

                SetPartnerMsg(1, new ObservableCollection<FriendInfoDataModel>(_list));
            }
            if (_enemyChange)
            {
                var _list = new List<FriendInfoDataModel>(m_DataModel.EnemyInfos);
                _list.Sort();

                SetPartnerMsg(2, new ObservableCollection<FriendInfoDataModel>(_list));
            }
            if (_blackChange)
            {
                var _list = new List<FriendInfoDataModel>(m_DataModel.BlackInfos);
                _list.Sort();


                SetPartnerMsg(3, new ObservableCollection<FriendInfoDataModel>(_list));
            }
        }

        private void UpdateRelation(CharacterSimpleData info)
        {
            foreach (var v in m_DataModel.ContactInfos)
            {
                if (v.CharacterId == info.Id)
                {
                    isRes = false;
                    v.Level = info.Level;
                    v.Ladder = info.Ladder;
                    v.IsOnline = info.Online;
                    v.Vip = info.Vip;
                    break;
                }
            }
        }

        private void OnPartnerClicTabEvent(IEvent ievent)
        {
            var _e = ievent as FriendClickTabEvent;
            for (var i = 0; i < m_DataModel.IsSelectTab.Count; i++)
            {
                m_DataModel.IsSelectTab[i] = false;
            }
            m_DataModel.IsSelectTab[_e.Type] = true;

            m_DataModel.SelectToggle = _e.Type;

            if (_e.Type != 4)
            {
                m_asyncState = -1;
                OnClearRelation();
                WriteChacheRecords();
            }
        }

        private void OnPartnerClicType(IEvent ievent)
        {
            var e = ievent as FriendClickType;
            switch (e.Type)
            {
                case 1:
                    {
                        SendChatMsg();
                    }
                    break;
                case 2:
                    {
                        LoadOther();
                    }
                    break;
                case 3:
                    {
                        ClearChatRec();
                    }
                    break;
                case 4:
                    {
                        WriteChacheRecords();
                        RefreshRelationOrder(true);
                    }
                    break;
                case 5:
                case 6:
                    {
                        OnInputFocus(e.Type);
                    }
                    break;
            }
        }

        private void OnFriendContactCell(IEvent ievent)
        {
            var e = ievent as FriendContactCell;
            ChoosePartnerRelationCell(e.ID);
        }

        private void OnPartnerContactAddFriendEvent(IEvent ievent)
        {
            var _e = ievent as FriendContactClickAddFriend;

            var _addFriendEvent = new FriendOperationEvent(1, 1, _e.Data.Name, _e.Data.Guid);
            EventDispatcher.Instance.DispatchEvent(_addFriendEvent);
        }

        private void OnPartnerOperaEvent(IEvent ievent)
        {
            var _e = ievent as FriendOperationEvent;
            var _ft = _e.FriendType;
            if (_e.OperationType == 1)
            {
                ClearChatRec();
                AddPartner(_e.Id, _e.FriendType);
                if (_e.FriendType == 3 && true == HasFriend(1, _e.Id))
                {//添加黑名单把好友删除
                    NetManager.Instance.StartCoroutine(DeletePartnerByIdCoroutine(_e.Id, 1));
                }
                else if (_e.FriendType == 1 && true == HasFriend(3, _e.Id))
                {//添加好友把黑名单删除
                    NetManager.Instance.StartCoroutine(DeletePartnerByIdCoroutine(_e.Id, 3));
                }
            }
            else
            {
                NetManager.Instance.StartCoroutine(DeletePartnerByIdCoroutine(_e.Id, _e.FriendType));
            }
        }

        private bool HasFriend(int type, ulong guid)
        {
            var l = GetFriMsg(type);
            foreach (var v in l)
            {
                if (v.Guid == guid)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnPartnerRecEvent(IEvent ievent)
        {

            var _e = ievent as FriendReceive;
            if (m_asyncState == -1)
            {
                return;
            }

            var _datas = _e.Data.Characters;
            m_DataModel.SeekInfos.Clear();
            if (_datas.Count == 0)
            {
                //没有查找结果
                m_DataModel.EmptyTips[4] = true;
                var _e1 = new ShowUIHintBoard(270103);
                EventDispatcher.Instance.DispatchEvent(_e1);
            }
            else
            {
                m_DataModel.EmptyTips[4] = false;
                RefreshPartnerMsg(4, _datas);
                //UpdateListByType((int)eTab.Find);
            }

            var _e2 = new FriendNotify(1);
            EventDispatcher.Instance.DispatchEvent(_e2);
        }

        private void OnAddNodeEvent(IEvent ievent)
        {
            var _e = ievent as AddFaceNode;
            if (_e.Type != 3)
            {
                return;
            }
            AddFace(_e.FaceId);
        }

        //private void OnprivChatEvent(IEvent ievent)
        //{
        //    var _e = ievent as ChatMainPrivateChar;
        //    var _arg = new FriendArguments();
        //    _arg.Type = 1;
        //    _arg.Data = _e.Data;
        //    if (State == FrameState.Open)
        //    {
        //        RefreshData(_arg);
        //    }
        //    else
        //    {
        //        var _e1 = new Show_UI_Event(UIConfig.FriendUI, _arg);
        //        EventDispatcher.Instance.DispatchEvent(_e1);
        //    }
        //}

        private void OnshowTargetChatEvent(IEvent ievent)
        {
            var _e = ievent as ChatShareItemEvent;
            if (_e.Type != 3)
            {
                return;
            }
            AddShowItem(_e.Data);
        }

        private void OncontVoiceChatEvent(IEvent ievent)
        {
            var _e = ievent as ChatVoiceContent;
            foreach (var _listCach in m_unWriteListCaches)
            {
                foreach (var _data in _listCach.Value)
                {
                    if (_data.SoundData == _e.SoundData)
                    {
                        _data.Content = _e.Content;
                        return;
                    }
                }
            }
        }
        #endregion






        private void AddChattingRecords(ulong charId, FriendMessageDataModel message)
        {
            var _data = message.MessageData as ChatMessageData;
            if (_data == null)
            {
                return;
            }

            List<ChatMessageData> list;
            if (!m_unWriteListCaches.TryGetValue(charId, out list))
            {
                list = new List<ChatMessageData>();
                m_unWriteListCaches[charId] = list;
            }

            if (list.Count == 0)
            {
                _data.ShowTime = 1;
            }
            else
            {
                var _last = list[list.Count - 1];
                var _dif = DateTime.FromBinary(_data.Times) - DateTime.FromBinary(_last.Times);
                if (_dif.TotalMinutes > 5)
                {
                    _data.ShowTime = 1;
                }
                else
                {
                    _data.ShowTime = 0;
                }
            }
            list.Add(_data);
            if (_data.SoundData != null)
            {
                var _e = new ChatSoundTranslateAddEvent(_data.SoundData);
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            //SaveChatRecord(charId, name, message, type,lv,ladder);
        }

        private void AddRelationCellMsg(ulong charId, string name, FriendMessageDataModel message)
        {
            if (m_DataModel.SelectContact.CharacterId == charId)
            {
                return;
            }
            m_DataModel.SelectContact.HasUpdate = false;//这里加完应该让他更新下
            //请玩家数据
            ApplyPlayerheadMsg(charId, info =>
            {
                //加入联系人
                var _infoData = GetRelationMsgData(charId);
                if (_infoData == null)
                {
                    _infoData = new ContactInfoDataModel();
                    _infoData.HasUpdate = true;
                    _infoData.CharacterId = info.CharacterId;
                    _infoData.Name = info.Name;
                    _infoData.Type = info.RoleId;
                    _infoData.UnreadCount = 0;
                }
                else
                {
                    m_DataModel.ContactInfos.Remove(_infoData);
                }
                _infoData.Ladder = info.Ladder;
                _infoData.Level = info.Level;
                _infoData.UnreadCount++;
                InsetRelationMsg(_infoData);
                RefreshNoticeUnreadNum();
                var _msgInfo = message.MessageData as ChatMessageData;
                if (_msgInfo == null)
                {
                    return;
                }
                _msgInfo.RoleId = _infoData.Type;
                _infoData.NextIndex = 0;
                AddRelationCellNextIndex(_infoData.Index);
                RefreshRelationCellNextIndex();
                //写入文件
                AddChattingRecords(charId, message);
            });
        }

        private void AddRelationCellNextIndex(int index)
        {
            var _c = m_DataModel.ContactInfos.Count;
            for (var i = 0; i < _c; i++)
            {
                var _d = m_DataModel.ContactInfos[i];
                if (_d.NextIndex != index)
                {
                    _d.NextIndex++;
                }
            }
        }

        private void AddRelationMsg(PlayerHeadInfoMsg info)
        {
            var _addInfo = GetRelationMsgData(info.CharacterId);
            if (_addInfo == null)
            {
                _addInfo = new ContactInfoDataModel();
                _addInfo.Name = info.Name;
                _addInfo.CharacterId = info.CharacterId;
            }
            else
            {
                //删除加到开始位置
                m_DataModel.ContactInfos.Remove(_addInfo);
            }
            _addInfo.HasUpdate = true;
            _addInfo.Ladder = info.Ladder;
            _addInfo.Level = info.Level;
            _addInfo.Type = info.RoleId;
            _addInfo.IsSelect = true;
            InsetRelationMsg(_addInfo);
            RefreshRelationCellNextIndex();
        }

        private void AddFace(int faceId)
        {
            var _dataChatInfoNode = new ChatInfoNodeData();
            _dataChatInfoNode.Type = (int)eChatLinkType.Face;
            _dataChatInfoNode.Id = faceId;
            var _str = "";
            using (var _ms = new MemoryStream())
            {
                Serializer.Serialize(_ms, _dataChatInfoNode);
                var _wrap = LZ4Codec.Encode32(_ms.GetBuffer(), 0, (int)_ms.Length);
                _str = Convert.ToBase64String(_wrap);
            }
            _str = SpecialCode.ChatBegin + _str + SpecialCode.ChatEnd;
            var _value = "{" + faceId + "}";
            m_dicItemLink[_value] = _str;

            var _inputStr = GameUtils.GetDictionaryText(100001058);
            if (m_DataModel.InputChat == _inputStr)
            {
                m_DataModel.InputChat = _value;
            }
            else
            {
                m_DataModel.InputChat += _value;
            }
        }

        private void AddPartner(ulong uid, int type)
        {
            if (uid < 1000)
            {
                //机器人提示玩家不在线
                GameUtils.ShowHintTip(200000003);
                return;
            }
            switch (type)
            {
                case 1:
                    {
                        var _friendMax = Table.GetClientConfig(320).Value.ToInt();
                        if (m_DataModel.FriendInfos.Count >= _friendMax)
                        {
                            //ShowUIHintBoard e = new ShowUIHintBoard("好友列表已满");
                            var _e = new ShowUIHintBoard(220200);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            return;
                        }
                        {
                            // foreach(var info in DataModel.FriendInfos)
                            var __enumerator5 = (m_DataModel.FriendInfos).GetEnumerator();
                            while (__enumerator5.MoveNext())
                            {
                                var _info = __enumerator5.Current;
                                {
                                    if (_info.Guid == uid)
                                    {
                                        //已经是好友了
                                        var _e = new ShowUIHintBoard(270099);
                                        EventDispatcher.Instance.DispatchEvent(_e);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        var _enemyMax = Table.GetClientConfig(321).Value.ToInt();
                        if (m_DataModel.EnemyInfos.Count >= _enemyMax)
                        {
                            //ShowUIHintBoard e = new ShowUIHintBoard("仇人列表已满");
                            var _e = new ShowUIHintBoard(220201);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            return;
                        }
                        {
                            // foreach(var info in DataModel.EnemyInfos)
                            var __enumerator6 = (m_DataModel.EnemyInfos).GetEnumerator();
                            while (__enumerator6.MoveNext())
                            {
                                var _info = __enumerator6.Current;
                                {
                                    if (_info.Guid == uid)
                                    {
                                        //已经是仇人了
                                        var _e = new ShowUIHintBoard(270100);
                                        EventDispatcher.Instance.DispatchEvent(_e);
                                        return;
                                    }
                                }
                            }
                        }
                        //   NetManager.Instance.StartCoroutine(DelFriendByIdCoroutine( uid, type));
                    }
                    break;
                case 3:
                    {
                        var _blackMax = Table.GetClientConfig(322).Value.ToInt();
                        if (m_DataModel.BlackInfos.Count >= _blackMax)
                        {
                            //ShowUIHintBoard e = new ShowUIHintBoard("屏蔽列表已满");
                            var _e = new ShowUIHintBoard(220202);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            return;
                        }
                        {
                            // foreach(var info in DataModel.BlackInfos)
                            var __enumerator7 = (m_DataModel.BlackInfos).GetEnumerator();
                            while (__enumerator7.MoveNext())
                            {
                                var _info = __enumerator7.Current;
                                {
                                    if (_info.Guid == uid)
                                    {
                                        //已经屏蔽了
                                        var _e = new ShowUIHintBoard(270101);
                                        EventDispatcher.Instance.DispatchEvent(_e);
                                        return;
                                    }
                                }
                            }
                        }
                        //       NetManager.Instance.StartCoroutine(DelFriendByIdCoroutine( uid, type));
                    }
                    break;
            }
            NetManager.Instance.StartCoroutine(AccIdAddPartnerCoroutine(uid, type));
        }

        private IEnumerator AccIdAddPartnerCoroutine(ulong uid, int type)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Info(".............AddFriendById..................");
                var _msg = NetManager.Instance.AddFriendById(uid, type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        AddPartnerMsg(type, _msg.Response);
                        switch (type)
                        {
                            case 1:
                                {
                                    m_DataModel.EmptyTips[0] = false;
                                    var _e = new ShowUIHintBoard(270222);
                                    EventDispatcher.Instance.DispatchEvent(_e);

                                    PlatformHelper.UMEvent("Friend", "Add", uid.ToString());
                                }
                                break;
                            case 2:
                                {
                                    m_DataModel.EmptyTips[1] = false;
                                    var _e = new ShowUIHintBoard(270223);
                                    EventDispatcher.Instance.DispatchEvent(_e);
                                }
                                break;
                            case 3:
                                {
                                    m_DataModel.EmptyTips[2] = false;
                                    var _e = new ShowUIHintBoard(270224);
                                    EventDispatcher.Instance.DispatchEvent(_e);

                                    PlatformHelper.UMEvent("Friend", "PingBi", uid.ToString());
                                }
                                break;
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FriendIsHave)
                    {
                        switch (type)
                        {
                            case 1:
                                {
                                    var _e = new ShowUIHintBoard(270099);
                                    EventDispatcher.Instance.DispatchEvent(_e);
                                }
                                break;
                            case 2:
                                {
                                    var _e = new ShowUIHintBoard(270100);
                                    EventDispatcher.Instance.DispatchEvent(_e);
                                }
                                break;
                            case 3:
                                {
                                    var _e = new ShowUIHintBoard(270101);
                                    EventDispatcher.Instance.DispatchEvent(_e);
                                }
                                break;
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FriendIsMore)
                    {
                        //ShowUIHintBoard e = new ShowUIHintBoard("好友列表已满");
                        var _e = new ShowUIHintBoard(220200);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_EnemyIsMore)
                    {
                        //ShowUIHintBoard e = new ShowUIHintBoard("仇人列表已满");
                        var _e = new ShowUIHintBoard(220201);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ShieldIsMore)
                    {
                        //ShowUIHintBoard e = new ShowUIHintBoard("屏蔽列表已满");
                        var _e = new ShowUIHintBoard(220202);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("AddFriendById errocode = {0}", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("AddFriendById state = {0}", _msg.State);
                }
            }
        }

        private void AddPartnerMsg(int type, CharacterSimpleData _friend)
        {
            var _list = new List<FriendInfoDataModel>(GetFriMsg(type));
            var _data = GetPartnerMsgDataModel(_friend);
            _list.Add(_data);
            _list.Sort();
            var _dataList = new ObservableCollection<FriendInfoDataModel>(_list);
            SetPartnerMsg(type, _dataList);
        }

        private void AddShowItem(BagItemDataModel itemData)
        {
            var _dataChatInfoNode = new ChatInfoNodeData();
            _dataChatInfoNode.Type = (int)eChatLinkType.Equip;
            _dataChatInfoNode.Id = itemData.ItemId;

            var _nowItemExdataCount0 = itemData.Exdata.Count;
            for (var i = 0; i < _nowItemExdataCount0; i++)
            {
                _dataChatInfoNode.ExData.Add(itemData.Exdata[i]);
            }
            var _str = "";
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, _dataChatInfoNode);
                var wrap = LZ4Codec.Encode32(ms.GetBuffer(), 0, (int)ms.Length);
                _str = Convert.ToBase64String(wrap);
            }

            _str = SpecialCode.ChatBegin + _str + SpecialCode.ChatEnd;
            var _tbTable = Table.GetItemBase(itemData.ItemId);
            var _value = _tbTable.Name;
            var _color = GameUtils.GetTableColorString(_tbTable.Quality);
            _value = String.Format("[{0}][{1}][-]", _color, _value);

            m_dicItemLink[_value] = _str;
            //         var inputStr = DataModel.InputChat + str;
            //         {
            //             var __enumerator3 = (mDicItemLink).GetEnumerator();
            //             while (__enumerator3.MoveNext())
            //             {
            //                 var i = __enumerator3.Current;
            //                 {
            //                     inputStr = inputStr.Replace(i.Key, i.Value);
            //                 }
            //             }
            //         }

            var _inputStr = GameUtils.GetDictionaryText(100001058);
            if (m_DataModel.InputChat == _inputStr)
            {
                m_DataModel.InputChat = _value;
            }
            else
            {
                m_DataModel.InputChat += _value;
            }
        }

        private void ApplyAllMsg()
        {
            for (var i = 1; i <= 3; i++)
            {
                ApplyPartners(i);
            }
            ApplyRecRelations();

            LoginSaveCache();
        }

        private void ApplyPartners(int type)
        {
            var _e2 = new Close_UI_Event(UIConfig.OperationList);
            EventDispatcher.Instance.DispatchEvent(_e2);
            NetManager.Instance.StartCoroutine(ApplypartnersCoroutine(type));
        }


        private IEnumerator ApplypartnersCoroutine(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Info(".............ApplyFriends..................");
                var _msg = NetManager.Instance.ApplyFriends(type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        RefreshPartnerMsg(type, _msg.Response.Characters);
                        if (_msg.Response.Characters.Count == 0)
                        {
                            m_DataModel.EmptyTips[type] = true;
                        }
                        else
                        {
                            m_DataModel.EmptyTips[type] = false;
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("ApplyFriends errocode = {0}", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ApplyFriends state = {0}", _msg.State);
                }
            }
        }

        private void ApplyPlayerheadMsg(ulong charId, Action<PlayerHeadInfoMsg> act)
        {
            NetManager.Instance.StartCoroutine(ApplyPlayerHeadMsgEnumerator(charId, act));
        }

        private IEnumerator ApplyPlayerHeadMsgEnumerator(ulong charId, Action<PlayerHeadInfoMsg> act)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyPlayerHeadInfo(charId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _info = _msg.Response;
                        if (act != null)
                        {
                            if (m_DataModel.SelectContact.HasUpdate == false)
                            {
                                act(_info);
                            }

                            //foreach (var item in m_DataModel.FriendInfos)
                            //{
                            //    m_DataModel.FriendCur = 0;
                            //    if (item.Guid == charId)
                            //    {
                            //        item.IsOnline = 1;
                            //        item.Ladder = _info.Ladder;
                            //        item.Level = _info.Level;
                            //    }
                            //    if (item.IsOnline > 0)
                            //    {
                            //        m_DataModel.FriendCur++;
                            //    }
                            //}
                            //foreach (var item in m_DataModel.ContactInfos)
                            //{
                            //    if (item.CharacterId == charId)
                            //    {
                            //        item.IsOnline = 1;
                            //        item.Ladder = _info.Ladder;
                            //        item.Level = _info.Level;
                            //    }
                            //}
                        }
                    }
                }
            }
        }

        private void NewApplyPlayerheadMsgEvent(IEvent ieve)
        {
            NewApplyPlayerheadMsg(0);
        }

        private void NewApplyPlayerheadMsg(int characterID)
        {
            NetManager.Instance.StartCoroutine(ApplyNewPlayerHeadMsgEnumerator(characterID));
        }

        private IEnumerator ApplyNewPlayerHeadMsgEnumerator(int characterID)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyFriendListData(characterID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var onlineNum = 0;
                        var _info = _msg.Response;
                        if (_info == null) yield return 0;
                        //在线列表
                        var onlineList=new ObservableCollection<FriendInfoDataModel>();
                        for (int i = 0; i < m_DataModel.FriendInfos.Count; i++)
                        {
                            for (int j = 0; j < _info.Records.Count; j++)
                            {
                                if (m_DataModel.FriendInfos[i].Guid == _info.Records[j].FriendID)
                                {
                                    onlineNum++;
                                    onlineList.Add(m_DataModel.FriendInfos[i]);
                                    m_DataModel.FriendInfos[i].IsOnline = _info.Records[j].IsOnLine;
                                    m_DataModel.FriendInfos[i].Level = _info.Records[j].FriendLevel;
                                    m_DataModel.FriendInfos[i].Ladder = _info.Records[j].FriendRebronLV;
                                    m_DataModel.FriendInfos[i].Name = _info.Records[j].FriendName;
                                }
                            }
                        }
                        for (int i = 0; i < m_DataModel.FriendInfos.Count; i++)
                        {
                            if (!onlineList.Contains(m_DataModel.FriendInfos[i]))
                            {
                                m_DataModel.FriendInfos[i].IsOnline = 0;
                            }
                        }
                        if (m_DataModel.ContactInfos.Count > 0)
                        {
                            for (int i = 0; i < m_DataModel.ContactInfos.Count; i++)
                            {
                                for (int j = 0; j < _info.Records.Count; j++)
                                {
                                    if (m_DataModel.ContactInfos[i].CharacterId == _info.Records[j].FriendID)
                                    {
                                        m_DataModel.ContactInfos[i].Level = _info.Records[j].FriendLevel;
                                        m_DataModel.ContactInfos[i].Ladder = _info.Records[j].FriendRebronLV;
                                        m_DataModel.ContactInfos[i].Name = _info.Records[j].FriendName;
                                       
                                    }
                                }
                            }
                        }

                        if (_info.Records.Count <= 0)
                        {
                            if (m_DataModel.FriendInfos.Count > 0)
                            {
                                for (int i = 0; i < m_DataModel.FriendInfos.Count; i++)
                                {
                                    m_DataModel.FriendInfos[i].IsOnline = 0;
                                }
                            }
                        }
                        m_DataModel.FriendCur = onlineNum;
                    }
                }
            }
        }

        private void ApplyRecRelations()
        {
            NetManager.Instance.StartCoroutine(ApplyRecRelationCoroutine());
        }

        private IEnumerator ApplyRecRelationCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.GetRecentcontacts(-1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        LoadHistoryChatRecord();
                        var _list = _msg.Response.Characters;
                        RefreshRecentRelations(_list);
                    }
                }
            }
        }

        private void CheckRefreshMsg(ContactInfoDataModel info,string name, int level, int ladder)
        {
            if (info.Ladder != ladder || info.Level != level || !string.IsNullOrEmpty(name))
            {
                info.Name = name;
                info.Ladder = ladder;
                info.Level = level;
                RefreshChatRec(info.CharacterId, info.Name, info.Level, info.Ladder);
            }
        }

        private void ClearChatRec()
        {
            m_DataModel.SelectContact.UnreadCount = 0;
            RefreshNoticeUnreadNum();
            m_loadSeekPostion = 0;
            m_DataModel.ChatMessages = new ObservableCollection<FriendMessageDataModel>();

            var _charId = m_DataModel.SelectContact.CharacterId;
            List<ChatMessageData> list;
            if (m_unWriteListCaches.TryGetValue(_charId, out list))
            {
                m_unWriteListCaches.Remove(_charId);
            }

            DelChatRec(_charId);
            DelRecRelations(_charId);
        }

        private void DelChatRec(ulong charId)
        {
            var _fileName = Path.Combine(m_characterChatDirectory, charId.ToString());
            var _hasFile = File.Exists(_fileName);
            if (_hasFile == false)
            {
                return;
            }
            File.Delete(_fileName);
        }

        private void DelRecRelations(ulong characterId)
        {
            NetManager.Instance.StartCoroutine(DelRecRelationsCoroutine(characterId));
        }

        private IEnumerator DelRecRelationsCoroutine(ulong characterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.DeleteRecentcontacts(characterId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                    }
                }
            }
        }

        private IEnumerator DeletePartnerByIdCoroutine(ulong uid, int type)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Info(".............DelFriendById..................");
                var _msg = NetManager.Instance.DelFriendById(uid, type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DeletePartnerMsg(type, uid);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("DelFriendById errocode = {0}", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("DelFriendById state = {0}", _msg.State);
                }
            }
        }

        private void DeletePartnerMsg(int type, ulong characterId)
        {
            var _infos = GetFriMsg(type);
            FriendInfoDataModel delInfo = null;
            int online = 0;
            {
                // foreach(var info in infos)
                var __enumerator9 = (_infos).GetEnumerator();
                while (__enumerator9.MoveNext())
                {
                    var _info = __enumerator9.Current;
                    {
                        if (_info.Guid == characterId)
                        {
                            delInfo = _info;
                            online = _info.IsOnline;
                            break;
                        }
                    }
                }
            }
            _infos.Remove(delInfo);
            if (_infos.Count == 0)
            {
                m_DataModel.EmptyTips[type] = true;
            }

            //从名单里删除
            switch (type)
            {
                case 1:
                    m_DataModel.FriendMax = m_DataModel.FriendInfos.Count;
                    m_DataModel.FriendCur -= online;
                    break;
                case 2:
                    m_DataModel.EnemyMax = m_DataModel.EnemyInfos.Count;
                    m_DataModel.EnemyCur -= online;
                    break;
                case 3:
                    m_DataModel.BlackMax = m_DataModel.BlackInfos.Count;
                    m_DataModel.BlackCur -= online;
                    break;
            }
            if (type == m_DataModel.SelectToggle)
            {
                UpdateListByType(type);
            }
        }

        private ContactInfoDataModel GainChatFileMsg(string fullName, string fileName)
        {
            var _charId = fileName.ToUlong();
            using (var _fs = new FileStream(fullName, FileMode.Open, FileAccess.Read))
            {
                var _type = 0;
                var _lv = 0;
                var _ladder = 0;
                var _charName = "";
                var _buffer = new byte[4];
                {
                    try
                    {
                        _fs.Read(_buffer, 0, 4);
                        _lv = SerializerUtility.ReadInt(_buffer, 0);

                        _fs.Read(_buffer, 0, 4);
                        _ladder = SerializerUtility.ReadInt(_buffer, 0);

                        _fs.Read(_buffer, 0, 4);
                        _type = SerializerUtility.ReadInt(_buffer, 0);

                        _fs.Read(_buffer, 0, 4);
                        var _length = SerializerUtility.ReadInt(_buffer, 0);
                        var _data = new byte[_length];
                        _fs.Read(_data, 0, _length);
                        _charName = Encoding.UTF8.GetString(_data);
                    }
                    catch (Exception)
                    {
                        _fs.Close();
                        DelChatRec(_charId);
                        return null;
                    }
                }

                _fs.Close();
                var _info = new ContactInfoDataModel();
                _info.Name = _charName;
                _info.HasUpdate = false;
                _info.CharacterId = _charId;
                _info.Level = _lv;
                _info.Type = _type;
                _info.Ladder = _ladder;
                return _info;
            }
        }

        private ContactInfoDataModel GetRelationMsgData(ulong id)
        {
            foreach (var _info in m_DataModel.ContactInfos)
            {
                if (_info.CharacterId == id)
                {
                    return _info;
                }
            }
            return null;
        }

        private FriendInfoDataModel GetPartnerMsg(int type, ulong guid)
        {
            switch (type)
            {
                case 0:
                    {
                        var _count = m_DataModel.FriendInfos.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _info = m_DataModel.FriendInfos[i];
                            if (_info.Guid == guid)
                            {
                                return _info;
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        var _count = m_DataModel.EnemyInfos.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _info = m_DataModel.EnemyInfos[i];
                            if (_info.Guid == guid)
                            {
                                return _info;
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        var _count = m_DataModel.BlackInfos.Count;
                        for (var i = 0; i < _count; i++)
                        {
                            var _info = m_DataModel.BlackInfos[i];
                            if (_info.Guid == guid)
                            {
                                return _info;
                            }
                        }
                    }
                    break;
            }
            return null;
        }

        private ObservableCollection<FriendInfoDataModel> GetFriMsg(int type)
        {
            switch (type)
            {
                case 1:
                    {
                        return m_DataModel.FriendInfos;
                    }
                    break;
                case 2:
                    {
                        return m_DataModel.EnemyInfos;
                    }
                    break;
                case 3:
                    {
                        return m_DataModel.BlackInfos;
                    }
                    break;
                case 4:
                    {
                        return m_DataModel.SeekInfos;
                    }
                    break;
            }
            return null;
        }

        private FriendInfoDataModel GetPartnerMsgDataModel(CharacterSimpleData _friend, FriendInfoDataModel data = null)
        {
            if (data == null)
            {
                data = new FriendInfoDataModel();
            }
            data.Guid = _friend.Id;
            data.Name = _friend.Name;
            data.Level = _friend.Level;
            data.TypeId = _friend.TypeId;
            data.SceneId = _friend.SceneId;
            data.FightPoint = _friend.FightPoint;
            data.Ladder = _friend.Ladder;
            data.ServerId = _friend.ServerId;
            var _serverName = data.ServerName;
            PlayerDataManager.Instance.ServerNames.TryGetValue(data.ServerId, out _serverName);
            data.ServerName = _serverName;
            data.IsOnline = _friend.Online;
            data.Vip = _friend.Vip;
            data.StarNum = _friend.StarNum;
            var _str = GameUtils.GetDictionaryText(240607);
            _str = string.Format(_str, data.Level, data.Ladder);
            data.LevelInfo = _str;
            _str = GameUtils.GetDictionaryText(240606);
            _str = string.Format(_str, data.FightPoint);
            data.FightPointInfo = _str;
            return data;
        }

        private void InsetRelationMsg(ContactInfoDataModel info)
        {
            foreach (var v in m_DataModel.FriendInfos)
            {
                if (v.Guid == info.CharacterId)
                {
                    info.Ladder = v.Ladder;
                    info.Vip = v.Vip;
                    break;
                }
            }

            m_DataModel.ContactInfos.Insert(0, info);

            RefreshRelationCellIndex();
            info.NextIndex = 0;
            AddRelationCellNextIndex(info.Index);

        }

        private bool IsBelongBalckListId(ulong id)
        {
            var _count = m_DataModel.BlackInfos.Count;
            for (var i = 0; i < _count; i++)
            {
                var _info = m_DataModel.BlackInfos[i];
                if (_info.Guid == id)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBelongBalckListName(string name)
        {
            var _count = m_DataModel.BlackInfos.Count;
            for (var i = 0; i < _count; i++)
            {
                var _info = m_DataModel.BlackInfos[i];
                if (_info.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBelongEnemyListId(ulong id)
        {
            var _count = m_DataModel.EnemyInfos.Count;
            for (var i = 0; i < _count; i++)
            {
                var _info = m_DataModel.EnemyInfos[i];
                if (_info.Guid == id)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBelongPartnerListId(ulong id)
        {
            var _count = m_DataModel.FriendInfos.Count;
            for (var i = 0; i < _count; i++)
            {
                var _info = m_DataModel.FriendInfos[i];
                if (_info.Guid == id)
                {
                    return true;
                }
            }
            return false;
        }

        private List<FriendMessageDataModel> LoadCharaChatRec(ulong charId)
        {
            m_loadSeekPostion = 0;
            var _ret = new List<FriendMessageDataModel>();
            List<ChatMessageData> list;
            if (m_unWriteListCaches.TryGetValue(charId, out list) && list.Count > 0)
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var _msg = new FriendMessageDataModel();
                    _msg.MessageData = list[i];
                    _ret.Insert(0, _msg);
                }
                return _ret;
            }
            _ret = LoadCharRec(charId);
            return _ret;
        }

        private List<FriendMessageDataModel> LoadCharRec(ulong charId)
        {
            var _msgList = new List<FriendMessageDataModel>();
            var _fileName = Path.Combine(m_characterChatDirectory, charId.ToString());
            var _hasFile = File.Exists(_fileName);
            if (_hasFile == false)
            {
                return _msgList;
            }

            using (var _fs = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                var _buffer = new byte[4];
                var _length = 0;
                for (var i = 0; i < m_pageChatCount; i++)
                {
                    m_loadSeekPostion -= 4;
                    _fs.Seek(m_loadSeekPostion, SeekOrigin.End);
                    _fs.Read(_buffer, 0, 4);
                    _length = SerializerUtility.ReadInt(_buffer, 0);
                    try
                    {
                        m_loadSeekPostion -= _length;
                        _fs.Seek(m_loadSeekPostion, SeekOrigin.End);
                        var _bytes = new byte[_length];
                        _fs.Read(_bytes, 0, _length);
                        ChatMessageData data;
                        using (var _ms = new MemoryStream(_bytes, false))
                        {
                            if (_ms.Length == 0)
                            {
                                m_loadSeekPostion += 4;
                                m_loadSeekPostion += _length;
                                break;
                            }
                            data = Serializer.Deserialize<ChatMessageData>(_ms);
                            if (data == null)
                            {
                                m_loadSeekPostion += 4;
                                m_loadSeekPostion += _length;
                                break;
                            }
                            if (data.SoundData != null && data.Content == "")
                            {
                                //没有记录翻译的再请求翻译一遍
                                var _e = new ChatSoundTranslateAddEvent(data.SoundData);
                                EventDispatcher.Instance.DispatchEvent(_e);
                            }
                            var _msg = new FriendMessageDataModel();
                            _msg.MessageData = data;
                            _msgList.Insert(0, _msg);
                        }
                    }
                    catch (Exception)
                    {
                        m_loadSeekPostion += 4;
                        m_loadSeekPostion += _length;
                        break;
                    }
                }
            }
            var _c = _msgList.Count;
            var _lastTime = DateTime.Now;
            for (var i = 0; i < _msgList.Count; i++)
            {
                var _msg = _msgList[i];
                var _msgInfo = _msg.MessageData as ChatMessageData;
                if (i == 0)
                {
                    _msgInfo.ShowTime = 1;
                    _lastTime = DateTime.FromBinary(_msgInfo.Times);
                    continue;
                }
                var _msgTime = DateTime.FromBinary(_msgInfo.Times);
                var _dif = _msgTime - _lastTime;
                _lastTime = _msgTime;
                if (_dif.TotalMinutes > 5)
                {
                    _msgInfo.ShowTime = 1;
                }
                else
                {
                    _msgInfo.ShowTime = 0;
                }
            }

            return _msgList;
        }

        private void LoadHistoryChatRecord()
        {
            var _charId = PlayerDataManager.Instance.GetGuid();
            var _chatDirectory = "";
#if !UNITY_EDITOR
        _chatDirectory = Path.Combine(Application.temporaryCachePath, "ChatHistory");
#else
            _chatDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ChatHistory");
#endif


            if (!Directory.Exists(_chatDirectory))
            {
                Directory.CreateDirectory(_chatDirectory);
            }
            m_characterChatDirectory = Path.Combine(_chatDirectory, _charId.ToString());
            if (!Directory.Exists(m_characterChatDirectory))
            {
                Directory.CreateDirectory(m_characterChatDirectory);
                return;
            }
            var _folder = new DirectoryInfo(m_characterChatDirectory);
            var _files = _folder.GetFiles();
            var _list = new List<ContactInfoDataModel>();
            if (_files.Length > 0)
            {
                var _fileList = new List<FileInfo>(_files);
                _fileList.Sort((l, r) => { return (int)(r.LastWriteTime - l.LastWriteTime).TotalMilliseconds; });

                foreach (var _file in _fileList)
                {
                    var _fileName = _file.FullName;
                    var _info = GainChatFileMsg(_fileName, _file.Name);
                    if (_info != null)
                    {
                        _list.Add(_info);
                    }
                }
            }
            m_DataModel.ContactInfos = new ObservableCollection<ContactInfoDataModel>(_list);
            RefreshRelationCellIndex();
        }

        private void LoadOther()
        {
            var _info = m_DataModel.SelectContact;
            var _showList = LoadCharRec(_info.CharacterId);
            if (_showList == null)
            {
                return;
            }

            var _count = _showList.Count;

            if (_count > 0)
            {
                if (m_DataModel.ChatMessages.Count > 0)
                {
                    var _bottom = _showList[_count - 1].MessageData as ChatMessageData;
                    var _top = m_DataModel.ChatMessages[0].MessageData as ChatMessageData;
                    if (_bottom != null && _top != null)
                    {
                        if (_top.ShowTime == 1)
                        {
                            var _dif = DateTime.FromBinary(_top.Times) - DateTime.FromBinary(_bottom.Times);
                            if (_dif.TotalMinutes < 5)
                            {
                                _top.ShowTime = 0;
                            }
                        }
                    }
                }

                for (var i = _count - 1; i >= 0; i--)
                {
                    var _msg = _showList[i];

                    m_DataModel.ChatMessages.Insert(0, _msg);
                }
            }
        }



        private void OnClearRelation()
        {
            m_DataModel.SelectContact.IsSelect = false;
            m_DataModel.SelectContact = s_emptyContactInfo;
        }


        private void OnInputFocus(int type)
        {
            switch (type)
            {
                case 5:
                    {
                        if (m_DataModel.InputChat == m_inputStr)
                        {
                            m_DataModel.InputChat = string.Empty;
                        }
                    }
                    break;
                case 6:
                    {
                        if (m_DataModel.InputSeek == m_inputStr2)
                        {
                            m_DataModel.InputSeek = string.Empty;
                        }
                    }
                    break;
            }
        }


        private void QuickFindPartner(bool showNoTip = true)
        {
            NetManager.Instance.StartCoroutine(FindPartnerCoroutine(showNoTip));
        }

        private void LoginSaveCache()
        {
            m_saveListCaches = new Dictionary<ulong, List<ChatMessageData>>();

            if (m_saveTImerTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(m_saveTImerTrigger);
            }

            m_saveTImerTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime, TickSaveCache, 1000 * 10);
        }

        private void SaveChatRecordList(ContactInfoDataModel info, List<ChatMessageData> list)
        {
            var _charId = info.CharacterId;
            var _name = info.Name;
            var _type = info.Type;
            var _level = info.Level;
            var _ladder = info.Ladder;

            var _fileName = Path.Combine(m_characterChatDirectory, _charId.ToString());
            var _hasFile = File.Exists(_fileName);
            var _buffer = new byte[4];
            using (var _fs = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                if (_hasFile == false)
                {
                    SerializerUtility.WriteInt(_buffer, _level, 0);
                    _fs.Write(_buffer, 0, 4);
                    SerializerUtility.WriteInt(_buffer, _ladder, 0);
                    _fs.Write(_buffer, 0, 4);

                    SerializerUtility.WriteInt(_buffer, _type, 0);
                    _fs.Write(_buffer, 0, 4);

                    var _bytes = Encoding.UTF8.GetBytes(_name);
                    SerializerUtility.WriteInt(_buffer, _bytes.Length, 0);
                    _fs.Write(_buffer, 0, 4);
                    _fs.Write(_bytes, 0, _bytes.Length);
                }
                _fs.Seek(0, SeekOrigin.End);
                foreach (var _data in list)
                {
                    using (var _ms = new MemoryStream())
                    {
                        Serializer.Serialize(_ms, _data);
                        var _bytes = _ms.ToArray();
                        _fs.Write(_bytes, 0, _bytes.Length);
                        SerializerUtility.WriteInt(_buffer, _bytes.Length, 0);
                        _fs.Write(_buffer, 0, 4);
                    }
                }
            }
        }

        private IEnumerator SeekCharactersCoroutine(string name)
        {
            using (new BlockingLayerHelper(0))
            {
                Logger.Info(".............SeekCharacters..................");
                var _msg = NetManager.Instance.SeekCharacters(name);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_asyncState = 3;
                        var _e2 = new FriendNotify(2);
                        EventDispatcher.Instance.DispatchEvent(_e2);
                        //                     var datas = msg.Response.Characters;
                        //                     DataModel.SeekInfos.Clear();
                        //                     if (datas.Count == 0)
                        //                     {
                        //                         //没有查找结果
                        //                         DataModel.EmptyTips[3] = true;
                        //                         ShowUIHintBoard e = new ShowUIHintBoard(270103);
                        //                         EventDispatcher.Instance.DispatchEvent(e);
                        //                     }
                        //                     else
                        //                     {
                        //                         DataModel.EmptyTips[3] = false;
                        //                         UpdateFriendInfo(3, datas);
                        //                     }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Unknow)
                    {
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_StringIsNone)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("SeekCharacters errocode = {0}", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("SeekCharacters state = {0}", _msg.State);
                }
            }
        }

        private bool FindPartner()
        {
            if (string.IsNullOrEmpty(m_DataModel.InputSeek))
            {
                //输入的名字不能为空
                var _e1 = new ShowUIHintBoard(270102);
                EventDispatcher.Instance.DispatchEvent(_e1);
                return false;
            }


            NetManager.Instance.StartCoroutine(SeekCharactersCoroutine(m_DataModel.InputSeek));
            return true;
        }

        private IEnumerator FindPartnerCoroutine(bool showNoTip)
        {
            using (new BlockingLayerHelper(0))
            {
                ShowUIHintBoard e = new ShowUIHintBoard(220974);
                EventDispatcher.Instance.DispatchEvent(e);

                Logger.Info(".............SeekFriends..................");
                var _msg = NetManager.Instance.SeekFriends("");
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_asyncState = 3;
                        var _e2 = new FriendNotify(2);
                        EventDispatcher.Instance.DispatchEvent(_e2);



                        //                     var datas = msg.Response.Characters;
                        //                     DataModel.SeekInfos.Clear();
                        //                     if (datas.Count == 0)
                        //                     {
                        //                         if (showNoTip)
                        //                         {
                        //                             ShowUIHintBoard e = new ShowUIHintBoard(270103);
                        //                             EventDispatcher.Instance.DispatchEvent(e);
                        //                         }
                        //                         DataModel.EmptyTips[3] = true;
                        //                     }
                        //                     else
                        //                     {
                        //                         DataModel.EmptyTips[3] = false;
                        //                         UpdateFriendInfo(3, datas);
                        //                     }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Unknow)
                    {
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("SeekFriends errocode = {0}", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("SeekFriends state = {0}", _msg.State);
                }
            }
        }

        private ContactInfoDataModel GetContact(ulong ID)
        {
            foreach (var info in m_DataModel.ContactInfos)
            {
                if (info.CharacterId == ID)
                    return info;
            }
            //--下面改为发送时候添加
            //for (int i = 1; i <= 2; i++)
            //{
            //    var infos = GetFriMsg(1);
            //    foreach (var info in infos)
            //    {
            //        if (info.Guid == ID)
            //        {
            //            return SetRelationChatMsg(ID, info.Name, info.Ladder, info.Level, info.TypeId);

            //        }
            //    }
            //}

            return null;
        }

        private ContactInfoDataModel AddContact(ulong id)
        {
            for (int i = 1; i <= 2; i++)
            {
                var infos = GetFriMsg(i);
                foreach (var info in infos)
                {
                    if (info.Guid == id)
                    {
                        return SetRelationChatMsg(id, info.Name, info.Ladder, info.Level, info.TypeId);

                    }
                }
            }
            return null;
        }
        private void ChoosePartnerRelationCell(ulong ID)
        {
            ContactInfoDataModel info = GetContact(ID);
            if (info != null)
            {
                m_DataModel.SelectContact.IsSelect = false;
                m_DataModel.SelectContact = info;
                m_DataModel.SelectContact.IsSelect = true;
                m_DataModel.SelectContact.UnreadCount = 0;
                RefreshNoticeUnreadNum();
                SetCharaChatRec(m_DataModel.SelectContact.CharacterId);
                //if (m_DataModel.SelectContact.HasUpdate == false)
                //{
                ApplyPlayerheadMsg(m_DataModel.SelectContact.CharacterId, RefreshChooseMsg);
                //}
                lastGuid = 0;
            }
            else
            {//这里不能这么写
                lastGuid = ID;
                OnClearRelation();
                ClearChatMessage();
            }
            foreach (var v in m_DataModel.CurInfos)
            {
                v.IsSelect = v.Guid == ID;
                if (v.IsSelect)
                {
                    v.UnreadNum = 0;
                }
            }
            foreach (var v in m_DataModel.FriendInfos)
            {
                if ( v.Guid == ID)
                {
                    v.UnreadNum = 0;
                }
            }
        }

        private bool HasContact(ulong id)
        {
            foreach (var info in m_DataModel.ContactInfos)
            {
                if (info.CharacterId == id)
                    return true;
            }
            return false;
        }
        private bool HasBlack(ulong id)
        {
            foreach (var v in m_DataModel.BlackInfos)
            {
                if (v.Guid == id)
                    return true;
            }
            return false;
        }

        private bool HasEnemy(ulong id)
        {
            foreach (var v in m_DataModel.EnemyInfos)
            {
                if (v.Guid == id)
                    return true;
            }
            return false;
        }
        //------------------------------------------------------Chat-----------------------------
        private void SendChatMsg()
        {
            if (string.IsNullOrEmpty(m_DataModel.InputChat))
            {
                GameUtils.ShowHintTip(270054);
                return;
            }

            ulong _id = m_DataModel.SelectContact.IsSelect ? m_DataModel.SelectContact.CharacterId : lastGuid;


            if (true == HasBlack(_id))
            {
                GameUtils.ShowHintTip(100001379);
                return;
            }
            if (true == HasEnemy(_id))
            {
                GameUtils.ShowHintTip(100001380);
                return;
            }
            var playerLevel = PlayerDataManager.Instance.GetLevel();
            var tbChatInfo = Table.GetChatInfo(6);
            var ChatNeedLevel = 1;
            if (null != tbChatInfo)
            {
                ChatNeedLevel = tbChatInfo.NeedLevel;
            }
            //私聊等级限制
            if (playerLevel < ChatNeedLevel)
            {
                var HintStr = string.Format(GameUtils.GetDictionaryText(210113),ChatNeedLevel);
                GameUtils.ShowHintTip(HintStr);
                return;
            }
            var _charData = m_DataModel.SelectContact;
            if (GetContact(_id) == null)
            {
                _charData = AddContact(_id);
                if (_charData == null)
                {
                    GameUtils.ShowHintTip(200002452);
                    return;
                }
                m_DataModel.SelectContact.IsSelect = false;
                m_DataModel.SelectContact = _charData;
                m_DataModel.SelectContact.IsSelect = true;
                ChoosePartnerRelationCell(_id);
            }



            var _chatContent = m_DataModel.InputChat;
            var _lenth = _chatContent.GetStringLength();
            if (_lenth > GameUtils.ChatWorldCount)
            {
                //字数太长了
                var _str = GameUtils.GetDictionaryText(2000002);
                _str = string.Format(_str, GameUtils.ChatWorldCount);
                var _e1 = new ShowUIHintBoard(_str);
                EventDispatcher.Instance.DispatchEvent(_e1);
                return;
            }
            foreach (var i in m_dicItemLink)
            {
                _chatContent = _chatContent.Replace(i.Key, i.Value);
            }

            _chatContent = _chatContent.RemoveColorFalg();

            _chatContent = "/" + _charData.Name + " " + _chatContent;
            var _content = new ChatMessageContent { Content = _chatContent };
            var _selfGuid = PlayerDataManager.Instance.GetGuid();
            NetManager.Instance.StartCoroutine(SendChatMessageEnumerator((int)eChatChannel.Whisper, _content, _selfGuid,
                _charData.Name));
        }

        private IEnumerator SendChatMessageEnumerator(int chatType,
            ChatMessageContent content,
            ulong characterId,
            string targerName)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ChatChatMessage(chatType, content, characterId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    m_DataModel.InputChat = "";
                    m_dicItemLink.Clear();
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //SetChannelChatCd(chatType);
                        PlatformHelper.UMEvent("Chat", chatType.ToString(), characterId.ToString());
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.NameNotFindCharacter)
                    {
                        //玩家名字不存在                
                        var _e1 =
                            new ChatMainHelpMeesage(string.Format(GameUtils.GetDictionaryText(2000001), targerName));
                        EventDispatcher.Instance.DispatchEvent(_e1);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_SetRefuseWhisper)
                    {
                        GameUtils.ShowHintTip(string.Format(GameUtils.GetDictionaryText(998), targerName));
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_SetShieldYou)
                    {
                        //{0}屏蔽了你
                        var _str = string.Format(GameUtils.GetDictionaryText(270056), targerName);
                        GameUtils.ShowHintTip(_str);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_SetYouShield)
                    {
                        //{0}屏蔽了你
                        var _str = string.Format(GameUtils.GetDictionaryText(270055), targerName);
                        GameUtils.ShowHintTip(_str);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ChatNone
                             || _msg.ErrorCode == (int)ErrorCodes.Error_ChatLengthMax
                             || _msg.ErrorCode == (int)ErrorCodes.Error_WhisperNameNone
                             || _msg.ErrorCode == (int)ErrorCodes.Error_NotWhisperSelf)
                    {
                        var _e = new ShowUIHintBoard(200000000 + _msg.ErrorCode);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        var _e = new ShowUIHintBoard(200000000 + _msg.ErrorCode);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                }
                else
                {
                    Logger.Error("SendChatMessage Error!............State..." + _msg.State);
                }
            }
        }

        private void SetCharaChatRec(ulong charId)
        {
            var _showList = LoadCharaChatRec(charId);
            m_DataModel.ChatMessages = new ObservableCollection<FriendMessageDataModel>(_showList);
        }

        private void ClearChatMessage()
        {
            m_DataModel.ChatMessages.Clear();
        }
        private ContactInfoDataModel SetRelationChatMsg(ulong charId, string name, int ladder, int lv, int roleId)
        {
            ContactInfoDataModel addInfo = null;
            // var _flag = 0;
            foreach (var _info in m_DataModel.ContactInfos)
            {
                // _flag++;
                if (_info.CharacterId == charId)
                {
                    addInfo = _info;
                }
                _info.IsSelect = _info.CharacterId == charId;

            }
            if (addInfo != null)
            {
                m_DataModel.ContactInfos.Remove(addInfo);
                InsetRelationMsg(addInfo);
                CheckRefreshMsg(m_DataModel.SelectContact,name, lv, ladder);
                RefreshRelationCellNextIndex();

            }
            else
            {
                if (roleId != -1 && ladder != -1 && lv != -1 && !string.IsNullOrEmpty(name))
                {
                    addInfo = new ContactInfoDataModel();
                    addInfo.Name = name;
                    addInfo.HasUpdate = true;
                    addInfo.CharacterId = charId;
                    addInfo.Ladder = ladder;
                    addInfo.Level = lv;
                    addInfo.Type = roleId;
                    addInfo.IsOnline = -1;
                    addInfo.IsSelect = true;
                    InsetRelationMsg(addInfo);
                    RefreshRelationCellNextIndex();
                }
                else
                {
                    //信息不全时，请求网络
                    ApplyPlayerheadMsg(charId, AddRelationMsg);
                }
            }
            return addInfo;
        }

        private void SetPartnerMsg(int type, ObservableCollection<FriendInfoDataModel> info)
        {
            switch (type)
            {
                case 1:
                    {
                        m_DataModel.FriendCur = 0;
                        foreach (var v in info)
                        {
                            if (v.IsOnline > 0)
                            {
                                m_DataModel.FriendCur++;
                            }
                        }
                        m_DataModel.FriendInfos = info;
                        m_DataModel.FriendMax = info.Count;


                        if (m_DataModel.FriendInfos.Count == 0)
                        {
                            m_DataModel.HasFriends = false;
                        }
                        else
                        {
                            m_DataModel.HasFriends = true;
                            //UpdateListByType(0);
                        }
                    }
                    break;
                case 2:
                    {
                        m_DataModel.EnemyCur = 0;
                        foreach (var v in info)
                        {
                            if (v.IsOnline > 0)
                            {
                                m_DataModel.EnemyCur++;
                            }
                        }
                        m_DataModel.EnemyInfos = info;
                        m_DataModel.EnemyMax = info.Count;
                    }
                    break;
                case 3:
                    {
                        m_DataModel.BlackCur = 0;
                        foreach (var v in info)
                        {
                            if (v.IsOnline > 0)
                            {
                                m_DataModel.BlackCur++;
                            }
                        }
                        m_DataModel.BlackInfos = info;
                        m_DataModel.BlackMax = info.Count;

                    }
                    break;
                case 4:
                    {
                        m_DataModel.SeekInfos.Clear();
                        m_DataModel.SeekInfos = info;
                        foreach (var i in m_DataModel.SeekInfos)
                        {
                            i.IsShowAddFriend = 1;
                        }
                    }
                    break;
            }
            if (info.Count == 0)
            {
                m_DataModel.EmptyTips[type] = true;
            }
            else
            {
                m_DataModel.EmptyTips[type] = false;
            }
            if (type == m_DataModel.SelectToggle)
            {
                UpdateListByType(type);
            }
        }
        private void TickSaveCache()
        {
            if (State != FrameState.Close)
            {
                //只在关闭时调用
                return;
            }
            if (m_unWriteListCaches.Count == 0)
            {
                return;
            }

            m_saveListCaches.Clear();

            //筛选要保存的
            foreach (var _listCach in m_unWriteListCaches)
            {
                var _charId = _listCach.Key;
                List<ChatMessageData> list = null;
                if (!m_saveListCaches.TryGetValue(_charId, out list))
                {
                    list = new List<ChatMessageData>();
                    m_saveListCaches.Add(_charId, list);
                }
                foreach (var _messageData in _listCach.Value)
                {
                    if (_messageData.SoundData != null && _messageData.Content == "")
                    {
                        break;
                    }
                    list.Add(_messageData);
                }
            }
            //从cache中删除
            foreach (var _listCach in m_saveListCaches)
            {
                var _charId = _listCach.Key;
                List<ChatMessageData> list = null;
                if (m_unWriteListCaches.TryGetValue(_charId, out list))
                {
                    foreach (var _messageData in _listCach.Value)
                    {
                        list.Remove(_messageData);
                    }
                }
            }

            //写入文件
            foreach (var _listCach in m_saveListCaches)
            {
                var _charId = _listCach.Key;
                if (_listCach.Value.Count == 0)
                {
                    continue;
                }
                var _contactInfo = GetRelationMsgData(_charId);
                if (_contactInfo == null)
                {
                    continue;
                }
                SaveChatRecordList(_contactInfo, _listCach.Value);
            }

            m_saveListCaches.Clear();
        }

        private void RefreshChatRec(ulong charId,string name, int lv, int ladder)
        {
            var _fileName = Path.Combine(m_characterChatDirectory, charId.ToString());
            var _hasFile = File.Exists(_fileName);
            if (_hasFile == false)
            {
                return;
            }
            var _buffer = new byte[4];
            using (var _fs = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                SerializerUtility.WriteInt(_buffer, lv, 0);
                _fs.Write(_buffer, 0, 4);
                SerializerUtility.WriteInt(_buffer, ladder, 0);
                _fs.Write(_buffer, 0, 4);

                _fs.Seek(4, SeekOrigin.Current);
                byte[] b_name = Encoding.UTF8.GetBytes(name);
                SerializerUtility.WriteInt(_buffer, b_name.Length, 0);
                _fs.Write(_buffer, 0, 4);

                _fs.Write(b_name, 0, b_name.Length);
            }
        }

        private void RefreshRelationCellIndex()
        {
            var _c = m_DataModel.ContactInfos.Count;
            for (var i = 0; i < _c; i++)
            {
                var _d = m_DataModel.ContactInfos[i];
                _d.Index = i;
            }
        }

        private void RefreshRelationCellNextIndex()
        {
            var _c = m_DataModel.ContactInfos.Count;
            for (var i = 0; i < _c; i++)
            {
                var _d = m_DataModel.ContactInfos[i];
                _d.NextIndex = i;
            }
        }

        private void RefreshRelationOrder(bool select = false)
        {
            var _list = new List<ContactInfoDataModel>(m_DataModel.ContactInfos);
            _list.Sort((l, r) => { return l.NextIndex - r.NextIndex; });

            m_DataModel.ContactInfos = new ObservableCollection<ContactInfoDataModel>(_list);
            RefreshRelationCellIndex();
            RefreshRelationCellNextIndex();
        }
        private List<FriendInfoDataModel> confriend = new List<FriendInfoDataModel>();
        private void RefreshPartnerMsg(int type, List<CharacterSimpleData> friends)
        {
            var _list = new List<FriendInfoDataModel>();
            {
                var __list8 = friends;
                var __listCount8 = __list8.Count;
                for (var __i8 = 0; __i8 < __listCount8; ++__i8)
                {
                    var _friend = __list8[__i8];
                    {
                        var _data = GetPartnerMsgDataModel(_friend);
                        _list.Add(_data);
                    }
                }
            }
            _list.Sort();

            //if (type == 1)
            //{
            //    confriend.Clear();
            //    confriend = _list;
            //}
            var _dataList = new ObservableCollection<FriendInfoDataModel>(_list);
            SetPartnerMsg(type, _dataList);
        }

        private void RefreshNoticeUnreadNum()
        {
            var _count = 0;
            foreach (var _info in m_DataModel.ContactInfos)
            {
                _count += _info.UnreadCount;
            }
            PlayerDataManager.Instance.NoticeData.ChatUnRead = _count;
            //if (PlayerDataManager.Instance.NoticeData.ChatUnRead > 0 && m_DataModel.FriendInfos.Count > 0)
            //{
            //    PlayerDataManager.Instance.NoticeData.FriendChatUnRead = 1;
            //}
        }

        private void RefreshRecentRelations(List<PlayerHeadInfoMsg> list)
        {
            var _localList = new List<ContactInfoDataModel>(m_DataModel.ContactInfos);
            var _orderList = new List<ContactInfoDataModel>();
            var _length = list.Count;
            for (var i = _length - 1; i >= 0; i--)
            {
                var _infoMsg = list[i];
                ContactInfoDataModel find = null;
                foreach (var _model in _localList)
                {
                    if (_model.CharacterId == _infoMsg.CharacterId)
                    {
                        find = _model;
                        _localList.Remove(_model);
                        break;
                    }
                }
                if (find == null)
                {
                    find = new ContactInfoDataModel();
                    find.Name = _infoMsg.Name;
                    find.HasUpdate = true;
                    find.CharacterId = _infoMsg.CharacterId;
                    find.Level = _infoMsg.Level;
                    find.Ladder = _infoMsg.Ladder;
                    find.Type = _infoMsg.RoleId;
                    find.UnreadCount = 0;
                    RefreshNoticeUnreadNum();
                    _orderList.Add(find);
                }
                else
                {
                    find.HasUpdate = true;
                    _orderList.Add(find);
                }
            }
            foreach (var _model in _localList)
            {
                _orderList.Add(_model);
            }

            m_DataModel.ContactInfos = new ObservableCollection<ContactInfoDataModel>(_orderList);
            RefreshRelationCellIndex();
        }

        private void RefreshChooseMsg(PlayerHeadInfoMsg info)
        {
            m_DataModel.SelectContact.HasUpdate = true;
            CheckRefreshMsg(m_DataModel.SelectContact,info.Name, info.Level, info.Ladder);
        }

        private void WriteChacheRecords()
        {
            foreach (var _listCach in m_unWriteListCaches)
            {
                var _charId = _listCach.Key;
                var _contactInfo = GetRelationMsgData(_charId);
                if (_contactInfo == null)
                {
                    continue;
                }
                SaveChatRecordList(_contactInfo, _listCach.Value);
            }
            m_unWriteListCaches.Clear();
        }
        private void OnClickFriendBtn(IEvent ievent)
        {
            FriendBtnEvent e = ievent as FriendBtnEvent;
            if (e == null)
                return;
            UpdateListByType(e.Type);
            lastGuid = 0;
            if (e.Type == 0)
            {
                if (m_DataModel.ContactInfos.Count > 0)
                {
                    lastGuid = m_DataModel.ContactInfos[0].CharacterId;
                }
            }
            else
            {
                var infos = GetFriMsg(e.Type);
                if (e.Type == 2)
                {
                    PlayerDataManager.Instance.NoticeData.FriendNewEnemy = 0;
                    PlayerDataManager.Instance.NoticeData.NewEnemy = 0;
                }
                else if (e.Type == 1)
                {
                    //PlayerDataManager.Instance.NoticeData.FriendChatUnRead = 0;
                }
                if (infos.Count > 0)
                {
                    lastGuid = infos[0].Guid;
                }
            }
            if (lastGuid > 0)
            {
                ChoosePartnerRelationCell(lastGuid);
            }

        }

        private void OnAddRelation(IEvent ievent)
        {
            AddRelationEvent e = ievent as AddRelationEvent;
            if (e != null)
            {
                var _d = e.data;
                SetRelationChatMsg(_d.CharacterId, _d.CharacterName, _d.Ladder, _d.Level, _d.RoleId);
                UpdateListByType((int)eTab.Temp);
            }
        }

        private bool isRes = true;
        private void UpdateListByType(int Type = -1)
        {
            PlayerDataManager.Instance.NoticeData.FriendTabType = Type;
            if (Type >= 0)
                m_DataModel.SelectToggle = Type;
            for (int i = 0; i < m_DataModel.IsSelectTab.Count; i++)
            {
                m_DataModel.IsSelectTab[i] = i == Type;
            }
            m_DataModel.CurInfos.Clear();
            if (m_DataModel.ContactInfos.Count > 0)
            {
                if (isRes && m_DataModel.FriendInfos.Count != 0)
                {
                    foreach (var v in m_DataModel.FriendInfos)
                    {
                        UpdateContacts(v);

                    }
                }

            }
            switch (Type)
            {

                case (int)eTab.Temp:
                    {

                        //if (isRes && m_DataModel.FriendInfos.Count != 0)
                        //{
                        //    foreach (var v in m_DataModel.FriendInfos)
                        //    {
                        //        UpdateContacts(v);

                        //    }
                        //}
                        //else
                        {
                           // SortContactFriend(m_DataModel.ContactInfos);
                            foreach (var v in m_DataModel.ContactInfos)
                            {
                                AddContact(v);
                            }
                        }
                    }
                    break;
                case (int)eTab.Friend://
                {
                    SortFriend(m_DataModel.FriendInfos);
                        foreach (var v in m_DataModel.FriendInfos)
                        {
                            v.FromType = Type;
                            AddFriend(v);
                        }
                    }
                    break;
                case (int)eTab.Enemy:
                    {
                        SortFriend(m_DataModel.EnemyInfos);
                        foreach (var v in m_DataModel.EnemyInfos)
                        {
                            v.FromType = Type;
                            AddFriend(v);
                        }
                    }
                    break;
                case (int)eTab.Black://黑名单
                    {
                        SortFriend(m_DataModel.BlackInfos);
                        foreach (var v in m_DataModel.BlackInfos)
                        {
                            v.FromType = Type;
                            AddFriend(v);
                        }
                    }
                    break;
            }
        }

        void SortFriend(ObservableCollection<FriendInfoDataModel> list)
        {
            ObservableCollection<FriendInfoDataModel> online = new ObservableCollection<FriendInfoDataModel>();
            ObservableCollection<FriendInfoDataModel> outline = new ObservableCollection<FriendInfoDataModel>();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsOnline == 0)
                    outline.Add(list[i]);
                else
                    online.Add(list[i]);
            }
            list.Clear();
            for (int i = 0; i < online.Count - 1; i++)
            {
                for (int j = 0; j < online.Count - i - 1; j++)
                {
                    if (online[j].Level < online[j + 1].Level)
                    {
                        var temp = online[j];
                        online[j] = online[j + 1];
                        online[j + 1] = temp;
                    }
                }
            }
            for (int i = 0; i < online.Count; i++)
            {
                list.Add(online[i]);
            }
            for (int i = 0; i < outline.Count - 1; i++)
            {
                for (int j = 0; j < outline.Count - i - 1; j++)
                {
                    if (outline[j].Level < outline[j + 1].Level)
                    {
                        var temp = outline[j];
                        outline[j] = outline[j + 1];
                        outline[j + 1] = temp;
                    }
                }
            }
            for (int i = 0; i < outline.Count; i++)
            {
                list.Add(outline[i]);
            }

        }
        void SortContactFriend(ObservableCollection<ContactInfoDataModel> list)
        {
            ObservableCollection<ContactInfoDataModel> online = new ObservableCollection<ContactInfoDataModel>();
            ObservableCollection<ContactInfoDataModel> outline = new ObservableCollection<ContactInfoDataModel>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsOnline == 0)
                    outline.Add(list[i]);
                else
                    online.Add(list[i]);
            }
            list.Clear();
            for (int i = 0; i < online.Count - 1; i++)
            {
                for (int j = 0; j < online.Count - i - 1; j++)
                {
                    if (online[j].Level < online[j + 1].Level)
                    {
                        var temp = online[j];
                        online[j] = online[j + 1];
                        online[j + 1] = temp;
                    }
                }
            }
            for (int i = 0; i < online.Count; i++)
            {
                list.Add(online[i]);
            }
            for (int i = 0; i < outline.Count - 1; i++)
            {
                for (int j = 0; j < outline.Count - i - 1; j++)
                {
                    if (outline[j].Level < outline[j + 1].Level)
                    {
                        var temp = outline[j];
                        outline[j] = outline[j + 1];
                        outline[j + 1] = temp;
                    }
                }
            }
            for (int i = 0; i < outline.Count; i++)
            {
                list.Add(outline[i]);
            }

        }
        private void AddFriend(FriendInfoDataModel data)
        {
            m_DataModel.CurInfos.Add(data);
        }

        private void AddContact(ContactInfoDataModel data)
        {
            FriendInfoDataModel _new = new FriendInfoDataModel();
            _new.FromType = (int)eTab.Temp;
            _new.Guid = data.CharacterId;
            _new.Name = data.Name;
            _new.Level = data.Level;
            _new.TypeId = data.Type;
            _new.Ladder = data.Ladder;
            _new.IsOnline = -1;
            _new.Vip = data.Vip;
            _new.StarNum = data.StarNum;
            _new.UnreadNum = data.UnreadNum;
            m_DataModel.CurInfos.Add(_new);

        }
        private void UpdateContacts(FriendInfoDataModel info)
        {
            foreach (var v in m_DataModel.ContactInfos)
            {
                if (v.CharacterId == info.Guid)
                {

                    m_DataModel.ContactInfos.Remove(v);
                    break;
                }
            }
            ContactInfoDataModel con = new ContactInfoDataModel();
            con.CharacterId = info.Guid;
            con.Level = info.Level;
            con.Ladder = info.Ladder;
            con.Name = info.Name;
            con.Type = info.TypeId;
            con.IsOnline = -1;
            con.Vip = info.Vip;
            con.StarNum = info.StarNum;
            con.UnreadNum = info.UnreadNum;
            m_DataModel.ContactInfos.Add(con);
        }
    }
}