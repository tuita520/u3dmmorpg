using UnityEngine;
using System.Collections;
using DataTable;

public class GainItemCtrler : MonoBehaviour {


    public UISprite ItemIcon;
    public UISprite ItemBorder;
    public UILabel ItemCount;

    public void SetItemIcon(int itemid,int count)
    {
        var ani = gameObject.GetComponentInChildren<UIPlayAnimation>();
        ani.Play(true, false);
        var ta = gameObject.GetComponentInChildren<TweenAlpha>();
        ta.ResetToBeginning();
        ta.PlayForward();
        var _tbItem = Table.GetItemBase(itemid);
        GameUtils.SetSpriteIcon(ItemIcon,_tbItem.Icon);
        if (count == 1)
        {
            ItemCount.text = "";
        }
        else
        {
            ItemCount.text = count.ToString();
        }
        ItemBorder.spriteName = string.Format("icon_{0}",_tbItem.Color);
    }
    public void OnFinishedOperation()
    {
        ComplexObjectPool.Release(gameObject);
    }
}
