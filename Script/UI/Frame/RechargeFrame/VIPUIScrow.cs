using UnityEngine;
using EventSystem;
using System.Collections;
using System;
using UnityEngine;

public class VIPUIScrow : MonoBehaviour {

    private Vector2 first = Vector2.zero;

    private Vector2 second = Vector2.zero;
    

    public void OnGUI()
    {

        if (UnityEngine.Event.current.type == EventType.MouseDown)
        {
            //记录鼠标按下的位置 

            first = UnityEngine.Event.current.mousePosition;

        }

        if (UnityEngine.Event.current.type == EventType.MouseUp)
        {
            //记录鼠标拖动的位置 

            second = UnityEngine.Event.current.mousePosition;

            if (second.x < first.x)
            {
                //响应向左事件 
                var e = new UIEvent_RechargeFrame_OnClick(5);
                e.exData = 1;
                EventDispatcher.Instance.DispatchEvent(e);
            }

            if (second.x > first.x)
            {
                //响应向右事件
                var e = new UIEvent_RechargeFrame_OnClick(5);
                e.exData = -1;
                EventDispatcher.Instance.DispatchEvent(e);
            }

            first = second;

        }

    }  
}
