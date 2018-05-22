#region using

using System.Collections;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class HandBookHasCell : MonoBehaviour
    {
        public ListItemLogic ItemLogic;

        public void OnCellClick()
        {
            HandBookItemDataModel data = ItemLogic.Item as HandBookItemDataModel;
            if (data != null)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_OnClickHasCell(data.BookId));
            }
        }

    }
}