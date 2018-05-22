using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

namespace ScriptController
{
    public class MessageBoxExController : IControllerBase
    {

        public MessageBoxExController()
        {
            // EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_SkillSelect.EVENT_TYPE, OnClicSkillItem);
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpData);
        }

        private MessageBoxExDataModel DataModel;

        private float deltaTime = 0;
        private int NeedDiamond = 0;
        public FrameState State { get; set; }

        private void OnExDataUpData(IEvent ievent)
        {
            var e = ievent as ExDataUpDataEvent;
            if (e == null)
            {
                return;
            }

            if (e.Key == (int) eExdataDefine.e630)
            {
                var logic = GameLogic.Instance;
                if (logic == null)
                {
                    return;
                }
                var scene = logic.Scene;
                if (scene == null)
                {
                    return;
                }
                var tbScene = Table.GetScene(scene.SceneTypeId);
                if (tbScene == null)
                {
                    return;
                }
                var tbFuben = Table.GetFuben(tbScene.FubenId);
                if (tbFuben == null)
                {
                    return;
                }

                if (e.Value <= 0 && State == FrameState.Close)
                {
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MessageBoxEx));
                }

                if (e.Value > 0 && State == FrameState.Open)
                {
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MessageBoxEx));
                }
            }
            else if (e.Key == (int)eExdataDefine.e632)
            {
                var logic = GameLogic.Instance;
                if (logic == null)
                {
                    return;
                }
                var scene = logic.Scene;
                if (scene == null)
                {
                    return;
                }
                var tbScene = Table.GetScene(scene.SceneTypeId);
                if (tbScene == null)
                {
                    return;
                }
                var tbFuben = Table.GetFuben(tbScene.FubenId);
                if (tbFuben == null)
                {
                    return;
                }

                if (e.Value <= 0 && State == FrameState.Close)
                {
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MessageBoxEx));
                }

                if (e.Value > 0 && State == FrameState.Open)
                {
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MessageBoxEx));
                }
            }

        
        }

        public void CleanUp()
        {
            DataModel = new MessageBoxExDataModel();
        }
        public void RefreshData(UIInitArguments data)
        {
            if (DataModel.TimeDown <= 0 )
            {
                DataModel.TimeDown = 11;
            }
            var sceneid = GameLogic.Instance.Scene.SceneTypeId;
            var tbScene = Table.GetScene(sceneid);
            if (null == tbScene)
            {
                return;
            }
            var tbFuben = Table.GetFuben(tbScene.FubenId);
            if (null == tbFuben)
            {
                return;
            }
            if (tbFuben.AssistType == (int) eDungeonAssistType.ElfWar)
            {
                NeedDiamond = NeedDiamond = int.Parse(Table.GetClientConfig(934).Value);
            }
            else if (tbScene.FubenId == 21000)
            {
                 NeedDiamond = NeedDiamond = int.Parse(Table.GetClientConfig(941).Value);
            }
            DataModel.ContentInfo = string.Format(GameUtils.GetDictionaryText(100003300), NeedDiamond);
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
            if (DataModel.TimeDown <= 0)
            {
                return;
            }

            deltaTime += Time.deltaTime;
            if (deltaTime > 1.0f)
            {
                deltaTime = 0;

                var logic = GameLogic.Instance;
                if (logic == null)
                {
                    return;
                }
                var scene = logic.Scene;
                if (scene == null)
                {
                    return;
                }
                var tbScene = Table.GetScene(scene.SceneTypeId);
                if (tbScene == null || tbScene.FubenId<0)
                {
                    return;
                }
                var tbFuben = Table.GetFuben(tbScene.FubenId);
                if (tbFuben == null)
                {
                    return;
                }

                DataModel.TimeDown -= 1;
                if (DataModel.TimeDown < 0.001f)
                {
                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MessageBoxEx));
                }
            }
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
    }
}
