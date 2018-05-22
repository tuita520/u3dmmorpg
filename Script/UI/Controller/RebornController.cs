/********************************************************************************* 

                         Scorpion



  *FileName:RegenerativeFrameCtrler

  *Version:1.0

  *Date:2017-06-19

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;

#endregion

namespace ScriptController
{
    public class RegenerativeFrameCtrler : IControllerBase
    {

        #region 构造函数
        public RegenerativeFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ConditionChangeEvent.EVENT_TYPE, OnQualificationExchangeEvent);
            EventDispatcher.Instance.AddEventListener(RebornOperateEvent.EVENT_TYPE, OnRegenerativeWorkEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDatumInitializeEvent);
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDatumRenovateEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnGradeExchangeEvent);
            EventDispatcher.Instance.AddEventListener(LevelUpInitEvent.EVENT_TYPE, OnGradeExchangeEvent);
            EventDispatcher.Instance.AddEventListener(Event_UpdateMissionData.EVENT_TYPE, OnEvent);
            var controllerBase = UIManager.Instance.GetController(UIConfig.MissionTrackList);
            MissionDataModel = controllerBase.GetDataModel("") as MissionTrackListDataModel;
        }
        #endregion

        #region 成员变量
        private RebornDataModel DataModel;
        private MissionTrackListDataModel MissionDataModel;
        private TransmigrationRecord tbReborn;
        #endregion

        #region 事件
        private void OnQualificationExchangeEvent(IEvent ievent)
        {
            var _e = ievent as ConditionChangeEvent;
            TransmigrationRecord tbReborn = null;
            if (DataModel.RebornId != -1)
            {
                tbReborn = Table.GetTransmigration(DataModel.RebornId);
                if (tbReborn == null)
                {
                    return;
                }
                if (tbReborn.ConditionCount == _e.ConditionId)
                {
                    RefurbishRevealRegenerative();
                }
            }
        }

        private void OnExDatumInitializeEvent(IEvent ievent)
        {
            var _e = ievent as ExDataInitEvent;
            SetRoleId();
            SetRebornId();
            SetMissionId();
            RefurbishRevealRegenerative();
            RefurbishAnnounceDatum();
            SetSkillPart();
        }

        private void OnExDatumRenovateEvent(IEvent ievent)
        {
            var _e = ievent as ExDataUpDataEvent;
            if (_e.Key == 51)
            {
                SetRebornId();
                RefurbishRevealRegenerative();
                RefurbishAnnounceDatum();
                SetSkillPart();
            }
        }

        private void OnGradeExchangeEvent(IEvent ievent)
        {
            RefurbishAnnounceDatum();
        }

        private void OnRegenerativeWorkEvent(IEvent ievent)
        {
            var _e = ievent as RebornOperateEvent;
            switch (_e.Type)
            {
                case 0:
                {
                    Reincarnation();
                    break;
                }
                case 1:
                {
                    GoToMission();
                    break;
                }           
            }
        }

        private void GoToMission()
        {
            MissionManager.Instance.GoToMissionPlace(DataModel.MissionId);
        }

        private void OnEvent(IEvent iEvent)
        {
            SetMissionId();
        }

        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new RebornDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            if (!PlayerDataManager.Instance.GetFlag(511))
            {
                var _list = new Int32Array();
                _list.Items.Add(511);
                PlayerDataManager.Instance.SetFlagNet(_list);
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnShow()
        {
            RefurbishRevealRegenerative();
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }
        #endregion

        #region 逻辑函数
        private void RefurbishRevealRegenerative()
        {
            var _robornId = PlayerDataManager.Instance.GetExData(51);
            var _rebornId = _robornId;
            var _tbReborn = Table.GetTransmigration(_rebornId);
            if (_tbReborn == null || _tbReborn.ConditionCount == -1)
            {
                DataModel.ShowReborn = false;
                return;
            }

            var _lv = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Level;
            var _dicCon = PlayerDataManager.Instance.CheckCondition(_tbReborn.ConditionCount);
            if (_dicCon == 0)
            {
                DataModel.ShowReborn = true;
            }
            else
            {
                DataModel.ShowReborn = false;
            }
        }

        private void RefurbishAnnounceDatum()
        {
            var _tbReborn = Table.GetTransmigration(DataModel.RebornId);
            if (_tbReborn == null)
            {
                PlayerDataManager.Instance.NoticeData.Reborn = false;
                return;
            }
            var _lv = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Level;

            if (DataModel.ShowReborn && _lv >= _tbReborn.TransLevel)
            {
                PlayerDataManager.Instance.NoticeData.Reborn = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.Reborn = false;
            }
        }

        private void Reincarnation()
        {
            var _tbReborn = Table.GetTransmigration(DataModel.RebornId);
            var _lv = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Level;
            if (_lv < _tbReborn.TransLevel)
            {
                //"等级不足"
                var _e = new ShowUIHintBoard(300102);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _dicCon = PlayerDataManager.Instance.CheckCondition(_tbReborn.ConditionCount);
            if (_dicCon != 0)
            {
                //"条件不足"
                var _e = new ShowUIHintBoard(_dicCon);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            if (PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold < _tbReborn.NeedMoney)
            {
                //"金币不足"
                var _e = new ShowUIHintBoard(210100);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            if (PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.MagicDust < _tbReborn.NeedDust)
            {
                //魔尘不足
                var _e = new ShowUIHintBoard(210112);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            NetManager.Instance.StartCoroutine(ReincarnationCoroutine());
        }

        private void SetSkillPart()
        {
            var rebornId = DataModel.RebornId;
            if (rebornId > 2)
                rebornId = 2;
            var nowTbReborn = Table.GetTransmigration(rebornId);
            if (nowTbReborn == null)
                return;
            var nextTbReborn = Table.GetTransmigration(rebornId + 1);
            if (nextTbReborn == null)
                return;
            switch (DataModel.RoleId)
            {
                case 0:
                {
                    DataModel.NowSkillId = nowTbReborn.zsRebornSkill[0];
                    DataModel.NextSkillId = nextTbReborn.zsRebornSkill[0];
                    DataModel.NowSkillDes = GameUtils.GetDictionaryText(nowTbReborn.zsRebornSkillDes);
                    DataModel.NextSkillDes = GameUtils.GetDictionaryText(nextTbReborn.zsRebornSkillDes);
                    break;
                }
                case 1:
                {
                    DataModel.NowSkillId = nowTbReborn.fsRebornSkill[0];
                    DataModel.NextSkillId = nextTbReborn.fsRebornSkill[0];
                    DataModel.NowSkillDes = GameUtils.GetDictionaryText(nowTbReborn.fsRebornSkillDes);
                    DataModel.NextSkillDes = GameUtils.GetDictionaryText(nextTbReborn.fsRebornSkillDes);
                    break;
                }
                case 2:
                {
                    DataModel.NowSkillId = nowTbReborn.gsRebornSkill[0];
                    DataModel.NextSkillId = nextTbReborn.gsRebornSkill[0];
                    DataModel.NowSkillDes = GameUtils.GetDictionaryText(nowTbReborn.gsRebornSkillDes);
                    DataModel.NextSkillDes = GameUtils.GetDictionaryText(nextTbReborn.gsRebornSkillDes);
                    break;
                }
            }
        }

        private IEnumerator ReincarnationCoroutine()  
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.Reincarnation(DataModel.RebornId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new RebornPlayAnimation());                  
                        PlayerDataManager.Instance.SetExData(51, DataModel.RebornId + 1);
                        PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.AddPoint);
                        PlatformHelper.UMEvent("Reborn", DataModel.RebornId.ToString());
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_TransmigrationID
                             || _msg.ErrorCode == (int)ErrorCodes.Error_ConditionNoEnough
                             || _msg.ErrorCode == (int)ErrorCodes.MoneyNotEnough
                             || _msg.ErrorCode == (int)ErrorCodes.Error_DataOverflow)
                    {
                        var _e = new ShowUIHintBoard(_msg.ErrorCode + 200000000);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("Reincarnation Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("Reincarnation Error!............State..." + _msg.State);
                }
            }
        }

        private void SetRoleId()
        {
            DataModel.RoleId = PlayerDataManager.Instance.GetRoleId();
        }

        private void SetRebornId()
        {
            TransmigrationRecord tbReborn = null;
            if (DataModel.RebornId != -1)
            {
                tbReborn = Table.GetTransmigration(DataModel.RebornId);
                if (tbReborn == null)
                {
                    return;
                }
                ConditionTrigger.Instance.RemoveCondition(tbReborn.ConditionCount);
            }
            var _robornId = PlayerDataManager.Instance.GetExData(51);
            PlayerDataManager.Instance.PlayerDataModel.Attributes.Resurrection = _robornId;
            DataModel.RebornId = _robornId;
            tbReborn = Table.GetTransmigration(DataModel.RebornId);
            if (tbReborn == null)
            {
                return;
            }
            DataModel.RebornChangeInfo = GameUtils.GetDictionaryText(tbReborn.RebornChangeInfo);
            ConditionTrigger.Instance.PushCondition(tbReborn.ConditionCount);
            DataModel.Attributes[0].Type = 1003;
            DataModel.Attributes[0].Value = tbReborn.AttackAdd;
            DataModel.Attributes[1].Type = 10;
            DataModel.Attributes[1].Value = tbReborn.PhyDefAdd;
            DataModel.Attributes[2].Type = 11;
            DataModel.Attributes[2].Value = tbReborn.MagicDefAdd;
            DataModel.Attributes[3].Type = 19;
            DataModel.Attributes[3].Value = tbReborn.HitAdd;
            DataModel.Attributes[4].Type = 20;
            DataModel.Attributes[4].Value = tbReborn.DodgeAdd;
            DataModel.Attributes[5].Type = 13;
            DataModel.Attributes[5].Value = tbReborn.LifeAdd;

            var _tbRebornNext = Table.GetTransmigration(DataModel.RebornId + 1);
            if (_tbRebornNext == null)
            {
                for (var i = 0; i < 6; i++)
                {
                    DataModel.Attributes[5].Change = -1;
                }
                DataModel.NextId = -1;
            }
            else
            {
                DataModel.NextId = DataModel.RebornId + 1;
                DataModel.Attributes[0].Change = _tbRebornNext.AttackAdd;
                DataModel.Attributes[1].Change = _tbRebornNext.PhyDefAdd;
                DataModel.Attributes[2].Change = _tbRebornNext.MagicDefAdd;
                DataModel.Attributes[3].Change = _tbRebornNext.HitAdd;
                DataModel.Attributes[4].Change = _tbRebornNext.DodgeAdd;
                DataModel.Attributes[5].Change = _tbRebornNext.LifeAdd;
            }
        }

        private void SetMissionId()
        {
            DataModel.MissionId = MissionDataModel.List[1].MissionId;
            if (DataModel.MissionId != -1)
            {
                TransmigrationRecord tbReborn = null;
                var _robornId = PlayerDataManager.Instance.GetExData(51);
                DataModel.RebornId = _robornId;
                bool missionFlag = CheckMission(DataModel.RebornId, tbReborn);
                ShowTrailer(!missionFlag);
            }
            else
            {
                DataModel.isShowMainUIReborn = false;
                ShowTrailer(true);
            }
        }

        private void ShowTrailer(bool isShow)
        {
            DataModel.ShowTrailer = isShow;
            if (isShow)
            {
                tbReborn = Table.GetTransmigration(DataModel.RebornId);
                if (tbReborn != null)
                {
                    DataModel.RebornTrailer = GameUtils.GetDictionaryText(tbReborn.TransmigrationTrailer);
                }
            }
        }

        private bool CheckMission(int index, TransmigrationRecord tbReborn)
        {
            if (index >= 0)
            {
                tbReborn = Table.GetTransmigration(index);
                if (tbReborn == null || tbReborn.MissionGroupID.Count == 0)
                {
                    DataModel.isShowMainUIReborn = false;
                    return false;
                }
                else
                {
                    if (tbReborn.MissionGroupID.Contains(DataModel.MissionId))
                    {
                        DataModel.isShowMainUIReborn = true;
                        return true;
                    }
                    else
                    {
                        DataModel.isShowMainUIReborn = false;
                        return false ;
                    }
                }
            }
            else
                return false ;
        }

        #endregion

    }
}