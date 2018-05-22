using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class ColorAdjustEffect : MonoBehaviour 
{
	
	public Material mMat;

 	public float Brightness = 1.1f;
// 	public float Saturation = 1;
	public float ContrastFactor = 1.08f;
	public  Material Mat
	{
		get
		{
			if(null==mMat)
			{
				try
				{
					mMat = new Material(Shader.Find("Scorpion/ColorAdjustEffect"));
				}
				catch (Exception e)
				{
					Logger.Error(e.Message);
				}
				
			}
			return mMat;
		}
	
	}
	bool CheckSupport()
	{
		return SystemInfo.supportsImageEffects && SystemInfo.supportsRenderTextures;
	}

	void OnEnable()
	{
#if !UNITY_EDITOR
try
{
#endif

		if(!CheckSupport())
		{
			enabled = false;
		}
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
	// Use this for initialization
	void Start () {
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

	void OnRenderImage(RenderTexture src,RenderTexture dest)
	{
		if(null==Mat)
		{
			enabled = false;
		}

	    if (GameLogic.Instance && GameLogic.Instance.Scene)
	    {
            Mat.SetColor("_Color", GameLogic.Instance.Scene.Color);
            Mat.SetFloat("_Brightness", GameLogic.Instance.Scene.Bright);
	    }
	    else
	    {
            Mat.SetColor("_Color", Color.white);
            Mat.SetFloat("_Brightness", 1);
	    }

	    Mat.SetFloat("_ContrastFactor", ContrastFactor);


		Graphics.Blit(src, dest, Mat);
	}
}
