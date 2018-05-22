using UnityEngine;
using System.Collections;


/// <summary>
/// 使用这个脚本可以控制了这个脚本的孩子的RenderQueue
/// 如果BackgroundWidget不为空，则会以BackgroundWidget为准，如果BackgroundWidget的SkippedRenderQueue不是0，那就是希望孩子卡再两层UI的中间，如果是0，那孩子就会在这个节点的上面（通常适用于特效）
/// 如果BackgroundWidget为空，那所有孩子节点的RenderQueue都会被设置为CustomRenderQueue，这种情况也不是很常见
/// *******************注意*******************
/// 如果设置了CustomRenderQueue，没有相应的SkippedRenderQueue配合，是由可能出问题的
/// </summary>
public class ChangeRenderQueue : MonoBehaviour {

    public UIWidget BackgroundWidget;
    public int CustomRenderQueue = 3025;
    public float Time = 0.0f;

    public void OnEnable()
    {
        if (Time > 0.0f)
        {
            ResourceManager.Instance.StartCoroutine(OnComplete());
        }
    }

    private IEnumerator OnComplete()
    {
        yield return new WaitForSeconds(0.6f);
        if(gameObject)
            gameObject.SetActive(false);
    }

    void Awake()
    {
        if (BackgroundWidget)
        {
#if UNITY_EDITOR
            if (BackgroundWidget.GetType() == typeof(UIWidget))
            {
                Debug.LogError("将不可绘制的Widget作为BackgroudWidget是不会生效的，换个别的吧。");
            }
#endif

            BackgroundWidget.onRender = mat =>
            {
                UpdateRenderQueue();
                BackgroundWidget.onRender = null;
            };
        }
        else
        {
            gameObject.SetRenderQueue(CustomRenderQueue);
        }
    }

    void UpdateRenderQueue()
    {
        var customRenderQueue = CustomRenderQueue;
        if (null != BackgroundWidget)
        {
            if (null != BackgroundWidget.drawCall)
            {
                if (BackgroundWidget.SkippedRenderQueue == 0)
                {
                    //Logger.Warn("将 {0} 的SkippedRenderQueue 设置一下吧，否则有些特效或者模型没有办法在UI中正常显示，一般设50就可以了。", BackgroundWidget.gameObject.name);
                }

                customRenderQueue = BackgroundWidget.drawCall.renderQueue + 1;
                customRenderQueue += BackgroundWidget.SkippedRenderQueue / 2;
            }
        }
        else
        {
            customRenderQueue = CustomRenderQueue;
        }

        gameObject.SetRenderQueue(customRenderQueue);
    }

    public void RefleshRenderQueue()
    {
        Awake();
    }
}
