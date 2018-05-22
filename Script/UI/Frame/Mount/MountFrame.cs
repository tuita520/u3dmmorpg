using System;
#region using
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
    public class MountFrame : MonoBehaviour
    {
        public GameObject mEffect;
	    public BindDataRoot Binding;
        private IControllerBase controller;

        public AnimationModel ModelRoot;
        public UIDragRotate ModelDrag;
        private Transform ModelRootTransform;
        private Vector3 ModelRootOriPos;
        public int RenderQueue = 3004;
        private void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (ModelRoot != null)
            {
                ModelRootTransform = ModelRoot.transform;
            }
            if (ModelRootTransform != null)
            {
                ModelRootOriPos = ModelRootTransform.localPosition;
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
	
	        Binding.RemoveBinding();
            EventDispatcher.Instance.RemoveEventListener(MountRefreshModel_Event.EVENT_TYPE, CreateFakeObj);
            EventDispatcher.Instance.RemoveEventListener(MountEffect_Event.EVENT_TYPE, OnEffectEvent);
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(22,0));
            EventDispatcher.Instance.DispatchEvent(new MountClickBtn_Event(0));
	    
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

            controller = UIManager.Instance.GetController(UIConfig.MountUI);
            if (controller == null)
            {
                return;
            }
            Binding.SetBindDataSource(controller.GetDataModel(""));
            Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);
            EventDispatcher.Instance.AddEventListener(MountRefreshModel_Event.EVENT_TYPE, CreateFakeObj);
            EventDispatcher.Instance.AddEventListener(MountEffect_Event.EVENT_TYPE, OnEffectEvent);
            
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(10));
        
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


	    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
        public void OnClickClose()
        {
            
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MountUI));
        }

        private void OnEffectEvent(IEvent ievent)
        {
            MountEffect_Event e = ievent as MountEffect_Event;
            if (e == null)
                return;
            if (e.Type != 0)
                return;
            NetManager.Instance.StartCoroutine(OnEffectCoroutine());
        }

        private IEnumerator OnEffectCoroutine()
        {
            mEffect.SetActive(false);
            mEffect.SetActive(true);
            yield return new WaitForSeconds(4);
            mEffect.SetActive(false);
        }

        private void CreateFakeObj(IEvent ievent)
        {
            int dataId = (ievent as MountRefreshModel_Event).MountId;
            if (ModelRoot)
            {
                ModelRoot.DestroyModel();
            }
            var tbEquip = Table.GetEquipBase(dataId);
            if (tbEquip == null)
            {
                return;
            }
            var tbMont = Table.GetWeaponMount(tbEquip.EquipModel);
            if (tbMont == null)
            {
                return;
            }
            StartCoroutine(CreateModelCoroutine(() =>
            {
                ModelRoot.DestroyModel();
                ModelRoot.CreateModel(tbMont.Path, tbEquip.AnimPath + "/Stand.anim", model =>
                {
                    ModelDrag.Target = model.transform;
                    ModelRoot.PlayAnimation();
                    model.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
                    model.gameObject.SetRenderQueue(RenderQueue);
                    var particle = model.gameObject.GetComponent<ParticleScaler>();
                    if (particle != null)
                    {
                        particle.Update();
                    }
                    
                    ModelDrag.Target.gameObject.SetActiveRecursive(true);
                    }, false);
            }));

            

        }

        IEnumerator CreateModelCoroutine(Action action)
        {
            if (transform.parent == null)
                yield return new WaitForEndOfFrame();
            action();
            yield break;
        }
        #region 点击事件
        public void OnClickFeed()
        {
            EventDispatcher.Instance.DispatchEvent(new MountClickBtn_Event(2));
        }

        public void OnClickSkill()
        {
            EventDispatcher.Instance.DispatchEvent(new MountClickBtn_Event(1));
        }

        public void OnClickRide()
        {
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(13));
        }

        public void OnClickMountUp()
        {
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(21));
        }
        public void OnClickMountUpAuto()
        {
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(22,1));
        }
        public void OnClickMountUpAutoCancel()
        {
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(22, 0));
        }
        public void OnClickMountSkillup()
        {
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(31));
        }

        public void OnClickMountBtn()
        {
            EventDispatcher.Instance.DispatchEvent(new MountMainTabEvent(0));
        }

        public void OnClickMountSkinBtn()
        {
            EventDispatcher.Instance.DispatchEvent(new MountMainTabEvent(1));
        }

        public void OnClickActiveSkinBtn()
        {
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(41));
        }

        #endregion 点击事件
    }
}