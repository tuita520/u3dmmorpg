#region using

using System;
using System.ComponentModel;
using ClientDataModel;
using DataTable;
using ScriptManager;
using EventSystem;

#endregion

namespace ScriptController
{
    public class CharacterInfoController : IControllerBase
    {
        private StarDataModel DataModel;
        public CharacterInfoController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnRefrehEquipBagItemStatus);
            EventDispatcher.Instance.AddEventListener(Attr_Change_Event.EVENT_TYPE, OnAttrChange);
            EventDispatcher.Instance.AddEventListener(ClickStarEvent.EVENT_TYPE, OnOpenStarAtt);
            EventDispatcher.Instance.AddEventListener(StarRefreshEvent.EVENT_TYPE, RefreshStar);

            
        }

        private IControllerBase Attribute;
        private IControllerBase BackPack;
        private FrameState mState;
       
        private void OnAttrChange(IEvent ievent)
        {
            var e = ievent as Attr_Change_Event;
            if (e.Type == eAttributeType.Strength
                || e.Type == eAttributeType.Agility
                || e.Type == eAttributeType.Intelligence
                || e.Type == eAttributeType.Endurance)
            {
                PlayerDataManager.Instance.RefreshEquipStatus();
                PlayerDataManager.Instance.RefreshEquipBagStatus();
            }
        }
      

        private void OnRefrehEquipBagItemStatus(IEvent ievent)
        {
            var e = ievent as UIEvent_BagChange;
            if (e.HasType(eBagType.Equip))
            {
                if (State == FrameState.Open)
                {
                    PlayerDataManager.Instance.RefreshEquipBagStatus();
                }
            }
        }

        public void CleanUp()
        {
            BackPack = UIManager.Instance.GetController(UIConfig.BackPackUI);
            Attribute = UIManager.Instance.GetController(UIConfig.AttriFrameUI);
        }

        public void OnChangeScene(int sceneId)
        {

        }

        public object CallFromOtherClass(string name, object[] param)
        {
            throw new NotImplementedException(name);
        }

        public void OnShow()
        {
            if (BackPack != null)
            {
                BackPack.CallFromOtherClass("SetPackType", new object[] {BackPackController.BackPackType.Character});
            }
            //清除主界面的装备更换提示
            EventDispatcher.Instance.DispatchEvent(new UIEvent_HintCloseEvent());
        }

        public void Close()
        {
            BackPack.Close();
            Attribute.Close();
        }

        public void Tick()
        {
        }
        private void RefreshStar(IEvent ievent)
        {
        
            int starNum = PlayerDataManager.Instance.GetExData(eExdataDefine.e688);
            DataModel.StarNum = starNum;

            int[] titleList = { 3019, 3020, 3021, 3022, 3023 };
            if (starNum == 0)//current not open  up down 
            {
                var midRecord = Table.GetNameTitle(titleList[starNum]);
                DataModel.MidAttributes = GameUtils.StarAddAttr(midRecord);
                DataModel.MidFixed = GameUtils.GetDictionaryText(100003036);
              //  DataModel.MidName = midRecord.Name;
                DataModel.MidCondition = midRecord.GainDesc;
                DataModel.ShowType = 1;
             //   ShowStarDetailEvent evt = new ShowStarDetailEvent(midRecord.Id, 2, 3);//cur 1 next 2 mid 3
             //   EventDispatcher.Instance.DispatchEvent(evt);
            }
            else if (starNum == 5)//mid
            {
                var midRecord = Table.GetNameTitle(titleList[starNum - 1]);
                DataModel.MidAttributes = GameUtils.StarAddAttr(midRecord);
                DataModel.MidFixed = GameUtils.GetDictionaryText(100003035);
             //   DataModel.MidName = midRecord.Name;
                DataModel.MidCondition = GameUtils.GetDictionaryText(100002169);//100003035
                DataModel.ShowType = 1;
              //  ShowStarDetailEvent evt = new ShowStarDetailEvent(midRecord.Id, 2, 3);//cur 1 next 2 mid 3
              //  EventDispatcher.Instance.DispatchEvent(evt);
            }
            else //up down
            {

                var titleRecord = Table.GetNameTitle(titleList[starNum - 1]);
                DataModel.CurrentAttributes = GameUtils.StarAddAttr(titleRecord);
                DataModel.CurrentFixed = GameUtils.GetDictionaryText(100003035);
              //  DataModel.CurrentName = titleRecord.Name;
                DataModel.CurrentCondition = GameUtils.GetDictionaryText(100002169);
              //  ShowStarDetailEvent evt1 = new ShowStarDetailEvent(titleRecord.Id, 2, 1);//cur 1 next 2 mid 3
              //  EventDispatcher.Instance.DispatchEvent(evt1);
                var nextRecord = Table.GetNameTitle(titleList[starNum]);
                DataModel.NextAttributes = GameUtils.StarAddAttr(nextRecord);
                DataModel.NextFixed = GameUtils.GetDictionaryText(100003036);
               // DataModel.NextName = nextRecord.Name;
                DataModel.NextCondition = nextRecord.GainDesc;
                DataModel.ShowType = 2;
              //  ShowStarDetailEvent evt = new ShowStarDetailEvent(nextRecord.Id, 2, 2);//cur 1 next 2 mid 3
               // EventDispatcher.Instance.DispatchEvent(evt);
            }
            DataModel.ShowEffect = 1;
        }
        
        private void OnOpenStarAtt(IEvent ievent)
        {
            DataModel.ShowEffect = DataModel.ShowEffect == 1 ? 2 : 1;
            int starNum = PlayerDataManager.Instance.GetExData(eExdataDefine.e688);

            int[] titleList = { 3019, 3020, 3021, 3022, 3023 };
            if (starNum == 0)//current not open  up down 
            {
                ShowStarDetailEvent evt = new ShowStarDetailEvent(titleList[starNum], 2, 2);//cur 1 next 2 mid 3
                EventDispatcher.Instance.DispatchEvent(evt);
            }
            else if (starNum == 5)//mid
            {
                ShowStarDetailEvent evt = new ShowStarDetailEvent(titleList[starNum - 1], 2, 2);//cur 1 next 2 mid 3
                EventDispatcher.Instance.DispatchEvent(evt);
            }
            else //up down
            {
                ShowStarDetailEvent evt1 = new ShowStarDetailEvent(titleList[starNum - 1], 2, 0);//cur 1 next 2 mid 3
                EventDispatcher.Instance.DispatchEvent(evt1);
                ShowStarDetailEvent evt = new ShowStarDetailEvent(titleList[starNum], 2, 1);//cur 1 next 2 mid 3
                EventDispatcher.Instance.DispatchEvent(evt);
            }
        }

        public void RefreshData(UIInitArguments data)
        {
            DataModel = new StarDataModel();
            RefreshStar(null);
            var packArg = new BackPackArguments();
            if (data == null)
            {
                packArg.Tab = 1;
            }
            else
            {
                packArg.Tab = data.Tab;
            }
            PlayerDataManager.Instance.RefreshEquipStatus();
            BackPack.RefreshData(packArg);
            BackPack.CallFromOtherClass("SetPackType", new object[] {BackPackController.BackPackType.Character});
            Attribute.RefreshData(data);
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State
        {
            get { return mState; }
            set { mState = value; }
        }
    }
}