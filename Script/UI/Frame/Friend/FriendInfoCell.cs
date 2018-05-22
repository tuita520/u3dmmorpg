
#region using

using System;
using ScriptManager;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
    public class FriendInfoCell : MonoBehaviour
    {
        public GameObject BtnMore;
        public ContactInfoDataModel data { get; set; }
        public void OnClickCell()
        {
            if (data == null)
                return;

            EventDispatcher.Instance.DispatchEvent(new FriendContactCell(data.CharacterId,0));
        }

        public void OnClickInfo()
        {
            if (data == null)
                return;

            var localPos = transform.root.InverseTransformPoint(transform.position);
            localPos.z = 0;
            UIConfig.OperationList.Loction = localPos;
            PlayerDataManager.Instance.ShowCharacterPopMenu(data.CharacterId, data.Name, 18, data.Level, data.Ladder,
                data.Type);
        }

        private void Start()
        {
#if !UNITY_EDITOR
            try
            {
#endif


#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void Update()
        {
#if !UNITY_EDITOR
            try
            {
#endif


#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }
    }
}
