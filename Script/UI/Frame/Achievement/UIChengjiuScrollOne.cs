using System;
using UnityEngine;
using System.Collections;
using EventSystem;

public class UIChengjiuScrollOne : MonoBehaviour {

    private UIScrollView scroll;
    Transform mTrans;
    public float upY = 0;
    public float downY = 0;
    void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        scroll = gameObject.GetComponent<UIScrollView>();
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
	void Start ()
    {
#if !UNITY_EDITOR
try
{
#endif

        EventDispatcher.Instance.AddEventListener(AchienentScrollOffestEvent.EVENT_TYPE, RefreshScrollist);        
	
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
        var ie = ieve as AchienentScrollOffestEvent;
        mTrans = transform;
        Vector3 before = mTrans.localPosition;
        Vector3 after = Vector3.zero;
        if (ie.TypeId < 6)
        {
            after = new Vector3(0, upY, 0);
        }
        else
        {
            after = new Vector3(0, downY, 0);
        }
        
        Vector3 offset = after - before;

        if (scroll != null)
        {
            scroll.MoveRelative(offset);
        } 
    }
}
