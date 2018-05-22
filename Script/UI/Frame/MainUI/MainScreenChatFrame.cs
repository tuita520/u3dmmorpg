using System;
#region using

using EventSystem;
using SignalChain;
using UnityEngine;

#endregion

namespace GameUI
{
	public class MainScreenChatFrame : MonoBehaviour, IChainRoot, IChainListener
	{
	    public GameObject ActiveTip;
	    public UIWidget ChatContent;
	    public StackLayout ChatMessages;
	    private Transform chatMessageTrans;
	    private bool isEnable = true;
	    private Transform messageTrans;
	    private int maxLength;
        //once time
	    private float hornTime;
        private float onceTime;
        private bool Playing = false;
	    public GameObject TrumpetBg;
        public GameObject TrumpetBgSham;
	    public UIPanel TrumpetIPanel;
	    public ChatMessageLogic TrumpetMsg;
	
	    private void Awake()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        messageTrans = TrumpetMsg.transform;
	        chatMessageTrans = ChatMessages.transform;
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    private void SetHornPostion()
	    {
	        return;
	        if (ActiveTip.activeSelf && TrumpetBg.activeSelf)
	        {
	            ActiveTip.transform.localPosition = new Vector3(0, 70, 0);
	
	            TrumpetBg.transform.localPosition = new Vector3(0, 30, 0);
	        }
	        else if (!ActiveTip.activeSelf && TrumpetBg.activeSelf)
	        {
	            TrumpetBg.transform.localPosition = new Vector3(0, 30, 0);
	        }
	        else if (ActiveTip.activeSelf && !TrumpetBg.activeSelf)
	        {
	            ActiveTip.transform.localPosition = new Vector3(0, 30, 0);
	        }
	    }
	
	    private void LateUpdate()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        if (isEnable)
	        {
	            isEnable = false;
	            ChatMessages.height = 0;
	            ChatMessages.ResetLayout();
	            var pos = chatMessageTrans.localPosition;
	            var h = ChatMessages.height - 80;
	            chatMessageTrans.localPosition = new Vector3(pos.x, h, pos.z);
	
	            SetHornPostion();
	        }
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void OnClickChatNode()
	    {
	        var e = new Show_UI_Event(UIConfig.ChatMainFrame);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }

        public void OnClickGoToActivity()
        {
            var e = new OnCLickGoToActivityByMainUIEvent();
	        EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickGoToActivity2()
        {
            var e = new OnCLickGoToActivityByMain2UIEvent();
            EventDispatcher.Instance.DispatchEvent(e);
        }


	    private void OnDestroy()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        EventDispatcher.Instance.RemoveEventListener(ChatMainNewTrumpet.EVENT_TYPE, OnNewHorn);
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    private void OnNewHorn(IEvent ievent)
	    {
	        var e = ievent as ChatMainNewTrumpet;
	        hornTime = GameUtils.TrumpetDurationTime;
            onceTime = 0;
            TrumpetBg.SetActive(true);
            TrumpetBgSham.SetActive(true);
	        messageTrans.localPosition = new Vector3(200f, 0f, 0f);//Vector3.zero;
            Playing = false;

            SetHornPostion();
	    }

		private void OnEnable()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (!EventDispatcher.Instance.HasEventListener(ChatMainNewTrumpet.EVENT_TYPE, OnNewHorn))
			{
				EventDispatcher.Instance.AddEventListener(ChatMainNewTrumpet.EVENT_TYPE, OnNewHorn);
			}

		
		
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
	        TrumpetBg.SetActive(false);
            TrumpetBgSham.SetActive(false);
	        maxLength = (int) TrumpetIPanel.baseClipRegion.z;
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
            //��ʼ
            if (hornTime > 0 && !Playing)
            {
                Playing = true;
                TrumpetBg.SetActive(true);
                TrumpetBgSham.SetActive(true);
               
            }
            //������
            else if (Playing)
            {
                //���ٲ���һ��
                var max = TrumpetMsg.GetMaxLength();
                var loc = messageTrans.localPosition;

                hornTime -= Time.deltaTime;
                onceTime += Time.deltaTime;

                loc.x -= Time.deltaTime * GameUtils.TrumpeMoveSpeedt;
                //���ν���
                if (loc.x < -max)
                {
                    if (hornTime <= 0 || hornTime < onceTime)
                    {
                        Playing = false;
                        hornTime = -1f;
                    }
                    else
                    {
                        //����һ��
                        loc.x = maxLength;
                        messageTrans.localPosition = loc;
                        onceTime = 0;
                    }

                }
                else
                {
                    //����
                    messageTrans.localPosition = loc;
                }
            }
            //����
            else if (hornTime < 0 && !Playing)
            {
                hornTime = 0;
                TrumpetBg.SetActive(false);
                TrumpetBgSham.SetActive(false);
            }

	        //if (hornTime > 0.0f)
	        //{
	        //    hornTime -= Time.deltaTime;
         //       onceTime += Time.deltaTime;
         //       if (hornTime <= 0.0f)
	        //    {
	        //        hornTime = 0.0f;
         //           onceTime = 0;
         //           TrumpetBg.SetActive(false);
	
	        //        SetHornPostion();
	        //    }
	        //    var max = TrumpetMsg.GetMaxLength();
	        // //   if (max > maxLength)
	        //    {
	        //        var loc = messageTrans.localPosition;
	        //        loc.x -= Time.deltaTime*GameUtils.TrumpeMoveSpeedt;
	        //        if (loc.x < -max)
	        //        {
	        //            loc.x = maxLength;
         //               //If the remaining time is less than the time required to run once, give up
         //               if (hornTime < onceTime)
         //               {
         //                  /// if (hornTime <= 0)
         //                   {
         //                       onceTime = 0;
         //                   }
                        
                          
         //                   TrumpetBg.SetActive(false);

         //                   SetHornPostion();
         //                   //return;
         //               }
         //               else
         //               {
         //                   onceTime = 0;
         //               }
	        //        }
	        //        messageTrans.localPosition = loc;
	        //    }
	        //}
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void Listen<T>(T message)
	    {
	        if (message is string && (message as string) == "ActiveChanged")
	        {
	            isEnable = true;
	        }
	    }
	}
}