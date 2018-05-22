#region using
using System;
using System.Collections;
using EventSystem;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using DataTable;
using Shared;
#endregion

namespace GameUI
{
    enum TurnState
    {
        UnInit = -1,    // 未打开界面
        Show,           // 未翻页
        Turning,        // 翻页中
        StartTurnAnim,  // 翻页开始动画
        EndTurnAnim     // 翻页结束动画
    }

    [Serializable]
    public class PageObjects
    {
        public GameObject Left;
        public GameObject Right;
    }

    public class EraBookFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public EraBookDummy BookDummy;
        public List<UIButton> CatalogButtons;
        public PageObjects CatalogPage;
        public PageObjects SkillPage;
        public List<GameObject> BookmarkList;
        public GameObject AnimGameObject;
        public float ShowBeforeTurnOver = 0.05f;
        public float ObjectOffset = 0.05f;
        public List<Texture2D> PatternTexture2Ds = new List<Texture2D>();
        public List<Texture2D> BlankTexture2Ds = new List<Texture2D>();
        public Texture2D CatalogBlankTexture;
        public Texture2D SkillTexture;
        public GameObject ZheZhao;
        public GameObject Effect;
        public Transform StampGameObject;

        private PageObjects showPageObjects;
        private TweenAlpha alphaAnim;
        private TurnState turnState = TurnState.UnInit;
        private int showModelId = -1;
        private List<string> effectPathList;
        private GameObject[] effectGameObjectList = { null, null };
        private float logicStartPage = -1;
        private int logicTurntoPage = -1;
        private int logicLastPage = -1;
        private int catalogTotalPage;
        private int showPage;
        private int startPage;
        private int turnToPage;
        private int lastPage;
        private int showMaxPage;
        private bool resetStartTexture;
        private GameObject effectGameObject;

        public UISprite[] CellLineLight;
        public UISprite[] CellLineLight_shiShi;
        private UISprite[] CellLineLight_Temp;

        public GameObject Go_ChuanShuo;//传说
        public GameObject Go_ShiShi;//史诗

