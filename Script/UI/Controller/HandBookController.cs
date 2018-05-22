
/********************************************************************************* 

                         Scorpion




  *FileName:HandBookController

  *Version:1.0

  *Date:2017-06-15

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
using ObjCommand;
using ScorpionNetLib;
using Shared;

#endregion

namespace ScriptController
{
    public class BesideInstructionFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        //悬赏cache
        private Dictionary<int, List<int>> m_bountyDic = new Dictionary<int, List<int>>();
        //bookbuffer
        private List<HandBookItemDataModel> m_BookBufList = new List<HandBookItemDataModel>();
        private List<HandBookItemDataModel> m_HasList = new List<HandBookItemDataModel>();
        private Dictionary<int, HandBookItemDataModel> m_bookCacheDictory = new Dictionary<int, HandBookItemDataModel>();
        //兑换数据
        private Dictionary<int, ItemComposeRecord> m_composeTableDictory = new Dictionary<int, ItemComposeRecord>();
        private HandBookDataModel m_BesideInstructionDataModel;
        private bool m_usingItem;
        private int FightId { get { return PlayerDataManager.Instance.mFightBook; } set{ PlayerDataManager.Instance.mFightBook = value;} }
        #endregion
        //当前参数缓存
        private HandBookArguments mCurrentHandBookArg;
        #region 构造函数
        public BesideInstructionFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(HankBookItemData_Event.EVENT_TYPE, OnChangeItemDataEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_ComposeBookPiece.EVENT_TYPE,
                OnComposeInstrucPieceClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnBookClick.EVENT_TYPE, ShowInstrucMsg);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnBookItemClick.EVENT_TYPE, OnBookItemClick);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnBookClickGet.EVENT_TYPE, OnBookItemGetClick);

        
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnGetBookClick.EVENT_TYPE, OnGetInstrucClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_ComposeBookPieceFromBookInfo.EVENT_TYPE,
                OnBookMsgToComposeBookPieceEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnBookGroupToggled.EVENT_TYPE,
                OnInstrucTeamToggledEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnBountyGroupToggled.EVENT_TYPE,
                OnBountyTeamToggledEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnGroupBookActive.EVENT_TYPE,
                OnInstrucActiveInTeamEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnBountyBookActive.EVENT_TYPE,
                OnBountyInstrucActiveEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_ComposeBookCardFromBookInfo.EVENT_TYPE,
                OnBookMsgToComposeBookCardEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnFightClick.EVENT_TYPE,
                OnBookMsgToFightEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnLevelupClick.EVENT_TYPE,
                OnBookMsgToLevelupEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_OnSummonMonster.EVENT_TYPE, OnSummonMonster);
            EventDispatcher.Instance.AddEventListener(UIEvent_OnClickHasCell.EVENT_TYPE, OnClickHasCell);
        
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_BesideInstructionDataModel = new HandBookDataModel();

            Table.ForeachItemCompose(table =>
            {
                if (table.Type != 0)
                {
                    return true;
                }
                if (!m_composeTableDictory.ContainsKey(table.ComposeView))
                {
                    m_composeTableDictory.Add(table.ComposeView, table);
                }
                return true;
            });

            m_bountyDic.Clear();
            m_BookBufList.Clear();
            Table.ForeachHandBook(table =>
            {
                var _book = MakeOneInstrucFromTable(table);
                _book.Index = m_BesideInstructionDataModel.Books.Count;
                m_BesideInstructionDataModel.Books.Add(_book);
                m_BookBufList.Add(_book);
                return true;
            });

            m_bookCacheDictory.Clear();
            var _books = m_BesideInstructionDataModel.Books;
            for (var i = 0; i < _books.Count; i++)
            {
                m_bookCacheDictory.Add(_books[i].BookId, _books[i]);
            }

            //分组界面数据
            Table.ForeachBookGroup(group =>
            {
                var _bookGroup = MakeOneInstrucGrop(group, group.Id);
                InitGroupA(group.GroupId,group.Desc);
                m_BesideInstructionDataModel.GropBooks[group.GroupId-1].GropBooks.Add(_bookGroup);
                return true;
            });

            //悬赏界面数据
            m_BesideInstructionDataModel.MonsterBounty.Clear();
            var _bountyTableCount = 0;
            {
                var _enumerator = m_bountyDic.GetEnumerator();
                while (_enumerator.MoveNext())
                {
                    var _pair = _enumerator.Current;
                    var _oneGroupCount = 0;
                    var _bountyBookGroup = new BountyBookGropDataModel();
                    var _dicId = Table.GetHandBook(_pair.Value[0]).RewardGroupId;
                    _bountyBookGroup.BountyName = GameUtils.GetDictionaryText(_dicId);
                    {
                        var _count2 = _pair.Value.Count;
                        for (var j = 0; j < _count2; j++)
                        {
                            var _key = _pair.Value[j];
                            HandBookItemDataModel item;
                            var _bExist = m_bookCacheDictory.TryGetValue(_key, out item);
                            if (_bExist)
                            {
                                _bountyBookGroup.BountyGroupBooks.Add(item);
                                if (item.BountyActive == -1 && item.BookCount >= 1)
                                {
                                    _oneGroupCount++;
                                }
                            }
                        }
                    }
                    _bountyBookGroup.NoticeCount = _oneGroupCount;
                    m_BesideInstructionDataModel.MonsterBounty.Add(_bountyBookGroup);
                    if (_oneGroupCount >= 1)
                    {
                        _bountyTableCount += _oneGroupCount;
                    }
                }
            }
            PlayerDataManager.Instance.NoticeData.HandBookWanted = _bountyTableCount;

            m_BesideInstructionDataModel.SelectedGropBooks.GropBooks = m_BesideInstructionDataModel.GropBooks[0].GropBooks;
            m_BesideInstructionDataModel.GropBooks[0].IsShow = true;
            m_BesideInstructionDataModel.SelectedBookItem.Copy(_books[0]);
        
        }

        private void InitGroupA(int id,string dec)
        {
            if (id <= m_BesideInstructionDataModel.GropBooks.Count)
            {
                m_BesideInstructionDataModel.GropBooks[id-1].Dec = dec;
                return;
            }
            for (int i = m_BesideInstructionDataModel.GropBooks.Count; i < id; i++)
            {
                HandBookGropADataModel group = new HandBookGropADataModel();
                group.Index = id;
                group.Dec = dec;
                m_BesideInstructionDataModel.GropBooks.Add(group);
            }
        }
        public void OnChangeScene(int sceneId)
        {
        }


        private void OnChangeItemDataEvent(IEvent iEvent)
        {
            UpdateNotice();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "UpdateNotice")
            {
                UpdateNotice();
            }
            else if (name == "RefreshCount")
            {
                RefreshData(null);
            }

            return null;
        }

        public void OnShow()
        {
            if (0 != m_BesideInstructionDataModel.BookInfoShow)
            {
                m_BesideInstructionDataModel.BookInfoShow = 0;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_BesideInstructionDataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as HandBookArguments;
            mCurrentHandBookArg = _args;
            if (_args != null && _args.Tab != -1)
            {
                m_BesideInstructionDataModel.TabSelect1 = _args.Tab;
            }
            else
            {
                m_BesideInstructionDataModel.TabSelect1 = 0;
            }
            var _c0 = m_BesideInstructionDataModel.Books.Count;
            m_BesideInstructionDataModel.HasBooks.Clear();
            m_HasList.Clear();
            for (var i = 0; i < _c0; i++)
            {
                var _book = m_BesideInstructionDataModel.Books[i];
                _book.BountyActive = PlayerDataManager.Instance.GetBountyBookEnable(_book.BookId);


                if (_book.BountyActive > 1)
                {
                    _book.AttrList.Clear();
                    Dictionary<int,int> dic = new Dictionary<int, int>();
                    var tbBookbase = Table.GetBookBase(_book.BookId*100 + _book.BountyActive);
                    if (tbBookbase != null)
                    {
                        for (int j = 0; j < tbBookbase.AttrList.Length; j++)
                        {
                            int attr = tbBookbase.AddAttr*tbBookbase.AttrList[j]/10000;
                            if (attr <= 0)
                                continue;
                            var str = new StringDataModel();
                            str.str = string.Format("{0}: +{1}", ExpressionHelper.AttrName[j + 1], attr);
                            _book.AttrList.Add(str);
                            dic.modifyValue(j + 1, attr);
                        }
                    }

                    var talentData = new int[(int)eAttributeType.AttrCount];
                    var talentDataRef = new int[(int)eAttributeType.AttrCount];
                    PlayerDataManager.Instance.ElfAttrConvert(dic, talentData, talentDataRef);
                    _book.FightPoint = PlayerDataManager.Instance.GetElfFightPoint(talentData, talentDataRef, 1, -2);


                }
                if (_book.BountyActive > 0)
                {
                    _book.Battle = FightId == _book.BookId ? 1 : 0;
                    m_HasList.Add(_book);
                }
                


                _book.BookCount = PlayerDataManager.Instance.GetItemTotalCount(_book.BookId).Count;
                var _table = Table.GetHandBook(_book.BookId);
                _book.BookPieceCount = PlayerDataManager.Instance.GetItemTotalCount(_table.PieceId).Count;

                if (_book.BountyActive < _table.ListCost.Count)
                {
                    //防止-1
                    int lv = _book.BountyActive >= 0 ? _book.BountyActive : 0;
                    _book.LevelupNeed = _table.ListCost[lv];
                
                }
                else
                    _book.LevelupNeed = -1;
                if (_book.BookPieceCount == 0 || _book.LevelupNeed == -1)
                {
                    _book.Composeable = 0;
                }
                else
                {
                    _book.Composeable = (float)_book.BookPieceCount / _book.LevelupNeed;
                }
            }
            m_BookBufList.Sort();
            m_HasList.Sort(RefreshListSort);
            var hasBook = new ObservableCollection<HandBookItemDataModel>();
            for (int i = 0; i < m_HasList.Count; i++)
            {          
                hasBook.Add(m_HasList[i]);                        
            }
            m_BesideInstructionDataModel.HasBooks = hasBook;

            m_BesideInstructionDataModel.Books = new ObservableCollection<HandBookItemDataModel>();
            for (var i = 0; i < _c0; i++)
            {
                m_BookBufList[i].Index = m_BesideInstructionDataModel.Books.Count;
                m_BesideInstructionDataModel.Books.Add(m_BookBufList[i]);
            }

            if (State == FrameState.Open)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_RestScrollViewPostion());
            }

            RenewNoticeData();
            m_usingItem = false;

            m_BesideInstructionDataModel.SelectedBookItem.Copy(m_BesideInstructionDataModel.Books[0]);
            CalcBookAttr();
            if (FightId > 0)
            {
                RefreshMonsterModel(FightId);            
            }
            else if (m_BesideInstructionDataModel.HasBooks.Count > 0)
            {
                RefreshMonsterModel(m_BesideInstructionDataModel.HasBooks[0].BookId);            
            }
            if (m_BesideInstructionDataModel.HasBooks.Count > 0)
            {
                m_BesideInstructionDataModel.IsShowFight = true;
            }
            else
            {
                m_BesideInstructionDataModel.IsShowFight = false;
            }
        }

        private void UpdateHasBook()
        {
            m_BesideInstructionDataModel.HasBooks.Clear();
            m_HasList.Clear();
            foreach (var _book in m_BesideInstructionDataModel.Books)
            {
                if (_book.BountyActive > 0)
                {
                    _book.Battle = FightId == _book.BookId ? 1 : 0;
                    if(_book.Battle == 1)
                        m_BesideInstructionDataModel.FightBookItem.Copy(_book);
                    m_HasList.Add(_book);                

                }
            }

            m_HasList.Sort(RefreshListSort);
            var hasBook = new ObservableCollection<HandBookItemDataModel>();
            for (int i = 0; i < m_HasList.Count; i++)
            {
                hasBook.Add(m_HasList[i]);                        
            }
            m_BesideInstructionDataModel.HasBooks = hasBook;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件

        private void OnInstrucActiveInTeamEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnGroupBookActive;
            var _BookItemDataModel = _e.DataModel;
            var _index = _e.index;
            var _id = _BookItemDataModel.BookId;
            //var _groupId = m_BesideInstructionDataModel.SelectedGropBooks.GropId;
            //NetManager.Instance.StartCoroutine(ActivateInstruction(_BookItemDataModel, _id, _groupId, _index));
        }

        private void OnInstrucTeamToggledEvent(IEvent ievent)
        {//重置组信息
            var _e = ievent as UIEvent_HandBookFrame_OnBookGroupToggled;
            int n = m_BesideInstructionDataModel.SelectedGropBooks.Index;
            m_BesideInstructionDataModel.GropBooks[n].IsShow = false;
            m_BesideInstructionDataModel.GropBooks[_e.Index].IsShow = true;

            m_BesideInstructionDataModel.SelectedGropBooks.GropBooks = m_BesideInstructionDataModel.GropBooks[_e.Index].GropBooks;
            m_BesideInstructionDataModel.SelectedGropBooks.Index = _e.Index;
        }

        private void OnBountyInstrucActiveEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnBountyBookActive;
//        _e.DataModel.LevelupNeed
            var _table = Table.GetHandBook(_e.DataModel.BookId);
            if (_e.DataModel.LevelupNeed > PlayerDataManager.Instance.GetItemTotalCount(_table.PieceId).Count)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnBookClick(_e.DataModel));
            }
            else
            {
                NetManager.Instance.StartCoroutine(BountyInstrucActive(_e.DataModel));            
            }
        }

        private void OnBountyTeamToggledEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnBountyGroupToggled;
            m_BesideInstructionDataModel.selectedBountyGroup = m_BesideInstructionDataModel.MonsterBounty[_e.Index];
        }

        private void OnBookMsgToComposeBookCardEvent(IEvent ievent)
        {
            var _selectBook = m_BesideInstructionDataModel.SelectedBook[0];
            var _castBook = m_BesideInstructionDataModel.SelectedBook[1];
            var _gold = PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes);
            if (_selectBook.BookUpgradeRequestCast > _gold)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                return;
            }
            if (_selectBook.UpGradeRequestCount > _castBook.BookCount)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                return;
            }

            NetManager.Instance.StartCoroutine(ComstituteInstrucCard(_selectBook.BookComposeTableId));
        }

        private void OnBookMsgToFightEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnFightClick;
            NetManager.Instance.StartCoroutine(SetBookFight(_e.Id));
        }

        private void OnSummonMonster(IEvent ievent)
        {
            if (m_BesideInstructionDataModel.FightBookItem.BookId <= 0)
                return;
            var tb = Table.GetHandBook(m_BesideInstructionDataModel.FightBookItem.BookId);
            if (tb!=null)
            {
                GameUtils.SummonMonster(tb.NpcId);                    
            }
        }

        private void OnClickHasCell(IEvent ievent)
        {
            UIEvent_OnClickHasCell e = ievent as UIEvent_OnClickHasCell;
            if (e != null)
            {
                if (e.bookId == m_BesideInstructionDataModel.FightBookItem.BookId)
                    return;
                foreach (var v in m_BesideInstructionDataModel.HasBooks)
                {
                    v.Selected = v.BookId == e.bookId ? 1 : 0;
                    if (v.Selected == 1)
                    {
                        m_BesideInstructionDataModel.FightBookItem.Copy(v);
                        EventDispatcher.Instance.DispatchEvent(new HandbookRefreshMonster_Event(e.bookId));
                    }
                }            
            }

        }
        private void OnBookMsgToLevelupEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnLevelupClick;
            var _table = Table.GetHandBook(_e.DataModel.BookId);
            if (_e.DataModel.LevelupNeed > PlayerDataManager.Instance.GetItemTotalCount(_table.PieceId).Count)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnBookClick(_e.DataModel));
            }
            else
            {
                NetManager.Instance.StartCoroutine(BountyInstrucActive(_e.DataModel));
            }
        }

        private int RefreshListSort(HandBookItemDataModel a1, HandBookItemDataModel a2)
        {
            if (a1.Color.CompareTo(a2.Color) != 0)
                return -(a1.Color.CompareTo(a2.Color));
            else if (a1.BountyActive.CompareTo(a2.BountyActive) != 0)
                return -(a1.BountyActive.CompareTo(a2.BountyActive));       
            else
                return 1;
        }  

        private void RefreshMonsterModel(int id)
        {
            //HandbookRefreshMonster_Event
      
            foreach (var book in m_BesideInstructionDataModel.Books)
            {
                if (book.BookId == id)
                {
                    m_BesideInstructionDataModel.FightBookItem.Copy(book);
                    EventDispatcher.Instance.DispatchEvent(new HandbookRefreshMonster_Event(id));                
                }
                book.Selected = (book.BookId == id)?1:0;
            }
        
        }


        private void OnGetInstrucClickEvent(IEvent ievent)
        {
            if (m_BesideInstructionDataModel.BookInfoShow == 1)
            {
                var _itemData = m_BesideInstructionDataModel.SelectedBook[0];//m_BesideInstructionDataModel.SelectedBookItem;
                //21去打怪获取
                if (21 == _itemData.TrackType)
                {
                    ObjManager.Instance.MyPlayer.LeaveAutoCombat();
                    GameControl.Executer.Stop();
                    var _command = GameControl.GoToCommand(_itemData.TrackParam[0], _itemData.TrackParam[1],
                        _itemData.TrackParam[2]);
                    var _command1 = new FuncCommand(() =>
                    {
                        GameControl.Instance.TargetObj = null;
                        ObjManager.Instance.MyPlayer.EnterAutoCombat();
                    });
                    GameControl.Executer.PushCommand(_command);
                    GameControl.Executer.PushCommand(_command1);                
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnBookClick(null));
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.HandBook));
                }
                else if (22 == _itemData.TrackType)
                {
                    var _e = new Show_UI_Event(UIConfig.DungeonUI);
                    EventDispatcher.Instance.DispatchEvent(_e);

                    GameUtils.GotoUiTab(25, _itemData.TrackParam[0]);
                }
                else if (23 == _itemData.TrackType)
                {
                    var _dicid = _itemData.TrackParam[0];
                    if (_dicid > 0)
                    {
                        var _ee = new ShowUIHintBoard(_dicid);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                    }
                }
                else if (23 == _itemData.TrackType)
                {
                    var _ee = new ShowUIHintBoard(_itemData.TrackParam[1]);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                }
                else if (24 == _itemData.TrackType)
                {
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame,
                        new RechargeFrameArguments { Tab = _itemData.TrackParam[1] }));
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.HandBook));
                }
            }
        }

        private void OnComposeInstrucPieceClickEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ConstituteInstrucPieceInfo());
        }

        private void OnBookMsgToComposeBookPieceEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ConstituteInstrucPieceInfo());
        }

        #endregion




        private IEnumerator ActivateInstruction(HandBookItemDataModel dataModel, int id, int groupId, int bitIndex)
        {
            //using (new BlockingLayerHelper(0))
            //{
            //    var _msg = NetManager.Instance.ActivateBook(id, groupId, bitIndex);
            //    yield return _msg.SendAndWaitUntilDone();


            //    if (_msg.State == MessageState.Reply)
            //    {
            //        if (_msg.ErrorCode == (int) ErrorCodes.OK)
            //        {
            //            //先修改playerdata里存储的dictionary
            //            PlayerDataManager.Instance.SetBookGroupEnable(groupId, bitIndex);

            //            var _groupDataModel = m_BesideInstructionDataModel.SelectedGropBooks;
            //            dataModel.BookCount--;
            //            // groupDataModel.BookEnable[bitIndex] = 1;
            //            _groupDataModel.GropBook[bitIndex].BookEnable = 1;
            //            _groupDataModel.GropCount++;
            //            if (_groupDataModel.GropCount >= _groupDataModel.GropMaxCount)
            //            {
            //                _groupDataModel.GropEnable = 1;
            //            }
            //            RenewNotice();
            //            var _ee = new ShowUIHintBoard(200011);
            //            EventDispatcher.Instance.DispatchEvent(_ee);
            //            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.HandBook);

            //            PlatformHelper.UMEvent("HandBook", "ActiveInGroup", id);
                   
            //        }
            //        else
            //        {
            //            UIManager.Instance.ShowNetError(_msg.ErrorCode);
            //        }
            //    }
            //}
            return null;
        }

        private void OnItemLevelUp(HandBookItemDataModel dataModel)
        {
            if (++dataModel.BountyActive <= 0)
            {
                dataModel.BountyActive ++;
            }
            dataModel.BookPieceCount -= dataModel.LevelupNeed;
            var tbHand = Table.GetHandBook(dataModel.BookId);
            if (tbHand != null)
            {
                if (dataModel.BountyActive < tbHand.ListCost.Count)
                    dataModel.LevelupNeed = tbHand.ListCost[dataModel.BountyActive];
                else
                    dataModel.LevelupNeed = -1;
            }
        }
        private  IEnumerator BountyInstrucActive(HandBookItemDataModel dataModel)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ActivateBook(dataModel.BookId);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.SetBountyBookEnable(dataModel.BookId);
                        dataModel.BookCount--;
                        OnItemLevelUp(dataModel);
                        RenewNotice();
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200011));
                        PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.HandBook);
                        PlatformHelper.UMEvent("HandBook", "Active", dataModel.BookId);
                        OnBookLevelUp(dataModel.BookId, dataModel.BountyActive);
                        if (dataModel.BookId == m_BesideInstructionDataModel.FightBookItem.BookId)
                        {
                            OnItemLevelUp(m_BesideInstructionDataModel.FightBookItem);
                        }
                        UpdateHasBook();
                        //UpdateNotice();
                        //激活或者升级成功后，展示下属性界面
                        //EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnBookClick(dataModel));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }
        private IEnumerator SetBookFight(int id)
        {
            if (id == -1)
                id = m_BesideInstructionDataModel.FightBookItem.BookId;
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.SetHandbookFight(id);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        FightId = id;
                        RefreshMonsterModel(id);
                        EventDispatcher.Instance.DispatchEvent(new MainUiRefreshSummonBtn());
                        UpdateHasBook();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }
        private IEnumerator ComstituteInstrucCard(int id)
        {
            using (new BlockingLayerHelper(0))
            {
                var _composeCount = 1;
                var _msg = NetManager.Instance.ComposeItem(id, _composeCount);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var _selectBook = m_BesideInstructionDataModel.SelectedBook[0];
                        var _castBook = m_BesideInstructionDataModel.SelectedBook[1];
                        _castBook.BookCount -= _selectBook.UpGradeRequestCount;
                        _selectBook.BookCount++;
                        HandBookItemDataModel book;
                        if (m_bookCacheDictory.TryGetValue(_castBook.BookId, out book))
                        {
                            book.BookCount = _castBook.BookCount;
                        }
                        else
                        {
                            Logger.Error("ComposeBookCard error, bookinfo: " + _castBook.BookId + "do not exist!");
                        }

                        if (m_bookCacheDictory.TryGetValue(_selectBook.BookId, out book))
                        {
                            book.BookCount = _selectBook.BookCount;
                        }
                        else
                        {
                            Logger.Error("ComposeBookCard error, bookinfo: " + _selectBook.BookId + "do not exist!");
                        }

                        RenewNotice();
                        var _ee = new ShowUIHintBoard(200010);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private IEnumerator ConstituteInstrucPieceInfo()
        {
            using (new BlockingLayerHelper(0))
            {
                var _book = m_BesideInstructionDataModel.Books[m_BesideInstructionDataModel.SelectedBookItem.Index];

                var _pieceId = Table.GetHandBook(_book.BookId).PieceId;
                var _pieceBag = PlayerDataManager.Instance.GetBag(2);
                var _bagIndex = -1;
                {
                    // foreach(var item in pieceBag.Items)
                    var __enumerator6 = (_pieceBag.Items).GetEnumerator();
                    while (__enumerator6.MoveNext())
                    {
                        var _item = __enumerator6.Current;
                        {
                            if (_pieceId == _item.ItemId)
                            {
                                _bagIndex = _item.Index;
                                break;
                            }
                        }
                    }
                }
                //检查碎片
                if (_bagIndex == -1)
                {
                    var _ee = new ShowUIHintBoard(200009);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    yield break;
                }

                m_usingItem = true;

                var _msg = NetManager.Instance.UseItem(2, _bagIndex, 1);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        if (m_usingItem)
                        {
                            var _bookTable = Table.GetHandBook(_book.BookId);
                            _book.BookCount++;
                            RenewNotice();

                            _book.BookPieceCount -= _bookTable.Count;
                            if (_book.LevelupNeed > 0)
                                _book.Composeable = (float) _book.BookPieceCount/_book.LevelupNeed;
                            else
                                _book.Composeable = 0;

                            m_BesideInstructionDataModel.SelectedBook[0].Copy(_book);
                            if (_book.Composeable < 1)
                            {
                                PlayerDataManager.Instance.NoticeData.HandBookCompose--;
                                ResetInstructions();
                            }
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                        var _ee = new ShowUIHintBoard(200010);
                        EventDispatcher.Instance.DispatchEvent(_ee);

                        PlatformHelper.UMEvent("HandBook", "Piece", _book.BookId);
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int) ErrorCodes.Error_SkillNoCD)
                        {
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                    }
                }
            }
        }

        private HandBookItemDataModel MakeOneInstrucFromTable(HandBookRecord table)
        {
            var _handbookitem = new HandBookItemDataModel();
            _handbookitem.BookId = table.Id;
            _handbookitem.BookSortId = table.ListSort;
            _handbookitem.ItemId = table.Id;
            _handbookitem.BookPieceCount = 0;
            _handbookitem.BookMaxCapacity = Table.GetItemBase(table.Id).MaxCount;
            if (table.NpcId != -1)
            {
                _handbookitem.MonsterName = table.Name;//Table.GetNpcBase(table.NpcId).Name;
                _handbookitem.MonsterLevel = Table.GetNpcBase(table.NpcId).Level;
            }
            _handbookitem.TrackType = table.TrackType;
            _handbookitem.locationName = table.TrackString;
            _handbookitem.TrackParam[0] = table.TrackParam[0];
            _handbookitem.TrackParam[1] = table.TrackParam[1];
            _handbookitem.TrackParam[2] = table.TrackParam[2];
            _handbookitem.BountyActive = PlayerDataManager.Instance.GetBountyBookEnable(table.Id);
            _handbookitem.AttrList.Clear();
            _handbookitem.Color = table.Color;
            var tbBookbase = Table.GetBookBase(table.Id * 100 + 1);
            if (tbBookbase != null)
            {
                Dictionary<int,int> dic = new Dictionary<int, int>();
                for (int i = 0; i < tbBookbase.AttrList.Length; i++)
                {
                    int attr = tbBookbase.AddAttr * tbBookbase.AttrList[i] / 10000;
                    if (attr <= 0)
                        continue;
                    var str = new StringDataModel();
                    str.str = string.Format("{0}: +{1}", ExpressionHelper.AttrName[i+1], attr);
                    _handbookitem.AttrList.Add(str);
                    dic.modifyValue(i + 1, attr);
                }
                var talentData = new int[(int)eAttributeType.AttrCount];
                var talentDataRef = new int[(int)eAttributeType.AttrCount];
                PlayerDataManager.Instance.ElfAttrConvert(dic, talentData, talentDataRef);
                _handbookitem.FightPoint = PlayerDataManager.Instance.GetElfFightPoint(talentData, talentDataRef, 1, -2);
            }

      



            _handbookitem.BountyMoney = table.Money;
            //-----bountybook data start------------------------------
            var _key = table.RewardGroupId;
            var _value = table.Id;
            List<int> output;
            if (m_bountyDic.TryGetValue(_key, out output))
            {
                output.Add(_value);
            }
            else
            {
                output = new List<int>();
                output.Add(_value);
                m_bountyDic.Add(_key, output);
            }
            //-----bountybook data end------------------------------

            //初始化合成属性
            ItemComposeRecord ComposeTable;
            var _bExist = m_composeTableDictory.TryGetValue(_handbookitem.BookId, out ComposeTable);
            if (_bExist)
            {
                _handbookitem.BookUpgradeRequestCast = ComposeTable.NeedValue;
                _handbookitem.UpGradeRequestBookId = ComposeTable.NeedId[0];
                _handbookitem.UpGradeRequestCount = ComposeTable.NeedCount[0];
                _handbookitem.BookComposeTableId = ComposeTable.Id;
            }
            return _handbookitem;
        }

        private HandBookGropDataModel MakeOneInstrucGrop(BookGroupRecord table, int GropId)
        {
            var _bookGrop = new HandBookGropDataModel();
            _bookGrop.GropId = table.Id;
            _bookGrop.GropName = table.Desc;
            var _maxCount = 0;
            var _count = 0;

            for (var i = 0; i < 6; i++)
            {
                var _id = table.ItemId[i];
                if (_id != -1)
                {
                    var _oneGroup = new OneGroupDataModel();
                    _oneGroup.Level = table.Level;
                    _maxCount++;
                    HandBookItemDataModel item;
                    var _bExist = m_bookCacheDictory.TryGetValue(_id, out item);
                    if (_bExist)
                    {
                        _oneGroup.book = item;
                        _oneGroup.BookEnable = 1;
                    }
               
                    _bookGrop.GropBook.Add(_oneGroup);
                }
            }

            _bookGrop.GropCount = _count;
            _bookGrop.GropMaxCount = _maxCount;
            _bookGrop.GropEnable = _count == _maxCount ? 1 : 0;
            for (var i = 0; i < 4; i++)
            {
                if (table.GroupAttrId[i] != -1)
                {
                    var _attrName = ExpressionHelper.AttrName[table.GroupAttrId[i]];
                    var _attrValue = table.GroupAttrValue[i];
                    _bookGrop.BookGropAttrInfo[i] = string.Format("{0}: +{1}", _attrName, _attrValue);
                }
                else
                {
                    _bookGrop.BookGropAttrInfo[i] = string.Empty;
                }
            }
            {
                // foreach(var oneGroupDataModel in bookGrop.GropBook)
                var __enumerator4 = (_bookGrop.GropBook).GetEnumerator();
                while (__enumerator4.MoveNext())
                {
                    var _oneGroupDataModel = __enumerator4.Current;
                    {
                        //if (_oneGroupDataModel.BookEnable != 1 && _oneGroupDataModel.book.BookCount >= 1)
                        if (_oneGroupDataModel.book.Composeable>0)
                        {
                            _bookGrop.NoticeCount++;
                        }
                    }
                }
            }
            return _bookGrop;
        }

   
        //刷新技能红点
        private void RenewNotice()
        {
            var _totalBountyCount = 0;
            var _totalGroupCount = 0;
            var _count = 0;
            {
                // foreach(var gropDataModel in mHandBookDataModel.MonsterBounty)
                var __enumerator9 = (m_BesideInstructionDataModel.MonsterBounty).GetEnumerator();
                while (__enumerator9.MoveNext())
                {
                    var _gropDataModel = __enumerator9.Current;
                    {
                        var _groupCount = 0;
                        {
                            // foreach(var book in gropDataModel.BountyGroupBooks)
                            var __enumerator12 = (_gropDataModel.BountyGroupBooks).GetEnumerator();
                            while (__enumerator12.MoveNext())
                            {
                                var _book = __enumerator12.Current;
                                {
                                    if (_book.BountyActive >0)
                                        _totalBountyCount++;
                                
                                    if (_book.Composeable >=1)
                                    {
                                        _groupCount++;
                                    }
                                
                                }
                            }
                        }
                        _gropDataModel.NoticeCount = _groupCount;
                        if (_groupCount >= 1)
                        {
                            _count+=_groupCount;
                        }
                    }
                }
            }
            PlayerDataManager.Instance.NoticeData.HandBookWanted = _count;
            _totalGroupCount = UpdateInstrucTeam();
            //图鉴组合红点
            _count = 0;
            // {
            //     // foreach(var oneGroup in mHandBookDataModel.GropBooks)
            //     var __enumerator10 = (m_BesideInstructionDataModel.GropBooks).GetEnumerator();
            //    while (__enumerator10.MoveNext())
            //    {
            //        var _oneGroup = __enumerator10.Current;
            //        {
            //            var _groupCount = 0;
            //            {
            //                // foreach(var oneGroupDataModel in oneGroup.GropBook)
            //                var __enumerator13 = (_oneGroup.GropBook).GetEnumerator();
            //                while (__enumerator13.MoveNext())
            //                {
            //                    var _oneGroupDataModel = __enumerator13.Current;
            //                    {
            //                        if (_oneGroupDataModel.BookEnable != 1)
            //                        {
            //                            if (_oneGroupDataModel.book.BookCount >= 1)
            //                            {
            //                                _groupCount++;
            //                            }
            //                        }
            //                        else
            //                        {
            //                            _totalGroupCount++;
            //                        }
            //                    }
            //                }
            //            }

            //            _oneGroup.NoticeCount = _groupCount;
            //            if (_groupCount >= 1)
            //            {
            //                _count++;
            //            }
            //        }
            //    }
            //}
            PlayerDataManager.Instance.NoticeData.HandBookGroup = _count;
            PlayerDataManager.Instance.TotalBountyCount = _totalBountyCount;
            PlayerDataManager.Instance.TotalGroupCount = _totalGroupCount;
        }

        private void RenewNoticeData()
        {
            var _totalBountyCount = 0;
            var _totalGroupCount = 0;
            var _c0 = m_BesideInstructionDataModel.Books.Count;
            var _noticeBookCount = 0;
            for (var i = 0; i < _c0; i++)
            {
                var _book = m_BesideInstructionDataModel.Books[i];
                if (_book.Composeable >= 1)
                {
                    _noticeBookCount++;
                }
            }
            if (PlayerDataManager.Instance.CheckCondition(43) == 0)
                PlayerDataManager.Instance.NoticeData.HandBookCompose = _noticeBookCount;
            else
                PlayerDataManager.Instance.NoticeData.HandBookCompose = 0;
            //图鉴组合table页上的红点
            _totalGroupCount = UpdateInstrucTeam();
            var _groupCount = 0;
            var _c3 = m_BesideInstructionDataModel.GropBooks.Count;
            //for (var i = 0; i < _c3; i++)
            //{
            //    var _oneGroup = m_BesideInstructionDataModel.GropBooks[i];
            //    if (_oneGroup.NoticeCount >= 1)
            //    {
            //        _groupCount++;
            //    }
            //    var __enumerator13 = (_oneGroup.GropBook).GetEnumerator();
            //    while (__enumerator13.MoveNext())
            //    {
            //        var _oneGroupDataModel = __enumerator13.Current;
            //        {
            //            if (_oneGroupDataModel.BookEnable == 1)
            //            {
            //                _totalGroupCount++;
            //            }
            //        }
            //    }
            //}
            PlayerDataManager.Instance.NoticeData.HandBookGroup = _groupCount;

            //悬赏红点
            var _bountys = m_BesideInstructionDataModel.MonsterBounty;
            var _c = _bountys.Count;
            var _bountyTableCount = 0;
            for (var i = 0; i < _c; i++)
            {
                var _oneGroupCount = 0;
                var _bountyBookGroup = _bountys[i].BountyGroupBooks;
                var _c2 = _bountyBookGroup.Count;
                for (var j = 0; j < _c2; j++)
                {
                    var _book = _bountyBookGroup[j];
                    if (_book.BountyActive > 0)
                        _totalBountyCount++;

                    if (_book.Composeable >= 1)
                    {
                        _oneGroupCount++;
                    }

                }
                _bountys[i].NoticeCount = _oneGroupCount;
                if (_oneGroupCount > 0)
                {
                    _bountyTableCount += _oneGroupCount;
                }
            }
            PlayerDataManager.Instance.NoticeData.HandBookWanted = _bountyTableCount;
            PlayerDataManager.Instance.TotalBountyCount = _totalBountyCount;
            PlayerDataManager.Instance.TotalGroupCount = _totalGroupCount;
        }

        private void ResetInstructions()
        {
            //         var list = new List<HandBookItemDataModel>(mHandBookDataModel.Books);
            //         list.Sort();
            //         mHandBookDataModel.Books = new ObservableCollection<HandBookItemDataModel>(list);
            var _c0 = m_BookBufList.Count;
            m_BookBufList.Sort();
            m_BesideInstructionDataModel.Books = new ObservableCollection<HandBookItemDataModel>();
            for (var i = 0; i < _c0; i++)
            {
                m_BookBufList[i].Index = m_BesideInstructionDataModel.Books.Count;
                m_BesideInstructionDataModel.Books.Add(m_BookBufList[i]);
            }
        }

        private void OnBookItemClick(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnBookItemClick;
            if (_e.DataModel == null)
                return;
            m_BesideInstructionDataModel.SelectedBookItem.Copy(_e.DataModel);
        }

        private void OnBookItemGetClick(IEvent ievent)
        {
            m_BesideInstructionDataModel.SelectedBook[0].Copy(m_BesideInstructionDataModel.SelectedBookItem);
            m_BesideInstructionDataModel.SelectedBook[1].Copy(new HandBookItemDataModel());
            m_BesideInstructionDataModel.BookInfoDesc = GameUtils.GetDictionaryText(m_BesideInstructionDataModel.SelectedBookItem.BookId);
            {
                var __enumerator7 = (m_BesideInstructionDataModel.Books).GetEnumerator();
                while (__enumerator7.MoveNext())
                {
                    var _dataModel = __enumerator7.Current;
                    {
                        if (m_BesideInstructionDataModel.SelectedBookItem.UpGradeRequestBookId == _dataModel.BookId)
                        {
                            m_BesideInstructionDataModel.SelectedBook[1].Copy(_dataModel);
                            break;
                        }
                    }
                }
            }
            m_BesideInstructionDataModel.BookInfoShow = 1;
        }
        private void ShowInstrucMsg(IEvent ievent)
        {
            var _e = ievent as UIEvent_HandBookFrame_OnBookClick;
        
            if (_e.DataModel == null)
            {
                m_BesideInstructionDataModel.BookInfoShow = 0;
            }
            else
            {
                m_BesideInstructionDataModel.SelectedBook[0].Copy(_e.DataModel);
                m_BesideInstructionDataModel.SelectedBook[1].Copy(new HandBookItemDataModel());
                m_BesideInstructionDataModel.BookInfoDesc = GameUtils.GetDictionaryText(_e.DataModel.BookId);
                {
                    var __enumerator7 = (m_BesideInstructionDataModel.Books).GetEnumerator();
                    while (__enumerator7.MoveNext())
                    {
                        var _dataModel = __enumerator7.Current;
                        {
                            if (_e.DataModel.UpGradeRequestBookId == _dataModel.BookId)
                            {
                                m_BesideInstructionDataModel.SelectedBook[1].Copy(_dataModel);
                                break;
                            }
                        }
                    }
                }
                m_BesideInstructionDataModel.BookInfoShow = 1;
           
            }
        }

        private int UpdateInstrucTeam()
        {
            var resultNum = 0;
            var _c0 = m_BesideInstructionDataModel.GropBooks.Count;
            for (var i = 0; i < _c0; i++)
            {
                var _oneGroup = m_BesideInstructionDataModel.GropBooks[i];

                foreach (var _team in _oneGroup.GropBooks)
                {
                    if (PlayerDataManager.Instance.GetBookGropEnable(_team.GropId))
                    {
                        _team.GropEnable = PlayerDataManager.Instance.GetBookGropEnable(_team.GropId)?1:0;
                        resultNum += PlayerDataManager.Instance.GetBookGropEnable(_team.GropId) ? 1 : 0;
                    }
                }
            }
            return resultNum;
        }

        private void UpdateNotice()
        {
            //RefreshData(null);
            RefreshData(mCurrentHandBookArg);
        }

        private void OnBookLevelUp(int bookId,int level)
        {
            foreach (var v in m_BesideInstructionDataModel.GropBooks)
            {//类
                foreach (var group in v.GropBooks)
                {//组
                    foreach (var item in group.GropBook)
                    {
                        if(item.book.BookId == bookId)
                            item.BookEnable = level;
                    }
                }
            }
        }

        private void CalcBookAttr()
        {
            Dictionary<int,int> dic = new Dictionary<int, int>();
            var book = PlayerDataManager.Instance.BountyBooks;
            foreach (var v in book)
            {
                var tbBookbase = Table.GetBookBase(v.Key*100+v.Value);
                if(tbBookbase == null)
                    continue;
                for (int i = 0; i < tbBookbase.AttrList.Length;i++)
                {
                    int attr= tbBookbase.AddAttr * tbBookbase.AttrList[i] / 10000;
                    if (attr<= 0)
                        continue;
                    dic.modifyValue(i + 1, attr);
                }
            }


            m_BesideInstructionDataModel.AttrList.Clear();
            foreach (var v in dic)
            {
                var str = new StringDataModel();
                str.str = string.Format("{0}: +{1}", ExpressionHelper.AttrName[v.Key], v.Value);
                m_BesideInstructionDataModel.AttrList.Add(str);            
            }

            var talentData = new int[(int)eAttributeType.AttrCount];
            var talentDataRef = new int[(int)eAttributeType.AttrCount];
            PlayerDataManager.Instance.ElfAttrConvert(dic, talentData, talentDataRef);
            m_BesideInstructionDataModel.FightPoint = PlayerDataManager.Instance.GetElfFightPoint(talentData, talentDataRef, 1, -2);

        }

        private ObservableCollection<HandBookItemDataModel> GetHasBook()
        {
            return m_BesideInstructionDataModel.HasBooks;
        }
    }
}