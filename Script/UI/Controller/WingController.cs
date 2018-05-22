/********************************************************************************* 

                         Scorpion



  *FileName:WingFrameCtrler

  *Version:1.0

  *Date:2017-07-13

  *Description:

**********************************************************************************/  
#region using

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class WingFrameCtrler : IControllerBase
    {
        private int chosenWingId = 0;

        #region 静态变量
    
        #endregion

        #region 成员变量

        private WingDataModel DataModel;
        private Coroutine m_AdvanceCoroutine;
        private bool m_bIsAutoAdvance;
        private Coroutine m_TrainCoroutine;

        private IControllerBase m_WingChargeController;

        private int[] m_arraryWingPartIco =
        {
            Table.GetClientConfig(290).Value.ToInt(), // "翅翼",
            Table.GetClientConfig(291).Value.ToInt(), // "翅鞘",
            Table.GetClientConfig(292).Value.ToInt(), // "翅羽",
            Table.GetClientConfig(293).Value.ToInt(), // "翅骨",
            Table.GetClientConfig(294).Value.ToInt() // "翅翎"
        };

        private string[] m_arraryWingPartName =
        {
            GameUtils.GetDictionaryText(270124), // "翅翼",
            GameUtils.GetDictionaryText(270125), // "翅鞘",
            GameUtils.GetDictionaryText(270126), // "翅羽",
            GameUtils.GetDictionaryText(270127), // "翅骨",
            GameUtils.GetDictionaryText(270128) // "翅翎"
        };

        private List<bool> m_listShowBegin = new List<bool> { true, true, true, true, true };

        #endregion

        #region 构造函数

        public WingFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(WingOperateEvent.EVENT_TYPE, OnWingOperatedEvent);
            EventDispatcher.Instance.AddEventListener(WingQuailtyCellClick.EVENT_TYPE, OnWingQuailtyCellTipEvent);
            EventDispatcher.Instance.AddEventListener(Event_LevelUp.EVENT_TYPE, OnLvUpEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagPropNumChangeEvent);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitionEvent);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            DataModel = new WingDataModel();
            m_TrainCoroutine = null;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "UpdateWingItem")
            {
                RenewalUpgradeWingProp(param[0] as ItemsChangeData);
            }
            else if (name == "InitWingItem")
            {
                InitionWingProp(param[0] as BagBaseData);
            }
            else if (name == "AmendPropertiesValue")
            {
                return AmendPropertiesValue((int)param[0], (int)param[1]);
            }

            return null;
        }

        public void OnShow()
        {
            RefreshWingModel(DataModel.QualityId);
            if (DataModel != null && DataModel.ShowTab >= 0)
            {
                OnTipWingPortion(0);
            }
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
           

            var _arg = data as WingArguments;
            if (_arg != null && _arg.Tab != -1)
            {
                DataModel.ShowTab = _arg.Tab;
                //OnTipWingPortion(0);
            }
            else
            {
                DataModel.ShowTab = 0;
            }

            InitionWingQuailty();
            ExamineGreyWingPart();

        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }

        #endregion

        #region 逻辑函数


        //----------------------------------------------------------Train
        //检查是否可进行培养
        private bool ExamineTrainWingPart()
        {
            var _tbWingTrain = Table.GetWingTrain(DataModel.PartData.TrainId);
            if (_tbWingTrain.UsedMoney > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.GoldRes);
                return false;
            }
            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);
            if (_tbWingTrain.Condition > _tbWingQuality.Segment)
            {
                var _str = GameUtils.GetDictionaryText(220304);
                _str = string.Format(_str, _tbWingTrain.Condition);
                var _e = new ShowUIHintBoard(_str);
                EventDispatcher.Instance.DispatchEvent(_e);
                return false;
            }
            if (_tbWingTrain.UpStarID == -1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220305));
                return false;
            }
            //if (tbWingTrain.MaterialCount > PlayerDataManager.Instance.GetItemCount(tbWingTrain.MaterialID))
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
            //    PlayerDataManager.Instance.ShowItemInfoGet(tbWingTrain.MaterialID);
            //    return false;
            //}
            if (!GameUtils.CheckEnoughItems(_tbWingTrain.MaterialID, _tbWingTrain.MaterialCount))
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                return false;
            }
            return true;
        }

        private WingChargeDataModel AcquireWingChargedDataModle()
        {
            if (m_WingChargeController != null)
            {
                var _tempModel = m_WingChargeController.GetDataModel("");
                if (_tempModel != null)
                {
                    var _wingCahrgeModel = _tempModel as WingChargeDataModel;
                    if (_wingCahrgeModel != null)
                    {
                        return _wingCahrgeModel;
                    }
                }
            }

            return null;
        }

        //----------------------------------------------------------Advanced
        //检查是否可进行进阶(分为成长跟突破)
        private bool ExamineWingAdvancement(bool isShowTip = true)
        {
            if (DataModel.ItemData == null || DataModel.ItemData.WingQuailty == -1)
            {
                return false;
            }
            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);
            if (_tbWingQuality == null)
                return false;

            var _needGold = 0;
            var _needItemId = -1;
            var _needItemCount = 0;
            if (DataModel.IsAdvanceFull) // 突破
            {
                _needItemId = _tbWingQuality.BreakNeedItem;
                _needItemCount = _tbWingQuality.BreakNeedCount;
                _needGold = _tbWingQuality.BreakNeedMoney;
            }
            else
            {
                _needItemId = _tbWingQuality.MaterialNeed;
                _needItemCount = _tbWingQuality.MaterialCount;
                _needGold = _tbWingQuality.UsedMoney;
            }

            if (_needGold > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold)
            {
                if (isShowTip)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210100));
                    PlayerDataManager.Instance.ShowItemInfoGet((int)eResourcesType.GoldRes);
                }
                return false;
            }
            if (_tbWingQuality.LevelLimit > PlayerDataManager.Instance.GetLevel())
            {
                if (isShowTip)
                {
                    var _str = GameUtils.GetDictionaryText(220307);
                    _str = string.Format(_str, _tbWingQuality.LevelLimit);
                    var _e = new ShowUIHintBoard(_str);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                return false;
            }
            if (_tbWingQuality.Segment >= GameUtils.WingQualityMax)
            {
                if (isShowTip)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220308));
                }
                return false;
            }

            if (_needItemCount > PlayerDataManager.Instance.GetItemCount(_needItemId))
            {
                if (isShowTip)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                    var _items = new Dictionary<int, int>();
                    _items[_needItemId] = _needItemCount;

                    var _tbGift = Table.GetGift(4000);
                    if (_tbGift != null)
                    {
                        if (m_WingChargeController == null)
                        {
                            m_WingChargeController = UIManager.Instance.GetController(UIConfig.WingChargeFrame);
                        }

                        if (AcquireWingChargedDataModle() != null && AcquireWingChargedDataModle().IsShowWingCharge == 1) // 没领取过翅膀商城
                        {
                            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.WingChargeFrame));
                        }
                        else
                        {
                            GameUtils.CheckEnoughItems(_items, true);
                        }
                    }
                    else
                    {
                        GameUtils.CheckEnoughItems(_items, true);
                    }
                }
                return false;
            }

            return true;
        }

        //检查所有阶翅膀图标是否为灰色
        private void ExamineGreyWingPart()
        {
            DataModel.TrainNotice = false;
            ExamineGreyWingPartIndex(0);
            //for (var i = 0; i < 5; i++)
            //{
            //    ExamineGreyWingPartIndex(i);
            //}
        }

        //检查某一阶阶翅膀图标是否为灰色
        private void ExamineGreyWingPartIndex(int index)
        {
            if (index < 0 || index >= 5)
            {
                return;
            }
            var _trainId = DataModel.ItemData.ExtraData[index * 2 + 1];
            var _tbTrain = Table.GetWingTrain(_trainId);

            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);

            if (_tbTrain.TrainCount == 1
                && _tbTrain.TrainStar == 1
                && _tbTrain.Condition > _tbWingQuality.Segment)
            {
                DataModel.IsIcoGrey[index] = true;
                DataModel.TrainNotice = true;
            }
            else
            {
                DataModel.IsIcoGrey[index] = false;
            }
        }

        //人物属性修正
        private static int AmendPropertiesValue(int attrId, int attrValue)
        {
            if (attrId == 21 || attrId == 22)
            {
                return attrValue * 100;
            }
            return attrValue;
        }

        //初始化升级所需物品数据
        private void InitionWingProp(BagBaseData bagBase)
        {
            if (bagBase.Items.Count == 0)
            {
                DataModel.ItemData.ItemId = -1;
                return;
            }
            var _itemInfo = bagBase.Items[0];
            DataModel.ItemData.Count = _itemInfo.Count;
            DataModel.ItemData.Index = _itemInfo.Index;
            DataModel.ItemData.ExtraData.InstallData(_itemInfo.Exdata);
            SettingWingPropId(_itemInfo.ItemId);
            RenewalCompleteAttribute();
            ExamineGreyWingPart();
        }

        //初始化翅膀升阶信息
        private void InitionWingQuailty()
        {
            var _roleType = PlayerDataManager.Instance.GetRoleId();
            if (DataModel.QualityDatas.Count == 0)
            {
                Table.ForeachWingQuality(recoard =>
                {
                    if (recoard.Career != _roleType)
                    {
                        return true;
                    }
                    if (recoard.Segment > GameUtils.WingQualityMax)
                    {
                        return true;
                    }
                    var _data = new WingQualityData();
                    _data.WingId = recoard.Id;
                    _data.IsGrey = recoard.Id > DataModel.ItemData.WingQuailty - 1;
                    DataModel.QualityDatas.Add(_data);

                    return true;
                });
            }
            RenewalWingAdvancedInfo();
            RenewalWingQualitiesAttribute(DataModel.ItemData.ItemId);
            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);
            if (_tbWingQuality == null)
            {
                Logger.Error("InitWingQuailtys: tbWingQuality ==null");
            }

            var _curGrowValue = DataModel.ItemData.ExtraData.Benison;
            var _maxGrowValue = 0;
            if (_tbWingQuality != null)
            {
                _maxGrowValue = _tbWingQuality.GrowProgress;
            }
            DataModel.IsAdvanceFull = (_curGrowValue >= _maxGrowValue);

            DataModel.AdvanceSlider.MaxValues = new List<int> { _maxGrowValue };
            if (_maxGrowValue != 0)
            {
                DataModel.AdvanceSlider.BeginValue = _curGrowValue / (float)_maxGrowValue;
                DataModel.AdvanceSlider.TargetValue = DataModel.AdvanceSlider.BeginValue;
            }
            else
            {
                DataModel.AdvanceSlider.BeginValue = 0.0f;
                DataModel.AdvanceSlider.TargetValue = DataModel.AdvanceSlider.BeginValue;
            }
        }



        //点击自动培养
        private void OnClickwingPartAutoTrain()
        {
            if (DataModel.IsAutoTrain)
            {
                OnTrainWingportionAuto();
            }
        }

        //点击翅膀部位培养按钮
        private void OnClickWingPartAutoTrain()
        {
            var _tbVip = PlayerDataManager.Instance.TbVip;
            if ((_tbVip.WingAdvanced == 0))
            {
                do
                {
                    _tbVip = Table.GetVIP(_tbVip.Id + 1);
                } while (_tbVip.WingAdvanced == 0);

                GameUtils.GuideToBuyVip(_tbVip.Id);
                return;
            }

            if (DataModel.IsAutoTrain)
            {
                DataModel.IsAutoTrain = false;
                if (m_TrainCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                    m_TrainCoroutine = null;
                }
               
                return;
            }
            DataModel.IsAutoTrain = true;
            OnTrainWingportionAuto();
        }

        //点击翅膀部位培养进阶
        private void OnTipWingAdvanceAuto()
        {
            var _tbVip = PlayerDataManager.Instance.TbVip;
            if ((_tbVip.WingAdvanced == 0))
            {
                do
                {
                    _tbVip = Table.GetVIP(_tbVip.Id + 1);
                } while (_tbVip.WingAdvanced == 0);

                GameUtils.GuideToBuyVip(_tbVip.Id);
                return;
            }

            if (DataModel.IsAutoAdvance)
            {
                DataModel.IsAutoAdvance = false;
                if (m_AdvanceCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_AdvanceCoroutine);
                    m_AdvanceCoroutine = null;
                }
            }
            else
            {
                DataModel.IsAutoAdvance = true;
            }
            WingAdvancementSelfmotion();
        }

        //点击进入部位进行培养
        private void OnTipWingPortion(int index)
        {
            int _index = 0;
            DataModel.IsAutoTrain = false;
            //PlayerDataManager.Instance.WeakNoticeData.WingTraining = false;
            //DataModel.CanUpGrade[index] = false;
            var _trainId = DataModel.ItemData.ExtraData[_index * 2 + 1];
            var _tbTrain = Table.GetWingTrain(_trainId);

            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);

            if (_tbTrain.TrainCount == 1
                && _tbTrain.TrainStar == 1
                && _tbTrain.Condition > _tbWingQuality.Segment)
            {
                //需要{0}阶翅膀
                var _str = string.Format(GameUtils.GetDictionaryText(270132), _tbTrain.Condition);
                var _e1 = new ShowUIHintBoard(_str);
                EventDispatcher.Instance.DispatchEvent(_e1);
                return;
            }
            var _partData = new WingPartData();

            _partData.IcoId = m_arraryWingPartIco[_index];
            _partData.Name = m_arraryWingPartName[_index];
            _partData.Layer = _tbTrain.TrainCount;
            _partData.Star = _tbTrain.TrainStar;
            _partData.ItemData = DataModel.ItemData;
            _partData.PartIndex = _index;
            _partData.TrainId = _trainId;
            _partData.Exp = DataModel.ItemData.ExtraData[_index * 2 + 2];

            _partData.TrainSlider.MaxValues = new List<int> { _tbTrain.ExpLimit };
            _partData.TrainSlider.BeginValue = _partData.Exp / (float)_tbTrain.ExpLimit;
            _partData.TrainSlider.TargetValue = _partData.TrainSlider.BeginValue;

            DataModel.PartData = _partData;
            DataModel.PartSelectedIndex = _index;

            DataModel.PartData.PropertyChanged += PartData_PropertyChanged;

            OnRenewalWingPortion();
        }

        private void PartData_PropertyChanged(object s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TrainId")
            {
                var _er = new WingNotifyLogicEvent(4, DataModel.PartData.TrainId);
                EventDispatcher.Instance.DispatchEvent(_er); 
            }

        }

        //刷新翅膀部位显示信息
        private void OnRenewalWingPortion()
        {
            var _partData = DataModel.PartData;
            var _tbTrain = Table.GetWingTrain(_partData.TrainId);
            var _index = _partData.PartIndex;

            if (_tbTrain.UpStarID == -1)
            {
                _partData.IsMaxTrain = true;
                PlayerDataManager.Instance.WeakNoticeData.WingTraining = false;
            }
            else
            {
                _partData.IsMaxTrain = false;
            }

            if (m_listShowBegin[_index])
            {
                if (_partData.Exp != 0 || _tbTrain.TrainStar != 1)
                {
                    m_listShowBegin[_index] = false;
                }
            }
            var _star = _tbTrain.TrainStar - 1;
            if (_tbTrain.UpStarID == -1)
            {
                _star = _tbTrain.TrainStar;
            }
            var _e = new WingRefreshStarPage(_tbTrain.PosX, _star, _partData.PartIndex, m_listShowBegin[_index]);

            if (m_listShowBegin[_index])
            {
                m_listShowBegin[_index] = false;
            }

            EventDispatcher.Instance.DispatchEvent(_e);
            RenewalWingPortionAttribute();

            var _er = new WingNotifyLogicEvent(4, DataModel.PartData.TrainId);
            EventDispatcher.Instance.DispatchEvent(_er); 

        }

        //显示部位信息
        private void OnShowPartInfo(int type)
        {
            if (type == 0)
            {
                //已到达最前端
                var _e = new ShowUIHintBoard(270129);
                EventDispatcher.Instance.DispatchEvent(_e);
            }
            else
            {
                var _partData = DataModel.PartData;
                var _tbTrain = Table.GetWingTrain(_partData.TrainId);
                if (_tbTrain.TrainCount == 10)
                {
                    //已到达最尾端
                    var _e = new ShowUIHintBoard(270130);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                else
                {
                    //当前培养到最大才能开启下一阶段
                    var _e = new ShowUIHintBoard(270131);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //培养部位
        private void OnTrainWingPortion()
        {
            if (DataModel.IsAutoTrain)
            {
                DataModel.IsAutoTrain = false;
                if (m_TrainCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                    m_TrainCoroutine = null;
                }
            }
            if (m_TrainCoroutine != null)
            {
                //NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                //m_TrainCoroutine = null;
                return;
            }
            if (!ExamineTrainWingPart())
            {
                if (m_TrainCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                    m_TrainCoroutine = null;
                }
                return;
            }
            m_TrainCoroutine = NetManager.Instance.StartCoroutine(WingCultivateCoroutine(0.0f));
        }

        //部位自动培养
        private void OnTrainWingportionAuto(float delay = 0.0f)
        {
            if (!ExamineTrainWingPart())
            {
                DataModel.IsAutoTrain = false;
                if (m_TrainCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                    m_TrainCoroutine = null;
                }
                return;
            }
            if (m_TrainCoroutine != null)
            {
                //NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                //m_TrainCoroutine = null;
                return;
            }
            m_TrainCoroutine = NetManager.Instance.StartCoroutine(WingCultivateCoroutine(delay));
        }

        //刷新翅膀阶数数据
        private void RenewalWingAdvanceData(int ret)
        {
            var _oldSliderValue = DataModel.AdvanceSlider.TargetValue;
            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);
            if (_tbWingQuality == null)
                return;

            if (ret == 1)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(220306));
                DataModel.ItemData.ExtraData.Benison = 0;
                //SetWingItemId(tbWingQuality.Id+1);
                RenewalWingAdvancedInfo();
                ExamineGreyWingPart();
                RenewalCompleteAttribute();
                PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Wing);

                _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.ItemId);
                var _maxList = new List<int>();
                _maxList.Add(_tbWingQuality.GrowProgress);
                DataModel.AdvanceSlider.BeginValue = 0.0f;
                DataModel.AdvanceSlider.MaxValues = _maxList;
                DataModel.AdvanceSlider.TargetValue = DataModel.AdvanceSlider.BeginValue;

                RefreshWingModel(DataModel.ItemData.ItemId);

                GameUtils.ShowModelDisplay(DataModel.ItemData.ItemId, null, 210000);
            }
            else
            {
                var _newGrowth = DataModel.ItemData.ExtraData.Benison;// +tbWingQuality.FailedAddValue;
                if (_newGrowth >= _tbWingQuality.GrowProgress)
                {
                    _newGrowth = _tbWingQuality.GrowProgress;
                    DataModel.IsAdvanceFull = true;
                    DataModel.ItemData.ExtraData.Benison = _newGrowth;
                    SettingWingPropId(_tbWingQuality.Id);
                }
                else
                {
                    DataModel.ItemData.ExtraData.Benison = _newGrowth;
                }

                DataModel.AdvanceSlider.MaxValues = new List<int> { _tbWingQuality.GrowProgress };
                DataModel.AdvanceSlider.BeginValue = _newGrowth / (float)_tbWingQuality.GrowProgress;
                DataModel.AdvanceSlider.TargetValue = DataModel.AdvanceSlider.BeginValue;
            }

            RenewalWingQualitiesAttribute(DataModel.ItemData.ItemId);

            if (DataModel.IsAutoAdvance)
            {
                if (DataModel.IsAdvanceFull)    // 进阶满了，取消自动
                {
                    DataModel.IsAutoAdvance = false;
                }
                else
                {
                    var _dif = DataModel.AdvanceSlider.TargetValue - _oldSliderValue;
                    var _costTime = Mathf.Max(_dif,0.5f);
                    WingAdvancementSelfmotion(_costTime);
                }
            }
        }

        private void RenewalWingAdvanceMsg(int itemId, int changeCount)
        {
            if (DataModel.ItemData == null || DataModel.ItemData.WingQuailty == -1)
            {
                return;
            }

            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);
            if (_tbWingQuality == null)
            {
                return;
            }
            var _needItem = -1;
            var _needItemCount = 0;
            var _needGold = 0;
            if (DataModel.IsAdvanceFull)
            {
                _needItem = _tbWingQuality.BreakNeedItem;
                _needGold = _tbWingQuality.BreakNeedMoney;
                _needItemCount = _tbWingQuality.BreakNeedCount;
            }
            else
            {
                _needItem = _tbWingQuality.MaterialNeed;
                _needGold = _tbWingQuality.UsedMoney;
                _needItemCount = _tbWingQuality.MaterialCount;
            }

            if (_needItem != itemId)
            {
                return;
            }

            if (_needGold > PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Gold)
            {
                return;
            }

            if (_tbWingQuality.LevelLimit > PlayerDataManager.Instance.GetLevel())
            {
                return;
            }
            if (_tbWingQuality.Segment >= GameUtils.WingQualityMax)
            {
                return;
            }

            //         var newvalue = PlayerDataManager.Instance.GetItemCount(needItem);
            //         var oldvalue = newvalue - changeCount;
            //         if (needItemCount > oldvalue && needItemCount <= newvalue)
            //         {
            //             PlayerDataManager.Instance.NoticeData.WingAdvance = true;
            //             PlayerDataManager.Instance.WeakNoticeData.BagTotal = true;
            //             //PlayerDataManager.Instance.WeakNoticeData.WingTotal = true;
            //             PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = true;
            //         }

            if (ExamineWingAdvancement(false))
            {
                PlayerDataManager.Instance.NoticeData.WingAdvance = true;
                PlayerDataManager.Instance.WeakNoticeData.BagTotal = true;
                //PlayerDataManager.Instance.WeakNoticeData.WingTotal = true;
                PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.WingAdvance = false;
            }
        }
        private List<bool> isWingTrain = new List<bool>();
        private void RenewalWingTrainNotice(int changeCount)
        {
            //调整界面后翅膀培养界面弱红点不在适用
            //return;
            var _wingItem = DataModel.ItemData;
            //for (var i = 0; i < 5; i++)
            for (var i = 0; i < 1; i++)
            {
                if (DataModel.IsIcoGrey[i])
                {
                    continue;
                }
                var _index = i * 2 + 1;
                var _id = _wingItem.ExtraData[_index];
                var _tbTrain = Table.GetWingTrain(_id);
                if (_tbTrain == null)
                {
                    continue;
                }

                var _count = _tbTrain.MaterialCount;
                var _gold = PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes);
                if (_tbTrain.UsedMoney > _gold)
                {
                    continue;
                }
                var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.WingQuailty);

                if (_tbTrain.Condition > _tbWingQuality.Segment)
                {
                    continue;
                }

                if (_tbTrain.UpStarID == -1)
                {
                    continue;
                }

                var _newvalue = PlayerDataManager.Instance.GetItemTotalCount(_tbTrain.MaterialID).Count;
                var _oldvalue = _newvalue - changeCount;
                //1912优化 当玩家羽毛数量≥20个 且金币数量≥20万时
                if (/*_count <= _oldvalue &&*/ _newvalue >= 20 && _gold >= 200000 && _count <= _newvalue && !DataModel.PartData.IsMaxTrain)
                {
                    PlayerDataManager.Instance.WeakNoticeData.WingTraining = true;
                    //PlayerDataManager.Instance.WeakNoticeData.BagTotal = true;
                    //PlayerDataManager.Instance.WeakNoticeData.WingTotal = true;
                    //PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = true;
                    DataModel.CanUpGrade[i] = true;
                }
                else
                {
                    DataModel.CanUpGrade[i] = false;
                }
                if (!DataModel.IsIcoGrey[i])
                {
                    isWingTrain.Add(DataModel.CanUpGrade[i]);
                }                      
            }
            foreach (var item in isWingTrain)
            {
                if (item)
                {
                    PlayerDataManager.Instance.WeakNoticeData.WingTraining = true;
                    if (isWingTrain != null)
                    {
                        isWingTrain.Clear();
                    }
                    return;
                }
                else
                {
                    PlayerDataManager.Instance.WeakNoticeData.WingTraining = false;
                }
            }
            if (isWingTrain != null)
            {
                isWingTrain.Clear();
            }
        }
        //刷新部位属性
        private void RenewalWingPortionAttribute()
        {
            var _index = DataModel.PartData.PartIndex;
            DataModel.PartData.PartAttributes.Clear();
            var _attrs = new ObservableCollection<AttributeChangeDataModel>();
            var _dicAttr = new Dictionary<int, int>();
            var _tbWingTrain = Table.GetWingTrain(DataModel.PartData.TrainId);
            var _tbWingTrainAddPropIDLength1 = _tbWingTrain.AddPropID.Length;
            for (var i = 0; i < _tbWingTrainAddPropIDLength1; i++)
            {
                var _nAttrId = _tbWingTrain.AddPropID[i];
                var _nValue = _tbWingTrain.AddPropValue[i];
                if (_nAttrId < 0 || _nValue <= 0)
                {
                    break;
                }
                if (_nValue > 0 && _nAttrId != -1)
                {
                    _dicAttr.modifyValue(_nAttrId, _nValue);
                }
            }

            var _dicAttrNext = new Dictionary<int, int>();
            if (_tbWingTrain.UpStarID != -1)
            {
                var _tbWingTrainNext = Table.GetWingTrain(_tbWingTrain.UpStarID);
                var _tbWingTrainNextAddPropIDLength2 = _tbWingTrainNext.AddPropID.Length;
                for (var i = 0; i < _tbWingTrainNextAddPropIDLength2; i++)
                {
                    var _nAttrId = _tbWingTrainNext.AddPropID[i];
                    var _nValue = _tbWingTrainNext.AddPropValue[i];
                    if (_nAttrId < 0 || _nValue <= 0)
                    {
                        break;
                    }
                    if (_nValue > 0 && _nAttrId != -1)
                    {
                        _dicAttrNext.modifyValue(_nAttrId, _nValue);
                    }
                }
            }
            {
                // foreach(var i in dicAttr)
                var _enumerator2 = (_dicAttr).GetEnumerator();
                while (_enumerator2.MoveNext())
                {
                    var i = _enumerator2.Current;
                    {
                        var _attr = new AttributeChangeDataModel();
                        _attr.Type = i.Key;
                        _attr.Value = i.Value;
                        var _nextValue = 0;
                        if (_dicAttrNext.TryGetValue(_attr.Type, out _nextValue))
                        {
                            _attr.Change = _nextValue - i.Value;
                            _attr.Change = AmendPropertiesValue(i.Key, _attr.Change);
                        }
                        _attr.Value = AmendPropertiesValue(i.Key, _attr.Value);
                        _attrs.Add(_attr);
                    }
                }
            }
            {
                // foreach(var i in dicAttrNext)
                var _enumerator3 = (_dicAttrNext).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var i = _enumerator3.Current;
                    {
                        var _type = i.Key;
                        var _value = 0;
                        if (!_dicAttr.TryGetValue(_type, out _value))
                        {
                            var _attr = new AttributeChangeDataModel();
                            _attr.Type = _type;
                            _attr.Value = _value;
                            _attr.Change = i.Value - _value;
                            _attr.Change = AmendPropertiesValue(i.Key, _attr.Change);
                            _attr.Value = AmendPropertiesValue(i.Key, _attr.Value);
                            _attrs.Add(_attr);
                        }
                    }
                }
            }
            DataModel.PartData.PartAttributes = _attrs;
        }

        //刷新翅膀总属性
        private void RenewalCompleteAttribute()
        {
            var _dicAttr = new Dictionary<int, int>();
            var _attrs = new ObservableCollection<AttributeBaseDataModel>();
            var _tbWing = Table.GetWingQuality(DataModel.ItemData.ItemId);
            if (_tbWing == null)
                return;

            PlayerAttr.FillWingAdvanceAttr(_dicAttr, DataModel.ItemData);

            //培养属性
            for (var i = 0; i < 1; ++i)
            {
                var _tbWingTrain = Table.GetWingTrain(DataModel.ItemData.ExtraData[1 + i * 2]);
                if (_tbWingTrain == null)
                {
                    continue;
                }
                for (var j = 0; j != _tbWingTrain.AddPropID.Length; ++j)
                {
                    var _nAttrId = _tbWingTrain.AddPropID[j];
                    var _nValue = _tbWingTrain.AddPropValue[j];
                    if (_nAttrId < 0 || _nValue <= 0)
                    {
                        break;
                    }
                    if (_nValue > 0 && _nAttrId != -1)
                    {
                        if (_nAttrId == 105)
                        {
                            if (_dicAttr.ContainsKey(5))
                            {
                                _dicAttr.modifyValue(5, _nValue);
                            }
                            if (_dicAttr.ContainsKey(6))
                            {
                                _dicAttr.modifyValue(6, _nValue);
                            }
                            if (_dicAttr.ContainsKey(7))
                            {
                                _dicAttr.modifyValue(7, _nValue);
                            }
                            if (_dicAttr.ContainsKey(8))
                            {
                                _dicAttr.modifyValue(8, _nValue);
                            }
                        }
                        else
                        {
                            _dicAttr.modifyValue(_nAttrId, _nValue);
                        }
                    }
                }
            }
            //翅膀战力
            DataModel.Fightforce = PlayerDataManager.Instance.GetAttrFightPoint(_dicAttr);
            {
                // foreach(var i in dicAttr)
                var _enumerator1 = (_dicAttr).GetEnumerator();
                while (_enumerator1.MoveNext())
                {
                    var _i = _enumerator1.Current;
                    {
                        var _attr = new AttributeBaseDataModel();
                        _attr.Type = _i.Key;
                        _attr.Value = _i.Value;
                        _attr.Value = AmendPropertiesValue(_i.Key, _attr.Value);
                        _attrs.Add(_attr);
                    }
                }
            }
            DataModel.WholeAttributes = _attrs;
        }

        //刷新翅膀阶数显示数据
        private void RenewalWingAdvancedInfo()
        {
            {
                // foreach(var data in DataModel.QualityDatas)
                var _enumerator4 = (DataModel.QualityDatas).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _data = _enumerator4.Current;
                    {
                        if (_data.ItemId <= DataModel.ItemData.ItemId)
                        {
                            _data.IsGrey = false;
                        }
                        else
                        {
                            _data.IsGrey = true;
                        }
                    }
                }
            }
        }

        //刷新翅膀升阶人物属性
        private void RenewalWingQualitiesAttribute(int wingId)
        {
            var _tbWing = Table.GetWingQuality(wingId);
            if (_tbWing == null)
            {
                return;
            }
            if (_tbWing.Segment > GameUtils.WingQualityMax)
            {
                GameUtils.ShowHintTip(200012);
                return;
            }

            {
                // foreach(var data in DataModel.QualityDatas)
                var _enumerator5 = (DataModel.QualityDatas).GetEnumerator();
                while (_enumerator5.MoveNext())
                {
                    var _data = _enumerator5.Current;
                    {
                        _data.IsSelect = _data.ItemId == wingId ? 1 : 0;
                        //var tbQuality = Table.GetWingQuality(data.WingId);
                        if (_data.IsSelect == 1)
                        {
                            var _tbWingQuality = Table.GetWingQuality(_data.WingId);
                            DataModel.SelectQuality = _tbWingQuality.Segment;
                            var _tbItem = Table.GetItemBase(_data.ItemId);
                            if (_tbItem != null)
                            {
                                DataModel.SelectName = _tbItem.Name;
                            }
                            else
                            {
                                DataModel.SelectName = "";
                            }
                            if(_data.WingId != chosenWingId)RefreshWingModel(_data.WingId);
                        }
                    }
                }
            }

            DataModel.QualityId = wingId;
            DataModel.QualityAttributes.Clear();
            var _attrs = new ObservableCollection<AttributeChangeDataModel>();
            var _dicAttr = new Dictionary<int, int>();
            var _dicAttrNext = new Dictionary<int, int>();
            var _dicAttrNextMax = new Dictionary<int, int>();

            if (wingId == DataModel.ItemData.WingQuailty)
            { // 当前阶段
                PlayerAttr.FillWingAdvanceAttr(_dicAttr, DataModel.ItemData);
                if (DataModel.IsAdvanceFull)
                {
                    PlayerAttr.FillWingBreakAttr(_dicAttrNext, wingId + 1);
                    // dicAttrNext = dicAttrNext - dicAttr;
                    var _tempAttrDict = new Dictionary<int, int>();
                    var _enumorator1 = _dicAttrNext.GetEnumerator();
                    while (_enumorator1.MoveNext())
                    {
                        int _attr;
                        if (_dicAttr.TryGetValue(_enumorator1.Current.Key, out _attr))
                        {
                            _tempAttrDict[_enumorator1.Current.Key] = _enumorator1.Current.Value - _attr;
                        }
                    }
                    _dicAttrNext = _tempAttrDict;
                }
                else
                { // 成长属性
                    for (var i = 0; i < _tbWing.GrowPropID.Length; ++i)
                    {
                        var _nAttrId = _tbWing.GrowPropID[i];
                        if (_nAttrId < 0)
                        {
                            break;
                        }
                        var _valueMin = _tbWing.GrowMinProp[i];
                        var _valueMax = _tbWing.GrowMaxProp[i];
                        if (_valueMin > 0 && _valueMax >= _valueMin)
                        {
                            _dicAttrNext.modifyValue(_nAttrId, _valueMin);
                            if (_valueMax != _valueMin)
                            {
                                _dicAttrNextMax.modifyValue(_nAttrId, _valueMax);
                            }
                        }
                    }
                }
            }
            else if (wingId > DataModel.ItemData.WingQuailty)
            { // 其它阶段
                PlayerAttr.FillWingAdvanceAttr(_dicAttr, DataModel.ItemData);
                PlayerAttr.FillWingBreakAttr(_dicAttrNext, wingId);

                // dicAttrNext = dicAttrNext - dicAttr;
                var _tempAttrDict = new Dictionary<int, int>();
                var _enumorator1 = _dicAttrNext.GetEnumerator();
                while (_enumorator1.MoveNext())
                {
                    int attr;
                    if (_dicAttr.TryGetValue(_enumorator1.Current.Key, out attr))
                    {
                        _tempAttrDict[_enumorator1.Current.Key] = _enumorator1.Current.Value - attr;
                    }
                }
                _dicAttrNext = _tempAttrDict;

                _dicAttr.Clear();
                PlayerAttr.FillWingBreakAttr(_dicAttr, wingId);
            }
            else
            {
                PlayerAttr.FillWingBreakAttr(_dicAttr, wingId);
            }

            var _enumerator7 = (_dicAttr).GetEnumerator();
            while (_enumerator7.MoveNext())
            {
                var _i = _enumerator7.Current;
                {
                    var _attr = new AttributeChangeDataModel();
                    _attr.Type = _i.Key;
                    _attr.Value = _i.Value;
                    int _nextValue;
                    if (_dicAttrNext.TryGetValue(_attr.Type, out _nextValue))
                    {
                        _attr.Change = _nextValue;
                        _attr.Change = AmendPropertiesValue(_i.Key, _attr.Change);
                        int _nextValueMax;
                        if (_dicAttrNextMax.TryGetValue(_attr.Type, out _nextValueMax))
                            _attr.ChangeEx = _nextValueMax;
                    }
                    _attr.Value = AmendPropertiesValue(_i.Key, _attr.Value);
                    _attrs.Add(_attr);
                }
            }

            DataModel.QualityAttributes = _attrs;
        }

        private void RefreshWingModel(int modelId)
        {
            var _e = new WingModelRefreh(modelId);
            EventDispatcher.Instance.DispatchEvent(_e);
            chosenWingId = modelId;
        }

        //刷新部位培养经验
        private void _RenewalWingCultivate(int ret)
        {
            //for (var i = 0; i < 5; i++)
            //{
            //    DataModel.CanUpGrade[i] = false;
            //}
            var _NowExp = DataModel.PartData.Exp;
            var _tbWingTrain = Table.GetWingTrain(DataModel.PartData.TrainId);
            var _oldTrainCount = _tbWingTrain.TrainCount;
            if (ret >0)
            {
                //暴击！增加经验 {0}
                _NowExp += _tbWingTrain.CritAddExp[ret-1];
                var _str = string.Format(GameUtils.GetDictionaryText(220301), _tbWingTrain.CritAddExp[ret-1]);
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_str));

                var _e5 = new WingNotifyLogicEvent(1, ret);
                EventDispatcher.Instance.DispatchEvent(_e5);
            }
            else
            {
                var _e5 = new WingNotifyLogicEvent(0, ret);
                EventDispatcher.Instance.DispatchEvent(_e5);
                //增加经验 {0}
                var _str = string.Format(GameUtils.GetDictionaryText(220300), _tbWingTrain.AddExp);
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_str));
                _NowExp += _tbWingTrain.AddExp;
            }
            var _levelup = 0;
            var _maxList = new List<int>();
            _maxList.Add(_tbWingTrain.ExpLimit);
            while (_NowExp >= _tbWingTrain.ExpLimit)
            {
                _levelup++;
                if (_tbWingTrain.UpStarID == -1)
                {
                    _NowExp = 0;
                    break;
                }
                _NowExp -= _tbWingTrain.ExpLimit;
                PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Wing);
                _tbWingTrain = Table.GetWingTrain(_tbWingTrain.UpStarID);
                if (_tbWingTrain == null)
                {
                    break;
                }
                _maxList.Add(_tbWingTrain.ExpLimit);
                
            }

            var _partIndex = DataModel.PartData.PartIndex;

            DataModel.PartData.Exp = _NowExp;
            DataModel.ItemData.ExtraData[_partIndex * 2 + 2] = DataModel.PartData.Exp;

            var _oldSliderValue = DataModel.PartData.TrainSlider.TargetValue;
            if (_levelup > 0)
            {
                DataModel.PartData.Layer = _tbWingTrain.TrainCount;
                DataModel.PartData.Star = _tbWingTrain.TrainStar;
                DataModel.ItemData.ExtraData[_partIndex * 2 + 1] = _tbWingTrain.Id;
                DataModel.PartData.TrainId = _tbWingTrain.Id;

                if (_tbWingTrain.UpStarID == -1)
                {
                    DataModel.PartData.IsMaxTrain = true;
                    PlayerDataManager.Instance.WeakNoticeData.WingTraining = false;
                }
                //ExamineGreyWingPartIndex(_partIndex);
                ExamineGreyWingPartIndex(0);
                RenewalCompleteAttribute();
                RenewalWingPortionAttribute();
                var _newTrainCount = _tbWingTrain.TrainCount;
                if (_newTrainCount != _oldTrainCount)
                {
                    //成功升级，当前重数已培养完成
                    var _e2 = new ShowUIHintBoard(220303);
                    EventDispatcher.Instance.DispatchEvent(_e2);

                    //var _e = new WingRefreshTrainCount(_tbWingTrain.PosX);
                    var _e = new WingRefreshTrainCount(_newTrainCount);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    OnRenewalWingPortion();
                    // PartIndex 0->翅翼  1-》翅翘 2-》翅羽 3-》翅骨 4->翅翎
                    PlatformHelper.UMEvent("WingTrain", DataModel.PartData.PartIndex.ToString(), _newTrainCount);
                }
                else
                {
                    //成功升级
                    var _e1 = new ShowUIHintBoard(220302);
                    EventDispatcher.Instance.DispatchEvent(_e1);

                    //点亮最后一重最后一个球
                    int count;
                    if (DataModel.PartData.IsMaxTrain)
                    {
                        count = _tbWingTrain.TrainStar == 10 ? 10 : _tbWingTrain.TrainStar - 1;
                    }
                    else
                    {
                        count = _tbWingTrain.TrainStar - 1;
                    }


                    var _e = new WingRefreshStarCount(count);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
                DataModel.PartData.TrainSlider.MaxValues = _maxList;
                DataModel.PartData.TrainSlider.TargetValue = DataModel.PartData.Exp / (float)_tbWingTrain.ExpLimit +
                                                             (_maxList.Count - 1);
            }
            else
            {
                DataModel.PartData.TrainSlider.MaxValues = new List<int> { _tbWingTrain.ExpLimit };
                DataModel.PartData.TrainSlider.TargetValue = DataModel.PartData.Exp / (float)_tbWingTrain.ExpLimit;

                var _dif = DataModel.PartData.TrainSlider.TargetValue - _oldSliderValue;
                var _costTime = System.Math.Max(_dif,0.5f);
                if (DataModel.IsAutoTrain)
                {
                    OnTrainWingportionAuto(_costTime);
                }
            }
        }

        //设置翅膀物品ID
        private void SettingWingPropId(int id)
        {
            DataModel.ItemData.ItemId = id;
            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.ItemId);
            if (_tbWingQuality.Segment == GameUtils.WingQualityMax)
            {
                DataModel.IsMaxAdvance = true;
            }
            else
            {
                DataModel.IsMaxAdvance = false;
            }

            var _curGrowValue = DataModel.ItemData.ExtraData.Benison;
            var _maxGrowValue = _tbWingQuality.GrowProgress;
            DataModel.IsAdvanceFull = (_curGrowValue >= _maxGrowValue);
            if (DataModel.IsAdvanceFull)
            {
                DataModel.ItemData.NeedItemId = _tbWingQuality.BreakNeedItem;
                DataModel.ItemData.NeedItemCount = _tbWingQuality.BreakNeedCount;
                DataModel.AdvanceCostMoney = _tbWingQuality.BreakNeedMoney;
            }
            else
            {
                DataModel.ItemData.NeedItemId = _tbWingQuality.MaterialNeed;
                DataModel.ItemData.NeedItemCount = _tbWingQuality.MaterialCount;
                DataModel.AdvanceCostMoney = _tbWingQuality.UsedMoney;
            }
        }

        //刷新翅膀升级所需物品
        private void RenewalUpgradeWingProp(ItemsChangeData changeData)
        {
            var _itemInfo = changeData.ItemsChange[0];
            SettingWingPropId(_itemInfo.ItemId);
            DataModel.ItemData.Count = _itemInfo.Count;
            DataModel.ItemData.Index = _itemInfo.Index;
            DataModel.ItemData.ExtraData.InstallData(_itemInfo.Exdata);
            //OnClickWingPart(0);
            RenewalCompleteAttribute();
            ExamineGreyWingPart();
            PlayerAttr.Instance.SetAttrChange(PlayerAttr.PlayerAttrChange.Equip);
        }

        //翅膀进阶
        private void WingAdvancement()
        {
            if (DataModel.IsAutoAdvance)
            {
                DataModel.IsAutoAdvance = false;
                if (m_AdvanceCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_AdvanceCoroutine);
                    m_AdvanceCoroutine = null;
                }
            }
            if (m_AdvanceCoroutine != null)
            {
                //NetManager.Instance.StopCoroutine(m_AdvanceCoroutine);
                return;
            }
            if (!ExamineWingAdvancement())
            {
                if (m_AdvanceCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_AdvanceCoroutine);
                    m_AdvanceCoroutine = null;
                }
                return;
            }
            m_AdvanceCoroutine = NetManager.Instance.StartCoroutine(WingAdvancementCoroutine(0.0f));
        }

        //翅膀自动进阶
        private void WingAdvancementSelfmotion(float delay = 0.0f)
        {
            if (!ExamineWingAdvancement())
            {
                DataModel.IsAutoAdvance = false;
                if (m_AdvanceCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(m_AdvanceCoroutine);
                    m_AdvanceCoroutine = null;
                }
                return;
            }
            if (m_AdvanceCoroutine != null)
            {
                //NetManager.Instance.StopCoroutine(m_AdvanceCoroutine);
                return;
            }
            m_AdvanceCoroutine = NetManager.Instance.StartCoroutine(WingAdvancementCoroutine(delay));
        }

        //发送翅膀进阶网络请求
        private IEnumerator WingAdvancementCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            using (new BlockingLayerHelper(1))
            {
                var _msg = NetManager.Instance.WingFormation(-1);
                yield return _msg.SendAndWaitUntilDone();
                m_AdvanceCoroutine = null;
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.SyncResources(_msg.Response.Resources);
                        NetManager.Instance.SyncItems(_msg.Response.Items);
                        RenewalWingAdvanceData(_msg.Response.AdvanceRet);
                        if (ExamineWingAdvancement(false))
                        {
                            PlayerDataManager.Instance.NoticeData.WingAdvance = true;
                            PlayerDataManager.Instance.WeakNoticeData.BagTotal = true;
                            PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = true;
                        }
                        else
                        {
                            PlayerDataManager.Instance.NoticeData.WingAdvance = false;
                        }

                        if (_msg.Response.AdvanceRet == 1)
                        {
                            var _tbWingQuality = Table.GetWingQuality(DataModel.ItemData.ItemId);
                            if (_tbWingQuality != null)
                            {
                                PlatformHelper.UMEvent("WingAdvance", _tbWingQuality.Segment.ToString());
                            }
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        DataModel.IsAutoAdvance = false;
                        if (_msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                        {
                            if (!ExamineWingAdvancement())
                            {

                            }
                        }
                        Logger.Error("WingFormation Error!............ErrorCode..." + _msg.ErrorCode);
                    }
                }
                else
                {
                    DataModel.IsAutoAdvance = false;
                    Logger.Error("WingFormation Error!............State..." + _msg.State);
                }
            }
        }

        //向服务器发送翅膀部位培养请求
        private IEnumerator WingCultivateCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            using (new BlockingLayerHelper(1))
            {
                if (!DataModel.IsAutoTrain && delay > 0.0001f)
                {
                    m_TrainCoroutine = null;
                    yield break;
                }

                //var _msg = NetManager.Instance.WingTrain(DataModel.PartData.PartIndex);
                var _msg = NetManager.Instance.WingTrain(0);
                yield return _msg.SendAndWaitUntilDone();
                m_TrainCoroutine = null;
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _er = new WingNotifyLogicEvent(2, 0);
                        EventDispatcher.Instance.DispatchEvent(_er);

                        PlayerDataManager.Instance.SyncResources(_msg.Response.Resources);
                        NetManager.Instance.SyncItems(_msg.Response.Items);
                        _RenewalWingCultivate(_msg.Response.TrainRet);
                    }
                    else
                    {
                        DataModel.IsAutoTrain = false;

                        if (_msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                        {
                            if (!ExamineTrainWingPart())
                            {
                                Logger.Error("WingTrain Error!............ErrorCode..." + _msg.ErrorCode);
                            }
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                            Logger.Error("WingTrain Error!............ErrorCode..." + _msg.ErrorCode);
                        }
                        
                    }
                }
                else
                {
                    DataModel.IsAutoTrain = false;
                    Logger.Error("WingTrain Error!............State..." + _msg.State);
                }
            }
        }


        #endregion

        #region 事件函数

        private void OnExDataInitionEvent(IEvent ievent)
        {       
            if (ExamineWingAdvancement(false))
            {
                PlayerDataManager.Instance.NoticeData.WingAdvance = true;
                PlayerDataManager.Instance.WeakNoticeData.BagTotal = true;
                //PlayerDataManager.Instance.WeakNoticeData.WingTotal = true;
                PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.WingAdvance = false;
            }
            RenewalWingTrainNotice(0);
        }
        private void OnBagPropNumChangeEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_BagItemCountChange;
            var _tb = Table.GetWingTrain(1);
            if (_e.ItemId == _tb.MaterialID)
            {
                RenewalWingTrainNotice(_e.ChangeCount);
            }

            if (DataModel.ItemData == null || DataModel.ItemData.WingQuailty == -1)
            {
                return;
            }
            RenewalWingAdvanceMsg(_e.ItemId, _e.ChangeCount);
        }
        private void OnLvUpEvent(IEvent ievent)
        {
            var _level = PlayerDataManager.Instance.GetLevel();
            if (_level % 10 != 0)
            {
                return;
            }
            if (ExamineWingAdvancement(false))
            {
                PlayerDataManager.Instance.NoticeData.WingAdvance = true;
                PlayerDataManager.Instance.WeakNoticeData.BagTotal = true;
                //PlayerDataManager.Instance.WeakNoticeData.WingTotal = true;
                PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = true;
            }
        }
        //监听所有翅膀操作事件包括
        // 1.	显示部位信息
        // 2.	翅膀部位培养
        // 3.	关闭自动培养
        // 4.	开启自动培养
        // 5.	翅膀升阶
        // 6.	开启自动升阶
        // 7.	关闭关闭自动升级
        private void OnWingOperatedEvent(IEvent ievent)
        {
            var _e = ievent as WingOperateEvent;
            switch (_e.Type)
            {
                //case -3: //遗弃
                //{
                //    OnShowPartInfo(_e.Index);
                //}
                //    break;
                case -2://Tab Menu切换事件
                {
                    if (_e.Index >= 0 && _e.Index <= 1)
                    {
                        DataModel.ShowTab = _e.Index;
                    }
                    DataModel.IsAutoTrain = false;
                    if (m_TrainCoroutine != null)
                    {
                        NetManager.Instance.StopCoroutine(m_TrainCoroutine);
                        m_TrainCoroutine = null;
                    }

                    DataModel.IsAutoAdvance = false;
                   
                }
                    break;
                case -1://培养
                    {
                        switch (_e.Index)
                        {
                            case 0://手动培养
                                {
                                    OnTrainWingPortion();
                                }
                                break;
                            case 1://自动培养
                                {
                                    OnClickWingPartAutoTrain();
                                }
                                break;
                            case 2://培养晋级了
                                {
                                    OnClickwingPartAutoTrain();
                                }
                                break;
                        }
                    }
                    break;
                case 0://切换部位，现在已保留index=0;
                {
                    OnTipWingPortion(_e.Index);
                }
                    break;
                case 1:  //翅膀进阶相关事件
                {
                    switch (_e.Index)
                    {
                        case 0://自动进阶
                        {
                            WingAdvancement();
                        }
                            break;
                        case 1://手动进阶
                        {
                            OnTipWingAdvanceAuto();
                        }
                            break;
                    }
                }
                    break;
            }
        }


        //监听翅膀cell点击事件

        private void OnWingQuailtyCellTipEvent(IEvent ievent)
        {
            var _e = ievent as WingQuailtyCellClick;
            if (_e == null)
                return;
            var _wingId = _e.Data.WingId;
            if (DataModel.QualityId != _wingId)
            {
                RenewalWingQualitiesAttribute(_wingId);
               // RefreshWingModel(_wingId);
            }
        }


        #endregion           
    }
}