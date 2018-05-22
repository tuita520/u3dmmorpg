/********************************************************************************* 

                         Scorpion



  *FileName:SkillFrameCtrler

  *Version:1.0

  *Date:2017-07-13

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections;
using System.Collections.Generic;
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

#endregion

namespace ScriptController
{
    public class SkillFrameCtrler : IControllerBase
    {
        #region 构造函数
        public SkillFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_SkillSelect.EVENT_TYPE, OnClicSkillItemEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_EquipSkill.EVENT_TYPE, OnSkillEquipEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_SwapEquipSkill.EVENT_TYPE, OnSwapSkillEquipEvent);
            //EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_OnDisable.EVENT_TYPE, SyncSkillEquipData);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_UpgradeSkill.EVENT_TYPE, OnUpGradeSkillEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_GoToCompose.EVENT_TYPE, OnGoToComposeEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_AddUnLearnedTalent.EVENT_TYPE, OnAddUnLearnedTalentEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_TalentBallClick.EVENT_TYPE, OnTalentBallClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_AddTalentPoint.EVENT_TYPE, OnAddTalentPointEvent);
            //EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_OnSkillBallOpen.EVENT_TYPE, OnSkillBallOpen);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_AddSkillBoxDataModel.EVENT_TYPE, OnAddSkillBoxesEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_OnSkillBallClose.EVENT_TYPE, OnSkillBallCloseEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_UnEquipSkill.EVENT_TYPE, OnUnEquipSkillEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_OnResetSkillTalent.EVENT_TYPE, OnResetSkillTalentEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnPlayerLevelUpGradeEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_OnResetTalent.EVENT_TYPE, OnResetTalentEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_NetSyncTalentCount.EVENT_TYPE, OnNetSyncTalentCountEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_SkillTalentChange.EVENT_TYPE, OnSkillTalentChangeEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_UseSkill.EVENT_TYPE, OnUseSkillSuccessEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_OnSkillTalentSelected.EVENT_TYPE, OnSkillBoxSelectedEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnLevelUpEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitionEvent);
            EventDispatcher.Instance.AddEventListener(Resource_Change_Event.EVENT_TYPE, OnResourceChangedEvent);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagInitEvent);
        }
        #endregion

        #region 成员变量
        private int currentShowTalentId;
        private SkillUiDataModel DataModel;
        private int mEquipSkillDirtyMark;
        private bool skillChanged;
        private bool IsInit = false;
        private List<int> TalentIdList = new List<int>();
        private SkillDataModel SkillDataModel
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.SkillData; }
        }
        #endregion

        #region 事件
        private void OnAddSkillBoxesEvent(IEvent ievent)
        {
            var _skilldata = SkillDataModel;
            var _e = ievent as UIEvent_SkillFrame_AddSkillBoxDataModel;
            var _skillId = _e.DataModel.SkillId;
            _e.DataModel.MaxCount = Table.GetSkill(_skillId).TalentMax;
            {
                // foreach(var talent in skilldata.Talents)
                var _enumerator10 = (_skilldata.Talents).GetEnumerator();
                while (_enumerator10.MoveNext())
                {
                    var _talent = _enumerator10.Current;
                    {
                        if (_skillId == Table.GetTalent(_talent.TalentId).ModifySkill)
                        {
                            _e.DataModel.CurrentCount += _talent.Count;
                        }
                    }
                }
            }
            int lastCount;
            PlayerDataManager.Instance.mSkillTalent.TryGetValue(_e.DataModel.SkillId, out lastCount);
            _e.DataModel.LastCount = lastCount;
            SkillItemDataModel skillItem;
            PlayerDataManager.Instance.mAllSkills.TryGetValue(_e.DataModel.SkillId, out skillItem);
            if (skillItem != null)
            {
                _e.DataModel.skillItem = skillItem;
            }
            SkillDataModel.SkillBoxes.Add(_e.DataModel);

            //红点
            if(_e.DataModel.LastCount != 0)
            {
                PlayerDataManager.Instance.NoticeData.SkillTalentStatus = true;
            }
        }
        private void OnAddTalentPointEvent(IEvent ievent)
        {
            var _skilldata = SkillDataModel;
            var _talentId = _skilldata.TalentIdSelected;
            var _table = Table.GetTalent(_talentId);
            var _beforeId = _table.BeforeId;

            //检查前置天赋
            if (_beforeId > 0)
            {
                TalentCellDataModel talent = null;
                var _skilldataTalentsCount1 = _skilldata.Talents.Count;
                for (var i = 0; i < _skilldataTalentsCount1; i++)
                {
                    if (_beforeId == _skilldata.Talents[i].TalentId)
                    {
                        talent = _skilldata.Talents[i];
                        break;
                    }
                }

                if (talent.Count < _table.BeforeLayer)
                {
                    var _e = new ShowUIHintBoard(705);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
            }

            //检查技能点
            var _skillid = Table.GetTalent(_talentId).ModifySkill;
            if (_skillid < 1)
            {
                //不在消耗技能点了
                //             if (skilldata.TalentCount < 1)
                //             {
                //                 var e = new ShowUIHintBoard(706);
                //                 EventDispatcher.Instance.DispatchEvent(e);
                //                 return;
                //             }
            }
            else
            {
                if (PlayerDataManager.Instance.mSkillTalent.ContainsKey(_skillid))
                {
                    if (PlayerDataManager.Instance.mSkillTalent[_skillid] < 1)
                    {
                        var _e = new ShowUIHintBoard(706);
                        EventDispatcher.Instance.DispatchEvent(_e);
                        //天赋点数不足，弹出对应技能书（石）的物品信息
                        GameUtils.ShowItemIdTip(_table.SkillItem);
                        return;

                    }
                }
            }

            if (_talentId >= 0)
            {
                NetManager.Instance.StartCoroutine(SendAddTalentPointMassageCoroutine(_talentId));
            }
            else
            {
                Logger.Error("SelectedTalentId error id:" + _talentId);
            }
        }
        private void OnAddUnLearnedTalentEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_AddUnLearnedTalent;
            _e.DataModel.InitializeTalentCell();
            SkillDataModel.Talents.Add(_e.DataModel);
            var _Talents = PlayerDataManager.Instance.mAllTalents;
            if (!_Talents.ContainsKey(_e.DataModel.TalentId))
            {
                _Talents.Add(_e.DataModel.TalentId, _e.DataModel);
            }
        }
        private void OnNetSyncTalentCountEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_NetSyncTalentCount;
            if (_e.TalentId == -1)
            {
                SkillDataModel.TalentCount = _e.Value;
                //PlayerDataManager.Instance.NoticeData.PlayerTalentCount = 0; //e.Value;
            }
        }
        //查看某个技能信息
        private void OnClicSkillItemEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_SkillFrame_SkillSelect;
            var _data = _ee.DataModel;
            RefurbishSelected(_data);
            var _enumerator = SkillDataModel.OtherSkills.GetEnumerator();

            while (_enumerator.MoveNext())
            {
                var _skill = _enumerator.Current;
                _skill.ShowToggle = false;
            }

            _data.ShowToggle = true;
        }

        private void OnExDataInitionEvent(IEvent ievent)
        {
            RefreshRedDot();
            if (PlayerDataManager.Instance.NoticeData.SkillTalentStatus)
            {
                PlayerDataManager.Instance.WeakNoticeData.SkillTotal = true;
            }
        }
        private void OnResourceChangedEvent(IEvent ievent)
        {
            var e = ievent as Resource_Change_Event;
            if (null == e)
            {
                return;
            }
            if (e.Type != eResourcesType.Spar)
            {
                return;
            }
            if (!IsInit)
            {
                var RoleId = PlayerDataManager.Instance.GetRoleId();
                var talentValues = string.Empty;
                if (0 == RoleId)
                {
                    talentValues = (Table.GetClientConfig(1400).Value);
                }
                else if (1 == RoleId)
                {
                    talentValues = (Table.GetClientConfig(1401).Value);
                }
                else if (2 == RoleId)
                {
                    talentValues = (Table.GetClientConfig(1402).Value);
                }
                var talentAll = talentValues.Split('|');
                for (int i = 0; i < talentAll.Length; i++)
                {
                    var item = talentAll[i];
                    SkillDataModel.TalentIdList.Add(item);
                }
                IsInit = true;
            }
            else
            {
                CheckSparNotice(); 
            }
        }
        private void OnFlagInitEvent(IEvent ievent)
        {
            var e = ievent as FlagInitEvent;
            if (null == e)
            {
                return;
            }
            CheckSparNotice(); 
        }


        private void CheckSparNotice()
        {
            var flag = PlayerDataManager.Instance.GetFlag(241);
            if (!flag)
            {
                PlayerDataManager.Instance.NoticeData.PlayerTalentCount = 0;
                return;
            }
            var TalentList = new List<bool>();
            var SparTotalNum = PlayerDataManager.Instance.GetRes((int)eResourcesType.Spar);
            TalentIdList.Clear();
            if (SkillDataModel.Talents.Count > 0)
            {
                for (int i = 0; i < SkillDataModel.Talents.Count; i++)
                {
                    var item = SkillDataModel.Talents[i];
                    var tbTalent = Table.GetTalent(item.TalentId);
                    TalentIdList.Add(item.TalentId);
                    if (null == tbTalent)
                    {
                        continue;
                    }
                    var tbSkillUpgrading = Table.GetSkillUpgrading(tbTalent.CastItemCount);
                    if (null == tbSkillUpgrading)
                    {
                        continue;
                    }
                    if (item.Count >= tbSkillUpgrading.Values.Count)
                    {
                        continue;
                    }
                    if (item.ShowLockTotal)
                    {
                        continue;
                    }
                    var ResSpar = tbSkillUpgrading.Values[item.Count];
                    if (SparTotalNum >= ResSpar)
                    {
                        TalentList.Add(true);
                    }
                    else
                    {
                        TalentList.Add(false);
                    }
                }
                for (int j = 0; j < SkillDataModel.TalentIdList.Count; j++)
                {
                    var id = int.Parse(SkillDataModel.TalentIdList[j]);
                    var talentData = new TalentCellDataModel();
                    talentData.TalentId = id;
                    if (TalentIdList.Contains(id))
                    {
                        continue;                        
                    }
                    talentData.InitializeTalentCell();
                    var tbTalent = Table.GetTalent(id);
                    if (null == tbTalent)
                    {
                        continue;
                    }
                    var tbSkillUpgrading = Table.GetSkillUpgrading(tbTalent.CastItemCount);
                    if (null == tbSkillUpgrading)
                    {
                        continue;
                    }
                    if (talentData.Count >= tbSkillUpgrading.Values.Count)
                    {
                        continue;
                    }
                    if (talentData.ShowLockTotal)
                    {
                        continue;
                    }
                    var ResSpar = tbSkillUpgrading.Values[talentData.Count];
                    if (SparTotalNum >= ResSpar)
                    {
                        TalentList.Add(true);
                        break;
                    }
                    else
                    {
                        TalentList.Add(false);
                    }
                }
            }
            else
            {
                var tbTalent = Table.GetTalent(int.Parse(SkillDataModel.TalentIdList[0]));
                if (null != tbTalent)
                {
                    var tbSkillUpGrading = Table.GetSkillUpgrading(tbTalent.CastItemCount);
                    if (null != tbSkillUpGrading)
                    {
                        var TalentValues = tbSkillUpGrading.Values;
                        var ResSpar = TalentValues[0];
                        if (SparTotalNum >= ResSpar)
                        {
                            TalentList.Add(true);
                        }
                        else
                        {
                            TalentList.Add(false);
                        }
                    }
                }
            }
            if (TalentList.Contains(true))
            {
                PlayerDataManager.Instance.NoticeData.PlayerTalentCount = 10;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.PlayerTalentCount = 0;
            }
        }
        private void RefreshRedDot()
        {            
            var _count = SkillDataModel.OtherSkills.Count;
            PlayerDataManager.Instance.WeakNoticeData.SkillCanUpgrade = false;
            for (var i = 0; i < _count - 2; i++)
            {
                var _skill = SkillDataModel.OtherSkills[i];
                _skill.RefreshCast();
                if (!_skill.ShowUpGradeBtn)
                {
                    continue;
                }

                if(_skill.SkillLv>=80)
                {
                    continue;
                }

                if(_skill.IsXpSkill)
                    continue;
                var _type = StringConvert.Level_Value_Ref(10000000 + 999, _skill.SkillLv - 1);
                if (_type == 5)
                {
                    var _spar = PlayerDataManager.Instance.GetRes((int)eResourcesType.Spar);
                    if (_spar < _skill.SkillSparCost)
                    {
                        continue;
                    }
                    PlayerDataManager.Instance.WeakNoticeData.SkillCanUpgrade = true;
                    PlayerDataManager.Instance.WeakNoticeData.SkillTotal = true;
                    return;
                }
                var _gold = PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes);
                if (_gold < _skill.SkillCost )
                {
                    continue;
                }
                PlayerDataManager.Instance.WeakNoticeData.SkillCanUpgrade = true;
                PlayerDataManager.Instance.WeakNoticeData.SkillTotal = true;
                return;
            }         
        }

        private void OnLevelUpEvent(IEvent ievent)
        {
            var _level = PlayerDataManager.Instance.GetLevel();

            var _count2 = SkillDataModel.Talents.Count;
            for (var i = 0; i < _count2; i++)
            {
                var _talents = SkillDataModel.Talents[i];
                if (_talents.NeedLevel <= _level)
                {
                    _talents.LevelLock = false;
                }
            }


            if (_level % 5 != 0)
            {
                return;
            }

            RefreshRedDot();
        }

        //玩家升级之后刷新技能学习数据
        private void OnPlayerLevelUpGradeEvent(IEvent ievent)
        {
            var _skilldata = SkillDataModel;
            {
                // foreach(var skillItemDataModel in skilldata.AllSkills)
                var _enumerator1 = (_skilldata.AllSkills).GetEnumerator();
                while (_enumerator1.MoveNext())
                {
                    var _skillItemDataModel = _enumerator1.Current;
                    {
                        _skillItemDataModel.RefreshLevelCast();
                    }
                }
            }
        }

        private void OnSkillBallCloseEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_OnSkillBallClose;
            _e.DataModel.ShowSkillBox = 0;
        }

        //     private void OnSkillBallOpen(IEvent ievent)
        //     {
        //         var e = ievent as UIEvent_SkillFrame_OnSkillBallOpen;
        //         var skillData = SkillDataModel;
        //         var skillDataSkillBoxesCount2 = skillData.SkillBoxes.Count;
        //         for (var i = 0; i < skillDataSkillBoxesCount2; i++)
        //         {
        //             skillData.SkillBoxes[i].ShowSkillBox = 0;
        //         }
        //         e.DataModel.ShowSkillBox = 1;
        //         OnSkillBoxSelected(new UIEvent_SkillFrame_OnSkillTalentSelected(e.DataModel.skillItem.SkillId));
        //     }

        private void OnSkillBoxSelectedEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_OnSkillTalentSelected;
            var _c = SkillDataModel.SkillBoxes.Count;
            for (var i = 0; i < _c; i++)
            {
                var _box = SkillDataModel.SkillBoxes[i];
                if (_e.skillid == _box.SkillId)
                {
                    SkillDataModel.SelectedSkillBox = _box;
                    _box.ShowSkillBox = 1;
                    if (null != _box.SkillTalents[0])
                    {
                        OnTalentBallClickEvent(new UIEvent_SkillFrame_TalentBallClick(_box.SkillTalents[0].TalentId));
                    }
                }
                else
                {
                    _box.ShowSkillBox = 0;
                }
            }
        }

        //拖拽技能到技能bar
        private void OnSkillEquipEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_SkillFrame_EquipSkill;
            var _nIndex = _ee.Index;
            var _skillId = _ee.SkillId;
            var _skillData = SkillDataModel;

            SkillItemDataModel equipSkill;
            if (!PlayerDataManager.Instance.mAllSkills.TryGetValue(_skillId, out equipSkill))
            {
                Logger.Error("player dont have this skill -----skillID = {0}--", _skillId);
                return;
            }

            //如果当前槽位冷却中,新加入技能重新冷却
            var _bNewSkill = true;
            var _lastSkillCD = (Math.Abs(_skillData.EquipSkills[_nIndex].CoolDownTime) > 0.0001f);

            //如果技能在别的槽位,把原来的槽位置空
            var _equipindex = _skillData.EquipSkills.IndexOf(equipSkill);
            if (_equipindex != -1)
            {
                var _nullSkill = new SkillItemDataModel();
                _nullSkill.SkillId = -1;
                _skillData.EquipSkills[_equipindex] = _nullSkill;
                _bNewSkill = false;
            }

            if (_bNewSkill && _lastSkillCD)
            {
                mEquipSkillDirtyMark = BitFlag.IntSetFlag(mEquipSkillDirtyMark, _nIndex);
            }

            _skillData.EquipSkills[_nIndex] = equipSkill;
            skillChanged = true;


            if (_ee.BSyncToServer)
            {
                OnSyncSkillEquipDataEvent(null);
            }
            else
            {
                PlayerDataManager.Instance.RefrehEquipPriority();
            }
        }

        //交换装备中的技能
        private void OnSwapSkillEquipEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_SkillFrame_SwapEquipSkill;
            var _skillData = SkillDataModel;
            var _swaptemp = _skillData.EquipSkills[_ee.FromIndex];
            _skillData.EquipSkills[_ee.FromIndex] = _skillData.EquipSkills[_ee.TargetIndex];
            _skillData.EquipSkills[_ee.TargetIndex] = _swaptemp;
            skillChanged = true;

            OnSyncSkillEquipDataEvent(null);
        }
        //重置技能天赋
        private void OnResetSkillTalentEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_OnResetSkillTalent;
            var _dataModel = SkillDataModel.SelectedSkillBox;
            var _skillId = _dataModel.SkillId;

            var _canReset = _dataModel.SkillTalents.All(talent => talent.Count <= 0);

            if (_canReset)
            {
                var _e2 = new ShowUIHintBoard(707);
                EventDispatcher.Instance.DispatchEvent(_e2);
                return;
            }

            var _desc = GameUtils.GetDictionaryText(702);
            var _skillName = Table.GetSkill(_skillId).Name;
            var _upgradeCast = Table.GetSkill(_skillId).ResetCount * _dataModel.CurrentCount;
            var _message = string.Format(_desc, _upgradeCast, _skillName);

            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _message, "",
                () =>
                {
                    var _itemCount = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Spar;

                    if (_itemCount < _upgradeCast)
                    {
                        var _e1 = new ShowUIHintBoard(703);
                        EventDispatcher.Instance.DispatchEvent(_e1);
                        return;
                    }

                    NetManager.Instance.StartCoroutine(SendResetSkillTalentMsgCoroutine(_dataModel));
                },
                () => { });
        }

        //重置天赋
        private void OnResetTalentEvent(IEvent ievent)
        {
            if (GetInherentCount() == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(707));
                return;
            }

            var _type = int.Parse(Table.GetClientConfig(258).Value);
            var _count = Table.GetClientConfig(259).Value;
            var _name = Table.GetItemBase(_type).Name;

            var _message = string.Format(GameUtils.GetDictionaryText(708), _count, _name);

            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _message, "",
                () => { NetManager.Instance.StartCoroutine(SendResetTalentMsgCoroutine()); },
                () => { });
        }
        private void OnSkillTalentChangeEvent(IEvent ievent)
        {
            RefreshData(new SkillFrameArguments
            {
                Tab = 1
            });
        }

        private void OnSyncSkillEquipDataEvent(IEvent ievent)
        {
            if (!skillChanged)
            {
                return;
            }

            NetManager.Instance.StartCoroutine(SyncEquipSkillDataCoroutine());
        }

        private void OnTalentBallClickEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_TalentBallClick;
            if (currentShowTalentId == -1 && _e.TalentId == -1)
            {
                return;
            }

            var _skillData = SkillDataModel;
            _skillData.TalentIdSelected = _e.TalentId;
            currentShowTalentId = _e.TalentId;
            RefurbishTalentBoardDesc(_e.TalentId);
        }

        //卸下装备中的技能
        private void OnUnEquipSkillEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_SkillFrame_UnEquipSkill;
            var _skillitem = new SkillItemDataModel();
            _skillitem.SkillId = -1;
            mEquipSkillDirtyMark = BitFlag.IntSetFlag(mEquipSkillDirtyMark, _ee.Index);
            SkillDataModel.EquipSkills[_ee.Index] = _skillitem;
            skillChanged = true;

            OnSyncSkillEquipDataEvent(null);
        }

        private void OnGoToComposeEvent(IEvent ievent)
        {
            var e = ievent as UIEvent_SkillFrame_GoToCompose;
            if (null == e)
                return;
            var composeId = 0;
            var tbTalent = Table.GetTalent(SkillDataModel.TalentIdSelected);
            if (null == tbTalent)
                return;
            Table.ForeachItemCompose(record =>
            {
                if (record.Type == (int)eComposeType.SkillBook && tbTalent.SkillItem == record.ComposeView)
                {
                    composeId = record.Id;
                    return true;
                }
                return true;
            });
            GameUtils.GotoUiTab(26, composeId);
        }

        private void OnUpGradeSkillEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_SkillFrame_UpgradeSkill;

            var _skillData = SkillDataModel.SelectSkill;

            //检查钱
            if (_skillData.SkillCost > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold)
            {
                /*
            var str = string.Format(GameUtils.GetDictionaryText(270255), Table.GetItemBase(2).Name);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, str, "", () =>
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ExchangeUI));
            });
             * */
                var _ee = new ShowUIHintBoard(210100);
                EventDispatcher.Instance.DispatchEvent(_ee);
                //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ExchangeUI));
                var e = new Show_UI_Event(UIConfig.WishingUI, new WishingArguments { Tab = 1 });
                EventDispatcher.Instance.DispatchEvent(e);
                return;
            }
            //检查技能水晶
            if (_skillData.SkillSparCost > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Spar)
            {
                var _ee = new ShowUIHintBoard(210108);
                EventDispatcher.Instance.DispatchEvent(_ee);
                GameUtils.ShowItemIdTip((int)eResourcesType.Spar);
                return;
            }

            var _skillid = _skillData.SkillId;
            if (_skillid != 0)
            {
                NetManager.Instance.StartCoroutine(SendUpGradeSkillMassageCoroutine(_skillid));
            }
            else
            {
                Logger.Debug("upgrade skill index error!");
            }
        }

        private void OnUseSkillSuccessEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UseSkill;
            var _id = _e.SkillId;
            SkillItemDataModel skill;
            if (PlayerDataManager.Instance.mAllSkills.TryGetValue(_id, out skill))
            {
                var _skilldata = SkillDataModel;
                _skilldata.CommonCoolDownTotal = Table.GetSkill(_id).CommonCd * 0.001f;
                _skilldata.CommonCoolDown = _skilldata.CommonCoolDownTotal;

                if (skill.ChargeLayer == skill.ChargeLayerTotal)
                {
                    skill.CoolDownTime = skill.CoolDownTimeTotal;
                }

                if (skill.ChargeLayerTotal > 1)
                {
                    skill.ChargeLayer--;
                }
            }
        }
        #endregion

        #region 逻辑函数
        private void ExchangeSkillLevelFromDataModel(int skillId)
        {
            var _skillData = SkillDataModel;
            {
                // foreach(var skill in skillData.OtherSkillss)
                var _enumerator3 = (_skillData.OtherSkills).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _skill = _enumerator3.Current;
                    {
                        _skill.ShowToggle = false;
                        if (_skill.SkillId == skillId)
                        {
                            _skill.SkillLv++;
                            var gold = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold - _skill.SkillCost;
                            PlayerDataManager.Instance.SetRes(2, gold);
                            var spar = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Spar - _skill.SkillSparCost;
                            PlayerDataManager.Instance.SetRes(5, spar);

                            _skill.RefreshCast();
                            RefurbishSelected(_skill);
                            _skill.ShowToggle = true;
                        }
                    }
                }
            }
            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.EquipSkill);
        }
        private void ExchangeTalentCountToDataModel(int talentId)
        {
            var _skillData = SkillDataModel;
            var _skillID = Table.GetTalent(talentId).ModifySkill;
            if (_skillID != -1)
            {
                {
                    // foreach(var skillBoxDataModel in skillData.SkillBoxes)
                    var _enumerator5 = (_skillData.SkillBoxes).GetEnumerator();
                    while (_enumerator5.MoveNext())
                    {
                        var _skillBoxDataModel = _enumerator5.Current;
                        {
                            if (_skillBoxDataModel.SkillId == _skillID)
                            {
                                _skillBoxDataModel.LastCount -= 1;
                                _skillBoxDataModel.CurrentCount += 1;
                                PlayerDataManager.Instance.SkillTalentPointChange(_skillBoxDataModel.SkillId, -1);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                //skillData.TalentCount--;
            }

            //去掉修炼小红点
            //PlayerDataManager.Instance.NoticeData.PlayerTalentCount = 0;//skillData.TalentCount;
            var _talentData = _skillData.Talents;
            var _list = new List<TalentCellDataModel>();
            {
                // foreach(var talent in talentData)
                var _enumerator7 = (_talentData).GetEnumerator();
                while (_enumerator7.MoveNext())
                {
                    var _talent = _enumerator7.Current;
                    {
                        if (_talent.TalentId == talentId)
                        {
                            _talent.Count++;
                            _list.Add(_talent);
                        }

                        if (Table.GetTalent(_talent.TalentId).BeforeId == talentId)
                        {
                            _list.Add(_talent);
                        }
                    }
                }
            }
            {
                var _list8 = _list;
                var _listCount8 = _list8.Count;
                for (var __i8 = 0; __i8 < _listCount8; ++__i8)
                {
                    var _talentCellDataModel = _list8[__i8];
                    {
                        _talentCellDataModel.InitializeTalentCell();
                    }
                }
            }
            //刷新面板
            if (_skillData.AttrPanel.Count > 0)
            {
                _skillData.AttrPanel.Clear();
            }
            {
                // foreach(var talentCellDataModel in talentData)
                var _enumerator9 = (_talentData).GetEnumerator();
                while (_enumerator9.MoveNext())
                {
                    var _talentCellDataModel = _enumerator9.Current;
                    {
                        if (_talentCellDataModel.Count > 0)
                        {
                            SkillDataModelExtension.AddTalentAttrToPanel(_skillData.AttrPanel, _talentCellDataModel);
                        }
                    }
                }
            }

            SkillItemDataModel skillItem;
            if (PlayerDataManager.Instance.mAllSkills.TryGetValue(_skillID, out skillItem))
            {
                skillItem.RefreshSkillData();
            }

            RefurbishTalentBoardDesc(talentId);

            var _e = new UIEvent_SkillFrame_RefreshTalentPanel();
            EventDispatcher.Instance.DispatchEvent(_e);
        }
        private void ClearCoolingDirty()
        {
            for (var i = 0; i < 5; i++)
            {
                if (BitFlag.GetLow(mEquipSkillDirtyMark, i))
                {
                    var _skill = SkillDataModel.EquipSkills[i];
                    _skill.CoolDownTime = _skill.CoolDownTimeTotal;
                    if (_skill.ChargeLayerTotal > 1)
                    {
                        _skill.ChargeLayer = 0;
                    }
                }
            }
            mEquipSkillDirtyMark = 0;
        }
        private int GetInherentCount()
        {
            var _count = 0;
            var _talents = SkillDataModel.Talents;
            var _c = _talents.Count;
            for (var i = 0; i < _c; i++)
            {
                var _table = Table.GetTalent(_talents[i].TalentId);
                if (_table.ModifySkill == -1)
                {
                    _count += _talents[i].Count;
                }
            }

            return _count;
        }
        private string GainSkillTypeString(int type)
        {
            var _id = 100000;
            if (type == 0)
            {
                _id = 100001;
            }
            return GameUtils.GetDictionaryText(_id);
        }
        private void RefurbishSelected(SkillItemDataModel dataModel)
        {
            SkillDataModel.SelectSkill = dataModel;
            var _skilldata = SkillDataModel;
            var _selected = _skilldata.SelectedSkillList[0];
            _selected.SkillId = dataModel.SkillId;
            _selected.SkillLv = dataModel.SkillLv;
            var _skillTable = Table.GetSkill(dataModel.SkillId);
            _selected.ManaCast = StringConvert.Level_Value_Ref(_skillTable.NeedMp, dataModel.SkillLv - 1);
            _selected.SkillType = GainSkillTypeString(_skillTable.ControlType);
            _selected.SkillItem = dataModel;

            var _current = 0;
            //         var skillTalent = PlayerDataManager.Instance.mSkillTalent; 
            //         if (skillTalent.ContainsKey(dataModel.SkillId))
            //         {
            //             current = skillTalent[dataModel.SkillId];
            //         }

            var _skillboxs = PlayerDataManager.Instance.PlayerDataModel.SkillData.SkillBoxes;
            SkillBoxDataModel box = null;
            for (var i = 0; i < _skillboxs.Count; i++)
            {
                if (_skillboxs[i].SkillId == dataModel.SkillId)
                {
                    box = _skillboxs[i];
                    break;
                }
            }

            if (null != box)
            {
                _current = box.LastCount + box.CurrentCount;
            }

            _selected.TalentCount = string.Format("{0}/{1}", _current, _skillTable.TalentMax);

            var _selected2 = _skilldata.SelectedSkillList[1];
            _selected2.SkillId = _selected.SkillId;
            _selected2.SkillLv = _selected.SkillLv == 0 ? 2 : _selected.SkillLv + 1;
            _selected2.ManaCast = _selected.ManaCast;
            _selected2.SkillType = _selected.SkillType;
        }
        private void RefurbishTalentBoardDesc(int talentId)
        {
            if (talentId < 0)
            {
                return;
            }
            var _skillData = SkillDataModel;
            var _descIndex = 0;
            TalentCellDataModel talentCellData = null;
            {
                // foreach(var talent in skillData.Talents)
                var _enumerator4 = (_skillData.Talents).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _talent = _enumerator4.Current;
                    {
                        if (_talent.TalentId == talentId)
                        {
                            _descIndex = _talent.Count;
                            talentCellData = _talent;
                        }
                    }
                }
            }

            var _talentTable = Table.GetTalent(talentId);

            if (_talentTable.ModifySkill == -1 && _talentTable.BeforeId != -1)
            {
                if (null == talentCellData || talentCellData.ShowLock)
                {
                    _skillData.TalentIdSelected = -1;
                    return;
                }
            }

            _skillData.SelectedTalentLevel = string.Format("{0}/{1}", _descIndex, _talentTable.MaxLayer);
            _skillData.ShowDesBoardAddButton = 1;
            //         if (talentTable.AttrId >= ExpressionHelper.AttrName.Count || talentTable.AttrId < 0)
            //         {
            //             return;
            //         }
            if (!ExpressionHelper.AttrName.ContainsKey(_talentTable.AttrId))
            {
                if (_descIndex == 0)
                {
                    _skillData.SelectedTalentDesc = _talentTable.BuffDesc[0];
                    _skillData.ShowDesBoardNext = 0;
                    _skillData.SelectedTalentDescNext = string.Empty;
                }
                else if (_descIndex == _talentTable.MaxLayer)
                {
                    _skillData.SelectedTalentDesc = _talentTable.BuffDesc[_talentTable.MaxLayer - 1];
                    _skillData.SelectedTalentDescNext = string.Empty;
                    _skillData.ShowDesBoardNext = 0;
                    _skillData.ShowDesBoardAddButton = 0;
                }
                else
                {
                    _skillData.SelectedTalentDesc = _talentTable.BuffDesc[_descIndex - 1];
                    _skillData.SelectedTalentDescNext = _talentTable.BuffDesc[_descIndex];
                    _skillData.ShowDesBoardNext = 1;
                }
            }
            else
            {
                var _attrName = ExpressionHelper.AttrName[_talentTable.AttrId];
                if (_descIndex == 0)
                {
                    //var _AttrValue = StringConvert.Level_Value_Ref(10000000 + _talentTable.SkillupgradingId,
                    //    talentCellData.Count);
                    //_skillData.SelectedTalentDesc = string.Format("{0}+{1}", _attrName,
                    //    GameUtils.AttributeValue(_talentTable.AttrId, _AttrValue));
                    _skillData.SelectedTalentDesc = string.Empty;
                    var _nextAttrValue = StringConvert.Level_Value_Ref(10000000 + _talentTable.SkillupgradingId,
                        talentCellData.Count + 1);
                    _skillData.SelectedTalentDescNext = string.Format("{0}+{1}", _attrName,
                        GameUtils.AttributeValue(_talentTable.AttrId, _nextAttrValue));
                    _skillData.ShowDesBoardNext = 1;
                }
                else if (_descIndex == _talentTable.MaxLayer)
                {
                    var _AttrValue = StringConvert.Level_Value_Ref(10000000 + _talentTable.SkillupgradingId,
                        talentCellData.Count);
                    _skillData.SelectedTalentDesc = string.Format("{0}+{1}", _attrName,
                        GameUtils.AttributeValue(_talentTable.AttrId, _AttrValue));
                    _skillData.SelectedTalentDescNext = string.Empty;
                    _skillData.ShowDesBoardNext = 0;
                    _skillData.ShowDesBoardAddButton = 0;
                }
                else
                {
                    var _AttrValue = StringConvert.Level_Value_Ref(10000000 + _talentTable.SkillupgradingId,
                        talentCellData.Count);
                    var _nextAttrValue = StringConvert.Level_Value_Ref(10000000 + _talentTable.SkillupgradingId,
                        talentCellData.Count + 1);
                    _skillData.SelectedTalentDesc = string.Format("{0}+{1}", _attrName,
                        GameUtils.AttributeValue(_talentTable.AttrId, _AttrValue));
                    _skillData.SelectedTalentDescNext = string.Format("{0}+{1}", _attrName,
                        GameUtils.AttributeValue(_talentTable.AttrId, _nextAttrValue));
                    _skillData.ShowDesBoardNext = 1;
                }
            }


            //天赋消耗刷新
            if (_talentTable.ModifySkill == -1)
            {
                var _tbUpgrade = Table.GetSkillUpgrading(_talentTable.CastItemCount);
                _skillData.TalentPanelUpgradeCast = _tbUpgrade.GetSkillUpgradingValue(talentCellData.Count);
            }
        }
        private void ResetSkillTalentDatum(SkillBoxDataModel skillBoxData)
        {
            var _count = 0;
            var _skillBoxDataSkillTalentsCount3 = skillBoxData.SkillTalents.Count;
            for (var i = 0; i < _skillBoxDataSkillTalentsCount3; i++)
            {
                _count += skillBoxData.SkillTalents[i].Count;
            }
            _count += skillBoxData.LastCount;
            skillBoxData.LastCount = _count;
            skillBoxData.CurrentCount = 0;
            if (PlayerDataManager.Instance.mSkillTalent.ContainsKey(skillBoxData.SkillId))
            {
                PlayerDataManager.Instance.mSkillTalent[skillBoxData.SkillId] = _count;
            }


            {
                // foreach(var talent in skillBoxData.SkillTalents)
                var _enumerator12 = (skillBoxData.SkillTalents).GetEnumerator();
                while (_enumerator12.MoveNext())
                {
                    var _talent = _enumerator12.Current;
                    {
                        _talent.Count = 0;
                        _talent.InitializeTalentCell();
                    }
                }
            }

            //刷新受天赋影响的技能
            PlayerDataManager.Instance.mAllSkills[skillBoxData.SkillId].RefreshSkillData();
        }
        private IEnumerator SendAddTalentPointMassageCoroutine(int talentId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.UpgradeInnate(talentId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlatformHelper.Event("skill", "addInnate");
                        PlatformHelper.UMEvent("skill", "addInnate");
                        ExchangeTalentCountToDataModel(talentId);
                        if (Table.GetTalent(talentId).ModifySkill == -1)
                        {
                            SkillDataModel.TalentResetButtonShow = true;
                            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Talant);
                        }
                        //加点成功特效提示
                        var _e = new UIEvent_SkillTalentUpEffect(talentId);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.Error_InnateNoPoint)
                        {
                            var _e = new ShowUIHintBoard(706);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            //天赋点数不足，弹出对应技能书（石）的物品信息
                            var _table = Table.GetTalent(SkillDataModel.TalentIdSelected);
                            GameUtils.ShowItemIdTip(_table.SkillItem);
                        }
                        else if (_msg.ErrorCode == (int)ErrorCodes.Error_InnateNoBefore)
                        {
                            var _e = new ShowUIHintBoard(705);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else if (_msg.ErrorCode == (int)ErrorCodes.Error_ResNoEnough)
                        {
                            var _ee = new ShowUIHintBoard(210108);
                            EventDispatcher.Instance.DispatchEvent(_ee);
                            GameUtils.ShowItemIdTip((int)eResourcesType.Spar);
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                    }
                }
            }
        }
        private IEnumerator SendResetSkillTalentMsgCoroutine(SkillBoxDataModel skillBoxData)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ResetSkillTalent(skillBoxData.SkillId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlatformHelper.Event("skill", "ResetSkillTalent", skillBoxData.SkillId);
                        PlatformHelper.UMEvent("skill", "ResetSkillTalent", skillBoxData.SkillId);

                        ResetSkillTalentDatum(skillBoxData);
                        RefurbishTalentBoardDesc(currentShowTalentId);
                        var _e = new ShowUIHintBoard(709);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                        {
                            var _e = new ShowUIHintBoard(703);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                    }
                }
            }
        }
        private IEnumerator SendResetTalentMsgCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _type = int.Parse(Table.GetClientConfig(258).Value);
                var _count = int.Parse(Table.GetClientConfig(259).Value);
                var _bagCount = PlayerDataManager.Instance.GetRes(_type);
                //检查需求道具
                if (_bagCount < _count)
                {
                    var _name = Table.GetItemBase(_type).Name;
                    var _message = string.Format(GameUtils.GetDictionaryText(701), _name);

                    var _e = new ShowUIHintBoard(_message);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    yield break;
                }

                var _msg = NetManager.Instance.ClearInnate(-1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlatformHelper.UMEvent("skill", "ResetXiuLian");
                        var _skillData = SkillDataModel;
                        var _talents = _skillData.Talents;
                        _skillData.AttrPanel.Clear();
                        _skillData.TalentResetButtonShow = false;
                        var _ee = new UIEvent_SkillFrame_RefreshTalentPanel();
                        EventDispatcher.Instance.DispatchEvent(_ee);
                        // foreach (var talent in talents)
                        var _enumerator = _talents.GetEnumerator();
                        while (_enumerator.MoveNext())
                        {
                            var _talent = _enumerator.Current;
                            if (Table.GetTalent(_talent.TalentId).ModifySkill == -1)
                            {
                                _talent.Count = 0;
                            }
                        }
                        _enumerator.Reset();
                        while (_enumerator.MoveNext())
                        {
                            var talent = _enumerator.Current;
                            if (Table.GetTalent(talent.TalentId).ModifySkill == -1)
                            {
                                talent.InitializeTalentCell();
                            }
                        }
                        _skillData.TalentCount = _msg.Response;
                        var e = new ShowUIHintBoard(709);
                        EventDispatcher.Instance.DispatchEvent(e);
                        PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Talant);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }
        //升级技能
        private IEnumerator SendUpGradeSkillMassageCoroutine(int skillid)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.UpgradeSkill(skillid);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.WeakNoticeData.SkillCanUpgrade = false;
                        PlayerDataManager.Instance.WeakNoticeData.SkillTotal = false;

                        PlatformHelper.Event("skill", "upgrade", skillid);
                        PlatformHelper.UMEvent("skill", "upgrade", skillid);
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_SkillFrame_SkillLevelUpEffect(skillid));
                        ExchangeSkillLevelFromDataModel(skillid);
                        RefreshRedDot();
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.Error_SkillLevelMax)
                        {
                            var _e = new ShowUIHintBoard(700);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                    }
                }
            }
        }
        private IEnumerator SyncEquipSkillDataCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _skillArray = new Int32Array();
                {
                    // foreach(var skillItemDataModel in SkillDataModel.EquipSkills)
                    var _enumerator2 = (SkillDataModel.EquipSkills).GetEnumerator();
                    while (_enumerator2.MoveNext())
                    {
                        var _skillItemDataModel = _enumerator2.Current;
                        {
                            _skillArray.Items.Add(_skillItemDataModel.SkillId == 0 ? -1 : _skillItemDataModel.SkillId);
                        }
                    }
                }
                var _msg = NetManager.Instance.EquipSkill(_skillArray);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        ClearCoolingDirty();
                        skillChanged = false;
                        ObjManager.Instance.PrepareMainPlayerSkillResources();
                        PlayerDataManager.Instance.RefrehEquipPriority();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error(string.Format("SyncEquipSkillData error! errorcode :{0}", _msg.ErrorCode));
                    }
                }
                else
                {
                    Logger.Error(string.Format("SyncEquipSkillData error! State :{0}", _msg.State));
                }
            }
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            var _skilldata = SkillDataModel;
            _skilldata.SelectedSkillList[0] = new SkillItemDataSelected();
            _skilldata.SelectedSkillList[1] = new SkillItemDataSelected();
            SkillDataModel.TalentIdList.Clear();

            DataModel = new SkillUiDataModel();
        }

        public void OnChangeScene(int sceneId)
        {
            RefreshRedDot();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
            RefreshRedDot();
        }

        public void Close()
        {
        }

        public void Tick()
        {
            //         var skillData = SkillDataModel;
            // 
            //         //公共cd
            //         if (skillData.CommonCoolDown > 0)
            //         {
            //             skillData.CommonCoolDown -= Time.deltaTime;
            //             if (skillData.CommonCoolDown <= 0)
            //             {
            //                 skillData.CommonCoolDown = 0;
            //             }
            //         }
            // 
            //         //技能cd
            //         int count = skillData.AllSkills.Count;
            //         for (int i = 0; i < count; i++)
            //         {
            //             var skill = skillData.AllSkills[i];
            //             if (skill.CoolDownTime > 0)
            //             {
            //                 skill.CoolDownTime -= Time.deltaTime;
            //                 if (skill.CoolDownTime <= 0)
            //                 {
            //                     skill.CoolDownTime = 0;
            //                     if (skill.ChargeLayer != skill.ChargeLayerTotal)
            //                     {
            //                         skill.ChargeLayer++;
            //                         if (skill.ChargeLayer != skill.ChargeLayerTotal)
            //                         {
            //                             skill.CoolDownTime = skill.CoolDownTimeTotal;
            //                         }
            //                     }
            //                 }
            //             }
            //         }
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as SkillFrameArguments;

            if (_args != null && _args.Tab != -1)
            {
                SkillDataModel.TabSelectIndex = _args.Tab;
            }
            else
            {
                SkillDataModel.TabSelectIndex = 0;
            }

            OnPlayerLevelUpGradeEvent(null);

            //刷新技能天赋数据,使用技能书后会变动
            var _boxes = SkillDataModel.SkillBoxes;
            var _skillTalentData = PlayerDataManager.Instance.mSkillTalent;
            var _boxesCount0 = _boxes.Count;
            for (var i = 0; i < _boxesCount0; i++)
            {
                var _box = _boxes[i];
                if (_skillTalentData.ContainsKey(_box.SkillId))
                {
                    _box.LastCount = PlayerDataManager.Instance.mSkillTalent[_box.SkillId];
                }
            }

            if (GetInherentCount() == 0)
            {
                SkillDataModel.TalentResetButtonShow = false;
            }
            else
            {
                SkillDataModel.TalentResetButtonShow = true;
            }

            RefurbishSelected(SkillDataModel.OtherSkills[0]);

            var _enumerator = SkillDataModel.OtherSkills.GetEnumerator();
            while (_enumerator.MoveNext())
            {
                var _skill = _enumerator.Current;
                if (_skill != null)
                {
                    _skill.ShowToggle = false;
                }
            }
            SkillDataModel.OtherSkills[0].ShowToggle = true;

            if (null == data)
            {
                return;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

    }
}