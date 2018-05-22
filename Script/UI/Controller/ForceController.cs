/********************************************************************************* 

                         Scorpion




  *FileName:ForceController

  *Version:1.0

  *Date:2017-06-12

  *Description:

**********************************************************************************/

#region using

using System.ComponentModel;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class PowerFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        private ForceDataModel m_DataModel;
        private Coroutine m_RefreshCoroutine;
        private bool HasSend { get; set; }
        #endregion

        #region 构造函数
        public PowerFrameCtrler()
        {
            CleanUp();
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            m_DataModel = new ForceDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as ForceArguments;
            if (_args == null)
            {
                return;
            }
            HasSend = false;
            var _oldValue = _args.OldValue;
            var _newValue = _args.NewValue;
            m_DataModel.BeginValue = _oldValue;
            m_DataModel.EndValue = _newValue;
            if (State == FrameState.Open)
            {
                HasSend = true;
                var _e = new FightValueChange(m_DataModel.BeginValue, m_DataModel.EndValue);
                EventDispatcher.Instance.DispatchEvent(_e);
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
            if (HasSend == false)
            {
                var _e = new FightValueChange(m_DataModel.BeginValue, m_DataModel.EndValue);
                EventDispatcher.Instance.DispatchEvent(_e);
            }
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件

        #endregion
 



   
    }
}