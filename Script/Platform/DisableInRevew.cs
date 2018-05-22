using System;
using UnityEngine;
using System.Collections;
using BehaviourMachine;
using ScriptManager;

public class DisableInRevew : MonoBehaviour 
{

    void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (gameObject.activeSelf && GameSetting.Instance.ReviewState == 1)
        {
            gameObject.SetActive(false);
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

}
