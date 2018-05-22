using System;
#region using

using UnityEngine;

#endregion

public class FullScreenBg : MonoBehaviour
{
    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif
	    var tex = GetComponent<UITexture>();
	    if (null != tex)
	    {
			var UiRoot = tex.root;
			var s = (float)UiRoot.activeHeight / Screen.height;
			tex.height = Mathf.CeilToInt(Screen.height * s);
#if UNITY_IOS
			//iOS用的PVR压缩图都是方的，硬编码
		    if (Screen.width*s/tex.width < 0.67f)
		    {
				tex.width = Mathf.CeilToInt(Screen.width * s * 5 / 4);
		    }
		    else
		    {
				tex.width = Mathf.CeilToInt(Screen.width * s * 4 / 3);
		    }
			
#else
			var des = Mathf.CeilToInt(Screen.width * s);
			var ratio = tex.height * 1.0f / tex.mainTexture.height;
			tex.width = (int)(tex.mainTexture.width * ratio);

			//如果宽度超出去的部分占原图的1/3，就把宽度缩小一些
			if (Screen.width * s / tex.width < 0.67f)
			{
				tex.width = (int)(tex.width * 0.8f);
			}
		    if (tex.width < des)
		    {
			    tex.width = des;
		    }
#endif
		}
	    else
	    {
			var BackGround = GetComponent<UIWidget>();
			var UiRoot = BackGround.root;
			var height = 1024;
			var width = 2048;
			var s = Screen.height / (float)UiRoot.activeHeight;
			BackGround.height = height;
			BackGround.width = width;
			BackGround.transform.localScale = Vector3.one * (UiRoot.activeHeight / 640.0f * 0.625f);
	    }

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    private void Update()
    {
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
}