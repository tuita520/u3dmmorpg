using System;
using UnityEngine;
using System.Collections;
using System.IO;
using EventSystem;

public class DownloadImageHandler : MonoBehaviour
{
    public UITexture texture;

    void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        EventDispatcher.Instance.AddEventListener(DownLoadImageEvent.EVENT_TYPE, DownLoad);
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    void DownLoad(IEvent evt)
    {
        var _e = evt as DownLoadImageEvent;
        if (_e.url != null)
        {
            texture.mainTexture = _e.url as Texture;
        }
    }

    void Destroy()
    {
        EventDispatcher.Instance.RemoveEventListener(DownLoadImageEvent.EVENT_TYPE, DownLoad);
    }
}
