/********************************************************************************* 

                         Scorpion



  *FileName:ComposingFrameCtrler

  *Version:1.0

  *Date:2017-06-17

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
using Shared;

#endregion

namespace ScriptController
{
    public class ComposingFrameCtrler : IControllerBase
    {

        #region 成员变量

        private ComposeUIDataModel DataModel;
        private Dictionary<int, List<ItemComposeRecord>> m_dicComposeTable = new Dictionary<int, List<ItemComposeRecord>>();
        private bool m_bIsInit;
        private FrameState m_State;

        #endregion

        #region 构造函数

        public ComposingFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(ComposeMenuCellClick.EVENT_TYPE, OnTipMenuCellPropEvent);
            EventDispatcher.Instance.AddEventListener(ComposeItemOnClick.EVENT_TYPE, OnTipComposedPropEvent);
            EventDispatcher.Instance.AddEventListener(ShowComposFlag_Event.EVENT_TYPE, OnSettingsComposedFlagEvent);
            EventDispatcher.Instance.AddEventListener(ComposeMenuTabClick.EVENT_TYPE, OnComposeMenuTabClick);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemCountChangeEvent);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChangeEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitEvent);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            DataModel = new ComposeUIDataModel();
            m_dicComposeTable.Clear();
            m_bIsInit = false;
            Initial();
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

        public void RefreshData(UIInitArguments data)
        {
            if (m_bIsInit == false)
            {
                m_bIsInit = true;
                Initial();
            }

            var _args = data as ComposeArguments;

            if (_args != null)
            {
                if (_args.BuildingData != null)
                {
                    //BuildingData = args.BuildingData;
                }
                else
                {
                }
                DataModel.ShowId = _args.Tab;
            }
            else
            {
                DataModel.ShowId = -1;

            }
            /*
        if (BuildingData != null)
        {
            var tbBuliding = Table.GetBuilding(BuildingData.TypeId);
            var tbBulidingServer = Table.GetBuildingService(tbBuliding.ServiceId);
//             mLimitCounts.Clear();
// 
//             for (int i = 1; i < 4; i++)
//             {
//                 if (tbBulidingServer.Param[i * 2] != -1)
//                 {
//                     mLimitCounts.Add(tbBulidingServer.Param[i * 2], tbBulidingServer.Param[i * 2 + 1]);
//                 }
//             }
            var add = tbBulidingServer.Param[0]/100;

            var ret = CityPetSkill.GetBSParamByIndex((BuildingType) tbBuliding.Type, tbBulidingServer, 0,
                BuildingData.PetList);
            DataModel.Add = add + ret;
        }
        else
        {
//             mLimitCounts.Clear();
//             var tbBulidingServer = Table.GetBuildingService(1000);
// 
//             for (int i = 1; i < 4; i++)
//             {
//                 if (tbBulidingServer.Param[i * 2] != -1)
//                 {
//                     mLimitCounts.Add(tbBulidingServer.Param[i * 2], tbBulidingServer.Param[i * 2 + 1]);
//                 }
//             }
            DataModel.Add = 0;
        }
        */
            var _showType = eComposeType.Ticket;
            if (DataModel.ShowId != -1)
            {
                var _tbCompose = Table.GetItemCompose(DataModel.ShowId);
                if (_tbCompose != null)
                {
                    _showType = (eComposeType)_tbCompose.Type;
                }
            }

            DataModel.CurTab = (int)_showType;

            var _maxType = (int)eComposeType.Count;
            for (var i = (int)eComposeType.Ticket; i < _maxType; i++)
            {
                DataModel.MenuState[i] = i == (int)_showType;
            }
            //UpgradeSelectToClean();
            ShowTabContent();
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State
        {
            get { return m_State; }
            set { m_State = value; }
        }

        #endregion

        #region 逻辑函数

        private IEnumerator ComposedPropCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _composeCount = PlayerDataManager.Instance.GetExData((int)eExdataDefine.e415);
                var _msg = NetManager.Instance.ComposeItem(DataModel.SelectIndex, 1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (_msg.Response == -1)
                        {
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(454));
                        }
                        else
                        {
                            PlatformHelper.Event("city", "compose", DataModel.SelectIndex);
                            //合成前几次获得经验
                            //                         if (BuildingData != null)
                            //                         {
                            //                             var tbBuilding = Table.GetBuilding(BuildingData.TypeId);
                            //                             var tbServer = Table.GetBuildingService(tbBuilding.ServiceId);
                            //                             if (composeCount < tbServer.Param[3])
                            //                             {
                            //                                 EventDispatcher.Instance.DispatchEvent(new UIEvent_ComposeFlyAnim(tbServer.Param[2]));
                            //                             }
                            //                         }
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(453));
                        }
                        EventDispatcher.Instance.DispatchEvent(new ComposeItemEffectEvent(_msg.Response != -1));
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemNoInBag_All)
                    {
                        var _e = new ShowUIHintBoard(300116);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_ItemComposeID
                             || _msg.ErrorCode == (int)ErrorCodes.ItemNotEnough
                             || _msg.ErrorCode == (int)ErrorCodes.MoneyNotEnough)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        Logger.Debug("ComposeItem..................." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("ComposeItem..................." + _msg.State);
                }
            }
        }

        private void Initial()
        {
            if (m_dicComposeTable.Count == 0)
            {
                Table.ForeachItemCompose(recoard =>
                {
                    var _type = recoard.Type;
                    if (_type == 0)
                    {
                        return true;
                    }
                    List<ItemComposeRecord> _list = null;
                    if (!m_dicComposeTable.TryGetValue(_type, out _list))
                    {
                        _list = new List<ItemComposeRecord>();
                        m_dicComposeTable[_type] = _list;
                    }
                    _list.Add(recoard);

                    return true;
                });
            }
            if (DataModel.MenuState == null)
            {
                DataModel.MenuState = new Dictionary<int, bool>();
                var _maxType = (int)eComposeType.Count;
                for (var i = (int)eComposeType.Ticket; i < _maxType; i++)
                {
                    DataModel.MenuState.Add(i, i == 0);
                }
            }


        }

        private void UpgradeSelect(int selectIndex, ComposeMenuDataModel clickMenu)
        {
            SettingsChooseIndex(selectIndex, clickMenu);
            {
                // foreach(var menuData in DataModel.MenuDatas)
                var _enumerator4 = (DataModel.MenuDatas).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _menuData = _enumerator4.Current;
                    {
                        if (_menuData.Type == 1)
                        {
                            if (_menuData.TableId == selectIndex)
                            {
                                _menuData.IsOpen = 1;
                            }
                            else
                            {
                                _menuData.IsOpen = 0;
                            }
                        }
                    }
                }
            }
        }

        //private Dictionary<int, int> mLimitCounts = new Dictionary<int, int>();
        //private Dictionary<int,List<int>> mAllLimit = new Dictionary<int, List<int>>(); 
        private void UpgradeSelectToClean()
        {
            DataModel.MenuDatas.Clear();


            var _list = new List<ComposeMenuDataModel>();
            var _roleId = PlayerDataManager.Instance.GetRoleId();
            {
                // foreach(var b in DataModel.MenuState)
                var _enumerator3 = (DataModel.MenuState).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _b = _enumerator3.Current;
                    {
                        var _menu = new ComposeMenuDataModel();
                        _menu.Type = 0;
                        _menu.TableId = _b.Key;
                        var _type = (eComposeType)_b.Key;

                        //                     if (type != eComposeType.Ticket && DataModel.NeedBack == false)
                        //                     {
                        // //NPC的合成服务 去掉技能书的合成页
                        //                         continue;
                        //                     }

                        switch (_type)
                        {
                            case eComposeType.Ticket:
                            {
                                //门票
                                _menu.TypeName = GameUtils.GetDictionaryText(540);
                            }
                                break;

                            case eComposeType.Rune:
                            {
                                //属性符文
                                _menu.TypeName = GameUtils.GetDictionaryText(541);
                            }
                                break;
                            case eComposeType.SkillBook:
                            {
                                //技能书
                                _menu.TypeName = GameUtils.GetDictionaryText(542);
                            }
                                break;
                            case eComposeType.SkillPiece:
                            {
                                //技能残章
                                _menu.TypeName = GameUtils.GetDictionaryText(543);
                                //menu.TypeName = "技能残章";
                            }
                                break;
                            case eComposeType.MayaShenQi:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(100002114);
                            }
                                break;
                            case eComposeType.MountSkill:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(539);
                            }
                                break;
                        }
                        _list.Add(_menu);

                        //                     int count = -1;
                        //                     if (!mLimitCounts.TryGetValue(b.Key, out count))
                        //                     {
                        //                         count = -1;
                        //                     }
                        var _level = PlayerDataManager.Instance.GetRes((int)eResourcesType.LevelRes);
                        if (_b.Value)
                        {
                            _menu.IsOpen = 1;
                            var _index = 0;
                            List<ItemComposeRecord> _itemList = null;
                            if (m_dicComposeTable.TryGetValue(_menu.TableId, out _itemList))
                            {
                                {
                                    var _list6 = _itemList;
                                    var _listCount6 = _list6.Count;
                                    var select = -1;
                                    ComposeMenuDataModel _subMenuRef = null;
                                    for (var _i6 = 0; _i6 < _listCount6; ++_i6)
                                    {
                                        var _record = _list6[_i6];
                                        {
                                            var _lv = -1;
                                            if (_level < _record.ComposeOpenLevel)
                                            {
                                                _lv = _record.ComposeOpenLevel;
                                            }
                                            else
                                            {
                                                _lv = -1;
                                            }
                                            if (!BitFlag.GetLow(_record.SortByCareer, _roleId))
                                            {
                                                continue;
                                            }
                                            var _subMenu = new ComposeMenuDataModel();
                                            _subMenu.PermitLevel = _lv;
                                            if ((DataModel.ShowId == -1 && _index == 0) || DataModel.ShowId == _record.Id)
                                            {
                                                select = _record.Id;
                                                SettingsChooseIndex(_record.Id, _subMenu);
                                                _subMenu.IsOpen = 1;
                                            }
                                            else
                                            {
                                                _subMenu.IsOpen = 0;
                                                if (_subMenuRef == null)
                                                {
                                                    _subMenuRef = _subMenu;
                                                }
                                            }
                                            _index++;
                                            _subMenu.Type = 1;
                                            _subMenu.TableId = _record.Id;
                                            _subMenu.ItemId = Table.GetItemCompose(_record.Id).ComposeView;
                                            _list.Add(_subMenu);
                                        }
                                    }

                                    if (select == -1 && _subMenuRef != null)
                                    {
                                        SettingsChooseIndex(_subMenuRef.TableId, _subMenuRef);
                                        _subMenuRef.IsOpen = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            DataModel.MenuDatas = new ObservableCollection<ComposeMenuDataModel>(_list);
        }

        private void ShowTabContent()
        {
            DataModel.MenuDatas.Clear();


            var _list = new List<ComposeMenuDataModel>();
            var _roleId = PlayerDataManager.Instance.GetRoleId();
            {
                var _enumerator3 = (DataModel.MenuState).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _b = _enumerator3.Current;
                    {
                        var _menu = new ComposeMenuDataModel();
                        _menu.Type = 0;
                        _menu.TableId = _b.Key;
                        var _type = (eComposeType)_b.Key;
                        switch (_type)
                        {
                            case eComposeType.Ticket:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(540);
                            }
                                break;

                            case eComposeType.Rune:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(541);
                            }
                                break;
                            case eComposeType.SkillBook:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(542);
                            }
                                break;
                            case eComposeType.SkillPiece:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(543);
                            }
                                break;
                            case eComposeType.MayaShenQi:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(100002114);
                            }
                                break;
                            case eComposeType.MountSkill:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(539);
                            }
                                break;
                        }
                        //_list.Add(_menu);

                        var _level = PlayerDataManager.Instance.GetRes((int)eResourcesType.LevelRes);
                        if (_b.Value)
                        {
                            _menu.IsOpen = 1;
                            var _index = 0;
                            List<ItemComposeRecord> _itemList = null;
                            if (m_dicComposeTable.TryGetValue(_menu.TableId, out _itemList))
                            {
                                {
                                    var _list6 = _itemList;
                                    var _listCount6 = _list6.Count;
                                    var select = -1;
                                    ComposeMenuDataModel _subMenuRef = null;
                                    for (var _i6 = 0; _i6 < _listCount6; ++_i6)
                                    {
                                        var _record = _list6[_i6];
                                        {
                                            var _lv = -1;
                                            if (_level < _record.ComposeOpenLevel)
                                            {
                                                _lv = _record.ComposeOpenLevel;
                                            }
                                            else
                                            {
                                                _lv = -1;
                                            }
                                            if (!BitFlag.GetLow(_record.SortByCareer, _roleId))
                                            {
                                                continue;
                                            }
                                            var _subMenu = new ComposeMenuDataModel();
                                            _subMenu.PermitLevel = _lv;
                                            if ((DataModel.ShowId == -1 && _index == 0) || DataModel.ShowId == _record.Id)
                                            {
                                                select = _record.Id;
                                                SettingsChooseIndex(_record.Id, _subMenu);
                                                _subMenu.IsOpen = 1;
                                            }
                                            else
                                            {
                                                _subMenu.IsOpen = 0;
                                                if (_subMenuRef == null)
                                                {
                                                    _subMenuRef = _subMenu;
                                                }
                                            }
                                            _index++;
                                            _subMenu.Type = 1;
                                            _subMenu.TableId = _record.Id;
                                            _subMenu.ItemId = Table.GetItemCompose(_record.Id).ComposeView;
                                            _list.Add(_subMenu);
                                        }
                                    }

                                    if (select == -1 && _subMenuRef != null)
                                    {
                                        SettingsChooseIndex(_subMenuRef.TableId, _subMenuRef);
                                        _subMenuRef.IsOpen = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            DataModel.MenuDatas = new ObservableCollection<ComposeMenuDataModel>(_list);
        }

        private void SettingsChooseIndex(int index, ComposeMenuDataModel clickMenu)
        {
            DataModel.PermitLevel = clickMenu.PermitLevel;
            //if (DataModel.PermitLevel != -1)
            //{
            //    DataModel.PermitLevel ++;
            //}
            DataModel.SelectIndex = index;
            if (index == -1)
            {
                return;
            }
            var _tbCompose = Table.GetItemCompose(DataModel.SelectIndex);
            var _count = 0;
            for (var i = 0; i < _tbCompose.NeedId.Count; i++)
            {
                if (_tbCompose.NeedId[i] != -1)
                {
                    _count++;
                }
            }
            DataModel.ConsumeCount = _count;
        }

        #endregion

        #region 事件

        private void OnBagItemCountChangeEvent(IEvent iEvent)
        {
            var e = iEvent as UIEvent_BagItemCountChange;
            if (e != null)
            {
                var tbItemBase = Table.GetItemBase(e.ItemId);
                if (null == tbItemBase)
                    return;
                if (22002 == tbItemBase.Id || 26700 == tbItemBase.Type)
                {
                    RefreshSkillBookNotice();
                }
            }
        }

        private void OnResourceChangeEvent(IEvent ievent)
        {
            var e = ievent as Resource_Change_Event;
            if (null != e)
            {
                if (e.Type == eResourcesType.Spar)
                {
                    RefreshSkillBookNotice();
                }
            }
        }

        private void OnExDataInitEvent(IEvent ievent)
        {
            var e = ievent as ExDataInitEvent;
            if (null != e)
            {
                RefreshSkillBookNotice();
            }
        }

        private void RefreshSkillBookNotice()
        {
            var skillBooks = m_dicComposeTable[(int)eComposeType.SkillBook];
            if (null != skillBooks)
            {
                PlayerDataManager.Instance.NoticeData.ComposeSkillBookNotice = false;
                foreach (var skillBook in skillBooks)
                {
                    if (IsItemCanCompose(skillBook.Id))
                    {
                        PlayerDataManager.Instance.NoticeData.ComposeSkillBookNotice = true;
                        return;
                    }
                }
            }
        }

        private bool IsItemCanCompose(int index)
        {
            var _playerData = PlayerDataManager.Instance.PlayerDataModel;
            var _tbCompose = Table.GetItemCompose(index);
            if (_tbCompose.ComposeOpenLevel > _playerData.Bags.Resources.Level)
                return false;
            if (!BitFlag.GetLow(_tbCompose.SortByCareer, PlayerDataManager.Instance.GetRoleId()))
                return false;
            if (PlayerDataManager.Instance.GetRemaindCapacity(eBagType.BaseItem) == 0)
                return false;

            for (var i = 0; i < 4; i++)
            {
                if (_tbCompose.NeedId[i] != -1)
                {
                    if (PlayerDataManager.Instance.GetItemCount(_tbCompose.NeedId[i]) < _tbCompose.NeedCount[i])
                        return false;
                }
            }
            switch (_tbCompose.NeedRes)
            {
                case 2:
                    {
                        if (_tbCompose.NeedValue > _playerData.Bags.Resources.Gold)
                            return false;
                    }
                    break;
                case 3:
                    {
                        if (_tbCompose.NeedValue > _playerData.Bags.Resources.Diamond)
                            return false;
                    }
                    break;
            }
            return true;
        }

        private void OnComposeMenuTabClick(IEvent iEvent)
        {
            var e = iEvent as ComposeMenuTabClick;
            if (null == e)
                return;
            MenuTabClickEvent(e.Tab);
        }

        private void OnSettingsComposedFlagEvent(IEvent e)
        {

            var _tbCompose = Table.GetItemCompose(DataModel.SelectIndex);
            for (var i = 0; i < 4; i++)
            {
                if (_tbCompose.NeedId[i] != -1)
                {
                    if (PlayerDataManager.Instance.GetItemCount(_tbCompose.NeedId[i]) > _tbCompose.NeedCount[i])
                    {
                        PlayerDataManager.Instance.NoticeData.ComposeNotice = true;
                        //return;
                    }
                    else
                    {
                        PlayerDataManager.Instance.NoticeData.ComposeNotice = false;
                    }
                }
            }
        }

        private void OnTipComposedPropEvent(IEvent ievent)
        {
            var _playerData = PlayerDataManager.Instance.PlayerDataModel;
            var _tbCompose = Table.GetItemCompose(DataModel.SelectIndex);
            if (DataModel.PermitLevel != -1)
            {
                var _str = string.Format(GameUtils.GetDictionaryText(300908), DataModel.PermitLevel);
                GameUtils.ShowHintTip(_str);
                return;
            }
            if (PlayerDataManager.Instance.GetRemaindCapacity(eBagType.BaseItem) == 0)
            {
                //
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(300116));
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                if (_tbCompose.NeedId[i] != -1)
                {
                    if (PlayerDataManager.Instance.GetItemCount(_tbCompose.NeedId[i]) < _tbCompose.NeedCount[i])
                    {
                        //"材料不足"
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                        if (_tbCompose.NeedId[i] == 22002)//如果是合成宝石
                        {
                            GameUtils.ShowQuickBuy(22002, 1);
                        }
                        return;
                    }
                }
            }
            switch (_tbCompose.NeedRes)
            {
                case 2:
                {
                    if (_tbCompose.NeedValue > _playerData.Bags.Resources.Gold)
                    {
                        //"金钱不足"
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                        return;
                    }
                }
                    break;
                case 3:
                {
                    if (_tbCompose.NeedValue > _playerData.Bags.Resources.Diamond)
                    {
                        //"钻石不足"
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                        return;
                    }
                }
                    break;
            }

            NetManager.Instance.StartCoroutine(ComposedPropCoroutine());
        }

        private void OnTipMenuCellPropEvent(IEvent ievent)
        {
            var _e = ievent as ComposeMenuCellClick;
            var _clickMenu = _e.MenuData;

            var selectSubMenu = -1;
            if (_clickMenu.Type == 0)
            {
                var _dic = new Dictionary<int, bool>();
                if (DataModel.MenuState[_clickMenu.TableId] == false)
                {
                    {
                        // foreach(var b in DataModel.MenuState)
                        var _enumerator1 = (DataModel.MenuState).GetEnumerator();
                        while (_enumerator1.MoveNext())
                        {
                            var _b = _enumerator1.Current;
                            {
                                _dic.Add(_b.Key, _b.Key == _clickMenu.TableId);
                            }
                        }
                    }
                }
                else
                {
                    {
                        // foreach(var b in DataModel.MenuState)
                        var _enumerator2 = (DataModel.MenuState).GetEnumerator();
                        while (_enumerator2.MoveNext())
                        {
                            var b = _enumerator2.Current;
                            {
                                _dic.Add(b.Key, false);
                            }
                        }
                    }
                }
                DataModel.CurTab = _clickMenu.TableId;
                DataModel.MenuState = _dic;
                UpgradeSelectToClean();
            }
            else
            {
                selectSubMenu = _clickMenu.TableId;
                UpgradeSelect(selectSubMenu, _clickMenu);
            }
        }

        private void MenuTabClickEvent(int tab)
        {
            var _dic = new Dictionary<int, bool>();
            if (DataModel.MenuState[tab] == false)
            {
                {
                    // foreach(var b in DataModel.MenuState)
                    var _enumerator1 = (DataModel.MenuState).GetEnumerator();
                    while (_enumerator1.MoveNext())
                    {
                        var _b = _enumerator1.Current;
                        {
                            _dic.Add(_b.Key, _b.Key == tab);
                        }
                    }
                }
            }
            else
            {
                {
                    // foreach(var b in DataModel.MenuState)
                    var _enumerator2 = (DataModel.MenuState).GetEnumerator();
                    while (_enumerator2.MoveNext())
                    {
                        var b = _enumerator2.Current;
                        {
                            _dic.Add(b.Key, false);
                        }
                    }
                }
            }
            DataModel.CurTab = tab;
            DataModel.MenuState = _dic;
            //UpgradeSelectToClean();
            ShowTabContent();
        }

        #endregion

  
    }
}