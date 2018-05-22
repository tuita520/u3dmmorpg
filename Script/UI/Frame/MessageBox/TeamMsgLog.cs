using System;
using EventSystem;
using UnityEngine;
using System.Collections;


public class TeamMsgLog : MonoBehaviour {
    public ListItemLogic itemLogic;
    public GameObject okBtn;
    public GameObject agreedBtn;
    public BindDataRoot BindRoot;


	// Use this for initialization
	void OnEnable () 
    {
#if !UNITY_EDITOR
try
{
#endif

        okBtn.gameObject.SetActive(true);
        agreedBtn.gameObject.SetActive(false); 
	
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

    public void OnClickAgreen()
    {
        okBtn.gameObject.SetActive(false);
        agreedBtn.gameObject.SetActive(true);

        var e = new TeamApplyItemCellClick_Event();
        e.index = itemLogic.Index;
        EventDispatcher.Instance.DispatchEvent(e);
    }

    private void OnDisable()
    {
#if !UNITY_EDITOR
	try
	{
#endif

        BindRoot.RemoveBinding();

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
    }
}
