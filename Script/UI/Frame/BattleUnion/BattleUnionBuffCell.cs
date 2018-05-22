using UnityEngine;
using System.Collections;
using EventSystem;
using ClientDataModel;
using System;

public class BattleUnionBuffCell : MonoBehaviour {

    public BattleUnionBuffItemDataModel ItemData;
    private GameObject BuffUpEffect;
    private Coroutine coroutine;
	// Use this for initialization
	void Start () {
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
	
	// Update is called once per frame
	void Update () {
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
    void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        EventDispatcher.Instance.AddEventListener(BattleUnionBuffUp.EVENT_TYPE,OnUpBattleUnionBuffEvent);

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
    void OnDisable()
    {
#if !UNITY_EDITOR
try
{
#endif
        EventDispatcher.Instance.RemoveEventListener(BattleUnionBuffUp.EVENT_TYPE, OnUpBattleUnionBuffEvent);

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
    private void OnUpBattleUnionBuffEvent(IEvent ievent)
    {
        var e = ievent as BattleUnionBuffUp;
        if (ItemData.BuffID == e.BuffId)
        {
            ResourceManager.PrepareResource<GameObject>
                ("Effect/UI/JiNengShengJi.prefab", res =>
                {
                    if (BuffUpEffect)
                    {
                        NGUITools.Destroy(BuffUpEffect);
                        if (null != coroutine)
                        {
                            ResourceManager.Instance.StopCoroutine(coroutine);
                        }
                    }
                    BuffUpEffect = NGUITools.AddChild(gameObject,res);
                    coroutine = ResourceManager.Instance.StartCoroutine(DestroyEffectUp());
                });
        }
    }

    private IEnumerator DestroyEffectUp()
    {
        yield return new WaitForSeconds(1);
        if (BuffUpEffect)
        {
            NGUITools.Destroy(BuffUpEffect);
            BuffUpEffect = null;
        }
        coroutine = null;
    }
}
