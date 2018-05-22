using System;
using UnityEngine;
using System.Collections;

public class testShow : MonoBehaviour {

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

    void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        int a = 0;
        a++;
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    void OnDestroy()
    {
#if !UNITY_EDITOR
try
{
#endif

        int a = 0;
        a++;

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
    void OnDisable()
    {
#if !UNITY_EDITOR
try
{
#endif

        int a = 0;
        a++;        
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

}
