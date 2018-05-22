using ScriptManager;
using System;
#region using
using SignalChain;
using EventSystem;
using UnityEngine;
using DataTable;
using DataContract;
using System.Collections.Generic;
using System.Collections;
using ScorpionNetLib;
using ClientService;
#endregion

namespace GameUI
{
    public class TowerUIFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        private IControllerBase controller;
        public CreateFakeCharacter ModelRoot;
        private Transform ModelRootTransform;
        private Vector3 ModelRootOriPos;
        public UILabel BossNameLabel;
        public List<UIEventTrigger> btnFloorList;
        public GameObject tipObj;
        private void Awake()
        {
#if !UNITY_EDITOR
try
{
#endif
            if (ModelRoot != null)
            {
                ModelRootTransform = ModelRoot.transform;
            }
            if (ModelRootTransform != null)
            {
                ModelRootOriPos = ModelRootTransform.localPosition;
            }

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }
        private void OnDisable()
        {
#if !UNITY_EDITOR
try
{
#endif

            EventDispatcher.Instance.RemoveEventListener(TowerRefreshBoss_Event.EVENT_TYPE, CreateFakeObj);
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
        private void OnEnable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
            controller = UIManager.Instance.GetController(UIConfig.ClimbingTowerUI);
                if (controller == null)
                {
                    return;
                }
                Binding.SetBindDataSource(controller.GetDataModel(""));
                Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel.Bags.Resources);
                EventDispatcher.Instance.AddEventListener(TowerRefreshBoss_Event.EVENT_TYPE, CreateFakeObj);

                NetManager.Instance.StartCoroutine(AskCheckTowerInfo());

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        public IEnumerator AskCheckTowerInfo()
        {
            var _msg = NetManager.Instance.CheckTowerDailyInfo(0);
            yield return _msg.SendAndWaitUntilDone();
            yield break;
        }
        private void Start()
        {
#if !UNITY_EDITOR
            try
            {
#endif
            for (int i = 0; i < btnFloorList.Count; i++)
            {
                var j = i;
                btnFloorList[i].onClick.Add(new EventDelegate(() => { OnClickFloor(j); }));
            }

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        public void OnClickClose()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ClimbingTowerUI));
        }

        public void OnClickStart()
        {
            EventDispatcher.Instance.DispatchEvent(new TowerBtnClickEvent(0)); 
        }

        public void OnClickSweep()
        {
            EventDispatcher.Instance.DispatchEvent(new TowerBtnClickEvent(1)); 
        }

        public void OnClickExitSweep()
        {
            EventDispatcher.Instance.DispatchEvent(new TowerBtnClickEvent(2)); 

        }

        public void OnClickFloor(int idx)
        {
            EventDispatcher.Instance.DispatchEvent(new TowerFloorClickEvent(idx)); 
            
        }

        public void OnClickBuySweep()
        {
            EventDispatcher.Instance.DispatchEvent(new TowerBtnClickEvent(3)); 
        }

        public void OnClickBtnTip()
        {
            tipObj.SetActive(!tipObj.activeSelf);
        }

        public void OnClickTipOther()
        {
            tipObj.SetActive(false);
        }

        public void OnClickGoToLianJin()
        {
            var guid = Table.GetGuidance(1024);
            var dic = Table.GetDictionary(guid.OpenTips);
            var condition = CheckFuctionOn(guid.TaskID, guid.State);
            if (condition != 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(dic.Desc[0]));
                return;
            }

            var list = CityManager.Instance.BuildingDataList;
            foreach (var buildingData in list)
            {
                var tb = Table.GetBuilding(buildingData.TypeId);
                if (null != tb)
                {
                    if (BuildingType.BraveHarbor == (BuildingType)tb.Type)
                    {
                        var ee = new Show_UI_Event(UIConfig.SailingUI,
                        new SailingArguments
                        {
                            BuildingData = buildingData
                        });
                        EventDispatcher.Instance.DispatchEvent(ee);
                    }
                }
            }
        }

        public int CheckFuctionOn(int iMissionId, int iMissionState)
        {
            int iCurrentMissionId = GameUtils.GetCurMainMissionId();

            if (iCurrentMissionId != iMissionId)
            {
                int index = Table.GetMissionBase(iMissionId).FlagId;
                if (PlayerDataManager.Instance.FlagData.GetFlag(index) == 0)
                {
                    return -1;
                }

            }
            else
            {
                if (iMissionState == (int)eMissionState.Unfinished)
                {
                    if ((int)MissionManager.Instance.GetMissionState(iCurrentMissionId) == (int)eMissionState.Acceptable)
                    {
                        return -1;
                    }
                }
                else if (iMissionState == (int)eMissionState.Finished)
                {
                    if ((int)MissionManager.Instance.GetMissionState(iCurrentMissionId) != (int)eMissionState.Finished)
                    {
                        return -1;
                    }
                }
            }
            return 0;
        }

        private void CreateFakeObj(IEvent ievent)
        {
            int dataId = (ievent as TowerRefreshBoss_Event).idBosd;
            if (ModelRoot != null)
            {
                ModelRoot.DestroyFakeCharacter();
            }
            if (-1 == dataId)
            {
                return;
            }

            var tableNpc = Table.GetCharacterBase(dataId);
            if (null == tableNpc)
            {
                return;
            }
            if (ModelRoot != null)
            {
                ModelRoot.Create(dataId, null, character =>
                {
                    character.SetScale(tableNpc.CameraMult / 10000f);
                    character.ObjTransform.localRotation = Quaternion.identity;
                    ModelRootTransform.localPosition = ModelRootOriPos + new Vector3(0, tableNpc.CameraHeight / 10000.0f, 0);
                    character.PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                });
            }
          if (BossNameLabel != null)
            {
                BossNameLabel.text = tableNpc.Name;
            }
        }
    }
}