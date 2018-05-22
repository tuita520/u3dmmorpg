using EventSystem;
using UnityEngine;

#region using

#endregion


namespace ScriptController
{
    public class UIBossMenuItem : MonoBehaviour
    {
        public int index;
        public void OnClick()
        {
            EventDispatcher.Instance.DispatchEvent(new UIBossHomeClickEvent(index));
        }
    }
}
