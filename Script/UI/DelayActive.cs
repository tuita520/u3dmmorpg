using System;
using UnityEngine;
using System.Collections;

public class DelayActive : MonoBehaviour {

    public GameObject target;
    public float time;
	// Use this for initialization
	void Start () {
#if !UNITY_EDITOR
        try
        {
#endif

            StartCoroutine(DelayToggle(time));

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

    private IEnumerator DelayToggle(float time)
    {
        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return null;
        }
        target.SetActive(true);
    }
}