        private void OnDisable()
        {
#if !UNITY_EDITOR
            try
            {
#endif

            EventDispatcher.Instance.RemoveEventListener(Event_EraTurnPage.EVENT_TYPE, OnEvent_TurnPage);
            EventDispatcher.Instance.RemoveEventListener(Event_EraPlayAnim.EVENT_TYPE, OnEvent_PlayAnim);
            EventDispatcher.Instance.RemoveEventListener(Event_EraAddTurnPage.EVENT_TYPE, OnEvent_AddPage);
            DestroyAnim();


#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            try
            {
#endif

            alphaAnim = AnimGameObject.GetComponent<TweenAlpha>();
            ZheZhao.SetActive(true);
            BookDummy.Builder.TurningCallBack = TurningPageCallBack;

            catalogTotalPage = EraManager.Instance.TotalCatalogPage;
            showMaxPage = catalogTotalPage + 5;

            InitBook(showMaxPage + 1);
            InitPage(-1);

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void Start()
        {
#if !UNITY_EDITOR
            try
            {
#endif

            BookDummy.Builder.rebuild = true;
            BookDummy.Builder.ForceUpdate();

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void OnEnable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
            var controllerBase = UIManager.Instance.GetController(UIConfig.EraBookUI);
            if (controllerBase == null)
            {
                return;
            }

            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            Binding.SetBindDataSource(controllerBase.GetDataModel("CurrSelectSkillInfo"));
            EventDispatcher.Instance.AddEventListener(Event_EraTurnPage.EVENT_TYPE, OnEvent_TurnPage);
            EventDispatcher.Instance.AddEventListener(Event_EraPlayAnim.EVENT_TYPE, OnEvent_PlayAnim);
            EventDispatcher.Instance.AddEventListener(Event_EraAddTurnPage.EVENT_TYPE, OnEvent_AddPage);

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void Event_EraUpdateLineLightCallBack()
        {
            var currEraId = EraManager.Instance.CurrentEraId;
            var currPage = EraManager.Instance.CurrPage;
            CellLineLight_Temp = null;
            var lineLightCount = 0;
            if (currPage == 12)
            {
                var pageInfo = EraManager.Instance.GetPageInfo(currPage - 1);
                if (pageInfo != null)
                {
                    for (int i = 0; i < pageInfo.eraList.Count; i++)
                    {
                        var state = pageInfo.eraList[i].State;
                        if (state == EraState.OnGoing)
                        {
                            lineLightCount = i;
                            break;
                        }
                    }

                    if (lineLightCount == 0)
                    {
                        for (int i = 0; i < pageInfo.eraList.Count; i++)
                        {
                            var state = pageInfo.eraList[i].State;
                            if (state == EraState.Finish || state == EraState.PlayAnimEnd)
                            {
                                lineLightCount ++;
                            }
                        }
                        lineLightCount = lineLightCount - 1 < 0 ? 0 : lineLightCount - 1;
                    }
                    currEraId = pageInfo.eraList[lineLightCount].Record.Id;
                }
                CellLineLight_Temp = CellLineLight_shiShi;
                StartCoroutine(SetActiveGoByType(1, 0));
            }
            else
            {
                StartCoroutine(SetActiveGoByType(0, 0));
            }

            if (currEraId == -1)
            {
                var pageInfo2 = EraManager.Instance.GetPageInfo(0);
                if (pageInfo2 != null)
                {
                    var count = pageInfo2.eraList.Count;
                    var isFinish = PlayerDataManager.Instance.GetFlag(pageInfo2.eraList[count - 1].Record.FinishFlagId);
                    if (isFinish)
                    {
                        currEraId = pageInfo2.eraList[count - 1].Record.Id;
                    }
                }
            }
            if (currEraId == -1) return;
            var tempEraId = -1;
            tempEraId = currEraId;
            if (currEraId / 10 > 0)
            {                
                tempEraId = (currEraId - 1)%10;                
            }
            
            var eraInfo = EraManager.Instance.GetEraInfo(currEraId);
            if (eraInfo != null)
            {
                if (currPage != 12)
                {
                    var isFinish2 = PlayerDataManager.Instance.GetFlag(eraInfo.Record.FinishFlagId);
                    if (!isFinish2)
                    {
                        tempEraId = EraManager.Instance.IsRealMax() ? tempEraId : tempEraId - 1;
                    }                                        
                }

                if (CellLineLight_Temp != null)
                {
                    for (int i = 0; i < CellLineLight_Temp.Count(); i++)
                    {
                        if (CellLineLight_Temp[i] == null) continue;
                        CellLineLight_Temp[i].fillAmount = 0;
                        if (eraInfo.Page == currPage - 1)
                        {
                            if (i < tempEraId)
                            {
                                CellLineLight_Temp[i].fillAmount = 1;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < CellLineLight.Count(); i++)
                    {
                        if (CellLineLight[i] == null) continue;
                        CellLineLight[i].fillAmount = 0;
                        if (eraInfo.Page == currPage - 1)
                        {
                            if (i < tempEraId)
                            {
                                CellLineLight[i].fillAmount = 1;
                            }
                        }
                    }
                }
             }            
        }


        void InitBook(int pageNums)
        {
            BookDummy.Builder.useholepage = false;
            BookDummy.Builder.InitTurnAnim = true;

            BookDummy.Builder.pageparams[0].madefront = PatternTexture2Ds[0];
            BookDummy.Builder.NumPages = pageNums;

            var typeList = EraManager.Instance.GetPageTypeList();
            var pageNum = Math.Min(typeList.Count, BookDummy.Builder.NumPages);
            //var patternCount = PatternTexture2Ds.Count;
            var markIndex = 0;
            var blankCount = BlankTexture2Ds.Count;
            for (var i = 0; i < pageNum; ++i)
            {
                var parms = BookDummy.Builder.pageparams[i + 1];
                var prevParms = BookDummy.Builder.pageparams[i];

                parms.visobjlow = -1f;
                parms.visobjhigh = 1.1f;

                var type = typeList[i];
                if (type == 0)
                { // 有title
                    prevParms.madeback = CatalogBlankTexture;
                    parms.madefront = CatalogBlankTexture;

                    if (parms.objects == null)
                    {
                        parms.objects = new List<MegaBookPageObject>();
                    }

                    //parms.visobjlow = -100f;
                    //parms.visobjhigh = 100f;
                    //var pageObj = CreatePageObject(0, BookmarkList[markIndex], true);
                    //parms.objects.Add(pageObj);
                    //++markIndex;
                }
                else if (type == 1)
                {
                    prevParms.madeback = CatalogBlankTexture;
                    parms.madefront = CatalogBlankTexture;
                }
                else if (type == 2)
                {
                    prevParms.madeback = SkillTexture;
                    var index1 = MyRandom.Random(10000) % blankCount;
                    parms.madefront = BlankTexture2Ds[index1];
                }
            }
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        private void OnEvent_TurnPage(IEvent ievent)
        {
            var e = ievent as Event_EraTurnPage;
            if (e == null)
                return;

            if (e.Init)
            {
                InitPage(-1);
            }

            var p = e.Page;
            if (e.Page <= 0) // 默认显示第一页
            {
                Logger.Error("Event_EraTurnPage  ", e.Page);
                p = 1;
            }
            SetPage(p);
        }

        private void OnEvent_AddPage(IEvent ievent)
        {
            var e = ievent as Event_EraAddTurnPage;
            if (e == null)
                return;

            SetPage(logicLastPage + e.AddPage);
        }

        private void OnEvent_PlayAnim(IEvent ievent)
        {
            var e = ievent as Event_EraPlayAnim;
            if (e == null)
                return;

            DestroyAnim();
            ComplexObjectPool.NewObject("Effect/Books/MFS_gaizhang.prefab", go =>
            {
                if (null == go)
                {
                    return;
                }

                effectGameObject = go;

                go.transform.parent = StampGameObject;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.Euler(0, 0, 0);

            });
        }

        private void DestroyAnim()
        {
            if (effectGameObject != null)
            {
                ComplexObjectPool.Release(effectGameObject, true);
                effectGameObject = null;
            }
        }

        private void InitPage(int page)
        {
            SkillPage.Left.SetActive(false);
            SkillPage.Right.SetActive(false);
            CatalogPage.Left.SetActive(false);
            CatalogPage.Right.SetActive(false);

            ClearPage();

            BookDummy.Builder.SetPage(page, true);
            BookDummy.Builder.ForceUpdate();
            ChangeState(TurnState.Show);

            alphaAnim.Sample(1f, true);
            alphaAnim.enabled = false;

            logicLastPage = -1;
        }

        public void Close()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraBookUI));
            InitPage(-1);
        }

        //传说Btn
        public void GotoCatalog1()
        {
            if (1 != logicLastPage)
            {
                GotoCatalog(0);   
            }
        }

        //史诗Btn
        public void GotoCatalog2()
        {
            if (12 != logicLastPage)
            {
                GotoCatalog(1);
            }
        }

        
        private IEnumerator SetActiveGoByType(int type,float delay = 0 )
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            if (Go_ShiShi != null && Go_ChuanShuo != null)
            {
                Go_ChuanShuo.SetActive(type==0);
                Go_ShiShi.SetActive(type==1);           
            }
        }

        public void GotoCatalog3()
        {
            GotoCatalog(2);

        }
        public void GotoCatalog4()
        {
            GotoCatalog(3);
        }

        private void GotoCatalog(int type)
        {
            if (turnState == TurnState.Show)
            {
                //StartCoroutine(SetActiveGoByType(type, 0f));
                EventDispatcher.Instance.DispatchEvent(new Event_CalalogClick(type));
            }
        }

        //下一页
        public void ShowNextPage(int type)
        {
            if (turnState == TurnState.Show)
            {
                SetPage(logicLastPage + 1);
                EventDispatcher.Instance.DispatchEvent(new Event_EraBookNextPage(logicLastPage + 1));
            }
        }

        //上一页
        public void ShowPrevPage()
        {
            if (turnState == TurnState.Show)
            {
                SetPage(logicLastPage - 1);
                EventDispatcher.Instance.DispatchEvent(new Event_EraBookNextPage(logicLastPage - 1));
            }
        }

        public void OnClick_Go()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraBookOperate(0));
        }

        //领取奖励
        public void OnClick_TakeWard()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraBookOperate(1));
        }

