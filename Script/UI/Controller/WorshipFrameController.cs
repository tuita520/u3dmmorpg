/********************************************************************************* 

                         Scorpion



  *FileName:WorshipedFrameCtrler

  *Version:1.0

  *Date:2017-06-03

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;

#endregion

namespace ScriptController
{
    public class WorshipedFrameCtrler : IControllerBase
    {

        #region 成员变量

        private PlayerInfoMsg m_modelInfo;
        private WorshipDataModel m_DataModel;

        #endregion

        #region 构造函数

        public WorshipedFrameCtrler()
        {
            EventDispatcher.Instance.AddEventListener(WorshipOpetion.EVENT_TYPE, OnOperationWorkEvent);
            EventDispatcher.Instance.AddEventListener(BattleUnionRefreshModelView.EVENT_TYPE, OnModelMsgEvent);
            CleanUp();
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            m_DataModel = new WorshipDataModel();
        }

        public void OnShow()
        {
            if (m_modelInfo != null)
            {
                EventDispatcher.Instance.DispatchEvent(new WorshipRefreshModelView(m_modelInfo));
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
            var _instance = PlayerDataManager.Instance;

            var _moneyStr = GameUtils.GetDictionaryText(270287);
            var _diaStr = GameUtils.GetDictionaryText(270288);
            var _money = int.Parse(Table.GetClientConfig(391).Value);
            var _level = _instance.GetLevel();
            var _moneyGet1 =
                Table.GetSkillUpgrading(int.Parse(Table.GetClientConfig(392).Value)).GetSkillUpgradingValue(_level);
            var _moneyGet2 = int.Parse(Table.GetClientConfig(393).Value);
            var _diamond = int.Parse(Table.GetClientConfig(394).Value);
            var _diamondGet1 =
                Table.GetSkillUpgrading(int.Parse(Table.GetClientConfig(395).Value)).GetSkillUpgradingValue(_level);
            var _diamondGet2 = int.Parse(Table.GetClientConfig(396).Value);
            m_DataModel.WorshipTotalCount = int.Parse(Table.GetClientConfig(390).Value);

            m_DataModel.MoneyStr = String.Format(_moneyStr, _money, _moneyGet1, _moneyGet2);
            m_DataModel.DiaStr = String.Format(_diaStr, _diamond, _diamondGet1, _diamondGet2);
            m_DataModel.WorshipCount = _instance.GetExData(eExdataDefine.e72);

            if (m_modelInfo != null)
            {
                m_DataModel.CastellanName = m_modelInfo.Name;
            }
            var _titleName = string.Empty;
            foreach (var item in _instance._battleCityDic)
            {
                if (item.Value.Type == 0)
                {
                    _titleName = item.Value.Name;
                    break;
                }
            }
            _titleName += GameUtils.GetDictionaryText(270293);
            m_DataModel.TitleName = _titleName;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
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

        private void SetExamineIndex(int index)
        {
            m_DataModel.CheckSelectIndex = index;
        }

        private void DisplayFightUI()
        {
            if (PlayerDataManager.Instance.GetExData(eExdataDefine.e282) <= 0)
            {
                //先加入公会才能参加城战
                var _e = new ShowUIHintBoard(270289);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.BattleUnionUI,
                new BattleUnionArguments { Tab = 5 }));
        }

        private void DisplayMsgUI()
        {
            if (m_modelInfo == null)
            {
                return;
            }
            var _charId = m_modelInfo.Id;
            if (_charId == ObjManager.Instance.MyPlayer.GetObjId())
            {
                var _e1 = new Show_UI_Event(UIConfig.CharacterUI);
                EventDispatcher.Instance.DispatchEvent(_e1);
                return;
            }
            PlayerDataManager.Instance.ShowPlayerInfo(_charId);
        }

        private void Worshiped()
        {
            var _instance = PlayerDataManager.Instance;
            var _errorId = _instance.CheckCondition(Table.GetClientConfig(397).Value.ToInt());
            if (_errorId != 0)
            {
                //膜拜次数不足
                var _e = new ShowUIHintBoard(_errorId);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            if (_instance.GetExData(eExdataDefine.e72) >= m_DataModel.WorshipTotalCount)
            {
                //膜拜次数不足
                var _e = new ShowUIHintBoard(270290);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            if (m_DataModel.CheckSelectIndex == 0)
            {
                if (_instance.GetRes((int)eResourcesType.GoldRes) < Table.GetClientConfig(391).Value.ToInt())
                {
                    var _e = new ShowUIHintBoard(210100);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
            }
            else
            {
                if (_instance.GetRes((int)eResourcesType.DiamondRes) < Table.GetClientConfig(394).Value.ToInt())
                {
                    var _e = new ShowUIHintBoard(210102);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
            }
            NetManager.Instance.StartCoroutine(WorshipedCoroutine());
        }

        private IEnumerator WorshipedCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.Worship(m_DataModel.CheckSelectIndex);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_DataModel.WorshipCount++;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        #endregion

        #region 事件函数

        private void OnModelMsgEvent(IEvent ievent)
        {
            var _e = ievent as BattleUnionRefreshModelView;
            m_modelInfo = _e.Info;
        }

        private void OnOperationWorkEvent(IEvent ievent)
        {
            var _e = ievent as WorshipOpetion;
            switch (_e.Type)
            {
                case 0:
                {
                    Worshiped();
                }
                    break;
                case 1:
                {
                    DisplayMsgUI();
                }
                    break;
                case 2:
                {
                    DisplayFightUI();
                }
                    break;
                case 3:
                {
                    SetExamineIndex(0);
                }
                    break;
                case 4:
                {
                    SetExamineIndex(1);
                }
                    break;
            }
        }

        #endregion

    }
}