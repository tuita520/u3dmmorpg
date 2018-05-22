using UnityEngine;
using System.Collections;
using DataContract;
using EventSystem;
using System;


public class NewFunctionAnimation : MonoBehaviour {
	public Transform PanelRoot;
	public float DelayTime = 1.2f;
	public float MoveTime = 1.2f;
	public float StayTime = 0.6f;
	public Transform EffectlRoot;
	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

	PanelRoot.gameObject.SetActive(false);
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

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

	void OnDisable()
	{
#if !UNITY_EDITOR
try
{
#endif

		PanelRoot.gameObject.SetActive(false);
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
	void OnDestroy()
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


	public void ShowNewFunction(string btnName, Transform tm, Action call = null)
	{
		var spr = tm.GetComponent<UISprite>();
		var root = spr.root;

		root.StartCoroutine(MoveToPos(spr, call));
	}

	IEnumerator MoveToPos(UISprite spr, Action call = null)
	{

		PanelRoot.gameObject.SetActive(true);

		//new a icon copy
		var go = new GameObject();
		var spr1 = go.AddComponent<UISprite>();
		spr1.atlas = spr.atlas;
		spr1.spriteName = spr.spriteName;
		spr1.width = spr.width;
		spr1.height = spr.height;
	    spr1.depth = spr.depth + 500;

		//attach
		go.transform.parent = EffectlRoot;
		go.transform.localPosition = Vector3.zero;
		go.transform.localScale = Vector3.one;

		EffectlRoot.localPosition = Vector3.zero;

		//delay
		yield return new WaitForSeconds(DelayTime);



		//destination
		var pos = transform.root.InverseTransformPoint(spr.transform.position);

		//move
		Vector3 start = EffectlRoot.localPosition;
		float elapse = 0;
		while (elapse < MoveTime)
		{
			elapse += Time.deltaTime;

			EffectlRoot.localPosition = Vector3.Lerp(start, pos, elapse / MoveTime);
			yield return null;
		}

		//stay
		yield return new WaitForSeconds(StayTime);

		//destroy
		PanelRoot.gameObject.SetActive(false);
		GameObject.Destroy(go);

		EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(0));

		if (null != call)
		{
			call();
		}
	}

}
