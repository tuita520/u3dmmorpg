using System;
#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class WingQualityCell : MonoBehaviour
	{
	    private BindDataRoot root;

	    void Awake()
	    {
#if !UNITY_EDITOR
try
{
#endif

	        root = GetComponent<BindDataRoot>();
	    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
	    public void OnClickCell()
	    {
            var e = new WingQuailtyCellClick(root.Source as WingQualityData);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	}
}