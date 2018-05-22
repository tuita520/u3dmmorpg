using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptController;
using ClientDataModel;
using DataTable;
using EventSystem;

namespace ScriptController
{
    public class ChestInfoController : IControllerBase
    {
        private ChestInfoDataModel m_DataModel { get; set; }
        private ItemInfoDataModel m_ItemInfoDataModel { get; set; }
        private EquipInfoDataModel m_EquipInfoDataModel;
        private IControllerBase BackPack;
        private ItemInfoArguments m_IIA = new ItemInfoArguments();
        public ChestInfoController()
        {
            EventDispatcher.Instance.AddEventListener(UIEvent_ClickChest.EVENT_TYPE, SetChestId);
            EventDispatcher.Instance.AddEventListener(UIEvent_OpenChest.EVENT_TYPE, OpenChest);
            EventDispatcher.Instance.AddEventListener(Close_UI_Event.EVENT_TYPE, OnClosed);
            CleanUp();
        }
    
        #region Interface
        public FrameState State { get; set; }
        public void CleanUp()
        {
            if (m_DataModel != null)
            {
                m_DataModel.ChestItemsList.Clear();
                m_DataModel = null;
            }
            m_DataModel = new ChestInfoDataModel();
       
        }
        public void OnShow()
        {
            if (BackPack == null)
            {
                BackPack = UIManager.Instance.GetController(UIConfig.BackPackUI);
            }

            BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Chest });
              
        }
        public void Close()
        {
            BackPack.CallFromOtherClass("SetPackType", new object[] { BackPackController.BackPackType.Character });
        }
        public void Tick()
        {

        }
        public void RefreshData(UIInitArguments data)
        {

        }
        public INotifyPropertyChanged GetDataModel(string name)
        {
            if (name == "ItemInfoDataModel")
            {
                return m_ItemInfoDataModel;
            }
            return m_DataModel;
        }
        public void OnChangeScene(int sceneId)
        {

        }
        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }
        #endregion

        private void SetChestId(IEvent ievent)
        {
            UIEvent_ClickChest e = (ievent as UIEvent_ClickChest);
            if (m_DataModel.TableId != e.TabIdx)
            {
                m_DataModel.TableId = e.TabIdx;
                InitDataModel();
            }
            if (string.IsNullOrEmpty(e.From))
            {
                m_DataModel.ShowGetButton = false;
                m_DataModel.ShowSellButton = false;
            }
            else if (e.From == "Store")
            {
                m_DataModel.ShowGetButton = false;
                m_DataModel.ShowSellButton = false;
            }
            else if (e.From == "Bag")
            {
                m_DataModel.ShowGetButton = false;
                m_DataModel.ShowSellButton = true;
            }
            if (m_ItemInfoDataModel == null)
            {
                m_ItemInfoDataModel = new ItemInfoDataModel();
            }
        
            m_IIA.DataModel = e.BagDataModel;

            var controller = UIManager.Instance.GetController(UIConfig.ItemInfoUI);
            controller.RefreshData(m_IIA);

            m_ItemInfoDataModel = (controller.GetDataModel("") as ItemInfoDataModel);
        
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ChestInfoUI, new ChestInfoUIArguments()));
        }
        private void InitDataModel()
        {
            if (m_DataModel.ChestItemsList == null)
            {
                m_DataModel.ChestItemsList = new ObservableCollection<BagItemDataModel>();
            }
            m_DataModel.ChestItemsList.Clear();

            ItemBaseRecord ibr = Table.GetItemBase(m_DataModel.TableId);
            if (ibr != null && !string.IsNullOrEmpty(ibr.BoxOut))
            {
                string[] strs = ibr.BoxOut.Split('|');
                if (strs != null)
                {
                    for (int i = 0; i < strs.Length; i++)
                    {
                        string[] str1s = strs[i].Split(',');
                        if (str1s != null)
                        {
                            if (str1s.Length == 2)
                            {
                                m_DataModel.ChestItemsList.Add(new BagItemDataModel() { ItemId = Convert.ToInt32(str1s[0]), Count = Convert.ToInt32(str1s[1]) });
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }
        private void OpenChest(IEvent e)
        {
            GameUtils.UseItem(m_ItemInfoDataModel);
        }
        private void OnClosed(IEvent ievent)
        {
            if (this.State == FrameState.Open)
            {
                Close_UI_Event e = ievent as Close_UI_Event;
                if (e.config.Equals( UIConfig.ItemInfoUI))
                {
                    var controller = UIManager.Instance.GetController(UIConfig.ItemInfoUI);
                    controller.RefreshData(m_IIA);

                    m_ItemInfoDataModel = (controller.GetDataModel("") as ItemInfoDataModel).GetValue();
                }
            }
        }
    }
}
