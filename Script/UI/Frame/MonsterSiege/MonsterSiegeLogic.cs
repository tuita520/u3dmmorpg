using System;
using ClientDataModel;
using UnityEngine;
using DataTable;
using EventSystem;

public class MonsterSiegeLogic : MonoBehaviour
{

    void OnEnable()
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
