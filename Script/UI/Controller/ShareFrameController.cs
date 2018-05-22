/********************************************************************************* 

                         Scorpion



  *FileName:PartakeFrameCtrler

  *Version:1.0

  *Date:2017-06-27

  *Description:

**********************************************************************************/
#region using

using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class PartakeFrameCtrler : IControllerBase
    {

        #region 成员变量
        private ShareDataModel mDataModel;
        #endregion

        #region 构造函数
        public PartakeFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UI_EVENT_ShareBtnShow.EVENT_TYPE, OnSetPartakeBtnSeenEvent);
        }
        #endregion

        #region 事件
        private void OnSetPartakeBtnSeenEvent(IEvent ievent)
        {
            var _e = ievent as UI_EVENT_ShareBtnShow;
            SetPartakeBtnSeen(_e != null && _e.isVisible);
        }
        #endregion


        #region 逻辑函数
        private void RefurbishDatum(object data, object dataEx = null)
        {
        }

        private void SetPartakeBtnSeen(bool IsVisible)
        {
#if UNITY_IPHONE || UNITY_EDITOR
            var _configTable = Table.GetClientConfig(210);
            mDataModel.IsShareButtonShow = false;
            mDataModel.IsShareButtonShow = (_configTable.Value == "1") && IsVisible;
#else
        mDataModel.IsShareButtonShow = false;
#endif
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            mDataModel = new ShareDataModel();
            SetPartakeBtnSeen(true);
        }
        public void Tick(float f)
        {
        }
        public void Close()
        {
        }

        public void Tick()
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

        public void RefreshData(UIInitArguments data)
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return mDataModel;
        }

        public FrameState State { get; set; }
        #endregion 
    }
}