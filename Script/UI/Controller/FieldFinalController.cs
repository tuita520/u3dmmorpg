
#region using

using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class FieldFinalController : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        private FieldFinalDataModel m_DataModel;
        #endregion

        #region 构造函数
        public FieldFinalController()
        {
            CleanUp();
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_DataModel = new FieldFinalDataModel();
            Table.ForeachWarFlag(tab =>
            {
                FieldFinalCellDataModel cell = new FieldFinalCellDataModel();
                cell.FlagId = tab.Id;
                cell.AllianceName = string.Empty;
                m_DataModel.Cells.Add(cell);
                return true;
            });
        }

        public void RefreshData(UIInitArguments data)
        {
            FieldFinalUIArgument info = data as FieldFinalUIArgument;
            if (info == null)
                return;
            foreach (var v in info._msg.list)
            {
                for (int i = 0; i < m_DataModel.Cells.Count; i++)
                {
                    var tmp = m_DataModel.Cells[i];
                    if (tmp.FlagId == v.id)
                    {
                        if (v.allianceId > 0)
                        {
                            tmp.AllianceName = v.name;
                            tmp.IsOccupy = true;
                        }
                        else
                        {
                            tmp.AllianceName = string.Empty;
                            tmp.IsOccupy = false;
                        }
                    }
                }

            }
            

        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return m_DataModel;
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
            //DataModel.ChangeValue = 0;
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

        #region 事件

        #endregion
 



   
    }
}