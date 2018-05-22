#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class TitleUIController : IControllerBase
    {
        public TitleUIController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_TitleItemOption.EVENT_TYPE, TitleItemOption);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, InitFlag);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, UpdateFlag);
            EventDispatcher.Instance.AddEventListener(TitleMenuCellClick.EVENT_TYPE, OnTipMenuCellPropEvent);
            EventDispatcher.Instance.AddEventListener(TitleBranchCellClick.EVENT_TYPE, SelectBrachTitle);
            EventDispatcher.Instance.AddEventListener(UIEvent_GetTitleNum.EVENT_TYPE, GotTitleIndex);
        }

        private TitleDataModel DataModel;
        private Dictionary<int, int> DicFlagId = new Dictionary<int, int>(); //flagId字典
        private Dictionary<int, List<int>> Group = new Dictionary<int, List<int>>();
        private Dictionary<int, bool> IdList = new Dictionary<int, bool>(); //<nameTitle表id,是否激活>
        //private Dictionary<int,int> StateList = new Dictionary<int, int>(); //State佩戴
        private bool IsFirstOpen = false;
        private int SelectedState = (int) TitleState.NoActive;
        private int SelectedTitleId = -1;
        private int SelectGroupId = -1;//当前选择的总目录表格ID 
        private int SelectListId = -1;//当前总目录中最高称号的Index 0,1,2


        private int choosedTitleIndex = 1;//上次选择的总目录index
        private int unfoldIndex = 0;//为记录上次选择的总目录Index的一个计数标志
        private int unFoldTitleNum = 0;//打开折叠的子目录个数
        private int clickTitleListIndex = 0;//当前选择的总目录的Index 0,1,2....
        private int clickBrachId;//当前选择的分目录表格ID
        private int clickBrachIndex;//当前选择的分目录的Index
        private int putOnBrachId;//当前佩戴的分目录表格ID
        private int putOnTotalIndex;//当前佩戴的总目录Index


        private int ReceivedIndex=0;//当前点击的目录下已经得到称号的最高表格ID  3001...
        private int RecIdxCliTtalCha;//当前点击的目录下已经得到称号的最高表格ID  3001... 只有在点击总目录才改变其值
        private int ReceiveMilitary = 0;
        private void GotTitleIndex(IEvent ievent)
        {
            var _e = ievent as UIEvent_GetTitleNum;
            ReceivedIndex = _e.TitleIndex;
        }

        private void OnTipMenuCellPropEvent(IEvent ievent)
        {
            var _e = ievent as TitleMenuCellClick;
            var _clickMenu = _e.MenuData;

            var selectSubMenu = -1;
            if (_clickMenu.Type == 0)
            {
                unFoldTitleNum = 0;
                var _dic = new Dictionary<int, bool>();
                if (DataModel.MenuState[_clickMenu.TableId] == false)
                {
                    {
                        unfoldIndex = 0;
                        // foreach(var b in DataModel.MenuState)
                        var _enumerator1 = (DataModel.MenuState).GetEnumerator();
                        while (_enumerator1.MoveNext())
                        {
                            var _b = _enumerator1.Current;
                            {
                                unfoldIndex++;
                                if (_b.Key == _clickMenu.TableId)
                                {
                                    choosedTitleIndex = unfoldIndex;
                                }
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
                DataModel.MenuState = _dic;
                NetManager.Instance.StartCoroutine(UpgradeSelectToCleanCortoutine());
            }
            else
            {
                DataModel.ItemSelected = GetSetItem(Group[SelectGroupId][SelectListId + 1]);//SelectGroupId  SelectListId + 1
            }
       
            //DataModel.ShowSelectTitle = Table.GetNameTitle(SelectGroupId + SelectListId).Name;//显示选中的称号在模型上
            clickBrachId = ReceivedIndex;
            RecIdxCliTtalCha = ReceivedIndex;
            if (SelectGroupId == putOnTotalIndex)
            {
                SetChooseMask(putOnBrachId);
                clickBrachTitle(putOnBrachId - SelectGroupId);
            }
            else
            {
                SetChooseMask(ReceivedIndex);
                clickBrachTitle(ReceivedIndex - SelectGroupId);
            }
        }

        private void SelectBrachTitle(IEvent ievent)
        {
       
            var bra = ievent as TitleBranchCellClick;
            clickBrachId = SelectGroupId + bra.Index - choosedTitleIndex;
            clickBrachIndex = bra.Index;
            var chooseIndex = bra.Index - choosedTitleIndex;

            List<int> titleList;
            if (Group.TryGetValue(SelectGroupId, out titleList))
            {
                if (chooseIndex >= 0 && chooseIndex < titleList.Count)
                {
                    var titleId = titleList[chooseIndex];
                    DataModel.ItemSelected = GetSetItem(titleId);

                    if (chooseIndex <= RecIdxCliTtalCha - SelectGroupId && DataModel.Lists[clickTitleListIndex].State != (int)TitleState.NoActive)//选择已经得到的称号显示佩戴
                    {
                        if (PlayerDataManager.Instance.TitleList.ContainsValue(clickBrachId))
                        {
                            DataModel.ItemSelected.State = 2;
                        }
                        else
                        {
                            DataModel.ItemSelected.State = 1;
                        }
                    } 
                }
                else
                {
                    Logger.Error("");
                }
            }
      
            //DataModel.ItemSelected = GetSetItem(Group[SelectGroupId][chooseIndex]);
            SetChooseMask(chooseIndex+SelectGroupId);
            //DataModel.ShowSelectTitle = Table.GetNameTitle(SelectGroupId + chooseIndex).Name;
        }

        private void clickBrachTitle(int index)
        {
            //clickBrachId = SelectGroupId + index - choosedTitleIndex;
            //clickBrachIndex = index;
            var chooseIndex = index;
            clickBrachId = SelectGroupId + chooseIndex ;
            clickBrachIndex = chooseIndex + choosedTitleIndex;
       
            List<int> titleList;
            if (Group.TryGetValue(SelectGroupId, out titleList))
            {
                if (chooseIndex >= 0 && chooseIndex < titleList.Count)
                {
                    var titleId = titleList[chooseIndex];
                    DataModel.ItemSelected = GetSetItem(titleId);
                    if (clickTitleListIndex >=DataModel.Lists.Count)
                    {
                        return;
                    }
                    if (chooseIndex <= RecIdxCliTtalCha - SelectGroupId && DataModel.Lists[clickTitleListIndex].State != (int)TitleState.NoActive)//选择已经得到的称号显示佩戴
                    {
                        if (PlayerDataManager.Instance.TitleList.ContainsValue(clickBrachId))
                        {
                            DataModel.ItemSelected.State = 2;
                        }
                        else
                        {
                            DataModel.ItemSelected.State = 1;
                        }
                    }
                }
                else
                {
                    Logger.Error("");
                }
            }

            //DataModel.ItemSelected = GetSetItem(Group[SelectGroupId][chooseIndex]);
            SetChooseMask(chooseIndex + SelectGroupId);
            //DataModel.ShowSelectTitle = Table.GetNameTitle(SelectGroupId + chooseIndex).Name;
    
        }

        private void SetPutOnState(int chooseBraIndex)
        {

            List<int> titleList;
            if (Group.TryGetValue(SelectGroupId, out titleList))
            {
                if (chooseBraIndex >= 0 && chooseBraIndex < titleList.Count)
                {
                    var titleId = titleList[chooseBraIndex];
                    DataModel.ItemSelected = GetSetItem(titleId);              
                    DataModel.ItemSelected.State = 2;

                }
          
            }
        }

        private void SetChooseMask(int selectIndex)
        {
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
        private void SetPutOnIcon(int selectIndex)
        {
            var _enumerator4 = (DataModel.MenuDatas).GetEnumerator();
            while (_enumerator4.MoveNext())
            {
                var _menuData = _enumerator4.Current;
                {
                    if (_menuData.Type == 1)
                    {
                        if (_menuData.TableId == selectIndex)
                        {
                            _menuData.ShowPutonIcon = true;
                        }
                        else
                        {
                            _menuData.ShowPutonIcon = false;
                        }
                    }

                }
            }

        }
        private Dictionary<int, List<ItemComposeRecord>> m_dicComposeTable = new Dictionary<int, List<ItemComposeRecord>>();
        private  IEnumerator  UpgradeSelectToCleanCortoutine()
        {
            yield return new WaitForSeconds(0.2f);
            var GetedTitle = 0;
            DataModel.MenuDatas.Clear();
            string a = GameUtils.GetDictionaryText(100002157);
            string b = GameUtils.GetDictionaryText(540);

            var _list = new List<TitleItemDataModel>();           
            {                
                var _enumerator3 = (DataModel.MenuState).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _b = _enumerator3.Current;
                    {
                        var _menu = new TitleItemDataModel();
                        _menu.Type = 0;
                        _menu.TableId = _b.Key;
                        var _type = (eTitleType)_b.Key;


                        switch (_type)
                        {
                            case eTitleType.Rank:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(100002157);
                            }
                                break;

                            case eTitleType.Level:
                            {
                             
                                _menu.TypeName = GameUtils.GetDictionaryText(100002158);
                            }
                                break;
                            case eTitleType.Strength:
                            {
                              
                                _menu.TypeName = GameUtils.GetDictionaryText(100002159);
                            }
                                break;
                            case eTitleType.Wings:
                            {
                              
                                _menu.TypeName = GameUtils.GetDictionaryText(1059);
                           
                            }
                                break;
                            case eTitleType.Beast:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(100002161);
                            }
                                break;
                            case eTitleType.Skill:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(100002162);
                            }
                                break;
                            case eTitleType.Collect:
                            {
                                _menu.TypeName = GameUtils.GetDictionaryText(100002163);
                            }
                                break;
                            case eTitleType.WingCulture:
                                {
                                    _menu.TypeName = GameUtils.GetDictionaryText(1060);
                                }
                                break;
                            case eTitleType.EquipFashion:
                                {
                                    _menu.TypeName = GameUtils.GetDictionaryText(100002306);
                                }
                                break;
                            case eTitleType.WeaponFashion:
                                {
                                    _menu.TypeName = GameUtils.GetDictionaryText(100002307);
                                }
                                break;
                            case eTitleType.WingFashion:
                                {
                                    _menu.TypeName = GameUtils.GetDictionaryText(100002308);
                                }
                                break;
                      
                        }
                        _list.Add(_menu);
                        if (_type == eTitleType.Rank)
                        {
                            GetedTitle = Table.GetHonor(ReceiveMilitary).TitleId;
                        }
                        else
                        {
                            GetedTitle = ReceivedIndex;
                        }
                       
                        if (_b.Value)
                        {
                            // var aaa = DataModel.ItemSelected.State;
                            
                            //if (SelectGroupId == -1) SelectGroupId = 3019;
                            var GainedNum = GetedTitle - SelectGroupId;
                            _menu.IsOpen = 0;
                            foreach (var tt in Group[SelectGroupId])
                            {
                                var _subMenu = new TitleItemDataModel();
                                _subMenu.Type = 1;
                                _subMenu.TableId = tt;
                                if (clickTitleListIndex >= DataModel.Lists.Count)
                                {
                                    yield break;
                                }
                                if (GainedNum >= 0 &&
                                    DataModel.Lists[clickTitleListIndex].State != (int) TitleState.NoActive)
                                {
                                    _subMenu.GainedTitle = true;
                                    GainedNum--;
                                }
                                unFoldTitleNum++;
                                if (_subMenu.TableId == GetedTitle)
                                {
                                    _subMenu.IsOpen = 1;
                                }
                                else
                                {
                                    _subMenu.IsOpen = 0;
                                }
                                _list.Add(_subMenu);
                            }
                        }
                        else
                        {
                            _menu.IsOpen = 1;
                    
                        }
                    }
                }
            }
            DataModel.MenuDatas = new ObservableCollection<TitleItemDataModel>(_list);
            ShowPuton();
        }



        //  private void SettingsChooseIndex(int index, TitleItemDataModel clickMenu)
        // {
        //DataModel.PermitLevel = clickMenu.PermitLevel;
        ////if (DataModel.PermitLevel != -1)
        ////{
        ////    DataModel.PermitLevel ++;
        ////}
        //DataModel.SelectIndex = index;
        //if (index == -1)
        //{
        //    return;
        //}
        //var _tbCompose = Table.GetItemCompose(DataModel.SelectIndex);
        //var _count = 0;
        //for (var i = 0; i < _tbCompose.NeedId.Count; i++)
        //{
        //    if (_tbCompose.NeedId[i] != -1)
        //    {
        //        _count++;
        //    }
        //}
        //DataModel.ConsumeCount = _count;
        // }




        private void UpgradeSelect(int selectIndex, TitleItemDataModel clickMenu)
        {
            // SettingsChooseIndex(selectIndex, clickMenu);
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
                var _maxType = (int) eTitleType.Count;

                DataModel.MenuState.Clear();
                foreach (int v in Enum.GetValues(typeof(eTitleType)))
                {
                    if(v != _maxType)
                        DataModel.MenuState.Add(v, false);
                }

                //for(var i=1;i<_maxType;i++)
                //{
                //    DataModel.MenuState.Add(i, i == 0);
                //}
            }
        }


        private enum TitleState
        {
            NoActive, //0 未激活 1激活未佩戴 2佩戴中
            ActiveNoPutOn,
            PutOn
        }

        private void GetGroupId(TitleItemDataModel item)
        {
            foreach (var ii in Group)
            {
                var count = ii.Value.Count;
                for (var i = 0; i < count; i++)
                {
                    if (item.Id == ii.Value[i])
                    {
                        SelectedState = item.State;
                        SelectGroupId = ii.Key;
                        SelectListId = i;
                        return;
                    }
                }
            }
            SelectGroupId = -1;
            SelectListId = -1;
        }

        private TitleItemDataModel GetSetItem(int titleId)
        {
            var item = new TitleItemDataModel();
            item.Id = titleId;
            if (titleId == SelectedTitleId)
            {
                item.State = SelectedState;
            }
            else
            {
                item.State = (int) TitleState.NoActive;
            }
            GameUtils.TitleAddAttr(item, Table.GetNameTitle(titleId));
            return item;
        }

        private void InitFlag(IEvent ievent)
        {
            var keys = IdList.Keys.ToList();
            foreach (var key in keys)
            {
                var tbNameTitle = Table.GetNameTitle(key);
                if (tbNameTitle.Pos != 1)
                {
                    continue;
                }
                IdList[key] = PlayerDataManager.Instance.GetFlag(tbNameTitle.FlagId);
            }
        }

        private void ListSort(List<TitleItemDataModel> list)
        {
            // var varList = list;
            list.Sort((a, b) =>
            {
                if (a.State > b.State)
                {
                    if (b.State == 0)
                    {
                        return -1;
                    }
                    if (b.State == 1)
                    {
                        return a.Id - b.Id;
                    }
                    return -1;
                }
                if (a.State == b.State)
                {
                    return a.Id - b.Id;
                }
                if (a.State == 0)
                {
                    return 1;
                }
                if (a.State == 1)
                {
                    return a.Id - b.Id;
                }
                return 1;
            });
        }

        private void PutOn(int BrachID,int index)
        {
            // NetManager.Instance.StartCoroutine(SelectTitle(DataModel.Lists[index].Id, index));//默认选择最高的称号佩戴
            NetManager.Instance.StartCoroutine(SelectTitle(BrachID, index));
        }


        private void RefleshTitle()
        {
            var list = new List<TitleItemDataModel>();

            foreach (var g in Group)
            {
                var i = g.Value.Count - 1;
                for (; i >= 0; i--)
                {
                    var id = g.Value[i];
                    bool active;
                    if ((IdList.TryGetValue(id, out active) && active) || i == 0)
                    {
                        var item = new TitleItemDataModel();
                  
                        item.Id = id;
                        item.State = active ? (int) TitleState.ActiveNoPutOn : (int) TitleState.NoActive;
                        GameUtils.TitleAddAttr(item, Table.GetNameTitle(id));
                        list.Add(item);
                        break;
                    }
                }
            }

            var titleList = PlayerDataManager.Instance.TitleList;
            for (var i = 0; i < list.Count; i++)
            {
                var ii = list[i];
                if (titleList.ContainsValue(ii.Id))
                {
                    ii.State = (int) TitleState.PutOn;
                }
                else
                {
                    if (ii.State != (int) TitleState.NoActive)
                    {
                        ii.State = (int) TitleState.ActiveNoPutOn;
                    }
                }
            }
            // ListSort(list);
            DataModel.Lists = new ObservableCollection<TitleItemDataModel>(list);
        }

        private IEnumerator SelectTitle(int Id, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.SelectTitle(Id);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        foreach (var model in DataModel.Lists)
                        {
                            if (model.State == (int)TitleState.PutOn)
                            {
                                model.State = (int)TitleState.ActiveNoPutOn;
                                break;
                            }
                        }

                        DataModel.Lists[index].State = (int)TitleState.PutOn;

                        foreach (var model in DataModel.MenuDatas)
                        {
                            if (model.State == (int)TitleState.PutOn)
                            {
                                model.State = (int)TitleState.ActiveNoPutOn;
                                break;
                            }
                        }
                        foreach (var model in DataModel.MenuDatas)
                        {
                            var stateList = PlayerDataManager.Instance.TitleList;
                            if(model.TableId==Id)
                            {
                                model.State = (int)TitleState.PutOn;
                            }
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
        }

        private void SetBtnShowState()
        {
            if (SelectListId == -1 || SelectListId == 0)
            {
                DataModel.ShowBackBtn = false;
            }
            else
            {
                DataModel.ShowBackBtn = true;
            }
            if (SelectListId == -1 || (SelectListId >= Group[SelectGroupId].Count - 1))
            {
                DataModel.ShowFrontBtn = false;
            }
            else
            {
                DataModel.ShowFrontBtn = true;
            }
        }



        private void TitleItemOption(IEvent ievent)
        {
            var e = ievent as UIEvent_TitleItemOption;
            switch (e.Type)
            {
                case 0:
                {

                    PutOn(clickBrachId,clickTitleListIndex);//e.Idx
                    SetPutOnState(clickBrachIndex - choosedTitleIndex);
                    putOnBrachId = clickBrachId;
                    putOnTotalIndex = SelectGroupId;
             
                    //SetPutOnIcon(clickBrachIndex - choosedTitleIndex + SelectGroupId);//显示穿戴记号
               
                    break;
                }
                case 1:
                {
                    DataModel.ItemSelected = DataModel.Lists[e.Idx];
                    DataModel.ItemShowed = DataModel.Lists[e.Idx];
                    SelectedTitleId = DataModel.Lists[e.Idx].Id;
                    GetGroupId(DataModel.ItemSelected);
                    SetBtnShowState();
                    DataModel.ShowInfo = true;
                    break;
                }
                case 2:
                {
                    DataModel.ShowInfo = false;
                    break;
                }
                case 3:
                {
                    DataModel.ItemSelected = GetSetItem(Group[SelectGroupId][SelectListId - 1]);
                    SelectListId -= 1;
                    SetBtnShowState();
                    break;
                }
                case 4:
                {
                    DataModel.ItemSelected = GetSetItem(Group[SelectGroupId][SelectListId + 1]);
                    SelectListId += 1;
                    SetBtnShowState();
                    break;
                }
                case 5:
                {
                    clickTitleListIndex = e.Idx;
                    //if (clickTitleListIndex > choosedTitleIndex-1 && clickTitleListIndex < unFoldTitleNum + choosedTitleIndex)
                    //{
                    //    return;
                    //}
                    if (clickTitleListIndex >= unFoldTitleNum + choosedTitleIndex)
                    {
                        clickTitleListIndex = e.Idx - unFoldTitleNum;            
                    }
                    if (clickTitleListIndex >= DataModel.Lists.Count)
                    {
                        return;
                    }
                    DataModel.ItemSelected = DataModel.Lists[clickTitleListIndex];
                    DataModel.ItemShowed = DataModel.Lists[clickTitleListIndex];
                    SelectedTitleId = DataModel.Lists[clickTitleListIndex].Id;
                    GetGroupId(DataModel.ItemSelected);
                    // SetBtnShowState();
                    //  DataModel.ShowInfo = true;
                    break;
                }
            }
        }

        private void UpdateFlag(IEvent ievent)
        {
            var e = ievent as FlagUpdateEvent;
            var key = DicFlagId.ContainsKey(e.Index);
            if (key)
            {
                IdList[DicFlagId[e.Index]] = e.Value;
            
            }
            RefleshTitle();
        }

        public void CleanUp()
        {
       
            DataModel = new TitleDataModel();
            IdList.Clear();
            DicFlagId.Clear();
            m_dicComposeTable.Clear();
            m_bIsInit = false;
            Table.ForeachNameTitle(table =>
            {
                if (table.Pos == -1)
                {
                    return true;
                }
                IdList.Add(table.Id, false);
                if (!DicFlagId.ContainsKey(table.FlagId))
                {
                    DicFlagId.Add(table.FlagId, table.Id);
                }
                return true;
            });

            Group.Clear();
            titleRecordList.Clear();
            Table.ForeachNameTitle(record =>
            {
                allList.Add(record);
                if (record.Pos != 1)
                {
                    return true;
                }

                if (record.FrontId != -1)
                {
                    return true;
                }
                titleRecordList.Add(record);
                return true;
            });

            titleRecordList.Sort(SortByTitleType);
            
            for (int i = 0; i < allList.Count; i++)
            {
                List<int> set;
                if (currIndex < titleRecordList.Count)
                {
                    if (!Group.TryGetValue(titleRecordList[currIndex].Id, out set))
                    {
                        set = new List<int>();
                        
                        if (allList[i] == null)
                        {
                            Logger.Error("NameTitle Table Get Data is NULL" );
                            return;
                        }                        
                        if (allList[i].TitleType == titleRecordList[currIndex].TitleType)
                        {
                            set.Add(allList[i].Id);
                            Group[allList[i].Id] = set;

                            while (allList[i] != null && allList[i].PostId != -1)
                            {
                                set.Add(allList[i].PostId);
                                allList[i] = Table.GetNameTitle(allList[i].PostId);
                            }
                            currIndex++;
                            i = 0;
                        }
                    }
                }
            }
        }

       private int currIndex = 0;
       private List<NameTitleRecord> titleRecordList = new List<NameTitleRecord>();
       private List<NameTitleRecord> allList = new List<NameTitleRecord>(); 

        private int SortByTitleType(NameTitleRecord a1, NameTitleRecord a2)
        {            
            if (a2.TitleType.CompareTo(a1.TitleType) != 0)
                return -(a2.TitleType.CompareTo(a1.TitleType));
            if (a2.Id.CompareTo(a1.Id) != 0)
                return -(a2.Id.CompareTo(a1.Id));
            return 1;            
        } 

        


        private bool m_bIsInit;
        public void RefreshData(UIInitArguments data)
        {

            DataModel.ShowName = PlayerDataManager.Instance.GetName();
            DataModel.ShowInfo = false;
            RefleshTitle();
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
                DataModel.ShowId = _args.Tab;
            }
            else
            {
                DataModel.ShowId = -1;

            }
            var _showType = eTitleType.Strength;
            //if (DataModel.ShowId != -1)
            //{
            //    var _tbCompose = Table.GetItemCompose(DataModel.ShowId);
            //    if (_tbCompose != null)
            //    {
            //        _showType = (eComposeType)_tbCompose.Type;
            //    }
            //}

            var _maxType = (int)eTitleType.Count;
            DataModel.MenuState.Clear();
            foreach (int v in Enum.GetValues(typeof(eTitleType)))
            {
                if (v != _maxType)
                    DataModel.MenuState.Add(v,v==(int)_showType);
            }

            //for (var i = (int)eTitleType.Strength; i < _maxType; i++)
            //{
            //    DataModel.MenuState[i] = i == (int)_showType;
            //}
            SelectGroupId = 3019;
            choosedTitleIndex = 1;
            unFoldTitleNum = 0;
            //  UpgradeSelectToClean();
            DataModel.ItemSelected = GetSetItem(Group[SelectGroupId][0]);
        
            //DataModel.ShowSelectTitle = Table.GetNameTitle(SelectGroupId).Name;

            clickTitleListIndex = 0;
            DataModel.ItemSelected = DataModel.Lists[clickTitleListIndex];//开始就默认点击第一个称号总目录
            DataModel.ItemShowed = DataModel.Lists[clickTitleListIndex];
            SelectedTitleId = DataModel.Lists[clickTitleListIndex].Id;
            GetGroupId(DataModel.ItemSelected);
            NetManager.Instance.StartCoroutine(InitClick());

        }

        private IEnumerator InitClick()
        {
            while (ReceivedIndex == 0)
            {
                yield return new WaitForEndOfFrame();
            }
            clickBrachId = ReceivedIndex;
            RecIdxCliTtalCha = ReceivedIndex;
            NetManager.Instance.StartCoroutine(UpgradeSelectToCleanCortoutine());
        }

        private void ShowPuton()
        {
            var stateList = PlayerDataManager.Instance.TitleList;
            for (int i = 0; i < DataModel.MenuDatas.Count; i++)
            {
                var id = DataModel.MenuDatas[i].TableId;
                if (stateList.ContainsValue(id))
                {
                    DataModel.MenuDatas[i].State = (int)TitleState.PutOn;
                }
            }  
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public void OnShow()
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "SendMilitary")
            {
                ReceiveMilitary = int.Parse(param[0].ToString());
            }
            return null;
        }

        public FrameState State { get; set; }
    }
}