#region using

using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class FuBenGroupCell : MonoBehaviour
	{
	    public ListItemLogic ItemLogic;
	
	    public void OnClickCell()
	    {	        
	        EventDispatcher.Instance.DispatchEvent( new DungeonGroupCellClick(ItemLogic.Index));
	    }
	}
}