using System;
using DataTable;
using EventSystem;
using UnityEngine;


public class MessageBoxExLogic : MonoBehaviour
{
	private BindDataRoot Binding;
	void Start ()
	{
#if !UNITY_EDITOR
try
{
#endif

	    Binding = gameObject.GetComponent<BindDataRoot>();
        if (Binding == null)
        {
            return;
        }
		var controllerBase = UIManager.Instance.GetController(UIConfig.MessageBoxEx);
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

    public void OnClickExitDungeon()
    {
        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MessageBoxEx));

        var e = new ExitFuBenWithOutMessageBoxEvent();
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void OnClickBuyTili()
    {
		var logic = GameLogic.Instance;
		if (logic == null)
		{
			return;
		}
		var scene = logic.Scene;
		if (scene == null)
		{
			return;
		}
		var tbScene = Table.GetScene(scene.SceneTypeId);
		if (tbScene == null)
		{
			return;
		}
	    if (21000 == tbScene.Id)
	    {
			EventDispatcher.Instance.DispatchEvent(new UIAcientBattleFieldOperationClickEvent(1));
	    }
	    else
	    {
			EventDispatcher.Instance.DispatchEvent(new OnClickBuyTiliEvent(1));    
	    }
        
    }
}