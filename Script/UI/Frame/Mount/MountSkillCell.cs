#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class MountSkillCell : MonoBehaviour
	{
	    public ListItemLogic item;

	    public void OnClickCell()
	    {
	        MountSkillDataModel data = item.Item as MountSkillDataModel;
	        if (data != null)
	        {
                EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(30, data.MountSkillId));	            
	        }

	    }
	}
}