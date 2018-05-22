using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EventSystem;
using TB.ComponentModel;

public class GainItemTipManager : MonoBehaviour
{
	[Range(0.2f,2)]
	public float IntervalTime = 0.8f;
	public class GainItemInfo
	{
		public int ItemId;
		public int Count;
	}

	public static GainItemTipManager Instance;
	private Queue<GainItemInfo> ItemQueue = new Queue<GainItemInfo>();

	public bool IsWorking
	{
		get;
		private set;
	}

	public void Init()
	{
		EventDispatcher.Instance.AddEventListener(GainNewItem_Event.EVENT_TYPE, AddInfo_Event);
		StartCoroutine(ProcessInfo());
		IsWorking = true;
	}

	public void Cleanup()
	{
		ItemQueue.Clear();
		StopCoroutine(ProcessInfo());
		EventDispatcher.Instance.RemoveEventListener(GainNewItem_Event.EVENT_TYPE, AddInfo_Event);
		var tf = gameObject.transform;
		for (int i = 0; i < tf.childCount; i++)
		{
			GameObject.Destroy(tf.GetChild(i).gameObject);
		}
		IsWorking = false;
	}

	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

		Instance = this;
	
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

		Instance = null;
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	private void AddInfo_Event(IEvent ievent)
	{
		var e = ievent as GainNewItem_Event;
		PushItemInfo(e.ItemId, e.Count);
	}

	IEnumerator ProcessInfo()
	{
		while (true)
		{
			if(ItemQueue.Count > 0)
			{
				var info = ItemQueue.Dequeue();
				ShowItemTip(info.ItemId, info.Count);
				yield return new WaitForSeconds(IntervalTime);
			}

			yield return new WaitForSeconds(0.1f);
		}
	}

	public void PushItemInfo(int itemId, int count)
	{
		var info = new GainItemInfo();
		info.ItemId = itemId;
		info.Count = count;
		ItemQueue.Enqueue(info);
	}

	private void ShowItemTip(int itemId,int count)
	{
		ComplexObjectPool.NewObject("UI/Hint/ItemTip", go =>
		{
			if (null == Instance)
			{
				ComplexObjectPool.Release(go);
				return;
			}

			if (false == IsWorking)
			{
				ComplexObjectPool.Release(go);
				return;
			}
			go.transform.parent = gameObject.GetComponent<UIPanel>().transform;
			go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(0.8f,0.8f,0.8f);
			go.transform.localRotation = Quaternion.identity;
            go.SetActive(false);
            go.SetActive(true);
            var Logic = go.GetComponent<GainItemCtrler>();
            Logic.SetItemIcon(itemId,count);
        });
	}
	[ContextMenu("test")]
	public void Test()
	{
		PushItemInfo(1, 1);
	}
}
