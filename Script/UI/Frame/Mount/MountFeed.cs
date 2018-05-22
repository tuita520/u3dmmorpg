#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class MountFeed : MonoBehaviour
	{
	    public void OnClickClose()
	    {
            EventDispatcher.Instance.DispatchEvent(new MountClickBtn_Event(0));
	    }
	}
}