/********************************************************************************* 

                         Scorpion




  *FileName:FuctionTipsFrameController

  *Version:1.0

  *Date:2017-06-12

  *Description:

**********************************************************************************/
#region using

using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;

#endregion

namespace ScriptController
{
    public class MethodSkillFrameCtrler : IControllerBase
    {


        #region 静态变量

        #endregion

        #region 成员变量
        private int m_currentShareTalentId;
        private FuctionDataModel m_DataModel;
        private int m_equipTipDirtyMark;
        private bool m_TipsChanged;
        #endregion

        #region 构造函数
        public MethodSkillFrameCtrler()
        {
            CleanUp();
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_DataModel = new FuctionDataModel();
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

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            if (m_DataModel != null)
            {
                Table.ForeachFunctionOn((record) =>
                {
                    var _condition = GameUtils.CheckFuctionOnCondition(record.OpenLevel, record.TaskID, record.State);
                    if (_condition != 0)
                    {
                        m_DataModel.FuctionCondition = record.FrameDesc;
                        m_DataModel.FuctionName = record.Name;
                        m_DataModel.FuctionDes = record.IconDesc;
                        m_DataModel.IconId = record.IconId;
                        if (GameUtils.CheckFuctionOnConditionByLevel(record.OpenLevel) != 0)
                        {
                            m_DataModel.ProgressType = 2;
                            m_DataModel.ProgressValue = PlayerDataManager.Instance.GetLevel();
                            if (m_DataModel.ProgressValue < 0)
                            {
                                m_DataModel.ProgressValue = 0;
                            }
                            m_DataModel.ProgressMaxVale = record.OpenLevel;
                        }
                        if (GameUtils.CheckFuctionOnConditionByMission(record.TaskID) != 0)
                        {
                            m_DataModel.ProgressType = 1;
                            int lastMissionOrder = GameUtils.GetMainMissionOrderByFunctionId(record.Id - 1);
                            m_DataModel.ProgressValue = Table.GetMissionBase(GameUtils.GetCurMainMissionId()).MissionBianHao - lastMissionOrder;
                            if (m_DataModel.ProgressValue < 0)
                            {
                                m_DataModel.ProgressValue = 0;
                            }
                            m_DataModel.ProgressMaxVale = Table.GetMissionBase(record.TaskID).MissionBianHao - lastMissionOrder;
                        }
                        return false;
                    }
                    return true;
                });
            }
        }



        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件

        #endregion

  

  



   
    }
}