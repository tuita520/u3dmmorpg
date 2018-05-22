#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClientDataModel;
using ScriptManager;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class WingFrame : MonoBehaviour
	{
	    public BindDataRoot Binding;
	    //public GameObject CacelAuto;
	    public Animation CritEffect;
	    public GameObject LightEffect;
	    //public List<WingLinkedNodeTransfer> LinkedNodelList;
	    private bool animationLock;
	    private WingLinkedNodeTransfer linkedNode;
	    public UIDragRotate ModelDrag;
	
	    private readonly List<Vector3> PosList = new List<Vector3>
	    {
	        new Vector3(615, 270, 00),
	        new Vector3(315, -100, 0),
	        new Vector3(-100, -200, 0),
	        new Vector3(185, 95, 0),
	        new Vector3(-230, 300, 0)
	    };
	    private Dictionary<int,Vector3> Wing2Scale = new Dictionary<int, Vector3>()
	    {
	        {10103,new Vector3(0.9f,1f,1f)},
            {10104,new Vector3(1.3f,1.1f,1f)},
            {10105,new Vector3(1.2f,1f,1f)},
            {10107,new Vector3(1.1f,1f,1f)},
            {10207,new Vector3(1.2f,1.2f,1.2f)},
	    }; 
	    //public List<UIButton> PartMenus;
	    public TweenPosition PartMoveProgress;
	    public TweenPosition PartMoveRight;
	    //public TweenScale PartScale;
	    public int RenderQueue = 3004;
	    //public GameObject Sky;
	    public TweenAlpha WholeAlpha;
	    public TweenPosition WholeMoveLeft;
	    public TweenPosition WholePostion;
	    public TweenScale WholeScale;
	    public AnimationModel WingModel;
	    public GameObject WingPart;
	    public GameObject WingWhole;
	    public UIScrollViewSimple ScrollView;
	    //public WingPartTrainningHelper helper;

        public List<Transform> LevelStar;
        public GameObject LevelStar_Effect;
        public float LevelStar_Effect_DuringTime = 0.4f;

        public List<Transform> ChengJiu;
        public GameObject ChengJiu_Effect;

        public List<GameObject> CritEffects;
        public List<int> CritSoundID = new List<int>(4) { 
            999,
            999,
            999,
            999
        };

        public GameObject Consumption_Effect;


        public UISprite Duan_Sprite;
        public UISprite Ji_Sprte;
        public UILabel Duan_0;
        public UILabel Duan_1;
        public UILabel Ji_0;
        public UILabel Ji_1;

        public List<UIFont> Fonts;

        int TrainID=-1;

        //private IEnumerator EndAnimation(float delay, GameObject obj)
        //{
        //    yield return new WaitForSeconds(delay);
        //    obj.SetActive(false);
        //}
	
        //private IEnumerator AfterLightBall(int index, Action act)
        //{
        //    yield return new WaitForSeconds(0.1f);
        //    linkedNode.SetBallActive(index, true);
        //    yield return new WaitForSeconds(0.3f);
        //    LightEffect.SetActive(false);
        //    animationLock = false;
        //    SetBallActive(index, true, true);
        //    act();
        //}

        //private void ResetBall(int star)
        //{
        //    for (var i = 0; i < starCount; i++)
        //    {
        //        linkedNode.SetBallActive(i, true);
        //    }
        //    for (var i = starCount; i < 10; i++)
        //    {
        //        linkedNode.SetBallActive(i, false);
        //    }
        //    if (starCount == 10)
        //    {
        //        starCount--;
        //    }
        //    linkedNode.MoveTo(starCount, false);
        //    for (var i = 0; i < star; i++)
        //    {
        //        SetBallActive(i, true, false);
        //    }
        //    for (var i = star; i < 10; i++)
        //    {
        //        SetBallActive(i, false, false);
        //    }

        //    if (null != helper)
        //    {
        //        helper.DoChangePartAnimation();
        //    }

        //}

        //private void SetBallActive(int index, bool active, bool doAnimation)
        //{
        //    if (null != helper)
        //    {
        //        helper.RefreshTrainningPart(index, active, doAnimation);
        //    }
        //    else
        //    {
        //        Logger.Info("can't find WingPartTrainningHelper");
        //    }
        //}

        void ResetEffect()
        {
            LightEffect.SetActive(false);
            LevelStar_Effect.SetActive(false);
            ChengJiu_Effect.SetActive(false);
            Consumption_Effect.SetActive(false);
        }
	    private void CreateModel(int id)
	    {
	        DestroyModel();
	        var tbEquip = Table.GetEquipBase(id);
	        if (tbEquip == null)
	        {
	            return;
	        }
	        var tbMont = Table.GetWeaponMount(tbEquip.EquipModel);
	        if (tbMont == null)
	        {
	            return;
	        }
	        WingModel.CreateModel(tbMont.Path, tbEquip.AnimPath + "/FlyIdle.anim", model =>
	        {
	            ModelDrag.Target = model.transform;
	            WingModel.PlayAnimation();
	            model.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
	            model.gameObject.SetRenderQueue(RenderQueue);
                //if (Wing2Scale.ContainsKey(id))
                //{
                //    var scale = Wing2Scale[id];
                //    model.gameObject.transform.localScale = scale;
                //}
//                model.gameObject.transform.localScale = new Vector3(2f,2f,2f);
	        });

            //grid.SetLookIndex(tbEquip.Ladder - 1 < 0 ? 0 : tbEquip.Ladder - 1);
	    }
	
	    //-----------------------------------------------Model-----
	    private void DestroyModel()
	    {
	        if (WingModel)
	        {
	            WingModel.DestroyModel();
	        }
	    }
	
        //private WingLinkedNodeTransfer GetNodeTransfer(int index, bool setShow = true)
        //{
        //    var count = LinkedNodelList.Count;
        //    if (index < 0 || index >= count)
        //    {
        //        return null;
        //    }
        //    var node = LinkedNodelList[index];

        //    for (var i = 0; i < count; i++)
        //    {
        //        if (index == i)
        //        {
        //            LinkedNodelList[i].gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            LinkedNodelList[i].gameObject.SetActive(false);
        //        }
        //    }
        //    return node;
        //    return null;
        //}
	
	    public void OnClickAdvanced()
	    {
	        var e = new WingOperateEvent(1, 0);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickAdvancedAuto()
	    {
	        var e = new WingOperateEvent(1, 1);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickClose()
	    {
	        //PlayerDataManager.Instance.WeakNoticeData.WingTraining = false;
	        var e = new WingOperateEvent(-2, 0);
	        EventDispatcher.Instance.DispatchEvent(e);
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.WingUI));
	    }


	    private void OnClickPart(int i)
	    {
	        animationLock = false;
	        var e = new WingOperateEvent(0, i);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickPartBack()
	    {
	        animationLock = false;
	        WholeScale.onFinished.Clear();
            //PartScale.onFinished.Clear();
            //PartScale.PlayReverse();
            //PartScale.onFinished.Add(new EventDelegate(PartToWholeTweenFinish));
	        PartMoveRight.PlayReverse();
	        PartMoveProgress.PlayReverse();
	        var e = new WingOperateEvent(-2, 0);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickPartTrain()
	    {
	        if (animationLock)
	        {
	            return;
	        }
	        var e = new WingOperateEvent(-1, 0);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickPartTrainAuto()
	    {
            //if (animationLock && CacelAuto.activeSelf == false)
            //{
            //    return;
            //}
            if (animationLock)
            {
                return;
            }
	        animationLock = false;
	        var e = new WingOperateEvent(-1, 1);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickPartTrainIfAuto()
	    {
	        animationLock = false;
	        var e = new WingOperateEvent(-1, 2);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickTabMenuAdvanced()
	    {
	        var e = new WingOperateEvent(-2, 0);
	        EventDispatcher.Instance.DispatchEvent(e);
	        WingModel.PlayAnimation();
	    }

        private int lastClickIndex = -1;

	    public void OnClickTabMenuTrain()
	    {
	        //PlayerDataManager.Instance.WeakNoticeData.WingTraining = false;
	        var e = new WingOperateEvent(-2, 1);
	        EventDispatcher.Instance.DispatchEvent(e);
            //if (lastClickIndex == -1)
	        {
                lastClickIndex = 0;
                ResetEffect();
                OnClickPart(0);
	        }
	    }
	
	    private void OnDestroy()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
	        EventDispatcher.Instance.RemoveEventListener(WingRefreshStarCount.EVENT_TYPE, OnRefreshWingStarCount);
	        EventDispatcher.Instance.RemoveEventListener(WingRefreshTrainCount.EVENT_TYPE, OnRefreshWingTrainCount);
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

            var stc = GetComponentInChildren<SelectToggleControl>();
            UIToggle[] toggle = stc.gameObject.GetComponentsInChildren<UIToggle>();
            for (int i = 0; i < toggle.Length; i++)
            {
                stc.OperateMenus[i] = toggle[i];
            }
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
	        //Sky.SetActive(false);
	
// 	        if (UICamera.mainCamera)
// 	        {
// 	            UICamera.mainCamera.nearClipPlane = -10f;
// 	        }
	        Binding.RemoveBinding();
	        DestroyModel();
            EventDispatcher.Instance.RemoveEventListener(WingNotifyLogicEvent.EVENT_TYPE, OnNotifyWingLogicEvent);
            EventDispatcher.Instance.RemoveEventListener(WingModelRefreh.EVENT_TYPE, OnRefreshWingModel);
            //EventDispatcher.Instance.RemoveEventListener(WingRefreshStarPage.EVENT_TYPE, OnRefreshWingStarPage);

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

            ResetEffect();
	        //CritEffect.gameObject.SetActive(false);
	        //OnPartBack();
	        animationLock = false;
            EventDispatcher.Instance.AddEventListener(WingNotifyLogicEvent.EVENT_TYPE, OnNotifyWingLogicEvent);
	        EventDispatcher.Instance.AddEventListener(WingModelRefreh.EVENT_TYPE, OnRefreshWingModel);
            //EventDispatcher.Instance.AddEventListener(WingRefreshStarPage.EVENT_TYPE, OnRefreshWingStarPage);

	        var controllerBase = UIManager.Instance.GetController(UIConfig.WingUI);
	        if (controllerBase == null)
	        {
	            return;
	        }
	        var dataModel = controllerBase.GetDataModel("") as WingDataModel;
            Binding.SetBindDataSource(dataModel);
	        Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel.Bags.Resources);
	        Binding.SetBindDataSource(PlayerDataManager.Instance.WeakNoticeData);
	        Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);

// 	        var xform = Sky.transform;
// 	        Sky.SetActive(true);
// 	        xform.parent = UICamera.mainCamera.transform;
// 	        xform.localPosition = Vector3.zero;
// 	        xform.localScale = Vector3.one*3000;
// 	        Sky.SetRenderQueue(3005);
// 	
// 	        UICamera.mainCamera.nearClipPlane = -1f;
	        if (dataModel != null)
	        {
	            var id = dataModel.QualityId;
	            var tbEquip = Table.GetEquipBase(id);
                if (null != tbEquip && null != ScrollView)
	            {
	               // grid.SetLookIndex(tbEquip.Ladder < 0 ? 0 : tbEquip.Ladder);
                    if (tbEquip.Ladder > 5)
	                {
                        StartCoroutine(DelayMoveScroll(tbEquip.Ladder));
	                }
	            }
	        }

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
	#endif
	    }


	    IEnumerator DelayMoveScroll(int index)
	    {
	        yield return new WaitForSeconds(0.1f);
            if (ScrollView.isActiveAndEnabled)
                ScrollView.SetLookIndex(index -1, true);
	    }
	
	    public void OnPartBack()
	    {
// 	        animationLock = false;
// 	        WingWhole.SetActive(true);
// 	        WingPart.SetActive(false);
// 	
// 	        WholePostion.ResetForPlay();
// 	        WholePostion.enabled = false;
// 	        WholeScale.ResetForPlay();
// 	        WholeScale.enabled = false;
// 	        WholeAlpha.ResetForPlay();
// 	        WholeAlpha.enabled = false;
// 	
// 	        WholeMoveLeft.ResetForPlay();
// 	        WholeMoveLeft.enabled = false;
// 	
// 	        {
// 	            var __list5 = PartMenus;
// 	            var __listCount5 = __list5.Count;
// 	            for (var __i5 = 0; __i5 < __listCount5; ++__i5)
// 	            {
// 	                var button = __list5[__i5];
// 	                {
// 	                    if (button.collider.enabled == false)
// 	                    {
// 	                        button.ResetDefaultColor();
// 	
// 	                        button.collider.enabled = true;
// 	                        button.SetState(UIButtonColor.State.Normal, true);
// 	                    }
// 	                }
// 	            }
// 	        }
	        var e = new WingOperateEvent(-2, -1);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnPartTrainShowOver()
	    {
	        animationLock = false;
	    }
	
	    private void OnRefreshWingModel(IEvent ievent)
	    {
	        var e = ievent as WingModelRefreh;
	        CreateModel(e.TableId);
	    }
	
	    private void OnNotifyWingLogicEvent(IEvent ievent)
	    {
	        var e = ievent as WingNotifyLogicEvent;
	        switch (e.Type)
	        {
                case 0://没暴击直接播下音效
                    {
                        SoundManager.Instance.PlaySoundEffect(CritSoundID[0]);
                    }
                    break;
	            case 1:
	            {
                    //CritEffect.gameObject.SetActive(true);
                    //CritEffect[CritEffect.clip.name].time = 0;
                    //CritEffect.Sample();
                    //CritEffect.Play(PlayMode.StopAll);
                    //if (gameObject.activeSelf)
                    //{
                    //    StartCoroutine(EndAnimation(0.6f, CritEffect.gameObject));
                    //}
                    FlashCrit(e.Ret-1);
	            }
	            break;
                case 2://培养消耗
                {
                    FlashConsumption();
                }
                break;
                case 4://培养变化，e.Ret新的ID；
                {
                    ChangeTrain(e.Ret);
                }
                break;
	        }
	    }
	
	    private void OnRefreshWingStarCount(IEvent ievent)
	    {
	        var e = ievent as WingRefreshStarCount;
	        var star = e.Star;
            FlashStar(star, () => {
                OnClickPartTrainIfAuto();
            });
           
            
            //animationLock = true;
            //SetLightBall(star - 1, () =>
            //{
            //    //linkedNode.MoveTo(star, true, OnClickPartTrainIfAuto);
            //    OnClickPartTrainIfAuto();

            //});
	    }
	
        //private void OnRefreshWingStarPage(IEvent ievent)
        //{
        //    var e = ievent as WingRefreshStarPage;
        //    {
        //        var __list3 = PartMenus;
        //        var __listCount3 = __list3.Count;
        //        for (var __i3 = 0; __i3 < __listCount3; ++__i3)
        //        {
        //            var button = __list3[__i3];
        //            {
        //                button.collider.enabled = false;
        //            }
        //        }
        //    }
        //    PartScale.onFinished.Clear();
        //    var index = e.Part;
        //    var star = e.Star;
        //    var showBeing = e.ShowBegin;
        //    WholePostion.ResetForPlay();
        //    WholePostion.to = PosList[index];
        //    WholePostion.PlayForward();
        //    WholeAlpha.ResetForPlay();
        //    WholeAlpha.PlayForward();
        //    WholeMoveLeft.ResetForPlay();
        //    WholeMoveLeft.PlayForward();
        //    WholeScale.enabled = false;
        //    WholeScale.onFinished.Clear();
        //    WholeScale.ResetForPlay();
        //    WholeScale.PlayForward();
        //    var layer = e.Layer;
        //    linkedNode = GetNodeTransfer(layer);
        //    WholeScale.SetOnFinished(new EventDelegate(() => { WholeToPartTweenFinish(star, showBeing); }));
        //    var e = ievent as WingRefreshStarPage;
        //    var star = e.Star;
        //    ResetBall(star);
        //}
	
	    private void OnRefreshWingTrainCount(IEvent ievent)
	    {
            var e = ievent as WingRefreshTrainCount;
// 	//翅膀升阶
// 	        var e = ievent as WingRefreshTrainCount;
// 	        var trainCount = e.TrainCount;
// 	
// 	        SetLightBall(9, () =>
// 	        {
//  	            animationLock = true;
// // 	            linkedNode.ZoomOut(() =>
// // 	            {
// // 	                linkedNode = GetNodeTransfer(trainCount);
// // 	                linkedNode.ZoomIn(OnClickPartTrainIfAuto);
// // 	            });
// 	            OnClickPartTrainIfAuto();
// 	        });
            int mod = e.TrainCount % 3;
            if (mod == 0)
            {
                FlashCount((e.TrainCount/3)-1);
            }
            
            FlashStar(0, () => {
                OnClickPartTrainIfAuto();
            });
            //OnClickPartTrainIfAuto();
	    }


        
        //public void PartShowFinish()
        //{
        //    PartScale.onFinished.Clear();
        //    linkedNode.ZoomIn(OnPartTrainShowOver);
        //}
	
        //public void PartToWholeTweenFinish()
        //{
        //    WingPart.SetActive(false);
        //    WholeScale.onFinished.Clear();
        //    WholeScale.PlayReverse();
        //    WholePostion.PlayReverse();
        //    WholeAlpha.PlayReverse();
        //    WholeScale.PlayReverse();
        //    WholeMoveLeft.PlayReverse();
        //    //{
        //    //    var __list5 = PartMenus;
        //    //    var __listCount5 = __list5.Count;
        //    //    for (var __i5 = 0; __i5 < __listCount5; ++__i5)
        //    //    {
        //    //        var button = __list5[__i5];
        //    //        {
        //    //            button.collider.enabled = true;
        //    //        }
        //    //    }
        //    //}
        //}
	
        //private void SetLightBall(int index, Action act)
        //{
        //    LightEffect.SetActive(true);
        //    if (gameObject.activeSelf)
        //    {
        //        StartCoroutine(AfterLightBall(index, act));
        //    }
        //}
        WaitForSeconds flashStarAction_wfs = new WaitForSeconds(0.125f);
        Coroutine flashStarAction_coroutine = null;

        WaitForSeconds flashCrit_wfs = new WaitForSeconds(0.8f);
        Coroutine flashhCritrAction_coroutine = null;

        WaitForSeconds flashConsumption_wfs = new WaitForSeconds(0.8f);
        Coroutine flashConsumptionAction_coroutine = null;
        void FlashStar(int index,Action act=null)
        {
            if (index < 0 || index >= LevelStar.Count) return;
            LevelStar_Effect.SetActive(false);
            if (!gameObject.activeSelf) return;

            LevelStar_Effect.transform.parent = LevelStar[index];
            LevelStar_Effect.transform.localPosition = Vector3.zero;
            LevelStar_Effect.SetActive(true);

            if (act != null)
            {
                if (flashStarAction_coroutine != null)
                {
                    StopCoroutine(flashStarAction_coroutine);
                    flashStarAction_coroutine = null;
                }
                if(gameObject.activeSelf)
                    flashStarAction_coroutine=StartCoroutine(FlashStarDoAction(act));
            }

        }
        void FlashCount(int index)
        {
            if (index < 0 || index >= ChengJiu.Count) return;
            ChengJiu_Effect.SetActive(false);
            ChengJiu_Effect.transform.parent = ChengJiu[index];
            ChengJiu_Effect.transform.localPosition = Vector3.zero;
            ChengJiu_Effect.SetActive(true);
            
        }
        IEnumerator FlashStarDoAction(Action act)
        {
            yield return  flashStarAction_wfs;
            act();
            yield return new WaitForSeconds(LevelStar_Effect_DuringTime);
            LevelStar_Effect.SetActive(false);
        }


        void FlashCrit(int index, Action act = null)
        {
            if (index < 0 || index > CritEffects.Count) return;
            if (flashhCritrAction_coroutine != null)
            {
                StopCoroutine(flashhCritrAction_coroutine);
                flashhCritrAction_coroutine = null;
                //return;
            }
            for (int i = 0; i < CritEffects.Count; i++)
            {
                if (CritEffects[i].activeSelf)
                {
                    //TweenScale[] tss = CritEffects[i].GetComponentsInChildren<TweenScale>();
                    //for(int j=0;j<tss.Length;j++){
                    //    //tss[j].PlayReverse();
                    //    tss[j].ResetForPlay();
                    //}

                    //TweenPosition[] tsp = CritEffects[i].GetComponentsInChildren<TweenPosition>();
                    //for (int j = 0; j < tsp.Length; j++)
                    //{
                    //    //tss[j].PlayReverse();
                    //    tss[j].ResetForPlay();
                    //}
                    //TweenAlpha[] tsa = CritEffects[i].GetComponentsInChildren<TweenAlpha>();
                    //for (int j = 0; j < tsa.Length; j++)
                    //{
                    //    //tsa[j].PlayReverse();
                    //    tsa[j].ResetForPlay();
                    //}
                    //TweenColor[] tsc = CritEffects[i].GetComponentsInChildren<TweenColor>();
                    //for (int j = 0; j < tsc.Length; j++)
                    //{
                    //    //tsc[j].PlayReverse();
                    //    tsc[j].ResetForPlay();
                    //}
                    CritEffects[i].SetActive(false);
                }
            }

            if (gameObject.activeSelf)
            {

                SoundManager.Instance.PlaySoundEffect(CritSoundID[index+1]);
                flashhCritrAction_coroutine = StartCoroutine(FlashCritAction(CritEffects[index]));
            }
             

        }
        IEnumerator FlashCritAction(GameObject critObj)
        {
            critObj.SetActive(true);
            TweenScale[] tsss = critObj.GetComponentsInChildren<TweenScale>();

            for (int j = 0; j < tsss.Length; j++)
            {
                tsss[j].ResetToBeginning();
                tsss[j].enabled = true;
                tsss[j].Play(true);
            }
            TweenPosition[] tspp = critObj.GetComponentsInChildren<TweenPosition>();
            for (int j = 0; j < tspp.Length; j++)
            {
                tsss[j].ResetToBeginning();
                tsss[j].enabled = true;
                tsss[j].Play(true);
            }

            TweenAlpha[] tsaa = critObj.GetComponentsInChildren<TweenAlpha>();
            for (int j = 0; j < tsaa.Length; j++)
            {
                tsaa[j].ResetToBeginning();
                tsaa[j].enabled = true;
                tsaa[j].Play(true);
            }
            TweenColor[] tscc = critObj.GetComponentsInChildren<TweenColor>();
            for (int j = 0; j < tscc.Length; j++)
            {
                tscc[j].ResetToBeginning();
                tscc[j].enabled = true;
                tscc[j].Play(true);
            }
            yield return flashCrit_wfs;
            if (critObj.activeSelf)
            {
                //TweenScale[] tss = critObj.GetComponentsInChildren<TweenScale>();
                //for (int j = 0; j < tss.Length; j++)
                //{
                //    //tss[j].PlayReverse();
                //    tss[j].ResetForPlay();
                //}

                //TweenPosition[] tsp = critObj.GetComponentsInChildren<TweenPosition>();
                //for (int j = 0; j < tss.Length; j++)
                //{
                //    //tss[j].PlayReverse();
                //    tss[j].ResetForPlay();
                //}
                //TweenAlpha[] tsa = critObj.GetComponentsInChildren<TweenAlpha>();
                //for (int j = 0; j < tss.Length; j++)
                //{
                //    //tsa[j].PlayReverse();
                //    tsa[j].ResetForPlay();
                //}
                //TweenColor[] tsc = critObj.GetComponentsInChildren<TweenColor>();
                //for (int j = 0; j < tsc.Length; j++)
                //{
                //    //tsc[j].PlayReverse();
                //    tsc[j].ResetForPlay();
                //}
                critObj.SetActive(false);
            }
            flashhCritrAction_coroutine = null;
        }


        void FlashConsumption()
        {
            Consumption_Effect.SetActive(false);
            if (flashConsumptionAction_coroutine != null)
            {
                StopCoroutine(flashConsumptionAction_coroutine);
                flashConsumptionAction_coroutine = null;
            }
            if (gameObject.activeSelf)
            {
                Consumption_Effect.SetActive(true);
                flashConsumptionAction_coroutine = StartCoroutine(FlashConsumptionAction());
            }
        }
        IEnumerator FlashConsumptionAction()
        {
            yield return flashConsumption_wfs;
            Consumption_Effect.SetActive(false);
        }

        void ResetTrain(int id)
        {
            //Debug.Log("Arthur------------------>ResetTrain,ID=" + id);
           
            var table = Table.GetWingTrain(id);
            int i_duan= Mathf.Max(table.TrainCount / 3, 1);
            Duan_Sprite.spriteName = "duan_" + i_duan.ToString();
            Ji_Sprte.spriteName = "ji_" + i_duan.ToString();

            Duan_0.bitmapFont = Fonts[i_duan - 1];
            Duan_1.bitmapFont = Fonts[i_duan - 1];

            Duan_0.text = table.TrainCount.ToString();
            Duan_1.text = table.TrainCount.ToString();

            Ji_0.bitmapFont = Fonts[i_duan - 1];
            Ji_1.bitmapFont = Fonts[i_duan - 1];

            Ji_0.text = table.TrainStar.ToString();
            Ji_1.text = table.TrainStar.ToString();

            TweenPosition tween_p = Duan_0.gameObject.GetComponent<TweenPosition>();
            TweenColor tween_c = Duan_0.gameObject.GetComponent<TweenColor>();
            tween_p.enabled = false;
            tween_c.enabled = false;

            Duan_0.transform.localPosition = tween_p.to;
            Duan_0.color = tween_c.to;

            tween_p = Duan_1.gameObject.GetComponent<TweenPosition>();
            tween_c = Duan_1.gameObject.GetComponent<TweenColor>();

            tween_p.enabled = false;
            tween_c.enabled = false;

            Duan_1.transform.localPosition = tween_p.to;
            Duan_1.color = tween_c.to;


            tween_p = Ji_0.gameObject.GetComponent<TweenPosition>();
            tween_c = Ji_0.gameObject.GetComponent<TweenColor>();

            tween_p.enabled = false;
            tween_c.enabled = false;

            Ji_0.transform.localPosition = tween_p.to;
            Ji_0.color = tween_c.to;


            tween_p = Ji_1.gameObject.GetComponent<TweenPosition>();
            tween_c = Ji_1.gameObject.GetComponent<TweenColor>();
            tween_p.enabled = false;
            tween_c.enabled = false;

            Ji_1.transform.localPosition = tween_p.to;
            Ji_1.color = tween_c.to;

        }
        void ChangeTrain(int id)
        {
            if (TrainID == -1)
            {
                TrainID = id;
                ResetTrain(id);
            }
            else if (TrainID != id)
            {
                var table = Table.GetWingTrain(TrainID);
                var table_new = Table.GetWingTrain(id);
                TrainID = id;

                int i_duan = Mathf.Max(table.TrainCount / 3, 1);
                int i_duan_new = Mathf.Max(table_new.TrainCount / 3, 1);

                if (i_duan != i_duan_new)
                {
                    Duan_Sprite.spriteName = "duan_" + i_duan_new.ToString();
                    Ji_Sprte.spriteName = "ji_" + i_duan_new.ToString();

                    Duan_0.bitmapFont = Fonts[i_duan_new - 1];
                    Duan_1.bitmapFont = Fonts[i_duan_new - 1];

                    Ji_0.bitmapFont = Fonts[i_duan_new - 1];
                    Ji_1.bitmapFont = Fonts[i_duan_new - 1];
                }

                if (table.TrainCount != table_new.TrainCount)
                {
                    FlashTrainCountAction(table_new.TrainCount);
                    FlashTrainStarAction(table_new.TrainStar);

                }
                else if (table.TrainStar != table_new.TrainStar)
                {
                    FlashTrainStarAction(table_new.TrainStar);
                }
            }
           
            //Debug.Log("Arthur------------------>ChangeTrain,Count=" + table.TrainCount + ",Star=" + table.TrainStar);
        }

        void FlashTrainCountAction(int newCount)
        {
            Duan_1.text = Duan_0.text;
            Duan_0.text = newCount.ToString();

            TweenPosition tween_p0 = Duan_0.gameObject.GetComponent<TweenPosition>();
            TweenColor tween_c0 = Duan_0.gameObject.GetComponent<TweenColor>();

            tween_p0.ResetToBeginning();
            tween_p0.enabled = true;
            tween_p0.Play(true);

            tween_c0.ResetToBeginning();
            tween_c0.enabled = true;
            tween_c0.Play(true);

            TweenPosition tween_p1 = Duan_1.gameObject.GetComponent<TweenPosition>();
            TweenColor tween_c1 = Duan_1.gameObject.GetComponent<TweenColor>();

            tween_p1.ResetToBeginning();
            tween_p1.enabled = true;
            tween_p1.Play(true);

            tween_c1.ResetToBeginning();
            tween_c1.enabled = true;
            tween_c1.Play(true);
        }
        void FlashTrainStarAction(int newStar)
        {
            Ji_1.text = Ji_0.text;
            Ji_0.text = newStar.ToString();

            TweenPosition tween_p0 = Ji_0.gameObject.GetComponent<TweenPosition>();
            TweenColor tween_c0 = Ji_0.gameObject.GetComponent<TweenColor>();

            tween_p0.ResetToBeginning();
            tween_p0.enabled = true;
            tween_p0.Play(true);

            tween_c0.ResetToBeginning();
            tween_c0.enabled = true;
            tween_c0.Play(true);

            TweenPosition tween_p1 = Ji_1.gameObject.GetComponent<TweenPosition>();
            TweenColor tween_c1 = Ji_1.gameObject.GetComponent<TweenColor>();

            tween_p1.ResetToBeginning();
            tween_p1.enabled = true;
            tween_p1.Play(true);

            tween_c1.ResetToBeginning();
            tween_c1.enabled = true;
            tween_c1.Play(true);
        }
	    private void Start()
	    {
	#if !UNITY_EDITOR
	        try
	        {
	#endif
            //helper = WingPart.GetComponentInChildren<WingPartTrainningHelper>();

	        animationLock = false;
            //var index = 0;
            //{
            //    var __list1 = PartMenus;
            //    var __listCount1 = __list1.Count;
            //    for (var __i1 = 0; __i1 < __listCount1; ++__i1)
            //    {
            //        var button = __list1[__i1];
            //        {
            //            var i = index;
            //            button.onClick.Add(new EventDelegate(() => { OnClickPart(i); }));
            //            index++;
            //        }
            //    }
            //}
            //{
            //    var __list2 = LinkedNodelList;
            //    var __listCount2 = __list2.Count;
            //    for (var __i2 = 0; __i2 < __listCount2; ++__i2)
            //    {
            //        var nodeTransfer = __list2[__i2];
            //        {
            //            nodeTransfer.gameObject.SetActive(false);
            //        }
            //    }
            //}
	        OnPartBack();
	        EventDispatcher.Instance.AddEventListener(WingRefreshStarCount.EVENT_TYPE, OnRefreshWingStarCount);
	        EventDispatcher.Instance.AddEventListener(WingRefreshTrainCount.EVENT_TYPE, OnRefreshWingTrainCount);
	        
	#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
	#endif
	    }
	
	    private void Update()
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
	
	    public void WholeToPartTweenFinish(int starCount, bool showBegin)
	    {
// 	        WholeScale.onFinished.Clear();
// 	        WingPart.SetActive(true);
// 	        PartScale.onFinished.Clear();
// 	        PartScale.ResetForPlay();
// 	        PartScale.PlayForward();
// 	        linkedNode.Reset();
// 	        if (showBegin == false)
// 	        {
// 	            for (var i = 0; i < starCount; i++)
// 	            {
// 	                linkedNode.SetBallActive(i, true);
// 	            }
// 	            for (var i = starCount; i < 10; i++)
// 	            {
// 	                linkedNode.SetBallActive(i, false);
// 	            }
// 	            if (starCount == 10)
// 	            {
// 	                starCount--;
// 	            }
// 	            linkedNode.MoveTo(starCount, false);
// 	        }
// 	        else
// 	        {
// 	            animationLock = true;
// 	            PartScale.SetOnFinished(new EventDelegate(PartShowFinish));
// 	        }
// 	        {
// 	            var __list4 = PartMenus;
// 	            var __listCount4 = __list4.Count;
// 	            for (var __i4 = 0; __i4 < __listCount4; ++__i4)
// 	            {
// 	                var button = __list4[__i4];
// 	                {
// 	                    button.collider.enabled = true;
// 	                }
// 	            }
// 	        }
// 	        PartMoveRight.ResetForPlay();
// 	        PartMoveRight.PlayForward();
// 	        PartMoveProgress.ResetForPlay();
// 	        PartMoveProgress.PlayForward();
// 	        WholeScale.onFinished.Clear();
	    }
	}
}