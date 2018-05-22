#region using

using System;
using System.Collections.Generic;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;

namespace ScriptManager
{

    #endregion

    public enum EraState
    {
        Finish = 0,
        OnGoing = 1,
        NotStart = 2,
        PlayAnim = 3,
        PlayAnimEnd = 4
    }

    public enum EraPageType
    {
        Catalog = 0,
        Content = 1
    }

    public class EraInfo
    {
        public MayaBaseRecord Record;
        public int Order;  // 当前类型的排序（从0开始，显示时要考虑标题显示）
        public int Page;
        public int GotoPage;
        public EraState State;
        public bool TakeAward;
    }

    public class PageInfo
    {
        public int Type;    // 0 目录  1 内容
        public bool ShowTitle;
        public List<EraInfo> eraList = new List<EraInfo>();
    }

    public class EraManager : Singleton<EraManager>
    {
        private int currentEraId;
        private int currentProgress;
        private int maxProgress;
        private int totalPage;
        private int totalCatalogPage;
        private Dictionary<int, MayaBaseRecord> missionIdEraDict = new Dictionary<int, MayaBaseRecord>();//任务激活序章  key 任务id 
        private Dictionary<int, EraInfo> fubenIdEraDict = new Dictionary<int, EraInfo>();//副本 mayaInfo   key 副本id 
        private Dictionary<int, int> preEraIdDict = new Dictionary<int, int>();
        private Dictionary<int, EraInfo> eraInfoDict = new Dictionary<int, EraInfo>();
        private Dictionary<int, EraInfo> flagEraDict = new Dictionary<int, EraInfo>();//完成标记位   key 标记为id 
        private Dictionary<int, PageInfo> pageInfoDict = new Dictionary<int, PageInfo>();
        private Dictionary<int, List<EraInfo>> typeInfo = new Dictionary<int, List<EraInfo>>();//分类
        private Dictionary<int, bool> typeNotice = new Dictionary<int, bool>();                 //分类提醒
        private Dictionary<int, int> catalogPageOffset = new Dictionary<int, int>(); // 各个类型目录的偏移  0=0  1 = 1
        private int onePageCount = 6; // 同时展示出来的左右两页算一页 static
        public int PlayAnimEraId = -1;
        private int currPage = -1;
        public int CurrShiShiEraId = -1;

        public int CurrPage
        {
            get { return currPage; }
            set { currPage = value; }
        }


        public EraManager()
        {
            Init();
        }

        public int GetJumpToPage(int eraId)
        {
            var page = 1;   // 默认是第一页
            EraInfo info;
            if (eraInfoDict.TryGetValue(eraId, out info))
            {
                page = info.GotoPage;
            }
            return LogicPage2UiPage(page);
        }

        /// <summary>
        /// 获取页
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetCatalogPage(int id)
        {
            int page;
            if (catalogPageOffset.TryGetValue(id, out page))
            {
                var uiPage = LogicPage2UiPage(page);

                return uiPage;
            }

            return 1;
        }

        public PageInfo GetPageInfo(int page)
        {
            PageInfo info;
            if (pageInfoDict.TryGetValue(page, out info))
            {
                return info;
            }

            return null;
        }


        public List<int> GetPageTypeList()
        {
            var pageTypes = new List<int>(totalPage);
            for (var i = 0; i < totalPage; ++i)
            {
                var pageInfo = pageInfoDict[i];
                if (pageInfo.Type == 0)
                {
                    if (pageInfo.ShowTitle)
                    {
                        pageTypes.Add(0);
                    }
                    else
                    {
                        pageTypes.Add(1);
                    }
                }
                else
                {
                    pageTypes.Add(2);
                }
            }

            return pageTypes;
        }

        public EraInfo IsEraBookFuben(int fubenId)
        {
            EraInfo info;
            if (fubenIdEraDict.TryGetValue(fubenId, out info))
            {
                //if (info.Record.Type == 0)
                //{
                    if (info.State == EraState.OnGoing)
                    {
                        return info;
                    }                
                //}
                //else
                //{
                    if (info.State == EraState.NotStart)
                    {
                        return info;
                    }
                //}
            }
            return null;
        }

