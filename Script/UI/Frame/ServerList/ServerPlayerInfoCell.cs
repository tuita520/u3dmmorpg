using UnityEngine;
using System.Collections;
using EventSystem;


namespace GameUI
{
    public class ServerPlayerInfoCell : MonoBehaviour
    {
        private void OnClick()
        {
            var listItem = GetComponent<ListItemLogic>();

            var e = new Event_ServerPlayerCellClick(listItem.Item);
            EventDispatcher.Instance.DispatchEvent(e);
        }
    }
}