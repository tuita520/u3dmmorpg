#region using

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class TeamFrameController : IControllerBase
    {
        private static CharacterBaseDataModel EmptyCharacterBaseData;
        private static TeamTargetChangeItemDataModel nowConfirmData = new TeamTargetChangeItemDataModel();
        public TeamFrameController()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(OpenTeamFromOtherEvent.EVENT_TYPE, OpenTeamFromOther);
            EventDispatcher.Instance.AddEventListener(TeamApplyListSyncEvent.EVENT_TYPE, RefreshRedSpot);
            EventDispatcher.Instance.AddEventListener(UIEvent_MainUITeamFrame_Show.EVENT_TYPE, UIEvent_MainUITeamFrame);
            EventDispatcher.Instance.AddEventListener(OnClickToggleTaskEvent.EVENT_TYPE, TeamChangeEven);
            EventDispatcher.Instance.AddEventListener(AutoMatchState_Event.EVENT_TYPE, OnAutoMatchState);

            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnUpdateFlagData);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagInit);

            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_Message.EVENT_TYPE, TeamMessage);
            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_Leave.EVENT_TYPE, OnClickLeaveTeam);
            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_Kick.EVENT_TYPE, OnClickKickTeam);

            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_NearTeam.EVENT_TYPE, Button_NearTeam);
            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_NearPlayer.EVENT_TYPE, Button_NearPlayer);

            EventDispatcher.Instance.AddEventListener(Event_TeamApplyOtherTeam.EVENT_TYPE, ApplyOtherTeam);

            EventDispatcher.Instance.AddEventListener(Event_TeamSwapLeader.EVENT_TYPE, SwapLeader);
            EventDispatcher.Instance.AddEventListener(Event_TeamKickPlayer.EVENT_TYPE, OnKickTeam);
            EventDispatcher.Instance.AddEventListener(Event_TeamLeaveTeam.EVENT_TYPE, OnLeaveTeam);
            EventDispatcher.Instance.AddEventListener(UIEvent_MatchingBack_Event.EVENT_TYPE, SendMatchingBack);
            EventDispatcher.Instance.AddEventListener(Event_TeamAcceptJoin.EVENT_TYPE, TeamAcceptJoin);
            EventDispatcher.Instance.AddEventListener(Event_TeamRefuseJoin.EVENT_TYPE, TeamRefuseJoin);
            EventDispatcher.Instance.AddEventListener(Enter_Scene_Event.EVENT_TYPE, OnEnterScene);

            EventDispatcher.Instance.AddEventListener(CharacterEquipChange.EVENT_TYPE, OnCharacterEquipChange);
            //Invite
            EventDispatcher.Instance.AddEventListener(Event_TeamInvitePlayer.EVENT_TYPE, InvitePlayer);
            EventDispatcher.Instance.AddEventListener(UIEvent_OperationList_AcceptInvite.EVENT_TYPE, AcceptInvite);
            EventDispatcher.Instance.AddEventListener(UIEvent_OperationList_RefuseInvite.EVENT_TYPE, RefuseInvite);

            //common
            EventDispatcher.Instance.AddEventListener(TeamOperateEvent.EVENT_TYPE, OnTeamOperate);
            EventDispatcher.Instance.AddEventListener(TeamApplyEvent.EVENT_TYPE, OnApplyTeam);
            EventDispatcher.Instance.AddEventListener(TeamMemberShowMenu.EVENT_TYPE, OnTeamMemberShowMenu);
            //cell
            EventDispatcher.Instance.AddEventListener(TeamNearbyPlayerClick.EVENT_TYPE, OnTeamNearbyOtherClick);
            EventDispatcher.Instance.AddEventListener(TeamNearbyTeamClick.EVENT_TYPE, OnTeamNearbyTeamClick);
            //charater
            EventDispatcher.Instance.AddEventListener(Character_Create_Event.EVENT_TYPE, OnCreateCharacter);
            //setting
            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_AutoJion.EVENT_TYPE, OnClickAutoJion);
            EventDispatcher.Instance.AddEventListener(UIEvent_TeamFrame_AutoAccept.EVENT_TYPE, OnClickAutoAccept);
            //map
            EventDispatcher.Instance.AddEventListener(SceneMapNotifyTeam.EVENT_TYPE, OnSceneMapNotify);

            EventDispatcher.Instance.AddEventListener(Event_TeamCreate.EVENT_TYPE, CreateTeam);

            // 打开组队 修改目标 页签
            EventDispatcher.Instance.AddEventListener(TeamTargetChange_Event.EVENT_TYPE, OpenTeamTargeChange);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeItemCellClick_Event.EVENT_TYPE, TeamTargetChangeItemCellClick);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeItemByOther_Event.EVENT_TYPE, TeamTargetChangeItemByOther);
            
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeLevelPlus_Event.EVENT_TYPE, OnClickTeamTargetChangeLevelPlus_Event);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeLevelSubStract_Event.EVENT_TYPE, OnClickTeamTargetChangeLevelSubStract_Event);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeLevelMaxPlus_Event.EVENT_TYPE, OnClickTeamTargetChangeLevelMaxPlus_Event);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeLevelMaxSubStract_Event.EVENT_TYPE, OnClickTeamTargetChangeLevelMaxSubStract_Event);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeConfirm_Event.EVENT_TYPE, OnClickTeamTargetChangeConfirm);
            EventDispatcher.Instance.AddEventListener(TeamTargetChangeNotify_Event.EVENT_TYPE, UpdateTeamTargetChangeNotify_Event);
            EventDispatcher.Instance.AddEventListener(TeamSearchList_Event.EVENT_TYPE, UpdateTeamSearchList_Event);
            EventDispatcher.Instance.AddEventListener(TeamSearchListClick_Event.EVENT_TYPE, UpdateTeamSearchListClick_Event);
            EventDispatcher.Instance.AddEventListener(TeamApplyListClick_Event.EVENT_TYPE, PullApplyList);
            EventDispatcher.Instance.AddEventListener(TeamSearchRefreshClick_Event.EVENT_TYPE, TeamSearchRefreshClick);
            EventDispatcher.Instance.AddEventListener(TeamAutoMatchClick_Event.EVENT_TYPE, AutoMatchClick);
            EventDispatcher.Instance.AddEventListener(TeamApplyItemCellClick_Event.EVENT_TYPE, TeamApplyItemCellClick);
            EventDispatcher.Instance.AddEventListener(TeamInviteNearbyClick_Event.EVENT_TYPE, TeamInviteNearbyClick);
            EventDispatcher.Instance.AddEventListener(TeamInviteFriendsClick_Event.EVENT_TYPE, TeamInviteFriendsClick);
            EventDispatcher.Instance.AddEventListener(TeamInviteBattleUnionClick_Event.EVENT_TYPE, TeamInviteBattleUnionClick);
            EventDispatcher.Instance.AddEventListener(TeamInviteClick_Event.EVENT_TYPE, TeamInviteClick);
            EventDispatcher.Instance.AddEventListener(TeamInviteClickCell_Event.EVENT_TYPE, TeamInviteClickCell);
            EventDispatcher.Instance.AddEventListener(ChatTeamClickEvent.EVENT_TYPE, ChatTeamByTargetLink);
            EventDispatcher.Instance.AddEventListener(TeamFaceItemEvent.EVENT_TYPE, TeamFaceItemEv);
            EventDispatcher.Instance.AddEventListener(TeamClearApplyList_Event.EVENT_TYPE, ApplyListClearEvent);
            EventDispatcher.Instance.AddEventListener(Event_MissionTabClick.EVENT_TYPE, ClickMissionTabEvent);
            EventDispatcher.Instance.AddEventListener(MissionOrTeamEvent.EVENT_TYPE, OnMissionOrTeamEvent);
            EventDispatcher.Instance.AddEventListener(TeamMemberLevelChangeEvent.EVENT_TYPE, OnMemberLevelChangeEvent);

            EventDispatcher.Instance.AddEventListener(TeamMemberNameChangeEvent.EVENT_TYPE, OnMemberNameChangeEvent);

        }

        private readonly OtherPlayerDataModel MyotherPlayer = new OtherPlayerDataModel();
        private object PostionTrigger;
        private bool isTeamUIChange = false;
        private TeamDataModel DataModel
        {
            get { return PlayerDataManager.Instance.TeamDataModel; }
            set { PlayerDataManager.Instance.TeamDataModel = value; }
        }

        TeamTargetChangeDataModel teamTargetChangeDataModel = new TeamTargetChangeDataModel();


        private void CreateTeam(IEvent ievent)
        {
            if (DataModel.TeamId > 0)
                return;
            NetManager.Instance.StartCoroutine(TeamCreateEnumerator());
        }

        private IEnumerator TeamCreateEnumerator()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(PlayerDataManager.Instance.GetGuid(), 0, 0, 0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //GameUtils.ShowHintTip(271006);
                        DataModel.TeamId = msg.Response;
                        ApplyTeam();
                        EventDispatcher.Instance.DispatchEvent(new MissionOrTeamEvent(1));
                        var e = new TeamTargetChangeConfirm_Event();
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("AcceptJoinTeam Error!............State..." + msg.State);
                }
            }
        }
        private void OnMemberLevelChangeEvent(IEvent ievent)
        {
            TeamMemberLevelChangeEvent e = ievent as TeamMemberLevelChangeEvent;
            if (e == null)
                return;
            for (int i = 0; i < DataModel.TeamList.Count; i++)
            {
                if (DataModel.TeamList[i].BaseDataModel == null) continue;
                if (e.characterId == DataModel.TeamList[i].Guid)
                {
                    DataModel.TeamList[i].BaseDataModel.Level = e.level;
                    DataModel.TeamList[i].BaseDataModel.Reborn = e.reborn;
                }
            }
        }
        private void OnMemberNameChangeEvent(IEvent ievent)
        {
            TeamMemberNameChangeEvent e = ievent as TeamMemberNameChangeEvent;
            if (e == null)
                return;
            for (int i = 0; i < DataModel.TeamList.Count; i++)
            {
                if (DataModel.TeamList[i].BaseDataModel == null) continue;
                if (e.characterId == DataModel.TeamList[i].Guid)
                {
                    DataModel.TeamList[i].Name = e.characterChangeName;
                    DataModel.TeamList[i].BaseDataModel.Name = e.characterChangeName;
                }
            }
        }

        private void OnMissionOrTeamEvent(IEvent ievent)
        {
            var e = ievent as MissionOrTeamEvent;
            if (null == e)
            {
                return;
            }
            var tab = e.Tab;
            DataModel.CurUITab = tab;
            if (DataModel.CurUITab == 1)//Team
            {
                EventDispatcher.Instance.DispatchEvent(new TeamOperateEvent(0, true));
                EventDispatcher.Instance.DispatchEvent(new Event_MissionTabClick(2));
            }
            else if (DataModel.CurUITab == 0)//Mission
            {
            }
        }

        private void AcceptInvite(IEvent ievent)
        {
            var ee = ievent as UIEvent_OperationList_AcceptInvite;
            NetManager.Instance.StartCoroutine(AcceptInvite(ee.PlayerId, ee.TeamId));
        }

        private IEnumerator AcceptInvite(ulong PlayerId, ulong TeamId)
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(PlayerDataManager.Instance.GetGuid(), 2, TeamId, 0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220106)));
                        if (!PlayerDataManager.Instance.GetFlag(500))
                        {
                            var flagList = new Int32Array();
                            flagList.Items.Add(500);
                            PlayerDataManager.Instance.SetFlagNet(flagList);
                        }
                        ApplyTeam();
                        EventDispatcher.Instance.DispatchEvent(new MissionOrTeamEvent(1));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_TeamIsFull)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220111)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveTeam)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220105)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotInvite)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220136)));
                    }
                }
                else
                {
                    Logger.Error("AcceptInvite Error!............State..." + msg.State);
                }
            }
        }

        private void AcceptJoinTeam(ulong toCharacterId)
        {
            if (DataModel.TeamId == 0)
            {
                //没有队伍
                ApplyTeam();

                Logger.Error("----------------Team-----AcceptJoinTeam------DataModel.TeamId");
                return;
            }
            if (IsLeader() == false)
            {
                //不是队长
                ApplyTeam();
                Logger.Error("----------------Team-----AcceptJoinTeam------IsLeader() == false");
                return;
            }
            if (GetTeamMemberCount() == 5)
            {
                //"你的队伍已经满了"
                GameUtils.ShowHintTip(220111);
                return;
            }
            var uGuid = PlayerDataManager.Instance.GetGuid();
            NetManager.Instance.StartCoroutine(AcceptJoinTeamEnumerator(uGuid, 0ul, toCharacterId));
        }

        private IEnumerator AcceptJoinTeamEnumerator(ulong characterId, ulong teamId, ulong toCharacterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(characterId, 4, teamId, toCharacterId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //同意对方的申请
                        GameUtils.ShowHintTip(271008);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_OtherHasTeam)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unknow)
                    {
                        //TODO
                        //"对方已经不在申请列表中了"
                        GameUtils.ShowHintTip(271009);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotTeam
                             || msg.ErrorCode == (int)ErrorCodes.Error_TeamNotFind
                             || msg.ErrorCode == (int)ErrorCodes.Error_TeamIsFull
                             || msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        ApplyTeam();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("AcceptJoinTeam Error!............State..." + msg.State);
                }
            }
        }

        private void AddTeamMember(ulong characterId)
        {
            var count = GetTeamMemberCount();
            if (count == 0)
            {
                //组建了队伍
                ApplyTeam();
            }
            else if (count == 5)
            {
                //队伍已满，状态错误
                ApplyTeam();
                return;
            }

            var index = GetMemberIndex(characterId);
            if (index != -1)
            {
                //已在队伍
                return;
            }

            NotifyTeamChange();
            var teamData = DataModel.TeamList[count];
            teamData.Guid = characterId;
            PlayerDataManager.Instance.ApplyPlayerInfo(characterId, ApplyTeamMemberInfo);
            //if (IsLeader())
            //{
            //    EventDispatcher.Instance.DispatchEvent(new TeamChangeLineupEvent(1));
            //}
            if (IsTeamFull())
            {
                if (IsLeader())
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                        GameUtils.GetDictionaryText(220142),//队伍已满，可以出发了！
                        "",
                        () =>
                        {
                            if (null != PlayerDataManager.Instance.currentTeamTarget)
                            {
                                var currentTeamTarget = PlayerDataManager.Instance.currentTeamTarget;
                                if (currentTeamTarget.isBelongIndex == 0)//附近
                                {
                                }
                                else if (currentTeamTarget.isBelongIndex == 1)//副本
                                {
                                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.DungeonUI));
                                }
                                else if (currentTeamTarget.isBelongIndex == 2)//活动
                                {
                                    var argList = new List<int>();
                                    argList.Add(-1);
                                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ActivityUI, new ActivityArguments
                                    {
                                        Tab = 2,
                                        Args = argList
                                    }));
                                }
                            }
                        });
                }
            }
        }

        private void ApplyMemberPostion()
        {
            if (DataModel.TeamId == 0)
            {
                return;
            }
            var ary = new Uint64Array();
            var myGuid = PlayerDataManager.Instance.GetGuid();
            for (var i = 0; i < 5; i++)
            {
                var one = DataModel.TeamList[i];
                if (myGuid != 0 && myGuid != one.Guid)
                {
                    ary.Items.Add(one.Guid);
                }
            }
            NetManager.Instance.StartCoroutine(ApplyMemberPostionCoroutine(ary));
        }

        private IEnumerator ApplyMemberPostionCoroutine(Uint64Array ary)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ApplyPlayerPostionList(ary);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var sceneMapController = UIManager.Instance.GetController(UIConfig.SceneMapUI);
                        var ret = msg.Response.List;
                        for (var i = 0; i < ret.Count; i++)
                        {
                            var guid = ary.Items[i];
                            var index = GetMemberIndex(guid);
                            var member = DataModel.TeamList[index];
                            var pos = ret[i];
                            if (pos.x == -1 || pos.y == -1)
                            {
                                member.ShowMap = false;
                            }
                            else
                            {
                                var v3 = new Vector3(GameUtils.DividePrecision(pos.x), 0, GameUtils.DividePrecision(pos.y));
                                member.ShowMap = true;
                                var loc =
                                    (Vector3)sceneMapController.CallFromOtherClass("ConvertSceneToMap", new object[] { v3 });
                                member.MapLoction = loc;
                            }
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("SendMatchingBack Error!............State..." + msg.State);
                }
            }
        }

        //----------------------------------------------------------申请加入-----------------------------
        private void ApplyOtherTeam(IEvent evt)
        {
            var e = evt as Event_TeamApplyOtherTeam;
            ApplyOtherTeam(e.CharacterId);
        }

        private void ApplyOtherTeam(ulong toCharacterId)
        {
            if (DataModel.TeamId != 0)
            {
                GameUtils.ShowHintTip(220105);
                return;
            }
            var uGuid = PlayerDataManager.Instance.GetGuid();
            NetManager.Instance.StartCoroutine(ApplyOtherTeamEnumerator(uGuid, 0ul, toCharacterId));
        }

        private IEnumerator ApplyOtherTeamEnumerator(ulong characterId, ulong teamId, ulong toCharacterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(characterId, 3, teamId, toCharacterId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {

                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220100)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveTeam)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220105)));
                        ApplyTeam();
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unline)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220103)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_TeamIsFull)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220111)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotTeam
                             || msg.ErrorCode == (int)ErrorCodes.Error_TeamNotFind)
                    {
                        //TODO
                        //GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        GameUtils.ShowHintTip(220136);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("Invite Error!............State..." + msg.State);
                }
            }
        }

        private void ApplySceneObj()
        {
            NetManager.Instance.StartCoroutine(ApplySceneObjEnumerator());
        }

        private IEnumerator ApplySceneObjEnumerator()
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ApplySceneObj(0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        foreach (var item in teamTargetChangeDataModel.InviteNearByList)
                        {
                            item.isNull = false;
                        }
                        DataModel.NearPlayerList.Clear();
                        {
                            var __list3 = msg.Response.Data;
                            var __listCount3 = __list3.Count;
                            if (__list3.Count == 0)
                            {
                                DataModel.EmptyTips[2] = true;
                            }
                            else
                            {
                                DataModel.EmptyTips[2] = false;
                            }
                            for (var __i3 = 0; __i3 < __listCount3; ++__i3)
                            {
                                var simpleInfo = __list3[__i3];
                                {
                                    string serverName;
                                    PlayerDataManager.Instance.ServerNames.TryGetValue(simpleInfo.Serverid, out serverName);

                                    var otherPlayer = new OtherPlayerDataModel
                                    {
                                        Guid = simpleInfo.CharacterId,
                                        Name = simpleInfo.Name,
                                        Level = simpleInfo.Level,
                                        TypeId = simpleInfo.Type,
                                        FightValue = simpleInfo.FightValue,
                                        SceneId = MyotherPlayer.SceneId,
                                        Ladder = simpleInfo.Ladder,
                                        StarNum = simpleInfo.Star,
                                        ServerName = serverName
                                    };

                                    DataModel.NearPlayerList.Add(otherPlayer);

                                    if (__i3 < 15)
                                    {
                                        var pro = Table.GetCharacterBase(simpleInfo.Type);
                                        teamTargetChangeDataModel.InviteNearByList[__i3].isNull = true;
                                        teamTargetChangeDataModel.InviteNearByList[__i3].characterId = (int)simpleInfo.CharacterId;
                                        teamTargetChangeDataModel.InviteNearByList[__i3].characterName = simpleInfo.Name;
                                        teamTargetChangeDataModel.InviteNearByList[__i3].levelStr = simpleInfo.Level.ToString() + "级";
                                        teamTargetChangeDataModel.InviteNearByList[__i3].profession = simpleInfo.Type;
                                        teamTargetChangeDataModel.InviteNearByList[__i3].StarNum = simpleInfo.Star;/*pro.Name*/;
                                        teamTargetChangeDataModel.InviteNearByList[__i3].professionName = GetLadderName(simpleInfo.Ladder, simpleInfo.Type)/*pro.Name*/;
                                        teamTargetChangeDataModel.InviteNearByList[__i3].ladder = GetLadderIconId(simpleInfo.Ladder, simpleInfo.Type);
                                    }
                                }
                            }
                        }

                        CloneInviteItem(teamTargetChangeDataModel.InviteNearByList);
                        //for (int i = 0; i < 5; i++)
                        //{
                        //    OtherPlayerDataModel otherPlayer = new OtherPlayerDataModel()
                        //    {
                        //        Guid = ObjManager.Instance.MyPlayer.GetObjId(),
                        //        Name = ObjManager.Instance.MyPlayer.Name,
                        //        Level = i*10 + 5,
                        //        TypeId = i%3,
                        //    };
                        //    DataModel.NearPlayerList.Add(otherPlayer);
                        //}
                    }
                    else
                    {
                        UIManager.Instance.RemoveBlockLayer();
                        Logger.Debug("ApplySceneObj..................." + msg.ErrorCode);
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    UIManager.Instance.RemoveBlockLayer();
                    Logger.Debug("ApplySceneObj..................." + msg.ErrorCode);
                }
            }
        }

        private IEnumerator ApplySceneTeamEnumerator()
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ApplySceneTeamLeaderObj(0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DataModel.NearTeamList.Clear();
                        var __list3 = msg.Response.Data;
                        var __listCount3 = __list3.Count;
                        if (__listCount3 == 0)
                        {
                            DataModel.EmptyTips[1] = true;
                        }
                        else
                        {
                            DataModel.EmptyTips[1] = false;
                        }
                        for (var __i3 = 0; __i3 < __listCount3; ++__i3)
                        {
                            var simpleInfo = __list3[__i3];
                            {
                                string serverName;
                                PlayerDataManager.Instance.ServerNames.TryGetValue(simpleInfo.Serverid, out serverName);

                                var otherPlayer = new OtherTeamDataModel
                                {
                                    Guid = simpleInfo.CharacterId,
                                    Name = simpleInfo.Name,
                                    Level = simpleInfo.Level,
                                    TypeId = simpleInfo.Type,
                                    FightValue = simpleInfo.FightValue,
                                    SceneId = MyotherPlayer.SceneId,
                                    Count = simpleInfo.RoleId,
                                    Ladder = simpleInfo.Ladder,
                                    ServeName = serverName
                                };
                                DataModel.NearTeamList.Add(otherPlayer);
                            }
                        }
                        //                     for (int i = 0; i < 5; i++)
                        //                     {
                        //                         OtherTeamDataModel otherTeam = new OtherTeamDataModel();
                        //                         //{
                        //                         msg.Response.Data
                        //                         otherTeam.Guid = ObjManager.Instance.MyPlayer.GetObjId();
                        //                         otherTeam.Name = ObjManager.Instance.MyPlayer.Name;
                        //                         otherTeam.Level = i * 10 + 5;
                        //                         otherTeam.TypeId = i % 3;
                        //                         otherTeam.Count = (i + 5) % 5 + 1;
                        //                         otherTeam.TeamId = DataModel.TeamId + (ulong)i;
                        //                         //};
                        //                         DataModel.NearTeamList.Add(otherTeam);
                        //                     }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        UIManager.Instance.RemoveBlockLayer();
                        Logger.Debug("ApplyNearTeam..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    UIManager.Instance.RemoveBlockLayer();
                    Logger.Debug("ApplySceneObj..................." + msg.ErrorCode);
                }
            }
        }

        private void ApplyTeam()
        {
            NetManager.Instance.StartCoroutine(ApplyTeamCoroutine());
        }

        private IEnumerator ApplyTeamCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.ApplyTeam(PlayerDataManager.Instance.GetGuid());
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (DataModel.AutoMatch != 1)
                        {
                            if (isTeamUIChange)
                            {
                                DataModel.TeamBackGround = 1;
                                if (!DataModel.HasTeam)
                                {
                                    DataModel.TeamBackGround = 2;
                                    DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220128));
                                }
                            }
                        }
                        else
                        {
                            DataModel.TeamBackGround = 2;
                            var count = GetTeamMemberCount();
                            DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220126), count, 5);
                        }
                        DataModel.TeamId = msg.Response.TeamId;
                        var index = 0;
                        {
                            var __list4 = msg.Response.Teams;
                            var __listCount4 = __list4.Count;
                            if (__list4.Count == 0)
                            {
                                DataModel.EmptyTips[0] = true;
                            }
                            else
                            {
                                DataModel.EmptyTips[0] = false;
                            }
                            for (var __i4 = 0; __i4 < __listCount4; ++__i4)
                            {
                                var simpleInfo = __list4[__i4];
                                {
                                    DataModel.TeamList[index].Guid = simpleInfo.CharacterId;
                                    DataModel.TeamList[index].Name = simpleInfo.Name;
                                    DataModel.TeamList[index].Level = simpleInfo.Level;

                                    DataModel.TeamList[index].TypeId = simpleInfo.Type;
                                    var title = Table.GetCharacterBase(DataModel.TeamList[index].TypeId);
                                    DataModel.TeamList[index].Carceer = GetLadderName(simpleInfo.Ladder, simpleInfo.Type);

                                    DataModel.TeamList[index].FightValue = simpleInfo.FightValue;
                                    DataModel.TeamList[index].Ladder = simpleInfo.Ladder;
                                    DataModel.TeamList[index].Equips.Clear();
                                    DataModel.TeamList[index].Equips = new Dictionary<int, int>(simpleInfo.EquipsModel);
                                    DataModel.TeamList[index].IsLeave = !simpleInfo.OnLine;
                                    DataModel.TeamList[index].StarNum = simpleInfo.Star;
                                    var scene = Table.GetScene(simpleInfo.SceneId);
                                    if(scene != null)
                                        DataModel.TeamList[index].SceneName = scene.Name;
                                    SetTeamMemberCharacterBase(DataModel.TeamList[index]);
                                    DataModel.TeamList[index].TeamMomentRebornHead = GetLadderIconId(simpleInfo.Ladder, simpleInfo.Type);

                                    if (DataModel.TeamList[index].IsLeave)
                                    {
                                        DataModel.TeamList[index].IsShowLeave = 1;
                                        DataModel.TeamList[index].PlayerLeaveLabel = DataModel.TeamList[index].Name + "(离线)";
                                    }
                                    else
                                    {
                                        DataModel.TeamList[index].IsShowLeave = 0;
                                        DataModel.TeamList[index].PlayerLeaveLabel = "";
                                    }

                                    index++;
                                }
                            }
                        }
                        for (var i = index; i != 5; ++i)
                        {
                            CleanTeamOne(DataModel.TeamList[i]);
                        }

                        CheckTeamOperation();

                        NotifyModelView();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                        Logger.Debug("ApplyTeam..................." + msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Debug("ApplyTeam..................." + msg.State);
                }
            }
        }

        /// <summary>
        /// 队伍是否满员
        /// </summary>
        private bool IsTeamFull()
        {
            var memCount = GetTeamMemberCount();
            if (memCount >= 5)
            {
                return true;
            }
            return false;
        }

        private void ApplyTeamMemberInfo(PlayerInfoMsg msg)
        {
            var index = GetMemberIndex(msg.Id);
            if (index == -1)
            {
                return;
            }
            var teamData = DataModel.TeamList[index];
            teamData.Name = msg.Name;
            teamData.Level = msg.Level;
            teamData.TypeId = msg.TypeId;
            teamData.FightValue = msg.FightPoint;
            teamData.Ladder = msg.Ladder;
            teamData.StarNum = msg.StarNum;
            teamData.TeamMomentRebornHead = GetLadderIconId(msg.Ladder, msg.TypeId);
            teamData.Equips = new Dictionary<int, int>(msg.EquipsModel);
            teamData.Carceer = GetLadderName(msg.Ladder, msg.TypeId);
            SetTeamMemberCharacterBase(teamData);
            NotifyModelView();
        }

        //tab：附近玩家
        private void Button_NearPlayer(IEvent ievent)
        {
            ApplySceneObj();
        }

        //-------------------------------------------
        private void Button_NearTeam(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ApplySceneTeamEnumerator());
        }

        private void CheckAutoSetting()
        {
            var join = PlayerDataManager.Instance.GetFlag(485);
            var accept = PlayerDataManager.Instance.GetFlag(486);
            if (DataModel.AutoJoin == join && DataModel.AutoAccept == accept)
            {
                return;
            }
            var tureArray = new Int32Array();
            var falseArray = new Int32Array();
            if (DataModel.AutoJoin != join)
            {
                if (DataModel.AutoJoin)
                {
                    tureArray.Items.Add(485);
                }
                else
                {
                    falseArray.Items.Add(485);
                }
            }

            if (DataModel.AutoAccept != accept)
            {
                if (DataModel.AutoAccept)
                {
                    tureArray.Items.Add(486);
                }
                else
                {
                    falseArray.Items.Add(486);
                }
            }
            PlayerDataManager.Instance.SetFlagNet(tureArray, falseArray);
        }

        private void CheckTeamOperation()
        {
            var myUid = PlayerDataManager.Instance.GetGuid();
            var isLeader = IsLeader();
            for (var i = 0; i < 5; i++)
            {
                var one = DataModel.TeamList[i];
                if (one.Guid == myUid)
                {
                    one.Operation = 1;
                }
                else
                {
                    if (isLeader)
                    {
                        one.Operation = 2;
                    }
                    else
                    {
                        one.Operation = 0;
                    }
                }
            }
        }

        private void CleanTeamOne(TeamOneDataModel teamOne)
        {
            teamOne.Guid = 0;
            teamOne.TypeId = -1;
            teamOne.Level = 0;
            teamOne.Name = "";
            teamOne.BaseDataModel = EmptyCharacterBaseData;
            teamOne.IsShowLeave = 0;
            teamOne.PlayerLeaveLabel = "";
        }

        //解散队伍
        private void DisbandTeam(ulong CharacterId)
        {
            //if (IsLeader())
            //{
            //    EventDispatcher.Instance.DispatchEvent(new TeamChangeLineupEvent(0));
            //}
            NotifyTeamChange(10);
            DataModel.EmptyTips[0] = true;
            DataModel.TeamId = 0;
            {
                // foreach(var i in DataModel.TeamList)
                var __enumerator2 = (DataModel.TeamList).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var i = __enumerator2.Current;
                    {
                        if (CharacterId == PlayerDataManager.Instance.CharacterGuid)
                        {
                            CleanTeamOne(i);
                            var e2 = new Close_UI_Event(UIConfig.OperationList);
                            EventDispatcher.Instance.DispatchEvent(e2);
                        }
                        else
                        {
                            if (i.Guid == CharacterId)
                            {
                                ApplyTeam();
                                break;
                            }
                        }
                    }
                }
            }
            NotifyModelView();
        }

        private void EnterScene(int scendId)
        {
            MyotherPlayer.SceneId = scendId;
        }

        //base
        private int GetMemberIndex(ulong characterId)
        {
            if (DataModel.TeamId == 0)
            {
                return -1;
            }
            for (var i = 0; i < 5; i++)
            {
                var one = DataModel.TeamList[i];
                if (one.Guid == characterId)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetTeamMemberCount()
        {
            var count = 0;
            for (var i = 0; i < 5; i++)
            {
                var one = DataModel.TeamList[i];
                if (one.Guid != 0)
                {
                    count++;
                }
            }
            return count;
        }

        private bool HasTeam()
        {
            return DataModel.TeamId != 0;
        }

        //-------------------------------------------------------邀请玩家----------------------------
        private void InvitePlayer(IEvent evt)
        {
            if (!DataModel.HasTeam)
            {
                //GameUtils.ShowHintTip(220141);
                //return;
                NetManager.Instance.StartCoroutine(TeamCreateEnumerator());//如果玩家没有队伍，对其他人进行队伍邀请，创建一个默认队伍
            }
            var e = evt as Event_TeamInvitePlayer;
            InvitePlayer(e.CharacterId);
        }

        private void InvitePlayer(ulong toCharacterId)
        {
            var uGuid = PlayerDataManager.Instance.GetGuid();
            //if (uGuid == toCharacterId)
            //{
            //  return;
            //}
            if (IsInTeam(toCharacterId))
            {
                //"已经是同一队伍了"
                GameUtils.ShowHintTip(300858);
                return;
            }
            NetManager.Instance.StartCoroutine(InvitePlayerEnumerator(uGuid, DataModel.TeamId, toCharacterId));
        }

        private IEnumerator InvitePlayerEnumerator(ulong characterId, ulong teamId, ulong toCharacterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(characterId, 1, teamId, toCharacterId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220117)));
                        PlatformHelper.UMEvent("Team", "Invite");
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unline)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220103)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveTeam)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220104)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_TeamIsFull)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220112)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_AlreadyToLeader)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220118)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_TeamFunctionNotOpen)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220124)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_SetRefuseTeam)
                    {
                        var e1 = new ChatMainHelpMeesage(string.Format(GameUtils.GetDictionaryText(997), ""));
                        EventDispatcher.Instance.DispatchEvent(e1);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("Invite Error!............State..." + msg.State);
                }
            }
        }

        private bool IsInTeam(ulong characterId)
        {
            var index = GetMemberIndex(characterId);
            if (index != -1)
            {
                return true;
            }
            return false;
        }

        private bool IsLeader()
        {
            var myUid = PlayerDataManager.Instance.GetGuid();
            var isLeader = myUid == DataModel.TeamList[0].Guid;
            if (!isLeader) teamTargetChangeDataModel.RedSpot = false;
            if (null != DataModel.TeamList[4] && DataModel.TeamList[4].Level > 0)
                teamTargetChangeDataModel.RedSpot = false;
            return isLeader;
        }

        private void KickTeam(ulong characterId)
        {
            if (DataModel.TeamId == 0)
            {
                //没有队伍,状态错误
                ApplyTeam();

                Logger.Error("------------Team--------KickTeam-----DataModel.TeamId");
                return;
            }
            var uGuid = PlayerDataManager.Instance.GetGuid();
            if (uGuid == characterId)
            {
                return;
            }
            if (IsLeader() == false)
            {
                //权限不足,状态错误
                Logger.Error("------------Team--------KickTeam-----IsLeader() == false");
                ApplyTeam();
                return;
            }
            NetManager.Instance.StartCoroutine(KickTeamEnumerator(uGuid, DataModel.TeamId, characterId));
        }

        private IEnumerator KickTeamEnumerator(ulong characterId, ulong teamId, ulong toCharacterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(characterId, 8, teamId, toCharacterId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        RemoveTeamMember(toCharacterId);
                        ApplyTeam();
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unknow
                             || msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotTeam
                             || msg.ErrorCode == (int)ErrorCodes.Error_TeamNotSame
                             || msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        Logger.Error("---------------Team----TeamMessage---{0}", msg.ErrorCode);
                        ApplyTeam();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("KickTeam Error!............State..." + msg.State);
                }
            }
        }

        private void LeaveTeam()
        {
            if (DataModel.TeamId == 0)
            {
                //没有队伍
                Logger.Error("----------Team------------LeaveTeam----DataModel.TeamId == 0");
                ApplyTeam();
                return;
            }
            NetManager.Instance.StartCoroutine(LeaveTeamEnumerator());
        }

        private IEnumerator LeaveTeamEnumerator()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(ObjManager.Instance.MyPlayer.GetObjId(), 5, DataModel.TeamId, 0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DisbandTeam(ObjManager.Instance.MyPlayer.GetObjId());

                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220108)));

                        PlatformHelper.UMEvent("Team", "Leave");
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_TeamNotFind
                             || msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotTeam)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        ApplyTeam();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("LeaveTeam Error!............State..." + msg.State);
                }
            }
        }

        private void NotifyModelView()
        {
            var e = new UIEvent_TeamFrame_RefreshModel(0);
            EventDispatcher.Instance.DispatchEvent(e);
            teamTargetChangeDataModel.isLeader = IsLeader();
            if (teamTargetChangeDataModel.isLeader)
                teamTargetChangeDataModel.IsShowAutoMatch = true;
            else
            {
                if (!DataModel.HasTeam)
                    teamTargetChangeDataModel.IsShowAutoMatch = true;
                else
                    teamTargetChangeDataModel.IsShowAutoMatch = false;
            }

            TeamFaceItemEv1(null);
            if (DataModel.AutoMatch == 1)
            {
                var count = GetTeamMemberCount();
                DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220126), count, 5);
            }

            if (DataModel.AutoMatch != 1)
            {
                if (isTeamUIChange)
                {
                    DataModel.TeamBackGround = 1;
                    if (!DataModel.HasTeam)
                    {
                        DataModel.TeamBackGround = 2;
                        DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220128));
                    }
                }
            }
            else
            {
                DataModel.TeamBackGround = 2;
                var count = GetTeamMemberCount();
                DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220126), count, 5);
            }
        }

        private void NotifyTeamChange(int type = 0)
        {
            CheckTeamOperation();
            var e = new TeamChangeEvent(type);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        //请求队伍信息
        private void OnApplyTeam(IEvent ievent)
        {
            ApplyTeam();
        }

        private void OnCharacterEquipChange(IEvent ievent)
        {
            var e = ievent as CharacterEquipChange;
            var index = GetMemberIndex(e.CharacterId);
            if (index == -1)
            {
                return;
            }
            var one = DataModel.TeamList[index];
            one.Equips[e.Part] = e.ItemId;
            if (State == FrameState.Open)
            {
                NotifyModelView();
            }
        }

        private void OnClickAutoAccept(IEvent ievent)
        {
            DataModel.AutoAccept = !DataModel.AutoAccept;
        }

        //----------------------------------------------------Setting-------
        private void OnClickAutoJion(IEvent ievent)
        {
            DataModel.AutoJoin = !DataModel.AutoJoin;
        }

        //踢出队伍
        private void OnClickKickTeam(IEvent ievent)
        {
            var ee = ievent as UIEvent_TeamFrame_Kick;
            KickTeam(DataModel.TeamList[ee.Index].Guid);
        }

        //离开队伍
        private void OnClickLeaveTeam(IEvent ievent)
        {
            LeaveTeam();
        }

        private void OnCloseSceneMap()
        {
            if (PostionTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(PostionTrigger);
                PostionTrigger = null;
            }
            for (var i = 0; i < 5; i++)
            {
                var member = DataModel.TeamList[i];
                if (member.ShowMap)
                {
                    member.ShowMap = false;
                }
            }
        }

        private void OnCreateCharacter(IEvent ievent)
        {
            var e = ievent as Character_Create_Event;
            var charId = e.CharacterId;
            var index = GetMemberIndex(charId);
            if (index == -1)
            {
                return;
            }
            var obj = ObjManager.Instance.FindCharacterById(charId);
            if (obj != null)
            {
                DataModel.TeamList[index].BaseDataModel = obj.CharacterBaseData;
                DataModel.TeamList[index].Equips = new Dictionary<int, int>(obj.EquipList);

                if (State == FrameState.Open)
                {
                    NotifyModelView();
                }
            }
        }

        private void OnEnterScene(IEvent ievent)
        {
            var e = ievent as Enter_Scene_Event;
            EnterScene(e.SceneId);
        }

        //auto flag
        private void OnFlagInit(IEvent ievent)
        {
            DataModel.AutoJoin = PlayerDataManager.Instance.GetFlag(485);
            DataModel.AutoAccept = PlayerDataManager.Instance.GetFlag(486);
        }

        private void OnKickTeam(IEvent ievent)
        {
            var ee = ievent as Event_TeamKickPlayer;
            KickTeam(ee.CharacterId);
        }

        private void OnLeaveTeam(IEvent evt)
        {
            LeaveTeam();
        }

        //----------------------------------------------------MapLoction-------
        private void OnOpenSceneMap()
        {
            if (PostionTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(PostionTrigger);
                PostionTrigger = null;
            }
            PostionTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime, ApplyMemberPostion, 3000);
        }

        private void OnSceneMapNotify(IEvent ievent)
        {
            var e = ievent as SceneMapNotifyTeam;
            var isOpen = e.IsOpen;
            if (isOpen)
            {
                OnOpenSceneMap();
            }
            else
            {
                OnCloseSceneMap();
            }
        }

        private void OnTeamMemberShowMenu(IEvent ievent)
        {
            var e = ievent as TeamMemberShowMenu;
            ShowTeamMemberMenu(e.Index);
        }

        private void OnTeamNearbyOtherClick(IEvent ievent)
        {
            var e = ievent as TeamNearbyPlayerClick;

            if (DataModel.NearPlayerList.Count <= e.Index || e.Index < 0)
            {
                Logger.Error("Button_OtherPlayer_Tip  index[{0}] is out", e.Index);
                return;
            }
            var playerData = DataModel.NearPlayerList[e.Index];
            if (e.Type == 0)
            {
                InvitePlayer(playerData.Guid);
            }
            else if (e.Type == 1)
            {
                PlayerDataManager.Instance.ShowCharacterPopMenu(playerData.Guid, playerData.Name, 5, playerData.Level,
                    playerData.Ladder, playerData.TypeId);
            }
        }

        //-----------------------------------------event----------------
        private void OnTeamNearbyTeamClick(IEvent ievent)
        {
            var e = ievent as TeamNearbyTeamClick;
            if (DataModel.NearTeamList.Count <= e.Index || e.Index < 0)
            {
                Logger.Error("Button_OtherPlayer_Tip  index[{0}] is out", e.Index);
                return;
            }
            var teamData = DataModel.NearTeamList[e.Index];
            if (e.Type == 0)
            {
                ApplyOtherTeam(teamData.Guid);
            }
            else if (e.Type == 1)
            {
                PlayerDataManager.Instance.ShowCharacterPopMenu(teamData.Guid, teamData.Name, 4, teamData.Level,
                    teamData.Ladder, teamData.TypeId);
            }
        }

        private void OnTeamOperate(IEvent ievent)
        {
            var e = ievent as TeamOperateEvent;
            switch (e.Type)
            {
                case 0:
                    {
                        if (DataModel.HasTeam == false)
                        {
                            if (DataModel.AutoMatch == 1)
                            {
                                DataModel.TeamBackGround = 2;
                                var count = GetTeamMemberCount();
                                DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220126), count, 5);
                            }
                            else
                            {

                                DataModel.TeamBackGround = 2;
                                DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220128));
                            }
                            EventDispatcher.Instance.DispatchEvent(new UIEvent_MainUITeamFrame_Show());
                        }
                        else
                        {
                            if (DataModel.AutoMatch != 1)
                            {
                                DataModel.TeamBackGround = 1;
                            }
                            else
                            {
                                DataModel.TeamBackGround = 2;
                                var count = GetTeamMemberCount();
                                DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220126), count, 5);
                            }
                            if (e.misTeamShow == true)
                            {
                                var e1 = new Show_UI_Event(UIConfig.TeamFrame);
                                EventDispatcher.Instance.DispatchEvent(e1);
                                var ee = new UIEvent_TeamFrame_NearTeam();
                                EventDispatcher.Instance.DispatchEvent(ee);
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (!DataModel.HasTeam)
                        {
                            var e0 = new Show_UI_Event(UIConfig.TeamFrame);
                            EventDispatcher.Instance.DispatchEvent(e0);
                            var e1 = new UIEvent_TeamFrame_NearTeam();
                            EventDispatcher.Instance.DispatchEvent(e1);

                            EventDispatcher.Instance.DispatchEvent(new OpenTeamFromOtherEvent(1));
                        }
                        else
                        {
                            var e1 = new Show_UI_Event(UIConfig.TeamFrame);
                            EventDispatcher.Instance.DispatchEvent(e1);

                        }
                       

                    }
                    break;
            }
        }


        private void OnUpdateFlagData(IEvent ievent)
        {
            var e = ievent as FlagUpdateEvent;
            if (e.Index == 485)
            {
                DataModel.AutoJoin = e.Value;
            }
            else if (e.Index == 485)
            {
                DataModel.AutoAccept = e.Value;
            }
        }

        private void RefuseInvite(IEvent ievent)
        {
            var ee = ievent as UIEvent_OperationList_RefuseInvite;
            NetManager.Instance.StartCoroutine(RefuseInvite(ee.PlayerId, ee.TeamId));
        }

        private IEnumerator RefuseInvite(ulong PlayerId, ulong TeamId)
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(PlayerDataManager.Instance.GetGuid(), 9, TeamId, 0);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                }
                else
                {
                    Logger.Error("RefuseInvite Error!............State..." + msg.State);
                }
            }
        }

        private void RemoveTeamMember(ulong characterId)
        {
            var index = GetMemberIndex(characterId);
            if (index == -1)
            {
                return;
            }
            NotifyTeamChange();
            for (var i = index; i < 5; i++)
            {
                if (i + 1 < 5)
                {
                    //DataModel.TeamList[i] = DataModel.TeamList[i + 1];
                    DataModel.TeamList[i].Guid = DataModel.TeamList[i + 1].Guid;
                    DataModel.TeamList[i].Name = DataModel.TeamList[i + 1].Name;
                    DataModel.TeamList[i].FightValue = DataModel.TeamList[i + 1].FightValue;
                    DataModel.TeamList[i].TypeId = DataModel.TeamList[i + 1].TypeId;
                    DataModel.TeamList[i].Operation = DataModel.TeamList[i + 1].Operation;
                    DataModel.TeamList[i].Level = DataModel.TeamList[i + 1].Level;
                    DataModel.TeamList[i].Ladder = DataModel.TeamList[i + 1].Ladder;
                    DataModel.TeamList[i].Equips = DataModel.TeamList[i + 1].Equips;
                    DataModel.TeamList[i].BaseDataModel = DataModel.TeamList[i + 1].BaseDataModel;
                    DataModel.TeamList[i].ShowMap = DataModel.TeamList[i + 1].ShowMap;
                    DataModel.TeamList[i].MapLoction = DataModel.TeamList[i + 1].MapLoction;
                    DataModel.TeamList[i].IsLeave = DataModel.TeamList[i + 1].IsLeave;
                    DataModel.TeamList[i].TeamMomentRebornHead = GetLadderIconId(DataModel.TeamList[i].Ladder, DataModel.TeamList[i].TypeId);
                    if (DataModel.TeamList[i].IsLeave)
                    {
                        DataModel.TeamList[i].IsShowLeave = 1;
                        DataModel.TeamList[i].PlayerLeaveLabel = DataModel.TeamList[index].Name + "(离线)";
                    }
                    else
                    {
                        DataModel.TeamList[i].IsShowLeave = 0;
                        DataModel.TeamList[i].PlayerLeaveLabel = "";
                    }
                }
                else
                {
                    CleanTeamOne(DataModel.TeamList[4]);
                }
            }
            NotifyModelView();
            //if (IsLeader())
            //{
            //    EventDispatcher.Instance.DispatchEvent(new TeamChangeLineupEvent(1));
            //}
        }

        //----------------------------------------------------Match-------
        private void SendMatchingBack(IEvent ievent)
        {
            var ee = ievent as UIEvent_MatchingBack_Event;
            SendMatchingBack(ee.Result);
        }

        private void SendMatchingBack(int result)
        {
            if (GameLogic.Instance != null && GameLogic.Instance.Scene != null)
            {
                var nowTbScene = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
                if (nowTbScene != null)
                {
                    if (nowTbScene.FubenId != -1 && result == 1) //当前正在副本中 就取消预约
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210123));
                        result = 0;
                    }
                }
            }

            NetManager.Instance.StartCoroutine(SendMatchingBackEnumerator(result));
        }

        private IEnumerator SendMatchingBackEnumerator(int result)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.MatchingBack(result);
                yield return msg.SendAndWaitUntilDone();
            }
        }

        private void SetLeaveState(bool isLeave, ulong characterId)
        {
            foreach (var item in DataModel.TeamList)
            {
                if (item.Guid == characterId)
                {
                    item.IsLeave = isLeave;
                    if (item.IsLeave)
                    {
                        item.IsShowLeave = 1;
                        item.PlayerLeaveLabel = item.Name + "(离线)";
                    }
                    else
                    {
                        item.IsShowLeave = 0;
                        item.PlayerLeaveLabel = "";
                    }
                    break;
                }
            }
        }

        private void SetTeamMemberCharacterBase(TeamOneDataModel oneData)
        {
            var myGuid = PlayerDataManager.Instance.GetGuid();
            if (oneData.Guid != 0)
            {
                var obj = ObjManager.Instance.FindCharacterById(oneData.Guid);
                if (obj != null)
                {
                    oneData.BaseDataModel = obj.CharacterBaseData;
                    return;
                }
            }

            oneData.BaseDataModel = new CharacterBaseDataModel();
            oneData.BaseDataModel.Level = oneData.Level;
            oneData.BaseDataModel.Reborn = oneData.Ladder;
            oneData.BaseDataModel.MaxHp = 100;
            oneData.BaseDataModel.Hp = 100;
            oneData.BaseDataModel.MaxMp = 100;
            oneData.BaseDataModel.Mp = 100;
        }

        private void ShowTeamMemberMenu(int memberIndex)
        {
            if (5 <= memberIndex || memberIndex < 0)
            {
                Logger.Error("ShowTeamMemberMenu  index[{0}] is out", memberIndex);
                return;
            }
            var teamMember = DataModel.TeamList[memberIndex];
            if (0 == teamMember.Level)
            {
                return;
            }
            var selfId = PlayerDataManager.Instance.GetGuid();
            var index = 6;
            if (teamMember.Guid != selfId)
            {
                index = IsLeader() ? 3 : 2;
            }
            PlayerDataManager.Instance.ShowCharacterPopMenu(teamMember.Guid, teamMember.Name, index, teamMember.Level,
                teamMember.Ladder, teamMember.TypeId);
        }

        //更换队长
        private void SwapLeader(IEvent ievent)
        {
            var ee = ievent as Event_TeamSwapLeader;
            SwapLeader(ee.CharacterId);
        }

        private void SwapLeader(ulong characterId)
        {
            var uGuid = PlayerDataManager.Instance.GetGuid();
            if (IsLeader() == false)
            {
                //权限不足,,状态错误
                Logger.Error("----------------Team-----SwapLeader------IsLeader() == false");
                ApplyTeam();
                return;
            }
            foreach (var item in DataModel.TeamList)
            {
                if (item.IsLeave)
                {
                    //玩家已离线
                    var e = new ShowUIHintBoard(200002404);
                    EventDispatcher.Instance.DispatchEvent(e);
                    return;
                }
            }


            NetManager.Instance.StartCoroutine(SwapLeaderEnumerator(uGuid, DataModel.TeamId, characterId));
        }

        private IEnumerator SwapLeaderEnumerator(ulong characterId, ulong teamId, ulong toCharacterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(characterId, 6, teamId, toCharacterId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SwapTeamMember(characterId, toCharacterId);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unline)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220103)));
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotTeam
                             || msg.ErrorCode == (int)ErrorCodes.Error_TeamNotSame
                             || msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader
                             || msg.ErrorCode == (int)ErrorCodes.Unknow)
                    {
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        ApplyTeam();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("KickTeam Error!............State..." + msg.State);
                }
            }
        }

        private void SwapTeamMember(ulong characterFrom, ulong characterTo)
        {
            var indexFrom = GetMemberIndex(characterFrom);
            var indexTo = GetMemberIndex(characterTo);
            if (indexFrom == -1 || indexTo == -1)
            {
                return;
            }
            var from = DataModel.TeamList[indexFrom];
            DataModel.TeamList[indexFrom] = DataModel.TeamList[indexTo];
            DataModel.TeamList[indexTo] = from;
            NotifyTeamChange();
            NotifyModelView();
        }

        private void TeamAcceptJoin(IEvent evt)
        {
            var e = evt as Event_TeamAcceptJoin;
            AcceptJoinTeam(e.CharacterId);
        }

        //有队伍消息
        private void TeamMessage(IEvent ievent)
        {
            var ee = ievent as UIEvent_TeamFrame_Message;
            //TODO
            Logger.Warn("-----TeamMessage----Type = {0}---- TeamId={1}-----CharacterID= {2}", ee.Type, ee.TeamId,
                ee.CharacterId);
            //TODO
            switch (ee.Type)
            {
                case 1: //被characterId邀请
                    {
                    }
                    break;
                case 2: //characterId2 推荐 characterId
                    {
                    }
                    break;
                case 3: //characterId加入了队伍
                    {
                        if (!PlayerDataManager.Instance.GetFlag(500))
                        {
                            var flagList = new Int32Array();
                            flagList.Items.Add(500);
                            PlayerDataManager.Instance.SetFlagNet(flagList);
                        }
                        AddTeamMember(ee.CharacterId);
                    }
                    break;
                case 4: //characterId想要加入队伍，是否同意
                    {
                    }
                    break;
                case 5: //characterId退出了队伍
                    {
                        RemoveTeamMember(ee.CharacterId);
                    }
                    break;
                case 8: //队伍中的characterId 下线了
                    {
                    }
                    break;
                case 9: //队伍中的characterId 上线了
                    {
                    }
                    break;
                case 7: //队伍解散了
                case 10: //被踢出队伍
                    {
                        DisbandTeam(ee.CharacterId);
                    }
                    break;
                case 6: //成为新队长了
                case 14: //换队长
                    {
                        var leader = DataModel.TeamList[0].Guid;
                        SwapTeamMember(leader, ee.CharacterId);
                    }
                    break;
            }
        }

        private void TeamRefuseJoin(IEvent evt)
        {
            var e = evt as Event_TeamRefuseJoin;
            TeamRefuseJoin(e.CharacterId);
        }

        private void TeamRefuseJoin(ulong toCharacterId)
        {
            if (DataModel.TeamId == 0)
            {
                //没有队伍
                ApplyTeam();
                Logger.Error("----------------Team-----TeamRefuseJoin------DataModel.TeamId");
                return;
            }
            if (IsLeader() == false)
            {
                //不是队长
                ApplyTeam();
                Logger.Error("----------------Team-----TeamRefuseJoin------IsLeader() == false");
                return;
            }
            NetManager.Instance.StartCoroutine(TeamRefuseJoinEnumerator(0ul, 0ul, toCharacterId));
        }

        private IEnumerator TeamRefuseJoinEnumerator(ulong characterId, ulong teamId, ulong toCharacterId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.TeamMessage(characterId, 10, teamId, toCharacterId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        GameUtils.ShowHintTip(271006);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotTeam)
                    {
                        //发现自己没有队伍
                        ApplyTeam();
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_CharacterNotLeader)
                    {
                        //自己不是队长
                        ApplyTeam();
                        GameUtils.ShowNetErrorHint(msg.ErrorCode);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Unknow)
                    {
                        //TODO
                        //GameUtils.ShowNetErrorHint(msg.ErrorCode);
                        GameUtils.ShowHintTip(271007);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("AcceptJoinTeam Error!............State..." + msg.State);
                }
            }
        }

        private void UpdataCharacterInfo()
        {
            for (var i = 0; i < 5; i++)
            {
                var one = DataModel.TeamList[i];
                if (one.Level == 0)
                {
                    continue;
                }

                if (one.BaseDataModel == null)
                {
                    continue;
                }
                if (one.BaseDataModel.Level == 0)
                {
                    continue;
                }
                if (one.BaseDataModel.Level > one.Level)
                {
                    one.Level = one.BaseDataModel.Level;
                }
                if (one.BaseDataModel.Reborn > one.Ladder)
                {
                    one.Ladder = one.BaseDataModel.Reborn;
                }
            }
        }

        public void CleanUp()
        {
            DataModel = new TeamDataModel();
            if (PostionTrigger != null)
            {
                TimeManager.Instance.DeleteTrigger(PostionTrigger);
                PostionTrigger = null;
            }
            EventDispatcher.Instance.RemoveEventListener(AutoMatchState_Event.EVENT_TYPE, OnAutoMatchState);
        }

        public void OnChangeScene(int sceneId)
        {
            CheckTeamOperation();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "HasTeam")
            {
                return HasTeam();
            }
            if (name == "IsInTeam")
            {
                var id = (ulong)param[0];
                return IsInTeam(id);
            }
            if (name == "SetLeaveState")
            {
                var isLeave = (bool)param[0];
                var id = (ulong)param[1];
                SetLeaveState(isLeave, id);
                TeamFaceItemEv1(null);
            }
            return null;
        }

        public void OnShow()
        {
            ApplyTeam();
            if (DataModel.OpenFromOther != 0)
            {
                int other = DataModel.OpenFromOther;
                DataModel.OpenFromOther = 0;
                DataModel.OpenFromOther = other;
            }
        }

        public void Close()
        {
            PlayerDataManager.Instance.CloseCharacterPopMenu();
            CheckAutoSetting();
            DataModel.OpenFromOther = 0;
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            UpdataCharacterInfo();
            teamTargetChangeDataModel.isApplyTeamList = true;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "TeamTargetChange")
                return teamTargetChangeDataModel;
            return DataModel;
        }

        public FrameState State { get; set; }

        private void OnAutoMatchState(IEvent ievent)
        {
            var e = ievent as AutoMatchState_Event;
            DataModel.AutoMatch = e.param;
            var count = GetTeamMemberCount();

            //MainUI自动组队提示面板
            if (PlayerDataManager.Instance.NoticeData.TeamOpenFlag)
            {
                if (DataModel.AutoMatch == 1)
                {
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_MainUITeamFrame_Show());
                    DataModel.TeamBackGround = 2;
                    DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220126), count, 5);
                }
                else
                {
                    if (isTeamUIChange)
                    {
                        DataModel.TeamBackGround = 1;
                        if (!DataModel.HasTeam)
                        {
                            DataModel.TeamBackGround = 2;
                            DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220128));
                        }
                    }
                    else
                    {
                        DataModel.TeamBackGround = 0;
                        DataModel.mainUITeamLabel = string.Format(GameUtils.GetDictionaryText(220128));
                    }
                }
            }

        }

        void OpenTeamTargeChange(IEvent ievent)
        {
            teamTargetChangeDataModel.TargetChangeList.Clear();

            EventDispatcher.Instance.DispatchEvent(new TeamTargetChangeItemCellClick_Event(0));
        }

        void TeamTargetChangeItemCellClick(IEvent ievent)
        {
            var e = ievent as TeamTargetChangeItemCellClick_Event;
            int chosenType = GetClickItemType(e.index);

            if (chosenType != 0)
            {
                for (int i = 0; i < teamTargetChangeDataModel.TargetChangeList.Count; i++)
                {
                    var ite = teamTargetChangeDataModel.TargetChangeList[i];
                    ite.select = false;
                    if (e.index == i)
                    {
                        Clone(teamTargetChangeDataModel.CurrentItemData, ite);
                        ite.select = true;
                    }
                }
            }
            bool isOpe = false;
            if (chosenType == 0)
            {
                int chose = GetClickItemGroup(e.index);
                isOpe = teamTargetChangeDataModel.CurrentChosenItem == e.index;
                teamTargetChangeDataModel.TargetChangeList = GetOriginalData(chose, !isOpe);

                teamTargetChangeDataModel.CurrentChosenItem = e.index;
                if (isOpe)
                {
                    teamTargetChangeDataModel.CurrentChosenItem = 0;
                    Clone(teamTargetChangeDataModel.CurrentItemData, teamTargetChangeDataModel.TargetChangeList[0]);

                }
                if (e.index == 0)
                {
                    Clone(teamTargetChangeDataModel.CurrentItemData, teamTargetChangeDataModel.TargetChangeList[0]);
                }
            }

            if (!DataModel.HasTeam)
            {
                PullSearchList(teamTargetChangeDataModel.CurrentItemData.isBelongIndex, teamTargetChangeDataModel.CurrentItemData.targetItemId);
            }
            //Logger.Error("CurrentItemData == isBelongIndex" + teamTargetChangeDataModel.CurrentItemData.isBelongIndex + " targetItemId=" + teamTargetChangeDataModel.CurrentItemData.targetItemId
            //    + " levelmini =" + teamTargetChangeDataModel.CurrentItemData.levelMini
            //    + " Name=" + teamTargetChangeDataModel.CurrentItemData.targetItemName);
        }

        void TeamTargetChangeItemByOther(IEvent ievent)
        {
            var e = ievent as TeamTargetChangeItemByOther_Event;
            teamTargetChangeDataModel.TargetChangeList.Clear();
            
           
            if (e.groupType != 0) //从别的入口打开的肯定类型不会是0
            {
                DataModel.OpenFromOther = 1;
                teamTargetChangeDataModel.TargetChangeList = GetOriginalData(e.groupType, true);

                for (int i = 0; i < teamTargetChangeDataModel.TargetChangeList.Count; i++)
                {
                    var ite = teamTargetChangeDataModel.TargetChangeList[i];
                    ite.select = false;
                    if (e.targetId == ite.targetItemId)
                    {
                        Clone(teamTargetChangeDataModel.CurrentItemData, ite);
                        ite.select = true;
                    }
                }
            }
            if (!DataModel.HasTeam)
            {
                PullSearchList(teamTargetChangeDataModel.CurrentItemData.isBelongIndex, teamTargetChangeDataModel.CurrentItemData.targetItemId);
            }
            else
            {
                PullSearchList(0, -1);
            }
        }

        int GetClickItemType(int inde)
        {
            int type = 0;

            for (int i = 0; i < teamTargetChangeDataModel.TargetChangeList.Count; i++)
            {
                if (inde == i)
                {
                    var ite = teamTargetChangeDataModel.TargetChangeList[i];
                    type = ite.targetItemGroupType;
                    break;
                }
            }

            return type;
        }

        int GetClickItemGroup(int inde)
        {
            int type = 0;

            for (int i = 0; i < teamTargetChangeDataModel.TargetChangeList.Count; i++)
            {
                if (inde == i)
                {
                    var ite = teamTargetChangeDataModel.TargetChangeList[i];
                    type = ite.isBelongIndex;
                    break;
                }
            }

            return type;
        }

        ObservableCollection<TeamTargetChangeItemDataModel> GetOriginalData(int chosenIndex = 0, bool isOpen = false)
        {
            ObservableCollection<TeamTargetChangeItemDataModel> data = new ObservableCollection<TeamTargetChangeItemDataModel>();

            for (int i = 0; i < 3; i++)
            {
                TeamTargetChangeItemDataModel item = new TeamTargetChangeItemDataModel();
                if (0 == i)
                {
                    item.targetItemGroupType = 0;
                    item.targetItemId = 0;
                    item.targetItemName = GameUtils.GetDictionaryText(100001477);//"附近";  
                    item.isOpen = false;
                    item.isBelongIndex = 0;
                    item.levelMini = 1;
                    var record = Table.GetClientConfig(103);
                    if (null != record)
                        item.levelMax = int.Parse(record.Value);
                    data.Add(item);
                }

                if (1 == i)
                {
                    var isHaveSecond1 = false;
                    item.targetItemGroupType = 0;
                    item.targetItemId = -1;
                    item.targetItemName = GameUtils.GetDictionaryText(100001476);//"多人副本";  
                    item.isBelongIndex = 1;
                    data.Add(item);

                    if (chosenIndex == 1)
                    {
                        if (isOpen)
                        {
                            // add copy data
                            int count = 0;
                            Table.ForeachFuben(recoard =>
                            {
                                if (recoard.AssistType == 2)
                                {
                                    var condition = recoard.EnterConditionId;
                                    var open = false;
                                    var enterLevel = 0;

                                    open = PlayerDataManager.Instance.CheckCondition(condition) == 0;
                                    if (open)
                                    {
                                        var conditionTab = Table.GetConditionTable(condition);
                                        for (int r = 0; r < conditionTab.ItemId.Length; r++)
                                        {
                                            if (conditionTab.ItemId[r] == 0)
                                            {
                                                enterLevel = conditionTab.ItemCountMin[r];

                                            }
                                        }

                                        if (enterLevel == 0)
                                        {
                                            enterLevel = 1;
                                        }

                                        count++;
                                        TeamTargetChangeItemDataModel recItem = new TeamTargetChangeItemDataModel();
                                        recItem.targetItemGroupType = 1;
                                        recItem.targetItemId = recoard.Id;//多人副本此处 目标id是 副本id  
                                        recItem.targetItemName = recoard.Name;
                                        recItem.isBelongIndex = 1;
                                        recItem.levelMini = enterLevel;
                                        var record = Table.GetClientConfig(103);
                                        if (null != record)
                                            recItem.levelMax = int.Parse(record.Value);
                                        if (count == 1)
                                        {
                                            recItem.select = true;
                                            Clone(teamTargetChangeDataModel.CurrentItemData, recItem);
                                        }
                                        data.Add(recItem);
                                        isHaveSecond1 = true;
                                    }


                                }
                                return true;
                            });

                            teamTargetChangeDataModel.CurrentChosenItem = 1;
                        }
                    }

                    //if (!isHaveSecond1)
                    //    data.Remove(item);
                }

                if (2 == i)
                {
                    var isHaveSecond = false;
                    item.targetItemGroupType = 0;
                    item.targetItemId = -1;
                    item.targetItemName = GameUtils.GetDictionaryText(100001475);//"多人活动";  
                    item.isBelongIndex = 2;
                    data.Add(item);

                    if (chosenIndex == 2)
                    {
                        // add activity data
                        if (isOpen)
                        {
                            // add copy data
                            int count = 0;
                            Table.ForeachDynamicActivity(recoard =>
                            {
                                if (recoard.IsOpenTeam == 1)
                                {
                                    for (int d = 0; d < recoard.FuBenID.Length; d++)
                                    {
                                       
                                        var tabFuben = Table.GetFuben(recoard.FuBenID[d]);//GetFuben
                                        if (null == tabFuben) continue;
                                        var condition = tabFuben.EnterConditionId;
                                        var open = false;
                                        var enterLevel = 0;
                                        open = PlayerDataManager.Instance.CheckCondition(condition) == 0;
                                        if (open)
                                        {
                                            var conditionTab = Table.GetConditionTable(condition);
                                            for (int r = 0; r < conditionTab.ItemId.Length; r++)
                                            {
                                                if (conditionTab.ItemId[r] == 0)
                                                {
                                                    enterLevel = conditionTab.ItemCountMin[r];
                                                }
                                            }

                                            if (enterLevel == 0)
                                            {
                                                enterLevel = 1;
                                            }

                                            count++;
                                            TeamTargetChangeItemDataModel recItem = new TeamTargetChangeItemDataModel();
                                            recItem.targetItemGroupType = 1;
                                            recItem.targetItemId = recoard.Id;//活动此处 目标id是 活动表idActivityid  
                                            recItem.targetItemName = tabFuben.Name;
                                            recItem.isBelongIndex = 2;
                                            recItem.levelMini = enterLevel;
                                            var record = Table.GetClientConfig(103);
                                            if (null != record)
                                                recItem.levelMax = int.Parse(record.Value);
                                            if (count == 1)
                                            {
                                                recItem.select = true;
                                                Clone(teamTargetChangeDataModel.CurrentItemData, recItem);
                                            }
                                            data.Add(recItem);
                                            isHaveSecond = true;
                                            break;
                                        }
                                    }
                                }
                                return true;
                            });
                        }
                    }

                    //if (!isHaveSecond)
                    //    data.Remove (item);
                }

            }


            return data;
        }

        void SetDefaultLevel()
        {
            int characterMaxLevel = 0;
            var record = Table.GetClientConfig(103);
            if (null != record)
                characterMaxLevel = int.Parse(record.Value);

            var charcterLevel = PlayerDataManager.Instance.GetLevel();

            if (charcterLevel + 50 > characterMaxLevel)
                teamTargetChangeDataModel.LevelMax = characterMaxLevel;
            else
                teamTargetChangeDataModel.LevelMax = charcterLevel + 50;

            if (charcterLevel - 50 < 1)
                teamTargetChangeDataModel.LevelMini = 1;
            else
                teamTargetChangeDataModel.LevelMini = charcterLevel - 50;
        }

        void UpdateTeamTargetChangeLevel(int levelMax, int levelMini)
        {
            int characterMaxLevel = 0;
            var record = Table.GetClientConfig(103);
            if (null != record)
                characterMaxLevel = int.Parse(record.Value);

            if (levelMax > characterMaxLevel)
                teamTargetChangeDataModel.LevelMax = characterMaxLevel;
            else
                teamTargetChangeDataModel.LevelMax = levelMax;

            if (levelMini < 1)
                teamTargetChangeDataModel.LevelMini = 1;
            else
                teamTargetChangeDataModel.LevelMini = levelMini;
        }

        void GetTargetLevel(int belongIndex, int targetId)
        {
            switch (belongIndex)
            {
                case 0:
                    SetDefaultLevel();
                    return;
                case 1:

                    break;
                case 2:
                    break;
            }
        }

        void Clone(TeamTargetChangeItemDataModel dataModel, TeamTargetChangeItemDataModel otherModel)
        {
            dataModel.isBelongIndex = otherModel.isBelongIndex;
            dataModel.isOpen = otherModel.isOpen;
            dataModel.levelMax = otherModel.levelMax;
            dataModel.levelMini = otherModel.levelMini;
            dataModel.select = otherModel.select;
            dataModel.targetItemGroupType = otherModel.targetItemGroupType;
            dataModel.targetItemId = otherModel.targetItemId;
            dataModel.targetItemName = otherModel.targetItemName;
        }

        void OnClickTeamTargetChangeLevelPlus_Event(IEvent ievent)
        {
            var limitLevel = teamTargetChangeDataModel.CurrentItemData.levelMax;
            teamTargetChangeDataModel.CurrentItemData.levelMini += 1;
            if (teamTargetChangeDataModel.CurrentItemData.levelMini > limitLevel)
                teamTargetChangeDataModel.CurrentItemData.levelMini = limitLevel;
        }

        void OnClickTeamTargetChangeLevelSubStract_Event(IEvent ievent)
        {
            teamTargetChangeDataModel.CurrentItemData.levelMini -= 1;
            if (teamTargetChangeDataModel.CurrentItemData.levelMini < 1)
                teamTargetChangeDataModel.CurrentItemData.levelMini = 1;
        }

        void OnClickTeamTargetChangeLevelMaxPlus_Event(IEvent ievent)
        {
            var charcterLevel = 0;
            var record = Table.GetClientConfig(103);
            if (null != record)
                charcterLevel = int.Parse(record.Value);
            if (null != teamTargetChangeDataModel.CurrentItemData)
            {
                teamTargetChangeDataModel.CurrentItemData.levelMax += 1;
                if (teamTargetChangeDataModel.CurrentItemData.levelMax > charcterLevel)
                    teamTargetChangeDataModel.CurrentItemData.levelMax = charcterLevel;
            }
        }

        void OnClickTeamTargetChangeLevelMaxSubStract_Event(IEvent ievent)
        {
            var limitLevel = teamTargetChangeDataModel.CurrentItemData.levelMini;
            teamTargetChangeDataModel.CurrentItemData.levelMax -= 1;
            if (teamTargetChangeDataModel.CurrentItemData.levelMax < limitLevel)
                teamTargetChangeDataModel.CurrentItemData.levelMax = limitLevel;
        }

        void OnClickTeamTargetChangeConfirm(IEvent ievent)
        {
            var msg = NetManager.Instance.ChangetTeamTarget(teamTargetChangeDataModel.CurrentItemData.isBelongIndex,
                teamTargetChangeDataModel.CurrentItemData.targetItemId,
                teamTargetChangeDataModel.CurrentItemData.levelMini,
                teamTargetChangeDataModel.CurrentItemData.levelMax, PlayerDataManager.Instance.NoticeData.DungeonType);
            msg.SendAndWaitUntilDone();

            if (msg.State == ScorpionNetLib.MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.Error_ChangeTeamTargetFail_001)
                {
                    int dicId = 220133;
                    GameUtils.ShowHintTip(dicId);
                }
            }
        }

        void UpdateTeamTargetChangeNotify_Event(IEvent ievent)
        {
            var e = ievent as TeamTargetChangeNotify_Event;
            teamTargetChangeDataModel.CurrentItemData.isBelongIndex = e.type;
            teamTargetChangeDataModel.CurrentItemData.targetItemId = e.targetID;
            teamTargetChangeDataModel.CurrentItemData.levelMini = e.levelMini;
            teamTargetChangeDataModel.CurrentItemData.levelMax = e.levelMax;

            string maxLeve = string.Empty;
            var record = Table.GetClientConfig(103);
            if (null != record)
                maxLeve = record.Value;
            string desForm = string.Empty;
            desForm = GameUtils.GetDictionaryText(220125);

            teamTargetChangeDataModel.TargetDes = string.Format(desForm, 1, maxLeve);
            switch (e.type)
            {
                case 0:
                    teamTargetChangeDataModel.TargetName = "无";
                    var leMini = e.levelMini;
                    var leMax = e.levelMax;
                    if (leMini < 1)
                        leMini = 1;
                    if (leMax > int.Parse(maxLeve))
                        leMax = int.Parse(maxLeve);
                    if (e.levelMini > 1)
                        teamTargetChangeDataModel.TargetDes = string.Format(desForm, leMini, leMax);
                    break;
                case 1:
                    teamTargetChangeDataModel.TargetName = "副本";
                    if (e.targetID != 0)
                    {
                        var tab = Table.GetFuben(e.targetID);
                        if (null == tab) break;
                        teamTargetChangeDataModel.TargetName = tab.Name;
                        teamTargetChangeDataModel.TargetDes = string.Format(desForm, e.levelMini, e.levelMax);
                    }
                    break;
                case 2:
                    teamTargetChangeDataModel.TargetName = "活动";
                    if (e.targetID != 0)
                    {
                        var tab = Table.GetDynamicActivity(e.targetID);//GetDynamicActivity
                        if (null == tab) break;
                        teamTargetChangeDataModel.TargetName = tab.Name;
                        teamTargetChangeDataModel.TargetDes = string.Format(desForm, e.levelMini, e.levelMax);
                        /**
                        if (e.readTableId == 0)
                        {
                            var tab = Table.GetDynamicActivity(e.targetID);//GetDynamicActivity
                            if (null == tab) break;
                            teamTargetChangeDataModel.TargetName = tab.Name;
                            teamTargetChangeDataModel.TargetDes = string.Format(desForm, e.levelMini, e.levelMax);
                        }
                        else
                        {
                            var tab = Table.GetFuben(e.targetID);//GetDynamicActivity
                            if (null == tab) break;
                            teamTargetChangeDataModel.TargetName = tab.Name;
                            teamTargetChangeDataModel.TargetDes = string.Format(desForm, e.levelMini, e.levelMax);
                        }**/

                    }
                    break;
            }
            nowConfirmData.isBelongIndex = teamTargetChangeDataModel.CurrentItemData.isBelongIndex;
            nowConfirmData.targetItemId = teamTargetChangeDataModel.CurrentItemData.targetItemId;
            nowConfirmData.levelMax = teamTargetChangeDataModel.CurrentItemData.levelMax;
            nowConfirmData.levelMini = teamTargetChangeDataModel.CurrentItemData.levelMini;
            nowConfirmData.targetItemGroupType = teamTargetChangeDataModel.CurrentItemData.targetItemGroupType;
            if (DataModel.HasTeam && teamTargetChangeDataModel.CurrentItemData.isBelongIndex != 0)
            {
                ShowUIHintBoard board =
                    new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(220140), teamTargetChangeDataModel.TargetName));                        
                EventDispatcher.Instance.DispatchEvent(board);
            }
            
            if (null != PlayerDataManager.Instance.currentTeamTarget)
            {
                PlayerDataManager.Instance.currentTeamTarget.isBelongIndex = nowConfirmData.isBelongIndex;
                PlayerDataManager.Instance.currentTeamTarget.targetItemId = nowConfirmData.targetItemId;
            }
        }

        void UpdateTeamSearchList_Event(IEvent ievent)
        {
            TeamSearchList_Event e = ievent as TeamSearchList_Event;
            teamTargetChangeDataModel.TargetChangeList.Clear();

            teamTargetChangeDataModel.TargetChangeList = GetOriginalData(0, true);
            
            PullSearchList(0, -1);
        }

        void PullSearchList(int groupType, int targetid)
        {
            var msg = NetManager.Instance.StartCoroutine(SearchListBack(groupType, targetid));
        }
        IEnumerator SearchListBack(int groupType, int targetid)
        {
            var msg = NetManager.Instance.SearchTeamList(groupType, targetid, PlayerDataManager.Instance.GetLevel());
            yield return msg.SendAndWaitUntilDone();

            if (msg.State != MessageState.Timeout)
            {
                if (null != msg.Response && null != msg.Response.SearchList) CloneSearchList(msg.Response.SearchList);
            }
        }

        void CloneSearchList(List<TeamSearchItem> list)
        {
            for (int i = 0; i < teamTargetChangeDataModel.SearchTeamList.Count - 1; i++)
            {
                teamTargetChangeDataModel.SearchTeamList[i].isNull = false;
            }

            for (int r = 0; r < list.Count; r++)
            {
                if (null == list[r].Name) continue;

                teamTargetChangeDataModel.SearchTeamList[r].targetItemName = list[r].Name;
                teamTargetChangeDataModel.SearchTeamList[r].isBelongIndex = list[r].TeamGroupType;
                teamTargetChangeDataModel.SearchTeamList[r].characterId = list[r].CharacterId;
                teamTargetChangeDataModel.SearchTeamList[r].teamId = list[r].TeamID;
                teamTargetChangeDataModel.SearchTeamList[r].levelMax = list[r].RoleLevel;
                teamTargetChangeDataModel.SearchTeamList[r].levelMini = list[r].Profession;
                teamTargetChangeDataModel.SearchTeamList[r].StarNum = list[r].StarNum;

                //var cer = Table.GetCharacterBase(list[r].Profession);
                teamTargetChangeDataModel.SearchTeamList[r].career = GetLadderName(list[r].Ladder, list[r].Profession);
                teamTargetChangeDataModel.SearchTeamList[r].teamCount = list[r].TeamID;
                teamTargetChangeDataModel.SearchTeamList[r].ladder = GetLadderIconId(list[r].Ladder, list[r].Profession);
                teamTargetChangeDataModel.SearchTeamList[r].isNull = true;
            }
        }

        void UpdateTeamSearchListClick_Event(IEvent ievent)
        {
            TeamSearchListClick_Event ev = ievent as TeamSearchListClick_Event;


            var teamId = teamTargetChangeDataModel.SearchTeamList[ev.index].teamId;
            var otherChracterId = teamTargetChangeDataModel.SearchTeamList[ev.index].characterId;
            var e = new UIEvent_TeamFrame_Message(3, (ulong)teamId, PlayerDataManager.Instance.GetGuid());
            EventDispatcher.Instance.DispatchEvent(e);
            var msg = NetManager.Instance.TeamMessage(PlayerDataManager.Instance.GetGuid(), 3, (ulong)teamId, (ulong)otherChracterId);
            msg.SendAndWaitUntilDone();
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220100));
        }

        void PullApplyList(IEvent ievent)
        {
            var msg = NetManager.Instance.StartCoroutine(ApplyListBack());
        }
        IEnumerator ApplyListBack()
        {
            teamTargetChangeDataModel.isApplyTeamList = false;
            teamTargetChangeDataModel.ApplyList.Clear();
            var msg = NetManager.Instance.TeamSearchApplyList(PlayerDataManager.Instance.GetGuid());
            yield return msg.SendAndWaitUntilDone();
            if (msg.State != MessageState.Timeout)
            {
                if (msg.ErrorCode == (int)ErrorCodes.Error_SearchApplyListFail_001)
                {
                    var dicId = 220134;
                    GameUtils.ShowHintTip(dicId);
                }
                if (msg.Response != null && msg.Response.SearchList != null)
                {
                    var list = msg.Response.SearchList;
                    for (int i = 0; i < list.Count; i++)
                    {

                        if (null != list && list.Count > 0)
                        {
                            var item = new TeamApplyListItemModel();

                            if (i < list.Count)
                            {
                                item.isNull = true;
                                //if (list[i].Profession < 3)
                                //{
                                //    item.profession = list[i].Profession;
                                //    var pro = Table.GetCharacterBase(item.profession);
                                //    item.professionName = pro.Name;
                                //}
                                //else
                                //{
                                //    Logger.Error("The current game does not have this Profession;TeamFrameController:2207");
                                //}
                                item.professionName = GetLadderName(list[i].Ladder, list[i].Profession);
                                item.characterName = list[i].Name;
                                item.level = list[i].RoleLevel;
                                item.characterId = list[i].CharacterId;
                                item.ladder = GetLadderIconId(list[i].Ladder, list[i].Profession);
                                item.StarNum = list[i].StarNum;
                            }
                            else
                            {
                                item.isNull = false;
                            }

                            teamTargetChangeDataModel.ApplyList.Add(item);
                        }
                    }

                }
            }
            if (teamTargetChangeDataModel.ApplyList.Count <= 0)
            {
                teamTargetChangeDataModel.isApplyTeamList = true;
            }
        }

        void ClearApplyList()
        {
            teamTargetChangeDataModel.ApplyList.Clear();
            teamTargetChangeDataModel.isApplyTeamList = true;
        }
        void TeamSearchRefreshClick(IEvent ievent)
        {
            PullSearchList(teamTargetChangeDataModel.CurrentItemData.isBelongIndex, teamTargetChangeDataModel.CurrentItemData.targetItemId);
        }

        void AutoMatchClick(IEvent ievent)
        {

            int isHaveTeam = DataModel.HasTeam == true ? 1 : 0;
            var msg = NetManager.Instance.AutoMatchBegin(isHaveTeam, teamTargetChangeDataModel.CurrentItemData.isBelongIndex, teamTargetChangeDataModel.CurrentItemData.targetItemId);
            msg.SendAndWaitUntilDone();
        }

        void TeamApplyItemCellClick(IEvent ievent)
        {
            TeamApplyItemCellClick_Event even = ievent as TeamApplyItemCellClick_Event;
            for (int i = 0; i < teamTargetChangeDataModel.ApplyList.Count; i++)
            {
                var ite = teamTargetChangeDataModel.ApplyList[i];
                if (null != ite && i == even.index)
                {
                    var toId = (ulong)ite.characterId;
                    var e = new Event_TeamAcceptJoin(toId);
                    EventDispatcher.Instance.DispatchEvent(e);
                    break;
                }
            }
        }

        void TeamInviteNearbyClick(IEvent ievent)
        {
            CloneInviteItem(teamTargetChangeDataModel.InviteNearByList);
        }

        void TeamInviteFriendsClick(IEvent ievent)
        {
            CloneInviteItem(teamTargetChangeDataModel.InviteFriendsList);
        }

        void TeamInviteBattleUnionClick(IEvent ievent)
        {
            CloneInviteItem(teamTargetChangeDataModel.InviteBattleUnionList);
        }

        void CloneInviteItem(ObservableCollection<TeamInviteItemModel> list)
        {
            bool isHave = false;
            for (int i = 0; i < teamTargetChangeDataModel.InviteList.Count; i++)
            {
                var item = teamTargetChangeDataModel.InviteList[i];
                if (i < list.Count && null != list[i])
                {
                    item.isNull = list[i].isNull;
                    item.characterId = list[i].characterId;
                    item.characterName = list[i].characterName;
                    item.levelStr = list[i].levelStr;
                    item.professionName = list[i].professionName;
                    item.profession = list[i].profession;
                    item.ladder = list[i].ladder;
                    item.StarNum = list[i].StarNum;
                    if (item.isNull != false) isHave = true;
                }
                else
                {
                    item.isNull = false;
                    isHave = false;
                }
            }
            teamTargetChangeDataModel.IsHaveInvite = !isHave;
        }

        void TeamInviteClick(IEvent ievent)
        {
            var myselfChracterId = PlayerDataManager.Instance.GetGuid();
            // 拉取附近数据
            ApplySceneObj();

            IControllerBase conCtrler = UIManager.GetInstance().GetController(UIConfig.FriendUI);
            if (conCtrler == null)
                return;
            conCtrler.CallFromOtherClass("NewApplyPlayerheadMsg", new[] { (object)0 });
            // 拉取好友数据
            var frindsList = PlayerDataManager.Instance.GetFriendTop15(1);
            for (int i = 0; i < teamTargetChangeDataModel.InviteFriendsList.Count; i++)
            {
                var item = teamTargetChangeDataModel.InviteFriendsList[i];
                item.isNull = false;

                var cou = PlayerDataManager.Instance.GetFriendTop15(1).Count;
                if (i < cou)
                {
                    item.isNull = true;
                    item.characterId = (int)frindsList[i].Guid;
                    item.characterName = frindsList[i].Name;
                    item.levelStr = frindsList[i].Level.ToString() + "级";
                    item.profession = frindsList[i].TypeId;
                    item.StarNum = frindsList[i].StarNum;
                    //var pro = Table.GetCharacterBase(frindsList[i].TypeId);
                    item.professionName = GetLadderName(frindsList[i].Ladder, frindsList[i].TypeId);
                    item.ladder = GetLadderIconId(frindsList[i].Ladder, frindsList[i].TypeId);
                }
            }

            // 拉取战盟数据
            if (PlayerDataManager.Instance.BattleUnionDataModel.MyUnion.UnionID <= 0)
            {
                int a = 0;
            }
            else
            {
                FightAllianceFrameCtrler fightCtrler = UIManager.GetInstance().GetController(UIConfig.BattleUnionUI) as FightAllianceFrameCtrler;
                if (fightCtrler == null)
                    return;
                fightCtrler.CallFromOtherClass("MainUIInition",null);//初始化战盟
                fightCtrler.CallFromOtherClass("CanRenewalInfo", new[] { (object)0 });//战盟信息刷新

                var battleList = PlayerDataManager.Instance.mUnionMembers;

                var tmp = battleList.OrderByDescending(i => i.Value.Level);

                for (int i = 0; i < teamTargetChangeDataModel.InviteBattleUnionList.Count; i++)
                {
                    var item = teamTargetChangeDataModel.InviteBattleUnionList[i];
                    item.isNull = false;
                }

                int r = 0;
                foreach (var bat in tmp)
                {
                    if (bat.Value.Online == 0 || bat.Value.ID == myselfChracterId) continue;
                    teamTargetChangeDataModel.InviteBattleUnionList[r].isNull = true;
                    teamTargetChangeDataModel.InviteBattleUnionList[r].characterName = bat.Value.Name;
                    teamTargetChangeDataModel.InviteBattleUnionList[r].levelStr = bat.Value.Level.ToString() + "级";
                    teamTargetChangeDataModel.InviteBattleUnionList[r].professionName = GetLadderName(bat.Value.RebornLadder, bat.Value.CareerId);//bat.Value.Career;
                    teamTargetChangeDataModel.InviteBattleUnionList[r].characterId = (int)bat.Value.ID;
                    teamTargetChangeDataModel.InviteBattleUnionList[r].profession = bat.Value.CareerId;
                    //teamTargetChangeDataModel.InviteBattleUnionList[r].StarNum = ;
                    teamTargetChangeDataModel.InviteBattleUnionList[r].ladder = GetLadderIconId(bat.Value.RebornLadder, bat.Value.CareerId);
                    r++;
                }
            }

        }

        void TeamInviteClickCell(IEvent ievent)
        {
            TeamInviteClickCell_Event eve = ievent as TeamInviteClickCell_Event;
            for (int i = 0; i < teamTargetChangeDataModel.InviteList.Count; i++)
            {
                if (eve.index == i)
                {
                    var item = teamTargetChangeDataModel.InviteList[i];
                    if (null != item && item.isNull == true && item.characterId > 0)
                        EventDispatcher.Instance.DispatchEvent(new Event_TeamInvitePlayer((ulong)item.characterId));
                }
            }
        }

        void ChatTeamByTargetLink(IEvent ievent)
        {
            if (nowConfirmData.isBelongIndex == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(220138)));
                return;
            }
            TeamWorldSpeakNewEvent speak = new TeamWorldSpeakNewEvent();
            EventDispatcher.Instance.DispatchEvent(speak);

            ChatTeamByTargetEvent eve = new ChatTeamByTargetEvent(DataModel.TeamId,
                nowConfirmData.isBelongIndex,
                nowConfirmData.targetItemId,
                nowConfirmData.levelMini,
                nowConfirmData.levelMax,
                11);//新频道 组队
            EventDispatcher.Instance.DispatchEvent(eve);
        }

        void TeamFaceItemEv(IEvent ievent)
        {
            TeamFaceItemEvent ev = ievent as TeamFaceItemEvent;
            TeamFaceItemModel ite = new TeamFaceItemModel();
            ite.characterId = ev.characterId;
            ite.sceneGuid = ev.sceneGuid;

            addMemberSceneGuid(ite);
            TeamFaceItemEv1(null);
        }
        void TeamFaceItemEv1(IEvent ievent)
        {
            var selfSceneGuid = (ulong)0;
            var characterId = (ulong)0;
            characterId = PlayerDataManager.Instance.CharacterGuid;
            //selfSceneGuid = GetMemberSceneGuid(characterId);
            selfSceneGuid = PlayerDataManager.Instance.mInitBaseAttr.SceneGuid;

            teamTargetChangeDataModel.teamMem1State = 101;
            teamTargetChangeDataModel.teamMem2State = 101;
            teamTargetChangeDataModel.teamMem3State = 101;
            teamTargetChangeDataModel.teamMem4State = 101;
            teamTargetChangeDataModel.teamMem5State = 101;
            int memCount = 0;
            // 102 同场景，103 不同场景，101不在线
            for (int i = 0; i < DataModel.TeamList.Count; i++)
            {
                var id = DataModel.TeamList[i].Guid;
                var scenid = GetMemberSceneGuid(id);
                if (id <= 0) continue;
                if (0 == i)
                {
                    if (selfSceneGuid == scenid)
                    {
                        teamTargetChangeDataModel.teamMem1State = 102;
                    }
                    else
                    {
                        if (!DataModel.TeamList[i].IsLeave)
                        {
                            if (scenid == selfSceneGuid)
                            {
                                teamTargetChangeDataModel.teamMem1State = 102;
                            }
                            else
                            {
                                if (scenid > 0)
                                    teamTargetChangeDataModel.teamMem1State = 103;
                                else
                                    teamTargetChangeDataModel.teamMem1State = 101;
                            }
                        }
                        else
                            teamTargetChangeDataModel.teamMem1State = 101;
                    }
                    if (characterId == id) teamTargetChangeDataModel.teamMem1State = 102;
                }

                else if (1 == i)
                {
                    if (!DataModel.TeamList[i].IsLeave)
                    {
                        if (scenid == selfSceneGuid)
                        {
                            teamTargetChangeDataModel.teamMem2State = 102;
                        }
                        else
                        {
                            if (scenid > 0)
                                teamTargetChangeDataModel.teamMem2State = 103;
                            else
                                teamTargetChangeDataModel.teamMem2State = 101;
                        }
                    }
                    else
                        teamTargetChangeDataModel.teamMem2State = 101;

                    if (characterId == id) teamTargetChangeDataModel.teamMem2State = 102;
                }

                else if (2 == i)
                {
                    if (!DataModel.TeamList[i].IsLeave)
                    {
                        if (scenid == selfSceneGuid)
                        {
                            teamTargetChangeDataModel.teamMem3State = 102;
                        }
                        else
                        {
                            if (scenid > 0)
                                teamTargetChangeDataModel.teamMem3State = 103;
                            else
                                teamTargetChangeDataModel.teamMem3State = 101;
                        }
                    }
                    else
                        teamTargetChangeDataModel.teamMem3State = 101;

                    if (characterId == id) teamTargetChangeDataModel.teamMem3State = 102;
                }

                else if (3 == i)
                {
                    if (!DataModel.TeamList[i].IsLeave)
                    {
                        if (scenid == selfSceneGuid)
                        {
                            teamTargetChangeDataModel.teamMem4State = 102;
                        }
                        else
                        {
                            if (scenid > 0)
                                teamTargetChangeDataModel.teamMem4State = 103;
                            else
                                teamTargetChangeDataModel.teamMem4State = 101;
                        }
                    }
                    else
                        teamTargetChangeDataModel.teamMem4State = 101;

                    if (characterId == id) teamTargetChangeDataModel.teamMem4State = 102;
                }

                else if (4 == i)
                {
                    if (!DataModel.TeamList[i].IsLeave)
                    {
                        if (scenid == selfSceneGuid)
                        {
                            teamTargetChangeDataModel.teamMem5State = 102;
                        }
                        else
                        {
                            if (scenid > 0)
                                teamTargetChangeDataModel.teamMem5State = 103;
                            else
                                teamTargetChangeDataModel.teamMem5State = 101;
                        }
                    }
                    else
                        teamTargetChangeDataModel.teamMem5State = 101;
                    if (characterId == id) teamTargetChangeDataModel.teamMem5State = 102;
                }

            }

            if (teamTargetChangeDataModel.teamMem5State == 102) memCount++;
            if (teamTargetChangeDataModel.teamMem4State == 102) memCount++;
            if (teamTargetChangeDataModel.teamMem3State == 102) memCount++;
            if (teamTargetChangeDataModel.teamMem2State == 102) memCount++;
            if (teamTargetChangeDataModel.teamMem1State == 102) memCount++;
            var tabExp = Table.GetExpInfo(memCount) as ExpInfoRecord;
            string form = Table.GetDictionary(220132).Desc[0];
            float inde = tabExp.TeamCountExpProp / 1000;
            string va = string.Empty;
            if (inde > 0)
                va = (inde - 10) * 10 + "";
            else
                va = "0";
            teamTargetChangeDataModel.teamMebDes = string.Format(form, va);
        }

        ulong GetMemberSceneGuid(ulong guid)
        {
            ulong sceneGuid = 0;
            bool isHave = false;
            for (int r = 0; r < teamTargetChangeDataModel.teamFaceList.Count; r++)
            {
                if (guid == teamTargetChangeDataModel.teamFaceList[r].characterId)
                {
                    sceneGuid = teamTargetChangeDataModel.teamFaceList[r].sceneGuid;
                    isHave = true;
                    break;
                }
            }
            if (!isHave && guid == PlayerDataManager.Instance.Guid)
                sceneGuid = PlayerDataManager.Instance.mInitBaseAttr.SceneGuid;

            return sceneGuid;
        }

        void addMemberSceneGuid(TeamFaceItemModel item)
        {
            bool isHave = false;
            for (int r = 0; r < teamTargetChangeDataModel.teamFaceList.Count; r++)
            {
                if (item.characterId == teamTargetChangeDataModel.teamFaceList[r].characterId)
                {
                    isHave = true;
                    teamTargetChangeDataModel.teamFaceList[r].sceneGuid = item.sceneGuid;
                    break;
                }
            }

            if (!isHave)
                teamTargetChangeDataModel.teamFaceList.Add(item);
        }

        void ApplyListClearEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ApplyListClear());
        }
        void ClickMissionTabEvent(IEvent ievent)
        {
            var e = ievent as Event_MissionTabClick;
            if (e == null)
            {
                return;
            }

            DataModel.TeamBackGround = e.nIndex;
        }
        IEnumerator ApplyListClear()
        {
            var msg = NetManager.Instance.TeamApplyListClear(PlayerDataManager.Instance.GetGuid());
            yield return msg.SendAndWaitUntilDone();

            if (msg.ErrorCode == (int)ErrorCodes.OK)
            {
                // to add applylistBtn click codes ...
                ClearApplyList();
            }
            else if (msg.ErrorCode == (int)ErrorCodes.Error_ClearApplyListFail_001)
            {
                int dicId = 220135;
                GameUtils.ShowHintTip(dicId);
            }

            yield break;
        }

        int GetLadderIconId(int ladder, int pro)
        {
            int defaultIconId = 0;
            var tabTrans = Table.GetTransmigration(ladder);
            var tabActor = Table.GetActor(pro);
            if (null != tabTrans && null != tabActor)
            {
                switch (pro)
                {
                    case 0: // 剑士
                        defaultIconId = tabTrans.zsRebornIconSquare;
                        break;
                    case 1: // 法师
                        defaultIconId = tabTrans.fsRebornIconSquare;
                        break;
                    case 2: // 弓箭手
                        defaultIconId = tabTrans.gsRebornIconSquare;
                        break;
                    //case 3: // 游侠
                    //    defaultIconId = tabTrans.fsRebornIconSquare;
                    //    break;
                }
            }

            return defaultIconId;
        }

        string GetLadderName(int ladder, int pro)
        {
            string defaultName = "";
            var tabTrans = Table.GetTransmigration(ladder);
            //var tabActor = Table.GetActor(pro);
            if (null != tabTrans)
            {
                switch (pro)
                {
                    case 0: // 剑士
                        defaultName = tabTrans.zsRebornName;
                        break;
                    case 1: // 法师
                        defaultName = tabTrans.fsRebornName;
                        break;
                    case 2: // 弓箭手
                        defaultName = tabTrans.gsRebornName;
                        break;
                    //case 3: // 游侠
                    //    defaultName = tabTrans.gsRebornName;
                    //    break;
                }
            }

            return defaultName;
        }

        void UIEvent_MainUITeamFrame(IEvent ievent)
        {
            isTeamUIChange = true;
        }

        void TeamChangeEven(IEvent ievent)
        {
            isTeamUIChange = false;
        }

        void RefreshRedSpot(IEvent ievent)
        {
            TeamApplyListSyncEvent e = ievent as TeamApplyListSyncEvent;
            teamTargetChangeDataModel.RedSpot = e.state;
            if (!IsLeader())
                teamTargetChangeDataModel.RedSpot = false;
            else
            {
                if (null != DataModel.TeamList[4] && DataModel.TeamList[4].Level > 0)
                    teamTargetChangeDataModel.RedSpot = false;
            }
        }

        void OpenTeamFromOther(IEvent ev)
        {
            OpenTeamFromOtherEvent e = ev as OpenTeamFromOtherEvent;
            DataModel.OpenFromOther = e.Other;
            if (DataModel.OpenFromOther != 0)
            {
                UpdateTeamSearchList_Event(null);
            }
        }
    }
}