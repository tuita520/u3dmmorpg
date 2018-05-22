using ScriptManager;
using DataTable;
using System;
#region using
using DataContract;
using System.Collections;
using System.Collections.Generic;
using EventSystem;
using UnityEngine;

#endregion

public class FightLeaderMaster : MonoBehaviour
{
    public int professionIndex = 0;
    private int characterCount = 0;
    public Vector3 ForwardAngle;
    private ObjFakeCharacter mFackeCharacter;
    public Vector3 Offset;
    public Vector3 Scale = Vector3.one;
    private void OnDestroy()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (null != mFackeCharacter)
        {
            mFackeCharacter.Destroy();
            mFackeCharacter.OnWingLoadedCallback = null;
            mFackeCharacter = null;
        }

        EventDispatcher.Instance.RemoveEventListener(FightLeaderMasterRefreshModelView.EVENT_TYPE, OnModelRefresh);
        characterCount = 0;

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    private void OnModelRefresh(IEvent ievent)
    {
        FightLeaderMasterRefreshModelView e = ievent as FightLeaderMasterRefreshModelView;
        if (professionIndex != e.Index) return;
        RefreshModel(e.Info);
    }

    private void OnWingLoaded(GameObject go)
    {
        var ani = go.GetComponent<Animation>();
        if (null != ani)
        {
            ani.enabled = false;
        }
    }

    private int GetNpcId(int roleId)
    {
        var npcId = 0;
        switch (roleId)
        {
            case 0://剑士
                {
                    npcId = 108;
                    break;
                }
            case 1://法师
                {
                    npcId = 109;
                    break;
                }
            case 2://弓手
                {
                    npcId = 110;
                    break;
                }
        }
        return npcId;
    }

    private int GetNpcObjId(int roleId)
    {
        var npcObjId = 0;
        switch (roleId)
        {
            case 0://剑士
                {
                    npcObjId = 108000;
                    break;
                }
            case 1://法师
                {
                    npcObjId = 109000;
                    break;
                }
            case 2://弓手
                {
                    npcObjId = 110000;
                    break;
                }
        }
        return npcObjId;
    }

    private void RefreshModel(PlayerInfoMsg info)
    {
        if (ObjManager.Instance.ObjPool.ContainsKey((ulong)GetNpcObjId(professionIndex)))
            ObjManager.Instance.RemoveObj((ulong)GetNpcObjId(professionIndex));
        if (null == info)
        {
            var init = new InitNPCData();
            var npcId = GetNpcId(professionIndex);
            var tbSceneNpc = Table.GetSceneNpc(npcId);
            if (null == tbSceneNpc)
                return;
            var tbNpc = Table.GetNpcBase(tbSceneNpc.DataID);
            if (null == tbNpc)
                return;
            var tbCharacterBase = Table.GetCharacterBase(tbSceneNpc.DataID);
            if (null == tbCharacterBase)
                return;
            init.DataId = tbSceneNpc.DataID;
            init.ObjId = (ulong)tbSceneNpc.DataID * 1000;
            init.Name = tbNpc.Name;
            init.Level = tbNpc.Level;
            init.HpMax = init.HpNow = tbCharacterBase.Attr[13];
            init.MpMax = init.MpNow = tbCharacterBase.Attr[14];
            init.DirX = (float)Math.Cos(tbSceneNpc.FaceDirection);
            init.DirZ = (float)Math.Sin(tbSceneNpc.FaceDirection);
            init.X = (float)tbSceneNpc.PosX;
            init.Z = (float)tbSceneNpc.PosZ;
            init.Y = GameLogic.GetTerrainHeight(init.X, init.Z);
            ObjManager.Instance.CreateNPCAsync(init);
            return;
        }
        var dataId = info.TypeId;
        var objId = info.Id;
        var equip = info.EquipsModel;
        var name = info.Name;
        var allianceName = string.Empty;
        var battleDic = PlayerDataManager.Instance._battleCityDic;
        foreach (var item in battleDic)
        {
            if (item.Value.Type == 0)
            {
                allianceName = item.Value.Name;
                break;
            }
        }

        /*
		var info = ObjManager.Instance.MyPlayer;
		var dataId = info.GetDataId();
		var objId = info.GetObjId();
		var equip = info.EquipList;
		var name = info.Name;
		var allianceName = "WWWWW";
		*/
        if (mFackeCharacter != null)
        {
            mFackeCharacter.Destroy();
        }
        mFackeCharacter = ObjFakeCharacter.Create(dataId, equip, character =>
        {
            if (null == mFackeCharacter)
            {
                character.Destroy();
                return;
            }

            if (character.GetObjId() != mFackeCharacter.GetObjId())
            {
                character.Destroy();
                return;
            }

            var collider = character.gameObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 1, 0);
            collider.height = 2;

            //character.transform.parent = transform;
            character.transform.position = gameObject.transform.position + Offset;
            character.transform.rotation = Quaternion.Euler(0, 180f, 0);
            //character.transform.forward = Quaternion.Euler(ForwardAngle.x, ForwardAngle.y, ForwardAngle.z) * Vector3.forward;
            character.transform.localScale = Scale;
            

            int inde = 0;
            switch (info.TypeId)
            {
                case 0:
                    inde = 460;
                    break;
                case 1:
                    inde = 461;
                    break;
                case 2:
                    inde = 462;
                    break;
            }
            var tab_Config = DataTable.Table.GetClientConfig(inde);
            if (null != tab_Config)
            {
                var titles = new Dictionary<int, string>();
                var tabid = 0;
                if (int.TryParse(tab_Config.Value, out tabid))
                {
                    titles.Add(tabid,null);
                    character.CreateNameBoard(name, titles);
                }
            }
        }, 0, false, -1, objId);
        mFackeCharacter.SetObjId(objId);
        //mFackeCharacter.OnWingLoadedCallback = OnWingLoaded;
        mFackeCharacter.gameObject.layer = LayerMask.NameToLayer("ObjLogic");
        mFackeCharacter.iType = (int)OBJ.TYPE.FAKE_FIGHTLEADER;
    }

    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        EventDispatcher.Instance.AddEventListener(FightLeaderMasterRefreshModelView.EVENT_TYPE, OnModelRefresh);

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    private IEnumerator StopAni(float time)
    {
        yield return new WaitForSeconds(time);

        if (null != mFackeCharacter)
        {
            mFackeCharacter.GetAnimationController().Stop(true);
        }
    }
}
