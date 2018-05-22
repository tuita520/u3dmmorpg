#region using

using System.Collections;
using ScriptManager;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

public partial class NetManager : ClientAgentBase, ILogin9xServiceInterface, ILogic9xServiceInterface,
                                  IScene9xServiceInterface, IRank9xServiceInterface, IActivity9xServiceInterface,
                                  IChat9xServiceInterface, ITeam9xServiceInterface
{
    public IEnumerator DungeonCompleteCoroutine(FubenResult result)
    {
        var e1 = new DungeonFightOver();
        EventDispatcher.Instance.DispatchEvent(e1);
        EventDispatcher.Instance.DispatchEvent(new ActivityAndDungeonCombatResultEvent((eDungeonCompleteType)result.CompleteType));
        if (result.FubenId == GameUtils.GetElfModelDisplayFubenId())
        { // 特殊处理
            yield return new WaitForSeconds(0.8f);
            UIManager.Instance.GetController(UIConfig.ElfUI).CallFromOtherClass("ElfDisplay", null);
            yield break;
        }
        var fubenId = result.FubenId;
        var tbFuben = Table.GetFuben(fubenId);
        var assistType = (eDungeonAssistType)tbFuben.AssistType;
        if (assistType == eDungeonAssistType.ClimbingTower)
        {
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return new WaitForSeconds(GameUtils.DungeonShowDelay / 1000.0f);
        }
        var completeType = (eDungeonCompleteType)result.CompleteType;
        var useSec = result.UseSeconds;
        var args = result.Args;
        var drawResult = result.Draw;
        var eraId = result.EraId;

        var tbScene = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
        if (tbScene != null && tbScene.FubenId != fubenId)
        {
            yield break;
        }

        if (completeType == eDungeonCompleteType.Success)
        {
            PlatformHelper.UMEvent("Fuben", "Complete", fubenId);
        }

        var e2 = new DungeonFightOverLater();
        EventDispatcher.Instance.DispatchEvent(e2);

        switch (assistType)
        {
            case eDungeonAssistType.Story: //剧情副本
                {
                    if (completeType == eDungeonCompleteType.Success)
                    {
                        var e = new Show_UI_Event(UIConfig.DungeonResult, new DungeonResultArguments
                        {
                            FubenId = fubenId,
                            Second = useSec,
                            DrawId = drawResult.DrawId,
                            DrawIndex = drawResult.SelectIndex,
                            EraId = eraId
                        });
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                }
                break;
            case eDungeonAssistType.Team: //组队副本
                {
                    if (completeType == eDungeonCompleteType.Success)
                    {
                        {
                            var e = new Show_UI_Event(UIConfig.DungeonRewardFrame, new DungeonRewardArguments
                            {
                                Seconds = useSec,
                                FubenId = fubenId
                            });
                            EventDispatcher.Instance.DispatchEvent(e);
                        }

                        {
                            var e = new UIEvent_DungeonReward(result.Draw);
                            EventDispatcher.Instance.DispatchEvent(e);
                        }
                    }
                }
                break;
            case eDungeonAssistType.DevilSquare: //恶魔广场
            case eDungeonAssistType.BloodCastle: //血色城堡
                {
                    var e = new Show_UI_Event(UIConfig.ActivityRewardFrame, new ActivityRewardArguments
                    {
                        Seconds = useSec,
                        CompleteType = completeType,
                        PlayerLevel = args[0],
                        FubenId = fubenId
                    });

                    EventDispatcher.Instance.DispatchEvent(e);
                }
                break;
            case eDungeonAssistType.WorldBoss: //世界boss
                {
                    var e = new Show_UI_Event(UIConfig.BossRewardFrame, new BossRewardArguments
                    {
                        Seconds = useSec,
                        CompleteType = completeType,
                        Items = drawResult.Items
                    });
                    EventDispatcher.Instance.DispatchEvent(e);
                }
                break;
            case eDungeonAssistType.CityExpMulty:
            case eDungeonAssistType.CityGoldSingle:
            case eDungeonAssistType.CityExpSingle:
            case eDungeonAssistType.FrozenThrone:
            case eDungeonAssistType.OrganRoom:
            case eDungeonAssistType.CastleCraft1:
            case eDungeonAssistType.CastleCraft2:
            case eDungeonAssistType.CastleCraft3:
            case eDungeonAssistType.CastleCraft4:
            case eDungeonAssistType.CastleCraft5:
            case eDungeonAssistType.CastleCraft6:
            case eDungeonAssistType.AllianceWar:
                {
                    if (args.Count > 0)
                    {
                        if (fubenId == 6100 || fubenId == 1019 || fubenId == 1050 || fubenId == 1020 || fubenId == 1021
                            || fubenId == 1022 || fubenId == 1023 || fubenId == 1030)
                        {
                            var e = new Show_UI_Event(UIConfig.DungeonResult, new DungeonResultArguments
                            {
                                FubenId = fubenId,
                                Second = useSec,
                                EraId = result.EraId
                            });
                            EventDispatcher.Instance.DispatchEvent(e);

                            var evn = new UIEvent_NewCityDungeonResult((int)completeType, result.SceneAddExp, fubenId, useSec, args);
                            EventDispatcher.Instance.DispatchEvent(evn);
                        }
                        else
                        {
                            var e = new Show_UI_Event(UIConfig.DungeonResult, new DungeonResultArguments
                            {
                                FubenId = fubenId,
                                Second = useSec,
                                EraId = result.EraId
                            });
                            EventDispatcher.Instance.DispatchEvent(e);

                            var leaderName = "";
                            if (result.Strs.Count > 0)
                            {
                                leaderName = result.Strs[0];
                            }
                            var evn = new UIEvent_CityDungeonResult(useSec, args, leaderName);
                            EventDispatcher.Instance.DispatchEvent(evn);
                        }
                    }
                }
                break;
            case eDungeonAssistType.MieShiWar:
                {
                    if (completeType == eDungeonCompleteType.Success)
                    {
                        if (args.Count > 0)
                        {
                            Show_UI_Event eventMonster = new Show_UI_Event(UIConfig.MishiResultUI, new DungeonResultArguments
                            {
                                FubenId = fubenId,
                                Second = useSec,
                                EraId = result.EraId
                            });
                            EventDispatcher.Instance.DispatchEvent(eventMonster);
                        }
                    }
                    else
                    {
                        if (args.Count > 0)
                        {
                            Show_UI_Event eventMonster = new Show_UI_Event(UIConfig.MishiResultUI, new DungeonResultArguments
                            {
                                FubenId = fubenId,
                                Second = useSec,
                                EraId = result.EraId
                            });
                            EventDispatcher.Instance.DispatchEvent(eventMonster);
                        }
                    }

                    var leaderName = "";
                    if (result.Strs.Count > 0)
                    {
                        leaderName = result.Strs[0];
                    }
                    var evn = new UIEvent_CityDungeonResult(useSec, args, leaderName);
                    EventDispatcher.Instance.DispatchEvent(evn);

                }
                break;
            case eDungeonAssistType.ClimbingTower:
                {
                    var e = new Show_UI_Event(UIConfig.ClimbingTowerRewardUI, new TowerRewardUIArguments
                    {
                        Result = (int)completeType,
                        FubenId = result.FubenId,
                        IsFirst = args[0]
                    });
                    EventDispatcher.Instance.DispatchEvent(e);
                }
                break;
            case eDungeonAssistType.Chiji:
                {
                         var e = new Show_UI_Event(UIConfig.ChickenRewardUI, new ChickenRewardUIArgument
                         {
                                FubenId = result.FubenId,
                                Rank = result.Args[0],
                                Items = result.Draw.Items
                         });
                         EventDispatcher.Instance.DispatchEvent(e);
                }
                break;
            default:
                break;
        }
    }

    public void NotifyReward(Dict_int_int_Data items)
    {
    }

    public void NotifyStartWarning(ulong timeOut)
    {
        EventDispatcher.Instance.DispatchEvent(new SceneTransition_Event());
    }

    public void NotifyRefreshDungeonInfo(DungeonInfo info)
    {
        EventDispatcher.Instance.DispatchEvent(new RefreshDungeonInfo_Event(info));
    }

    public void NotifyStartXpSkillGuide(int placeholder)
    {
        EventDispatcher.Instance.DispatchEvent(new NotifyStartXpSkillGuide_Event());
    }

    public void NotifyStartMaYaFuBenGuide(int type)
    {
        EventDispatcher.Instance.DispatchEvent(new NotifyStartMaYaFuBenGuide_Event(type));
    }


    public void SyncSceneBuilding(BuildingList data)
    {
        /* 目前不需要这功能
        if (!CityManager.Instance.IsInCityScene())
        {
            return;
        }
        {
            var __list1 = data.Data;
            var __listCount1 = __list1.Count;
            for (int __i1 = 0; __i1 < __listCount1; ++__i1)
            {
                var building = __list1[__i1];
                {
                    CityManager.Instance.RefreshBuilding(building.AreaId, building.TypeId);
                }
            }
        }
		 * */
    }

    public void SyncPetMission(PetMissionList msg)
    {
        {
            var __list2 = msg.Data;
            var __listCount2 = __list2.Count;
            for (var __i2 = 0; __i2 < __listCount2; ++__i2)
            {
                var petMission = __list2[__i2];
                {
                    petMission.Name = GameUtils.ServerStringAnalysis(petMission.Name);
                    //潜规则一下，任务类型是必有的条件
                    if (petMission.ConditionIds.Count > 0)
                    {
                        if (0 != petMission.ConditionIds[1])
                        {
                            petMission.ConditionIds.Insert(1, 0);
                            petMission.ConditionParam.Insert(1, petMission.Type);
                        }
                    }

                    var data = CityManager.Instance.GetPetMission(petMission.Id);

                    if (null != data)
                    {
                        CityManager.Instance.SetPetMission(petMission.Id, petMission);
                    }
                    else
                    {
                        CityManager.Instance.PetMissionDataList.Add(petMission);
                    }
                }
            }
        }
        CityManager.Instance.UpdatePetMissionState(false);
        EventDispatcher.Instance.DispatchEvent(new UIEvent_UpdatePetMissionList());
    }

    public void DeletePetMission(int missionId)
    {
        var CityManagerInstancePetMissionDataListCount0 = CityManager.Instance.PetMissionDataList.Count;
        for (var i = 0; i < CityManagerInstancePetMissionDataListCount0; i++)
        {
            if (CityManager.Instance.PetMissionDataList[i].Id == missionId)
            {
                CityManager.Instance.PetMissionDataList.RemoveAt(i);
                EventDispatcher.Instance.DispatchEvent(new UIEvent_UpdatePetMissionList());
                break;
            }
        }
    }

    public void DungeonComplete(FubenResult result)
    {
        var e1 = new DungeonCompleteEvent();
        EventDispatcher.Instance.DispatchEvent(e1);
        StartCoroutine(DungeonCompleteCoroutine(result));
    }

    public void LogicSyncAllianceMessage(int type, string name1, int allianceId, string name2)
    {
        var e = new UIEvent_UnionJoinReply();
        e.Type = type;
        e.Name1 = name1;
        e.AllianceId = allianceId;
        e.Name2 = name2;
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void NotifyStoreBuyed(long storeId, ulong Aid, string Aname)
    {
        var e = new UIEvent_OnTradingItemSelled(storeId);
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void AstrologyDrawOver(DrawItemResult Items, long drawTime)
    {
        var e = new UIEvent_AstrologyDrawResult();
        e.DrawItems = Items;
        e.DrawTime = drawTime;
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void NotifyRechargeSuccess(int rechargeId)
    {
        EventDispatcher.Instance.DispatchEvent(new RechargeSuccessEvent(rechargeId));
    }

    public void SyncOperationActivityItem(MsgOperActivtyItemList items)
    {
        if (0 == items.Items.Count)
        {//告诉客户端需要重新请求活动数据
            EventDispatcher.Instance.DispatchEvent(new SyncOperationActivityItemEvent(null));
        }
        else
        {
            foreach (var item in items.Items)
            {
                EventDispatcher.Instance.DispatchEvent(new SyncOperationActivityItemEvent(item));
            }
        }
    }

    public void SyncOperationActivityTerm(int id, int param)
    {
        EventDispatcher.Instance.DispatchEvent(new SyncOperationActivityTermEvent(id, param));
    }

    public void NotifyTableChange(int flag)
    {
        if (BitFlag.GetLow(flag, (int)eNotifyTableChangeFlag.RechargeTables))
        {
            EventDispatcher.Instance.DispatchEvent(new RechageActivityInitTables());
        }
    }

    public void ElfDrawOver(DrawItemResult Items, long drawTime)
    {
        var e = new ElfGetDrawResult();
        e.DrawItems = Items;
        e.DrawTime = drawTime;
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void SyncAllianceMessage(int type, string name1, int allianceId, string name2)
    {
    }

    public void TeamSyncAllianceMessage(int type, string name1, int allianceId, string name2)
    {
        var e = new UIEvent_UnionJoinReply();
        e.Type = type;
        e.Name1 = name1;
        e.AllianceId = allianceId;
        e.Name2 = name2;
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void ChangeAllianceData(int type, int param1, int param2)
    {
        var e = new UIEvent_UnionSyncDataChange();
        e.Type = type;
        e.param1 = param1;
        e.param2 = param2;
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void BroadcastSceneMonsterCount(int count)
    {
        var e = new MonsterCountRecvEvent(count);

        EventDispatcher.Instance.DispatchEvent(e);
    }
    public void NotifyQueueMessage(TeamCharacterMessage tcm)
    {
        var arg = new LineConfirmArguments { Msg = tcm };
        var e = new Show_UI_Event(UIConfig.LineConfim, arg);
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void NotifyQueueResult(ulong characterId, int result)
    {
        var e = new LineMemberConfirmEvent(characterId, result);
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void NotifyAllianceWarOccupantData(AllianceWarOccupantData data)
    {
        var e = new BattleUnionSyncOccupantChange(data);
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void NotifyAllianceWarChallengerData(AllianceWarChallengerData data)
    {
        var e = new BattleUnionSyncChallengerDataChange(data);
        EventDispatcher.Instance.DispatchEvent(e);
    }

    public void SyncAllianceChatMessage(int chatType,
                                        ulong characterId,
                                        string characterName,
                                        ChatMessageContent content)
    {
        GameUtils.OnReceiveChatMsg(chatType, characterId, characterName, content);
    }

    public void NotifyDamageList(DamageList list)
    {
        EventDispatcher.Instance.DispatchEvent(new RefreshDamageListEvent(list));
    }
    public void NotifyPointList(PointList list)
    {
        EventDispatcher.Instance.DispatchEvent(new RefreshMieshiDamageListEvent(list));
    }


    public void NotifyFubenInfo(FubenInfoMsg info)
    {
        SceneManager.Instance.RegisterLoadSceneOverAction(isAfterLoadScene =>
        {
            EventDispatcher.Instance.DispatchEvent(new RefreshDungeonInfoEvent(info));
            Scene.SetLogicId(info.LogicId, isAfterLoadScene && SceneManager.Instance.EnterSceneCounter == 1);
        });
    }

    public void NotifyStrongpointStateChanged(int camp, int index, int state, float time)
    {
        EventDispatcher.Instance.DispatchEvent(new StrongpointStateChangedEvent(camp, index, state, time));
    }

    public void SyncLevelChange(LevelUpAttrData Attr)
    {
        EventDispatcher.Instance.DispatchEvent(new UIEvent_SyncLevelUpAttrChange(Attr));
    }

    public void NotifyAllianceWarNpcData(int reliveCount, Int32Array data)
    {
        EventDispatcher.Instance.DispatchEvent(new GuardStateChange(data.Items, reliveCount));
    }

    public void NotifyScenePlayerInfos(ScenePlayerInfos info)
    {
        EventDispatcher.Instance.DispatchEvent(new ScenePlayerInfoEvent(info));
    }

    public void NotifyNpcStatus(MapNpcInfos infos)
    {
        EventDispatcher.Instance.DispatchEvent(new MapNpcInfoEvent(infos));
    }

    void ILogin9xServiceInterface.NotifyQueueIndex(int index)
    {
        NotifyQueueIndex(index);
    }

    public void SyncDataToClient(SceneSyncData syncData)
    {
        SyncCenter.ApplySync(syncData);
    }

    public void SyncMyDataToClient(SceneSyncData syncData)
    {
        SyncCenter.ApplySync(syncData);
    }

    public void Logout(ulong characterId)
    {
    }

    public void ChatNotify(int chatType, ulong characterId, string characterName, ChatMessageContent content)
    {
        GameUtils.OnReceiveChatMsg(chatType, characterId, characterName, content);
    }

    public void NotifyActivityState(int activityId, int state)
    {
        PlayerDataManager.Instance.ActivityState[activityId] = state;
        EventDispatcher.Instance.DispatchEvent(new ActivityStateChangedEvent());
    }
    public void NotifyBatteryData(int activityId, ActivityBatteryOne battery)
    {
        EventDispatcher.Instance.DispatchEvent(new MieShiOnPaotaiBtn_Event(false));
    }

    public void NotifyPlayerCanIn(int fubenId, long canInEndTime)
    {
        var tbFuben = Table.GetFuben(fubenId);
        //是否现在进入：{0}
        UIManager.Instance.ShowLimitMessage(MessageBoxType.OkCancel,
            string.Format(GameUtils.GetDictionaryText(270012), tbFuben.Name), "",
            () =>
            {
                GameUtils.EnterFuben(fubenId);
            },
            null, false, true, true, 10);
    }
    public void NotifyMieShiActivityState(int activityId, int state)
    {
        EventDispatcher.Instance.DispatchEvent(new ApplyMishiPortrait_Event());

        IControllerBase mController = UIManager.Instance.GetController(UIConfig.MonsterSiegeUI);

        if (mController == null)
        {
            return;
        }
        ClientDataModel.MonsterDataModel DataModel = mController.GetDataModel("") as ClientDataModel.MonsterDataModel;
        DataModel.ActivityState = state;

    }
    public void ActivateBookGroup(int groupId)
    {
        PlayerDataManager.Instance.SetBookGroupEnable(groupId);
        IControllerBase mController = UIManager.Instance.GetController(UIConfig.HandBook);
        if (mController == null)
        {
            return;
        }
    }

    public void SyncMount(ulong characterId, int mountId)
    {

    }
    public void AddSkillPoint(int skillId, int point)
    {
        PlayerDataManager.Instance.SkillTalentPointChange(skillId, point);
    }
    public void AutoMatchEnd(int MathchState)
    {
        Logger.Error("TeamWorkRefrerrence AutoMatchEnd client =" + MathchState);
    }

    public void AutoMatchStateChange(int MathchState)
    {
        EventDispatcher.Instance.DispatchEvent(new AutoMatchState_Event(MathchState));
    }
    public void SCSyncTeamMemberLevelChange(ulong characterId, ulong changeCharacterId, int reborn, int level)
    {
        EventDispatcher.Instance.DispatchEvent(new TeamMemberLevelChangeEvent(changeCharacterId, reborn, level));
    }
    public void NotifyChangetTeamTarget(int type, int targetID, int levelMini, int levelMax, int readTableId)
    {
        EventDispatcher.Instance.DispatchEvent(new TeamTargetChangeNotify_Event(type, targetID, levelMini, levelMax, readTableId));
    }

    public void NodifyTeamMemberPlayerNameChange(ulong characterId, ulong changeCharacterId, string changeName)
    {
        EventDispatcher.Instance.DispatchEvent(new TeamMemberNameChangeEvent(changeCharacterId, changeName));
    }
    public void NotifyTeamMemberScene(ulong characterId, ulong changeCharacterId, ulong sceneId)
    {
        EventDispatcher.Instance.DispatchEvent(new TeamFaceItemEvent(changeCharacterId, sceneId));
    }

    public void NotifyTeamScenGuid(ulong characterId, ulong changeCharacterId, ulong sceneGuid)
    {
        EventDispatcher.Instance.DispatchEvent(new TeamFaceItemEvent(changeCharacterId, sceneGuid));
    }

    public void TeamApplyListSync(ulong characterId, bool state)
    {
        EventDispatcher.Instance.DispatchEvent(new TeamApplyListSyncEvent(state));
    }

    public void NotifyChatRoseEffectChange(int chatType)
    {
        EventDispatcher.Instance.DispatchEvent(new ChatRoseEffectChangeEvent(chatType));
    }

    public void BroadcastAnchorEnterRoom(string charName)
    {
        EventDispatcher.Instance.DispatchEvent(new AnchorEnterRoomEvent(charName == GVoiceManager.Instance.OnlineAnchorName));
    }

    public void CK_NotifyCheckenSceneInfo(MsgCheckenSceneInfo info)
    {
        if (GameLogic.Instance.Scene != null)
            GameLogic.Instance.Scene.GetChijiInfo(info);
        EventDispatcher.Instance.DispatchEvent(new ChickenSafeChangeEvent(info));

        
    }

    public void CK_NotifyHurt(int param)
    {

    }
    public void CK_NotifyClientLevelup(ulong id, int lv, int exp, Dict_int_int_Data buff)
    {
        if (buff.Data != null && buff.Data.Count > 0)
        {
            OnCheckenSkillUpEvent e = new OnCheckenSkillUpEvent(lv, exp, buff.Data);
            EventDispatcher.Instance.DispatchEvent(e);
        }
        if (ObjManager.Instance == null)
        {
            return;
        }
        ObjManager.Instance.MyPlayer.UpdateChickenExp(lv, exp);
        /**  foreach (var item in ObjManager.Instance.ObjPool)
          {
              if (item.Value != null && item.Value.GetObjId() == id)
              {
                  Debug.Log(id.ToString() + "-------------" + lv + "========" + exp);
                  (item.Value as ObjCharacter).UpdateChickenExp(lv, exp);
                  break;
              }
          }**/
    }

    public void CK_NotifyRankList(MsgCheckenRankList rank)
    {
        RefreshChijRankiListEvent e = new RefreshChijRankiListEvent(rank);
        EventDispatcher.Instance.DispatchEvent(e);
    }
    public void NoticeTeamMemberError(string msg)
    {
        GameUtils.ShowHintTip(msg);
    }

    public void NotifyFieldFinal(MsgWarFlagInfoList msg)
    {
        var l = Table.GetClientConfig(1223).Value.Split('|');
        if (l == null)
            return;
        var CurrentScene = GameLogic.Instance.Scene.SceneTypeId;

        foreach (var id in l)
        {
            if (int.Parse(id) == CurrentScene)
            {
                var info = new FieldFinalUIArgument(msg);
                var e = new Show_UI_Event(UIConfig.FieldFinalUI, info);
                EventDispatcher.Instance.DispatchEvent(e);
                return;
            }
        }
    }

    public void SCNotifyAllianceActiveTask(DBActiveTask msg)
    {
        EventDispatcher.Instance.DispatchEvent(new ActiveTaskInfoEvent(msg));
    }

    public void NotifyMieShiActivityInfo(CommonActivityData msg)
    {
        EventDispatcher.Instance.DispatchEvent(new MieshiMsgEvent(msg));
    }

    public void SCNotifyPlayerExitAlliance(ulong exieplayerid, string name, bool bChange, ulong leaderId, string leaderName)
    {
        EventDispatcher.Instance.DispatchEvent(new PlayerExitAllianceMsgEvent(exieplayerid, name, bChange, leaderId, leaderName));
    }
}