#region using

using System;
using System.Collections;
using System.Collections.Generic;
using ScriptManager;
using ScriptController;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class FriendFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public GameObject BlockLayer;
        public UIInput ChatInput;
        public UIInput FindInput;
        public UIGridSimple GridContact;
        private float blockTime = -1.0f;
        private bool enable;
        private bool canBind = true;


        public GameObject[] mBtns;
        public StackLayout mStackLayout;
        public GameObject mList;
        public GameObject mParent;
        public UIScrollViewSimple grid;

        public UIGridSimple speGrid;


        public void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif

            for (int i = 0; i < mBtns.Length; i++)
            {
                UIEventTrigger evt = mBtns[i].GetComponent<UIEventTrigger>();
                if (evt != null)
                {
                    int j = i;
                    evt.onClick.Add(new EventDelegate(() => { OnClickFriendTab(j); }));
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

        private void OnUpdateStackEvent(IEvent ievent)
        {
            FriendTabUpdateEvent e = ievent as FriendTabUpdateEvent;
            if (e != null)
            {
                if (e.Type >= 0)
                    UpdateStack(e.Type);
                else
                {
                    if ((int)controllerBase.CallFromOtherClass("GetContactInfoNumber", null) == 0 && (int)controllerBase.CallFromOtherClass("GetFriendsInfoNumber", null) == 0)
                    {//û����ϵ�˺ͺ���
                        OnClickFriendTab(4);

                    }
                    else if ((int)controllerBase.CallFromOtherClass("GetContactInfoNumber", null) > 0)
                    {//����ϵ��
                        OnClickFriendTab(0);
                        //EventDispatcher.Instance.DispatchEvent(new FriendContactCell(controllerBase.GetLastChatRelation().CharacterId,0));
                    }
                    else if ((int)controllerBase.CallFromOtherClass("GetFriendsInfoNumber", null) > 0)
                    {//�к���
                        OnClickFriendTab(1);
                        //EventDispatcher.Instance.DispatchEvent(new FriendContactCell(controllerBase.m_DataModel.FriendInfos[0].Guid, controllerBase.m_DataModel.FriendInfos[0].FromType));
                    }
                }
            }
        }
        private void UpdateStack(int idx = 4)
        {
            for (int i = 0; i < mBtns.Length; i++)
            {
                mBtns[i].transform.parent = null;
            }
            mList.transform.parent = null;
            for (int i = 0; i < mBtns.Length; i++)
            {
                mBtns[i].transform.parent = mParent.transform;
                if (i == idx)
                {
                    mList.transform.parent = mParent.transform;
                }
            }
            mStackLayout.ResetLayout();
            grid.ResetScrollViewPostionOffset();
        }
        public void OnClickFriendTab(int idx)
        {
            UpdateStack(idx);
            StartCoroutine(OnClickFriendTabCoroutine(idx));

        }

        IEnumerator OnClickFriendTabCoroutine(int idx)
        {
            yield return new WaitForEndOfFrame();
            EventDispatcher.Instance.DispatchEvent(new FriendBtnEvent(idx));
        }
        private void LateUpdate()
        {
#if !UNITY_EDITOR
            try
            {
#endif

                if (enable)
                {
                    GridContact.enabled = true;
                    enable = false;
                }

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        public void OnClickBag()
        {
            var arg = new ChatItemListArguments();
            arg.Type = 3;
            var e = new Show_UI_Event(UIConfig.ChatItemList, arg);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        //-------------------------------------------------------Chat------------
        public void OnClickChatSend()
        {
            var e = new FriendClickType(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickChatTab()
        {
            enable = true;
            var e = new FriendClickType(4);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickClearRecord()
        {
            var e = new FriendClickType(3);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.FriendUI));
        }

        public void OnClickFace()
        {
            var arg = new FaceListArguments();
            arg.Type = 3;
            var e = new Show_UI_Event(UIConfig.FaceList, arg);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickQuickSeekBtn()
        {
            EventDispatcher.Instance.DispatchEvent(new FriendSeekBtnClick(1));
        }
     
        public void OnClickSeekBtn()
        {
            EventDispatcher.Instance.DispatchEvent(new FriendSeekBtnClick(0));
        }
        private int m_tabIndex = 1;
        public void OnClickSeekBackBtn()
        {
            OnEvent_SelectTab(m_tabIndex);
        }
        private void OnEvent_SelectTab(int i)
        {
            m_tabIndex = i;
            var e = new FriendClickTabEvent(i);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        private void OnEvent_CancleBind(IEvent ievent)
        {
            var e = ievent as CloseUiBindRemove;
            if (e.Config != UIConfig.FriendUI)
            {
                return;
            }
            if (e.NeedRemove == 0)
            {
                canBind = false;
            }
            else
            {
                if (canBind == false)
                {
                    RemoveBindEvent();
                }
                canBind = true;
            }
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
            try
            {
#endif
                if (canBind == false)
                {
                    RemoveBindEvent();
                }
                canBind = true;
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
                if (canBind)
                {
                    RemoveBindEvent();
                    controllerBase.State = FrameState.Close;
                }
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private PartnerFrameCtrler controllerBase = null;
        private void OnEnable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
                SetBlockLayer(false);

                if (canBind)
                {
                    EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CancleBind);
                    EventDispatcher.Instance.AddEventListener(FriendNotify.EVENT_TYPE, OnEvent_FriendNotify);
                    EventDispatcher.Instance.AddEventListener(FriendTabUpdateEvent.EVENT_TYPE, OnUpdateStackEvent);
                    
                    controllerBase = UIManager.Instance.GetController(UIConfig.FriendUI) as PartnerFrameCtrler;
                    if (controllerBase == null)
                    {
                        return;
                    }
                    Binding.SetBindDataSource(controllerBase.GetDataModel(""));
                    Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);
                }
                canBind = true;
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
            OnUpdateStackEvent(new FriendTabUpdateEvent(-1));
            controllerBase.State = FrameState.Open; 
        }

        private void OnEvent_FriendNotify(IEvent ievent)
        {
            var e = ievent as FriendNotify;
            if (e.Type == 1)
            {
                blockTime = -1.0f;
                SetBlockLayer(false);
            }
            else if (e.Type == 2)
            {
                blockTime = 30.0f; //30s ����ʱ
                SetBlockLayer(true);
            }
        }

        public void OnInputFindForcus()
        {
            var e = new FriendClickType(6);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnInputFindLostForcus()
        {
            if (String.IsNullOrEmpty(FindInput.label.text))
            {
                FindInput.label.text = GameUtils.GetDictionaryText(240612);
            }
        }

        public void OnInputForcus()
        {
            var e = new FriendClickType(5);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnInputLostForcus()
        {
            if (String.IsNullOrEmpty(ChatInput.label.text))
            {
                ChatInput.label.text = GameUtils.GetDictionaryText(100001058);    
            }
        }

        private void RemoveBindEvent()
        {
            Binding.RemoveBinding();
            EventDispatcher.Instance.RemoveEventListener(FriendNotify.EVENT_TYPE, OnEvent_FriendNotify);
            EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CancleBind);
            EventDispatcher.Instance.RemoveEventListener(FriendTabUpdateEvent.EVENT_TYPE, OnUpdateStackEvent);
        }

        private void SetBlockLayer(bool value)
        {
            if (BlockLayer != null)
            {
                BlockLayer.SetActive(value);
            }
        }

        private void Start()
        {
#if !UNITY_EDITOR
            try
            {
#endif  
            if (ChatInput != null)
            {
                ChatInput.value = null;
                OnInputLostForcus();
            }                                         
                //var index = 0;
                //foreach (var tab in TabList)
                //{
                //    var j = index;
                //    var deleget = new EventDelegate(() => { OnEvent_SelectTab(j); });
                //    index++;
                //    tab.onClick.Add(deleget);
                //}
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
                if (blockTime > 0)
                {
                    blockTime -= Time.deltaTime;
                    if (blockTime < 0)
                    {
                        SetBlockLayer(false);
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
    }
}
