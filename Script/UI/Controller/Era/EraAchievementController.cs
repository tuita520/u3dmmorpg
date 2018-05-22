#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class EraAchievementController : IControllerBase
    {
        private EraAchievementDataModel DataModel = new EraAchievementDataModel();

        private List<EraInfo> eraInfoList;

        public EraAchievementController()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(Event_EraAchvOperate.EVENT_TYPE, OnEvent_Operate);
            EventDispatcher.Instance.AddEventListener(UI_EventEraAchvItemAward.EVENT_TYPE, OnEvent_AchvAward);
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments args)
        {
            eraInfoList = EraManager.Instance.GetEraInfos(2);

            for (var i = 0; i < DataModel.ButtonDataModel.Count; ++i)
            {
                var data = DataModel.ButtonDataModel[i];
                if (i < eraInfoList.Count)
                {
                    data.EraId = eraInfoList[i].Record.Id;
                }
                else
                {
                    data.EraId = -1;
                }
            }

            DataModel.TabIndex = -1;
            SelectTabIndex(0);
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void CleanUp()
        {
        }
        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }
        public void OnShow()
        {
        }

        private void OnEvent_Operate(IEvent ievent)
        {
            var e = ievent as Event_EraAchvOperate;
            if (e == null)
            {
                return;
            }


            switch (e.Operate)
            {
                case 0:
                {
                    SelectTabIndex(e.Param0);
                }
                    break;
                case 1:
                {
                    var eraInfo = GetEraInfo(DataModel.TabIndex);
                    NetManager.Instance.StartCoroutine(TakeEraAward(eraInfo));
                }
                    break;
            } 
        }

        private void OnEvent_AchvAward(IEvent ievent)
        {
            var e = ievent as UI_EventEraAchvItemAward;
            if (e == null)
            {
                return;
            }

            NetManager.Instance.StartCoroutine(TakeAchvAward(e.Id));
        }

        #region message
        private IEnumerator TakeAchvAward(int achvId)
        {
            var tbAchv = Table.GetAchievement(achvId);
            if (null == tbAchv)
            {
                yield break;
            }

            if (PlayerDataManager.Instance.GetFlag(tbAchv.RewardFlagId))
            {
                yield break;
            }

            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.EraTakeAchvAward(achvId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.SetFlag(tbAchv.RewardFlagId);
                        var enumorator = DataModel.AchvCellList.GetEnumerator();
                        while (enumorator.MoveNext())
                        {
                            var achvItem = enumorator.Current;
                            if (achvItem != null && achvItem.Id == achvId)
                            {
                                achvItem.State = 0;
                                ++DataModel.AchvItemDataModel.CurrentCount;
                                break;
                            }
                        }

                        UpdateEraAchv(GetEraInfo(DataModel.TabIndex));
                    }
                }
            }
        }

        private IEnumerator TakeEraAward(EraInfo eraInfo)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.CSEnterEraById(eraInfo.Record.Id);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.SetFlag(eraInfo.Record.FinishFlagId);
                        UpdateEraAchv(GetEraInfo(DataModel.TabIndex));
                    }
                }
            }
        }

        #endregion

        private EraInfo GetEraInfo(int index)
        {
            return eraInfoList[index];
        }

        private void SelectTabIndex(int index)
        {
            if (DataModel.TabIndex == index)
            {
                return;
            }

            DataModel.TabIndex = index;

            ResetAchvList(GetEraInfo(index));
        }

        private void ResetAchvList(EraInfo info)
        {
            DataModel.AchvCellList.Clear();

            if (info == null)
            {
                return;
            }

            if (info.Record.ActiveType != (int)EraActiveType.Achievement)
            {
                return;
            }

            DataModel.AchvItemDataModel.CurrentCount = 0;
            for (var i = 0; i < info.Record.ActiveParam.Count; ++i)
            {
                var achievementId = info.Record.ActiveParam[i];
                if (achievementId < 0)
                {
                    continue;
                }
                var tbAchv = Table.GetAchievement(achievementId);
                if (tbAchv == null)
                {
                    continue;
                }
                var achvItem = new EraAchvCellDataModel();
                achvItem.Id = achievementId;
                var state = PlayerDataManager.Instance.GetAccomplishmentStatus(achvItem.Id);
                achvItem.State = (int) state;
                if (state == eRewardState.HasGot)
                {
                    ++DataModel.AchvItemDataModel.CurrentCount;                
                }
                var progress = Mathf.Min(PlayerDataManager.Instance.GetAccomplishmentProgress(achievementId), tbAchv.ExdataCount);
                achvItem.CurrentProgress = GameUtils.GetBigValueStr(progress);
                achvItem.MaxProgress = GameUtils.GetBigValueStr(tbAchv.ExdataCount);

                for (var j = 0; j < achvItem.Rewards.Count; ++j)
                {
                    var itemData = achvItem.Rewards[j];
                    if (j < tbAchv.ShowItems.Count)
                    {
                        itemData.ItemId = tbAchv.ShowItems[j];
                    }
                    else
                    {
                        itemData.ItemId = -1;
                    }
                }

                DataModel.AchvCellList.Add(achvItem);
            }

            UpdateEraAchv(info);
        }

        private void UpdateEraAchv(EraInfo info)
        {
            var itemData = DataModel.AchvItemDataModel;
            var finishFlag = info.Record.FinishFlagId;
            itemData.MaxCount = DataModel.AchvCellList.Count;
            var finish = PlayerDataManager.Instance.GetFlag(finishFlag);
            if (finish)
            {
                itemData.State = 2;
            }
            else
            {
                if (itemData.CurrentCount >= itemData.MaxCount)
                {
                    itemData.State = 1;
                }
                else
                {
                    itemData.State = 0;
                }
            }

            var tbMaya = Table.GetMayaBase(itemData.EraId);
            if (tbMaya != null)
            {
                itemData.AwardNotice = string.Format(GameUtils.GetDictionaryText(100001381), tbMaya.Name);
            }
        }

        public FrameState State { get; set; }
    }
}
