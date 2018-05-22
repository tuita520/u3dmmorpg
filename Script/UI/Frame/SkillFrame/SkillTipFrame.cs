



using System;
#region using

using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class SkillTipFrame : MonoBehaviour
    {

        public BindDataRoot Binding;
        private bool deleteBind = true;
        private void OnDestroy()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (deleteBind == false)
            {
                RemoveBindEvent();
            }
            deleteBind = true;
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

        public void OnCloseTipClick()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SkillTipFrameUI));
        }

        // Use this for initialization
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
        private void RemoveBindEvent()
        {
            Binding.RemoveBinding();
        }
        private void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (deleteBind)
            {

                var controllerBase = UIManager.Instance.GetController(UIConfig.SkillTipFrameUI);
                if (controllerBase == null)
                {
                    return;
                }
                Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            }
            deleteBind = true;

        
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

            var e = new UIEvent_SkillFrame_OnDisable();
            EventDispatcher.Instance.DispatchEvent(e);
            if (deleteBind)
            {
                RemoveBindEvent();
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