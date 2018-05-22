using System;
using UnityEngine;
using System.Collections;
using ClientDataModel;
using EventSystem;

public class DepotCleanUpCell : MonoBehaviour
{
    private ListItemLogic itemList;
    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        itemList = gameObject.GetComponent<ListItemLogic>();
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    public void OnItemClick()
    {
        var itemData = itemList.Item as BattleUnionDepotClearUpDataModel;
        if (null == itemData)
        {
            return;
        }
        var e = new BattleUnionDepotCleanUpToggleEvent();
        e.Ladder = itemData.Ladder;
        e.Quality = itemData.Quality;
        e.Num = itemData.Num;
        e.Index = itemList.Index;
        EventDispatcher.Instance.DispatchEvent(e);
    }
}
