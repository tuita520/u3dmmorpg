using UnityEngine;
using System.Collections;
using GameUI;
using ClientDataModel;
using EventSystem;

public class SuitPartCell : MonoBehaviour {

    void OnClick()
    {
        var root = GetComponent<BindDataRoot>();
        if (null == root) return;

        var data = root.Source as ItemIdDataModel;
        if (data != null)
        {
            if (data.ItemId != -1)
            {
                var strongController = UIManager.Instance.GetController(UIConfig.NewStrongUI);
                var dataModel = strongController.GetDataModel("") as NewStrongDataModel;
                if (null != dataModel)
                {
                    GameUtils.ShowItemIdTip(data.ItemId, -1, dataModel.CurrentSelectSuit.EnchantLevel);
                }
            }
        }
    }
}
