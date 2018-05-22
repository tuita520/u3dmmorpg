#region using
using System;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class EquipSkillTipFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        private IControllerBase artifactController;

        private void OnDisable()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            Binding.RemoveBinding();

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
            //玛雅神器技能说明TIPS特殊处理
            artifactController = UIManager.Instance.GetController(UIConfig.ArtifactUi);
            if (artifactController == null)
            {
                return;
            }
            if (FrameState.Open == artifactController.State)
            {
                gameObject.transform.localPosition = new Vector3(379, 0, 0);
                gameObject.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }
            else
            {
                gameObject.transform.localScale = Vector3.one;
            }

            var controllerBase = UIManager.Instance.GetController(UIConfig.EquipSkillTipUI);
            if (controllerBase == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
          
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
            if (FrameState.Open == artifactController.State)
            {
                gameObject.transform.localPosition = new Vector3(379, 0, 0);
                gameObject.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        public void OnClick_Close()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipSkillTipUI));
        }
    }
}
