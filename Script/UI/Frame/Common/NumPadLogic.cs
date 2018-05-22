#region using

using System;
using System.Collections;
using EventSystem;
using GameUI;
using UnityEngine;
using ClientDataModel;
using DataTable;

#endregion

namespace GameUI
{
	public class NumPadLogic : MonoBehaviour
	{
	    /// <summary>
	    ///     通过关闭按钮返回,result 回调中返回-1
	    /// </summary>
	    /// <param name="minValue"></param>
	    /// <param name="MaxValue"></param>
	    /// <param name="result"></param>
        public static void ShowNumberPad(int minValue, int MaxValue, Action<int> result, int UINum=0)
	    {
	        var uiroot = GameUtils.GetUiRoot();
	        if (null == uiroot)
	        {
	            return;
	        }
	        var res = ResourceManager.PrepareResourceSync<GameObject>(prefab);
	        Instance = Instantiate(res) as GameObject;
	        if (Instance != null)
	        {
	            var logic = Instance.GetComponent<NumPadLogic>();
	            logic.MinValue = minValue;
	            logic.MaxValue = MaxValue;
                logic.UINum = UINum;
	            var t = Instance.transform;
	            //t.parent = uiroot.transform;
	            t.SetParentEX(uiroot.transform);
	            t.localScale = Vector3.one;
                t.localPosition = Vector3.zero;
	            var collider = Instance.AddComponent<BoxCollider>();
	            collider.isTrigger = true;
	            collider.size = new Vector3(1136, 1136, 0);
	            Instance.SetActive(true);
	        }
	        callBack = result;
	    }
	
	    #region 私有
	
	    // Use this for initialization
	    public int MinValue { get; set; }
	    public int MaxValue { get; set; }
	
	    private int mValue;
	    private UIButton mEnterButton;
        public int UINum { get; set; }//对应不同UI，使用不同的Datamodel
	    public int outPutValue
	    {
	        get { return mValue; }
	        set
	        {
	            if (mValue != value)
	            {
	                mValue = value;
	                if (null != OutPutLabel)
	                {
	                    OutPutLabel.text = mValue.ToString();
	                }
	            }
	        }
	    }
	
	    public static GameObject Instance;
	    private const string prefab = "UI/Common/NumberPad.prefab";
	    private static Action<int> callBack;
	    public UILabel OutPutLabel;
	    public UILabel TipLabel;
        public bool isOne = false;
	
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        var scripts = gameObject.GetComponentsInChildren<BtnValue>();
	
