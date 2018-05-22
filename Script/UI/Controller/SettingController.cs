/********************************************************************************* 

                         Scorpion



  *FileName:SettingFrameCtrler

  *Version:1.0

  *Date:2017-07-13

  *Description:

**********************************************************************************/


#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class SettingFrameCtrler : IControllerBase
    {
        #region 构造函数
        public SettingFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDatumInitializeEvent);
            EventDispatcher.Instance.AddEventListener(SettingExdataUpdate.EVENT_TYPE, OnExDatumUpDataEvent);
            EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagUpdateEvent);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagDataInitEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_RefreshPush.EVENT_TYPE, OnRefurbishPushEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_QualitySetting.EVENT_TYPE, OnQualityChangeEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_ResolutionSetting.EVENT_TYPE, OnResolutionChangeEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_VisibleEyeClick.EVENT_TYPE, OnSeenEyeClickEvent);
            EventDispatcher.Instance.AddEventListener(SettingShowMessageBoxEvent.EVENT_TYPE, OnMessageBoxShowEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_VisibleEyeCanBeStart.EVENT_TYPE, SetVisibleStart);
            EventDispatcher.Instance.AddEventListener(SettingOperateModifyPlayerNameEvent.EVENT_TYPE, OnSettingOperateModifyPlayerNameEvent);
        }



        #endregion

        #region 成员变量
        private Action QualityChanged;
        private SettingDataModel BackUpModel { get; set; }

        private AutoCombatData CombatData
        {
            get { return DataModel.AutoCombat; }
        }

        private SettingDataModel DataModel { get; set; }

        private int lastGameQuality;
        #endregion

        #region 逻辑函数
        //技能自动瞄准开关
        private void AutoCollimate()
        {
            var _visible = DataModel.SystemSetting.Other[4];
            GameSetting.Instance.TargetSelectionAssistant = !_visible;
        }

        //摄像机震动开关
        private void CameraShock()
        {
            var _visible = DataModel.SystemSetting.Other[5];
            GameSetting.Instance.CameraShakeEnable = !_visible;
        }

        private bool CanPiackUpItem(int itemId)
        {
            return true;////设置界面打不打勾控制回收不再控制拾取，打不打勾都拾取
            //var _SetData = DataModel.AutoCombat;
            //if (itemId == 2)
            //{
            //    if (_SetData.Pickups[4])
            //    {
            //        return true;
            //    }
            //    return false;
            //}

            //var _tbItem = Table.GetItemBase(itemId);
            //if (_tbItem.Type >= 10000 && _tbItem.Type <= 10099)
            //{
            //    if (_SetData.Pickups[_tbItem.Quality])
            //    {
            //        return true;
            //    }
            //    return false;
            //    if (_tbItem.Quality >= 0 && _tbItem.Quality <= 3)//白绿蓝紫装备自动回收 特殊处理
            //    {
            //        //if (_SetData.Pickups[_tbItem.Quality])
            //        //{
            //        //    return false;
            //        //}
            //        return true;
            //    }
            //    else if (_tbItem.Quality <= 7)//防止越界
            //    {
            //        if (_SetData.Pickups[_tbItem.Quality])
            //        {
            //            return true;
            //        }
            //        return false;
            //    }
            //}
            //if (_tbItem.Type == 30000)
            //{
            //    if (_SetData.Pickups[5])
            //    {
            //        return true;
            //    }
            //    return false;
            //}

            //if (_tbItem.Type == 24000)
            //{
            //    if (_SetData.Pickups[6])
            //    {
            //        return true;
            //    }
            //    return false;
            //}
            //if (_SetData.Pickups[7])
            //{
            //    return true;
            //}
            //return false;
        }
        private bool CanAutoRecyleItem(int itemId)
        {
            var _SetData = DataModel.AutoCombat;
            var _tbItem = Table.GetItemBase(itemId);
            if (_tbItem.Type >= 10000 && _tbItem.Type <= 10099)
            {
                if (_tbItem.Quality >= _SetData.Pickups.Count)
                {
                    return false;
                }
                if (_tbItem.Quality >= 0 && _tbItem.Quality <= 3)//白绿蓝紫装备自动回收 特殊处理
                {
                    if (GameUtils.GetNotRecoveryEquipList().Contains((itemId)))
                    {
                        return false;
                    }
                    if (_SetData.Pickups[_tbItem.Quality])
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
        private void ChangeFps()
        {
            PlayerPrefs.SetInt(GameSetting.LowFpsKey, DataModel.SystemSetting.Other[7] ? 30 : 60);
            var _fps = DataModel.SystemSetting.Other[7] ? 30 : 60;
            Application.targetFrameRate = _fps;
            PlayerPrefs.Save();
        }
        private void OtherPlayerEffectSeen()
        {
            var _visible = DataModel.SystemSetting.Other[3];
            GameSetting.Instance.ShowEffect = !_visible;
        }
        private void OtherPlayerSeen()
        {
            var _visible = DataModel.SystemSetting.Other[2];
            GameSetting.Instance.ShowOtherPlayer = !_visible;
        }

        private void OtherPlayerSeenTitle()
        {
            var _visible = DataModel.SystemSetting.Other2[0];
            GameSetting.Instance.ShowOtherPlayerNameTitle = !_visible;
        }

        //屏幕节电
        private void PowerSave()
        {
            var _visible = DataModel.SystemSetting.Other[6];
            GameSetting.Instance.PowerSaveEnabe = !_visible;
        }
        private void RegisterPropertyChanged()
        {
            DataModel.PushList.PropertyChanged += (sender, args) =>
            {
                int id;
                if (int.TryParse(args.PropertyName, out id))
                {
                    var _key = string.Format("PushKey{0}", id);
                    var _value = DataModel.PushList[id] ? 1 : 0;
                    PlayerPrefs.SetInt(_key, _value);
                    RefurbishPushById(id);
                }
            };

            DataModel.SystemSetting.Other.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "0")
                {
                    PlayerDataManager.Instance.SetFlag(480, DataModel.SystemSetting.Other[0]);
                    PlatformHelper.Event("setting", "other", 0);
                }
                else if (args.PropertyName == "1")
                {
                    PlayerDataManager.Instance.SetFlag(481, DataModel.SystemSetting.Other[1]);
                    PlatformHelper.Event("setting", "other", 1);
                }
                else if (args.PropertyName == "2")
                {
                    OtherPlayerSeen();
                    PlayerDataManager.Instance.SetFlag(482, DataModel.SystemSetting.Other[2]);
                    PlatformHelper.Event("setting", "other", 2);
                    RefreshSeenEye();
                }
                else if (args.PropertyName == "3")
                {
                    OtherPlayerEffectSeen();
                    PlayerDataManager.Instance.SetFlag(483, DataModel.SystemSetting.Other[3]);
                    PlatformHelper.Event("setting", "other", 3);
                    RefreshSeenEye();
                }
                else if (args.PropertyName == "4")
                {
                    AutoCollimate();
                    PlayerDataManager.Instance.SetFlag(488, DataModel.SystemSetting.Other[4]);
                    PlatformHelper.Event("setting", "other", 4);
                }
                else if (args.PropertyName == "5")
                {
                    CameraShock();
                    PlayerDataManager.Instance.SetFlag(489, DataModel.SystemSetting.Other[5]);
                    PlatformHelper.Event("setting", "other", 5);
                }
                else if (args.PropertyName == "6")
                {
                    PowerSave();
                    PlayerDataManager.Instance.SetFlag(490, DataModel.SystemSetting.Other[6]);
                    PlatformHelper.Event("setting", "other", 6);
                }
                else if (args.PropertyName == "7")
                {
                    ChangeFps();
                }
            };
            DataModel.SystemSetting.Other2.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "0")
                {
                    OtherPlayerSeenTitle();
                }
            };

            DataModel.SystemSetting.Sound.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == "0")
                    {
                        SoundManager.Instance.EnableBGM = !DataModel.SystemSetting.Sound[0];
                        PlayerPrefs.SetInt(SoundManager.BGMPrefsKey, SoundManager.Instance.EnableBGM ? 1 : 0);
                        PlatformHelper.Event("setting", "sound", 0);
                    }
                    else if (args.PropertyName == "1")
                    {
                        SoundManager.Instance.EnableSFX = !DataModel.SystemSetting.Sound[1];
                        PlayerPrefs.SetInt(SoundManager.SFXPrefsKey, SoundManager.Instance.EnableSFX ? 1 : 0);
                        PlatformHelper.Event("setting", "sound", 1);
                    }
                };

            DataModel.AutoCombat.Pickups.PropertyChanged +=
                (sender, args) => { EventDispatcher.Instance.DispatchEvent(new UIEvent_PickSettingChanged()); };
        }
        private void RefreshSeenEye()
        {
            var isInPvP = false;
            if (null != GameLogic.Instance)
            {
                var sceneType = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
                if (null == sceneType)
                    return;
                isInPvP = sceneType.Type == (int)eSceneType.Pvp; //PvP场景特殊处理
            }

            if (DataModel.SystemSetting.Other[3] && (DataModel.SystemSetting.Other[2] || isInPvP))
            {
                DataModel.SystemSetting.VisibleEye = 0;
                DataModel.SystemSetting.VisibleEyeTipShow = false;
                GameSetting.Instance.GameQualityLevel = -3;
            }
            else
            {
                DataModel.SystemSetting.VisibleEye = 1;
                GameSetting.Instance.GameQualityLevel = PlayerPrefs.GetInt(GameSetting.GameQuilatyKey, GameSetting.Instance.GameQualityLevel);
            }
        }
        private IEnumerator ReloadSceneCorotinue(int level)
        {
            using (new BlockingLayerHelper(0))
            {
                var _placeHolder = 0;
                var _msg = NetManager.Instance.ApplyPlayerData(_placeHolder);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //目前只把buff列表发过来了,其他东西客户端自己都知道
                        var _oldData = PlayerDataManager.Instance.mInitBaseAttr;
                        var _myPlayer = ObjManager.Instance.MyPlayer;
                        var _data = new PlayerData();
                        _data.CharacterId = _myPlayer.CharacterBaseData.CharacterId;
                        _data.SceneId = _oldData.SceneId;
                        _data.Name = _myPlayer.Name;
                        _data.RoleId = _myPlayer.RoleId;
                        _data.Level = _myPlayer.GetLevel();
                        _data.MoveSpeed = _myPlayer.GetMoveSpeed();
                        _data.Camp = _myPlayer.GetCamp();
                        _data.X = _myPlayer.Position.x;
                        _data.Y = _myPlayer.Position.z;
                        _data.MpMax = PlayerDataManager.Instance.GetAttribute(eAttributeType.MpMax);
                        _data.MpMow = PlayerDataManager.Instance.GetAttribute(eAttributeType.MpNow);
                        _data.HpMax = PlayerDataManager.Instance.GetAttribute(eAttributeType.HpMax);
                        _data.HpNow = PlayerDataManager.Instance.GetAttribute(eAttributeType.HpNow);
                        _data.AreaState = (int)_myPlayer.AreaState;
                        var _enumeraotr = _myPlayer.EquipList.GetEnumerator();
                        while (_enumeraotr.MoveNext())
                        {
                            var _equip = _enumeraotr.Current;
                            _data.EquipsModel.Add(_equip.Key, _equip.Value);
                        }
                        //暂时没有用到
                        _data.SceneGuid = _oldData.SceneGuid;

                        var _buffs = _msg.Response.Buff;
                        _data.Buff.AddRange(_buffs);
                        _data.MountId = _msg.Response.MountId;
                        PlayerDataManager.Instance.mInitBaseAttr = _data;
                        yield return new WaitForSeconds(0.1f);
                        //QualityChanged = () =>
                        {
                            //手机上设置质量等级会卡主,所以挪到这里执行,缓存里的资源也需要从新加载
                            ResourceManager.Instance.ClearCache(true);
                            GameSetting.Instance.GameQualityLevel = level;
                            PlayerPrefs.SetInt(GameSetting.GameQuilatyKey, level);
                        }
                        ;
                        Application.LoadLevel("Loading");
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }
        #endregion

        #region 事件
        private void OnExDatumInitializeEvent(IEvent ievent)
        {
            var _e = ievent as ExDataInitEvent;
            var _playerData = PlayerDataManager.Instance;
            CombatData.Hp = _playerData.GetExData(59) / 100.0f;
            CombatData.Mp = _playerData.GetExData(60) / 100.0f;
            var _FanJi = _playerData.GetExData(628);
            CombatData.IsResponded = BitFlag.GetLow(_FanJi, 0);
            var _pick = _playerData.GetExData(61);
            for (var i = 0; i < 8; i++)
            {
                CombatData.Pickups[i] = BitFlag.GetLow(_pick, i);
            }
            if (BitFlag.GetLow(_pick, 9))
            {
                CombatData.Ranges[2] = true;
                CombatData.Ranges[0] = false;
                CombatData.Ranges[1] = false;
            }
            else
            {
                if (BitFlag.GetLow(_pick, 8))
                {
                    CombatData.Ranges[1] = true;
                    CombatData.Ranges[0] = false;
                    CombatData.Ranges[2] = false;
                }
                else
                {
                    CombatData.Ranges[0] = true;
                    CombatData.Ranges[1] = false;
                    CombatData.Ranges[2] = false;
                }
            }

            //初始化 广播表
            Table.ForeachBroadcast(record =>
            {
                if (record != null && record.Time != null)
                {
                    for (int j = 0; j < record.Time.Count; ++j)
                    {
                        var _tableTime = Game.Instance.ServerTime.Date.AddSeconds(record.Time[j]);
                        if (DateTime.Now > _tableTime)
                        {
                            _tableTime = _tableTime.AddDays(1);
                        }
                        var _different = (_tableTime - DateTime.Now).TotalSeconds;

                        if (GameUtils.CheckIsWeekLoopOk(record.WeekLoop))
                        {
                            TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(_different), () =>
                            {
                                var _content = new ChatMessageContent();
                                var _str = GameUtils.GetDictionaryText(record.DictId);
                                if (_str == null) return;

                                _content.Content = _str;

                                GameUtils.OnReceiveChatMsg((int)eChatChannel.SystemScroll, 0, string.Empty, _content);

                                //发事件通知主界面显示提示条
                                EventDispatcher.Instance.DispatchEvent(new ClientBroadCastEvent(record.Id));
                            }, (int)TimeSpan.FromDays(1).TotalMilliseconds);
                        }
                    }
                }
                return true;
            });
        }
        private void OnExDatumUpDataEvent(IEvent ievent)
        {
            var _e = ievent as SettingExdataUpdate;
            var _playerData = PlayerDataManager.Instance;
            switch (_e.Type)
            {
                case eExdataDefine.e59:
                    {
                        CombatData.Hp = _playerData.GetExData(eExdataDefine.e59) / 100.0f;
                    }
                    break;
                case eExdataDefine.e60:
                    {
                        CombatData.Mp = _playerData.GetExData(eExdataDefine.e60) / 100.0f;
                    }
                    break;
                case eExdataDefine.e61:
                    {
                        var _pick = _playerData.GetExData(eExdataDefine.e61);
                        if (BitFlag.GetLow(_pick, 9))
                        {
                            CombatData.Ranges[2] = true;
                            CombatData.Ranges[0] = false;
                            CombatData.Ranges[1] = false;
                        }
                        else
                        {
                            if (BitFlag.GetLow(_pick, 8))
                            {
                                CombatData.Ranges[1] = true;
                                CombatData.Ranges[0] = false;
                                CombatData.Ranges[2] = false;
                            }
                            else
                            {
                                CombatData.Ranges[0] = true;
                                CombatData.Ranges[1] = false;
                                CombatData.Ranges[2] = false;
                            }
                        }
                        for (var i = 0; i < 8; i++)
                        {
                            CombatData.Pickups[i] = BitFlag.GetLow(_pick, i);
                        }
                    }
                    break;
                case eExdataDefine.e628:
                    {
                        var _pick = _playerData.GetExData(eExdataDefine.e628);
                        CombatData.IsResponded = BitFlag.GetLow(_pick, 0);
                    }
                    break;
            }
        }
        private void OnSeenEyeClickEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_VisibleEyeClick;
            if (_e == null) return;

            if (null == GameLogic.Instance)
                return;
            var sceneType = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
            if (null == sceneType)
                return;
            if (sceneType.Type == (int)eSceneType.Pvp)//PvP场景特殊处理
            {
                DataModel.SystemSetting.Other[2] = false;//强制关闭屏蔽
                DataModel.SystemSetting.Other[3] = _e.Visible;
            }
            else
            {
                DataModel.SystemSetting.Other[2] = _e.Visible;//屏蔽其他玩家
                DataModel.SystemSetting.Other[3] = _e.Visible;//屏蔽他人特效
            }
        }
        private void OnSettingOperateModifyPlayerNameEvent(IEvent ievent)
        {
            var evt = ievent as SettingOperateModifyPlayerNameEvent;
            switch (evt.Type)
            {
                case 0:
                    NetManager.Instance.StartCoroutine(ModifyPlayerName(evt.ChangeName));
                    break;
            }

        }
        private void OnMessageBoxShowEvent(IEvent ievent)
        {
            var evt = ievent as SettingShowMessageBoxEvent;

            switch (evt.Type)
            {
                case 0:

                    if (DataModel.AutoCombat.Pickups[3])
                    {
                        DataModel.AutoCombat.Pickups[3] = false;
                    }
                    else
                    {
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 100003019, "", () =>
                        {
                            DataModel.AutoCombat.Pickups[3] = true;
                        });
                    }
                    break;
                case 1:
                    if (DataModel.AutoCombat.IsResponded)
                    {
                        DataModel.AutoCombat.IsResponded = false;
                    }
                    else
                    {
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 270330, "", () =>
                        {
                            DataModel.AutoCombat.IsResponded = true;
                        });
                    }
                    break;
                default:
                    break;
            }

        }
        private void OnFlagDataInitEvent(IEvent ievent)
        {
            //初始化画质,一定要在最前
            Game.SetGameQuality();
            DataModel.SystemSetting.Other[0] = PlayerDataManager.Instance.GetFlag(480);
            DataModel.SystemSetting.Other[1] = PlayerDataManager.Instance.GetFlag(481);
            DataModel.SystemSetting.Other[2] = PlayerDataManager.Instance.GetFlag(482);
            DataModel.SystemSetting.Other[3] = PlayerDataManager.Instance.GetFlag(483);
            DataModel.SystemSetting.Other[4] = PlayerDataManager.Instance.GetFlag(488);
            DataModel.SystemSetting.Other[5] = PlayerDataManager.Instance.GetFlag(489);
            DataModel.SystemSetting.Other[6] = PlayerDataManager.Instance.GetFlag(490);

            DataModel.SystemSetting.Other2[0] = !GameSetting.Instance.ShowOtherPlayerNameTitle;
            RefreshSeenEye();
            OtherPlayerSeen();
            OtherPlayerSeenTitle();
            OtherPlayerEffectSeen();
            CameraShock();
            AutoCollimate();
            PowerSave();
        }
        private void OnFlagUpdateEvent(IEvent ievent)
        {
            var _e = ievent as FlagUpdateEvent;
            var _index = _e.Index;
            //拒绝组队
            if (_index == 480)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[0] != _e.Value)
                {
                    DataModel.SystemSetting.Other[0] = _e.Value;
                }
            }
            //拒绝私聊
            else if (_index == 481)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[1] != _e.Value)
                {
                    DataModel.SystemSetting.Other[1] = _e.Value;
                }
            }
            //屏蔽其他玩家
            else if (_index == 482)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[2] != _e.Value)
                {
                    DataModel.SystemSetting.Other[2] = _e.Value;
                }
            }
            //屏蔽他人特效
            else if (_index == 483)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[3] != _e.Value)
                {
                    DataModel.SystemSetting.Other[3] = _e.Value;
                }
            }
            //技能自动瞄准
            else if (_index == 488)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[4] != _e.Value)
                {
                    DataModel.SystemSetting.Other[4] = _e.Value;
                }
            }
            //摄像机震动
            else if (_index == 489)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[5] != _e.Value)
                {
                    DataModel.SystemSetting.Other[5] = _e.Value;
                }
            }
            //屏幕节电
            else if (_index == 490)
            {
                if (DataModel.SystemSetting.Other != null && DataModel.SystemSetting.Other[6] != _e.Value)
                {
                    DataModel.SystemSetting.Other[6] = _e.Value;
                }
            }
        }
        private void OnQualityChangeEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_QualitySetting;
            DataModel.SystemSetting.QualityToggle = _e.level;
            GameSetting.Instance.GameQualityLevel = _e.level;
            lastGameQuality = _e.level;
        }
        private void OnResolutionChangeEvent(IEvent ievent)
        {
            // var _e = ievent as UIEvent_ResolutionSetting;
            //  DataModel.SystemSetting.Resolution = _e.level;
            //  GameSetting.Instance.GameResolutionLevel = _e.level;
        }

        #endregion

        #region 固有函数
        public void CleanUp()
        {

            DataModel = new SettingDataModel();
            BackUpModel = new SettingDataModel();

            DataModel.SystemSetting.Sound[0] = PlayerPrefs.GetInt(SoundManager.BGMPrefsKey, 1) == 0;
            DataModel.SystemSetting.Sound[1] = PlayerPrefs.GetInt(SoundManager.SFXPrefsKey, 1) == 0;
            SoundManager.Instance.EnableBGM = !DataModel.SystemSetting.Sound[0];
            SoundManager.Instance.EnableSFX = !DataModel.SystemSetting.Sound[1];

            var _lowFps = PlayerPrefs.GetInt(GameSetting.LowFpsKey, 60);
            DataModel.SystemSetting.Other[7] = (_lowFps == 30);

            RegisterPropertyChanged();

            var _tbConfig = Table.GetClientConfig(1007);
            var _lowfps = _tbConfig.Value.ToInt();
            if (_lowfps > 1)
            {
                lowFrameTime = 1f / _lowfps;
            }
        }
        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "CanPiackUpItem")
            {
                return CanPiackUpItem((int)param[0]);
            }
            if (name.Equals("GetQualityChanged"))
            {
                return QualityChanged;
            }
            if (name.Equals("SetQualityChanged"))
            {
                QualityChanged = null;
            }
            if (name.Equals("GetEyeIsOpen"))
            {
                return (DataModel.SystemSetting.VisibleEye == 1);
            }
            if (name == "CanAutoRecyleItem")
            {
                return CanAutoRecyleItem((int)param[0]);
            }
            return null;
        }

        public void OnShow()
        {
            //清除主界面的装备更换提示
            EventDispatcher.Instance.DispatchEvent(new UIEvent_HintCloseEvent());
        }
        private float timeinterval = 0;
        private float lastTime = 0;
        private int lastFrameCount = 0;
        private float lowFrameTime = 0.1f;
        private bool visableEyeStart = false;
        private Coroutine setvisibleEyeCoroutine = null;

        private void SetVisibleStart(IEvent ievent)
        {
            var e = ievent as UIEvent_VisibleEyeCanBeStart;
            if (e == null) return;

            if (setvisibleEyeCoroutine != null)
            {
                ResourceManager.Instance.StopCoroutine(setvisibleEyeCoroutine);
                setvisibleEyeCoroutine = null;
            }

            setvisibleEyeCoroutine = ResourceManager.Instance.StartCoroutine(DelaySetVisibleEye(e.Start));
        }

        IEnumerator DelaySetVisibleEye(bool bStart)
        {
            yield return new WaitForSeconds(10);
            visableEyeStart = bStart;
            setvisibleEyeCoroutine = null;
        }

        public void Tick()
        {
            if (!visableEyeStart)
            {
                return;
            }

            timeinterval += Time.deltaTime;

            if (timeinterval < 5)
            {
                return;
            }
            timeinterval -= 5;

            if (lastFrameCount == 0)
            {
                lastFrameCount = Time.frameCount;
                lastTime = Time.fixedTime;
                return;
            }

            var _frameTime = (Time.fixedTime - lastTime) / (Time.frameCount - lastFrameCount);

            if (DataModel.SystemSetting.VisibleEye == 1 && _frameTime > lowFrameTime)
            {
                DataModel.SystemSetting.VisibleEyeTipShow = true;
            }
            else
            {
                DataModel.SystemSetting.VisibleEyeTipShow = false;
            }

            lastFrameCount = Time.frameCount;
            lastTime = Time.fixedTime;
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as SettingArguments;
            if (_args != null)
            {
                DataModel.Tab = _args.Tab;
            }
            BackUpModel.AutoCombat.Hp = DataModel.AutoCombat.Hp;
            BackUpModel.AutoCombat.Mp = DataModel.AutoCombat.Mp;
            BackUpModel.AutoCombat.IsResponded = DataModel.AutoCombat.IsResponded;
            for (var i = 0; i < 3; i++)
            {
                BackUpModel.AutoCombat.Ranges[i] = DataModel.AutoCombat.Ranges[i];
            }
            for (var i = 0; i < 8; i++)
            {
                BackUpModel.AutoCombat.Pickups[i] = DataModel.AutoCombat.Pickups[i];
            }

            for (var i = 0; i < DataModel.SystemSetting.Other.Count; i++)
            {
                BackUpModel.SystemSetting.Other[i] = DataModel.SystemSetting.Other[i];
            }

            var _QualityLevel = PlayerPrefs.GetInt(GameSetting.GameQuilatyKey, GameSetting.Instance.GameQualityLevel);
            GameSetting.Instance.GameQualityLevel = _QualityLevel;
            DataModel.SystemSetting.QualityToggle = _QualityLevel;
            // lastGameQuality = _QualityLevel;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "AutoCombat")
            {
                return DataModel.AutoCombat;
            }
            return DataModel;
        }

        public void Close()
        {
            var _dic = new Dictionary<int, int>();
            if (Math.Abs(BackUpModel.AutoCombat.Hp - DataModel.AutoCombat.Hp) > 0.01)
            {
                _dic.Add(59, (int)(DataModel.AutoCombat.Hp * 100));
            }
            if (Math.Abs(BackUpModel.AutoCombat.Mp - DataModel.AutoCombat.Mp) > 0.01)
            {
                _dic.Add(60, (int)(DataModel.AutoCombat.Mp * 100));
            }

            BackUpModel.AutoCombat.Mp = DataModel.AutoCombat.Mp;


            var _chgPick = false;
            var _flag61 = 0;
            for (var i = 0; i < 8; i++)
            {
                if (BackUpModel.AutoCombat.Pickups[i] != DataModel.AutoCombat.Pickups[i])
                {
                    _chgPick = true;
                    break;
                }
            }

            _flag61 = 0;

            for (var i = 0; i < 8; i++)
            {
                if (DataModel.AutoCombat.Pickups[i])
                {
                    _flag61 = BitFlag.IntSetFlag(_flag61, i);
                }
            }


            var _chgRang = false;
            for (var i = 0; i < 3; i++)
            {
                if (BackUpModel.AutoCombat.Ranges[i] != DataModel.AutoCombat.Ranges[i])
                {
                    _chgRang = true;
                    break;
                }
            }

            for (var i = 0; i < 3; i++)
            {
                if (DataModel.AutoCombat.Ranges[i])
                {
                    if (i == 1)
                    {
                        _flag61 = BitFlag.IntSetFlag(_flag61, 8);
                    }
                    else if (i == 2)
                    {
                        _flag61 = BitFlag.IntSetFlag(_flag61, 9);
                    }
                }
            }

            if (_chgPick || _chgRang)
            {
                _dic.Add(61, _flag61);
            }

            var _chgFanJi = false;
            var _flag628 = 0;

            if (BackUpModel.AutoCombat.IsResponded != DataModel.AutoCombat.IsResponded)
            {
                _chgFanJi = true;
            }

            if (DataModel.AutoCombat.IsResponded)
            {
                _flag628 = BitFlag.IntSetFlag(_flag628, 0);
            }
            if (_chgFanJi)
            {
                _dic.Add(628, _flag628);
            }

            if (_dic.Count > 0)
            {
                PlayerDataManager.Instance.SetExDataNet(_dic);
            }

            var _tureArray = new Int32Array();
            var _falseArray = new Int32Array();

            for (var i = 0; i < 4; i++)
            {
                if (DataModel.SystemSetting.Other[i] != BackUpModel.SystemSetting.Other[i])
                {
                    if (DataModel.SystemSetting.Other[i])
                    {
                        _tureArray.Items.Add(480 + i);
                    }
                    else
                    {
                        _falseArray.Items.Add(480 + i);
                    }
                }
            }


            //后添加的两个设置
            //又增加一个屏幕节能开关
            if (DataModel.SystemSetting.Other[4] != BackUpModel.SystemSetting.Other[4])
            {
                if (DataModel.SystemSetting.Other[4])
                {
                    _tureArray.Items.Add(488);
                }
                else
                {
                    _falseArray.Items.Add(488);
                }
            }

            if (DataModel.SystemSetting.Other[5] != BackUpModel.SystemSetting.Other[5])
            {
                if (DataModel.SystemSetting.Other[5])
                {
                    _tureArray.Items.Add(489);
                }
                else
                {
                    _falseArray.Items.Add(489);
                }
            }

            if (DataModel.SystemSetting.Other[6] != BackUpModel.SystemSetting.Other[6])
            {
                if (DataModel.SystemSetting.Other[6])
                {
                    _tureArray.Items.Add(490);
                }
                else
                {
                    _falseArray.Items.Add(490);
                }
            }


            if (_tureArray.Items.Count > 0 || _falseArray.Items.Count > 0)
            {
                PlayerDataManager.Instance.SetFlagNet(_tureArray, _falseArray);
            }
        }

        public FrameState State { get; set; }
        #endregion

        #region 推送相关

        /// <summary>
        ///     0 世界boss
        ///     1 古堡争霸
        ///     2 邪恶监牢
        ///     3 诅咒堡垒
        ///     4 黄金部队刷新
        ///     5 地图统领刷新
        ///     6 头脑风暴
        ///     7 免费精灵抽奖
        ///     8 免费许愿池抽奖
        ///     9 孵化室完成
        ///     10 矿洞满
        ///     11 伐木场满
        ///     12 预留----------
        ///     13
        ///     14
        ///     15
        ///     16
        ///     17
        /// </summary>
        private void InitializePush()
        {
            var _count = DataModel.PushList.Count;
            for (var i = 0; i < _count; i++)
            {
                var _key = string.Format("PushKey{0}", i);
                var _defaultValue = 0;
                if (i == 0 || i == 1 || i > 6)
                {
                    _defaultValue = 1;
                }
                else
                {
                    _defaultValue = 0;
                }
                DataModel.PushList[i] = PlayerPrefs.GetInt(_key, _defaultValue) == 1;
            }

            //  上边PushList的PropertyChanged会自动注册推送,所以不用再次注册
            //  RefurbishAllPush();
        }

        private void OnRefurbishPushEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_RefreshPush;
            RefurbishPushById(_e.id);
        }

        private void RefurbishPushById(int id)
        {
            switch (id)
            {
                case -1:
                    InitializePush();
                    break;
                case 0:
                    WorldBossPush();
                    break;
                case 1:
                    CityWarPush();
                    break;
                case 2:
                    UglyDungeonPush();
                    break;
                case 3:
                    DratCastlePush();
                    break;
                case 4:
                    GoldenArmyPush();
                    break;
                case 5:
                    MapGodPush();
                    break;
                case 6:
                    BrainStromPush();
                    break;
                case 7:
                    ElfDrawPush();
                    break;
                case 8:
                    WishingWellPush();
                    break;
                case 9:
                    BrooderHousePush();
                    break;
                case 10:
                    MinePush();
                    break;
                case 11:
                    LogPlacePush();
                    break;
            }
        }


        private void RefurbishAllPush()
        {
            WorldBossPush();
            CityWarPush();
            UglyDungeonPush();
            DratCastlePush();
            GoldenArmyPush();
            MapGodPush();
            BrainStromPush();
            ElfDrawPush();
            WishingWellPush();
            BrooderHousePush();
            MinePush();
            LogPlacePush();
        }

        private void WorldBossPush()
        {
            for (var i = 0; i < 7; i++)
            {
                var _key = string.Format("worldboss{0}", i);
                PlatformHelper.DeleteLocalNotificationWithKey(_key);
            }

            if (!CheckCondition(0))
            {
                return;
            }
            var _now = Game.Instance.ServerTime;
            var _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 12, 05, 0);
            for (var i = 0; i < 7; i++)
            {
                var _key = string.Format("worldboss{0}", i);
                var _target = _targetTime.AddDays(i);
                if (_target < _now)
                {
                    continue;
                }
                var _diff = _target - _now;
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240136), _diff.TotalSeconds);
            }
        }

        // 古堡争霸
        private void CityWarPush()
        {
            //没有了
            return;
            for (var i = 0; i < 7; i++)
            {
                var _key = string.Format("CityBattle{0}", i);
                PlatformHelper.DeleteLocalNotificationWithKey(_key);
            }

            if (!CheckCondition(1))
            {
                return;
            }
            var _now = Game.Instance.ServerTime;
            var _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 21, 0, 0);
            for (var i = 0; i < 7; i++)
            {
                var _key = string.Format("CityBattle{0}", i);
                var _target = _targetTime.AddDays(i);
                if (_target < _now)
                {
                    continue;
                }
                var _diff = _target - _now;
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240149), _diff.TotalSeconds);
            }
        }

        private void UglyDungeonPush()
        {
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 1; j++)
                {
                    var _key = string.Format("EvilDungeon{0}H{1}", i, j);
                    PlatformHelper.DeleteLocalNotificationWithKey(_key);
                }
            }

            if (!CheckCondition(2))
            {
                return;
            }

            var _controller = UIManager.Instance.GetController(UIConfig.ActivityUI);
            var _IsMaxCount = _controller.CallFromOtherClass("IsDevilSquareMaxCount", null);
            var _now = Game.Instance.ServerTime;
            var _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 10, 15, 0);
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 1; j++)
                {
                    var _key = string.Format("EvilDungeon{0}H{1}", i, j);
                    var _diff = _targetTime - _now;
                    _targetTime = _targetTime.AddHours(1);
                    if (i == 0 && (bool)_IsMaxCount)
                    {
                        continue;
                    }
                    PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240150), _diff.TotalSeconds);
                }
                _targetTime = new DateTime(_now.Year, _now.Month, _now.AddDays(1).Day, 10, 15, 0);
            }
        }

        private void DratCastlePush()
        {
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 1; j++)
                {
                    var _key = string.Format("CurseCastleD{0}H{1}", i, j);
                    PlatformHelper.DeleteLocalNotificationWithKey(_key);
                }
            }

            if (!CheckCondition(3))
            {
                return;
            }

            var _controller = UIManager.Instance.GetController(UIConfig.ActivityUI);
            var _IsMaxCount = _controller.CallFromOtherClass("IsBloodCastleMaxCount", null);
            var _now = Game.Instance.ServerTime;
            var _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 10, 35, 0);
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 1; j++)
                {
                    var _key = string.Format("CurseCastleD{0}H{1}", i, j);
                    var _diff = _targetTime - _now;
                    _targetTime = _targetTime.AddHours(1);
                    if (i == 0 && (bool)_IsMaxCount)
                    {
                        continue;
                    }
                    PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240151), _diff.TotalSeconds);
                }
                _targetTime = new DateTime(_now.Year, _now.Month, _now.AddDays(1).Day, 10, 35, 0);
            }
        }

        private void GoldenArmyPush()
        {
            for (var i = 0; i < 3; i++)
            {
                var _key1 = string.Format("GoldenArmyAM{0}", i);
                var _key2 = string.Format("GoldenArmyPM{0}", i);
                PlatformHelper.DeleteLocalNotificationWithKey(_key1);
                PlatformHelper.DeleteLocalNotificationWithKey(_key2);
            }

            if (!CheckCondition(4))
            {
                return;
            }

            var _now = Game.Instance.ServerTime;
            var _targetTime1 = new DateTime(_now.Year, _now.Month, _now.Day, 11, 57, 0);
            var _targetTime2 = new DateTime(_now.Year, _now.Month, _now.Day, 19, 57, 0);
            for (var i = 0; i < 3; i++)
            {
                var _key1 = string.Format("GoldenArmyAM{0}", i);
                var _key2 = string.Format("GoldenArmyPM{0}", i);
                var _diff1 = _targetTime1 - _now;
                var _diff2 = _targetTime2 - _now;
                _targetTime1 = _targetTime1.AddDays(1);
                _targetTime2 = _targetTime2.AddDays(1);

                PlatformHelper.SetLocalNotification(_key1, GameUtils.GetDictionaryText(240152), _diff1.TotalSeconds);
                PlatformHelper.SetLocalNotification(_key2, GameUtils.GetDictionaryText(240152), _diff2.TotalSeconds);
            }
        }

        private void MapGodPush()
        {
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 5; j++)
                {
                    var _key = string.Format("MapLordD{0}H{1}", i, j);
                    PlatformHelper.DeleteLocalNotificationWithKey(_key);
                }
            }

            if (!CheckCondition(5))
            {
                return;
            }

            var _now = Game.Instance.ServerTime;
            for (var i = 0; i < 3; i++)
            {
                var _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 10, 57, 0);
                var _key = string.Format("MapLordD{0}H{1}", i, 0);
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240153),
                    (_targetTime - Game.Instance.ServerTime).TotalSeconds);

                _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 13, 57, 0);
                _key = string.Format("MapLordD{0}H{1}", i, 1);
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240153),
                    (_targetTime - Game.Instance.ServerTime).TotalSeconds);

                _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 16, 57, 0);
                _key = string.Format("MapLordD{0}H{1}", i, 2);
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240153),
                    (_targetTime - Game.Instance.ServerTime).TotalSeconds);

                _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 18, 57, 0);
                _key = string.Format("MapLordD{0}H{1}", i, 3);
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240153),
                    (_targetTime - Game.Instance.ServerTime).TotalSeconds);

                _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 21, 57, 0);
                _key = string.Format("MapLordD{0}H{1}", i, 4);
                PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240153),
                    (_targetTime - Game.Instance.ServerTime).TotalSeconds);


                _now = _now.AddDays(1);
            }
        }

        private void BrainStromPush()
        {
            for (var i = 0; i < 7; i++)
            {
                var _key = string.Format("BrainStrom{0}", i);
                PlatformHelper.DeleteLocalNotificationWithKey(_key);
            }

            if (!CheckCondition(6))
            {
                return;
            }
            var _controller = UIManager.Instance.GetController(UIConfig.AnswerUI);
            var _ret = _controller.CallFromOtherClass("IsMaxAnser", null);

            var _now = Game.Instance.ServerTime;

            for (var i = 0; i < 7; i++)
            {
                var _key = string.Format("BrainStrom{0}", i);
                var _targetTime = new DateTime(_now.Year, _now.Month, _now.Day, 19, 30, 0);
                if (i == 0)
                {
                    if (!(bool)_ret)
                    {
                        PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240154),
                            (_targetTime - Game.Instance.ServerTime).TotalSeconds);
                    }
                }
                else
                {
                    PlatformHelper.SetLocalNotification(_key, GameUtils.GetDictionaryText(240154),
                        (_targetTime - Game.Instance.ServerTime).TotalSeconds);
                }

                _now = _now.AddDays(1);
            }
        }


        private void ElfDrawPush()
        {
            const string key = "ElfDraw";
            PlatformHelper.DeleteLocalNotificationWithKey(key);

            if (!CheckCondition(7))
            {
                return;
            }
            var _controller = UIManager.Instance.GetController(UIConfig.ElfUI);
            var _ret = _controller.CallFromOtherClass("GetIsFreeDraw", null);
            if ((int?)_ret == 0)
            {
                var data = _controller.CallFromOtherClass("FreeDrawTime", null);
                if (data is DateTime)
                {
                    PlatformHelper.SetLocalNotification(key, GameUtils.GetDictionaryText(240155),
                            ((DateTime)data - Game.Instance.ServerTime).TotalSeconds);
                }

            }
        }

        private void WishingWellPush()
        {
            const string key = "WishingPool";
            PlatformHelper.DeleteLocalNotificationWithKey(key);

            if (!CheckCondition(8))
            {
                return;
            }

            var _controller = UIManager.Instance.GetController(UIConfig.WishingUI);
            var _ret = _controller.CallFromOtherClass("GetNextFreeTime", null);

            if (_ret != null)
            {
                var _dateData = (long)_ret;
                var _now = Game.Instance.ServerTime;
                var _targetTime = Extension.FromServerBinary(_dateData);

                if (_targetTime > _now)
                {
                    PlatformHelper.SetLocalNotification(key, GameUtils.GetDictionaryText(240156),
                        (_targetTime - _now).TotalSeconds);
                }
            }
        }

        private void BrooderHousePush()
        {
            //         for (var i = 0; i < 5; i++)
            //         {
            //             var key = string.Format("HatchingHouse{0}", i);
            //             PlatformHelper.DeleteLocalNotificationWithKey(key);
            //         }
            // 
            //         if (!CheckCondition(9))
            //         {
            //             return;
            //         }
            // 
            //         var controller = UIManager.Instance.GetController(UIConfig.HatchingHouse);
            //         var ret = controller.CallFromOtherClass("GetLastTimeList", null);
            //         var list = ret as List<long>;
            //         if (list == null)
            //         {
            //             return;
            //         }
            // 
            //         var c = Math.Min(5, list.Count);
            //         var now = Game.Instance.ServerTime;
            //         for (var i = 0; i < c; i++)
            //         {
            //             var key = string.Format("HatchingHouse{0}", i);
            //             var targetTime = Extension.FromServerBinary(list[i]);
            //             PlatformHelper.SetLocalNotification(key, GameUtils.GetDictionaryText(240157),
            //                 (targetTime - now).TotalSeconds);
            //         }
        }


        private void MinePush()
        {
            //         const string key = "MinePush";
            //         PlatformHelper.DeleteLocalNotificationWithKey(key);
            // 
            //         if (!CheckCondition(10))
            //         {
            //             return;
            //         }
            //         var controller = UIManager.Instance.GetController(UIConfig.CityUI);
            //         var ret = controller.CallFromOtherClass("GetMineMaxTime", null);
            //         if (ret != null)
            //         {
            //             var now = Game.Instance.ServerTime;
            //             var targetTime = ret as DateTime? ?? new DateTime();
            //             PlatformHelper.SetLocalNotification(key, GameUtils.GetDictionaryText(240158),
            //                 (targetTime - now).TotalSeconds);
            //         }
        }

        private void LogPlacePush()
        {
            //         const string key = "LogPlacePush";
            //         PlatformHelper.DeleteLocalNotificationWithKey(key);
            // 
            //         if (!CheckCondition(11))
            //         {
            //             return;
            //         }
            // 
            //         var controller = UIManager.Instance.GetController(UIConfig.CityUI);
            //         var ret = controller.CallFromOtherClass("GetWoodMaxTime", null);
            //         if (ret != null)
            //         {
            //             var now = Game.Instance.ServerTime;
            //             var targetTime = ret as DateTime? ?? new DateTime();
            //             PlatformHelper.SetLocalNotification(key, GameUtils.GetDictionaryText(240159),
            //                 (targetTime - now).TotalSeconds);
            //         }
        }

        private bool CheckCondition(int id)
        {
            var _conditionId = -1;
            DailyActivityRecord table;
            RewardInfoRecord table2;
            switch (id)
            {
                case 0:
                    table = Table.GetDailyActivity(1002);
                    _conditionId = table.OpenCondition;
                    break;
                case 1:
                    //table = Table.GetDailyActivity(1006);
                    //_conditionId = table.OpenCondition;
                    break;
                case 2:
                    table = Table.GetDailyActivity(1000);
                    _conditionId = table.OpenCondition;
                    break;
                case 3:
                    table = Table.GetDailyActivity(1001);
                    _conditionId = table.OpenCondition;
                    break;
                case 4:
                    table = Table.GetDailyActivity(1003);
                    _conditionId = table.OpenCondition;
                    break;
                case 5:
                    table = Table.GetDailyActivity(1004);
                    _conditionId = table.OpenCondition;
                    break;
                case 6:
                    table = Table.GetDailyActivity(1005);
                    _conditionId = table.OpenCondition;
                    break;
                case 7:
                    table2 = Table.GetRewardInfo(1);
                    _conditionId = table2.ConditionId;
                    break;
                case 8:
                    table2 = Table.GetRewardInfo(0);
                    _conditionId = table2.ConditionId;
                    break;
                case 9:
                    //                 table2 = Table.GetRewardInfo(9);
                    //                 conditionId = table2.ConditionId;
                    break;
                case 10:
                    //                 table2 = Table.GetRewardInfo(12);
                    //                 conditionId = table2.ConditionId;
                    break;
                case 11:
                    //                 table2 = Table.GetRewardInfo(13);
                    //                 conditionId = table2.ConditionId;
                    break;
            }

            if (_conditionId == -1)
            {
                return false;
            }

            var _result = DataModel.PushList[id];
            return _result;
        }

        #endregion

        private IEnumerator ModifyPlayerName(string changeName)
        {
            using (var blockingLayer = new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ModifyPlayerName(changeName);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == ScorpionNetLib.MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.PlayerDataModel.CharacterBase.Name = _msg.Response;

                        //OnCloseModifyPlayer();
                        EventDispatcher.Instance.DispatchEvent(new SettingUIModifyPlayerNameEvent(0));
                        var objMyPlayer = ObjManager.Instance.MyPlayer;
                        if (objMyPlayer != null)
                        {
                            objMyPlayer.OnNameBoardRefresh();
                        }
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.Error_NAME_IN_USE)
                        {
                            var _dicId = _msg.ErrorCode + 200000000;
                            var _tbDic = Table.GetDictionary(_dicId);
                            var _info = "";
                            _info = _tbDic.Desc[GameUtils.LanguageIndex];
                            UIManager.Instance.ShowMessage(MessageBoxType.Ok, _info, "");
                            //EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(210104)));
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                            Logger.Error(".....UpgradeHonor...ErrorCode....{0}.", _msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    Logger.Debug("CreateCharacter.................." + _msg.State);
                }
            }
        }
    }
}