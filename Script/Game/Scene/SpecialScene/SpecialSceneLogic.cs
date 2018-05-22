#region using

using System;
using System.Collections.Generic;
using System.Security.Permissions;
using DataTable;
using EventSystem;
using FastShadowReceiver;
using PathologicalGames;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

public class SpecialSceneLogic : MonoBehaviour
{
    #region Mono
    private GameObject Model;
    private Action<GameObject> CallBack;
    private float AnimationTime = 0.0f;
    private float TotalTime;
    private float WaitTime = 0.6f;

    private Vector3 Curpos;
    private Vector3 Targetpos1 = Vector3.zero;
    private Vector3 Targetpos2 = Vector3.zero;

    private Vector3 CurQua = Vector3.zero;
    private Vector3 TargetQua1 = Vector3.zero;
    private Vector3 TargetQua2 = Vector3.zero;

    private float CurScale = 1;
    private float TargetScale = 1;
    

    private void Awake()
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

    private void Start()
    {
#if !UNITY_EDITOR
        try
        {
#endif
            Model = null;

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }



    public void OnDestroy()
    {
#if !UNITY_EDITOR
        try
        {
#endif
        DestroyModel();
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public void Update()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif
        if (Model == null)
        {
            return;
        }

        if (AnimationTime < 0)
        {
            DestroyModel();
            if (CallBack != null)
            {
                CallBack(Model);
                CallBack = null;
            }
            
            return;
        }

        WaitTime -= Time.deltaTime;
        if (WaitTime > 0)
        {
            return;
        }
        AnimationTime += Time.deltaTime;

        var delta = 1.0f;
        if (AnimationTime > TotalTime)
        {
            AnimationTime = -1;
            delta = 1.0f;
        }
        else
        {
            delta = AnimationTime/TotalTime;
        }

        if (Targetpos2 != Vector3.zero)
        {
            Model.transform.position = Bezier.BezierCurve(Curpos, Targetpos1, Targetpos2, delta); 
        }
        else
        {
            Model.transform.position = Bezier.BezierCurve(Curpos, Targetpos1, delta);
        }

        var Qua = Vector3.Lerp(CurQua, TargetQua1, delta);
        Model.transform.rotation = Quaternion.Euler(Qua);

        var Scale = Mathf.Lerp(CurScale, TargetScale, delta);
        Model.transform.localScale = new Vector3(Scale, Scale, Scale);

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
    }

    public void DestroyModel()
    {
        if (null != Model)
        {
            ComplexObjectPool.Release(Model);
            Model = null;
        }
    }

    public void CreateWeaponModel(int equipId, Vector3 pos, Vector3 rotation, float curScale)
    {
        if(Model != null)
        {
            DestroyModel();
        }

        var tbEquip = Table.GetEquipBase(equipId);
        if (tbEquip == null)
        {
            return;
        }
        var tbMont = Table.GetWeaponMount(tbEquip.EquipModel);
        if (tbMont == null)
        {
            return;
        }

        ComplexObjectPool.NewObject(tbMont.Path,
        go =>
        {
            var goTransform = go.transform;
            if (null != GameLogic.Instance.Scene.NpcRoot)
            {
                goTransform.parent = GameLogic.Instance.Scene.NpcRoot.transform;
            }
            goTransform.position = pos;
            goTransform.rotation = Quaternion.Euler(rotation);
            goTransform.localScale = new Vector3(curScale, curScale, curScale); ;
            goTransform.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.ObjLogic));
            
            Model = go;
        });
    }

    public void PlaySceneMoveAnimation(int equipId, Vector3 curQua, Vector3 targetQua1, Vector3 curPos, Vector3 targetPos1, float curScale, float targetScale, float totalTime, Action<GameObject> callBack = null)
    {
        CreateWeaponModel(equipId, curPos, curQua, curScale);
        Curpos = curPos;
        Targetpos1 = targetPos1;

        CurQua = curQua;
        TargetQua1 = targetQua1;

        CurScale = curScale;
        TargetScale = targetScale;

        TotalTime = Math.Max(totalTime, 0.01f);
        CallBack = callBack;
        AnimationTime = 0.0f;
        WaitTime = 0.6f;
    }

    public void PlaySceneMoveAnimation(int equipId, Vector3 curQua, Vector3 targetQua1, Vector3 curPos, Vector3 targetPos1, Vector3 targetPos2, float curScale, float targetScale, float totalTime, Action<GameObject> callBack = null)
    {
        Targetpos2 = targetPos2;
        PlaySceneMoveAnimation(equipId, curQua, targetQua1, curPos, targetPos1, curScale, targetScale, totalTime, callBack);
    }
    #endregion
}