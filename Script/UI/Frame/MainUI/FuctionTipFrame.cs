



using ScriptManager;
using System;
#region using

using EventSystem;
using UnityEngine;
using ClientDataModel;
#endregion

namespace GameUI
{
    public class FuctionTipFrame : MonoBehaviour
    {

        public BindDataRoot Binding;
        private MissionTrackListDataModel taskListDM;

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
           
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.FuctionTipFrame));
            if (taskListDM.List[0].MissionId!=null)
            {
                if (-1 != taskListDM.List[0].MissionId)
                {
                    MissionManager.Instance.GoToMissionPlace(taskListDM.List[0].MissionId);
                } 
            }
        }

        // Use this for initialization
        private void Start()
        {
#if !UNITY_EDITOR
try
{
#endif

            var controllerBase = UIManager.Instance.GetController(UIConfig.MissionTrackList);
            taskListDM = controllerBase.GetDataModel("") as MissionTrackListDataModel;
        
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

                var controllerBase = UIManager.Instance.GetController(UIConfig.FuctionTipFrame);
                if (controllerBase == null)
                {
                    return;
                }
                Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            }
            deleteBind = true;
            EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(1));
        
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

            EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(-1));

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