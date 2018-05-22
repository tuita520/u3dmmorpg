using System;
using EventSystem;
using UnityEngine;


public class NewOfflineExpFrame : MonoBehaviour
{
	public BindDataRoot Binding;
	void Start ()
	{
#if !UNITY_EDITOR
try
{
#endif

        var controllerBase = UIManager.Instance.GetController(UIConfig.NewOfflineExpFrame);
        if (controllerBase == null) return;
        Binding.SetBindDataSource(controllerBase.GetDataModel(""));
	
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

    public void OnGetClick()
    {
        var iEvent = new OnOfflineExpCloses_Event();
        EventDispatcher.Instance.DispatchEvent(iEvent);
    }

    public void OnClickSingle()
    {
        EventDispatcher.Instance.DispatchEvent(new ChangeOfflineTypeEvent(1));
    }

    public void OnClickDouble()
    {
        EventDispatcher.Instance.DispatchEvent(new ChangeOfflineTypeEvent(2));
    }

}