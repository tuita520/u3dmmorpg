using System.Collections;
using System;
#region using

using DataTable;
using EventSystem;
using UnityEngine;
using ScriptManager;
#endregion

public class Transfer : MonoBehaviour
{
    private const float TIME_INTERVAL = 5.0f;
    private float mLastTeleportTime;
    private TransferRecord mTableData;
    public int TransferId = -1;

    //当进入触发器时触发切换场景事件
    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - mLastTeleportTime < TIME_INTERVAL)
        {
            return;
        }

        if (null == ObjManager.Instance.MyPlayer)
        {
            return;
        }

        if (other.gameObject != ObjManager.Instance.MyPlayer.gameObject)
        {
            return;
        }

        if (null == mTableData)
        {
            Logger.Warn("null==Table.GetTransfer({0})", TransferId);
            return;
        }


        var tableScene = Table.GetScene(mTableData.ToSceneId);
        if (null == tableScene)
        {
            Logger.Warn("null==Table.GetScene({0})", mTableData.ToSceneId);
            return;
        }

        if (ObjManager.Instance.MyPlayer.GetLevel() < tableScene.LevelLimit)
        {
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210207));
            return;
        }
        if (tableScene.IsPublic == 0)
        {
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005011));
            return;
        }
        //飞鞋和传送阵冲突，上一次飞鞋传送或传送点传送和这次间隔小于1s不提交请求（bug号：5329）
        var timeDiff = (Game.Instance.ServerTime - PlayerDataManager.Instance.ChangeSceneTime).TotalSeconds;
        if (timeDiff <= 1)
        {
            return;
        }
        PlayerDataManager.Instance.ChangeSceneTime = Game.Instance.ServerTime;
        if (ObjManager.Instance != null && ObjManager.Instance.MyPlayer != null)
        {
            ObjManager.Instance.MyPlayer.IsChangeScene = true; 
        }

        NetManager.Instance.StartCoroutine(GameUtils.WaitSetIsChangeScene(0.5f));

        NetManager.Instance.StartCoroutine(NetManager.Instance.SendTeleportRequestCoroutine(TransferId));
        mLastTeleportTime = Time.time;
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif
        if (ObjManager.Instance != null && ObjManager.Instance.MyPlayer != null)
        {
            ObjManager.Instance.MyPlayer.IsChangeScene = false;
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
    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        mTableData = Table.GetTransfer(TransferId);
        if (null == mTableData)
        {
            Logger.Warn("null==Table.GetTransfer({0})", TransferId);
            return;
        }

        //transform.localScale = new Vector3(mTableData.TransferRadius, mTableData.TransferRadius, mTableData.TransferRadius);
        var collider = GetComponent<CapsuleCollider>();
        if (null != collider)
        {
            collider.radius = mTableData.TransferRadius;
        }
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
}