        //主界面任务/书 跳转到页面
        public void GotoCurrentPage()
        {
            var info =  GetEraInfo(CurrentEraId);
            GotoEraBookPage(info.Page + 1, true);
            SelectCurrentEra(CurrentEraId);
        }

        //选中当前进行的元素
        public void SelectCurrentEra(int eraID)
        {
            EventDispatcher.Instance.DispatchEvent(new EraSelectCurrEra(eraID));
        }

        public int LogicPage2UiPage(int logicPage)
        {
            return logicPage + 1;
        }

        public int UiPage2LogicPage(int uipage)
        {
            return uipage - 1;
        }

        //刷新页内容  页码
        public List<int> RefreshPageContent(int uipage, ref List<string> effectPathList)
        {
            var args = new Event_UpdateEraPage();
            args.Page = uipage;
            var logicPage = UiPage2LogicPage(uipage);
            currPage = uipage;
            //EventDispatcher.Instance.DispatchEvent(new Event_EraUpdateLineLight());

            PageInfo pageInfo;
            if (!pageInfoDict.TryGetValue(logicPage, out pageInfo))
            {
                return null;
            }
            args.Type = pageInfo.Type;
            args.ShowTitle = pageInfo.ShowTitle;//是否显示标题
            args.Page = uipage;
            for (var i = 0; i < pageInfo.eraList.Count; ++i)
            {
                args.EraIdList.Add(pageInfo.eraList[i].Record.Id);
            }
            //更新页数据
            EventDispatcher.Instance.DispatchEvent(args);

            var retList = new List<int>();
            retList.Add(args.Type);
            if (args.Type == (int) EraPageType.Content)
            {
                //retList.Add(pageInfo.eraList[0].Record.ModelId);
                string[] paths = pageInfo.eraList[0].Record.EffectPath.Split('|');
                effectPathList = paths.ToList();
            }
            else
            {
                if (effectPathList != null)
                {
                    effectPathList.Clear();
                }
                //retList.Add(-1);
            }
            return retList;
        }

