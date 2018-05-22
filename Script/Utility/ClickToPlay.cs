using UnityEngine;
using System.Collections;

public class ClickToPlay : MonoBehaviour {

    public string EffectPrefabPath = "Effect/UI/UI_TuJianHeCheng.prefab";
    private GameObject effectObj;
    public int CustomRenderQueue = 6000;
    public UIWidget BackgroundWidget;

    void OnClick()
    {
        if (effectObj == null)
        {
            ResourceManager.PrepareResource<GameObject>
                (EffectPrefabPath, res =>
                {
                    if (gameObject == null)
                    {
                        Destroy(res);
                        return;
                    }
                    effectObj = NGUITools.AddChild(gameObject, res);
                }, true, true, true, false, true);

            var renderQueue = effectObj.GetComponent<ChangeRenderQueue>();
            if (null != renderQueue)
            {
                renderQueue.BackgroundWidget = BackgroundWidget;
                renderQueue.CustomRenderQueue = CustomRenderQueue;
            }
        }
        else
        {
            effectObj.SetActive(false);
            effectObj.SetActive(true);
        }
    }
}
