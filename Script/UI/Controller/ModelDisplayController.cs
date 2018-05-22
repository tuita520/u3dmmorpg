/********************************************************************************* 

                         Scorpion




  *FileName:ModelDisplayController

  *Version:1.0

  *Date:2017-06-28

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class ShowModelFrameCtrler : IControllerBase
    {
        #region 静态变量

        #endregion

        #region 成员变量
        private ModelDisplayDataModel DataModel;
        private Action okAction;
        #endregion

        #region 构造函数
        public ShowModelFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(ModelDisplay_Equip_Event.EVENT_TYPE, OnEquipEvent);
            EventDispatcher.Instance.AddEventListener(ShowItemHint.EVENT_TYPE, OnShowItemPromptEvent);
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new ModelDisplayDataModel();
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
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
        public void OnShow()
        {
            var _e = new ModelDisplay_ShowModel_Event(DataModel.Item.ItemId, 0);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        public void RefreshData(UIInitArguments data)
        {
            if (data.Args.Count <= 0)
                return;

            var okNameDictId = 100000908;
            var showItemId = data.Args[0];

            okAction = null;
            var args = data as ModleDisPlayUIArguments;
            if (args != null)
            {
                okAction = args.OkAction;
                if (args.OkDictId >= 0)
                {
                    okNameDictId = args.OkDictId;
                }
            }

            DataModel.Item.ItemId = showItemId;
            DataModel.Item.Count = 1;
            DataModel.Effect = 1;
            DataModel.ButtonName = GameUtils.GetDictionaryText(okNameDictId);

            var tbEquip = Table.GetItemBase(showItemId);
            if (tbEquip != null)
            {
                DataModel.ItemName = tbEquip.Name;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private  void OnShowItemPromptEvent(IEvent ievent)
        {
            var _e = ievent as ShowItemHint;
            if (_e == null)
                return;

            if (State == FrameState.Open || State == FrameState.Loading)
            {
                if (_e.ItemId == DataModel.Item.ItemId)
                {
                    return;
                }
            }                     
            var tbItem = Table.GetItemBase(_e.ItemId);
            if (tbItem == null)
            {
                Logger.Error("GainNewItem GetItemBase == null itemId ={0}", _e.ItemId);
                return;
            }
            NetManager.Instance.StartCoroutine(IEetorShowGainItem(_e.ItemId, tbItem.InitInBag));
           
        }
        private IEnumerator IEetorShowGainItem(int itemId,int bagId)
        {
            yield return new WaitForSeconds(0.8f);
            var controller = UIManager.Instance.GetController(UIConfig.CharacterUI);
            if (controller != null)
            {
                if (controller.State == FrameState.Open)
                {
                    yield break;
                }
            }
            
            var Items = PlayerDataManager.Instance.PlayerDataModel.Bags.Bags[bagId].Items;
            int bagIndex = -1;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].ItemId != -1)
                {
                    if (Items[i].ItemId == itemId)
                    {
                        bagIndex = Items[i].Index;
                        break;
                    }
                }
            }
            if(bagIndex == -1)
                yield break;



            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.GainItemHintUI,
                new GainItemHintArguments { ItemId = itemId, BagIndex = bagIndex }));
        }

        private void OnEquipEvent(IEvent ievent)
        {
            var e = ievent as ModelDisplay_Equip_Event;
            if (e == null)
            {
                return;
            }

            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ModelDisplayFrame));

            if (okAction != null)
            {
                okAction();
            }
            else
            {
                Equip(DataModel.Item.ItemId);
            }
        }
        #endregion  

        private void Equip(int equipId)
        {
            if (equipId <= 0)
            {
                return;
            }

            var _tbItemBase = Table.GetItemBase(equipId);
            var _itemType = GameUtils.GetItemInfoType(_tbItemBase.Type);
            if (_itemType == eItemInfoType.Equip)
            {
                GameUtils.ChangeEquip(equipId);
            }
            else if (_itemType == eItemInfoType.Wing)
            {

            }
            else if (_itemType == eItemInfoType.Elf)
            {
            
            }
        }
    }
}
