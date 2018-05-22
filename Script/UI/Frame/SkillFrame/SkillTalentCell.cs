using System;
#region using
using System.Collections;
using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class SkillTalentCell : MonoBehaviour
    {
        public BindDataRoot Binding = null;
        public float EffectScale = 1.0f;
        private GameObject useableEffect;
        private GameObject unlockEffect;
        public int SkillTalentId;
        public GameObject TalentName = null;
        public TalentCellDataModel CellDataModel { get; set; }


        public void InitEffect()
        {
            ShowTalentBallLabel(CellDataModel.Count);
            ShowParticleEffect();
        }

        private void OnClick()
        {
            var e = new UIEvent_SkillFrame_TalentBallClick(SkillTalentId);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            EventDispatcher.Instance.RemoveEventListener(UIEvent_SkillTalentUpEffect.EVENT_TYPE, StartEffectTalentUp);
            CellDataModel.PropertyChanged -= OnEvent_PropertyChange;
            if (Binding != null)
                Binding.RemoveBinding();

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

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        private void OnDrag(Vector2 delta)
        {
            if (null != TouchSpinning.s_tsInstance)
            {
                TouchSpinning.s_tsInstance.OnDrag(delta);
            }
        }

        private void OnEnable()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillTalentUpEffect.EVENT_TYPE, StartEffectTalentUp);
            TalentName.GetComponent<UILabel>().text = Table.GetTalent(SkillTalentId).Name;
#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        private void OnPress(bool press)
        {
            if (null != TouchSpinning.s_tsInstance)
            {
                TouchSpinning.s_tsInstance.OnPress(press);
            }
        }

        private void OnEvent_PropertyChange(object o, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "TalentEnable")
            {
                //PlayUnlockEffect(CellDataModel.TalentEnable);
            }

            if (args.PropertyName == "ShowLine")
            {
                PlayConnection(CellDataModel.ShowLine);
            }

            if (args.PropertyName == "Count")
            {
                ShowTalentBallLabel(CellDataModel.Count);
            }
        }

        private void ShowParticleEffect()
        {
            //  PlayUnlockEffect(CellDataModel.TalentEnable);
            PlayConnection(CellDataModel.ShowLine);
        }

        private void ShowTalentBallLabel(int count)
        {
            var TalentTable = Table.GetTalent(SkillTalentId);
            var countLabel = transform.FindChild("CountLabel");
            if (null != countLabel)
            {
                var label = countLabel.GetComponent<UILabel>();
                label.text = string.Format("{0}/{1}", count, TalentTable.MaxLayer);
            }
            else
            {
                Logger.Error("can not find uilabel from TalentCell!!");
            }
        }

        private void PlayConnection(bool bShow)
        {
            if (bShow)
            {
                var trans = transform.FindChild("ProgressBar");
                var slider = trans.GetComponent<UISlider>();
                if (null != slider)
                {
                    slider.value = 1.0f;
                }

                //ResourceManager.PrepareResource<GameObject>
                //    ("Effect/UI/SkillFrame/UI_Talent_Enabled.prefab", res =>
                //    {
                //        if (useableEffect)
                //        {
                //            NGUITools.Destroy(useableEffect);
                //        }
                //        useableEffect = NGUITools.AddChild(gameObject, res);
                //        var scale = useableEffect.transform.localScale;
                //        scale.x = scale.x * EffectScale;
                //        scale.y = scale.y * EffectScale;
                //        useableEffect.transform.localScale = scale;
                //    }, true, true, true, true);
            }
            else
            {
                var slider = gameObject.GetComponentInChildren<UISlider>();
                if (null != slider)
                {
                    slider.value = 0;
                }

                if (useableEffect)
                {
                    NGUITools.Destroy(useableEffect);
                    useableEffect = null;
                }
            }
        }

        private void PlayUnlockEffect(bool bShow)
        {
            if (bShow)
            {
                var skillbox = gameObject.transform.parent.parent.parent.GetComponent<SkillOutBox>();
                if (skillbox.BoxDataModel.skillItem.SkillLv < 1)
                {
                    return;
                }
                ResourceManager.PrepareResource<GameObject>
                    ("Effect/UI/SkillFrame/UI_Talent_Open.prefab", res =>
                    {
                        if (unlockEffect)
                        {
                            NGUITools.Destroy(unlockEffect);
                        }
                        unlockEffect = NGUITools.AddChild(gameObject, res);
                    });
            }
            else
            {
                if (unlockEffect)
                {
                    NGUITools.Destroy(unlockEffect);
                    unlockEffect = null;
                }
            }
        }

        private void Start()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            CellDataModel.PropertyChanged += OnEvent_PropertyChange;
            if (Binding != null)
                Binding.SetBindDataSource(CellDataModel);
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
        private Coroutine coroutine;
        private GameObject levelUpEffect;
        private void StartEffectTalentUp(IEvent ievent)
        {
            var e = ievent as UIEvent_SkillTalentUpEffect;
            if (SkillTalentId == e.skillId)
            {
                ResourceManager.PrepareResource<GameObject>
                    ("Effect/UI/JiNengShengJi.prefab", res =>
                    {
                        if (levelUpEffect)
                        {
                            NGUITools.Destroy(levelUpEffect);
                            if (null != coroutine)
                            {
                                ResourceManager.Instance.StopCoroutine(coroutine);
                            }
                        }
                        levelUpEffect = NGUITools.AddChild(gameObject, res);
                        coroutine = ResourceManager.Instance.StartCoroutine(DestroyEffectLevelup());
                    });
            }
        }

        private IEnumerator DestroyEffectLevelup()
        {
            yield return new WaitForSeconds(1);
            if (levelUpEffect)
            {
                NGUITools.Destroy(levelUpEffect);
                levelUpEffect = null;
            }
            coroutine = null;
        }
    }
}