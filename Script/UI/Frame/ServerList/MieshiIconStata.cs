using System;
using UnityEngine;
using DataTable;

public class MieshiIconStata : MonoBehaviour {

    private int oldStata;
    public int stata
    {
        set {
            if (oldStata != value)
            {
                oldStata = value;
                if (target != null)
                {
                    if (oldStata == 0)
                    {
                        target.enabled = false;
                        target.spriteName = "";
                    }
                    else if (oldStata == 1)
                    {
                        target.enabled = true;
                        target.spriteName = Table.GetDictionary(300000010).Desc[0];
                    }
                    else if (oldStata == 2)
                    {
                        target.enabled = true;
                        target.spriteName = Table.GetDictionary(300000011).Desc[0];
                    }
                }
            }
        }
    }
    public UISprite target;
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
}
