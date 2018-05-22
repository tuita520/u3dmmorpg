/********************************************************************************* 

                         Scorpion



  *FileName:ResurgeFrameCtrler

  *Version:1.0

  *Date:2017-06-20

  *Description:

**********************************************************************************/


#region using

using System.Collections;
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
    public class ResurgeFrameCtrler : IControllerBase
    {

        #region 构造函数
        public ResurgeFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(RelieveOperateEvent.EVENT_TYPE, OnResurgeActEvent);
            EventDispatcher.Instance.AddEventListener(RefreshReliveInfoEvent.EVENT_TYPE, OnRefurbishResurgeInformationEvent);
        }
        #endregion

        #region 成员变量
        private int mFreeTime = 0;

        private ReliveDataModel DataModel;
        #endregion

        #region 事件
        private void OnRefurbishResurgeInformationEvent(IEvent ievent)
        {
            var _e = ievent as RefreshReliveInfoEvent;
            var _name = _e.KillerName;
            float time = 0;

            if (null != GameLogic.Instance)
            {
                if (GameLogic.Instance.Scene != null)
                {
                    time = GameLogic.Instance.Scene.TableScene.SafeReliveCD;
                }
            }

            if (time > 0)
            {
                DataModel.FreeClick = false;
            }
            else
            {
                DataModel.FreeClick = true;
                time = 0.5f;
            }
            DataModel.FreeTime = Game.Instance.ServerTime.AddSeconds(time);
            DataModel.KillName = _name;

            if (UIConfig.MainUI.Visible())
            {
                var _ee = new Show_UI_Event(UIConfig.ReliveUI);
                EventDispatcher.Instance.DispatchEvent(_ee);
            }
        }

        private void OnResurgeCountdownEvent(IEvent ievent)
        {
            DataModel.FreeClick = true;
        }

        private void OnResurgeActEvent(IEvent ievent)
        {
            var _e = ievent as RelieveOperateEvent;
            switch (_e.Type)
            {
                case 0:
                {
                    ResurgeStane();
                }
                    break;
                case 1:
                {
                    ResurgeDiamond();
                }
                    break;
                case 2:
                {
                    ResurgeFranco();
                }
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region 逻辑函数
        private void ResurgeDiamond()
        {
            var _dia = Table.GetClientConfig(900).ToInt();
            if (PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Diamond < _dia)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210102));
                /*
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 1043, "",
                () => { EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame)); });
             * */
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame));
                return;
            }
            NetManager.Instance.StartCoroutine(ResurgeTypeCoroutine(1));
        }

        private IEnumerator SecedePitCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ExitDungeon(-10);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _logic = GameLogic.Instance;
                        if (_logic == null)
                        {
                            yield break;
                        }
                        var _scene = _logic.Scene;
                        if (_scene == null)
                        {
                            yield break;
                        }
                        var _tbScene = Table.GetScene(_scene.SceneTypeId);
                        if (_tbScene == null)
                        {
                            yield break;
                        }
                        PlatformHelper.UMEvent("Fuben", "Exit", _tbScene.FubenId.ToString());
                    }
                    else
                    {
                        Logger.Error(".....ExitDungeon.......{0}.", _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error(".....ExitDungeon.......{0}.", _msg.State);
                }
            }
        }

        private void ResurgeFranco()
        {
            if (IsDescInstance())
            {
                NetManager.Instance.StartCoroutine(SecedePitCoroutine());
            }
            else
            {
                NetManager.Instance.StartCoroutine(ResurgeTypeCoroutine(2));
            }
        }

        private void ResurgeStane()
        {
            if (PlayerDataManager.Instance.GetItemTotalCount(22019).Count <= 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(1042));
                GameUtils.GotoUiTab(79, 3);
                //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame));
                return;
            }
            NetManager.Instance.StartCoroutine(ResurgeTypeCoroutine(0));
        }

        private IEnumerator ResurgeTypeCoroutine(int t)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ReliveType(t);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _e = new Close_UI_Event(UIConfig.ReliveUI);
                        EventDispatcher.Instance.DispatchEvent(_e);

                        var _e1 = new Show_UI_Event(UIConfig.MainUI);
                        EventDispatcher.Instance.DispatchEvent(_e1);
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterNoDie)
                    {
                        //TODO
                        var _e = new Close_UI_Event(UIConfig.ReliveUI);
                        EventDispatcher.Instance.DispatchEvent(_e);

                        var _e1 = new Show_UI_Event(UIConfig.MainUI);
                        EventDispatcher.Instance.DispatchEvent(_e1);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("ReliveType Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("ReliveType Error!............State..." + _msg.State);
                }
            }
        }

        private bool IsDescInstance()
        {
            var _logic = GameLogic.Instance;
            if (_logic == null)
            {
                return false;
            }
            var _scene = _logic.Scene;
            if (_scene == null)
            {
                return false;
            }
            var _tbScene = Table.GetScene(_scene.SceneTypeId);
            if (_tbScene == null)
            {
                return false;
            }

            if (_tbScene.SafeReliveCD != -1)
            {
                return false;
            }

            var _tbFuben = Table.GetFuben(_tbScene.FubenId);
            if (_tbFuben == null)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new ReliveDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {

            //DataModel.StoneCount = PlayerDataManager.Instance.GetItemTotalCount(22019);
            DataModel.StoneCount.Count = 1;
            if (DataModel.FreeClick)
            {
                DataModel.FreeTime = Game.Instance.ServerTime.AddSeconds(0.5f);
            }

            //		float time = 0;
            //         if (DataModel.FreeTime < Game.Instance.ServerTime)
            //         {
            // 			if (null != GameLogic.Instance)
            // 			{
            // 				time = GameLogic.Instance.Scene.TableScene.SafeReliveCD;
            // 			}
            // 			if (time > 0)
            // 			{
            // 				DataModel.FreeClick = false;
            // 			}
            // 			else
            // 			{
            // 				DataModel.FreeClick = true;
            // 				time = 0.5f;
            // 			}
            // 			DataModel.FreeTime = Game.Instance.ServerTime.AddSeconds(time);
            //         }


            DataModel.FuHuoDiamond = Table.GetClientConfig(900).ToInt();

            if (IsDescInstance())
            {
                DataModel.IsShowFuHuoTime = false;
                DataModel.SafeFuHuoDesc = GameUtils.GetDictionaryText(100001188);
            }
            else
            {
                DataModel.IsShowFuHuoTime = true;
                DataModel.SafeFuHuoDesc = GameUtils.GetDictionaryText(100000690);
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
            if (false == DataModel.FreeClick && 0 == Time.frameCount % 15)
            {
                if ((Game.Instance.ServerTime - DataModel.FreeTime).TotalSeconds >= 0)
                {
                    DataModel.FreeClick = true;
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

        public FrameState State { get; set; }
        #endregion

    }
}