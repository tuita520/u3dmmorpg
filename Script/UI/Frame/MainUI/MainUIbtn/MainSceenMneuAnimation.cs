using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainSceenMneuAnimation : MonoBehaviour
{

	public float Offset = -75;
	public float DurationTime = 0.2f;
	public float Delay = 0.01f;

	List<Transform> BtnList = new List<Transform>();
	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

		for (int i = gameObject.transform.childCount - 1;i>=0; i--)
		{
			BtnList.Add(gameObject.transform.GetChild(i));	
		}
		
	
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

	public bool IsButtonActive(Transform tf)
	{
		return tf.GetChild(0).active;
	}

	public void Open(bool immediately = false)
	{
		var pars = gameObject.GetComponentsInChildren<ParticleSystem>(true);
		foreach (var par in pars)
		{
			par.gameObject.SetActive(true);
		}

		int i = 0;

		if (immediately)
		{
			i = 0;
			foreach (var btn in BtnList)
			{
				if (!IsButtonActive(btn))
				{
					continue;
				}

				btn.localPosition = new Vector3(i*Offset,0,0);
				var widget = btn.GetComponent<UIWidget>();
				if (null != widget)
				{
					widget.alpha = 1;
				}
				i++;
			}

			return;
		}

		int total = 0;
		foreach (var btn in BtnList)
		{
			if (!IsButtonActive(btn))
			{
				continue;
			}
			total++;
		}

		i = 0;
		foreach (var btn in BtnList)
		{
			if (!IsButtonActive(btn))
			{
				continue;
			}

			{
				var tween = btn.GetComponent<TweenPosition>();
				if (null == tween)
				{
					tween = btn.gameObject.AddComponent<TweenPosition>();
				}
				//tweenPos.from = new Vector3(Offset * i, 0, 0);
				tween.from = btn.localPosition;
				tween.to = new Vector3(Offset * i, 0, 0);

				tween.duration = DurationTime;
				//tween.duration = AniDuration;
				tween.delay = i * Delay;
				tween.ResetToBeginning();
				tween.Play();
			}

			{
				var tween = btn.GetComponent<TweenAlpha>();
				if (null == tween)
				{
					tween = btn.gameObject.AddComponent<TweenAlpha>();
				}
				tween.from = tween.value;
				tween.to = 1;
				tween.duration = DurationTime;
				//tween.duration = AniDuration;
				tween.delay = i * Delay;
				tween.ResetToBeginning();
				tween.Play();
			}



			i++;
		}

	}

	public void Close(bool immediately = false)
	{
		var pars = gameObject.GetComponentsInChildren<ParticleSystem>(true);
		foreach (var par in pars)
		{
			par.gameObject.SetActive(false);
		}

// 		var crq = gameObject.GetComponentInChildren<ChangeRenderQueue>();
// 		crq.gameObject.SetActive(true);

		int i = 0;

		if (immediately)
		{
			i = 0;
			foreach (var btn in BtnList)
			{
				if (!IsButtonActive(btn))
				{
					continue;
				}

				btn.localPosition = Vector3.zero;
				var widget = btn.GetComponent<UIWidget>();
				if (null != widget)
				{
					widget.alpha = 0;
				}
				i++;
			}

			return;
		}

		int total = 0;
		foreach (var btn in BtnList)
		{
			if (!IsButtonActive(btn))
			{
				continue;
			}
			total++;
		}

		i = 0;
		foreach (var btn in BtnList)
		{
			if (!IsButtonActive(btn))
			{
				continue;
			}

			{
				var tween = btn.GetComponent<TweenPosition>();
				if (null == tween)
				{
					tween = btn.gameObject.AddComponent<TweenPosition>();
				}
				//tween.from = Vector3.zero;
				tween.from = btn.localPosition;
				tween.to = new Vector3((i-1)*Offset , 0, 0);
				tween.duration = DurationTime;
				//tween.duration = AniDuration;
				tween.delay = i * Delay;
				tween.ResetToBeginning();
				tween.Play();
			}


			{
				var tween = btn.GetComponent<TweenAlpha>();
				if (null == tween)
				{
					tween = btn.gameObject.AddComponent<TweenAlpha>();
				}
				tween.from = tween.value;
				tween.to = 0;
				tween.duration = DurationTime;
				//tween.duration = AniDuration;
				//tween.delay = 0;
				tween.delay = i * Delay;
				tween.ResetToBeginning();
				tween.Play();
			}
			i++;
		}
		
	}
}
