using ScriptManager;
using GameUI;
using ObjCommand;
using System;
#region using

using System.Collections;
using System.Collections.Generic;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

public class GameControl : MonoBehaviour
{
    private bool btest = false;
    private static readonly Dictionary<int, int> AutoSkillBuff = new Dictionary<int, int>
    {
        {7, 7},
        {111, 114},
        {208, 210},
        {209, 211}
    };

    public static CommandExecuter Executer = new CommandExecuter();
    public static GameControl Instance;
    private static int systemCount = 0;
    // Update is called once per frame
    private bool btestfps = false;
    public Vector2 JoyStickDirection = Vector2.up;
    public bool JoyStickPressed = false;
    private bool mMouseButtonUp = true;
#if UNITY_EDITOR
    public string MoviePath = "Movie/YongZheDaLu_Movie";
#endif
    public ObjCharacter TargetObj { get; set; }
    //战旗表
    private Dictionary<int, int> WarFlagDic = new Dictionary<int, int>();
    //矿脉表
    private Dictionary<int, int> LodeDic = new Dictionary<int, int>();
    //纪念碑
    private Dictionary<int, int> MonumentDic = new Dictionary<int, int>();
    private bool IsInit = false; 
    private void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        enabled = false;
        Instance = this;
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    public bool CheckControlType(SkillItemDataModel skill, BuffManager buffManager)
    {
        var buffdata = buffManager.GetBuffData();
        var count = buffdata.Count;
        var controlType = skill.ControlType;
        for (var i = 0; i < count; i++)
        {
            var buff = buffdata[i];

            var tbBuff = Table.GetBuff(buff.BuffTypeId);
            for (var j = 0; j < tbBuff.effectid.Length; j++)
            {
                if (tbBuff.effectid[j] == 9)
                {
                    if (BitFlag.GetAnd(tbBuff.effectparam[j, 1], controlType) > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public ErrorCodes CheckSkill(int skillId, bool isAuto = false)
    {
        var player = ObjManager.Instance.MyPlayer;
        if (null == player)
        {
            Logger.Error("CheckSkill ObjManager.Instance.MyPlayer is null");
            return ErrorCodes.Unknow;
        }


        if (player.IsInSafeArea())
        {
            return ErrorCodes.Error_SafeArea;
        }

        if (player.Dead)
        {
            return ErrorCodes.Error_CharacterDie;
        }

// 	    if (player.GetCurrentStateName() == OBJ.CHARACTER_STATE.DIZZY)
// 	    {
// 			return ErrorCodes.Error_Dizzy;
// 	    }

        if (isAuto)
        {
            var buffId = -1;
            if (AutoSkillBuff.TryGetValue(skillId, out buffId))
            {
                var buffManager = ObjManager.Instance.MyPlayer.GetBuffManager();
                if (buffManager != null && buffManager.HasBuff(buffId))
                {
                    return ErrorCodes.Unknow;
                }
            }
        }

        var PlayerDataModel = PlayerDataManager.Instance.PlayerDataModel;
        //CD
        var skillStates = PlayerDataManager.Instance.PlayerDataModel.SkillData.SkillStates;

        if (skillStates == null)
        {
            Logger.Error("CheckSkill SkillStates is null");
            return ErrorCodes.Unknow;
        }
        SkillStateData skillState = null;
        var tbSkill = Table.GetSkill(skillId);
        if (null == tbSkill)
        {
            return ErrorCodes.Unknow;
        }

        if (PlayerDataManager.Instance.PlayerDataModel.SkillData.EquipSkills == null)
        {
            Logger.Error("CheckSkill EquipSkills is null");
            return ErrorCodes.Unknow;
        }

        var isSkipCd = false;
        SkillItemDataModel skillItem = null;
        var skillLevel = 0;
        if (!PlayerDataManager.Instance.mAllSkills.TryGetValue(skillId, out skillItem))
        {
            return ErrorCodes.Unknow;
        }

        //MP
        var needMp = StringConvert.Level_Value_Ref(tbSkill.NeedMp, skillLevel);
        if (needMp != -1 && PlayerDataModel.Attributes.MpNow < needMp)
        {
            return ErrorCodes.Error_MpNoEnough;
        }
        //HP
        var needHP = StringConvert.Level_Value_Ref(tbSkill.NeedHp, skillLevel);
        if (needHP != -1 && PlayerDataModel.Attributes.HpNow < needHP)
        {
            return ErrorCodes.Error_HpNoEnough;
        }

        if (skillStates.TryGetValue(skillId, out skillState))
        {
            if (skillState.State == SkillState.Send)
            {
                return ErrorCodes.Error_SkillNoCD;
            }
        }
        if (PlayerDataManager.Instance.PlayerDataModel.SkillData.CommonCoolDown > 0 && tbSkill.Type != 3) //xp技能忽视cd
        {
            return ErrorCodes.Error_SkillNoCD;
        }

        if ((skillItem.CoolDownTime > 0 && skillItem.ChargeLayerTotal <= 1) || skillItem.ChargeLayer == 0)
        {
            return ErrorCodes.Error_SkillNoCD;
        }

        if (0 != tbSkill.IsEquipCanUse)
        {
            var euipList = PlayerDataManager.Instance.PlayerDataModel.EquipList;
            if (null == euipList)
            {
                return ErrorCodes.Error_SkillNeedWeapon;
            }
            if (euipList.Count <= (int) eEquipType.WeaponMain)
            {
                return ErrorCodes.Error_SkillNeedWeapon;
            }
            var item = euipList[(int) eEquipType.WeaponMain];
            if (null == item)
            {
                return ErrorCodes.Error_SkillNeedWeapon;
            }
            if (-1 == item.ItemId)
            {
                return ErrorCodes.Error_SkillNeedWeapon;
            }
        }

        //anger
        var needAnger = StringConvert.Level_Value_Ref(tbSkill.NeedAnger, skillLevel);


        //妫€鏌uff闄愬埗鏁堟灉
        if (CheckControlType(skillItem, player.GetBuffManager()))
        {
            return ErrorCodes.Error_SkillNotUse;
        }
        return ErrorCodes.OK;
    }

    public bool CheckSkillCondition(int skillId)
    {
        return false;
    }

    private IEnumerator ExecuteCommand()
    {
        yield return new WaitForSeconds(0.2f);
        var command = Executer.GetCurrentCommand();
        if (null != command)
        {
            command.OnBegin();
        }
    }

    public static BaseCommand FlyToSceneCommand(int sceneId)
    {
        return new FlyToSceneCommand(sceneId);
    }

    public static float GetSkillReleaseDistance(SkillRecord data)
    {
        //SkillTargetType type = (SkillTargetType)data.TargetType;
        var type = (SkillTargetType) ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetType);
        switch (type)
        {
            case SkillTargetType.SELF:
                return 0;
            case SkillTargetType.SINGLE:
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam1);
                //return data.TargetParam[0];
            }
            case SkillTargetType.CIRCLE:
                //return data.TargetParam[0];
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam1);
            }
            case SkillTargetType.SECTOR:
                //return data.TargetParam[0];
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam1);
            }
            case SkillTargetType.RECT:
                //return data.TargetParam[1];
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam2);
            }
            case SkillTargetType.TARGET_CIRCLE:
                //return data.TargetParam[1];
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam2);
            }
            case SkillTargetType.TARGET_RECT:
                //return data.TargetParam[2];
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam3);
            }
            case SkillTargetType.TARGET_SECTOR:
                //return data.TargetParam[2];
            {
                return ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetParam3);
            }
            default:
            {
                Logger.Warn("(SkillTargetType)[{0}] not Find ", type);
                return 0;
            }
        }
    }

    public static BaseCommand   GoToCommand(int sceneId, float x, float y, float offset = 0.5f)
    {
        BaseCommand command = null;
        if (sceneId == GameLogic.Instance.Scene.SceneTypeId)
        {
            command = new MoveCommand(ObjManager.Instance.MyPlayer, new Vector3(x, 0, y), offset);
        }
        else
        {
            command = new GoToCommand(sceneId, new Vector3(x, 0, y), offset);
        }
        return command;
    }

    public void MoveDirection(Vector2 dir)
    {
        Executer.Stop();
        var player = ObjManager.Instance.MyPlayer;
        if (null == player)
        {
            return;
        }

        var e = new MapSceneCancelPath();
        EventDispatcher.Instance.DispatchEvent(e);

        var camera = GameLogic.Instance.MainCamera;
        var v = camera.gameObject.transform.rotation.eulerAngles;
        var f = -Mathf.Deg2Rad*v.y;
        dir = new Vector2(dir.x*Mathf.Cos(f) - dir.y*Mathf.Sin(f), dir.x*Mathf.Sin(f) + dir.y*Mathf.Cos(f));
        JoyStickDirection = dir;

        player.LeaveAutoCombat();

        player.MoveDirection(dir);
    }

    public bool MoveTo(Vector3 vec)
    {
        var player = ObjManager.Instance.MyPlayer;
        if (null == player)
        {
            return false;
        }
        EventDispatcher.Instance.DispatchEvent(new RestoreMainUIMenu());
        player.LeaveAutoCombat();
        Executer.Stop();
        if (player.IsAttakNoMove())
        {
            Executer.PushCommand(new MoveCommand(player, vec));
            return true;
        }
        return player.MoveTo(vec, 0.05f, false);
    }

    /// <summary>
    ///     鏀诲嚮鎸夐挳
    /// </summary>
    /// <param name="skillId">鎶€鑳絀d</param>
    /// <returns></returns>
    public bool OnAttackBtnClick(int skillId, bool selectTarget = true)
    {
        Logger.Info("OnAttackBtnClick SkillID = " + skillId);
        PlatformHelper.Event("Skill", "Manual", skillId);
        var myself = ObjManager.Instance.MyPlayer;

        //琛ㄦ牸鏁版嵁
        var data = Table.GetSkill(skillId);
        if (data == null)
        {
            return false;
        }

        if (selectTarget)
        {
            TargetObj = ObjManager.Instance.SelectTargetForSkill(myself, data);
        }

        //鐩爣绫诲瀷
        var targetType = (SkillTargetType) ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetType);

        if (targetType == SkillTargetType.SELF ||
            targetType == SkillTargetType.CIRCLE)
        {
            return UseSkill(myself.GetObjId(), skillId);
        }
        if (targetType == SkillTargetType.SECTOR)
        {
            var targetObj = TargetObj;
            if (targetObj != null)
            {
                myself.FaceTo(targetObj.Position);
                return UseSkill(myself.GetObjId(), skillId, targetObj.GetObjId());
            }
            return UseSkill(myself.GetObjId(), skillId);
        }
        if (targetType == SkillTargetType.RECT)
        {
            var targetObj = TargetObj;
            if (targetObj != null)
            {
                myself.FaceTo(targetObj.Position);
                return UseSkill(myself.GetObjId(), skillId);
            }
            return UseSkill(myself.GetObjId(), skillId);
        }
        if (targetType == SkillTargetType.SINGLE ||
            targetType == SkillTargetType.TARGET_CIRCLE ||
            targetType == SkillTargetType.TARGET_RECT ||
            targetType == SkillTargetType.TARGET_SECTOR)
        {
            var targetObj = TargetObj;
            if (targetObj == null || targetObj.Dead)
            {
                targetObj = ObjManager.Instance.SelectNearestCharacter(ObjManager.Instance.MyPlayer.Position,
                    character => !character.Dead && ObjManager.Instance.MyPlayer.IsMyEnemy(character));
            }

            if (null == targetObj || targetObj.Dead)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(713));
                return false;
            }

            return SelectTarget(targetObj.gameObject, skillId);
        }
        Logger.Info("Unknow target type[{0}]", targetType);
        return false;
    }

    public void OnClickEvent(int idx, int arg1)
    {
        var myself = ObjManager.Instance.MyPlayer;
        if (myself.IsAutoFight())
        {
            myself.AutoCombatSkill();
        }

        Executer.Stop();

        // 鏅€氭敾鍑?
        if (0 == idx)
        {
            var skillId = PlayerDataManager.Instance.GetNormalSkill(true);
            OnAttackBtnClick(skillId);
        }
        // 浣跨敤鎶€鑳?
        else if (1 == idx)
        {
            OnAttackBtnClick(arg1);
        }
        // 鍒囨崲鏀诲嚮/闈炴敾鍑荤姸鎬?
        else if (2 == idx)
        {
            OnSwitchBtnClick();
        }
    }

    private void OnDestroy()
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

    public void OnLoadSceneOver()
    {
        enabled = true;
        StartCoroutine(ExecuteCommand());

        GameUtils.RecordKeyPoint("EnterGameOver", 28);
    }

    private void OnSwitchBtnClick()
    {
        Logger.Info("OnSwitchBtnClick");
    }

    public void OnTouchRelease()
    {
        var player = ObjManager.Instance.MyPlayer;
        if (null != player)
        {
            player.SendMoveToServer(true);
        }
    }

    public bool SelectTarget(GameObject gameObject, int skill = -1)
    {
        //null鍒ゆ柇
        if (null == gameObject)
        {
            return false;
        }

        //鑾峰緱涓昏鑷繁
        var myself = ObjManager.Instance.MyPlayer;
        if (null == myself)
        {
            return false;
        }

        //鐩爣寰楁槸涓猳bj
        var obj = gameObject.GetComponent<ObjBase>();
        if (null == obj)
        {
            return false;
        }

        if (skill == -1)
        {
            TargetCharacter(obj);
        }
        //鍋滄褰撳墠鐨勬寚浠?
        //Executer.Stop();

        //璁＄畻鑷繁璺濈鐩爣鐨勮窛绂?
        var distance = Vector3.Distance(obj.Position, myself.Position);

        //榛樿鎶€鑳?
        var skillId = skill;
        if (skillId == -1)
        {
            skillId = PlayerDataManager.Instance.GetNormalSkill();
        }

        if (obj.IsCharacter() && myself.IsMyEnemy(obj as ObjCharacter))
        {
			if (obj.GetObjType() == OBJ.TYPE.NPC || obj.GetObjType() == OBJ.TYPE.RETINUE)
            {
//濡傛灉鏄痭pc	
                var npc = obj as ObjNPC;
                if (npc == null)
                {
                    return false;
                }
                if (!npc.CanBeInteractive())
                {
                    return false;
                }
            }
            var character = obj as ObjCharacter;

            if (character.Dead)
            {
                return false;
            }

            //瀵瑰叾閲婃斁鎶€鑳?
            var data = Table.GetSkill(skillId);
            //鐩爣绫诲瀷
            var targetType = (SkillTargetType) ObjMyPlayer.GetSkillData_Data(data, eModifySkillType.TargetType);

            //涓嶉渶瑕佺洰鏍囩殑鎶€鑳?
            if (targetType == SkillTargetType.SELF ||
                targetType == SkillTargetType.CIRCLE ||
                targetType == SkillTargetType.SECTOR ||
                targetType == SkillTargetType.RECT)
            {
                //浣跨敤鎶€鑳?
                return UseSkill(myself.GetObjId(), skillId, character.GetObjId());
            } //闇€瑕佺洰鏍囩殑鎶€鑳?
            if (targetType == SkillTargetType.SINGLE ||
                targetType == SkillTargetType.TARGET_CIRCLE ||
                targetType == SkillTargetType.TARGET_RECT ||
                targetType == SkillTargetType.TARGET_SECTOR)
            {
                var maxSkillDistance = GetSkillReleaseDistance(data);
                if (maxSkillDistance - 0.5 < 0.0f)
                {
                    maxSkillDistance = 0.5f;
                }

                if (distance > maxSkillDistance - 0.5)
                {
//璺濈涓嶅
                    var offset = maxSkillDistance - 1.0f;
                    if (offset < 0.0f)
                    {
                        offset = 0.1f;
                    }
                    Executer.PushCommand(new MoveCommand(myself, character.Position, offset));
                    Executer.PushCommand(new AttackCommand(myself.GetObjId(), skillId, character.GetObjId()));
                }
                else
                {
                    TargetObj = character;
                    PlayerDataManager.Instance.SetSelectTargetData(TargetObj, 3);
                    ObjManager.Instance.MyPlayer.FaceTo(character.Position);
                    return UseSkill(myself.GetObjId(), skillId, character.GetObjId());
                }
            }
            else
            {
                Logger.Error("Unknow skill target type = {0}", targetType);
            }
        }
        else if (obj.GetObjType() == OBJ.TYPE.NPC)
        {
            var npc = obj as ObjNPC;

            if (!npc.CanBeInteractive())
            {
                return false;
            }
            if (npc.TableNPC.NpcType == (int)eNpcType.PickUpNpc)
            {
                if (distance > GameSetting.Instance.MaxDistance_NPC)
                {
                    var command = new MoveCommand(myself, npc.Position, GameSetting.Instance.MaxDistance_NPC);
                    Executer.PushCommand(command);
                    return false;
                }
                else
                {
                    {
                        if (!IsInit)
                        {
                            Table.ForeachWarFlag(tb =>
                            {
                                WarFlagDic.Add(tb.Id, tb.FlagModel);
                                return true;
                            });
                            Table.ForeachLode(tb =>
                            {
                                if (tb.Id < 100000)
                                {
                                    LodeDic.Add(tb.Id, tb.NpcId);  
                                }
                                else if (tb.Id >= 100000 && tb.Id <= 100020)//策划指定墓碑使用此区间
                                {
                                    MonumentDic.Add(tb.Id, tb.NpcId);   
                                }
                                return true;
                            });
                            IsInit = true;
                        }
                        var npcId = npc.TableNPC.Id;
                        if (WarFlagDic.ContainsValue(npcId))
                        {
                            //战旗
                            PlayerDataManager.Instance.HoldLode(npcId);
                            return false;                            
                        }
                        else if (LodeDic.ContainsValue(npcId))
                        {
                            //采矿
                            PlayerDataManager.Instance.CollectLode(npcId);
                            return false;
                        }
                        else if (MonumentDic.ContainsValue(npcId))
                        {
                            //祭拜
                            PlayerDataManager.Instance.WorshipMonument(npcId);
                            return false;
                        }
                    }
                    EventDispatcher.Instance.DispatchEvent(new PickUpNpc_Event(npc.GetDataId(), npc.GetObjId()));
                    return false;
                }
            }

            var npcDataId = npc.GetDataId();
            if (npcDataId >= 108 && npcDataId <= 110)//判断是否是排行NPC
            {
                EventDispatcher.Instance.DispatchEvent(new OnRankNpcClick_Event(npcDataId));
            }

            myself.StopMove();
            if (distance <= GameSetting.Instance.MaxDistance_NPC)
            {
                if (MissionManager.Instance.OpenMissionByNpcId(npc.GetDataId(), npc.GetObjId()))
                {
                    npc.DoDialogue();
                    //TODO
                    if (myself.IsAutoFight())
                    {
                        myself.LeaveAutoCombat();
                    }
                }
            }
            else
            {
                var command = new MoveCommand(myself, npc.Position, GameSetting.Instance.MaxDistance_NPC);
                Executer.PushCommand(command);
                var command1 = new FuncCommand(() =>
                {
                    if (MissionManager.Instance.OpenMissionByNpcId(npc.GetDataId(), npc.GetObjId()))
                    {
                        npc.DoDialogue();
                        //TODO
                        if (myself.IsAutoFight())
                        {
                            myself.LeaveAutoCombat();
                        }
                    }
                });
                Executer.PushCommand(command1);
            }
        }
        else if (obj.GetObjType() == OBJ.TYPE.DROPITEM)
        {
//濡傛灉鏄帀钀界墿鍝?
            var dropItem = obj as ObjDropItem;
            myself.StopMove();
            var dis = GameSetting.Instance.MaxDistance_DropItem;

            if (dropItem.mTableData != null && dropItem.mTableData.Type == 300)
            {
                dis = GameUtils.AutoPickUpBuffDistance;
            }

            if (distance <= dis)
            {
                dropItem.Pickup();
            }
            else
            {
                var command = new MoveCommand(myself, dropItem.Position, dis);
                Executer.PushCommand(command);
                var command1 = new FuncCommand(() =>
                {
                    if (null != dropItem)
                    {
                        dropItem.Pickup();
                    }
                });
                Executer.PushCommand(command1);
            }
        }
        else if (obj.GetObjType() == OBJ.TYPE.FAKE_CHARACTER)
        {

            if (((ObjFakeCharacter)obj).iType == (int)eFakeCharacterTypeDefine.MieShiFakeCharacterType)
            {
                EventDispatcher.Instance.DispatchEvent(new ApplyPortraitAward_Event(obj.GetDataId()));
                
            }
            else
            {
                if (((ObjFakeCharacter)obj).iType == (int)OBJ.TYPE.FAKE_FIGHTLEADER)
                {
                    if (GuideTrigger.IsFunctionOpen("BtnRank"))
                    {
                        var e = new Show_UI_Event(UIConfig.RankUI, new RankArguments { RankId = obj.GetObjId()});
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                    else
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(1736));
                    }
                }
                else
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.WorshipFrame));
            }

        }
        return false;
    }

    // Use this for initialization
    private void Start()
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

    public void TargetCharacter(ObjBase obj)
    {
        var myself = ObjManager.Instance.MyPlayer;
        if (myself == obj)
        {
            return;
        }
        var character = obj as ObjCharacter;
        if (character == null)
        {
            return;
        }

        switch (character.GetObjType())
        {
            case OBJ.TYPE.INVALID:
                break;
            case OBJ.TYPE.OTHERPLAYER:
                if (GameSetting.Instance.ShowOtherPlayer)
                {
                    TargetObj = character;
                    PlayerDataManager.Instance.SetSelectTargetData(TargetObj, 3);
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_MainuiCloseList());
                }
                break;
            case OBJ.TYPE.NPC:

            {
                TargetObj = character;
                PlayerDataManager.Instance.SetSelectTargetData(TargetObj, 3);
                EventDispatcher.Instance.DispatchEvent(new UIEvent_MainuiCloseList());
            }
                break;
			case OBJ.TYPE.RETINUE:
		        break;
            case OBJ.TYPE.DROPITEM:
                break;
            case OBJ.TYPE.AUTOPLAYER:
                break;
            case OBJ.TYPE.MYPLAYER:
                break;
            case OBJ.TYPE.FAKE_CHARACTER:
                break;
            case OBJ.TYPE.ELF:
                break;
            default:
                break;
        }
    }

    private IEnumerator TestNetCoroutine()
    {
        var msg = NetManager.Instance.RobotcFinishFuben(0);
        yield return msg.SendAndWaitUntilDone();
        if (msg.State == MessageState.Reply)
        {
            if (msg.ErrorCode == (int) ErrorCodes.OK)
            {
                Logger.Error("RobotcFinishFuben............OK......");
            }
            else
            {
                UIManager.Instance.ShowNetError(msg.ErrorCode);
                Logger.Error("RobotcFinishFuben............msg.ErrorCode......{0}", msg.ErrorCode);
            }
        }
        else
        {
            Logger.Error("RobotcFinishFuben............msg.State......{0}", msg.State);
        }
    }

	public int Item = 212405;
    private void Update()
    {
#if !UNITY_EDITOR
        try
        {
#endif

#if UNITY_EDITOR

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MissionTip, new MissionTipArguments(2)));
        }
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            if (UIManager.Instance.UiVisible(UIConfig.NewStrongUI))
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.NewStrongUI));
            }
            else
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.NewStrongUI,
                    new StrongArguments()));
            }
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_FixIphoneX(-1));
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_FixIphoneX(1));
        }
        else if (Input.GetKeyDown(KeyCode.Comma))
        {
        }
        else if (Input.GetKeyDown(KeyCode.F1))
        {
           
            //GuideManager.Instance.StopGuiding();
            //GuideManager.Instance.StartGuide(1);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            PlayCG.Instance.PlayCGFile(VideoFile);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            int[] Test = {10000, 10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10009, 10010, 10011};
            EventDispatcher.Instance.DispatchEvent(new Event_AchievementTip(Test[UnityEngine.Random.RandomRange(0, Test.Length)]));
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            MissionManager.Instance.StartDialog(90);

//			MissionManager.Instance.ChangedSceneByMission(200, 17);
// 			var arg = new StoreArguments() { Type = 14 };
// 			Show_UI_Event e = new Show_UI_Event(UIConfig.CustomShopFrame, arg);
// 			EventDispatcher.Instance.DispatchEvent(e);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            GameUtils.GotoUiTab(19, 1);
//             var arg = new StoreArguments() { Type  = 104};
//             Show_UI_Event e = new Show_UI_Event(UIConfig.StoreEquip, arg);
//             EventDispatcher.Instance.DispatchEvent(e);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.AcientBattleFieldFrame));
            //GameUtils.GotoUiTab(19, 0);
            var e = new Show_UI_Event(UIConfig.DungeonResult, new DungeonResultArguments
                {
                    FubenId = 6,
                    Second = 100,
                    DrawId = 0,
                    DrawIndex = 1,
                    EraId =  1
                });
            EventDispatcher.Instance.DispatchEvent(e);
//             var arg = new CharacterArguments() {Tab = -1};
//             Show_UI_Event e = new Show_UI_Event(UIConfig.CharacterUI,arg);
//             EventDispatcher.Instance.DispatchEvent(e);
//             var arg = new EquipUIArguments() { Tab = 1 };
//             var e = new Show_UI_Event(UIConfig.EquipUI, arg);
//             EventDispatcher.Instance.DispatchEvent(e);
//             var e = new Show_UI_Event(UIConfig.BattleResult, new BattleResultArguments
//             {
//                 DungeonId = 2000,
//                 BattleResult = 1,
//                 First = 0
//             });
//             EventDispatcher.Instance.DispatchEvent(e);
//             SkillEquipMainUiAnime e = new SkillEquipMainUiAnime(4,1);
//             EventDispatcher.Instance.DispatchEvent(e);

//             var objId = ObjManager.Instance.MyPlayer.GetObjId();
//             PlayerDataManager.Instance.ApplyCharacterSimpleInfo(objId, (PlayerInfoMsg info) =>
//             {
//                 Show_UI_Event e = new Show_UI_Event(UIConfig.PlayerInfoUI, info);
//                 EventDispatcher.Instance.DispatchEvent(e);
//             });
            //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard("asdfasdf"));
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.EquipInfoUI, 210407));
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            UIManager.Instance.DestoryCloseUi();
//             TestChat = new ChatMessageDataModel()
//             {
//                 Type = (int)eChatChannel.World,
//                 Content = "",
//                 SoundData = new byte[]{},
//                 Name = "asdf",
//                 CharId = 0ul,
//             };
//             EventDispatcher.Instance.DispatchEvent(new Event_PushMessage(TestChat));
//             EventDispatcher.Instance.DispatchEvent(e);
//            GameUtils.ShowHintTip("adfasdfefe");
//             P1vP1Change_One one = new P1vP1Change_One();
//             one.Type = 0;
//             one.NewRank = 101;
//             one.OldRank = 102;
//             one.Name = "adfdsf";
//             ArenaFightRecoardChange e = new ArenaFightRecoardChange(one);
//             EventDispatcher.Instance.DispatchEvent(e);
//            UIManager.Instance.ShowBlockLayer();
//             var d = new WingArguments();
//             d.Tab = 1;
//             Show_UI_Event e = new Show_UI_Event(UIConfig.WingUI,d);
//             EventDispatcher.Instance.DispatchEvent(e);
//             Show_UI_Event e = new Show_UI_Event(UIConfig.BattleUI);
//             EventDispatcher.Instance.DispatchEvent(e);
            var e1 = new ChatMainHelpMeesage("asdfasdfasdf");
            EventDispatcher.Instance.DispatchEvent(e1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
//             BuffResult result = new BuffResult();
//             result.Type = BuffType.HT_REBOUND;
//             result.Damage = 100;
//             result.TargetObjId = ObjManager.Instance.MyPlayer.GetObjId();
//             var e = new ShowDamageBoardEvent(ObjManager.Instance.MyPlayer.Position, result);
//             EventDispatcher.Instance.DispatchEvent(e);
//             TestChat.Content = "adsfadsfasfdeasdfeadsfeadfe";
//             EventDispatcher.Instance.DispatchEvent(new ChatVoiceContent(TestChat));
            //UIManager.Instance.ShowBlockLayer(1);
//             var d = new WingArguments();
//             d.Tab = 0;
//             Show_UI_Event e = new Show_UI_Event(UIConfig.WingUI,d);
//             EventDispatcher.Instance.DispatchEvent(e);
            // EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.BattleUnionUI));

            //UIManager.Instance.RemoveBlockLayer();
			GameUtils.GotoUiTab(92, -1, Item);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            UIManager.Instance.ShowBlockLayer();
//            UIManager.Instance.ShowMessage(MessageBoxType.Ok, "adffd","adsfasdf");
/*            UIManager.Instance.RemoveBlockLayer();*/
//             var d = new ComposeArguments();
//             d.Tab = 102;
//             Show_UI_Event e = new Show_UI_Event(UIConfig.ComposeUI, d);
//             EventDispatcher.Instance.DispatchEvent(e);
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.AstrologyUI));
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
//             Show_UI_Event e = new Show_UI_Event(UIConfig.ReliveUI, Game.Instance.ServerTime.AddSeconds(60));
//                         EventDispatcher.Instance.DispatchEvent(e);
//                         var list = new List<int> { 100, 0, 1 };
//                         EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.DungeonResult, list));
//            NetManager.Instance.StartCoroutine(TestNetCoroutine());
//             var d = new CharacterArguments();
//             d.Tab = 0;
//             Show_UI_Event e = new Show_UI_Event(UIConfig.CharacterUI, d);
//             EventDispatcher.Instance.DispatchEvent(e);
// //             SkillEquipMainUiAnime e = new SkillEquipMainUiAnime(4, 0);
//              EventDispatcher.Instance.DispatchEvent(e);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!string.IsNullOrEmpty(MoviePath))
            {
                PlayMovie.Play(MoviePath);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            Game.Instance.ExitToSelectRole();
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            if (null != DebugHelper.helperInstance)
            {
                var console = DebugHelper.helperInstance.GetComponent<DevConsole.Console>();
                console.PrintInput("!!SpeedSet,300");
            }
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            Game.Instance.ExitToLogin();
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            var args = new UIInitArguments();
            args.Args = new List<int>();
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.EraAchievementUI, args));
//             EventDispatcher.Instance.DispatchEvent(new System_Notice_Event("1196306"));
        }
        else if (Input.GetKeyDown(KeyCode.F7))
        {
            var args = new UIInitArguments();
            args.Args = new List<int>();
            args.Args.Add(1);
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.EraGetNoticeUI, args));

            //ObjManager.Instance.MyPlayer.PopTalk("fdasfdsafdsa");
        }
        else if (Input.GetKeyDown(KeyCode.F8))
        {
			EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.OperationActivityFrame));
        }
        else if (Input.GetKeyDown(KeyCode.F9))
        {
            //GameUtils.GotoUiTab(80, 0);
            var player = ObjManager.Instance.MyPlayer;
            PlayerDataManager.Instance.mInitBaseAttr.ModelId = 700;
            if (player == null)
            {
                return;
            }
            player.ModelId = 700;
        }
		else if (Input.GetKeyDown(KeyCode.F10))
		{
            var player = ObjManager.Instance.MyPlayer;
            PlayerDataManager.Instance.mInitBaseAttr.ModelId = -1;
            if (player == null)
            {
                return;
            }
            player.ModelId = -1;
		    //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.P1VP1Frame));
		}
        else if(Input.GetKeyDown(KeyCode.Home))
        {
            if (btest == false)
            {
                var e = new Show_UI_Event(UIConfig.CharacterUI);
                EventDispatcher.Instance.DispatchEvent(e);
            }
            else
            {
                var e = new Close_UI_Event(UIConfig.CharacterUI);
                EventDispatcher.Instance.DispatchEvent(e);
            }
            btest = !btest;

        }
        else if (Input.GetKeyDown(KeyCode.End))
        {
            PlayerDataManager.Instance.HoldLode(242010);
        }
        else if (Input.GetKeyDown(KeyCode.PageDown))
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.FieldMineUI));
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
        //    var arg = new SmithyFrameArguments();
        //    arg.Tab = 1;
        //    arg.BuildingData = CityManager.Instance.BuildingDataList[0];
        //    var e = new Show_UI_Event(UIConfig.SmithyUI, arg);
        //    EventDispatcher.Instance.DispatchEvent(e);
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ChickenFightUI));
        }
        else if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            PlayerDataManager.Instance.InputControlHPEffect = true;
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            PlayerDataManager.Instance.InputControlHPEffect = false;
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.FieldFinalUI));
        }
