using EventSystem;
using System;
#region using

using ClientDataModel;
using UnityEngine;

#endregion

namespace GameUI
{
    public class ElfSkillInfoCell : MonoBehaviour
    {
        public ListItemLogic ItemLogic;

        public void OnClickIcon()
        {
            var data = ItemLogic.Item as ElfSkillInfoDataModel;
            if (data != null)
            {
                data.IsSelect = true;
                EventDispatcher.Instance.DispatchEvent(new ElfSkillInfoCell_SelectEvent(ItemLogic.Index));
                //if (data.ItemId != -1)
                //{
                //    GameUtils.ShowItemIdTip(data.ItemId);
                //}
            }
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
    }
}
