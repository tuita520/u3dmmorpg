using System;
using UnityEngine;
using System.Collections;
using EventSystem;
public class MieShiTapLogic : MonoBehaviour {

	// Use this for initialization
	void Start () {
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
	void Update () {
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

    public void OnCloseBtn()
    {
        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MieShiTapUI));
    }


}
