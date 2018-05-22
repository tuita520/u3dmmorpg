using System;
#region using

using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class GetEquipCell : MonoBehaviour
    {
        public ListItemLogic ItemLogic;
        public void OnClickIcon()
        {
            if (ItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new Event_EquipInfoClick(ItemLogic.Index));
            }
        }
    }
}