	        var count = scripts.Length;
	        for (var i = 0; i < count; i++)
	        {
	            var script = scripts[i];
	            var label = script.gameObject.GetComponentInChildren<UILabel>();
	            var btn = script.gameObject.GetComponentInChildren<UIButton>();
	            btn.onClick.Add(new EventDelegate(script.NumberClick));
	
	            if (script.Value == 10)
	            {
	                label.text = "Del";
	            }
	            else if (script.Value == 11)
	            {
	                label.text = "Enter";
	                mEnterButton = btn;
	            }
	            else
	            {
	                label.text = script.Value.ToString();
	            }
	        }
	        InputNumber(new UIEvent_NumberPad_Click(0));
            TipLabel.text = string.Format("输入数量(最大{0})", MaxValue);
	
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
	        EventDispatcher.Instance.AddEventListener(UIEvent_NumberPad_Click.EVENT_TYPE, InputNumber);
            MinValue = 1;
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
	        EventDispatcher.Instance.RemoveEventListener(UIEvent_NumberPad_Click.EVENT_TYPE, InputNumber);
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void InputNumber(IEvent ievent)
	    {
	        var e = ievent as UIEvent_NumberPad_Click;
	        var input = e.keyValue;
	
	        if (input == 10)
	        {
	            outPutValue = outPutValue/10;
	        }
	        else if (input == 11)
	        {
	            if (callBack != null)
	            {
	                try
	                {
	                    callBack(outPutValue);
	                }
	                catch (Exception exp)
	                {
	                    Logger.Error("NumberPad callback error:{0}", exp);
	                }
	            }
	            NetManager.Instance.StartCoroutine(DestoryNextFrame());
	            return;
	        }
	        else
	        {
                if (outPutValue == 0)
                {
                    outPutValue = input;
                }
                else
                {
	                outPutValue = outPutValue*10 + input;
	            }
	        }
	
	
	        if (outPutValue < MinValue)
	        {
	            OutPutLabel.color = Color.red;
	            mEnterButton.isEnabled = false;
	        }
	        else
	        {
	            OutPutLabel.color = Color.white;
	            mEnterButton.isEnabled = true;
	        }
	
	        if (outPutValue > MaxValue)
	        {
	            outPutValue = MaxValue;
	        }
	
	        OutPutLabel.text = outPutValue.ToString();


            if (UINum == 1)
            {
                var controller = UIManager.Instance.GetController(UIConfig.QuickBuyUi);
                if (null != controller)
                {
                    var datamodel = controller.GetDataModel("") as QuickBuyDataModel;
                    if (null != datamodel)
                    {
                        datamodel.OneBuy.BuyCount = outPutValue;
                        if (datamodel.OneBuy.BuyCount == 0)
                        {
                            datamodel.OneBuy.BuyCount = 1;
                        }
                        datamodel.OneBuy.Item.Count = datamodel.OneBuy.BuyCount;
                        var _item = Table.GetItemBase(datamodel.OneBuy.Item.ItemId);
                        if (_item == null || _item.StoreID == -1)
                        {
                            return;
                        }

                        var _tbStore = Table.GetStore(_item.StoreID);
                        if (_tbStore != null)
                        {
                            datamodel.OriginalPrice = datamodel.OneBuy.Item.Count * _tbStore.Old;
                            datamodel.DiscountPrice = datamodel.OneBuy.Item.Count * _tbStore.NeedValue;
                        }
                    }
                }
            }
            else if (UINum == 2)
            {
                var storeCtrl = UIManager.Instance.GetController(UIConfig.StoreUI);
                if (null != storeCtrl)
                {
                    var datamodel = storeCtrl.GetDataModel("") as StoreDataModel;
                    if (null != datamodel)
                    {
                        datamodel.SelectCount = outPutValue;
                        if (datamodel.SelectCount == 0)
                        {
                            datamodel.SelectCount = 1;
                        }
                        //datamodel.SelectCount = outPutValue;
                    }
                }
            }
            else if (UINum == 3)
            {
                var teamCtrl = UIManager.Instance.GetController(UIConfig.TeamFrame);
                if (null != teamCtrl)
                {
                    var datamodel = teamCtrl.GetDataModel("TeamTargetChange") as TeamTargetChangeDataModel;
                    if (null != datamodel)
                    {
                        //var oldnini = datamodel.CurrentItemData.levelMini;
                        if (outPutValue < datamodel.CurrentItemData.levelMax)
                        {
                            datamodel.CurrentItemData.levelMini = outPutValue;
                            if (datamodel.CurrentItemData.levelMini <= 0)
                            {
                                datamodel.CurrentItemData.levelMini = 1;
                            }
                        }
                        //else
                        //{
                        //    datamodel.CurrentItemData.levelMini = oldnini;
                        //}
                    }
                }
            }
            else if (UINum == 4)
            {
                var teamCtrl = UIManager.Instance.GetController(UIConfig.TeamFrame);
                if (null != teamCtrl)
                {
                    var datamodel = teamCtrl.GetDataModel("TeamTargetChange") as TeamTargetChangeDataModel;
                    if (null != datamodel)
                    {
                        //var oldmax = datamodel.CurrentItemData.levelMini;
                        if (datamodel.CurrentItemData.levelMini < outPutValue)
                        {
                            datamodel.CurrentItemData.levelMax = outPutValue;
                            if (datamodel.CurrentItemData.levelMax >= 400)
                            {
                                datamodel.CurrentItemData.levelMax = 400;
                            }
                        }
                        //else
                        //{
                        //    datamodel.CurrentItemData.levelMini = oldmax;
                        //}
                    }
                }
            }              
	    }    

	    private IEnumerator DestoryNextFrame()
	    {
	        yield return new WaitForEndOfFrame();
	        Destroy(Instance);
	        Instance = null;
	    }
	
	    public void OnExitClick()
	    {
	        if (callBack != null)
	        {
	            try
	            {
	                callBack(-1);
	            }
	            catch (Exception exp)
	            {
	                Logger.Error("NumberPad callback error:{0}", exp);
	            }
	        }
	        NetManager.Instance.StartCoroutine(DestoryNextFrame());
	    }
	
	    #endregion
	}
}