using ScriptManager;
using GameUI;
using System;
#region using

using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ClientService;
using ScorpionNetLib;
#endregion

namespace GameUI
{
    internal class WorldMapFrame : MonoBehaviour
    {
        private List<MapObjItem> SceneList = new List<MapObjItem>();

        private void Start()
        {
#if !UNITY_EDITOR
try
{
#endif

            var sceneItems = GetComponentsInChildren<MapObjItem>();
            SceneList.AddRange(sceneItems);
           var playerLevel = PlayerDataManager.Instance.GetLevel();
            
            var __array1 = sceneItems;
            var __arrayLength1 = __array1.Length;
            for (var __i1 = 0; __i1 < __arrayLength1; ++__i1)
            {
                var sceneItemLogic = __array1[__i1];
                {
                    var sceneTable = Table.GetScene(sceneItemLogic.sceneId);
                    if (null == sceneTable)
                    {
                        Logger.Error("sceneId{0} do not find !!!!", sceneItemLogic.sceneId);
                        continue;
                    }
                    var dataModel = new SceneItemDataModel();
                    dataModel.SceneId = sceneItemLogic.sceneId;
                    dataModel.TransferCast = sceneTable.ConsumeMoney;

                    dataModel.Enable = (sceneTable.IsPublic == 1) && (sceneTable.LevelLimit <= playerLevel);
                    sceneItemLogic.dataModel = dataModel;
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_SceneMap_AddSceneItemDataModel(dataModel));
                }
            }
        
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
                NetManager.Instance.StartCoroutine(OnApplyLodeInfoCoroutine(SceneList));
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        public SceneItemDataModel getItemData(int sceneId)
        {
            foreach (var v in SceneList)
            {
                if (v.sceneId == sceneId)
                    return v.dataModel;
            }
            return null;
        }
        public IEnumerator OnApplyLodeInfoCoroutine(List<MapObjItem> scenelist)
        {
            var playerLevel = PlayerDataManager.Instance.GetLevel();
            var msg = NetManager.Instance.ClientApplyHoldLode(PlayerDataManager.Instance.ServerId);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State != MessageState.Reply || msg.ErrorCode != (int) ErrorCodes.OK)
                yield break;
            foreach (var item in msg.Response.LodeList)
            {
                var datamodel = getItemData(item.SceneId);
                if(datamodel == null) continue;
                if (string.IsNullOrEmpty(item.TeamName))
                    datamodel.Text = GameUtils.GetDictionaryText(290007);
                else
                    datamodel.Text = string.Format(GameUtils.GetDictionaryText(290006), item.TeamName);
            }
        }
    }
}