        public void OnClick_TitleTips()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraBookOperate(2));
        }

        public void OnClick_CloseTitleTips()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraBookOperate(3));
        }

        public void OnClick_EraGoto()
        {
            if (turnState == TurnState.Show)
                EventDispatcher.Instance.DispatchEvent(new OnClick_EraGotoEvent());
        }

        private void ChangeType(EraPageType type)
        {
            switch (type)
            {
                case EraPageType.Catalog:
                    showPageObjects = CatalogPage;
                    break;
                case EraPageType.Content:
                    showPageObjects = SkillPage;
                    break;
                default:
                    showPageObjects = null;
                    break;
            }
        }

        private void DeletePageObject(int page, int type)
        {
            if (type == 1)
            {
                BookDummy.Builder.pages[page].objects.RemoveAll(o => { return o.isBookMark; });
            }
            else if (type == 0)
            {
                BookDummy.Builder.pages[page].objects.RemoveAll(o => { return !o.isBookMark; });
            }
            else
            {
                BookDummy.Builder.pages[page].objects.Clear();
            }
        }

        private MegaBookPageObject CreatePageObject(int side, GameObject gameObject, bool bookmark)
        {
            var pageObject = new MegaBookPageObject();
            pageObject.isBookMark = bookmark;
            if (bookmark)
            {
                pageObject.visilow = -100;
                pageObject.visihigh = 100;
            }
            if (side == 0)
            {
                pageObject.pos = new Vector3(50, 0, 50);
                pageObject.rot = new Vector3(-90, -90, 0);
                pageObject.offset = -ObjectOffset;
                pageObject.obj = gameObject;
                pageObject.attachforward = new Vector3(0.005f, 0.0f, 0.0f);
            }
            else
            {
                pageObject.pos = new Vector3(50, 0, 50);
                pageObject.rot = new Vector3(90, 90, 0);
                pageObject.offset = ObjectOffset;
                pageObject.obj = gameObject;
                pageObject.attachforward = new Vector3(0.005f, 0.0f, 0.0f);
            }

            return pageObject;
        }

        // side: 0,正 1,反
        private void AttachPageObjects(int uiPage, int side, GameObject gameObject, bool bookmark)
        {
            if (uiPage < 0)
                return;

            if (!bookmark)
            {
                DeletePageObject(uiPage, 0);
            }

            var pageObject = CreatePageObject(side, gameObject, bookmark);
            gameObject.SetActive(true);
            BookDummy.Builder.AttachObject(BookDummy.Builder.pages[uiPage], pageObject);
        }

        private void ClearPageObject(int page)
        {
            if (page < 0 || page >= BookDummy.Builder.pages.Count)
                return;

            var objs = BookDummy.Builder.pages[page].objects;
            for (var i = 0; i < objs.Count; ++i)
            {
                if (objs[i].obj != null && objs[i].isBookMark == false)
                {
                    objs[i].obj.SetActive(false);
                    objs[i].obj = null;
                }
            }

            DeletePageObject(page, 0);
        }

        private void ClearPage()
        {
            ClearPageObject(lastPage);
            ClearPageObject(lastPage - 1);

            if (lastPage != turnToPage)
            {
                ClearPageObject(turnToPage);
                ClearPageObject(turnToPage - 1);
            }

            DestroyAnim();
        }

        private void SetPage(float val)
        {
            if (val < -1)
                val = 1;

            if (val > EraManager.Instance.TotalPage)
            {
                val = EraManager.Instance.TotalPage;
            }

            if (turnState == TurnState.Show)
            {
                if ((int) val == logicLastPage)
                {
                    return;
                }

                logicStartPage = logicLastPage;
                logicTurntoPage = (int) val;

                SetTurnToPage(logicStartPage, logicTurntoPage);
                ChangeState(TurnState.StartTurnAnim);
            }
            else
            {
                Logger.Error("SetPage:" + showPage);
            }
        }

        private void SetTurnToPage(float lStartPage, float lTurntoPage)
        {
            startPage = (int)lStartPage;
            turnToPage = (int)lTurntoPage;
            showPage = startPage;
            if (showPage < 0)
                showPage = 0;

            var turnCount = (int)(lTurntoPage - lStartPage);
            if (lTurntoPage > lStartPage)
            {
                if (turnToPage > showMaxPage)
                {
                    startPage = (int)BookDummy.Builder.page;
                    turnToPage = Math.Min(startPage + turnCount, showMaxPage);

                    if (startPage == turnToPage)
                    {
                        startPage -= turnCount;
                        resetStartTexture = true;
                    }
                }
            }
            else
            {
                if (startPage > showMaxPage)
                {
                    startPage = showMaxPage;
                    resetStartTexture = true;
                }

                if (turnToPage > showMaxPage)
                {
                    turnToPage = showMaxPage;
                    resetStartTexture = true;
                }

                if (turnToPage == startPage)
                {
                    turnToPage = startPage - 1;
                }
            }

            var beginPage = Math.Max(startPage, 0);
            var endPage = turnToPage;
            if (endPage == (int)BookDummy.Builder.page)
            {
                resetStartTexture = true;
            }
            var patternCount = PatternTexture2Ds.Count;

            if (beginPage < endPage)
            {
                for (var i = beginPage; i < endPage; ++i)
                {
                    var index1 = MyRandom.Random(10000) % patternCount;
                    var texture = PatternTexture2Ds[index1];
                    if (i != beginPage)
                    {
                        BookDummy.Builder.SetPageTexture(texture, i, true);
                    }
                    BookDummy.Builder.SetPageTexture(texture, i, false);
                }
            }
            else
            {
                for (var i = beginPage - 1; i > endPage; --i)
                {
                    var index1 = MyRandom.Random(10000) % patternCount;
                    var texture = PatternTexture2Ds[index1];
                    if (i != beginPage - 1)
                    {
                        BookDummy.Builder.SetPageTexture(texture, i, false);
                    }
                    BookDummy.Builder.SetPageTexture(texture, i, true);
                }
            }

            SetPageTexture(logicTurntoPage, endPage);
        }

        private void SetPageTexture(int logicPage, int uiPage)
        {
            if (uiPage < 0)
                return;

            if (logicPage <= catalogTotalPage)
            {
                BookDummy.Builder.SetPageTexture(CatalogBlankTexture, uiPage - 1, false);
                BookDummy.Builder.SetPageTexture(CatalogBlankTexture, uiPage, true);
            }
            else
            {
                BookDummy.Builder.SetPageTexture(SkillTexture, uiPage - 1, false);
                BookDummy.Builder.SetPageTexture(BlankTexture2Ds[0], uiPage, true);
            }
        }

        private void ShowEffect(bool show)
        {
            for (var i = 0; i < effectGameObjectList.Length; ++i)
            {
                var effectGameObject = effectGameObjectList[i];
                if (effectGameObject != null)
                {
                    ComplexObjectPool.Release(effectGameObject, forceDestory: true);
                    effectGameObjectList[i] = null;
                }
            }

            if (show && effectPathList != null)
            {
                for (var j = 0; j < effectPathList.Count; ++j)
                {
                    NewEffect(effectPathList[j], j);
                }
            }
            Effect.SetActive(show);
        }

        private void NewEffect(string effectPath, int index)
        {
            ComplexObjectPool.NewObject(effectPath, go =>
            {
                if (null == go)
                {
                    return;
                }

                var renderQueue = go.GetComponent<ChangeRenderQueue>();
                if (renderQueue == null)
                {
                    renderQueue = go.AddComponent<ChangeRenderQueue>();
                }
                renderQueue.CustomRenderQueue = 3145;
                effectGameObjectList[index] = go;

                go.transform.parent = Effect.gameObject.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
            });
        }

        private void TurningPageCallBack(float delta)
        {
            if (turnState == TurnState.Turning)
            {
                if (delta < ShowBeforeTurnOver && Math.Abs(BookDummy.Builder.page - BookDummy.Builder.Flip) < 1)
                { // 翻页完毕，设置当前页
                    ChangeState(TurnState.EndTurnAnim);                    
                    EventDispatcher.Instance.DispatchEvent(new Event_EraChangeCurrentPage(logicTurntoPage));
                }
            }
        }

        private void TweenAlphaFinish()
        {
            if (turnState == TurnState.StartTurnAnim)
            {
                if (resetStartTexture)
                {
                    BookDummy.Builder.SetPage(startPage, true);
                    SetPageTexture((int)logicStartPage, startPage);
                    resetStartTexture = false;
                }

                ChangeState(TurnState.Turning);
            }
            else if (turnState == TurnState.EndTurnAnim)
            { // 翻页结束
                ChangeState(TurnState.Show);
            }
        }

        private void PlayStartTurnAnim()
        {
            alphaAnim.tweenFactor = 0;
            alphaAnim.SetOnFinished(TweenAlphaFinish);
            alphaAnim.PlayForward();
            ShowEffect(false);
        }

        private void PlayEndTurnAnim()
        {
            var page = BookDummy.Builder.GetCurrentPage();
            AttachPageObjects(page - 1, 1, showPageObjects.Left, false);
            AttachPageObjects(page, 0, showPageObjects.Right, false);

            alphaAnim.SetOnFinished(TweenAlphaFinish);
            alphaAnim.tweenFactor = 1;
            alphaAnim.PlayReverse();
            ShowEffect(true);
        }

        private void ChangeState(TurnState state)
        {
            turnState = state;
            switch (state)
            {
                case TurnState.Show:
                    {
                        
                        logicLastPage = (int)logicTurntoPage;
                        lastPage = BookDummy.Builder.GetCurrentPage();
                        turnToPage = -1;                      
                    }
                    break;
                case TurnState.StartTurnAnim:
                    {
                        if (logicLastPage < 0)
                        {
                            ChangeState(TurnState.Turning);//翻页中
                        }
                        else
                        {
                            PlayStartTurnAnim();
                        }
                    }
                    break;
                case TurnState.Turning:
                    {
                        ClearPage();

                        var page = (int)BookDummy.Builder.page;
                        BookDummy.Builder.SetPage(turnToPage, false);
                        var retList = EraManager.Instance.RefreshPageContent(logicTurntoPage, ref effectPathList);
                        if (retList != null)
                        {
                            var type = retList[0];
                            ChangeType((EraPageType)type);
                            //showModelId = retList[1];
                        }
                        else
                        {
                            showModelId = -1;
                        }

                        if (page == turnToPage)
                        {
                            Logger.Error("MegaBook TurnPage Same");
                            ChangeState(TurnState.Show);
                        }
                    }
                    break;
                case TurnState.EndTurnAnim:
                    {
                        Event_EraUpdateLineLightCallBack();
                        if (showPageObjects != null)
                        {
                           
                            PlayEndTurnAnim();
                        }
                        else
                        {
                            ChangeState(TurnState.Show);
                        }
                    }
                    break;
            }
        }
    }
}
