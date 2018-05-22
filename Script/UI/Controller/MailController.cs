
/********************************************************************************* 

                         Scorpion




  *FileName:MailController

  *Version:1.0

  *Date:2017-06-28

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class PostFrameCtrler : IControllerBase
    {
        #region 静态变量
        private static readonly MailCellData EmptyMailCellData = new MailCellData
        {
            InfoData = new MailInfoData()
        };
        #endregion

        #region 成员变量
        private bool m_Init;
        private int Tab { get; set; }
        private readonly MailCellDataComplarer m_mailComparer = new MailCellDataComplarer();
        private MailDataModel DataModel { get; set; }
        #endregion

        #region 构造函数
        public PostFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(MailSyncEvent.EVENT_TYPE, OnMailSyncEvent); //SC更新邮件
            EventDispatcher.Instance.AddEventListener(MailCellClickEvent.EVENT_TYPE, OnClickPostCellEvent); //邮件点击
            EventDispatcher.Instance.AddEventListener(MailOperactionEvent.EVENT_TYPE, OnOperaPostEvent); //邮件操作
            EventDispatcher.Instance.AddEventListener(Enter_Scene_Event.EVENT_TYPE, OnSwitchSceneEvent); //进入场景
            EventDispatcher.Instance.AddEventListener(SNSTabEvent.EVENT_TYPE, OnClickTab);
            EventDispatcher.Instance.AddEventListener(MailUIRefreshEvent.EVENT_TYPE, MailUIRefresh);
            EventDispatcher.Instance.AddEventListener(MailInfoClickEvent.EVENT_TYPE, OnMailInfoClickEvent);

            
        }
        #endregion

        #region 固有函数
        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void CleanUp()
        {
            DataModel = new MailDataModel();
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "AddMailData")
            {
                AddPostData(param[0] as List<MailCell>, (bool)param[1]);
            }

            return null;
        }
        private bool isShowOldMailNothing = false;
        public void OnShow()
        {

        }
        private void MailUIRefresh(IEvent ieve)
        {
            isShowOldMailNothing = true;
            if (DataModel.MailCells.Count == 1 && DataModel.MailCells[0].State != (int)MailStateType.OldMailHave)
            {
                PlayerDataManager.Instance.NoticeData.MailCount = 0;
                PlayerDataManager.Instance.NoticeData.MailNew = false;
            }
        }
        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            //if (DataModel.SelectData.Id == ulong.MaxValue)
            //{
            if (MailCells.Count > 0)
            {
                MailCellData cellData = null;
                cellData = GetCellDataByState(MailStateType.NewMailHave);
                if (cellData == null)
                {
                    cellData = GetCellDataByState(MailStateType.NewMail);
                }
                if (cellData == null)
                {
                    cellData = MailCells[0];
                    GetCellDataByStateIndex = 0;
                }
                DataModel.SelectData = cellData;
                cellData.IsClicked = 1;
                RequestMailData(GetCellDataByStateIndex != -1 ? GetCellDataByStateIndex : 0);

                OnCheckAllChoose(true);
            }
            //}     
        }

        private int GetCellDataByStateIndex = -1;
        private MailCellData GetCellDataByState(MailStateType state)
        {
            for (int i = 0; i < DataModel.MailCells.Count; i++)
            {
                if (DataModel.MailCells[i].State == (int)state)
                {
                    GetCellDataByStateIndex = i;
                    return DataModel.MailCells[i];
                }
            }
            return null;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件

        private void OnClickTab(IEvent iEvent)
        {
            SNSTabEvent e = iEvent as SNSTabEvent;
            if (e != null && e.index > 0)
            {//1邮件 2反馈
                Tab = e.index;
                if (Tab == 2)
                    DataModel.IsSelect = 0;
            }
        }
        //切换场景判断邮件是否快满
        private void OnSwitchSceneEvent(IEvent iEvent)
        {
            WarnMailNum();
        }
        //单邮件点击事件
        private void OnClickPostCellEvent(IEvent ievent)
        {
            var _e = ievent as MailCellClickEvent;
            var _index = _e.Index;
            if (_e.Type == 1)
            {
                RequestMailData(_index);
            }
            else if (_e.Type == 2)
            {
                OnMailCellCheck(_index, _e.Value);
            }
        }

        //邮件操作事件
        private void OnOperaPostEvent(IEvent ievent)
        {
            var _e = ievent as MailOperactionEvent;
            switch (_e.Type)
            {
                case 1:
                    {
                        GetMail();
                    }
                    break;
                case 2:
                    {
                        GetMails();
                    }
                    break;
                case 3:
                    {
                        CheckMails();
                    }
                    break;
                case 4:
                    {
                        OnCheckAllChoose(true);
                    }
                    break;
                case 5:
                    {
                        OnCheckAllChoose(false);
                    }
                    break;
                case 6:
                    {
                        CheckOneMail();
                    }
                    break;
                case 7:
                    {//点击问题反馈
                        DataModel.IsSelect = 0;
                    }
                    break;
                case 8:
                    {
                        OnSendQuestion();
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnMailInfoClickEvent(IEvent ievent)
        {
            MailInfoClickEvent evt = ievent as MailInfoClickEvent;
            if (evt.vec != null && DataModel.SelectData != null && DataModel.SelectData.InfoData.characterId > 0)
            {
                UIConfig.OperationList.Loction = evt.vec;
                PlayerDataManager.Instance.ShowCharacterPopMenu(DataModel.SelectData.InfoData.characterId, DataModel.SelectData.InfoData.characterName, 10);
            }
        }
        //SC邮件更新
        private void OnMailSyncEvent(IEvent ievent)
        {
            if (PlayerDataManager.Instance.GetLoginApplyState().GetFlag((int)eLoginApplyType.Mail) == 0)
                return;
            var _e = ievent as MailSyncEvent;
            var _mails = _e.List;
            AddPostData(_mails.Mails, false);

            for (int i = 0; i < _mails.Mails.Count; i++)
            {
                PlatformHelper.UMEvent("Mail", "GetMail", _mails.Mails[i].Name);
            }
        }
        #endregion







        //更新邮件信息
        private void AddPostData(List<MailCell> mails, bool clean)
        {
            var _list = new List<MailCellData>();
            var _listQA = new List<MailCellData>();
            if (clean)
            {
            }
            else
            {
                _list.AddRange(DataModel.MailCells);
                _listQA.AddRange(DataModel.QAMailCells);
            }
            var _mailCells = _list;
            {
                var __list3 = mails;
                var __listCount3 = __list3.Count;
                for (var __i3 = 0; __i3 < __listCount3; ++__i3)
                {
                    var _mail = __list3[__i3];
                    {
                        var _cellData = new MailCellData
                        {
                            Id = _mail.Guid,
                            IsApply = false,
                            Name = _mail.Name,
                            DateTime = Extension.FromServerBinary(_mail.StartTime),
                            State = _mail.State,
                            MailType = _mail.Type
                        };
                        if (_mail.Type == 0)
                        {
                            if (_mailCells.Contains(_cellData, m_mailComparer))
                            {
                                _mailCells.Remove(_cellData);
                                clean = true;
                            }
                            _mailCells.Add(_cellData);
                        }
                        else
                        {
                            if (_listQA.Contains(_cellData, m_mailComparer))
                            {
                                _listQA.Remove(_cellData);
                            }
                            _listQA.Add(_cellData);
                        }
                    }
                }
            }
            _list.Sort();
            _listQA.Sort();
            DataModel.MailCells = new ObservableCollection<MailCellData>(_list);
            DataModel.QAMailCells = new ObservableCollection<MailCellData>(_listQA);

            DataModel.CellCount = _list.Count;
            DataModel.QACellCount = _listQA.Count;

            if (CellCount == 1)
            {
                RequestMailData(0);
            }
            RenewalNotice();

            DataModel.IsSelectAll = false;
        }

        //更新是否有邮件提示
        private void RenewalNotice()
        {
            if (DataModel.MailCells.Count == 0)
            {
                DataModel.IsEmpty = true;
            }
            else
            {
                DataModel.IsEmpty = false;
            }

            var _hasNew = false;
            var _itemCount = 0;
            {
                // foreach(var mail in DataModel.MailCells)
                var __enumerator12 = (DataModel.MailCells).GetEnumerator();
                while (__enumerator12.MoveNext())
                {
                    var _mail = __enumerator12.Current;
                    {
                        if (_mail.State == (int)MailStateType.NewMail || _mail.State == (int)MailStateType.NewMailHave ||
                            _mail.State == (int)MailStateType.NewMailNothing ||
                            _mail.State == (int)MailStateType.OldMailHave)
                        {
                            _hasNew = true;
                        }
                        if (_mail.State == (int)MailStateType.NewMailHave || _mail.State == (int)MailStateType.OldMailHave ||
                            _mail.State == (int)MailStateType.NewMail || _mail.State == (int)MailStateType.NewMailNothing)
                        {
                            _itemCount++;
                        }
                    }
                }
            }
            if (!isShowOldMailNothing && DataModel.MailCells.Count == 1)
            {
                _hasNew = true;
                _itemCount = 1;
            }
            PlayerDataManager.Instance.NoticeData.MailCount = _itemCount;
            PlayerDataManager.Instance.NoticeData.MailNew = _hasNew;
        }

        //请求邮件
        private IEnumerator ApplyMailsCoroutine(MailCellData data)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyMailInfo(data.Id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _mailInfo = _msg.Response;
                        data.State = _mailInfo.State;
                        var _content = GameUtils.ConvertChatContent(_mailInfo.Text);
                        data.InfoData.Content = _content.Replace("\\n", "\n");
                        if (!string.IsNullOrEmpty(_mailInfo.Send))
                        {
                            data.InfoData.Sender = _mailInfo.Send;
                        }
                        else
                        {
                            data.InfoData.Sender = GameUtils.GetDictionaryText(9000);
                        }
                        data.InfoData.characterId = 0;
                        data.InfoData.characterName = "";
                        if (_mailInfo.ExtendType == (int) SendToCharacterMailType.BeKillInfo)
                        {
                            data.InfoData.characterId = ulong.Parse(_mailInfo.ExtendPara0);
                            data.InfoData.characterName = _mailInfo.ExtendPara1;
                        }
                        var _index = 0;
                        //if (data.IsReceive != 1)
                        //{
                        {
                            var __list9 = _mailInfo.Items;
                            var __listCount9 = __list9.Count;
                            for (var __i9 = 0; __i9 < __listCount9; ++__i9)
                            {
                                var i = __list9[__i9];
                                {
                                    data.InfoData.Items[_index].ItemId = i.ItemId;
                                    data.InfoData.Items[_index].Count = i.Count;
                                    data.InfoData.Items[_index].Exdata.InstallData(i.Exdata);
                                    _index++;
                                }
                            }
                        }
                        //}
                        //for (var _i = _index; _i < 5; _i++)
                        //{
                        //    data.InfoData.Items[_i].Reset();
                        //}
                        DataModel.SelectData.IsApply = true;
                        RenewalNotice();
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_MailNotFind)
                    {
                        //邮件没有找到
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("ApplyMailInfo Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ApplyMailInfo Error!............State..." + _msg.State);
                }
            }
        }
        private void CheckOneMail()
        {
            var _data = DataModel.SelectData;
            if (null == _data.Name)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(422));
                return;
            }
            var _isAttach = false;
            var _onemial = new Uint64Array();
            _onemial.Items.Add(_data.Id);
            if (_onemial.Items.Count == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(422));
                return;
            }
            if (_isAttach)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 417, "", DeleteOneMail);
                return;
            }
            DeleteOneMail();
        }
        private void DeleteOneMail()
        {
            var _data = DataModel.SelectData;
            if (null == _data.Name)
            {
                return;
            }
            var _onemial = new Uint64Array();
            _onemial.Items.Add(_data.Id);
            if (_onemial.Items.Count == 0)
            {
                return;
            }
            NetManager.Instance.StartCoroutine(CheckMailsCoroutine(_onemial));
        }
        //查看邮件
        private void CheckMails()
        {
            var _isAttach = false;
            var _mials = new Uint64Array();
            {
                // foreach(var cell in DataModel.MailCells)
                var __enumerator7 = (DataModel.MailCells).GetEnumerator();
                while (__enumerator7.MoveNext())
                {
                    var _cell = __enumerator7.Current;
                    {
                        if (_cell.IsSelect)
                        {
                            _mials.Items.Add(_cell.Id);
                            if (_cell.IsAttach)
                            {
                                _isAttach = true;
                            }
                        }
                    }
                }
            }
            if (_mials.Items.Count == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(422));
                return;
            }

            if (_isAttach)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 417, "", DeleteMails);
                return;
            }
            DeleteMails();
        }

        //删除邮件
        private void DeleteMails()
        {
            var _mials = new Uint64Array();
            {
                // foreach(var cell in DataModel.MailCells)
                var __enumerator8 = (DataModel.MailCells).GetEnumerator();
                while (__enumerator8.MoveNext())
                {
                    var _cell = __enumerator8.Current;
                    {
                        if (_cell.IsSelect)
                        {
                            _mials.Items.Add(_cell.Id);
                        }
                    }
                }
            }
            if (_mials.Items.Count == 0)
            {
                return;
            }
            NetManager.Instance.StartCoroutine(CheckMailsCoroutine(_mials));
        }

        //查看邮件
        private IEnumerator CheckMailsCoroutine(Uint64Array mails)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.DeleteMail(mails);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(418));
                        {
                            var _mailList = new List<MailCellData>(MailCells.ToArray());
                            var _list11 = mails.Items;
                            var _listCount11 = _list11.Count;
                            var _isReset = false;

                            for (var _i11 = 0; _i11 < _listCount11; ++_i11)
                            {
                                var _mail = _list11[_i11];
                                {
                                    var _data = GetMailCellData(_mail);

                                    PlatformHelper.UMEvent("Mail", "Delete", _data.Name);

                                    if (_data == DataModel.SelectData)
                                    {
                                        _isReset = true;
                                    }
                                    _data.Id = 0;
                                    _mailList.Remove(_data);

                                    DataModel.IsSelectAll = false;
                                }
                            }
                            isShowOldMailNothing = false;
                            MailCells = new ObservableCollection<MailCellData>(_mailList);
                            CellCount = MailCells.Count;

                            if (_isReset && CellCount > 0)
                            {
                                RequestMailData(0);
                            }
                            else
                            {
                                ResetChooseMailData();
                            }

                        }

                        RenewalNotice();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("ReceiveMail Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ReceiveMail Error!............State..." + _msg.State);
                }
            }
        }

        private MailCellData GetMailCellData(ulong id)
        {
            return MailCells.FirstOrDefault(cell => cell.Id == id);
        }

        private void Init()
        {
            m_Init = true;
            DataModel.MaxCount = GameUtils.MaxMailCount;
        }

        private ObservableCollection<MailCellData> MailCells
        {
            get
            {
                if (Tab == 2)
                    return DataModel.QAMailCells;
                else
                    return DataModel.MailCells;
            }
            set
            {
                if (Tab == 2)
                    DataModel.QAMailCells = value;
                else
                    DataModel.MailCells = value;
            }
        }

        private int CellCount
        {
            get
            {
                if (Tab == 2)
                    return DataModel.QACellCount;
                else
                    return DataModel.CellCount;
            }
            set
            {
                if (Tab == 2)
                    DataModel.QACellCount = value;
                else
                    DataModel.CellCount = value;
            }
        }

        //请求邮件信息
        private void RequestMailData(int index)
        {
            if (index >= MailCells.Count)
            {
                return;
            }
            var _cellData = MailCells[index];
            {
                // foreach(var cell in DataModel.MailCells)
                var __enumerator2 = (MailCells).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var _cell = __enumerator2.Current;
                    {
                        _cell.IsClicked = _cell == _cellData ? 1 : 0;
                    }
                }
            }
            DataModel.SelectData = _cellData;
            DataModel.IsSelect = 1;
            if (_cellData.IsApply)
            {
                return;
            }
            NetManager.Instance.StartCoroutine(ApplyMailsCoroutine(_cellData));
        }

        //checkbox变化调用
        private void OnChangeChooseAll(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelectAll")
            {
                var _isSelect = DataModel.IsSelectAll;
                {
                    // foreach(var cell in DataModel.MailCells)
                    var __enumerator1 = (DataModel.MailCells).GetEnumerator();
                    while (__enumerator1.MoveNext())
                    {
                        var cell = __enumerator1.Current;
                        {
                            cell.IsSelect = _isSelect;
                        }
                    }
                }
            }
        }

        private void OnCheckAllChoose(bool isSelect)
        {
            if (DataModel != null)
            {
                var __enumerator1 = (DataModel.MailCells).GetEnumerator();
                while (__enumerator1.MoveNext())
                {
                    var _cell = __enumerator1.Current;
                    {
                        _cell.IsSelect = isSelect;
                    }
                }

                DataModel.IsSelectAll = isSelect;
            }
        }

        private void OnSendQuestion()
        {
            if (PlayerDataManager.Instance.GetExData((int)(int)eExdataDefine.e750) <= 0)
            {
                // 反馈次数不足提示
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 300000148);
                return;
            }
            NetManager.Instance.StartCoroutine(SendQuestionCoroutine());
        }
        private IEnumerator SendQuestionCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                MailQuestion qa = new MailQuestion();
                qa.Guid = PlayerDataManager.Instance.GetGuid();
                qa.Name = PlayerDataManager.Instance.GetName();
                qa.Title = DataModel.InputName;
                qa.Text = DataModel.InputChat;
                if (string.IsNullOrEmpty(DataModel.InputName.Trim()))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100001395);
                    yield break;
                }
                if (string.IsNullOrEmpty(DataModel.InputChat.Trim()))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100001395);
                    yield break;
                }
                if (GameUtils.CheckSensitiveName(DataModel.InputName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100001393);
                    yield break;
                }
                if (GameUtils.CheckSensitiveName(DataModel.InputChat))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100001394);
                    yield break;
                }
                var _msg = NetManager.Instance.SendQuestion(qa);
                yield return _msg.SendAndWaitUntilDone();
                DataModel.InputChat = string.Empty;
                DataModel.InputName = string.Empty;
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 100001391);
            }
        }

        private void OnMailCellCheck(int index, int value)
        {
            if (index >= DataModel.MailCells.Count)
            {
                return;
            }
            var _cellData = DataModel.MailCells[index];

            _cellData.IsSelect = value == 1;

            if (_cellData.IsSelect == false)
            {
                if (DataModel.IsSelectAll)
                {
                    DataModel.IsSelectAll = false;
                }
            }
            else
            {
                if (DataModel.IsSelectAll == false)
                {
                    var _isAll = true;
                    foreach (var _cell in DataModel.MailCells)
                    {
                        if (_cell.IsSelect == false)
                        {
                            _isAll = false;
                            break;
                        }
                    }
                    if (_isAll)
                    {
                        DataModel.IsSelectAll = true;
                    }
                }
            }
        }

        //领取邮件
        private void GetMail()
        {
            var _data = DataModel.SelectData;
            if (_data.Id == 0)
            {
                return;
            }
            if (_data.IsReceive == 1)
            {
                //邮件已经领取
                var _e = new ShowUIHintBoard(3200001);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            if (_data.IsAttach == false)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(419));
                return;
            }
            var _mials = new Uint64Array();
            _mials.Items.Add(_data.Id);
            NetManager.Instance.StartCoroutine(GetMailsCoroutine(_mials));
        }

        //批量领取邮件
        private void GetMails()
        {
            var _mials = new Uint64Array();
            var _isCheck = false;
            {
                // foreach(var cell in DataModel.MailCells)
                var __enumerator6 = (DataModel.MailCells).GetEnumerator();
                while (__enumerator6.MoveNext())
                {
                    var _cell = __enumerator6.Current;
                    {
                        if (_cell.IsSelect)
                        {
                            _isCheck = true;
                            //if (_cell.IsAttach)
                            //{
                            _mials.Items.Add(_cell.Id);
                            //}
                        }
                    }
                }
            }

            if (_isCheck == false)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(421));
                return;
            }
            if (_mials.Items.Count == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(419));
                return;
            }
            NetManager.Instance.StartCoroutine(GetMailsCoroutine(_mials));
        }

        //领取邮件
        private IEnumerator GetMailsCoroutine(Uint64Array mails)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ReceiveMail(mails);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //DataModel.IsSelectAll = false;
                        var _receieve = _msg.Response;
                        if (_receieve == 0)
                        {
                            //您包裹已满！
                            var _e = new ShowUIHintBoard(302);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            yield break;
                        }
                        if (mails.Items.Count == _receieve)
                        {
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(420));
                        }
                        else
                        {
                            //背包已满，不能领取全部邮件
                            GameUtils.ShowHintTip(3200006);
                        }

                        for (var i = 0; i < _receieve; i++)
                        {
                            var _id = mails.Items[i];
                            var _data = GetMailCellData(_id);
                            _data.State = 2;
                            if (_data.IsApply)
                            {
                                {
                                    // foreach(var item in data.InfoData.Items)
                                    var __enumerator10 = (_data.InfoData.Items).GetEnumerator();
                                    while (__enumerator10.MoveNext())
                                    {
                                        var _item = __enumerator10.Current;
                                        {
                                            //_item.ItemId = -1;
                                            //_item.Count = 0;
                                        }
                                    }
                                }
                            }
                            PlatformHelper.UMEvent("Mail", "GetItem", _data.Name);
                        }
                        RenewalNotice();
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_MailNotFind
                             || _msg.ErrorCode == (int)ErrorCodes.Error_MailReceiveOver)
                    {
                        //邮件没有找到
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        Logger.Error("ReceiveMail Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ReceiveMail Error!............State..." + _msg.State);
                }
            }
        }

        //情况邮件箱
        private void ResetChooseMailData()
        {
            DataModel.SelectData = EmptyMailCellData;
            DataModel.IsSelect = 0;
        }

        //邮件警告数量45，50分别提示
        private void WarnMailNum()
        {
            if (m_Init == false)
            {
                Init();
            }
            var _count = DataModel.MailCells.Count;
            if (_count >= DataModel.MaxCount)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 416, "",
                    () =>
                    {
                        FriendArguments arg = new FriendArguments();
                        arg.Tab = 1;
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SNSFrameUI, arg));
                    });
            }
            else if (_count > DataModel.MaxCount - 5)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 415, "",
                    () =>
                    {
                        FriendArguments arg = new FriendArguments();
                        arg.Tab = 1;
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SNSFrameUI, arg));
                       
                    });
            }
        }


    }
}