using System;
using UnityEngine;
using System.Collections;

public class ChangeColorEffect : MonoBehaviour 
{

    private int i = 0;
    private Color[] ColorArr = { Color.black, Color.blue, Color.green, Color.red };
    private float ChangeColorTime = 0.5f;
    private UILabel EnterVoiceEnterTipLabel;

    private void Start() 
    {
#if !UNITY_EDITOR
try
{
#endif

        if (EnterVoiceEnterTipLabel == null) 
        {
            EnterVoiceEnterTipLabel = gameObject.GetComponent<UILabel>();
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

        ChangeColor();
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    private void ChangeColor()
    {
        if (EnterVoiceEnterTipLabel == null) return;

        ChangeColorTime += Time.deltaTime;
        if (ChangeColorTime > 0.5f * i)
        {
            if (i <= ColorArr.Length - 1)
            {
                EnterVoiceEnterTipLabel.color = ColorArr[i];
            }
            else
            {
                i = 0;
                ChangeColorTime = 0;
                EnterVoiceEnterTipLabel.color = ColorArr[i];
            }
            i++;
        }
    }
	
}
