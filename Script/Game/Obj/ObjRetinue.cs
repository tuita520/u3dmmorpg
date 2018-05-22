#region using

using System;
using ScriptManager;
using UnityEngine;

#endregion

public class ObjRetinue : ObjNPC
{
    //拥有者id
    public ulong OwnerId;

    public bool GetIsMe()
    {
        return OwnerId == PlayerDataManager.Instance.GetGuid();
    }

    public override OBJ.TYPE GetObjType()
    {
        return OBJ.TYPE.RETINUE;
    }

    public override bool Init(InitBaseData initData, Action callback = null)
    {
        if (!base.Init(initData))
        {
            return false;
        }
        var retinueData = initData as InitRetinueData;
        if (retinueData == null)
        {
            return false;
        }
        OwnerId = retinueData.Owner;

		if (OwnerId >= 100000000 && OwnerId!=ObjManager.Instance.MyPlayer.GetObjId())
	    {
		    gameObject.layer = LayerMask.NameToLayer(GAMELAYER.OhterPlayer);
	    }
        return true;
    }

    protected override void InitNavMeshAgent()
    {
        if (null == TableCharacter)
        {
            return;
        }
        if (null == TableNPC)
        {
        }
    }

    protected override void OnSetModel()
    {
        if (mRenderer)
        {
            mRenderer.enabled = false;
            var c = mOtherRenderers.Count;
            for (int j = 0; j < c; j++)
            {
                var r = mOtherRenderers[j];
                if (r)
                    r.enabled = false;
            }
        }
        base.OnSetModel();
    }
}