using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BehaviourMachine;

public class Delay : MonoBehaviour {
	
	public float delayTime = 1.0f;

#if UNITY_EDITOR
	void Start()
	{
#if !UNITY_EDITOR
try
{
#endif

		StartDelay ();
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
#endif
    public void StartDelay()
    { 
        ComplexObjectPool.SetActive(gameObject, false);
        Invoke("DelayFunc", delayTime);
    }

    private void DelayFunc()
    {
        ComplexObjectPool.SetActive(gameObject, true);
    }

}
