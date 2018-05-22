using System;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;


namespace GameUI
{
    public class TeamTargetChangeItemCell : MonoBehaviour
    {

        public ListItemLogic ItemLogic;

        public void OnCliclMenuCell()
        {
            if (ItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new TeamTargetChangeItemCellClick_Event(ItemLogic.Index));
            }
        }

        public void OnClickSubBranchCell()
        {

            if (ItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new TeamTargetChangeItemCellClick_Event(ItemLogic.Index));
            }
        }
    }
}