using System;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

namespace GameUI
{
    public class AnnouncementHelper
    {
        public static void ShowAnnouncement(string url, Action afterClose)
        {
            if (string.IsNullOrEmpty(url))
            {
                Logger.Debug("show web view ,url = null");
                return;
            }

            var uiroot = GameUtils.GetUiRoot();
            var trans = uiroot.transform.FindChild("StartupWindow/Announcement");

            if (trans == null)
            {
                trans = uiroot.transform.Find("TYPE_BASE/ServerList/Announcement/Announcement");
            }

            if (null != trans)
            {
                var announcement = trans.gameObject;
                if (!announcement.activeSelf)
                {
                    announcement.SetActive(true);
                }
                var an = announcement.GetComponent<Announcement>();
                an.Show(url, afterClose);

                var btnTrans = announcement.transform.FindChild("content/CloseBtn");
                if (null != btnTrans)
                {
                    var btn = btnTrans.GetComponent<UIButton>();
                    btn.onClick.Add(new EventDelegate(() =>
                    {
                       announcement.SetActive(false);
                    }
                        ));
                }
            }
            else
            {
                ResourceManager.PrepareResource<GameObject>(Resource.PrefabPath.Announcement, (res) =>
                {
                    var go = NGUITools.AddChild(uiroot.gameObject, res);
                    var ann = go.AddComponent<Announcement>();
                    ann.Show(url, afterClose);
                    var btnTrans = go.transform.FindChild("content/CloseBtn");
                    if (null != btnTrans)
                    {
                        var btn = btnTrans.GetComponent<UIButton>();
                        btn.onClick.Add(new EventDelegate(() => { NGUITools.Destroy(go); }
                            ));
                    }
                }, true, true, true); 
            }


        }
    }

    public class Announcement : MonoBehaviour
    {


        private Action afterClose = null;
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

        private void OnDisable()
        {
#if !UNITY_EDITOR
try
{
#endif

            PlatformHelper.CLoseWebView();
            if (null != afterClose)
            {
                afterClose();
            }
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

        public void Show(string url, Action callback)
        {
            try
            {
                afterClose = callback;
                var bkTrans = transform.FindChild("content/NoticeForDevice");
                if (null == bkTrans)
                {
                    Logger.Error("bkTrans is null");
                    Destroy(gameObject);
                    return; 
                }
                var uiroot = GameUtils.GetUiRoot();
                if (null == uiroot)
                {
                    Logger.Error("uiroot is null");
                    Destroy(gameObject);
                    return;
                }
                var resoultionRadio = 1f;//GameUtils.GetResolutionRadio();
                var scale = uiroot.activeHeight / (float)Screen.height;
                var sprWidget = bkTrans.GetComponent<UIWidget>();

                Vector2 size;

                if (null != sprWidget)
                {
//                     Destroy(gameObject);
//                     return;
                    size = sprWidget.localSize;
                }
                else
                {
                    Logger.Error("sprWidget is null");
                    size = new Vector2(1020f, 540f);
                }

                var uicamera = uiroot.GetComponentInChildren<UICamera>();
                if (null == uicamera)
                {
                    Logger.Error("can't find uicamera!!! on show announce!");
                    Destroy(gameObject);
                    return;
                }

                var pos = uicamera.camera.WorldToScreenPoint(bkTrans.position);
                float width = 0;
                float height = 0;
                float x = 0;
                float y = 0;

                if (RuntimePlatform.Android == Application.platform)
                {
                    width = Mathf.CeilToInt(size.x / scale);
                    height = Mathf.CeilToInt(size.y / scale);
                    x = pos.x;
                    y = Screen.height - pos.y;
                }
                else if (RuntimePlatform.IPhonePlayer == Application.platform)
                {
                    width = Mathf.CeilToInt(size.x / scale);
                    height = Mathf.CeilToInt(size.y / scale);
                    x = pos.x;
                    y = Screen.height - pos.y;
                }

                PlatformHelper.ShowWebView(url, x / resoultionRadio, y / resoultionRadio,
                    width / resoultionRadio, height / resoultionRadio);
            }
            catch (Exception e)
            {
                Logger.Error("show ann exception:{0}" , e);
                Destroy(gameObject);
                if (null != callback)
                {
                    callback();
                }
            }

        }
    }
}

