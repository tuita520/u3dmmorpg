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
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class ElfController : IControllerBase
    {
        public ElfController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInit);
            EventDispatcher.Instance.AddEventListener(ElfExdataUpdate.EVENT_TYPE, OnUpdateExData);
            EventDispatcher.Instance.AddEventListener(ElfCellClickEvent.EVENT_TYPE, OnElfCellClick);
            EventDispatcher.Instance.AddEventListener(ElfCell1ClickEvent.EVENT_TYPE, OnElfCell1Click);
            EventDispatcher.Instance.AddEventListener(ElfFlyOverEvent.EVENT_TYPE, OnElfFlyOver);
            EventDispatcher.Instance.AddEventListener(ElfOperateEvent.EVENT_TYPE, OnElfOperate);
            EventDispatcher.Instance.AddEventListener(ElfReplaceEvent.EVENT_TYPE, OnElfReplace);
            EventDispatcher.Instance.AddEventListener(ElfGetOneShowEvent.EVENT_TYPE, OnClickGetShow);
            EventDispatcher.Instance.AddEventListener(ElfShowCloseEvent.EVENT_TYPE, OnClickShowClose);
            EventDispatcher.Instance.AddEventListener(ElfGetDrawResult.EVENT_TYPE, GetDrawResult);
            EventDispatcher.Instance.AddEventListener(ElfOneDrawInfoEvent.EVENT_TYPE, ShowOneDrawInfo);
            EventDispatcher.Instance.AddEventListener(UIEvent_ElfShowDrawGetEvent.EVENT_TYPE, ShowDrawGet);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChanged);
            EventDispatcher.Instance.AddEventListener(ElfOnClickShowSkillTips.EVENT_TYPE, OnClickShowSkillTips);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDateUpdate);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDateInit);
            EventDispatcher.Instance.AddEventListener(ExData64InitEvent.EVENT_TYPE, OnExDataInit64Event);
            EventDispatcher.Instance.AddEventListener(ExData64UpDataEvent.EVENT_TYPE, OnExdateUpdate64Event);

        }

        private const int MAX_SKILLCOUNT = 3;
        private int BagItemCount;
        private ElfDataModel DataModel;
        private readonly ElfItemDataModelComparer ElfItemComparer = new ElfItemDataModelComparer();
        private bool IsOneDraw;
        private readonly int MaxElfFightCount = 3;
        private bool mIsSetOffset;
        private Coroutine mTimeCoroutine;
        private int SelectElfIndex;
        private int selectSkillIndex = 0;
        private int selectSkillBookIndex = -1;
        private static int s_PetBookItemType = 26900;
        private int displayElfId = -1;
        Dictionary<int, BagItemDataModel> skillbookBagMap = new Dictionary<int, BagItemDataModel>();
        private Dictionary<int, TotalCount> elfCountDictionary = new Dictionary<int, TotalCount>();
        private Coroutine ButtonPress { get; set; }
        private TotalCount GetElfTotalCount(int itemId)
        {
            TotalCount totalCount;
            elfCountDictionary.TryGetValue(itemId, out totalCount);
            if (totalCount == null)
            {
                totalCount = new TotalCount();
                elfCountDictionary.Add(itemId, totalCount);
            }
            return totalCount;
        }

        private void ClearElfTotalCount()
        {
            elfCountDictionary.Clear();
        }

        private void AddElfTotalCount(int itemId, int count)
        {
            var totalCount = GetElfTotalCount(itemId);
            totalCount.Count += count;
            RefreshStarInfo();
        }

        private int CheckElfType(int elfId, int targetIndex)
        {
            var sameIndex = 0;
            var nowFightCount = DataModel.FightElfCount;
            if (targetIndex > nowFightCount)
            {
                return 0;
            }
            var tbElf = Table.GetElf(elfId);
            if (tbElf == null)
            {
                return 0;
            }
            for (var i = 0; i < nowFightCount; i++)
            {
                if (i == targetIndex)
                {
                    continue;
                }
                var id = DataModel.Formations[i].ElfData.ItemId;
                if (id == -1)
                {
                    continue;
                }
                var tb = Table.GetElf(id);
                if (tb == null)
                {
                    continue;
                }
                if (tbElf.ElfType == tb.ElfType)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool ElfIdInFormation(int itemId)
        {
            for (var i = 0; i < MaxElfFightCount; i++)
            {
                var item = DataModel.Formations[i].ElfData;
                if (item.ItemId == itemId)
                {
                    return true;
                }
            }
            return false;
        }

        private void ElfOperate(int type)
        {
            ElfOperate(DataModel.SelectElf, type);
        }
        private int CalcResolveElf(int elfId, int level, int starLevel)
        {
            var tbElf = Table.GetElf(elfId);
            if (tbElf == null)
                return 0;

            var tbLevel = Table.GetLevelData(level);
            if (tbLevel == null)
                return 0;

            var getValue = (tbLevel.ElfResolveValue / 100.0f * tbElf.ResolveCoef[0]) + tbElf.ResolveCoef[1] * (1 + starLevel);

            return (int)getValue;
        }

        private void RecycleAllElf()
        {
            var getItem = 0;
            var elfIndexList = new List<int>();
            for (var i = 0; i < DataModel.ElfList.Count; ++i)
            {
                var elfData = DataModel.ElfList[i];
                if (elfData.Index < 3)
                    continue;
                var tbElf = Table.GetElf(elfData.ItemId);
                if (tbElf == null)
                {
                    continue;
                }

                var tbItem = Table.GetItemBase(elfData.ItemId);
                if (tbItem == null || tbItem.Quality >= 3)
                {
                    continue;
                }
            
                elfIndexList.Add(elfData.Index);
                getItem += CalcResolveElf(tbElf.Id, elfData.Exdata.Level, elfData.Exdata.StarLevel);
            }

            if (elfIndexList.Count > 0)
            {
                var tbCallbackItem = Table.GetItemBase(7);
                if (tbCallbackItem == null)
                {
                    return;
                }
                var color = GameUtils.GetTableColorString(tbCallbackItem.Quality);
                var name = String.Format("[{0}][{1}][-]", color, tbCallbackItem.Name);

                var str = string.Format(GameUtils.GetDictionaryText(225000), name, getItem);

                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "",
                    () => { NetManager.Instance.StartCoroutine(ResolveElfCoroutine(elfIndexList)); });
            }        
        }

        private IEnumerator ResolveElfCoroutine(List<int> elfIndexList)
        {
            using (new BlockingLayerHelper(0))
            {
                var elfs = new Int32Array();
                elfs.Items.AddRange(elfIndexList);
                var msg = NetManager.Instance.ResolveElfList(elfs);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var count = msg.Response;
                        //分解成功，获得{0}精魄
                        var str = string.Format(GameUtils.GetDictionaryText(240310), count);
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(str));
                        mIsSetOffset = true;
                        DataModel.Skill.ShowRecycleAnim = false;
                        DataModel.Skill.ShowRecycleAnim = true;
                    }
                }
            }
        }

        private void ElfOperate(ElfItemDataModel elfData, int type, int targetIndex = -1)
        {
            var data = elfData;
            if (data.ItemId == -1)
            {
                return;
            }
            var tbItem = Table.GetItemBase(data.ItemId);
            var tbElf = Table.GetElf(tbItem.Exdata[0]);
            var nowFightCount = DataModel.FightElfCount;
            switch (type)
            {
                case 0:
                {
                    if (elfData.Index < 0)
                    {
                        return;
                    }
                    if (elfData.Index >= MaxElfFightCount)
                    {
                        //精灵不在战斗
                        var e = new ShowUIHintBoard(240304);
                        EventDispatcher.Instance.DispatchEvent(e);
                        return;
                    }
                }
                    break;
                case 1:
                {
                    if (nowFightCount == 1 || targetIndex >= nowFightCount)
                    {
                        GameUtils.ShowHintTip(240311);
                        return;
                    }
                    if (targetIndex == -1)
                    {
                        for (var i = 1; i < nowFightCount; i++)
                        {
                            if (DataModel.Formations[i].ElfData.ItemId == -1)
                            {
                                targetIndex = i;
                                break;
                            }
                        }
                    }
                    if (elfData.Index == targetIndex)
                    {
                        return;
                    }
                    if (targetIndex == -1)
                    {
                        targetIndex = 1;
                    }
                    if (targetIndex < 1 || targetIndex > 2)
                    {
                        return;
                    }
                }
                    break;
                case 2:
                {
                    if (elfData.Index == 0)
                    {
                        //"上阵精灵已在展示中"
                        var e = new ShowUIHintBoard(240305);
                        EventDispatcher.Instance.DispatchEvent(e);
                        return;
                    }
                    if (elfData.Index >= MaxElfFightCount)
                    {
                        if (CheckElfType(elfData.ItemId, 0) != -1)
                        {
                            GameUtils.ShowHintTip(240312);
                            return;
                        }
                    }
                }
                    break;
                case 4:
                {
                    var tbLevel = Table.GetLevelData(DataModel.FormationLevel);
                    if (tbLevel.FightingWayExp <= 0)
                    {
                        GameUtils.ShowHintTip(200002906);
                        return;
                    }
//                if (tbLevel.FightingWayExp > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.ElfPiece)
//                {
////阵法经验不足
//                    GameUtils.ShowHintTip(200002907);
//                    return;
//                }
                    if (!GameUtils.CheckEnoughItems(7, tbLevel.FightingWayExp))
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(240307));
                        return;
                    }
                }
                    break;
                case 5:
                {
                    if (elfData.Exdata.Level == tbElf.MaxLevel || (PlayerDataManager.Instance.GetLevel() <= elfData.Exdata.Level && tbElf.MaxLevel == -1))
                    {
                        //精灵已达到最大的等级
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(240306));
                        return;
                    }

                    //if (elfData.LvExp > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.ElfPiece)
                    //{
                    //没有足够精魂可供升级
                    if (!GameUtils.CheckEnoughItems(7, elfData.LvExp))
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(240307));
                        return;
                    }
                    //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.QuickBuyUi)); 
                    //}
                }
                    break;
                case 6:
                {
                    if (elfData.Index <= 2)
                    {
                        //不能分解出战精灵
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(240308));
                        return;
                    }

                    if (tbItem.Quality >= 3 || elfData.Exdata.Level > 1)
                    {
                        //是否确认分解,获得{0}精魄
                        var count = CalcResolveElf(tbElf.Id, data.Exdata.Level, data.Exdata.StarLevel);
                        var str = string.Format(GameUtils.GetDictionaryText(240309), count);
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "",
                            () => { NetManager.Instance.StartCoroutine(ElfOperateCoroutine(type, elfData, 0)); });
                        return;
                    }
                }
                    break;
                case 7:
                {
                    //var haveRes = PlayerDataManager.Instance.GetRes(DataModel.StarCostResId);
                    //if (haveRes < DataModel.StarCostResCount)
                    //{
                    //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    //    return;
                    //}
                    if (!GameUtils.CheckEnoughItems(7, DataModel.StarCostResCount))
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                        return;
                    }
                    var costElfs = new List<ElfItemDataModel>();
                    for (var i = 0; i < DataModel.StarCostItems.Count; ++i)
                    {
                        var itemId = DataModel.StarCostItems[i].ItemId;
                        if (itemId == -1)
                            continue;

                        var itemCount = DataModel.StarCostItems[i].Count;
                        var totalCount = DataModel.StarCostItems[i].TotalCount;
                        if (totalCount < itemCount)
                        {
                            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                            PlayerDataManager.Instance.ShowItemInfoGet(itemId);
                            return;
                        }

                        var bagList = new List<ElfItemDataModel>();
                        var count = DataModel.ElfBag.Count;
                        for (var j = 3; j < count; ++j)
                        {
                            var elf = DataModel.ElfBag[j];
                            if (elf == null || elf.ItemId != itemId || elf.IsSelect)
                            {
                                continue;
                            }

                            bagList.Add(elf);
                        }

                        bagList.Sort((a, b) =>
                        {
                            return (a.Exdata.StarLevel < b.Exdata.StarLevel) ? -1 : 1;
                        });

                        if (itemCount > bagList.Count) // 不考虑叠加
                            return;

                        for (var j = 0; j < itemCount; ++j)
                        {
                            costElfs.Add(bagList[j]);
                        }
                    }

                    var notice = "";
                    var enumorator = costElfs.GetEnumerator();
                    while (enumorator.MoveNext())
                    {
                        var item = enumorator.Current;
                        var tbItemElf = Table.GetItemBase(item.ItemId);
                        if (tbItemElf != null && item.Exdata.StarLevel > 0)
                        {
                            var color = GameUtils.GetTableColorString(tbItemElf.Quality);
                            var name = String.Format("[{0}][{1}][-]", color, tbItemElf.Name);
                            notice += string.Format(GameUtils.GetDictionaryText(100002172), item.Exdata.StarLevel, name);
                        }
                    }
                    if (!string.IsNullOrEmpty(notice))
                    {
                        var noticeStr = string.Format(GameUtils.GetDictionaryText(100002173), notice);
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, noticeStr, "", () =>
                        {
                            NetManager.Instance.StartCoroutine(ElfOperateCoroutine(type, elfData, targetIndex));
                        });
                        return;
                    }
                }
                    break;
                case 8:
                {
                    var items = new Dictionary<int, int>();
                    for (var i = 0; i < DataModel.Skill.CostItems.Count; ++i)
                    {
                        var itemId = DataModel.Skill.CostItems[i].ItemId;
                        if (itemId == -1)
                            continue;
                        var itemCount = DataModel.Skill.CostItems[i].Count;
                        items.modifyValue(itemId, itemCount);
                    }

                    if (!GameUtils.CheckEnoughItems(items))
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                        return;
                    }
                }
                    break;
            }
            NetManager.Instance.StartCoroutine(ElfOperateCoroutine(type, elfData, targetIndex));
        }

        private IEnumerator ElfOperateCoroutine(int type, ElfItemDataModel elfData, int targetIndex)
        {
            if (elfData == null)
            {
                yield break;
            }
            using (new BlockingLayerHelper(0))
            {
                var index = elfData.Index;
                var tbElf = Table.GetElf(elfData.ItemId);
                var msg = NetManager.Instance.ElfOperate(index, type, targetIndex);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        switch (type)
                        {
                            case 0:
                            {
                                var retIndex = GetFreeIndex();
                                EventDispatcher.Instance.DispatchEvent(new ElfFlyEvent(elfData.Index, retIndex, true));
                            }
                                break;
                            case 1:
                            {
                                var elfData1 = DataModel.ElfBag[targetIndex];
                                var fromIdx = elfData.Index;
                                var toIdx = targetIndex;
                                var needCallBack = (elfData1.ItemId != -1 && fromIdx < MaxElfFightCount) ||
                                                   fromIdx < MaxElfFightCount;
                                if (!needCallBack)
                                {
                                    MoveElfBag(fromIdx, toIdx);
                                }
                                EventDispatcher.Instance.DispatchEvent(new ElfFlyEvent(fromIdx, toIdx, needCallBack));
                            }
                                break;
                            case 2:
                            {
                                var elfData1 = DataModel.ElfBag[targetIndex];
                                var fromIdx = elfData.Index;
                                var toIdx = targetIndex;

                                //--------------------------------------------之前上面有了就换一下，因为客户端不刷新
                                if (elfData1 != null && elfData1.ItemId != -1)
                                {
                                    var tbtempElf = Table.GetElf(elfData1.ItemId);
                                    if (tbtempElf != null && tbtempElf.ElfType != tbElf.ElfType) //类型冲突就不找了 直接休息吧
                                    {
                                        for (var i = 1; i != DataModel.FightElfCount; ++i)
                                        {
                                            var tempbatItem = DataModel.ElfBag[i];
                                            if (tempbatItem == null || tempbatItem.ItemId == -1)
                                            {
                                                MoveElfBag(0, i);
                                                break;
                                            }
                                        }
                                    }
                                }
                                //----------------------------------------------------之间是新加的

                                var needCallBack = (elfData1.ItemId != -1 && fromIdx < MaxElfFightCount) ||
                                                   fromIdx < MaxElfFightCount;
                                if (!needCallBack)
                                {
                                    MoveElfBag(fromIdx, toIdx);
                                }
                                EventDispatcher.Instance.DispatchEvent(new ElfFlyEvent(fromIdx, toIdx, needCallBack));
                                ElfSort();
                                PlatformHelper.UMEvent("PetFight", tbElf.ElfName, elfData.Exdata.Level.ToString());
                            }
                                break;
                            case 4: //Formation
                            {
                                EventDispatcher.Instance.DispatchEvent(new FormationLevelupEvent());

                                var level = (int) msg.Response;
                                UpdateFormationLevel(level);
                            }
                                break;
                            case 5: //Enchance
                            {
                                EventDispatcher.Instance.DispatchEvent(new ElfLevelupEvent());

                                elfData.Exdata.Level = (int) msg.Response;
                                var tbLevel = Table.GetLevelData(elfData.Exdata.Level);
                                elfData.LvExp = tbLevel.ElfExp*tbElf.ResolveCoef[0]/100;
                                RefreshElfAttribute(DataModel.SelectElf);
                                PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Elf);

                                //遍历所有宠物 确定当前升级是否为等级最高的宠物
                                bool isThisMaxLevel = true;
                                foreach (var item in DataModel.Items)
                                {
                                    if (item.Exdata.Level >= elfData.Exdata.Level && item.Index != elfData.Index)
                                    {
                                        isThisMaxLevel = false;
                                        break;
                                    }
                                }
                                if (isThisMaxLevel)
                                {
                                    PlatformHelper.UMEvent("PetLevel", elfData.Exdata.Level.ToString());
                                }
                            }
                                break;
                            case 6: //Resolve
                            {
                                var count = msg.Response;
                                //分解成功，获得{0}精魄
                                var str = string.Format(GameUtils.GetDictionaryText(240310), count);
                                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(str));
                                mIsSetOffset = true;
                                DataModel.Skill.ShowRecycleAnim = false;
                                DataModel.Skill.ShowRecycleAnim = true;
                                PlatformHelper.UMEvent("PetRecycle", tbElf.ElfName, elfData.Exdata.Level.ToString());
                            }
                                break;
                            case 7:
                            {
                                EventDispatcher.Instance.DispatchEvent(new ElfLevelupEvent());

                                elfData.Exdata.StarLevel = (int)msg.Response;
                                DataModel.CurrentStar = elfData.Exdata.StarLevel;
                                RefreshStarInfo();
                                RefreshModel(elfData.ItemId, elfData.Exdata.StarLevel, false);

                                PlatformHelper.UMEvent("PetStar", tbElf.ElfName, elfData.Exdata.StarLevel.ToString());
                            }
                                break;
                            case 8:
                            {
                                EventDispatcher.Instance.DispatchEvent(new ElfSkillEvent(0));

                                var exid = (int) ElfExdataDefine.BuffLevel1 + targetIndex*2;
                                if (exid < elfData.Exdata.Count)
                                {
                                    elfData.Exdata[exid] = (int)msg.Response;
                                    if (DataModel.Skill.SelectElf.Index == index && selectSkillIndex == targetIndex)
                                    {
                                        var skill = DataModel.Skill.SkillInfos[selectSkillIndex];
                                        RefreshSkillData(skill);
                                        RefreshSkillInfo();
                                        //RefreshSelectSkill(targetIndex);
                                        skill.PlayAnim = 0;
                                        skill.PlayAnim = 2;
                                    }
                                }
                                PlatformHelper.UMEvent("PetSkillLevelUp", tbElf.ElfName, elfData.Exdata.StarLevel.ToString());                            
                            }
                                break;
                        }
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.Error_ElfBattleMax)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error("----------------msg.ErrorCode---------{0}", msg.ErrorCode);
                    }
                    else if (msg.ErrorCode == (int) ErrorCodes.Error_ElfNotFind
                             || msg.ErrorCode == (int) ErrorCodes.Error_ElfAlreadyBattle
                             || msg.ErrorCode == (int) ErrorCodes.Error_ElfNotBattle
                             || msg.ErrorCode == (int) ErrorCodes.Error_ElfNotBattleMain
                             || msg.ErrorCode == (int) ErrorCodes.Error_ElfIsBattleMain)
                    {
                        //状态错误，重新请求数据
                        var e = new ShowUIHintBoard(270084);
                        EventDispatcher.Instance.DispatchEvent(e);
                        ReApplyBagData();
                        Logger.Error("----------------msg.ErrorCode---------{0}", msg.ErrorCode);
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error("----------------msg.ErrorCode---------{0}", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("----------------msg.State---------{0}", msg.State);
                }
            }
        }

        private void ElfReplace(int f, int t)
        {
            var from = GetElfItemDataModel(f);
            var to = GetElfItemDataModel(t);
            if (from == null || to == null)
            {
                return;
            }
            if (from.ItemId == -1 && to.ItemId == -1)
            {
                return;
            }
            if (to.Index > 2)
            {
                return;
            }

            if (to.Index == 0)
            {
                ElfOperate(from, 2);
            }
            else if (from.Index == 0)
            {
                if (to.ItemId == -1)
                {
                    ElfOperate(from, 1, to.Index);
                }
                else
                {
                    ElfOperate(to, 2);
                }
            }
            else
            {
                ElfOperate(from, 1, to.Index);
            }
            // NetManager.Instance.StartCoroutine(ElfReplaceCoroutine( from, to));
        }

        private void GetElfFightCount()
        {
            var oldCount = DataModel.FightElfCount;
            var newCount = 0;
            if (PlayerDataManager.Instance.CheckCondition(GameUtils.ElfSecondCondition) != 0)
            {
                newCount = 1;
            }
            else if (PlayerDataManager.Instance.CheckCondition(GameUtils.ElfThirdCondition) != 0)
            {
                newCount = 2;
            }
            else
            {
                newCount = MaxElfFightCount;
            }

            DataModel.FightElfCount = newCount;
            var formations = DataModel.Formations;
            formations[1].IsLocked = newCount <= 1 && DataModel.ElfBag[1].ItemId <= 0;
            formations[2].IsLocked = newCount <= 2 && DataModel.ElfBag[2].ItemId <= 0;
        }

        private ElfItemDataModel GetElfItemDataModel(int index)
        {
            if (index < 0 || index >= DataModel.ElfBag.Count)
            {
                return null;
            }
            return DataModel.ElfBag[index];
        }

        private int GetFreeIndex()
        {
            var c = DataModel.ElfBag.Count;
            for (var i = MaxElfFightCount; i < c; i++)
            {
                var data = DataModel.ElfBag[i];
                if (data.ItemId == -1)
                {
                    return data.Index;
                }
            }
            return -1;
        }

        private void InitElfBag(BagBaseData bagData)
        {
            var tbBagBase = Table.GetBagBase(4);
            if (tbBagBase == null)
            {
                return;
            }
            var listItem = new List<ElfItemDataModel>();
            for (var i = 0; i < tbBagBase.MaxCapacity + 3; i++)
            {
                var itemData = new ElfItemDataModel();
                itemData.Index = i;
                listItem.Add(itemData);
            }
            DataModel.MaxElfCount = tbBagBase.InitCapacity;
            DataModel.ElfBag = new List<ElfItemDataModel>(listItem);
            var list = bagData.Items;
            var listCount = list.Count;
            BagItemCount = listCount;

            ClearElfTotalCount();
            for (var i = 0; i < listCount; ++i)
            {
                var item = list[i];
                if (item.Index < 0 || item.Index >= DataModel.ElfBag.Count)
                {
                    continue;
                }
                var itemData = DataModel.ElfBag[item.Index];
                itemData.ItemId = item.ItemId;
                itemData.Exdata.InstallData(item.Exdata);
                SetElfLevelExp(itemData);
                if (item.Index >= MaxElfFightCount)
                {
                    AddElfTotalCount(item.ItemId, 1);
                }
                RefreshElfAttribute(itemData);
            }
            GetElfFightCount();
            SortAndRefreshElf();
        }

        private void MoveElfBag(int from, int to)
        {
            var bag = DataModel.ElfBag;

            var fromData = bag[from];
            var toData = bag[to];

            if (fromData.Index >= MaxElfFightCount && toData.Index < MaxElfFightCount)
            {
                if (fromData.ItemId != toData.ItemId)
                {
                    AddElfTotalCount(fromData.ItemId, -1);
                    AddElfTotalCount(toData.ItemId, 1);
                }
            }
            else if (fromData.Index < MaxElfFightCount && toData.Index >= MaxElfFightCount)
            {
                if (fromData.ItemId != toData.ItemId)
                {
                    AddElfTotalCount(toData.ItemId, -1);
                    AddElfTotalCount(fromData.ItemId, 1);
                }
            }

            toData.Index = from;
            fromData.Index = to;

            bag[from] = toData;
            bag[to] = fromData;

            SetFormationElf(from, toData);
            SetFormationElf(to, fromData);
            RefreshShowElf(false);
            GetElfFightCount();
        }

        private void OnElfCell1Click(IEvent ievent)
        {
            if (DataModel.IsAnimating)
            {
                return;
            }

            var e = ievent as ElfCell1ClickEvent;
            var data = e.DataModel;
            DataModel.SelectElf = data;
            var formations = DataModel.Formations;
            var selIdx = 0;
            foreach (var formation in formations)
            {
                if (formation.IsSelect)
                {
                    break;
                }
                ++selIdx;
            }
            if (selIdx == formations.Count)
            {
                Logger.Error("In OnElfCell1Click(),no formation elf selected!");
            }
            else if (data.Index == selIdx)
            {
                ElfOperate(data, 0, selIdx);
            }
            else if (selIdx == 0)
            {
                ElfOperate(data, 2, selIdx);
            }
            else
            {
                ElfOperate(data, 1, selIdx);
            }
        }

        private void OnElfCellClick(IEvent ievent)
        {
            var e = ievent as ElfCellClickEvent;
            var data = e.DataModel;
            if (DataModel.TabIndex == 2)
            {
                RefreshAllSkillInfo(e.Index);
            }
            else
            {
                SelectElfIndex = e.Index;
                SetSelectElf(data);            
            }
        }

        private void OnElfFlyOver(IEvent ievent)
        {
            var e = ievent as ElfFlyOverEvent;
            MoveElfBag(e.FromIdx, e.ToIdx);
        }

        private bool OnAdd()
        {
            if (DataModel.Skill.RecycleCount < DataModel.Skill.ItemData.Count)
            {
                DataModel.Skill.RecycleCount++;
                DataModel.Skill.RecycleRate = (float)(DataModel.Skill.RecycleCount - 1) / (DataModel.Skill.ItemData.Count - 1);
                return true;
            }
            return false;
        }

        private bool OnDel()
        {
            if (DataModel.Skill.RecycleCount > 1)
            {
                DataModel.Skill.RecycleCount--;
                DataModel.Skill.RecycleRate = (float)(DataModel.Skill.RecycleCount - 1) / (DataModel.Skill.ItemData.Count - 1);
                return true;
            }
            return false;
        }

        private IEnumerator ButtonAddOnPress()
        {
            var pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(pressCd);
                if (OnAdd() == false)
                {
                    NetManager.Instance.StopCoroutine(ButtonPress);
                    ButtonPress = null;
                    yield break;
                }
                if (pressCd > 0.01)
                {
                    pressCd = pressCd * 0.8f;
                }
            }
            yield break;
        }

        private IEnumerator ButtonDelOnPress()
        {
            var pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(pressCd);
                if (OnDel() == false)
                {
                    NetManager.Instance.StopCoroutine(ButtonPress);
                    ButtonPress = null;
                    yield break;
                }
                if (pressCd > 0.01)
                {
                    pressCd = pressCd * 0.8f;
                }
            }
            yield break;
        }


        private void OnElfOperate(IEvent ievent)
        {
            var e = ievent as ElfOperateEvent;
            switch (e.Type)
            {
                case 0: //disfight
                {
                    ElfOperate(0);
                }
                    break;
                case 1: //fight
                {
                    ElfOperate(1);
                }
                    break;
                case 2: //Show
                {
                    ElfOperate(2);
                }
                    break;
                case 3: //
                {
                    //ElfOperate(3);
                }
                    break;
                case 10:
                {
                    ElfOperate(4);
                }
                    break;
                case 11:
                {
                    ElfOperate(5);
                }
                    break;
                case 12: //回收
                {
                    ElfOperate(6);
                }
                    break;
                case 13: //升星
                {
                    ElfOperate(7);
                }
                    break;
                case 14:
                { // 技能升级
                    if (selectSkillIndex >= DataModel.Skill.SkillInfos.Count)
                    {
                        return;
                    }

                    var skillInfo = DataModel.Skill.SkillInfos[selectSkillIndex];
                    ElfOperate(DataModel.Skill.SelectElf, 8, skillInfo.ExdataId);
                }
                    break;
                case 15:
                { // 回收技能书
                    OnClickRecycle();
                }
                    break;
                case 16:
                {
                    NetManager.Instance.StartCoroutine(RecycleItemCorotion());
                }
                    break;
                case 17:
                {
                    DataModel.Skill.ShowRecycleBox = false;
                }
                    break;
                case 18:
                { // 替换
                    var bookItem = DataModel.Skill.SkillBook[selectSkillBookIndex];
                    var useItemId = bookItem.ItemId;
                    var itemBuffId = GetItemBuffId(useItemId);
                    var tbBuff = Table.GetBuff(itemBuffId);
                    if (tbBuff == null)
                        return;

                    var str = "";
                    var selElf = GetSkillSelectElf();
                    var buffId = GameUtils.GetElfBuffId(selElf, selectSkillIndex);
                    var buffLvl = GameUtils.GetElfBuffLevel(selElf, selectSkillIndex);
                    if (buffId != -1)
                    {
                        var currentBuff = "";
                        var tbBuff2 = Table.GetBuff(buffId);
                        if (tbBuff2 != null)
                        {
                            currentBuff = tbBuff2.Name;
                        }
                        str = string.Format(GameUtils.GetDictionaryText(100002141), currentBuff, buffLvl, tbBuff.Name, 1);
                    }
                    else
                    {
                        str = string.Format(GameUtils.GetDictionaryText(100002213), tbBuff.Name, 1);
                    }

                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "", () =>
                    {
                        NetManager.Instance.StartCoroutine(ReplaceSkillCoroutine(selElf, selElf.Index, selectSkillIndex,
                            bookItem.BagItem.BagId, bookItem.BagItem.Index));
                    });
                }
                    break;
                case 19:
                {
                    var itemString = Table.GetClientConfig(1209).Value;
                    var itemIds = itemString.Split('|');
                    var index = UiIndexToExIdx(selectSkillIndex);
                    if (index >= 0 && index < itemIds.Length)
                    {
                        GameUtils.ShowQuickBuy(int.Parse(itemIds[index]), 1);                
                    }
                }
                    break;
                case 20: //formation info
                {
                    if (!DataModel.ShowElfList)
                    {
                        DataModel.ShowFormationInfo = !DataModel.ShowFormationInfo;
                        EventDispatcher.Instance.DispatchEvent(new ElfPlayAnimationEvent(0, DataModel.ShowFormationInfo,
                            false));
                    }
                }
                    break;
                case 21: //close formation info
                {
                    if (DataModel.ShowFormationInfo)
                    {
                        DataModel.ShowFormationInfo = false;
                        EventDispatcher.Instance.DispatchEvent(new ElfPlayAnimationEvent(0, false, false));
                    }
                }
                    break;
                case 22: //elf list
                {
                    if (DataModel.ShowElfList)
                    {
                        DataModel.ShowElfList = false;
                        EventDispatcher.Instance.DispatchEvent(new ElfPlayAnimationEvent(1, false, false));
                    }
                }
                    break;
                case 23:
                {
                    RecycleAll();
                }
                    break;
                case 24:
                {
                    RecycleAllElf();
                }
                    break;
                case 25:
                {
                    DataModel.ShowStarUi = !DataModel.ShowStarUi;
                }
                    break;
                case 26:
                { // 60,0,4
                    var argList = new List<int>();
                    argList.Add(4);
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ActivityUI, new ActivityArguments
                    {
                        Tab = 0,
                        Args = argList
                    }));
                }
                    break;
                case 30: //显示精灵信息0
                case 31: //显示精灵信息1
                case 32: //显示精灵信息2
                {
                    var formations = DataModel.Formations;
                    var formation = formations[e.Type - 30];
                    SetSelectElf(formation.ElfData);
                    var selIdx = DataModel.Items.IndexOf(DataModel.SelectElf);
                    if (selIdx > 0)
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_ElfSetGridLookIndex(selIdx));
                    }
                    DataModel.TabIndex = 1;
                    DataModel.OnPropertyChanged("TabIndex");
                }
                    break;
                case 41: //展示精灵1
                case 42: //展示精灵2
                {
                    ElfOperate(DataModel.ElfBag[e.Type - 40], 2, 0);
                }
                    break;
                case 50: //cell被点击0
                case 51: //cell被点击1
                case 52: //cell被点击2
                {
                    var formations = DataModel.Formations;
                    var formation = formations[e.Type - 50];
                    if (formation.IsLocked)
                    {
                        GameUtils.ShowHintTip(240311);
                        return;
                    }
                    foreach (var f in formations)
                    {
                        if (f != formation)
                        {
                            f.IsSelect = false;
                        }
                    }

                    var newValue = !formation.IsSelect;
                    formation.IsSelect = newValue;
                    if (DataModel.ShowElfList != newValue)
                    {
                        DataModel.ShowElfList = newValue;
                        EventDispatcher.Instance.DispatchEvent(new ElfPlayAnimationEvent(1, newValue, false));
                    }
                }
                    break;
                case 59:
                { // 技能
                    if (!RefreshIsShowElfOrSkillPage())
                    {
                        DataModel.TabIndex = 2;
                        if (SelectElfIndex < 0)
                            SelectElfIndex = 0;
                        RefreshAllSkillInfo(SelectElfIndex);
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002224));
                    }
                }
                    break;
                case 60: //tab 0 被点击
                {
                    DataModel.TabIndex = 0;
                    ResetUI();
                    for (int i = 0, imax = DataModel.Formations.Count; i < imax; ++i)
                    {
                        RefreshFormationModel(i);
                    }
                }
                    break;
                case 61: //tab 1 被点击
                {
                    PlayerDataManager.Instance.WeakNoticeData.ElfCanUpgrade = false;
                    if (!RefreshIsShowElfOrSkillPage())
                    {
                        DataModel.TabIndex = 1;
                        RefreshShowElf(false, 0);
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002224));
                    }
                }
                    break;
                case 62: //tab 2 被点击
                {
                    DataModel.TabIndex = 3;
                }
                    break;
                case 63: //抽奖精灵显示
                case 64:
                case 65:
                case 66:
                case 67:
                case 68:
                    ShowElfItemInfo(e.Type - 63);
                    break;
                case 70:
                { // Add Press
                    ButtonPress = NetManager.Instance.StartCoroutine(ButtonAddOnPress());
                }
                    break;
                case 71:
                { //Add Release
                    if (ButtonPress != null)
                    {
                        NetManager.Instance.StopCoroutine(ButtonPress);
                        ButtonPress = null;
                    }
                }
                    break;
                case 72:
                { // Del Press
                    ButtonPress = NetManager.Instance.StartCoroutine(ButtonDelOnPress());
                }
                    break;
                case 73:
                { //Del Release
                    if (ButtonPress != null)
                    {
                        NetManager.Instance.StopCoroutine(ButtonPress);
                        ButtonPress = null;
                    }
                }
                    break;
                case 74:
                { // add
                    OnAdd();
                }
                    break;
                case 75:
                { // del
                    OnDel();
                }
                    break;
            }
        }

        private bool RefreshIsShowElfOrSkillPage()
        {
            return DataModel.IsShowMaskUi = !(DataModel.ElfBag.Where(d => d.ItemId != -1).ToList().Count > 0);    
        }

        private void OnElfReplace(IEvent ievent)
        {
            var e = ievent as ElfReplaceEvent;
            ElfReplace(e.From, e.To);
        }

        private void OnExDataInit(IEvent ievent)
        {
            var lv = PlayerDataManager.Instance.GetExData(eExdataDefine.e82);
            UpdateFormationLevel(lv);
            var ResCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.ElfPiece);
            CheckJingPoTip(ResCount);
            CheckElfInfoNotice();
        }
        private void CheckJingPoTip(int ElfPieceCount)
        {
            if (ElfPieceCount >= DataModel.JingPoTipCount)
            {
                PlayerDataManager.Instance.NoticeData.ElfJingPoTip = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.ElfJingPoTip = false;
            }
        }
        private void CheckElfInfoNotice()
        {
            var JingPoResCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.ElfPiece);
            var ElfcCount = DataModel.ElfBag.Count;
            DataModel.ElfIsCanLevelUp.Clear();
            var PlayerLevel = PlayerDataManager.Instance.GetLevel();
            for (var i = 0; i < ElfcCount; i++)
            {
                var elf = DataModel.ElfBag[i];
                if (elf == null || elf.ItemId == -1 || elf.LvExp < 1 || (elf.State != 2 && elf.State != 1))
                {
                    continue;
                }
                if (elf.Exdata.Level >= PlayerLevel)
                {
                    DataModel.ElfIsCanLevelUp.Add(false);
                }
                else
                {
                    if (elf.LvExp <= JingPoResCount)
                    {
                        DataModel.ElfIsCanLevelUp.Add(true);
                    }
                    else
                    {
                        DataModel.ElfIsCanLevelUp.Add(false);
                    } 
                }
            }
            if (DataModel.ElfIsCanLevelUp.Contains(true))
            {
                PlayerDataManager.Instance.WeakNoticeData.ElfCanUpgrade = true;
            }
            else
            {
                PlayerDataManager.Instance.WeakNoticeData.ElfCanUpgrade = false;
            }
        }
        private void CheckElfCanLevelUp(ElfItemDataModel elfItem)
        {
            var JingPoResCount = PlayerDataManager.Instance.GetRes((int)eResourcesType.ElfPiece);
            if (-1 == elfItem.ItemId)
            {
                return;
            }
            var elflevel = elfItem.Exdata.Level;
            var playerlevel = PlayerDataManager.Instance.GetLevel();
            if (elflevel >= playerlevel)
            {
                DataModel.LevelUpNotice = false;
            }
            else
            {
                if (JingPoResCount > elfItem.LvExp)
                {
                    DataModel.LevelUpNotice = true;
                }
                else
                {
                    DataModel.LevelUpNotice = false;
                } 
            }
        }
        private void OnExDataInit64Event(IEvent ievent)
        {
            RefreshFreeDrawTime();
        }

        private void OnExdateUpdate64Event(IEvent ievent)
        {
            ExData64UpDataEvent e = ievent as ExData64UpDataEvent;
            if(e != null && e.Key == 14)
                RefreshFreeDrawTime();
        }

        private void OnLevelUp(IEvent ievent)
        {
            GetElfFightCount();
        }
        private void OnClickShowSkillTips(IEvent ievent)
        {
            var e = ievent as ElfOnClickShowSkillTips;
            if (e == null || DataModel == null || DataModel.Skill == null)
            {
                return;
            }

            if (e.mIsShow == 0)
            {
                DataModel.Skill.IsShowTips = false;
            }
            else
            {
                DataModel.Skill.IsShowTips = true;
            }
        }

        private void OnResourceChanged(IEvent ievent)
        {
            //if (State != FrameState.Open)
            //    return;

            var e = ievent as Resource_Change_Event;
            if (e.Type != eResourcesType.ElfPiece)
            {
                return;
            }

            var oldvalue = e.OldValue;
            var newvalue = e.NewValue;
            var count = DataModel.ElfBag.Count;
            PlayerDataManager.Instance.WeakNoticeData.ElfTotal = false;
            CheckJingPoTip(newvalue);
            for (var i = 0; i < count; i++)
            {
                var elf = DataModel.ElfBag[i];
                if (elf == null || elf.ItemId == -1 || elf.LvExp < 1)
                {
                    continue;
                }
                RefreshStarLevelUp(elf);
                //var needValue = elf.LvExp;
                //if (Table.GetElf(elf.ItemId).MaxLevel == elf.Exdata.Level)
                //{
                //    elf.CanUpgrade = false;
                //}
                //else
                //{
                //    elf.CanUpgrade = needValue <= newvalue;
                //}

//             if (needValue > oldvalue && needValue <= newvalue)
//             {
//                 PlayerDataManager.Instance.WeakNoticeData.ElfTotal = true;
//                 PlayerDataManager.Instance.WeakNoticeData.ElfCanUpgrade = true;
//             }
                //if (needValue <= newvalue)
                //{
                //PlayerDataManager.Instance.WeakNoticeData.ElfTotal = true;
                //PlayerDataManager.Instance.WeakNoticeData.ElfCanUpgrade = true;
                //}
            }
            DataModel.StarCanLevelUp = DataModel.SelectElf.CanStarLevelUp;
            CheckElfInfoNotice();
            CheckElfCanLevelUp(DataModel.SelectElf);
        }

        private void OnBagItemCountChange(IEvent ievent)
        {
            var e = ievent as UIEvent_BagItemCountChange;
            if (e == null)
                return;

            var tbRecord = Table.GetItemBase(e.ItemId);
            if (tbRecord != null && tbRecord.Type == s_PetBookItemType)
            {
                InitSkillBookBag();
                RefreshSelectSkill(selectSkillIndex);
            }
        }

        private void OnUpdateExData(IEvent ievent)
        {
            var e = ievent as ElfExdataUpdate;
            if (e.Type == eExdataDefine.e82)
            {
                UpdateFormationLevel(e.Value);
            }
        }

        private void ReApplyBagData()
        {
            NetManager.Instance.StartCoroutine(ReApplyBagDataCoroutine());
        }

        private IEnumerator ReApplyBagDataCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ApplyBagByType((int) eBagType.Elf);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        InitElfBag(msg.Response);
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error("ApplyBagByType Error!............ErrorCode..." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ApplyBagByType Error!............State..." + msg.State);
                }
            }
        }

        private IEnumerator ReplaceSkillCoroutine(ElfItemDataModel elfData, int elfBagIndex, int exBuffIdx, int itemBagId, int itemBagIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ReplaceElfSkill(elfBagIndex, exBuffIdx, itemBagId, itemBagIndex);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var exDefine = (int)ElfExdataDefine.BuffId1 + exBuffIdx*2;
                        elfData.Exdata[exDefine] = msg.Response;
                        elfData.Exdata[exDefine + 1] = 1;
                        var dataIdx = UiIndexToExIdx(selectSkillIndex);
                        if (dataIdx >= 0 && dataIdx <= DataModel.Skill.SkillInfos.Count)
                        {
                            var skill = DataModel.Skill.SkillInfos[dataIdx];
                            RefreshSkillData(skill);
                            skill.PlayAnim = 0;
                            skill.PlayAnim = 1;
                        }
                        RefreshSkillLevelUp(selectSkillIndex);
                        RefreshSkillInfo();
                        EventDispatcher.Instance.DispatchEvent(new ElfSkillEvent(1));
                    }
                    else
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error("----------------msg.ErrorCode---------{0}", msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("----------------msg.State---------{0}", msg.State);
                }
            }
        }


        private void RefresElfLists()
        {
            RefreshGroupInfo();
            RefreshElfAttribute(DataModel.SelectElf);
            RefreshFormationAttribute();
        }

        private void RefresFightElf()
        {
            var data = DataModel.Formations[0].ElfData;
            DataModel.FightElf = data;
            if (data.ItemId == -1)
            {
                if (ObjManager.Instance.MyPlayer != null)
                {
                    ObjManager.Instance.MyPlayer.RefresElfFollow(-1, -1);
                }
                return;
            }
            var tbElf = Table.GetElf(data.ItemId);
            if (tbElf == null)
            {
                if (ObjManager.Instance.MyPlayer != null)
                {
                    ObjManager.Instance.MyPlayer.RefresElfFollow(-1, -1);
                }
            }
            else
            {
                if (ObjManager.Instance.MyPlayer != null)
                {
                    var colorId = GameUtils.GetElfStarColorId(tbElf.ElfModel, data.Exdata.StarLevel);
                    ObjManager.Instance.MyPlayer.RefresElfFollow(tbElf.ElfModel, colorId);
                }
            }
        }

        private void RefreshElfAttribute(ElfItemDataModel elfData)
        {
            if (elfData.ItemId == -1)
            {
                return;
            }

            var tbItem = Table.GetItemBase(elfData.ItemId);
            var tbElf = Table.GetElf(tbItem.Exdata[0]);
            var level = elfData.Exdata.Level;
            var fightAttr = new Dictionary<int, int>();
            for (var i = 0; i < 6; i++)
            {
                DataModel.BaseAttrAdd[i] = "";
            }
            var maxLevel = elfData.Exdata.Level == tbElf.MaxLevel;

            DataModel.IsNotMaxLevel = !maxLevel;

            if (maxLevel)
            {
                //elfData.CanUpgrade = false;
            }

            for (var i = 0; i < 6; i++)
            {
                var id = tbElf.ElfInitProp[i];
                //var value = GameUtils.EquipAttrValueRef(id, tbElf.ElfProp[i]);
                var value = tbElf.ElfProp[i];
                DataModel.BaseAttr[i].Reset();
                if (id != -1)
                {
                    //var valuelevel = GameUtils.EquipAttrValueRef(id, tbElf.GrowAddValue[i]);
                    var valuelevel = tbElf.GrowAddValue[i];
                    value += valuelevel*(level - 1);

                    GameUtils.SetAttributeBase(DataModel.BaseAttr, i, id, value);
                    //value = GameUtils.EquipAttrValueRef(id, value);
                    fightAttr.modifyValue(id, value);
                    if (maxLevel == false)
                    {
                        var addValue = valuelevel;
                        DataModel.BaseAttrAdd[i] = string.Format("{0}", GameUtils.AttributeValue(id, addValue));
                    }
                }
            }

            if (elfData.Exdata.Count <= (int) ElfExdataDefine.StarLevel)
            {
                return;
            }

            var starLevel = elfData.Exdata[(int)ElfExdataDefine.StarLevel];
            for (var i = 0; i < tbElf.StarAttrId.Length; i++)
            {
                if (tbElf.StarAttrId[i] != -1)
                {
                    var id = tbElf.StarAttrId[i];
                    var value = tbElf.StarAttrValue[i];
                    //var realValue = GameUtils.EquipAttrValueRef(id, value);     
                    if (i < starLevel)
                    {
                        GameUtils.SetAttributeBase(DataModel.InnateAttr, i, id, value);
                        DataModel.InnateExtra[i] = GameUtils.GetDictionaryText(100002140);
                        //if (elfData.Index < 3)
                        //    DataModel.InnateExtraColor[i] = "ADFF00";
                        //else
                        //    DataModel.InnateExtraColor[i] = "888888";  
                        DataModel.InnateExtraColor[i] = "ADFF00";  
                        fightAttr.modifyValue(id, value);
                    }
                    else
                    {
                        GameUtils.SetAttributeBase(DataModel.InnateAttr, i, id, value);
                        DataModel.InnateExtra[i] = GameUtils.GetDictionaryText(100002135 + i); ;
                        DataModel.InnateExtraColor[i] = "888888";
                    }
                }
                else
                {
                    DataModel.InnateAttr[i].Reset();
                }
            }

            elfData.FightPoint = PlayerDataManager.Instance.GetElfAttrFightPoint(fightAttr, -1, -2);
            var skill = new Dictionary<int, int>();
            for (int i = 0; i < 3; i++)
            {
                var buffId = GameUtils.GetElfBuffId(elfData, i);
                var buffLevel = GameUtils.GetElfBuffLevel(elfData, i);
                if (buffId >= 0 && buffLevel >= 0)
                {
                    skill[buffId] = buffLevel;
                }
            }
            elfData.FightPoint += PlayerDataManager.Instance.GetElfSkillFightPoint(skill);
        }

        //---------------------------------------------------Group---------------------
        private void RefreshFormationAttribute()
        {
            var baseAttr = new Dictionary<int, int>();
            var innateAttr = new Dictionary<int, int>();
            var groupList = new Dictionary<int, int>();
            var tbLevel = Table.GetLevelData(DataModel.FormationLevel);
            var levelRate = tbLevel.FightingWayIncome + 10000.0;

            for (var i = 0; i < MaxElfFightCount; i++)
            {
                var formation = DataModel.Formations[i];
                var elfData = formation.ElfData;
                if (elfData.ItemId == -1)
                {
                    continue;
                }
                var rate = 1; //elfData.Index == 0 ? 1.0 : 0.1;
                var level = elfData.Exdata.Level;
                var tbItem = Table.GetItemBase(elfData.ItemId);
                var tbElf = Table.GetElf(tbItem.Exdata[0]);
                for (var j = 0; j < 6; j++)
                {
                    var id = tbElf.ElfInitProp[j];
                    var value = tbElf.ElfProp[j];
                    if (id != -1)
                    {
                        if (level > 1)
                        {
                            var upvalue = tbElf.GrowAddValue[j];
                            value += upvalue*(level - 1);
                        }
                        value = (int) (rate*value*levelRate/10000.0);
                        if (value > 0)
                        {
                            baseAttr.modifyValue(id, value);
                        }
                    }
                }

                if (elfData.Index == 0)
                {
                    for (var j = 0; j < 6; j++)
                    {
                        var id = elfData.Exdata[j + 2];
                        var value = elfData.Exdata[j + 8];
                        if (id != -1 && value > 0)
                        {
                            innateAttr.modifyValue(id, value);
                        }
                    }
                }

                for (var j = 0; j < 3; j++)
                {
                    var groupId = tbElf.BelongGroup[j];
                    if (groupId != -1)
                    {
                        groupList.modifyValue(groupId, 1);
                    }
                }
            }

            var baseCount = baseAttr.Count;
            var index = 0;
            {
                // foreach(var i in baseAttr)
                var __enumerator4 = (baseAttr).GetEnumerator();
                while (__enumerator4.MoveNext())
                {
                    var i = __enumerator4.Current;
                    {
                        if (index >= DataModel.GroupBaseAttr.Count)
                        {
                            break;
                        }
                        DataModel.GroupBaseAttr[index].Type = i.Key;
                        DataModel.GroupBaseAttr[index].Value = i.Value;
                        index++;
                    }
                }
            }
            for (var i = baseCount; i < DataModel.GroupBaseAttr.Count; i++)
            {
                DataModel.GroupBaseAttr[i].Reset();
            }

            index = 0;
            var innateCount = innateAttr.Count;
            {
                // foreach(var i in innateAttr)
                var __enumerator5 = (innateAttr).GetEnumerator();
                while (__enumerator5.MoveNext())
                {
                    var i = __enumerator5.Current;
                    {
                        DataModel.GroupInnateAttr[index].Type = i.Key;
                        DataModel.GroupInnateAttr[index].Value = i.Value;
                        index++;
                    }
                }
            }

            for (var i = innateCount; i < 6; i++)
            {
                DataModel.GroupInnateAttr[i].Reset();
            }

            var showList = new List<int>();
            {
                // foreach(var i in groupList)
                var __enumerator6 = (groupList).GetEnumerator();
                while (__enumerator6.MoveNext())
                {
                    var i = __enumerator6.Current;
                    {
                        var groupId = i.Key;
                        var tbElfGroup = Table.GetElfGroup(groupId);
                        var flag = true;
                        for (var j = 0; j < 3; j++)
                        {
                            var elfId = tbElfGroup.ElfID[j];
                            if (elfId == -1)
                            {
                                continue;
                            }
                            if (!ElfIdInFormation(elfId))
                            {
                                flag = false;
                                break;
                            }
                        }

                        if (flag)
                        {
                            showList.Add(groupId);
                        }
                    }
                }
            }

            for (var i = 0; i < showList.Count; i++)
            {
                var tbElfGroup = Table.GetElfGroup(showList[i]);
                SetGroupAttr(DataModel.GroupInfos[i], tbElfGroup, DataModel.SelectElf.Index);
            }

            for (var i = showList.Count; i < DataModel.GroupInfos.Count; i++)
            {
                DataModel.GroupInfos[i].Reset();
            }
        }

        //---------------------------------------------------Main---------------------
        private void RefreshGroupInfo()
        {
            if (DataModel.SelectElf.ItemId == -1)
            {
                return;
            }
            var tbElf = Table.GetElf(DataModel.SelectElf.ItemId);
            for (var i = 0; i < DataModel.SingleGroups.Count; i++)
            {
                var groupId = tbElf.BelongGroup[i];
                var info = DataModel.SingleGroups[i];
                if (groupId != -1)
                {
                    var tbElfGroup = Table.GetElfGroup(groupId);
                    SetGroupAttr(info, tbElfGroup, DataModel.SelectElf.Index, true);
                }
                else
                {
                    info.Reset();
                }
            }
        }

        private void RefreshStarLevelUp(ElfItemDataModel elfData)
        {
            elfData.CanStarLevelUp = false;
            if (elfData.ItemId < 0)
                return;

            var tbElf = Table.GetElf(elfData.ItemId);
            if (tbElf == null)
                return;

            var maxStar = tbElf.ElfStarUp.Count;
            var curStar = elfData.Exdata.StarLevel;
            if (curStar < 0 || curStar >= maxStar)
            {
                return;
            }

            var id = tbElf.ElfStarUp[curStar];
            var tbConsumeArray = Table.GetConsumArray(id);
            if (tbConsumeArray == null)
            {
                return;
            }

            for (var i = 0; i < tbConsumeArray.ItemId.Length; ++i)
            {
                var itemId = tbConsumeArray.ItemId[i];
                if (itemId == -1)
                    break;
                var needCount = tbConsumeArray.ItemCount[i];
                int haveCount;
                if (itemId < (int)eResourcesType.CountRes)
                {
                    haveCount = PlayerDataManager.GetInstance().GetItemCount(itemId);
                }
                else
                {
                    var tbItem = Table.GetItemBase(itemId);
                    if (tbItem == null)
                        continue;

                    if (tbItem.InitInBag == (int)eBagType.Elf)
                    {
                        haveCount = GetElfTotalCount(itemId).Count;
                        if (elfData.Index >= MaxElfFightCount)
                            --haveCount;
                    }
                    else
                    {
                        haveCount = PlayerDataManager.GetInstance().GetItemCount(itemId);
                    }
                }
                if (haveCount < needCount)
                {
                    return;
                }
            }

            elfData.CanStarLevelUp = true;
        }

        private void RefreshStarInfo()
        {
            var tbElf = Table.GetElf(DataModel.SelectElf.ItemId);
            if (tbElf == null)
                return;

            DataModel.MaxStar = tbElf.ElfStarUp.Count;
            DataModel.CurrentStar = DataModel.SelectElf.Exdata.StarLevel;
            var curStarLevel = DataModel.CurrentStar;
            if (curStarLevel < 0 || curStarLevel >= DataModel.MaxStar)
            {
                return;
            }

            var id = tbElf.ElfStarUp[curStarLevel];
            var tbConsumeArray = Table.GetConsumArray(id);
            if (tbConsumeArray == null)
            {
                DataModel.StarCostResId = -1;
                return;
            }

            var index = 0;
            for (var i = 0; i < tbConsumeArray.ItemId.Length; ++i)
            {
                var itemId = tbConsumeArray.ItemId[i];
                if (itemId == -1)
                    break;
                var itemCount = tbConsumeArray.ItemCount[i];
                if (itemId < (int) eResourcesType.CountRes)
                {
                    DataModel.StarCostResId = itemId;
                    DataModel.StarCostResCount = itemCount;                
                }
                else
                {
                    if (index < DataModel.StarCostItems.Count)
                    {
                        var data = DataModel.StarCostItems[index];
                        data.ItemId = itemId;

                        var tbItem = Table.GetItemBase(itemId);
                        if (tbItem == null)
                            continue;

                        if (tbItem.InitInBag == (int) eBagType.Elf)
                        {
                            data.Count = itemCount;
                            var total = GetElfTotalCount(data.ItemId).Count;
                            if (DataModel.SelectElf.Index >= MaxElfFightCount)
                                --total;
                            data.TotalCount = total;                        
                        }
                        else
                        {
                            data.TotalCount = PlayerDataManager.GetInstance().GetItemCount(itemId);
                        }
                        ++index;
                    }
                }
            }
            for (var i = index; i < DataModel.StarCostItems.Count; ++i)
            {
                DataModel.StarCostItems[i].ItemId = -1;
            }
        }

        #region SKILL

        ElfItemDataModel GetSkillSelectElf()
        {
            return DataModel.Skill.SelectElf;
        }

        int GetItemBuffId(int itemId)
        {
            if (itemId == -1)
                return -1;

            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
                return -1;

            if (tbItem.Type != s_PetBookItemType)
                return -1;

            var buffGroupId = tbItem.Exdata[0];
            if (buffGroupId == -1)
                return -1;

            var tbBuffGroup = Table.GetBuffGroup(buffGroupId);
            if (tbBuffGroup == null)
                return -1;

            if (tbBuffGroup.BuffID.Count == 0)
                return -1;

            return tbBuffGroup.BuffID[0];
        }

        private int GetItemBuffType(int itemId)
        {
            if (itemId == -1)
                return -1;

            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
                return -1;

            if (tbItem.Type != s_PetBookItemType)
                return -1;

            return tbItem.Exdata[1];
        
        }

        private void RefreshModel(int elfId, int star, bool isSkillTab)
        {
            var type = isSkillTab ? 1 : 0;
            if (elfId == -1)
            {
                var e1 = new ElfModelRefreshEvent(-1, -1, 0, type);
                EventDispatcher.Instance.DispatchEvent(e1);
            }
            else
            {
                var tbElf = Table.GetElf(elfId);
                if (tbElf != null)
                {
                    var colorId = GameUtils.GetElfStarColorId(tbElf.ElfModel, star);
                    var e1 = new ElfModelRefreshEvent(tbElf.ElfModel, colorId, tbElf.Offset, type);
                    EventDispatcher.Instance.DispatchEvent(e1);
                }
            }
        }

        private void RefreshAllSkillInfo(int elfIndex)
        {
            if (elfIndex < 0 || elfIndex >= DataModel.Items.Count)
                return;

            var elfItem = DataModel.Items[elfIndex];
            if (elfItem == null)
                return;

            DataModel.Skill.SelectElf = elfItem;

            var __enumerator1 = (DataModel.Items).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var dataModel = __enumerator1.Current;
                dataModel.IsSelect = dataModel == elfItem;
            }

            RefreshModel(DataModel.Skill.SelectElf.ItemId, elfItem.Exdata.StarLevel, true);
            RefreshElfSkill(DataModel.Skill.SelectElf);
        }

        private void RefreshElfSkill(ElfItemDataModel elfItem)
        {
            int[] buffTypeText = { 100002142, 100002143, 100002144 };
            var skillInfoDict = new Dictionary<int, ElfSkillInfoDataModel>(MAX_SKILLCOUNT);
            for (var i = 0; i < MAX_SKILLCOUNT; ++i)
            { // 攻击，防御，辅助  顺序添加
                var item = new ElfSkillInfoDataModel();
                item.BuffId = -1;
                item.BuffLevel = 0;
                item.ExdataId = i;  // 扩展计数的buff索引
                item.Type = GameUtils.GetDictionaryText(buffTypeText[i]);
                skillInfoDict[i] = item;
            }

            for (var i = 0; i < MAX_SKILLCOUNT; ++i)
            {
                var exId = (int)ElfExdataDefine.BuffId1 + i * 2;
                if (exId >= elfItem.Exdata.Count)
                    return;
                var buffId = elfItem.Exdata[exId];
                if (buffId < 0)
                    continue;

                var tbBuff = Table.GetBuff(buffId);
                if (tbBuff == null)
                    continue;
                ElfSkillInfoDataModel item;
                if (skillInfoDict.TryGetValue(tbBuff.SkillType, out item))
                {
                    item.BuffId = buffId;
                    item.BuffLevel = elfItem.Exdata[(int)ElfExdataDefine.BuffLevel1 + i * 2];
                }
            }

            DataModel.Skill.SkillInfos.Clear();
            for (var i = 0; i < MAX_SKILLCOUNT; ++i)
            {
                DataModel.Skill.SkillInfos.Add(skillInfoDict[i]);
            }

            RefreshSelectSkill(0);
        }

        private void RefreshSkillData(ElfSkillInfoDataModel item)
        {
            item.BuffId = -1;
            item.BuffLevel = 0;

            ElfItemDataModel elfItem = GetSkillSelectElf();
            if (elfItem == null)
                return;

            var exId = (int)ElfExdataDefine.BuffId1 + item.ExdataId * 2;
            if (exId >= elfItem.Exdata.Count)
                return;
            var buffId = elfItem.Exdata[exId];
            item.BuffId = buffId;

            exId = (int)ElfExdataDefine.BuffLevel1 + item.ExdataId * 2;
            if (exId >= elfItem.Exdata.Count)
                return;
            item.BuffLevel = elfItem.Exdata[exId];
        }

        private void RefreshSelectSkill(int buffIndex)
        {
            selectSkillIndex = buffIndex;

            var skillType = -1;
            var ii = 0;
            var __enumerator1 = (DataModel.Skill.SkillInfos).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var dataModel = __enumerator1.Current;
                if (ii == selectSkillIndex)
                {
                    dataModel.IsSelect = true;
                    skillType = dataModel.ExdataId;
                }
                else
                {
                    dataModel.IsSelect = false;
                }
                ++ii;
            }

            var buffId = GameUtils.GetElfBuffId(GetSkillSelectElf(), selectSkillIndex);
            var buffLevel = GameUtils.GetElfBuffLevel(GetSkillSelectElf(), selectSkillIndex);
            DataModel.Skill.CurrentBuff.BuffId = buffId;
            DataModel.Skill.CurrentBuff.BuffLevel = buffLevel;
            DataModel.Skill.Operate = 0;

            RefreshSkillLevelUp(selectSkillIndex);
            RefreshSkillBook(skillType);
        }

        private int UiIndexToExIdx(int uiIndex)
        {
            if (uiIndex >= 0 && uiIndex < DataModel.Skill.SkillInfos.Count)
            {
                var skillInfo = DataModel.Skill.SkillInfos[uiIndex];
                return skillInfo.ExdataId;            
            }

            return -1;
        }

        // 获得技能界面当前选中的精灵，技能索引uiIndex所对应的技能buffid
        private int GetElfSkillSelectBuffId(int uiIndex)
        {
            var skillIndex = UiIndexToExIdx(uiIndex);
            if (skillIndex < 0)
                return -1;

            var buffId = GameUtils.GetElfBuffId(GetSkillSelectElf(), skillIndex);
            return buffId;
        }

        private void RefreshSkillLevelUp(int uiIndex)
        {
            var buffId = GetElfSkillSelectBuffId(uiIndex);
            if (buffId == -1)
            {
                DataModel.Skill.Operate = 2;
                return;
            }

            var buff = Table.GetBuff(buffId);
            if (buff == null)
            {
                DataModel.Skill.Operate = 2;
                return;
            }

            var skillIndex = UiIndexToExIdx(uiIndex);
            var buffLevel = GameUtils.GetElfBuffLevel(GetSkillSelectElf(), skillIndex);
            if (buffLevel > buff.ElfSkillUp.Count) // 满级
            {
                DataModel.Skill.IsMaxLevel = true;
                return;
            }
            DataModel.Skill.IsMaxLevel = false;

            if (buffLevel < 1)
                return;

            var id = buff.ElfSkillUp[buffLevel - 1];
            var tbConsumeArray = Table.GetConsumArray(id);
            if (tbConsumeArray == null)
            {
                Logger.Error("elf buff levelup buffId = {0} Cannot find id={1} in ConsumArray.txt", buffId, id);
                //DataModel.StarCostResId = -1;
                for (var i = 0; i < DataModel.Skill.CostItems.Count; ++i)
                {
                    DataModel.Skill.CostItems[i].ItemId = -1;
                }
                return;
            }

            var index = 0;
            for (var i = 0; i < tbConsumeArray.ItemId.Length; ++i)
            {
                var itemId = tbConsumeArray.ItemId[i];
                if (itemId == -1)
                    break;
                var itemCount = tbConsumeArray.ItemCount[i];
                if (index < DataModel.Skill.CostItems.Count)
                {
                    var data = DataModel.Skill.CostItems[index];
                    data.ItemId = itemId;
                    data.Count = itemCount;
                    ++index;
                }
            }
            for (var i = index; i < DataModel.StarCostItems.Count; ++i)
            {
                DataModel.Skill.CostItems[i].ItemId = -1;
            }        
        }

        private void InitSkillBookBag()
        {
            skillbookBagMap.Clear();

            var playerBags = PlayerDataManager.Instance.PlayerDataModel.Bags;
            var __enumerator2 = (playerBags.Bags[(int)eBagType.BaseItem].Items).GetEnumerator();
            while (__enumerator2.MoveNext())
            {
                var item = __enumerator2.Current;
                if (item != null && item.ItemId != -1)
                {
                    var tbRecord = Table.GetItemBase(item.ItemId);
                    if (tbRecord != null && tbRecord.Type == s_PetBookItemType)
                    {
                        skillbookBagMap[item.Index] = item;
                    }
                }
            }
        }

        private void RefreshSkillBook(int skillType)
        {
            var lastCount = DataModel.Skill.SkillBook.Count;
            DataModel.Skill.SkillBook.Clear();

            var itemList = new List<BagItemDataModel>();
            var enumerator1 = skillbookBagMap.GetEnumerator();
            while (enumerator1.MoveNext())
            {
                var item = enumerator1.Current.Value;
                //var buffId = GetItemBuffId(item.ItemId);
                var buffType = GetItemBuffType(item.ItemId);
                if (buffType == -1 || buffType != skillType)
                {
                    continue;
                }
                itemList.Add(item);
            }

            itemList.Sort((a, b) =>
            {
                var rbItemA = Table.GetItemBase(a.ItemId);
                var rbItemB = Table.GetItemBase(b.ItemId);
                if (rbItemA != null && rbItemB != null)
                {
                    return (rbItemA.Quality > rbItemB.Quality) ? -1 : 1;
                }

                return 0;
            });

            var enumerator = itemList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                var equipItem = new ItemIdSelectDataModel();
                equipItem.ItemId = item.ItemId;
                equipItem.Count = item.Count;
                equipItem.BagItem = item;
                equipItem.Select = false;
                DataModel.Skill.SkillBook.Add(equipItem);            
            }

            DataModel.Skill.HaveSkillBook = DataModel.Skill.SkillBook.Count > 0;

            if (lastCount != DataModel.Skill.SkillBook.Count)
            {
                selectSkillBookIndex = -1;
            }

            EventDispatcher.Instance.DispatchEvent(new Event_ElfSkillBookLookIndex(selectSkillBookIndex));

            selectSkillBookIndex = -1;
        }

        #endregion

        private void OnCountChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RecycleRate")
            {
                DataModel.Skill.RecycleCount = (int)(Mathf.Round(DataModel.Skill.RecycleRate * (DataModel.Skill.ItemData.Count - 1)) + 1);
            }
        }

        private void RecycleAll()
        {
            var callBackType = -1;
            var getItem = 0;
            var itemList = new RecycleItemList();
            var enumorator = DataModel.Skill.SkillBook.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var bagItem = enumorator.Current.BagItem;
                if (bagItem != null)
                {
                    var tbItem = Table.GetItemBase(bagItem.ItemId);
                    if (tbItem == null)
                    {
                        continue;
                    }

                    if (tbItem.Quality != 2)    // 蓝色
                    {
                        continue;
                    }

                    callBackType = tbItem.CallBackType;
                    getItem += tbItem.CallBackPrice * bagItem.Count;

                    var item = new RecycleItem();
                    item.BagType = bagItem.BagId;
                    item.Index = bagItem.Index;
                    item.ItemId = bagItem.ItemId;
                    item.Count = bagItem.Count;
                    itemList.RecycleList.Add(item);
                }
            }

            if (itemList.RecycleList.Count <= 0)
            {
                return;
            }

            var tbCallbackItem = Table.GetItemBase(callBackType);
            if (tbCallbackItem == null)
            {
                return;
            }
            var color = GameUtils.GetTableColorString(tbCallbackItem.Quality);
            var name = String.Format("[{0}][{1}][-]", color, tbCallbackItem.Name);

            var str = string.Format(GameUtils.GetDictionaryText(225000), name, getItem);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "", () =>
            {
                NetManager.Instance.StartCoroutine(RecycleItemListCorotion(itemList));
            });
        }

        private IEnumerator RecycleItemListCorotion(RecycleItemList items)
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.RecycleBagItemList(items);
                yield return msg.SendAndWaitUntilDone();

                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    { // 回收成功
                        var e1 = new ShowUIHintBoard(270110);
                        EventDispatcher.Instance.DispatchEvent(e1);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Info(string.Format("SellItemCorotion....State = {0} ErroeCode = {1}", msg.State, msg.ErrorCode));
                }
            }
        }

        private void OnClickRecycle()
        {
            if (selectSkillBookIndex < 0 || selectSkillBookIndex >= DataModel.Skill.SkillBook.Count)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002154));
                return;
            }

            DataModel.Skill.ItemData = DataModel.Skill.SkillBook[selectSkillBookIndex].BagItem;

            DataModel.Skill.RecycleCount = DataModel.Skill.ItemData.Count;
            DataModel.Skill.RecycleRate = 1.0f;
            var tbItem = Table.GetItemBase(DataModel.Skill.ItemData.ItemId);
            if (tbItem != null)
                DataModel.Skill.CallBackType = tbItem.CallBackType;
            if (DataModel.Skill.ItemData.Count == 1)
            {
                GameUtils.RecycleConfirm(DataModel.Skill.ItemData.ItemId,1, () =>
                {
                    NetManager.Instance.StartCoroutine(RecycleItemCorotion());
                });
            }
            else
            {
                DataModel.Skill.ShowRecycleBox = true;
            }
        }
        private IEnumerator RecycleItemCorotion()
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var item = DataModel.Skill.ItemData;
                var msg = NetManager.Instance.RecycleBagItem(item.BagId, item.ItemId, item.Index, DataModel.Skill.RecycleCount);
                yield return msg.SendAndWaitUntilDone();

                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    { // 回收成功
                        var e1 = new ShowUIHintBoard(270110);
                        EventDispatcher.Instance.DispatchEvent(e1);
                        DataModel.Skill.ShowRecycleBox = false;
                        DataModel.Skill.ShowRecycleAnim = false;
                        DataModel.Skill.ShowRecycleAnim = true;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Info(string.Format("SellItemCorotion....State = {0} ErroeCode = {1}", msg.State, msg.ErrorCode));
                }
            }
        }

        private void OnSelectSkillBook(IEvent iEvent)
        {
            var e = iEvent as IconIdSelectEvent;
            if (e != null)
            {
                SelectSkillBook(e.Index, e.Select);
            }
        }

        private void OnSelectSkill(IEvent iEvent)
        {
            var e = iEvent as ElfSkillInfoCell_SelectEvent;
            if (e != null)
            {
                RefreshSelectSkill(e.Index);
            }
        }

        private void SelectSkillBook(int index, bool select)
        {
            var skillDataModel = DataModel.Skill;
            if (selectSkillBookIndex >= 0 && selectSkillBookIndex < skillDataModel.SkillBook.Count)
            {
                skillDataModel.SkillBook[selectSkillBookIndex].Select = false;
            }
            selectSkillBookIndex = index;
            skillDataModel.SkillBook[selectSkillBookIndex].Select = select;

            if (select)
            {
                skillDataModel.Operate = 1;
            }
            else
            {
                var buffId = GetElfSkillSelectBuffId(selectSkillIndex);
                if (buffId == -1)
                {
                    DataModel.Skill.Operate = 2;
                    return;
                }
                skillDataModel.Operate = 0;
                selectSkillBookIndex = -1;
            }

            if (index >= 0 && index <= skillDataModel.SkillBook.Count)
            {
                var skill = skillDataModel.SkillBook[index];
                var buffId = GetItemBuffId(skill.ItemId);
                skillDataModel.NextBuff.BuffId = buffId;
                skillDataModel.NextBuff.BuffLevel = 1;
            }
            else
            {
                skillDataModel.NextBuff.BuffId = -1;
            }
        }

        private void RefreshShowElf(bool bRefreshElfList = true, int elfIdx = -1)
        {
            var elfs = DataModel.ElfBag.Where(d => d.ItemId != -1).ToList();

            if (elfs.Count > MaxElfFightCount)
            {
                elfs.Sort(MaxElfFightCount, elfs.Count - MaxElfFightCount, ElfItemComparer);
            }
            DataModel.Items = new ObservableCollection<ElfItemDataModel>(elfs);
            DataModel.NowElfCount = DataModel.Items.Count;
            if (bRefreshElfList)
            {
                ElfSort();
            }

            if (DataModel.SelectElf.ItemId == -1 || elfIdx != -1)
            {
                if (elfIdx != -1)
                {
                    SelectElfIndex = elfIdx;
                }
                if (DataModel.Items.Count > 0)
                {
                    if (SelectElfIndex >= DataModel.Items.Count)
                    {
                        SelectElfIndex = DataModel.Items.Count - 1;
                    }

                    var index = SelectElfIndex;
                    if (DataModel.Items.Count <= index)
                    {
                        index = DataModel.Items.Count - 1;
                    }
                    SetSelectElf(DataModel.Items[index]);
                }
            }
            RefresElfLists();
            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Elf);

            if (mIsSetOffset)
            {
                mIsSetOffset = false;
                EventDispatcher.Instance.DispatchEvent(new UIEvent_ElfSetGridLookIndex(-1));
            }
            else
            {
                var selIdx = DataModel.Items.IndexOf(DataModel.SelectElf);
                if (selIdx > 0)
                {
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_ElfSetGridLookIndex(selIdx));
                }
            }
            RefreshIsShowElfOrSkillPage();
        }
        private void ElfSort()
        {
            DataModel.ElfList.Clear();
            var elfs = DataModel.ElfBag.Where(d => d.ItemId != -1).ToList();
            var OtherList = elfs.Where(d => d.State != 2).ToList();
            var AllList = new List<ElfItemDataModel>();
            foreach (var item in elfs)
            {
                if (item.State == 2)
                {
                    AllList.Add(item);
                }
            }
            OtherList.Sort(ElfItemComparer);
            foreach (var item in OtherList)
            {
                AllList.Add(item);
            }
            DataModel.ElfList = new ObservableCollection<ElfItemDataModel>(AllList);
        }
        private void ResetInfo()
        {
            for (var i = 0; i < 6; i++)
            {
                DataModel.BaseAttr[i].Reset();
                DataModel.InnateAttr[i].Reset();
                DataModel.GroupInnateAttr[i].Reset();

                DataModel.BaseAttrAdd[i] = "";
            }
            for (var i = 0; i < DataModel.GroupBaseAttr.Count; i++)
            {
                DataModel.GroupBaseAttr[i].Reset();
            }
            for (var i = 0; i < DataModel.SingleGroups.Count; i++)
            {
                DataModel.SingleGroups[i] = new ElfGroupInfoData();
            }
        }

        private void ResetUI()
        {
            DataModel.UIGetShow = 0;
            //DataModel.UIGetOneShow = 0;
            var formations = DataModel.Formations;
            foreach (var f in formations)
            {
                f.IsSelect = false;
            }
            if (DataModel.ShowFormationInfo)
            {
                DataModel.ShowFormationInfo = false;
                EventDispatcher.Instance.DispatchEvent(new ElfPlayAnimationEvent(0, false, true));
            }
            if (DataModel.ShowElfList)
            {
                DataModel.ShowElfList = false;
                EventDispatcher.Instance.DispatchEvent(new ElfPlayAnimationEvent(1, false, true));
            }
            RefreshShowElf();

            DataModel.ShowStarUi = false;
        }

        private void SetElfLevelExp(ElfItemDataModel itemData)
        {
            if (itemData.ItemId == -1)
            {
                itemData.LvExp = 0;
                return;
            }

            var tbElf = Table.GetElf(itemData.ItemId);
            if (tbElf == null)
            {
                itemData.LvExp = 0;
                return;
            }

            if (tbElf.MaxLevel == itemData.Exdata.Level)
            {
                itemData.LvExp = -1;
                //itemData.CanUpgrade = false;
            }
            else
            {
                var tbLevel = Table.GetLevelData(itemData.Exdata.Level);
                itemData.LvExp = tbLevel.ElfExp*tbElf.ResolveCoef[0]/100;
                //var res = PlayerDataManager.Instance.GetRes((int) eResourcesType.ElfPiece);
                //itemData.CanUpgrade = itemData.LvExp <= res;
            }
        }

        private void RefreshFormationModel(int elfIndex)
        {
            if (elfIndex < 0 || DataModel.Formations[elfIndex].IsLocked)
            {
                return;
            }

            var elfId = DataModel.Formations[elfIndex].ElfData.ItemId;
            if (elfId < 0)
                return;

            var tbElf = Table.GetElf(elfId);
            if (tbElf == null)
            {
                return;
            }
            var dataId = tbElf.ElfModel;
            if (dataId == -1)
            {
                return;
            }
            var tableNpc = Table.GetCharacterBase(dataId);
            if (null == tableNpc)
            {
                Logger.Error("In CreateFormationElfModel(), null == tableNpc!!!!!!!");
                return;
            }

            var offset = tableNpc.CameraHeight / 10000.0f;
            var scale = tableNpc.CameraMult / 10000.0f;
            int colorId = GameUtils.GetElfStarColorId(dataId, DataModel.Formations[elfIndex].ElfData.Exdata.StarLevel);
            EventDispatcher.Instance.DispatchEvent(new FormationElfModelRefreshEvent(elfIndex, dataId, colorId, offset, scale));
        
        }

        private void SetFormationElf(int elfIdx, ElfItemDataModel elf)
        {
            var formations = DataModel.Formations;
            if (elfIdx >= DataModel.Formations.Count)
            {
                return;
            }

            formations[elfIdx].Install(elf);

            RefreshFormationModel(elfIdx);
        
            if (elfIdx == 0)
            {
                RefresFightElf();
            }
        }

        private void SetGroupAttr(ElfGroupInfoData infoData, ElfGroupRecord tbElfGroup, int elfIdx, bool isCheck = false)
        {
            var isFight = elfIdx < MaxElfFightCount;
            var isActive = isFight;
            infoData.GroupId = tbElfGroup.Id;
            for (var j = 0; j < MaxElfFightCount; j++)
            {
                var itemId = tbElfGroup.ElfID[j];
                infoData.ItemList[j] = itemId;
                if (itemId == -1)
                {
                    continue;
                }
                if (isCheck)
                {
                    var isFormation = isFight && ElfIdInFormation(itemId);
                    if (!isFormation)
                    {
                        isActive = false;
                    }
                    infoData.IsFight[j] = isFormation;
                }
                else
                {
                    infoData.IsFight[j] = isFight;
                }
            }
            infoData.AttrColor = isActive ? GameUtils.green : GameUtils.grey;
            var groupAttr = new Dictionary<int, int>();
            var tbElfGroupGroupPorpLength3 = tbElfGroup.GroupPorp.Length;
            for (var j = 0; j < tbElfGroupGroupPorpLength3; j++)
            {
                var attrId = tbElfGroup.GroupPorp[j];
                if (attrId == -1)
                {
                    break;
                }
                var attrValue = tbElfGroup.PropValue[j];
                groupAttr.modifyValue(attrId, attrValue);
            }
            var index = 0;
            {
                var enumerator1 = (groupAttr).GetEnumerator();
                while (enumerator1.MoveNext())
                {
                    var j = enumerator1.Current;
                    {
                        infoData.GroupAttr[index].Type = j.Key;
                        infoData.GroupAttr[index].Value = j.Value;
                        index++;
                    }
                }
            }
            var count4 = infoData.GroupAttr.Count;
            for (var j = index; j < count4; j++)
            {
                infoData.GroupAttr[j].Type = -1;
                infoData.GroupAttr[j].Value = 0;
            }
        }

        private void SetSelectElf(ElfItemDataModel data)
        {
            // foreach(var dataModel in DataModel.Items)
            var __enumerator1 = (DataModel.Items).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var dataModel = __enumerator1.Current;
                {
                    if (dataModel == data)
                    {
                        dataModel.IsSelect = true;
                    }
                    else
                    {
                        dataModel.IsSelect = false;
                    }
                }
            }

            //if (DataModel.SelectElf == data)
            //{
            //    return;
            //}

            DataModel.SelectElf = data;
            DataModel.StarCanLevelUp = DataModel.SelectElf.CanStarLevelUp;
            RefreshModel(data.ItemId, data.Exdata.StarLevel, false);
            if (data.ItemId != -1)
            {
                CheckElfCanLevelUp(DataModel.SelectElf);
                RefreshGroupInfo();
                RefreshElfAttribute(DataModel.SelectElf);
                RefreshSkillInfo();
                RefreshStarInfo();
            }
        }

        private void RefreshSkillInfo()
        {
            DataModel.HasBuff = false;
            for (var i = 0; i < DataModel.ElfBuffList.Count; ++i)
            {
                var buffId = GameUtils.GetElfBuffId(DataModel.SelectElf, i);
                DataModel.ElfBuffList[i].BuffId = buffId;
                DataModel.ElfBuffList[i].BuffLevel = GameUtils.GetElfBuffLevel(DataModel.SelectElf, i);
                if (buffId > 0)
                {
                    DataModel.HasBuff = true;
                }
            }
        }

        private void ShowDrawGet(IEvent ievent)
        {
            if (IsOneDraw)
            {
                DataModel.UIGetOneShow = 1;
            }
            else
            {
                DataModel.UIGetOneShow = 10;
            }
        }

        private void ShowElfItemInfo(int index)
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ElfInfoUI, new ElfInfoArguments
            {
                DataModel = DataModel.ElfShowItems[index]
            }));
        }

        private void ShowOneDrawInfo(IEvent ievent)
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ElfInfoUI,
                new ElfInfoArguments {DataModel = DataModel.OneGetItem}));
        }

        private void SortAndRefreshElf()
        {
            var bag = DataModel.ElfBag;
            for (int i = 0, imax = bag.Count >= MaxElfFightCount ? bag.Count : MaxElfFightCount; i < imax; i++)
            {
                SetFormationElf(i, bag[i]);
            }
            RefreshShowElf();

            var enumorator = bag.GetEnumerator();
            while (enumorator.MoveNext())
            {
                RefreshStarLevelUp(enumorator.Current);
            }

            DataModel.StarCanLevelUp = DataModel.SelectElf.CanStarLevelUp;
        }

        private void DisplayElf()
        {
            if (displayElfId < 0)
            {
                displayElfId = 0;
                return;
            }
            GameUtils.ShowModelDisplay(displayElfId, () =>
            {
                var e = new ExitFuBenWithOutMessageBoxEvent();
                EventDispatcher.Instance.DispatchEvent(e);
                displayElfId = -1;
            });
        }

        private void UpdateElfBag(ItemsChangeData bagData)
        {
            {
                // foreach(var change in bagData.ItemsChange)
                var __enumerator2 = (bagData.ItemsChange).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var change = __enumerator2.Current;
                    {
                        var changeItem = change.Value;
                        var index = changeItem.Index;
                        var itemData = DataModel.ElfBag[index];

                        if (DataModel.ElfBag.Count > changeItem.Index)
                        {
                            if (itemData.ItemId == -1)
                            {
                                BagItemCount++;
                                if (index >= MaxElfFightCount && changeItem.ItemId != -1)
                                {
                                    AddElfTotalCount(changeItem.ItemId, 1);
                                }

                                PlayerDataManager.Instance.GainNewItem(changeItem.ItemId, 1, index);

                                var fubenId = SceneManager.Instance.GetFubenId();
                                if (fubenId == GameUtils.GetElfModelDisplayFubenId() && changeItem.ItemId > 0)
                                {
                                    if (displayElfId == 0)
                                    {
                                        displayElfId = changeItem.ItemId;
                                        DisplayElf();
                                    }
                                    else
                                    {
                                        displayElfId = changeItem.ItemId;
                                    }
                                }
                            }
                            if (changeItem.ItemId == -1)
                            {
                                BagItemCount--;
                                if (index >= MaxElfFightCount && itemData.ItemId != -1)
                                {
                                    AddElfTotalCount(itemData.ItemId, -1);
                                }
                            }
                        }
                        if (itemData != DataModel.SelectElf)
                        {
                            if (DataModel.TabIndex != 2)
                                itemData.IsSelect = false;
                        }

                        //var elfItem = GetElfItem(index);
                        itemData.ItemId = changeItem.ItemId;
                        itemData.Index = changeItem.Index;
                        itemData.Exdata.InstallData(changeItem.Exdata);
                        SetElfLevelExp(itemData);
                        RefreshElfAttribute(itemData);
                    }
                }
            }

            // 升级技能，不刷新
            if (DataModel.TabIndex != 2)
                SortAndRefreshElf();
        }

        private void UpdateFormationLevel(int level)
        {
            DataModel.FormationLevel = level;
            RefreshFormationAttribute();
            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Elf);
        }

        public void CleanUp()
        {
            if (DataModel != null)
            {
                DataModel.Skill.PropertyChanged -= OnCountChange;
            }

            DataModel = new ElfDataModel();
            DataModel.Skill.PropertyChanged += OnCountChange;
            DataModel.OneIconID = int.Parse(Table.GetClientConfig(500).Value);
            DataModel.TenIconID = int.Parse(Table.GetClientConfig(502).Value);
            DataModel.OneMoney = int.Parse(Table.GetClientConfig(501).Value);
            DataModel.TenMoney = int.Parse(Table.GetClientConfig(503).Value);
            DataModel.JingPoTipCount = int.Parse(Table.GetClientConfig(1218).Value);
            for (var i = 0; i < DataModel.ShowList.Count; i++)
            {
                var item = new ItemIdDataModel();
                var tbClientConfig = Table.GetClientConfig(510 + i);
                if (tbClientConfig != null)
                {
                    item.ItemId = int.Parse(tbClientConfig.Value);
                }
                DataModel.ShowList[i] = item;
            }
            for (var i = 0; i < DataModel.ElfShowItems.Count; i++)
            {
                var item = new ElfItemDataModel();
                var tbClientConfig = Table.GetClientConfig(510 + i);
                if (tbClientConfig != null)
                {
                    item.ItemId = int.Parse(tbClientConfig.Value);
                }
                InitElfRandomProp(item);
                DataModel.ElfShowItems[i] = item;
            }
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "UpdateElfBag")
            {
                UpdateElfBag(param[0] as ItemsChangeData);
            }
            else if (name == "InitElfBag")
            {
                InitElfBag(param[0] as BagBaseData);
            }
            else if (name == "GetFightModel")
            {
                var itemId = DataModel.Formations[0].ElfData.ItemId;

                if (itemId == -1)
                {
                    return -1;
                }
                var tbElf = Table.GetElf(itemId);
                return tbElf.ElfModel;
            }
            else if (name == "GetFightColorId")
            {
                var itemId = DataModel.Formations[0].ElfData.ItemId;
                if (itemId == -1)
                {
                    return -1;
                }
                var tbElf = Table.GetElf(itemId);
                if (tbElf == null)
                    return -1;

                var colorId = GameUtils.GetElfStarColorId(tbElf.ElfModel, DataModel.Formations[0].ElfData.Exdata.StarLevel);
                return colorId;
            }
            else if (name == "SetGroupAttr")
            {
                SetGroupAttr((ElfGroupInfoData) param[0], (ElfGroupRecord) param[1], (int) param[2], (bool) param[3]);
            }
            else if (name == "GetIsFreeDraw")
            {
                return DataModel.IsFreeDraw;
            }
            else if (name == "ElfDisplay")
            {
                DisplayElf();
            }
            else if (name == "FreeDrawTime")
            {
                return DataModel.FreeElfTime;
            }
            return null;
        }

        public void OnShow()
        {
            if (DataModel.TabIndex == 2)
            {
                RefreshModel(DataModel.Skill.SelectElf.ItemId, DataModel.Skill.SelectElf.Exdata.StarLevel, true);
            }
            else if (DataModel.TabIndex == 1)
            {
                RefreshModel(DataModel.SelectElf.ItemId, DataModel.SelectElf.Exdata.StarLevel, false);       
            }
            //else if (DataModel.TabIndex == 0)
            {
                for (int i = 0, imax = DataModel.Formations.Count; i < imax; ++i)
                {
                    RefreshFormationModel(i);
                }
            }
            var exdataCount = (PlayerDataManager.Instance.GetExData((int)eExdataDefine.e410)) % 10;
            var con = 10 - exdataCount;
            DataModel.ElfCount = string.Format(GameUtils.GetDictionaryText(100002302), con);

            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnLevelUp);
            EventDispatcher.Instance.AddEventListener(IconIdSelectEvent.EVENT_TYPE, OnSelectSkillBook);
            EventDispatcher.Instance.AddEventListener(ElfSkillInfoCell_SelectEvent.EVENT_TYPE, OnSelectSkill);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemCountChange);
            if (null == mTimeCoroutine )
            {
                RefreshFreeDrawTime();
            }
        }

        public void Close()
        {
            if (mTimeCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(mTimeCoroutine);
                mTimeCoroutine = null;
            }

            DataModel.Skill.ShowRecycleAnim = false;

            EventDispatcher.Instance.RemoveEventListener(Event_LevelUp.EVENT_TYPE, OnLevelUp);
            EventDispatcher.Instance.RemoveEventListener(IconIdSelectEvent.EVENT_TYPE, OnSelectSkillBook);
            EventDispatcher.Instance.RemoveEventListener(ElfSkillInfoCell_SelectEvent.EVENT_TYPE, OnSelectSkill);
            EventDispatcher.Instance.RemoveEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemCountChange);
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as ElfArguments;
            if (args != null)
            {
                DataModel.TabIndex = args.Tab;
            }
            mIsSetOffset = false;
            GetElfFightCount();
            ResetUI();
            SortAndRefreshElf();
            InitSkillBookBag();
            if (DataModel.TabIndex == 2)
                RefreshAllSkillInfo(0);
            PlayerDataManager.Instance.NoticeData.ElfJingPoTip = false;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "Resource")
            {
                return PlayerDataManager.Instance.PlayerDataModel.Bags.Resources;
            }
            return DataModel;
        }

        public FrameState State { get; set; }

        #region 抽奖代码
        private List<int> sore = new List<int>();  
        private void GetDrawResult(IEvent ievent)
        {
            var e = ievent as ElfGetDrawResult;
            DataModel.UIGetOneShow = 0; //0 不显示 ，1 显示单抽，10显示10抽
            var draw = e.DrawItems.Items;
            if (draw.Count == 0)
            {
            }
            else if (draw.Count == 1)
            {
                DataModel.OneGetItem.Index = draw[0].Index;
                DataModel.OneGetItem.ItemId = draw[0].ItemId;
                DataModel.OneGetItem.Exdata.InstallData(draw[0].Exdata);
                //if (FreeType == 100)
                //{
                //    //Draw.FreeNowCount--;
                //    long time = e.DrawTime;
                //   // RefreshFightTime(time);
                //}
                var tbItem = Table.GetItemBase(draw[0].ItemId);
                DataModel.OneDrawName = tbItem.Name;
                DataModel.UIGetShow = 1;
                IsOneDraw = true;
                RefreshFreeDrawTime();
                var ee = new ElfGetDrawResultBack(1);
                EventDispatcher.Instance.DispatchEvent(ee);
                // DataModel.UIGetOneShow = 1;
            }
            else if (draw.Count == 10)
            {
                if (sore != null)
                {
                    sore.Clear();
                }
                var drawCount3 = draw.Count;

                //随机出精灵
                System.Random r = new System.Random();
                for (var i = 0; i < drawCount3; i++)
                {
                    int number = r.Next(0, drawCount3);
                    while (sore.Contains(number))
                    {
                        number = r.Next(0, drawCount3);
                    }
                    sore.Add(number);

                    DataModel.TenGetItem[i].ItemId = draw[number].ItemId;
                    DataModel.TenGetItem[i].Index = draw[number].Index;
                    DataModel.TenGetItem[i].Exdata.InstallData(draw[number].Exdata);
                    var tbItem = Table.GetItemBase(draw[number].ItemId);
                    DataModel.TenNameList[i] = tbItem.Name;
                }

                //for (var i = 0; i < drawCount3; i++)
                //{
                //    DataModel.TenGetItem[i].ItemId = draw[i].ItemId;
                //    DataModel.TenGetItem[i].Index = draw[i].Index;
                //    DataModel.TenGetItem[i].Exdata.InstallData(draw[i].Exdata);
                //    var tbItem = Table.GetItemBase(draw[i].ItemId);
                //    DataModel.TenNameList[i] = tbItem.Name;
                //}
                DataModel.UIGetShow = 1;
                IsOneDraw = false;
                //DataModel.UIGetOneShow = 0;
                var ee = new ElfGetDrawResultBack(10);
                EventDispatcher.Instance.DispatchEvent(ee);
            }
        }

        private void RefreshFreeDrawTime()
        {
            DataModel.IsFreeDraw = 0;
            PlayerDataManager.Instance.NoticeData.ElfDraw = false;
            var Serverfreetime = PlayerDataManager.Instance.GetExData64(14);
            DataModel.FreeElfTime = Extension.FromServerBinary(Serverfreetime);
            if (mTimeCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(mTimeCoroutine);
                mTimeCoroutine = null;
            }
            mTimeCoroutine = NetManager.Instance.StartCoroutine(TimerCoroutine(DataModel.FreeElfTime));
            EventDispatcher.Instance.DispatchEvent(new UIEvent_RefreshPush(7, 0));
        }

        private IEnumerator TimerCoroutine(DateTime time)
        {
            while (time > Game.Instance.ServerTime)
            {
                yield return new WaitForSeconds(1f);
                if (State == FrameState.Open)
                {
                    var timeSpan = time - Game.Instance.ServerTime;
                    var str = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    DataModel.DrawTimeString = str + GameUtils.GetDictionaryText(300404);
                }
            }
            DataModel.IsFreeDraw = 1;
            PlayerDataManager.Instance.NoticeData.ElfDraw = true;
        }

        private void OnClickGetShow(IEvent ievent)
        {
            var e = ievent as ElfGetOneShowEvent;
            var Moneytype = 0;
            var Money = 0;
            var drawCount = 0;
            DataModel.UIGetOneShow = 0;
            DataModel.UIGetShow = 0;
            if (e.Type == 1)
            {
                Moneytype = int.Parse(Table.GetClientConfig(500).Value);
                Money = DataModel.OneMoney;
                drawCount = 1;
            }
            else if (e.Type == 10)
            {
                Moneytype = int.Parse(Table.GetClientConfig(502).Value);
                Money = DataModel.TenMoney;
                drawCount = 10;
            }
            var data = PlayerDataManager.Instance.GetExData64(14);
            var FreeTime = Extension.FromServerBinary(data);
            var freecount = 0;
            if (FreeTime > Game.Instance.ServerTime)
            {
                freecount = 0;
            }
            else
            {
                freecount = 1;
            }
            //判断金币是否够了
            if (Moneytype == 2)
            {
                if (e.Type == 10 || freecount <= 0)
                {
                    if (Money > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                        PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.GoldRes);
                        return;
                    }
                }
            }
            //判断钻石是否够了
            else if (Moneytype == 3)
            {
                if (e.Type == 10 || freecount <= 0)
                {
                    if (Money > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Diamond)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                        PlayerDataManager.Instance.ShowItemInfoGet((int) eResourcesType.DiamondRes);
                        return;
                    }
                }
            }
            var tbbag = Table.GetBagBase((int) eBagType.Elf);
            if (tbbag.MaxCapacity < BagItemCount + drawCount)
            {
                var ee = new ShowUIHintBoard(270219);
                EventDispatcher.Instance.DispatchEvent(ee);
                return;
            }
            NetManager.Instance.StartCoroutine(DrawElf(drawCount));
        }

        private IEnumerator DrawElf(int drawCount)
        {
            using (new BlockingLayerHelper(0))
            {
                var code = 0;
                if (drawCount == 1)
                {
                    code = 200;
                }
                else if (drawCount == 10)
                {
                    code = 201;
                }
                var msg = NetManager.Instance.DrawLotteryPetEgg(code);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        //if (drawCount == 1)
                        //{
                        //    PlayerDataManager.Instance.NoticeData.ElfDraw = false;
                        //}

                        //if (code == 200)
                        //{i
                        //    PlayerDataManager.Instance.NoticeData.ElfDraw = false;
                        //}
                    }
                    else
                    {
                        if (msg.ErrorCode == (int) ErrorCodes.Unknow)
                        {
                            var e = new ShowUIHintBoard(200000001);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else if (msg.ErrorCode == (int) ErrorCodes.Error_ItemNoInBag_All)
                        {
                            var e = new ShowUIHintBoard(200002003);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else if (msg.ErrorCode == (int) ErrorCodes.Error_SeedTimeNotOver)
                        {
                            var e = new ShowUIHintBoard(220900);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                        else if (msg.ErrorCode == (int) ErrorCodes.ItemNotEnough)
                        {
                            var e = new ShowUIHintBoard(200000005);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                    }
                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
        }

        private void OnClickShowClose(IEvent ievent)
        {
            DataModel.UIGetShow = 0;
        }

        private void OnExDateUpdate(IEvent ievent)
        {
            ExDataUpDataEvent e = ievent as ExDataUpDataEvent;
            if (e != null && e.Key == 410)
            {
                var con = 10 - e.Value % 10;
                DataModel.ElfCount = string.Format(GameUtils.GetDictionaryText(100002302), con);
            }
        }
        private void OnExDateInit(IEvent ievent)
        {
            var exdataCount = (PlayerDataManager.Instance.GetExData((int)eExdataDefine.e410)) % 10;
            var con = 10 - exdataCount;
            DataModel.ElfCount = string.Format(GameUtils.GetDictionaryText(100002302), con);
        }
        #endregion

        #region 生成随机精灵

        private readonly List<int> IndextoAttrId = new List<int>
        {
            13,
            14,
            9,
            12,
            19,
            20,
            17,
            21,
            22,
            23,
            24,
            26,
            25,
            105,
            110,
            113,
            114,
            119,
            120,
            106,
            111,
            98,
            99
        };

        private void InitElfRandomProp(ElfItemDataModel bagItem)
        {
            var tbElf = Table.GetElf(bagItem.ItemId);
            if (tbElf == null)
            {
                return;
            }

            GameUtils.BuildShowElfExData(bagItem, bagItem.ItemId);

            var addCount = RandomElfAddCount(tbElf.RandomPropCount);
            InitElfAddAttr(bagItem, tbElf, addCount);
        }

        //随机属性条数随机
        private int RandomElfAddCount(int EquipRelateId)
        {
            if (EquipRelateId == -1)
            {
                return 0;
            }
            var tbRelate = Table.GetEquipRelate(EquipRelateId);
            if (tbRelate == null)
            {
                Logger.Error("EquipRelate Id={0} not find", EquipRelateId);
                return 0;
            }
            var AddCount = 0;
            var nRandom = MyRandom.Random(10000);
            var nTotleRandom = 0;
            for (var i = 0; i != tbRelate.AttrCount.Length; ++i)
            {
                nTotleRandom += tbRelate.AttrCount[i];
                if (nRandom < nTotleRandom)
                {
                    if (i == 0)
                    {
                        return 0;
                    }
                    AddCount = i;
                    break;
                }
            }
            return AddCount;
        }

        //初始化附加属性
        private void InitElfAddAttr(ElfItemDataModel bagItem, ElfRecord tbElf, int addCount)
        {
            if (addCount <= 0 || addCount > 6)
            {
                return;
            }
            int nRandom, nTotleRandom;
            var TbAttrPro = Table.GetEquipEnchantChance(tbElf.RandomPropPro);
            if (TbAttrPro == null)
            {
                Logger.Error("Equip InitAddAttr Id={0} not find EquipEnchantChance Id={1}", tbElf.Id, tbElf.RandomPropPro);
                return;
            }
            var tempAttrPro = new Dictionary<int, int>();
            var nTotleAttrPro = 0;
            for (var i = 0; i != 23; ++i)
            {
                var nAttrpro = TbAttrPro.Attr[i];
                if (nAttrpro > 0)
                {
                    nTotleAttrPro += nAttrpro;
                    tempAttrPro[i] = nAttrpro;
                }
            }
            //属性值都在这里
            var tbEnchant = Table.GetEquipEnchant(tbElf.RandomPropValue);
            if (tbEnchant == null)
            {
                Logger.Error("Equip InitAddAttr Id={0} not find tbEquipEnchant Id={1}", tbElf.Id, tbElf.RandomPropValue);
                return;
            }
            //整理概率
            var AttrValue = new Dictionary<int, int>();
            for (var i = 0; i != addCount; ++i)
            {
                nRandom = MyRandom.Random(nTotleAttrPro);
                nTotleRandom = 0;
                foreach (var i1 in tempAttrPro)
                {
                    nTotleRandom += i1.Value;
                    if (nRandom < nTotleRandom)
                    {
                        //AddCount = i1.Key;
                        AttrValue[i1.Key] = tbEnchant.Attr[i1.Key];
                        nTotleAttrPro -= i1.Value;
                        tempAttrPro.Remove(i1.Key);
                        break;
                    }
                }
            }
            var NowAttrCount = AttrValue.Count;
            if (NowAttrCount < addCount)
            {
                //Logger.Error("Equip InitAddAttr AddAttr Not Enough AddCount={0},NowAttrCount={1}", addCount, NowAttrCount);
            }

            for (var i = 0; i != NowAttrCount; ++i)
            {
                var nKey = AttrValue.Keys.Min();
                var nAttrId = GetAttrId(nKey);
                if (nAttrId == -1)
                {
                    continue;
                }
                var fValue = tbEnchant.Attr[nKey];
                AddAttr(bagItem, i, nAttrId, fValue);
                AttrValue.Remove(nKey);
            }
        }

        //增加附加属性
        private void AddAttr(ElfItemDataModel bagItem, int nIndex, int nAttrId, int nAttrValue)
        {
            bagItem.Exdata[nIndex + 2] = nAttrId;
            bagItem.Exdata[nIndex + 8] = nAttrValue;
        }


        private int GetAttrId(int index)
        {
            if (index > IndextoAttrId.Count || index < 0)
            {
                Logger.Error("GetAttrId index={0}", index);
                return -1;
            }
            return IndextoAttrId[index];
        }

        #endregion
    }
}
