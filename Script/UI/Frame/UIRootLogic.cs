using System;
#region using

using UnityEngine;

#endregion

public class UIRootLogic : MonoBehaviour
{
	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

		GameObject.DontDestroyOnLoad(gameObject);
		UIManager.Instance.SetUIRoot(gameObject, gameObject.GetComponentInChildren<Camera>());
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

}