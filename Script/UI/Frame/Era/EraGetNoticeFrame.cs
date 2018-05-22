#region using
using System;
using System.Collections;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class EraGetNoticeFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        private Transform FromObject;
        private bool closeClick = false;

        private void OnDisable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
            EventDispatcher.Instance.RemoveEventListener(UIEvent_EraGetAlpha.EVENT_TYPE, AlphaChange);

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
            var controllerBase = UIManager.Instance.GetController(UIConfig.EraGetNoticeUI);
            if (controllerBase == null)
            {
                return;
            }

            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            EventDispatcher.Instance.AddEventListener(UIEvent_EraGetAlpha.EVENT_TYPE, AlphaChange);
            closeClick = false; 

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            try
            {
#endif

            FromObject = gameObject.transform.FindChild("Tubiao/Icon1");

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
            if (closeClick)
            {
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new EraBookFlyOverEvent());
            var fromPos = transform.root.transform.InverseTransformPoint(FromObject.position);
            EventDispatcher.Instance.DispatchEvent(new Event_EraNoticeFlyIcon(fromPos));
            closeClick = true;
        }

        private Coroutine alpahCoroutine;
        private void AlphaChange(IEvent ievent)
        {
            var e = ievent as UIEvent_EraGetAlpha;
            var widget = gameObject.transform.GetComponent<UIWidget>();
            if (widget == null)
                return;

            if (alpahCoroutine != null)
            {
                if (Game.Instance != null)
                    Game.Instance.StopCoroutine(alpahCoroutine);
                alpahCoroutine = null;
            }
            if (Game.Instance != null)
                alpahCoroutine = Game.Instance.StartCoroutine(ChangeAlpha(widget, e.Time, e.CallBack));
        }

        IEnumerator ChangeAlpha(UIWidget widget, float time, Action callback)
        {
            float elapse = 0;
            widget.alpha = 1.0f;

            while (elapse < time)
            {
                yield return new WaitForSeconds(0.033f);
                elapse += 0.033f;
                if (widget != null)
                    widget.alpha = Mathf.Lerp(1, 0, elapse / time);
            }
            if (widget != null)
                widget.alpha = 1;

            Logger.Debug("EraNotice ChangeAlpha Over,Start CallBack");

            if (callback != null)
            {
                callback();
            }

            alpahCoroutine = null;
        }
    }

}
