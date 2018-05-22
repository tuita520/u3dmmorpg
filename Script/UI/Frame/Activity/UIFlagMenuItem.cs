using UnityEngine;
using System.Collections;
using EventSystem;
namespace GameUI
{
    public class UIFlagMenuItem : MonoBehaviour
    {
        public ListItemLogic ItemLogic;
        public void OnClickCell()
        {
            EventDispatcher.Instance.DispatchEvent(new FieldFlagMenuItemClickEvent(ItemLogic.Index));
        }
    }
}
