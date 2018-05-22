/********************************************************************************* 

                         Scorpion



  *FileName:UIAccomplishmentTipFrameCtrler

  *Version:1.0

  *Date:2017-07-12

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class UIAccomplishmentTipFrameCtrler : IControllerBase
    {

        #region 静态变量

        private static int s_iMaxAchievement = 10;

        #endregion

        #region 成员变量

        private List<int> m_listAchievementTipQueue = new List<int>();
        private FrameState m_State;
        private AchievementTipDataModel m_DataModel { get; set; }

        #endregion

        #region 构造函数

        public UIAccomplishmentTipFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(Event_AchievementTip.EVENT_TYPE, OnAccomplishmentTipEvent);
            EventDispatcher.Instance.AddEventListener(Event_NextAchievementTip.EVENT_TYPE, OnNextAccomplishmentTipEvent);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            m_DataModel = new AchievementTipDataModel();
            m_listAchievementTipQueue.Clear();
        }

        public void OnChangeScene(int sceneId)
        {
            m_listAchievementTipQueue.Clear();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            throw new NotImplementedException(name);
        }

        public void OnShow()
        {
        }

        public void Close()
        {
            m_listAchievementTipQueue.Clear();
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public FrameState State
        {
            get { return m_State; }
            set { m_State = value; }
        }

        #endregion

        #region 事件函数

        private void OnAccomplishmentTipEvent(IEvent ievent)
        {
            var _a1 = Table_Tamplet.Convert_Int(Table.GetClientConfig(562).Value);
            if (!PlayerDataManager.Instance.GetFlag(_a1))
            {
                return;
            }

            if (GuideManager.Instance.IsGuiding())
            {
                return;
            }

            var _e = ievent as Event_AchievementTip;
            if (null == _e)
            {
                return;
            }

            var _id = _e.Id;
            m_listAchievementTipQueue.Add(_id);

            if (1 == m_listAchievementTipQueue.Count)
            {
                m_DataModel.Id = _id;
                EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.AchievementTip));
            }
            else if (m_listAchievementTipQueue.Count > s_iMaxAchievement)
            {
                m_listAchievementTipQueue.RemoveAt(0);
            }
        }

        private void OnNextAccomplishmentTipEvent(IEvent ievent)
        {
            if (m_listAchievementTipQueue.Count > 0)
            {
                m_DataModel.Id = m_listAchievementTipQueue[0];
                m_listAchievementTipQueue.RemoveAt(0);
            }
            else
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.AchievementTip));
            }
        }


        #endregion          
    }
}