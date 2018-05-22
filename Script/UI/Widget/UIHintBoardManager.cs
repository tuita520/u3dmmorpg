#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataTable;
using EventSystem;
using Shared;
using UnityEngine;

#endregion

public class UIHintBoardManager : MonoBehaviour
{
    public GameObject HighRoot;
    public GameObject LowRoot;
    public GameObject AnchorRoseRoot;
    public GameObject ZhuBo_MeiGuiEffect;
//    public GameObject Test_ChiJiAnQuanQu;
//    public GameObject Test_ChiJi;
    
    private readonly Dictionary<int, HintInfoQueue> mHintInfoQueues = new Dictionary<int, HintInfoQueue>();
	public bool IsWorking
	{
		get;
		private set;
	}
    public enum eRootLayer
    {
        High = 0,
        Low = 1
    }

	public static UIHintBoardManager Instance { get; private set; }

    private void OnShowErrorTip(IEvent ievent)
    {
        var e = ievent as UIEvent_ErrorTip;
        var errorCode = e.ErrorCode;
        var dicId = 200000000 + (int) errorCode;
        var table = Table.GetDictionary(dicId);
        if (null != table)
        {
            ShowInfo(14, GameUtils.GetDictionaryText(dicId));
        }
        else
        {
            ShowInfo(14, GameUtils.GetDictionaryText(200000001));
        }
    }

