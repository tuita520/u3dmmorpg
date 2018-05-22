#region using
using System;
using EventSystem;
using UnityEngine;
using System.Collections.Generic;
using ClientDataModel;

#endregion

namespace GameUI
{
    public class ChickenSceneMapFrame : MonoBehaviour
    {
        public BindDataRoot Binding;
        public Transform CurrentMap;
        public UITexture Texture;
        private readonly Dictionary<ulong, ListItemLogic> itemLogicDict = new Dictionary<ulong, ListItemLogic>();
        public Transform CharCursor;
        public Transform SafeSp;
        public void OnClickClose()
        {
            var e = new Close_UI_Event(UIConfig.ChickenSceneMapUI);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickMapLoc()
        {
            var worldPos = UICamera.currentCamera.ScreenToWorldPoint(UICamera.lastTouchPosition);
            var localPos = Texture.transform.InverseTransformPoint(worldPos);
            Logger.Info("Touch Postion {0}", localPos);
            var e = new ChickenMapSceneClickLoction(localPos);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickPlayers()
        {
        }

        public void OnClickSharePostion()
        {
            var arg = new ChatMainArguments { Type = 1 };
            var e = new Show_UI_Event(UIConfig.ChatMainFrame, arg);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        public void OnClickWorldMap()
        {
        }

        private void OnDisable()
        {
#if !UNITY_EDITOR
            try
            {
#endif
            EventDispatcher.Instance.RemoveEventListener(ChickenSceneMapRadar.EVENT_TYPE, OnShowRadar);
            EventDispatcher.Instance.RemoveEventListener(ChickenSceneMapRemoveRadar.EVENT_TYPE, OnRemoveShowRadar);

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
            EventDispatcher.Instance.AddEventListener(ChickenSceneMapRadar.EVENT_TYPE, OnShowRadar);
            EventDispatcher.Instance.AddEventListener(ChickenSceneMapRemoveRadar.EVENT_TYPE, OnRemoveShowRadar);

            var controllerBase = UIManager.Instance.GetController(UIConfig.ChickenSceneMapUI);
            if (controllerBase == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }

        private void OnShowRadar(IEvent ievent)
        {
            var e = ievent as ChickenSceneMapRadar;
            if (e == null)
                return;
            var data = e.DataModel;
            if (itemLogicDict.ContainsKey(data.CharacterId))
            {
                itemLogicDict[data.CharacterId].gameObject.SetActive(true);
                return;
            }
            if (e.Prefab != "")
            {
                CreateCharRadar(data, e.Prefab);
            }
        }

        private void OnRemoveShowRadar(IEvent ievent)
        {
            var e = ievent as ChickenSceneMapRemoveRadar;
            if (e == null)
                return;

            RemoveCharRadar(e.id);
        }

        private void CreateCharRadar(MapRadarDataModel data, string prefab)
        {
            var id = data.CharacterId;
            ComplexObjectPool.NewObject(prefab, o =>
            {
                var oTransform = o.transform;
                oTransform.SetParentEX(CharCursor.transform);
                oTransform.localScale = Vector3.one;
                o.SetActive(true);
                var i = o.GetComponent<ListItemLogic>();
                i.Item = data;
                var r = o.GetComponent<BindDataRoot>();
                r.Source = data;

                itemLogicDict[data.CharacterId] = i;
            });
        }
        private void RemoveCharRadar(ulong id)
        {
            ListItemLogic obj;
            if (itemLogicDict.TryGetValue(id, out obj))
            {
                obj.gameObject.SetActive(false);
                ComplexObjectPool.Release(obj.gameObject);
                itemLogicDict.Remove(id);
            }
        }

        public void OnMapSceneMsgCancel()
        {
            EventDispatcher.Instance.DispatchEvent(new MapSceneMsgOperation(1));
        }

        public void OnMapSceneMsgCheck()
        {
            EventDispatcher.Instance.DispatchEvent(new MapSceneMsgOperation(2));
        }

        public void OnMapSceneMsgOK()
        {
            EventDispatcher.Instance.DispatchEvent(new MapSceneMsgOperation(0));
        }

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
    }
}