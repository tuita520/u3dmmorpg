using ClientDataModel;
using System;
#region using
using ScorpionNetLib;
using EventSystem;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#endregion

namespace GameUI
{
    public class ArtifactFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public UICenterOnChild UICenter;
        public UICenterOnChild itemUICenter;
        public UIScrollView ModelScrollView;
        public UIScrollView ItemScrollView;
        public Camera PersCamera;
        private GameObject lastCenterObject = null;
        private int enableCount = 0;
        private List<GameObject> MaYaWeaponList = new List<GameObject>();
        public GameObject Model;
        private void OnDisable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipInfoUI));
            EventDispatcher.Instance.RemoveEventListener(EnableFrameEvent.EVENT_TYPE, OnEvent_EquipmentInfo);
            EventDispatcher.Instance.RemoveEventListener(PlayCgEvent.EVENT_TYPE, OnEvent_PlayCg);
            EventDispatcher.Instance.RemoveEventListener(UpdateMaYaUIModelEvent.EVENT_TYPE, OnRefreshMaYaUIModel);
            //EventDispatcher.Instance.RemoveEventListener(MaYaWuQiDestoryModel_Event.EVENT_TYPE, DestoryModelEvent);
            AddEnableCount(-1);
            Binding.RemoveBinding();

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }
     
        private EquipInfoDataModel equipDataModel;
        private void OnEnable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            var controllerBase = UIManager.Instance.GetController(UIConfig.ArtifactUi);
            if (controllerBase == null)
            {
                return;
            }
            equipDataModel = controllerBase.GetDataModel("EquipInfo") as EquipInfoDataModel;
            if (equipDataModel == null) return;
            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            Binding.SetBindDataSource(controllerBase.GetDataModel("EquipInfo"));
            EventDispatcher.Instance.AddEventListener(EnableFrameEvent.EVENT_TYPE, OnEvent_EquipmentInfo);
            EventDispatcher.Instance.AddEventListener(PlayCgEvent.EVENT_TYPE, OnEvent_PlayCg);
            EventDispatcher.Instance.AddEventListener(UpdateMaYaUIModelEvent.EVENT_TYPE, OnRefreshMaYaUIModel);
            EventDispatcher.Instance.AddEventListener(MaYaWuQiDestoryModel_Event.EVENT_TYPE, DestoryModelEvent);

            AddEnableCount(1);
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        private void OnEvent_PlayCg(IEvent ievent)
        {
            var e = ievent as PlayCgEvent;
            if (e != null && PersCamera != null)
            {
                if (e.State == 0)
                {
                    PersCamera.enabled = false;
                }
                else if (e.State == 1)
                {
                    PersCamera.enabled = true;
                }
            }
        }

        private void OnEvent_EquipmentInfo(IEvent ievent)
        {
            var e = ievent as EnableFrameEvent;
            if (e != null)
            {
                if (e.Id < 0)
                {
                    AddEnableCount(1);
                }
                else if (e.Id > 0)
                {
                    AddEnableCount(-1);
                }
                else
                {
                    enableCount = 1;
                    if (PersCamera != null)
                    {
                        PersCamera.enabled = (enableCount > 0);
                    }
                }
            }
        }

        private void AddEnableCount(int count)
        {
            enableCount += count;
            if (PersCamera != null)
            {
                PersCamera.enabled = (enableCount > 0);
            }
        }


        private void Start()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            //EventDispatcher.Instance.DispatchEvent(new UpdateMaYaUIModelEvent(2));
            UICenter.onCenter = OnSelectModel;          

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        private void LateUpdate()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            if (ModelScrollView.currentMomentum != Vector3.zero)
                ModelScrollView.MoveOtherScrollView(ItemScrollView);
            //if (ItemScrollView.currentMomentum != Vector3.zero)
            //    ItemScrollView.MoveOtherScrollView(ModelScrollView);

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }
        public IEnumerator OnRefreshMaYaUIModelCorount(int idx)
        {
            yield return new WaitForEndOfFrame() ;
            var game = UICenter.transform.GetChild(idx);
            UICenter.CenterOn(game);
            var obj = UICenter.centeredObject;

            var itemUI = itemUICenter.transform.GetChild(idx);
            itemUICenter.CenterOn(itemUI);
            var obj1 = itemUICenter.centeredObject;
            if (null == obj || null == obj1)
            {
                yield break ;
            }

            //MaYaWeaponList[_e.listNum] = obj;
            var item = obj.GetComponent<ListItemLogic>();
            EventDispatcher.Instance.DispatchEvent(new ArtifactSelectEvent(item));

        }
        public void OnRefreshMaYaUIModel(IEvent ieve)
        {
            var _e = ieve as UpdateMaYaUIModelEvent;
            if(_e!=null)
                NetManager.Instance.StartCoroutine(OnRefreshMaYaUIModelCorount(_e.listNum));           
        }
        public void DestoryModelEvent(IEvent ieve)
        {
            if (Model == null) return;
            for (int i = 0; i < Model.transform.childCount; i++)
            {
                Destroy(Model.transform.GetChild(i).gameObject);
            }
        }
        private void OnSelectModel(GameObject centerObject)
        {
            var obj = UICenter.centeredObject;

            if (null == obj || obj == lastCenterObject)
            {
                return;
            }

            lastCenterObject = obj;

            var item = obj.GetComponent<ListItemLogic>();
            EventDispatcher.Instance.DispatchEvent(new ArtifactSelectEvent(item));
        }

        public void OnClick_Buy()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(0));
        }

        public void OnClick_CloseBuyBox()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(9));
        }

        public void OnClick_ViewSkill()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(1));
        }

        public void OnClick_GoAdvacne()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(2));
        }
        public void OnClick_CloseSkill()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(3));
        }

        public void OnClick_MyWeapon()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(4));
        }

        public void OnClickBtnClose()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(7));
            enableCount = 0;
        }

        public void OnClickHelp()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(5));
        }
        public void OnClickCloseHelp()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(6));
        }

        public void OnClickBuyInfoBuy()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(12));
        }
        public void OnClickBuyInfoMax()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(13));
        }

        public void OnClickBuyInfoAdd()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(14));
        }
        public void OnClickBuyInfoDel()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(15));
        }

        public void OnClickBuyInfoAddPress()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(16));
        }
        public void OnClickBuyInfoDelPress()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(17));
        }

        public void OnClickBuyInfoAddUnPress()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(18));
        }
        public void OnClickBuyInfoDelUnPress()
        {
            EventDispatcher.Instance.DispatchEvent(new ArtifactOpEvent(19));
        }
        public void skillMsg()
        {
            if (equipDataModel.BuffId != -1)
            {
                var arg = new UIInitArguments();
                arg.Args = new List<int>();
                arg.Args.Add(equipDataModel.BuffId);
                arg.Args.Add(equipDataModel.BuffLevel);
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.EquipSkillTipUI, arg));
            }
        }
    }
}