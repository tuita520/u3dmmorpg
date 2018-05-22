using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EventSystem;
using ClientDataModel;
using System;
using GameUI;
using DataTable;
using System.ComponentModel;
namespace GameUI
{
    public class FieldMineFrame : MonoBehaviour
    {
        public FieldMineDataModel DataModel;
        public BindDataRoot Binding;
        private bool removeBind = true;
        public List<GameObject> LodeList;
        private void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif
            if (removeBind)
            {
                var controller = UIManager.Instance.GetController(UIConfig.FieldMineUI);
                DataModel = controller.GetDataModel("") as FieldMineDataModel;
                Binding.SetBindDataSource(DataModel);
            }
            removeBind = true;
            EventDispatcher.Instance.AddEventListener(LodePositionSetEvent.EVENT_TYPE, OnPositionSetEvent);


#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }
        void Start()
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

        // Update is called once per frame
        void Update()
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
        private void OnDisable()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (removeBind)
            {
                RemoveBindingEvent();
            }
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
try
{
#endif


            if (removeBind == false)
            {
                RemoveBindingEvent();
            }
            removeBind = true;


#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }
        private void RemoveBindingEvent()
        {
            EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUIBindingRemove);
            EventDispatcher.Instance.RemoveEventListener(LodePositionSetEvent.EVENT_TYPE, OnPositionSetEvent);
            Binding.RemoveBinding();
        }
        private void OnCloseUIBindingRemove(IEvent ievent)
        {
            var e = ievent as CloseUiBindRemove;
            if (e.Config != UIConfig.AcientBattleFieldFrame)
            {
                return;
            }
            if (e.NeedRemove == 0)
            {
                removeBind = false;
            }
            else
            {
                if (removeBind == false)
                {
                    RemoveBindingEvent();
                }
                removeBind = true;
            }
        }
        public void OnClickLodeItem(int index)
        {
            var e = new LodeItemClickEvent(index);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void EnterBtnClick()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldItemOperationEvent());
        }

        public void OnClickPreviewBtn()
        {
            var e = new FieldPreviewEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        public void OnClosePreviewBtn()
        {
            var e = new FieldPreviewEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnBtnClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.FieldMineUI));
        }
        private void OnPositionSetEvent(IEvent ievent)
        {
            var e = ievent as LodePositionSetEvent;
            if (e.idx >= LodeList.Count)
            {
                return;
            }
            LodeList[e.idx].transform.localPosition = new Vector3(e.posX,e.posY,0);
        }
    }
}