    private void OnShowUIHint(IEvent ievent)
    {
        var e = ievent as ShowUIHintBoard;
        PushInfoQuene(e.TableId, e.Info, e.WaitSec);
        if (e.DicId == 210100)
        {
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ExchangeUI));
            var ee = new Show_UI_Event(UIConfig.WishingUI, new WishingArguments { Tab = 1 });
            EventDispatcher.Instance.DispatchEvent(ee);
        }
    }

    private void PushInfoQuene(int tableId, string info, int waitSec = -1)
    {
        HintInfoQueue infoQueue;
        var tbComText = Table.GetCombatText(tableId);
        if (tbComText == null)
        {
            return;
        }
        if (tbComText.QueueLimit == -1 && tbComText.IntervalTime == 0)
        {
            ShowInfo(tableId, info, -1, (eRootLayer) tbComText.TextLayer);
            return;
        }
        if (!mHintInfoQueues.TryGetValue(tbComText.Group, out infoQueue))
        {
            infoQueue = new HintInfoQueue();
            infoQueue.CdTime = tbComText.IntervalTime;
            infoQueue.CdTimer = Game.Instance.ServerTime.AddMilliseconds(-infoQueue.CdTime);
            mHintInfoQueues[tbComText.Group] = infoQueue;
        }
        if (tbComText.QueueLimit == -1 || infoQueue.Info.Count < tbComText.QueueLimit)
        {
            infoQueue.Info.Add(new HintParam
            {
                TableId = tableId,
                Text = info,
                WaitSec = waitSec,
                RootLayer = (eRootLayer) tbComText.TextLayer
            });
        }
    }

    private void ShowInfo(int tableId, string info, int waitSec = -1, eRootLayer rootLayer = eRootLayer.High)
    {
        ComplexObjectPool.NewObject("UI/DamageBoard.prefab", o =>
        {
			if (null == Instance)
			{
				ComplexObjectPool.Release(o);
				return;
			}
			if (false==IsWorking)
			{
				ComplexObjectPool.Release(o);
				return;
			}

            var oTransform = o.transform;
            if (rootLayer == eRootLayer.High)
            {
                oTransform.SetParentEX(HighRoot.transform);
            }
            else if (rootLayer == eRootLayer.Low)
            {
                oTransform.SetParentEX(LowRoot.transform);
            }
            if (!o.activeSelf)
            {
                o.SetActive(false);
                o.SetActive(true);
            }
            oTransform.localScale = Vector3.one;
            oTransform.localPosition = Vector3.zero;
            var logic = o.GetComponent<DamageBoardLogic>();
            logic.StartAction(Vector3.zero, tableId, info, DamageBoardLogic.BoardShowType.UI);
            if (waitSec >= 0)
            {
                logic.StayTime = waitSec*1000;
            }
        },null, null, false, false, false,tableId.ToString());
    }

	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

		Instance = this;
		IsWorking = false;
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    public void Start()
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

    private bool isStartPlay = false;

    private void Update()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        {
            // foreach(var infoQueue in mHintInfoQueues)
            var __enumerator1 = (mHintInfoQueues).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var infoQueue = __enumerator1.Current;
                {
                    if (infoQueue.Value.Info.Count == 0)
                    {
                        continue;
                    }
                    if (infoQueue.Value.CdTimer.AddMilliseconds(infoQueue.Value.CdTime) <= Game.Instance.ServerTime)
                    {
                        var info = infoQueue.Value.Info[0];
                        ShowInfo(info.TableId, info.Text, info.WaitSec, info.RootLayer);
                        infoQueue.Value.CdTimer = Game.Instance.ServerTime;
                        infoQueue.Value.Info.RemoveAt(0);
                    }
                }
            }

            if (RoseTimeList.Count == 0 || RoseEffectList.Count == 0) return;
            if (RoseTimeList.Count > 0)
            {
                if (Game.Instance.ServerTime >= RoseTimeList[0])
                {
                    PlayEffect();
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

	void OnDestroy()
	{
#if !UNITY_EDITOR
try
{
#endif

		Instance = null;
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	public void Init()
	{
		EventDispatcher.Instance.AddEventListener(ShowUIHintBoard.EVENT_TYPE, OnShowUIHint);
		EventDispatcher.Instance.AddEventListener(UIEvent_ErrorTip.EVENT_TYPE, OnShowErrorTip);
        EventDispatcher.Instance.AddEventListener(ChatRoseEffectChangeEvent.EVENT_TYPE, ChatRoseEffectChangeCallBack);
		IsWorking = true;
	}


    private List<GameObject> RoseEffectList = new List<GameObject>();
    private List<DateTime> RoseTimeList = new List<DateTime>();

    private int count = -1;

    private void ChatRoseEffectChangeCallBack(IEvent iEvent)
    {
        if (iEvent != null)
        {
            var v = iEvent as ChatRoseEffectChangeEvent;
            if (v.Count == 999)
            {                
                if (ZhuBo_MeiGuiEffect != null)
                {
                    count++;                    
                    DateTime tempTime = Game.Instance.ServerTime;
                    DateTime dt = tempTime.AddMilliseconds(4700 * count);
                    RoseTimeList.Add(dt);
                    RoseEffectList.Add(ZhuBo_MeiGuiEffect);
                }
            }
        }
    }


    private IEnumerator DestoryEffect(GameObject obj)
    {
        yield return new WaitForSeconds(5f);        
        Destroy(obj);
        if (RoseTimeList.Count == 0 || RoseEffectList.Count == 0)
        {
            count = -1;
        }
    }


    private void PlayEffect()
    {
        if (RoseTimeList.Count == 0 || RoseEffectList.Count == 0) return;
        
        GameObject obj = Instantiate(RoseEffectList[0]) as GameObject;
        if (AnchorRoseRoot != null)
        {
            obj.transform.parent = AnchorRoseRoot.transform;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
            RoseTimeList.RemoveAt(0);
            RoseEffectList.RemoveAt(0);
            StartCoroutine(DestoryEffect(obj));
        }        
    }


    //private void PlayRoseEffect()
    //{
    //    if (MeiGuiEffectQue.Count > 0)
    //    {
    //        GameObject obj = Instantiate(MeiGuiEffectQue[0]) as GameObject;
    //        if (AnchorRoseRoot != null)
    //        {
    //            obj.transform.parent = AnchorRoseRoot.transform;
    //            obj.transform.localScale = Vector3.one;
    //            obj.SetActive(true);
    //            Destroy(obj, 5);
    //            MeiGuiEffectQue.RemoveAt(0);
    //        }
    //    }
    //}


    public void Cleanup()
	{
		mHintInfoQueues.Clear();
		EventDispatcher.Instance.RemoveEventListener(ShowUIHintBoard.EVENT_TYPE, OnShowUIHint);
		EventDispatcher.Instance.RemoveEventListener(UIEvent_ErrorTip.EVENT_TYPE, OnShowErrorTip);
        EventDispatcher.Instance.RemoveEventListener(ChatRoseEffectChangeEvent.EVENT_TYPE, ChatRoseEffectChangeCallBack);
		var tf = HighRoot.transform;
		for (int i = 0; i < tf.childCount; i++)
		{
			GameObject.Destroy(tf.GetChild(i).gameObject);
		}
		tf = LowRoot.transform;
		for (int i = 0; i < tf.childCount; i++)
		{
			GameObject.Destroy(tf.GetChild(i).gameObject);
		}
		IsWorking = false;
	}

    public class HintParam
    {
        public eRootLayer RootLayer;
        public int TableId;
        public string Text;
        public int WaitSec;
    }

    public class HintInfoQueue
    {
        public DateTime CdTimer;
        public List<HintParam> Info = new List<HintParam>();
        public int CdTime { get; set; }
    }

    

}