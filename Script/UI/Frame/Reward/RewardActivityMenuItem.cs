using UnityEngine;
using System.Collections;
using EventSystem;
using ClientDataModel;

namespace GameUI
{
    public class RewardActivityMenuItem : MonoBehaviour
    {

        public int item;
        public void OnClick()
        {
            EventDispatcher.Instance.DispatchEvent(new RewardActivityItemClickEvent(item));
        }

    }
}

