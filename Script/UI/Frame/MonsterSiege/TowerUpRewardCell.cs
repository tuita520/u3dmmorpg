using System;
using UnityEngine;
using System.Collections;
using DataTable;
using EventSystem;

public class TowerUpRewardCell : MonoBehaviour
{

    public UISprite mIcon;
    public UISprite mLingqu;
    public UISprite mBorder;
    public GameObject mEffect;
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

    public void SetLingqu(bool b)
    {
        mLingqu.gameObject.SetActive(b);
        if(b==true)
            mEffect.SetActive(false);
    }

    public void SetEffect(bool b)
    {
        mEffect.SetActive(b);
    }
    public void SetIcon(ItemBaseRecord tb)
    {
        var tbIcon = Table.GetIcon(tb.Icon);
        if (tbIcon != null)
        {
            //mIcon.atlas.name = tbIcon.Atlas;
            mIcon.spriteName = tbIcon.Sprite;
            mBorder.spriteName = string.Format("icon_{0}", tb.Color);
        }
    }
}
