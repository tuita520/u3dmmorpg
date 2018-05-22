using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataContract;
using DataTable;
using EventSystem;

public class NewStrongFrame2 : MonoBehaviour {


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
    private void OnEnable()
    {
#if !UNITY_EDITOR
	try
	{
#endif
        EventDispatcher.Instance.DispatchEvent(new UIEvent_NewStrongOperation(3));
        //EventDispatcher.Instance.DispatchEvent(new SuitShowModelEvent());

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
      

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
    }

}
