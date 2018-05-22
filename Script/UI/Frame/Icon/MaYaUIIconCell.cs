using UnityEngine;
using System.Collections;
using System;
using EventSystem;
using ClientDataModel;

public class MaYaUIIconCell : MonoBehaviour
{

    public ListItemLogic ItemLogic;

    public void OnClickIcon()
    {
        var data = ItemLogic.Item as ItemIdDataModel;
        if (data != null)
        {
            if (data.ItemId != -1)
            {
                EventDispatcher.Instance.DispatchEvent(new UpdateMaYaUIModelEvent(ItemLogic.Index));
            }
        }
    }
}
