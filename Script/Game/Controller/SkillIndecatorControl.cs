using System;
using UnityEngine;
using System.Collections;

public class SkillIndecatorControl : MonoBehaviour
{
	[SerializeField]
	private float TotalTime = 0;
	[SerializeField]
	private float mElapseTime = 0;

	private Material mMat = null;
	public  Material Mat
	{
		get
		{
			if (null == mMat)
			{
				mMat = gameObject.renderer.material;
			}
			return mMat;
		}
	}
	
	void OnEnable () {
#if !UNITY_EDITOR
try
{
#endif

		mElapseTime = 0;
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	private float a;
	// Update is called once per frame
	void Update ()
	{
#if !UNITY_EDITOR
try
{
#endif

		mElapseTime += Time.deltaTime;
		if (null!=Mat)
		{
			var p = (TotalTime > 0) ? Mathf.Lerp(0, 1, mElapseTime / TotalTime) : 0;
			mMat.SetFloat("_FillPercent", p);
		}		
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	public void BeginCountDown(float t)
	{
		TotalTime = t;
		mElapseTime = 0;
	}
}
