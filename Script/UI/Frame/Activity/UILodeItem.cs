using UnityEngine;
using System.Collections;
using EventSystem;
using DataTable;
using ClientDataModel;
namespace GameUI
{
    public class UILodeItem : MonoBehaviour
    {
        public int Index;
        public void OnClickCell()
        {
            EventDispatcher.Instance.DispatchEvent(new LodeItemClickEvent(Index));
        }
	}
}

