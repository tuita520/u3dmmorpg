using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptController;
using ClientDataModel;
using DataContract;
using DataTable;
using EventSystem;
using Shared;
using UnityEngine;

public class ResultStarLogic : MonoBehaviour {
    public GameObject[] objList;
    public GameObject effect;
    
    public void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        for (int i = 0; i < objList.Length; i++)
        {
            if (objList[i] != null)
            {
                objList[i].SetActive(false);
            }
        }
        effect.SetActive(false);
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    public void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        var ctr = UIManager.GetInstance().GetController(UIConfig.DungeonResult) as CachotOutcomeFrameCtrler;
        if (ctr == null)
            return;
        NetManager.Instance.StartCoroutine(ShowStarCorount((int)ctr.CallFromOtherClass("GetStarNum", null)));
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}


    private IEnumerator ShowStarCorount(int num)
    {
        int i = 0 ;
        yield return new WaitForSeconds(0.2f);
        while(num > 0 && i<objList.Length)
        {
            if (!effect)
                break;
            effect.SetActive(false);
            if (!objList[i])
                break;
            objList[i].SetActive(false);
            effect.transform.parent = objList[i].transform.parent;
            effect.transform.localPosition = objList[i].transform.localPosition;
            effect.SetActive(true);
            yield return new WaitForSeconds(0.4f);
            if(!objList[i])
                break;
            objList[i].SetActive(true);
            if(!effect)
                break;
            effect.SetActive(false);
            i++ ;
            num--;
        }
    }

}
