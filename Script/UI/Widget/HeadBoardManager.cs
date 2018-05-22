using System;
using UnityEngine;
using System.Collections;
using EventSystem;
using ScriptManager;

public class HeadBoardManager : MonoBehaviour
{

    public static HeadBoardManager Instance;
    public bool IsWorking
    {
        get;
        private set;
    }
    void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        Instance = this;
        IsWorking = false;

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
    // Use this for initialization


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

    public void Init()
    {
        IsWorking = true;
        if (PlayerDataManager.Instance.BattleMishiMaster != null)
        {
            var e = new BattleMishiRefreshModelMaster(PlayerDataManager.Instance.BattleMishiMaster);
            EventDispatcher.Instance.DispatchEvent(e);
        }
    }
    public void Cleanup()
    {
        IsWorking = false;
    }
}