        public void GotoEraBookPage(int uipage, bool init)
        {
            if (uipage <= 0)
            {
                Logger.Error("GotoEraBookPage: " + uipage);
                uipage = 1;
            }

            if (init)
            {
                var args = new UIInitArguments();
                args.Args = new List<int>();
                args.Args.Add(uipage);
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.EraBookUI, args));           
            }
            else
            {
                EventDispatcher.Instance.DispatchEvent(new Event_EraTurnPage(uipage));            
            }
        }

        /// <summary>
        /// 获取信息列表
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<EraInfo> GetEraInfos(int type)
        {
            List<EraInfo> infoList;
            typeInfo.TryGetValue(type, out infoList);

            return infoList;
        }

        public EraInfo GetEraInfo(int eraId)
        {
            EraInfo info;
            if (eraInfoDict.TryGetValue(eraId, out info))
            {
                return info;
            }

            return null;
        }

        public EraState GetEraState(int eraId)
        {
            EraInfo info;
            if (eraInfoDict.TryGetValue(eraId, out info))
            {
                return info.State;
            }

            return EraState.NotStart;
        }

        public void Init()
        {
            currentEraId = -1;
            preEraIdDict.Clear();
            missionIdEraDict.Clear();
            pageInfoDict.Clear();
            catalogPageOffset.Clear();
            flagEraDict.Clear();
            eraInfoDict.Clear();

            var typeCountDict = new Dictionary<int, int>();

            var lastId = -1;
            Table.ForeachMayaBase(record =>
            {
                int oldCount = 0;
                if (!typeCountDict.TryGetValue(record.Type, out oldCount))
                {
                }
                typeCountDict[record.Type] = oldCount + 1;

                var info = new EraInfo();
                info.Order = typeCountDict[record.Type] - 1;
                info.Record = record;
                eraInfoDict[record.Id] = info;

                if (record.ActiveType == (int) EraActiveType.Mission)
                {
                    if (record.ActiveParam[0] >= 0)
                    {
                        missionIdEraDict[record.ActiveParam[0]] = record;
                    }
                }

                if (record.FunBenId >= 0)
                {
                    fubenIdEraDict[record.FunBenId] = info;                
                }

                //元素标记位Dic
                if (record.FinishFlagId > 0)
                {
                    flagEraDict[record.FinishFlagId] = info;
                }

                List<EraInfo> infoList;
                if (!typeInfo.TryGetValue(record.Type, out infoList))
                {
                    infoList = new List<EraInfo>();
                    typeInfo[record.Type] = infoList;
                    typeNotice[record.Type] = false;
                }
                infoList.Add(info);

                preEraIdDict[record.Id] = lastId;
                lastId = record.Id;

                return true;
            });


            // ---------------- UI显示相关 -------------------------
            var titleUse = 1;
            var totalContentPage = 0; //内容页
            totalCatalogPage = 0;   //导航页
            var typePageCount = new Dictionary<int, int>(); // 各个类型目录占用的页数 //0=1  1 = 1
            var contentPageOffset = new Dictionary<int, int>(); //内容页偏移  //0=0  1=10
            for (var i = 0; i < typeCountDict.Count; ++i)
            {
                var count = typeCountDict[i];
                onePageCount = count;
                typePageCount[i] = count / onePageCount;
                //typePageCount[i] = (int)Math.Ceiling((double)(titleUse + count) / onePageCount);
                if (i > 0)
                {
                    catalogPageOffset[i] = typePageCount[i] + typeCountDict[i - 1];
                    contentPageOffset[i] = contentPageOffset[i - 1] + typeCountDict[i - 1];
                }
                else
                {
                    catalogPageOffset[i] = 0;
                    contentPageOffset[i] = 0;
                }

                totalCatalogPage += typePageCount[i];//导航页 1+1 = 2
                totalContentPage += count;//内容页 10+9 = 19
            }

            totalPage = totalCatalogPage + totalContentPage; //21

            // 刷新当前所在的页数及点击前往的页数
            var enumorator1 = eraInfoDict.GetEnumerator();
            while (enumorator1.MoveNext())
            {
                var eraValue = enumorator1.Current.Value;
                var pageOffset = catalogPageOffset[eraValue.Record.Type];// 0=0  1 = 1

                onePageCount = typeCountDict[eraValue.Record.Type];
                eraValue.Page = pageOffset + eraValue.Order/onePageCount;
                eraValue.GotoPage = totalCatalogPage + catalogPageOffset[eraValue.Record.Type] + eraValue.Order - 1; //contentPageOffset[eraValue.Record.Type]       
                PageInfo pageInfo;
                if (!pageInfoDict.TryGetValue(eraValue.Page, out pageInfo))
                {
                    pageInfo = new PageInfo();
                    pageInfo.Type = 0;
                    pageInfo.ShowTitle = ((eraValue.Order + titleUse) < onePageCount);
                    pageInfoDict[eraValue.Page] = pageInfo;
                }
                pageInfoDict[eraValue.Page].eraList.Add(eraValue);

                pageInfo = new PageInfo();
                pageInfo.ShowTitle = false;
                pageInfo.Type = 1;
                pageInfo.eraList.Add(eraValue);
                pageInfoDict[eraValue.GotoPage] = pageInfo;
            }        
        }

        //获取元素状态
        private EraState GetState(EraInfo eraInfo)
        {
            if (eraInfo == null || eraInfo.Record == null)
            {
                return EraState.Finish;
            }

            var tbMayaBase = eraInfo.Record;
            
            if (tbMayaBase.ActiveType == (int)EraActiveType.Mission) 
            {
                //播放动画
                if (tbMayaBase.Id == PlayAnimEraId )
                {
                    return EraState.PlayAnim;
                }

                //正在进行
                if (tbMayaBase.Id == CurrentEraId)
                {
                    return EraState.OnGoing;
                }

                //完成
                var finish = PlayerDataManager.Instance.GetFlag(tbMayaBase.FinishFlagId);
                if (finish)
                {
                    return EraState.Finish;
                } 
                return EraState.NotStart;

            }
        
            //条件
            if (tbMayaBase.ActiveType == (int) EraActiveType.Condition)
            {
                var finish = PlayerDataManager.Instance.GetFlag(tbMayaBase.FinishFlagId);
                if (finish)
                {
                    return EraState.Finish;
                } 
                return EraState.NotStart;             
            }
            return EraState.Finish;
        }

        public void ResetState()
        {            
            var enumorator = eraInfoDict.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var info = enumorator.Current.Value;
                info.State = GetState(info);
                info.TakeAward = PlayerDataManager.Instance.GetFlag(info.Record.GotAward);

                if (typeNotice[info.Record.Type] == false)
                {
                    if (ShowNotice(info))
                    {
                        typeNotice[info.Record.Type] = true;
                    }
                }
            }

            UpdateNotice();
        }

        public bool ShowNotice(EraInfo info)
        {
            if (info == null)
            {
                return false;
            }

            return (info.State == EraState.Finish || info.State == EraState.PlayAnim || info.State == EraState.PlayAnimEnd)
                   && info.TakeAward == false;
        }

        private void UpdateNotice()
        {
            PlayerDataManager.Instance.NoticeData.EraAward = GetTotalNotice();
        }

        public bool RefreshEraNotice(int type)
        {
            var notice = false;
            var infos = GetEraInfos(type);
            if (infos == null)
                return false;
            var enumorator1 = infos.GetEnumerator();
            while (enumorator1.MoveNext())
            {
                var cellData = enumorator1.Current;
                if (ShowNotice(cellData))
                {
                    notice = true;
                    break;
                }
            }

            typeNotice[type] = notice;

            UpdateNotice();

            return notice;
        }

        public bool GetTotalNotice()
        {
            var notice = false;
            var enumorator = typeNotice.GetEnumerator();
            while (enumorator.MoveNext())
            {
                notice = enumorator.Current.Value;
                if (notice)
                    break;
            }

            return notice;
        }

        public void Clear()
        {
            currentEraId = -1;
            PlayAnimEraId = -1;
        }

        //刷新标记位
        public void RefreshFlagId(int flagId)
        {
            EraInfo info;
            if (flagEraDict.TryGetValue(flagId, out info))
            {
                info.State = GetState(info);            
            }
        }

        //设置元素动画
        public void SetAnimEra(int eraId)
        {
            //if (PlayAnimEraId != -1)
            //{
            //    var tbMayaBaseLast = Table.GetMayaBase(PlayAnimEraId);
            //    if (tbMayaBaseLast != null)
            //    {
            //        RefreshFlagId(tbMayaBaseLast.FinishFlagId);
            //    }
            //}

            //播放元素动画 =  根据当前进行的元素id
            PlayAnimEraId = Math.Max(0, eraId);
            var tbMayaBase = Table.GetMayaBase(PlayAnimEraId);
            if (tbMayaBase != null)
            {
                //刷新完成标记位
                RefreshFlagId(tbMayaBase.FinishFlagId);
            }
        }

        public void RefreshByMainMission()
        {
            if (PlayerDataManager.Instance.ExtData.Count <= 0)
                return;

            var eraId = PlayerDataManager.Instance.GetExData(eExdataDefine.e710);
            CurrentEraId = eraId;

            // 刷新事件
            EventDispatcher.Instance.DispatchEvent(new Event_EraProgressUpdate());
        }

        public void RereshEraProgress()
        {
            var tbMayaBase = Table.GetMayaBase(CurrentEraId);
            if (tbMayaBase == null)
            {
                return;
            }

            if (tbMayaBase.ActiveType == (int)EraActiveType.Mission)
            {
                var maxMissionId = tbMayaBase.ActiveParam[0];

                var lastEraId = GetLastEraId(currentEraId);
                var beginMissionId = GetMissionId(lastEraId);
                var beginMissionIndex = GetMissionIndex(beginMissionId) + 1;
                var maxMissionIndex = GetMissionIndex(maxMissionId);
                var missionId = GameUtils.GetCurMainMissionId();
                var currentMissionIndex = GetMissionIndex(missionId);

                if (currentMissionIndex < beginMissionIndex)
                {
                    currentMissionIndex = beginMissionIndex;                
                }
                else if (currentMissionIndex > maxMissionIndex)
                {
                    currentMissionIndex = maxMissionIndex;                
                }

                maxProgress = maxMissionIndex - beginMissionIndex;  // 特殊处理
                currentProgress = currentMissionIndex - beginMissionIndex;
            }        
        }

        public MayaBaseRecord GetEraRecordByMissionId(int missionId)
        {
            MayaBaseRecord record;
            if (missionIdEraDict.TryGetValue(missionId, out record))
            {
                return record;
            }
            return null;
        }

        private int GetMissionId(int eraId)
        {
            var tbMayaBase = Table.GetMayaBase(eraId);
            if (tbMayaBase != null)
            {
                return tbMayaBase.ActiveParam[0];
            }

            return -1;
        }

        // 获得当前进行的玛雅纪元Id
        public int CurrentEraId
        {
            get { return currentEraId; }
            private set
            {
                if (value < 0)
                {
                    currentEraId = value;
                    return;
                }

                if (eraInfoDict.Count == 0)
                    return;

                var lastEraId = currentEraId;
                currentEraId = value;
                if (currentEraId != lastEraId)
                {
                    EraInfo info;
                    if (eraInfoDict.TryGetValue(lastEraId, out info))
                    {
                        info.State = GetState(info);                    
                    }
                }

                if (eraInfoDict.ContainsKey(currentEraId))
                {
                    eraInfoDict[currentEraId].State = EraState.OnGoing;                
                }
            
                RereshEraProgress();
            }
        }

        public int CurrentProgress
        {
            get { return currentProgress; }
        }
        public int MaxProgress
        {
            get { return maxProgress; }
        }

        public int TotalPage { get { return totalPage; } }
        public int TotalCatalogPage { get { return totalCatalogPage; } }

        // 0，进行中 1，去挑战 2，挑战完成
        public int GetCurrentEraState()
        {
            if (currentProgress < maxProgress)
            {
                return 0;
            }

            if (currentEraId < 0)
                return 0;

            var tbMaya = Table.GetMayaBase(currentEraId);
            if (tbMaya == null || tbMaya.FunBenId < 0)
                return 0;

            var finish = PlayerDataManager.Instance.GetFlag(tbMaya.FinishFlagId);
            if (!finish)
            {
                return 1;
            }

            return 2;
        }

        public bool IsRealMax()
        {
            var max = GetCurrentEraState() == 1;
            if (max)
            {
                if (eMissionState.Unfinished == GameUtils.GetCurMainMissionState())
                {
                    return true;
                }
            }

            return false;
        }

        private int GetLastEraId(int eraId)
        {
            int lastEraId;
            if (preEraIdDict.TryGetValue(eraId, out lastEraId))
            {
                return lastEraId;
            }

            return -1;
        }

        private int GetMissionIndex(int missionId)
        {
            if (missionId < 0)
                return 0;

            var tbMission = Table.GetMissionBase(missionId);
            if (tbMission != null)
            {
                return tbMission.MissionBianHao;
            }

            return 0;
        }

        public int GetEraIdByNotTakeReward()
        {        
            var enumorator = eraInfoDict.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var info = enumorator.Current.Value; 
                if (info != null && ShowNotice(info))
                {
                    return info.Record.Id;
                }
            }
            return -1;
        }
    }
}