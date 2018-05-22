#region using
using System;
using EventSystem;
using UnityEngine;
using System.Collections.Generic;
#endregion

namespace GameUI
{
    public class EraAchievementFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public List<UIButton> MenuList;

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

        private void OnEnable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
            var controllerBase = UIManager.Instance.GetController(UIConfig.EraAchievementUI);
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

            for (var i = 0; i < MenuList.Count; i++)
            {
                var menu = MenuList[i];
                var btn = menu.GetComponent<UIButton>();
                var j = i;
                btn.onClick.Add(new EventDelegate(() => { OnTabClick(j); }));
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


#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

        public void Close()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraAchievementUI));            
        }

        public void OnTabClick(int index)
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraAchvOperate(0, index));
        }

        public void OnClickTakeAward()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraAchvOperate(1, -1));            
        }
    }
}
