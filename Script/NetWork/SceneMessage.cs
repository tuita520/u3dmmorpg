#region using

using System;
using System.Collections;
using ScriptManager;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

public partial class NetManager : ClientAgentBase, ILogin9xServiceInterface, ILogic9xServiceInterface,
                                  IScene9xServiceInterface, IRank9xServiceInterface, IActivity9xServiceInterface,
                                  IChat9xServiceInterface, ITeam9xServiceInterface
{
    public IEnumerator SendTeleportRequestCoroutine(int typeId,bool Loop = false)
    {
        var msg = Instance.SendTeleportRequest(typeId);
        yield return msg.SendAndWaitUntilDone();

        if (ObjManager.Instance != null && ObjManager.Instance.MyPlayer != null)
        {
            ObjManager.Instance.MyPlayer.IsChangeScene = false;
        }

        if (msg.State != MessageState.Reply)
        {
            Logger.Debug("SendTeleportRequestCoroutine:MessageState.Timeout");
            yield break;
        }

        if (msg.ErrorCode != (int) ErrorCodes.OK)
        {
            var errorCode = (ErrorCodes) msg.ErrorCode;
            switch (errorCode)
            {
                case ErrorCodes.Error_LevelNoEnough:
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000108));
                }
                    break;
                case ErrorCodes.Error_DistanceTooMuch:
                {
                    if (Loop == false)
                    {
                        yield return new WaitForSeconds(1f);
                        NetManager.Instance.StartCoroutine(SendTeleportRequestCoroutine(typeId, true));
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("SendTeleportRequestCoroutine:MessageState[{0}]", errorCode);
                    }
                    break;
                }
                default:
                {
                    UIManager.Instance.ShowNetError(msg.ErrorCode);
                    Logger.Debug("SendTeleportRequestCoroutine:MessageState[{0}]", errorCode);
                }
                    break;
            }
        }
        else
        {
            PlatformHelper.Event("TeleportPortal");
        }
    }

    public void ReplyChangeScene(PlayerData data)
    {
        SceneManager.Instance.mIsLoadSceneOver = false;
        PlayerDataManager.Instance.mInitBaseAttr = data;
        var FormerSceneId = -1;
        if (null != GameLogic.Instance && null != GameLogic.Instance.Scene)
        {
            FormerSceneId = GameLogic.Instance.Scene.SceneTypeId;
        }
        StartCoroutine(SceneManager.Instance.EnterSceneCoroutinue( FormerSceneId,data.SceneId));

        /*
        //ÅÅ¶ÓÇÐ³¡¾°
        Action ChangeSceneAct = () =>
        {
            Game.Instance.ChangeScenestate = Game.eChangeSceneState.Changing;
            
            
        };

        Game.Instance.ChangeSceneList.Enqueue(ChangeSceneAct);
		 */
    }
    public void NotifyLodeInfo(MsgSceneLode info)
    {
        SceneManager.Instance.UnionName = info.TeamName;
        PlayerDataManager.Instance.LodeInfo = info;
    }

    public void NotifyPlayEffect(int effectId)
    {
        EventDispatcher.Instance.DispatchEvent(new Event_ChickenPickUp(effectId));
    }
    public void NotifyEquipChanged(ulong characterId, int part, int itemId)
    {
        try
        {
            var character = ObjManager.Instance.FindCharacterById(characterId);
            if (character != null)
            {
                if (part == (int) eBagType.Mount)
                {
                    if(itemId>0)
                        character.Mount(itemId);
                    else 
                        character.Dismount();
                }
                else
                {
                    character.ChangeEquip(part, itemId);                    
                }
            }

            if (character == ObjManager.Instance.MyPlayer)
            {
                EventDispatcher.Instance.DispatchEvent(new MyEquipChangedEvent(part, itemId));
            }

            var e = new CharacterEquipChange(characterId, part, itemId);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
    }

    public void NotifyCampChange(int campId, Vector2Int32 pos)
    {
        SceneManager.Instance.RegisterLoadSceneOverAction(b =>
        {
            if (ObjManager.Instance == null)
            {
                return;
            }
            var objMy = ObjManager.Instance.MyPlayer;
            if (objMy == null)
            {
                return;
            }
            if (objMy.GetCamp() == campId)
            {
//
                return;
            }
            objMy.SetCamp(campId);
            var p = new Vector2(GameUtils.DividePrecision(pos.x), GameUtils.DividePrecision(pos.y));
            objMy.Position = new Vector3(p.x, GameLogic.GetTerrainHeight(p.x, p.y), p.y);
            {
                // foreach(var objBase in ObjManager.Instance.ObjPool)
                var __enumerator1 = (ObjManager.Instance.ObjPool).GetEnumerator();
                while (__enumerator1.MoveNext())
                {
                    var objBase = __enumerator1.Current;
                    {
                        var obj = objBase.Value as ObjCharacter;
                        if (obj)
                        {
                            obj.OnNameBoardRefresh();
                        }
                    }
                }
            }
            var e = new PlayerCampChangeEvent();
            EventDispatcher.Instance.DispatchEvent(e);
        });
    }

    public void SyncFuBenStore(StoneItems itemlst, int storeType)
    {
        var e = new UpdateFuBenStoreStore_Event(itemlst, storeType);
        EventDispatcher.Instance.DispatchEvent(e);
    }
     public void SendMieshiResult(MieshiResultMsg msg)
    {
        var e = new MieshiResultEvent(msg);
        EventDispatcher.Instance.DispatchEvent(e);
    }
}