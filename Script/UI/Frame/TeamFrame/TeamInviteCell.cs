using System;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

namespace GameUI
{
    public class TeamInviteCell : MonoBehaviour
    {

        public ListItemLogic ItemLogic;

        public void OnCliclMenuCell()
        {
            if (ItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new TeamInviteClickCell_Event(ItemLogic.Index));
            }
        }
    }
}
