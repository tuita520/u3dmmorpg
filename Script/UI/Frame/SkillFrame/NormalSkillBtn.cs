using System;
#region using

using System.Collections;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class NormalSkillBtn : MonoBehaviour
	{
	    //技能cd文字
	    public UILabel CdLabel;

        private float mLastRealTime = 0.0f;
	    public int CD = 30;
	    private float mCDTimer = 0.0f;

	    public void OnClickSkill()
	    {
	        if (mCDTimer <= 0)
	        {
	            setCD(CD);
                EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnSummonMonster());
            }
	    }
	    private void Start()
	    {
#if !UNITY_EDITOR
try
{
#endif

	        CdLabel.gameObject.SetActive(false);
	        mLastRealTime = Time.realtimeSinceStartup;
	    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	    private void setCD(float cd)
	    {
	        mCDTimer = cd;
            mLastRealTime = Time.realtimeSinceStartup;
	        CdLabel.gameObject.SetActive(true);
            CdLabel.text = ((int)mCDTimer).ToString();
        }
	    public void Update()
	    {
#if !UNITY_EDITOR
try
{
#endif

	        if (mCDTimer <= 0.0f)
	            return;
	        mCDTimer -= (Time.realtimeSinceStartup - mLastRealTime);
            mLastRealTime = Time.realtimeSinceStartup;
	        CdLabel.text = ((int) mCDTimer).ToString();
	        if (mCDTimer <= 0.0f)
	        {
                CdLabel.gameObject.SetActive(false);    
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
}