#endif

#if UNITY_STANDALONE
        Vector2 v = new Vector2();
        if (Input.GetKey(KeyCode.W))
        {
            v.y = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            v.x = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            v.y = -1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            v.x = 1;
        }
        if (v.x != 0 || v.y != 0)
        {
            MoveDirection(v);
        }
	    if (Input.GetKeyUp(KeyCode.J))
	    {
			OnClickEvent(0,-1);
	    }
		else 
		{
			int idx = -1;
			if (Input.GetKeyUp(KeyCode.U))
			{
				idx = 0;
			}
			else if (Input.GetKeyUp(KeyCode.I))
			{
				idx = 1;
			}
			else if (Input.GetKeyUp(KeyCode.O))
			{
				idx = 2;
			}
			else if (Input.GetKeyUp(KeyCode.P))
			{
				idx = 3;
			}
			var skill = PlayerDataManager.Instance.PlayerDataModel.SkillData.EquipSkills;
			if (null != skill &&idx>=0&& idx < skill.Count)
			{
				OnClickEvent(1, skill[idx].SkillId);
			}
			
		}
#endif
        Executer.Update();


#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public bool UseSkill(ulong casterId, int skillId, ulong targetId = TypeDefine.INVALID_ULONG)
    {
        var errorCode = CheckSkill(skillId);
        if (ErrorCodes.OK == errorCode)
        {
            NetManager.Instance.DoUseSkill(casterId, skillId, targetId);
            return true;
        }
        if (ErrorCodes.Error_SkillNoCD == errorCode)
        {
            if (skillId != PlayerDataManager.Instance.GetNormalSkill())
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(714));
            }
        }
        else if (ErrorCodes.Error_MpNoEnough == errorCode)
        {
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(715));
        }
        else if (ErrorCodes.Error_HpNoEnough == errorCode)
        {
            EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(715));
        }
        else if (ErrorCodes.Error_SafeArea == errorCode)
        {
            //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(41003));//By zhaohui 注释
        }
        else
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_ErrorTip(errorCode));
        }
        return false;
    }

#if UNITY_EDITOR
    private static int Test = 0;
    private static ChatMessageDataModel TestChat;
	public string VideoFile = "Video/HeroBorn.txt";
#endif
}