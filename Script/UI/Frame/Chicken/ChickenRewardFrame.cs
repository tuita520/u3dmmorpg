using UnityEngine;
using System.Collections;
using EventSystem;

namespace GameUI
{
    public class ChickenRewardFrame : MonoBehaviour
    {

        public BindDataRoot Binding;

        public void OnClickOk()
        {
            var e = new Close_UI_Event(UIConfig.ChickenRewardUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }

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
            var controllerBase = UIManager.Instance.GetController(UIConfig.ChickenRewardUI);
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
