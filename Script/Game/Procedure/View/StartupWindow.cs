#region using

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using DataTable;
using GameUI;
using UnityEngine;
using Debug = UnityEngine.Debug;

#endregion

public class StartupWindow : MonoBehaviour
{
    public GameObject BigUpdateTip;
    public UILabel CountLabel = null;
    public UILabel ErrorLabel;
    public GameObject ErrorTip;
    public UILabel ProgressLabel = null;
    public UILabel StatusLabel;
    public UILabel TotalSize;
    public GameObject UpdatePanel = null;
    public UISlider UpdateProgress = null;
    private string UpdateUrl;
    public GameObject WaitingTip;
    public GameObject WifiTip;

    public UILabel UpdateTip;
    public UILabel LateUpdateTip;
    public UILabel VersionLabel;

    public GameObject ResetTip;
    public UILabel ResetLabel;
	public float Percent;
	public UIProgressBar ProgressBar = null;

    private void Update()
    {
#if !UNITY_EDITOR
try
{
#endif
	    if (Percent >= 0.98)
	    {
		    ProgressBar.value = 1;
	    }
	    else
	    {
			ProgressBar.value = Mathf.Lerp(ProgressBar.value, Percent, Time.deltaTime);    
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
