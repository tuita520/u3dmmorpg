
#region using

using System;
using SignalChain;
using EventSystem;
using UnityEngine;
using DataTable;
using DataContract;
using System.Collections.Generic;
using System.Collections;
using ScorpionNetLib;
using ClientService;
using ScriptManager;

#endregion

namespace GameUI
{
    public class ShiZhuangFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        private IControllerBase controller;

        public CreateFakeCharacter ModelRoot;
        public UIDragRotate ModelDrag;

        public enum eBagType
        {
            Equip = 26,
            Wing = 27,
            Weapon = 28,
        }

        private void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif

            //TO BE ADDED

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

            //TO BE ADDED

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
            CreateCharacterModel();
            controller = UIManager.Instance.GetController(UIConfig.ShiZhuangUI);
            if (controller == null)
            {
                return;
            }
            Binding.SetBindDataSource(controller.GetDataModel(""));
            Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);
            Binding.SetBindDataSource(UIManager.Instance.GetController(UIConfig.WingUI).GetDataModel(""));
            Binding.SetBindDataSource(UIManager.Instance.GetController(UIConfig.BackPackUI).GetDataModel(""));
            EventDispatcher.Instance.AddEventListener(MyEquipChangedEvent.EVENT_TYPE, OnSelfEquipChanged);

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
            DestroyCharacterModel();
            Binding.RemoveBinding();
            EventDispatcher.Instance.RemoveEventListener(MyEquipChangedEvent.EVENT_TYPE, OnSelfEquipChanged);

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }

        #region 回调函数

        public void OnSelfEquipChanged(IEvent ievent)
        {
            var evn = ievent as MyEquipChangedEvent;

            if (null == ModelRoot.Character)
            {
                return;
            }
            ModelRoot.Character.GetComponent<ObjFakeCharacter>().ChangeEquip(evn.Part, evn.Item);
        }

        #endregion

        #region 逻辑函数
        private void CreateCharacterModel()
        {
            DestroyCharacterModel();
            var player = ObjManager.Instance.MyPlayer;
            var elfId = -1;
            var elfColorId = -1;
            GameUtils.GetFightElfId(ref elfId, ref elfColorId);
            ModelRoot.Create(player.GetDataId(), player.EquipList, character => { ModelDrag.Target = character.transform; });
        }

        private void DestroyCharacterModel()
        {
            ModelRoot.DestroyFakeCharacter();
        }

        #endregion

        #region 按钮事件
        public void OnClickClose()
        {
            var e = new Close_UI_Event(UIConfig.ShiZhuangUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickCharacter()
        {
            OnClickClose();
            var e = new Show_UI_Event(UIConfig.CharacterUI);
            EventDispatcher.Instance.DispatchEvent(e);
            PlayerDataManager.Instance.WeakNoticeData.BagTotal = false;
        }

        public void OnClickStore()
        {
            OnClickClose();
            var e = new UIEvent_RechargeFrame_OnClick(2);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickDepot()
        {
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel) < 3)
            {
                var str = GameUtils.GetDictionaryText(100000675);//VIP3将开启随身仓库
                GameUtils.ShowHintTip(str);
            }
            else
            {
                OnClickClose();
                var e = new UIEvent_RechargeFrame_OnClick(3);
                EventDispatcher.Instance.DispatchEvent(e);
            }
        }

        public void OnClickHideEquip()
        {
            var e = new ShiZhuangOperaEvent(2);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickHideWeapon()
        {
            var e = new ShiZhuangOperaEvent(3);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickHideWing()
        {
            var e = new ShiZhuangOperaEvent(4);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickWing()
        {
            var e = new ShiZhuangSelectTypeEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickEquip()
        {
            var e = new ShiZhuangSelectTypeEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickWeapon()
        {
            var e = new ShiZhuangSelectTypeEvent(2);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickBagTab()
        {
            var e = new ShiZhuangChangeTabEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickStoreTab()
        {
            var e = new ShiZhuangChangeTabEvent(1);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickBuyInfoClose()
        {
            var e = new ShiZhuangOperaEvent(0);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickConfirmBuy()
        {
            EventDispatcher.Instance.DispatchEvent(new ShiZhuangOperaEvent(1));
        }
        #endregion





    }
}
