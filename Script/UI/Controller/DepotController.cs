/********************************************************************************* 

                         Scorpion



  *FileName:EntrepotFrameCtrler

  *Version:1.0

  *Date:2017-06-09

  *Description:

**********************************************************************************/  
#region using

using System.ComponentModel;
using ScriptManager;
using EventSystem;

#endregion

namespace ScriptController
{
    public class EntrepotFrameCtrler : IControllerBase
    {


        #region 成员变量

        private IControllerBase m_BackPack;
        private FrameState m_State;

        #endregion

        #region 构造函数

        public EntrepotFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnUpdatePropBagItemStateEvent);
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            m_BackPack = UIManager.Instance.GetController(UIConfig.BackPackUI);
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
            m_BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Depot });
        }

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            m_BackPack.RefreshData(data);
            PlayerDataManager.Instance.RefreshEquipBagStatus(eBagType.Depot);
            m_BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Depot });
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return null;
        }

        public FrameState State
        {
            get { return m_State; }
            set { m_State = value; }
        }

        #endregion


        #region 事件

        private void OnUpdatePropBagItemStateEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_BagChange;
            if (State == FrameState.Open)
            {
                if (_e.HasType(eBagType.Equip))
                {
                    PlayerDataManager.Instance.RefreshEquipBagStatus();
                }
                if (_e.HasType(eBagType.Depot))
                {
                    PlayerDataManager.Instance.RefreshEquipBagStatus(eBagType.Depot);
                }
            }
        }

        #endregion   
    }
}