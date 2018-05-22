#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class MountCell : MonoBehaviour
	{
	    public ListItemLogic ListItem;
	
	    public void OnClickCell()
	    {
            var dateModel = ListItem.Item as MountItemDataModel;
	        if (dateModel.IsSelect == false)
	        {
	            if (dateModel.IsOpen == 0)
	            {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(274033));
	                return;
	            }
                EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(20, dateModel.MountId));	            
	        }

        }
	}
}