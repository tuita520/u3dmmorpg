using System;
using UnityEngine;
using System.Collections;

public class PressShowChildren : MonoBehaviour {

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

    private void OnPress(bool isPressed)
    {
        var children = transform.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.gameObject != gameObject)
            {
                child.gameObject.SetActive(isPressed); 
            }
        }
    }
}
