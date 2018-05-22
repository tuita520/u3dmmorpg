using System;
using UnityEngine;
using System.Collections;
using EventSystem;

public class FuBenScrollOne : MonoBehaviour {

    private UIScrollViewSimple scroll;
    Transform mTrans;
    void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

#if UNITY_EDITOR
        try
        {
#endif
            scroll = gameObject.GetComponent<UIScrollViewSimple>();

#if UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    void OnEnable()
    {
#if !UNITY_EDITOR
        try
        {
#endif

        EventDispatcher.Instance.AddEventListener(FuBenScrollOffestEvent.EVENT_TYPE, RefreshScrollist);

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public void RefreshScrollist(IEvent ieve)
    {
        var ie = ieve as FuBenScrollOffestEvent;
        mTrans = transform;
        Vector3 before = mTrans.localPosition;
        Vector3 after = Vector3.zero;
        if (ie.Index < 4)
        {
            after = new Vector3(0, 0, 0);
        }
        else
        {
            after = new Vector3(0, 465f, 0);
        }

        Vector3 offset = after - before;

        if (scroll != null)
        {
            scroll.MoveRelative(offset);
        }
    }
}
