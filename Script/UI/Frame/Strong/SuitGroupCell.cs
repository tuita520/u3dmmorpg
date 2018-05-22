using UnityEngine;
using System.Collections;
using ClientDataModel;
using EventSystem;

public class SuitGroupCell : MonoBehaviour 
{
    void OnClick()
    {
        var logic = GetComponent<ListItemLogic>();
        if (null == logic) return;

        EventDispatcher.Instance.DispatchEvent(new UIEvent_NewStrongOperation(1, logic.Item));
    }

}
