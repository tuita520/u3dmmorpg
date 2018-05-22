using System;
#region using

using System.Collections;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;
using System.Collections.Generic;
#endregion

namespace GameUI
{
    public class ModelDisplayFrame : MonoBehaviour
	{
        public BindDataRoot Binding;
        public AnimationModel Model;
        public WeaponModel EquipModel;
        public GameObject EffectOne;
        public GameObject EffectLoop;
        private CreateFakeCharacter ModelRoot;
        public Vector3 EquipForward = new Vector3(-45, 90, 0);
        public float RotateSpeed = 1.0f;
        public List<EquipPosition> ListEquipOffset = new List<EquipPosition>();
        public int ModelRenderQueue = 3004;
        public float LoopEffectDealy = 1.0f;

        private Coroutine effectCoroutine = null;

        void Awake()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            ModelRoot = gameObject.transform.FindChild("Anim/ModelView").GetComponent<CreateFakeCharacter>();

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

        private void OnEnable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            var controllerBase = UIManager.Instance.GetController(UIConfig.ModelDisplayFrame);
            if (controllerBase == null)
            {
                return;
            }

            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            EventDispatcher.Instance.AddEventListener(ModelDisplay_ShowModel_Event.EVENT_TYPE, OnEvent_ShowModel);
            EffectOne.SetActive(false);
            EffectLoop.SetActive(false);

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
            DestroyModel();
            Binding.RemoveBinding();
            EventDispatcher.Instance.RemoveEventListener(ModelDisplay_ShowModel_Event.EVENT_TYPE, OnEvent_ShowModel);

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        // Update is called once per frame
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

        private void OnDestroy()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;                
            }
            DestroyModel();

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        public void OnClick_Equip()
        {
            var e = new ModelDisplay_Equip_Event();
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnEvent_ShowModel(IEvent ievent)
        {
            var e = ievent as ModelDisplay_ShowModel_Event;
            if (e != null && e.UIType == 0)
            {
                ShowModel(e.EquipId);
            }
        }

        private void ShowModel(int itemId)
        {
            DestroyModel();

            var tbItemBase = Table.GetItemBase(itemId);
            if (tbItemBase == null)
                return;

            var itemType = GameUtils.GetItemInfoType(tbItemBase.Type);
            switch (itemType)
            {
                case eItemInfoType.Equip:
                    CreateWeaponModel(itemId);
                    break;
                case eItemInfoType.Wing:
                    CreateWingModel(itemId);
                    break;
                case eItemInfoType.Item:
                    CreateItemModel(itemId);
                    break;
                case eItemInfoType.Elf:
                    CreateElf(itemId);
                    break;
                default:
                    return;
            }

            EffectOne.SetActive(true);
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }

            //ParticleSystem[] psArray = EffectOne.GetComponentsInChildren<ParticleSystem>();
            //foreach (ParticleSystem ps in psArray)
            //{
            //    duration = Math.Max(ps.duration, duration);
            //}
            effectCoroutine = StartCoroutine(WaitEffectCoroutine(LoopEffectDealy));

        }

        private void CreateWingModel(int equipId)
        {
            var tbEquip = Table.GetEquipBase(equipId);
            if (tbEquip == null)
            {
                return;
            }
            var tbMont = Table.GetWeaponMount(tbEquip.EquipModel);
            if (tbMont == null)
            {
                return;
            }

            Model.CreateModel(tbMont.Path, tbEquip.AnimPath + "/FlyIdle.anim", theModel =>
            {
                if (theModel != null)
                {
                    theModel.transform.parent = Model.transform;
                    Vector3 v = Vector3.zero;
                    foreach (var ep in ListEquipOffset)
                    {
                        if (ep.EquipId == equipId)
                        {
                            v = ep.OriginPosition;
                            break;
                        }
                    }
                    theModel.transform.localPosition = v;
                    theModel.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
                    theModel.gameObject.SetRenderQueue(ModelRenderQueue);

                    Model.PlayAnimation();
                }
            });
        }

        private void CreateItemModel(int equipId)
        {
            var tbEquip = Table.GetEquipBase(equipId);
            if (tbEquip == null)
            {
                return;
            }
            var tbMont = Table.GetWeaponMount(tbEquip.EquipModel);
            if (tbMont == null)
            {
                return;
            }

            Model.CreateModel(tbMont.Path, tbEquip.AnimPath + "/Stand.anim", theModel =>
            {
                if (theModel != null)
                {
                    theModel.transform.parent = Model.transform;
                    Vector3 v = Vector3.zero;
                    Vector3 r = Vector3.zero;
                    Vector3 s = Vector3.one;
                    foreach (var ep in ListEquipOffset)
                    {
                        if (ep.EquipId == equipId)
                        {
                            v = ep.OriginPosition;
                            r = ep.OriginRotation;
                            s = ep.OriginScale;
                            break;
                        }
                    }
                    theModel.transform.localPosition = v;
                    theModel.transform.localRotation = Quaternion.Euler(r);
                    theModel.transform.localScale = s;
                    theModel.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
                    theModel.gameObject.SetRenderQueue(ModelRenderQueue);
                    if (theModel.GetComponent<RotateAround>() == null)
                    {
                        var rotate = theModel.AddComponent<RotateAround>();
                        rotate.Forward = false;
                        rotate.Up = true;
                        rotate.Speed = RotateSpeed;
                    }
                    Model.PlayAnimation();
                }
            });
        }

        private void CreateWeaponModel(int equipId)
        {
            EquipModel.CreateModel(equipId, theModel =>
            {
                if (null == theModel)
                {
                    return;
                }
                theModel.transform.parent = EquipModel.transform;
                theModel.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
                theModel.gameObject.SetRenderQueue(ModelRenderQueue);

                Vector3 v = new Vector3(0.0f, 0.0f, 1.32f);
                foreach (var ep in ListEquipOffset)
                {
                    if (ep.EquipId == equipId)
                    {
                        v = ep.OriginPosition;
                        break;
                    }
                }
                theModel.transform.localPosition = v;
                theModel.transform.rotation = Quaternion.Euler(EquipForward);

                if (theModel.GetComponent<RotateAround>() == null)
                {
                    var rotate = theModel.AddComponent<RotateAround>();
                    rotate.Forward = true;
                    rotate.Speed = RotateSpeed;
                }
            });
        }

        private void CreateElf(int itemId)
        {
            if (ModelRoot == null || itemId < 0)
                return;

            var tbElf = Table.GetElf(itemId);
            if (tbElf == null)
                return;

            var dataId = tbElf.ElfModel;
            var tableNpc = Table.GetCharacterBase(dataId);
            if (null == tableNpc)
                return;

            var offset = tableNpc.CameraHeight / 10000.0f;
            ModelRoot.DestroyFakeCharacter();

            ModelRoot.Create(dataId, null, character =>
            {
                character.SetScale(tableNpc.CameraMult / 10000.0f);
                character.ObjTransform.localPosition = new Vector3(0, offset, 0);
            });
        }

        private void DestroyModel()
        {
            if (Model != null)
            {
                Model.RemoveAllCompent();
                Model.DestroyModel();
            }

            if (EquipModel != null)
            {
                EquipModel.RemoveAllCompent();
                EquipModel.DestroyModel();
            }

            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }

            if (ModelRoot != null)
            {
                ModelRoot.DestroyFakeCharacter();
            }
        }

        private IEnumerator WaitEffectCoroutine(float time)
        {
            yield return new WaitForSeconds(time);
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }

            EffectLoop.SetActive(true);
        }
	}
}