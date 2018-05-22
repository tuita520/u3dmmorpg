#region using

using System;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;
using GameUI;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class EraGetNoticeController : IControllerBase
    {
        private EraBookTipDataModel DataModel = new EraBookTipDataModel();

        public EraGetNoticeController()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, OnExDataUpdate);
            EventDispatcher.Instance.AddEventListener(Event_EraNoticeFlyIcon.EVENT_TYPE, FlyIcon);
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments args)
        {
            if (args == null || args.Args == null || args.Args.Count == 0)
            {
                return;
            }

            DataModel.EraId = args.Args[0];      
        
            var tbMayaBase = Table.GetMayaBase(DataModel.EraId);
            if (tbMayaBase != null)
            {
                var dictId = 0;
                var roleId = PlayerDataManager.Instance.GetRoleId();
                if (roleId < tbMayaBase.DisplayDescIds.Count)
                {
                    dictId = tbMayaBase.DisplayDescIds[roleId];
                }
                else if (tbMayaBase.DisplayDescIds.Count > 0)
                {
                    dictId = tbMayaBase.DisplayDescIds[0];
                }

                DataModel.Desc = GameUtils.GetDictionaryText(dictId);
                if (tbMayaBase.SkillIds.Count > 0 && tbMayaBase.SkillIds[0]>0)
                {
                    DataModel.EraSkillId = tbMayaBase.SkillIds[roleId];
                }
                else
                {
                    DataModel.EraSkillId = -1;
                }
            }
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

        private void CreateNewPage(string FlyPrefab, int IconId, Vector3 From, bool OnTop, Action<GameObject> CallBack)
        {
            ComplexObjectPool.NewObject(FlyPrefab, go =>
            {
                if (go == null)
                {
                    return;
                }
                var frame = go.GetComponent<BookIconFrame>();
                frame.IconId = IconId;

                var pgo = new GameObject();
                pgo.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
                pgo.name = "flybookpage";
                var panel = pgo.AddComponent<UIPanel>();
                panel.depth = OnTop ? 2 : 0;
                pgo.transform.SetParent(UIManager.Instance.GetUIRoot(UIType.TYPE_BASE).transform);
                pgo.transform.localPosition = Vector3.zero;
                pgo.transform.localScale = Vector3.one;
                pgo.transform.localRotation = Quaternion.identity;

                go.transform.SetParent(pgo.transform);
                go.transform.localPosition = From;
                go.transform.localScale = Vector3.one;

                if (CallBack != null)
                {
                    CallBack(pgo);
                }
                else
                {
                    Logger.Error("Callback must not null!!!");
                }
            });
        }

        private void OnExDataUpdate(IEvent ievent)
        {
            var e = ievent as ExDataUpDataEvent;
            if (e == null)
            {
                return;
            }

            var exId = e.Key;
            if (exId == (int)eExdataDefine.e710)
            {
                var eraId = EraManager.Instance.CurrentEraId;
                if (eraId == -1)
                { // 第一个开启时，播个动画
                    PlayerDataManager.Instance.NeedPlayUnlock = true;
                    PlayerDataManager.Instance.CheckShowSkill(e.Value);
                }

                EraManager.Instance.RefreshByMainMission();
            }
            else if (exId == (int)eExdataDefine.e711)
            {
                PlayerDataManager.Instance.CheckShowSkill();
            }
        }

        private void PlayOpenAnim()
        {
            ObjManager.Instance.MyPlayer.StopMove();
            ObjManager.Instance.MyPlayer.LeaveAutoCombat();
            EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(1));
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));

            if (PlayerDataManager.Instance.GetFlag(3011))
            {
                return;
            }
            CreateNewPage("UI/MainUI/FlyIcon.prefab", 1201914, Vector3.zero, false, (go) =>
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                var evn = new MainUI_FlyIcon_Event();
                evn.ToUiName = Table.GetClientConfig(703).Value;
                evn.FlyIcon.From = Vector3.zero;
                evn.FlyIcon.Delay = 0.8f;
                evn.FlyIcon.Time = 0.8f;
                evn.FlyIcon.Stay = 0.4f;
                evn.FlyIcon.FlyObject = go;
                evn.Callback = (endPos) =>
                {
                    EventDispatcher.Instance.DispatchEvent(new Event_UnlockEraBook(0));
                    PlayerDataManager.Instance.SetFlag(3011, true);                
                    GuideManager.Instance.StartGuide(10020);                
                    var tbMaya = Table.GetMayaBase(0);
                    if (tbMaya != null && tbMaya.FinishFlagId > 0)
                    {
                        PlayerDataManager.Instance.SetFlag(tbMaya.FinishFlagId, true);
                        EraManager.Instance.RefreshFlagId(tbMaya.FinishFlagId);
                    }
                    EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(0));
                };
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                EventDispatcher.Instance.DispatchEvent(new MainUI_FlyIcon2_Event(evn));
                EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(1));
            });
        }

        private void FlyIcon(IEvent ievent)
        {
            var e = ievent as Event_EraNoticeFlyIcon;
            if (e == null)
            {
                return;
            }

            if (e.IsOpen)
            {
            
                EraManager.Instance.SetAnimEra(e.FirstEraId - 1);
                PlayOpenAnim();
                return;
            }

            var tbMayaBase = Table.GetMayaBase(DataModel.EraId);
            if (tbMayaBase == null)
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraGetNoticeUI));
                return;
            }

            EraManager.Instance.SetAnimEra(DataModel.EraId);

            if (tbMayaBase.FlagId >= 0)
            { // 开启功能
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraGetNoticeUI));
                GameLogic.Instance.GuideTrigger.ShowNewFunction(tbMayaBase.FlagId);    
                PlayerDataManager.Instance.SetFlag(tbMayaBase.FlagId, true);
            }
            else if (tbMayaBase.SkillIds.Count > 0 && tbMayaBase.SkillIds[0] >= 0)
            { // 开启技能
                EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(1));
                EventDispatcher.Instance.DispatchEvent(new UIEvent_ShowMainButton(false, false));
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.MissionFrame));

                ObjManager.Instance.MyPlayer.StopMove();
                ObjManager.Instance.MyPlayer.LeaveAutoCombat();

                var roleId = PlayerDataManager.Instance.GetRoleId();
                var skillId = tbMayaBase.SkillIds[roleId];
                Action flyOverAction = () =>
                {
                    PlayerDataManager.Instance.SetExData((int)eExdataDefine.e711, -1);
                    PlayerDataManager.Instance.LearnSkill(skillId);

                    //ObjManager.Instance.MyPlayer.EnterAutoCombat();
                    //MissionManager.Instance.OnEnterNormalScene();
                    EventDispatcher.Instance.DispatchEvent(new UI_BlockMainUIInputEvent(0));
                };
            
                var equipPos = PlayerDataManager.Instance.GetEquipSkillPos();                        
                var tbSkill = Table.GetSkill(skillId);
                var skillIcon = tbSkill.Icon;
                if (equipPos > 0 && tbSkill.Type == 1)
                {
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                    EventDispatcher.Instance.DispatchEvent(new Event_UnlockEraBook(DataModel.EraId));
                            
                    FlyToSkill(skillIcon, e.StartPos, equipPos, (endPos2) =>
                    {
                        flyOverAction();                    
                        EventDispatcher.Instance.DispatchEvent(new FlySkillOverPlayEffect_Event(equipPos));
                    });
                }
                else
                {
                    FlyToMayaBook(skillIcon, e.StartPos, (endPos) =>
                    {
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                        EventDispatcher.Instance.DispatchEvent(new Event_UnlockEraBook(DataModel.EraId));
                        flyOverAction();
                    });
                }


                ////飞向玛雅书
                //FlyToMayaBook(tbMayaBase.BkIconId, e.StartPos, (endPos) =>
                //{
                //    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                //    EventDispatcher.Instance.DispatchEvent(new Event_UnlockEraBook(DataModel.EraId));

                //    if (equipPos >= 0)
                //    { //飞到技能栏
                //        var tbSkill = Table.GetSkill(skillId);
                //        if (tbSkill.Type != 1)
                //        {
                //            flyOverAction();
                //            return;
                //        }

                //        var skillIcon = tbSkill.Icon;
                //        FlyToSkill(skillIcon, endPos, equipPos, (endPos2) =>
                //        {
                //            flyOverAction();
                //        });
                //    }
                //    else
                //    {
                //        flyOverAction();
                //    }
                //});
            }
        }

        private void FlyToMayaBook(int iconId, Vector3 startPos, Action<Vector3> callBack)
        {
        
            //UI/MainUI/BookPage.prefab
            CreateNewPage("UI/MainUI/FlyIcon.prefab", iconId, startPos, true, (go) =>
            {
                var path = Table.GetClientConfig(703);
                var evn = new MainUI_FlyIcon_Event(path.Value, callBack);
                evn.FlyIcon.From = startPos;
                evn.FlyIcon.Delay = 0.2f;
                evn.FlyIcon.Time = 0.7f;
                evn.FlyIcon.Stay = 0.1f;
                evn.SetAlphaTo(1.0f, 0.6f);
                evn.FlyIcon.UseBezier = true;
                evn.FlyIcon.UseRotate = true;
                evn.FlyIcon.FlyObject = go;
                EventDispatcher.Instance.DispatchEvent(new UIEvent_EraGetAlpha(0.8f, 0.01f, () =>
                {
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                    Logger.Debug("...UIEvent_EraGetAlpha ..CallBack");
                    EventDispatcher.Instance.DispatchEvent(new MainUI_FlyIcon2_Event(evn));
                }));          
            });
        }

        private void FlyToSkill(int iconId, Vector3 startPos, int equipPos, Action<Vector3> callBack)
        {
            CreateNewPage("UI/MainUI/FlyIcon.prefab", iconId, startPos, true, (go) =>
            {
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MainUI));
                var evn = new MainUI_FlyIcon_Event(equipPos, callBack);
                evn.FlyIcon.From = startPos;
                evn.FlyIcon.Delay = 0.3f;
                evn.FlyIcon.Time = 0.5f;
                evn.FlyIcon.Stay = 0.2f;
                evn.SetAlphaTo(1.0f, 1.0f);
                evn.FlyIcon.FlyObject = go;
                EventDispatcher.Instance.DispatchEvent(new MainUI_FlyIcon2_Event(evn));
            });
        }

        public FrameState State { get; set; }
    }
}
