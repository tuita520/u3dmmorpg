/********************************************************************************* 

                         Scorpion




  *FileName:GainItemHintController

  *Version:1.0

  *Date:2017-06-13

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;
using Shared;
using UnityEngine;
using System;

#endregion

namespace ScriptController
{
    public class GetTermRemindFrameCtrler : IControllerBase
    {

        #region 静态变量

        #endregion

        #region 成员变量
        private GainItemHintDataModel m_DataModel;

        //提示框的存在时间，到时间后会自动关闭
        private float m_RemainTime = 0f;

        //定时器handler，用来自动关闭提示界面
        private  object m_Timer;

        //倒计时10秒穿戴
        private float[]CountdownTime ={ 9f,9f} ;

        //缓存，显示不了的提示暂存在这里
        private List<CacheEntry> m_Caches = new List<CacheEntry>();
        #endregion

        #region 构造函数
        public GetTermRemindFrameCtrler()
        {
            CleanUp();
        
            m_RemainTime = int.Parse(Table.GetClientConfig(109).Value) / 1000.0f;

            EventDispatcher.Instance.AddEventListener(UIEvent_HintEquipEvent.EVENT_TYPE, OnBtnEquipEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HintUseItemEvent.EVENT_TYPE, OnBtnUseClickEvent);
            EventDispatcher.Instance.AddEventListener(EquipChangeEvent.EVENT_TYPE, OnEquipAlterEvent);
            EventDispatcher.Instance.AddEventListener(UseItemEvent.EVENT_TYPE, OnObjUseEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_HintCloseEvent.EVENT_TYPE, OnCloseUIEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, BagItemAlter);
        }
        #endregion

        #region 固有函数
        public FrameState State { get; set; }
        public void CleanUp()
        {
            m_DataModel = new GainItemHintDataModel();
        }

        public void OnShow()
        {
        }

        public void Close()
        {
            if (m_Timer != null)
            {
                TimeManager.Instance.DeleteTrigger(m_Timer);
                m_Timer = null;
            }
            m_DataModel.UseMask = 0;
            m_Caches.Clear();
        }

        public void Tick()
        {
            if (State != FrameState.Open)
                return;
            for (int i = 0; i < m_DataModel.Entrys.Count; i ++)
            {
                if (m_DataModel.Entrys[i].BagItemData.ItemId <= 0)
                {
                    ShutPanel(i);
                    return;
                }
            }
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as GainItemHintArguments;
            if (_args == null)
            {
                return;
            }

            ResetTimer();

            var _itemId = _args.ItemId;
            var _bagIdx = _args.BagIndex;

            if (CheckItemIdEntry(_itemId))
            {
                return;
            }

            // 选择上面那个，还是下面那个
            var _entryId = 0;
            for (; _entryId < 2; _entryId++)
            {
                if (!BitFlag.GetLow(m_DataModel.UseMask, _entryId))
                {
                    break;
                }
            }

            if (_entryId < 2)
            {
                //如果还有空闲的tip pannel，则显示
                CheckAndShowMsg(_entryId, _itemId, _bagIdx);
            }
            else
            {
                //如果没有空闲的tip pannel，则缓存下来
                var _entry = m_Caches.Find(e => e.ItemId == _itemId);
                if (_entry == null)
                {
                    _entry = new CacheEntry { ItemId = _itemId, BagIdx = _bagIdx };
                    m_Caches.Add(_entry);
                }
            }
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

        #endregion



        #region 事件

        //装备
        private void OnBtnEquipEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HintEquipEvent;
            var _index = _e.Index;
            var _data = m_DataModel.Entrys[_index];
            var _bagItem = _data.BagItemData;

            IControllerBase control = UIManager.Instance.GetController(UIConfig.EquipComPareUI);
            control.CallFromOtherClass("ReplaceEquip", new[] { _bagItem });
        }

        //响应装备更换事件
        private void OnEquipAlterEvent(IEvent ievent)
        {
            if (State != FrameState.Open)
            {
                return;
            }

            var _e = ievent as EquipChangeEvent;
            var _equip = _e.Item;
            if (_equip.ItemId == -1)
            {
                return;
            }
            var _playerData = PlayerDataManager.Instance;
            var _worstEquip = _playerData.FindWorstEquip(_equip);

            //把缓存里比这件装备差的，都移除掉
            var _toRemove = new List<CacheEntry>();
            {
                var __list1 = m_Caches;
                var __listCount1 = __list1.Count;
                for (var __i1 = 0; __i1 < __listCount1; ++__i1)
                {
                    var _entry = __list1[__i1];
                    {
                        var _tbItem2 = Table.GetItemBase(_entry.ItemId);
                        var _item2 = _playerData.GetItem(_tbItem2.InitInBag, _entry.BagIdx);

                        int fightValueAdd;
                        if (_playerData.CompareEquips(_worstEquip, _item2, out fightValueAdd))
                        {
                            if (fightValueAdd <= 0)
                            {
                                _toRemove.Add(_entry);
                            }
                        }
                    }
                }
            }

            for (var i = m_Caches.Count - 1; i >= 0; --i)
            {
                if (_toRemove.Contains(m_Caches[i]))
                {
                    m_Caches.RemoveAt(i);
                }
            }

            for (var i = 0; i < 2; ++i)
            {
                // 检查是否需要关闭hint panel
                if (BitFlag.GetLow(m_DataModel.UseMask, i))
                {
                    var _data2 = m_DataModel.Entrys[i];
                    var _bagItem = _data2.BagItemData;
                    if (_bagItem.BagId == _equip.BagId && _bagItem.Index == _equip.Index)
                    {
                        ShutPanel(i);
                    }
                    else
                    {
                        int fightValueAdd;
                        if (_playerData.CompareEquips(_worstEquip, _bagItem, out fightValueAdd))
                        {
                            if (fightValueAdd <= 0)
                            {
                                ShutPanel(i);
                            }
                            else
                            {
                                _data2.FightValueOld = _worstEquip.FightValue;
                                _data2.FightValueAdd = fightValueAdd;
                            }
                        }
                    }
                }
            }
        }

        //使用
        private void OnBtnUseClickEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_HintUseItemEvent;
            var _data = m_DataModel.Entrys[_e.Index];
            GameUtils.UseItem(_data.BagItemData);
        }

        //响应物品被使用的事件
        private void OnObjUseEvent(IEvent ievent)
        {
            OnUseNewObjEvent(ievent);
            //if (State != FrameState.Open)
            //{
            //    return;
            //}

            //var e = ievent as UseItemEvent;
            //var item = e.Item;

            //var itemTotalCount = PlayerDataManager.Instance.GetItemTotalCount(item.ItemId);
            //if (itemTotalCount.Count > 1)
            //{
            //    item.Count = itemTotalCount.Count - 1;
            //    ResetTimer();
            //    return;
            //}

            //for (var i = 0; i < 2; ++i)
            //{
            //    // 检查是否需要关闭hint panel
            //    if (BitFlag.GetLow(DataModel.UseMask, i))
            //    {
            //        var itemData = DataModel.Entrys[i].BagItemData;
            //        if (itemData == item)
            //        {
            //            ClosePanel(i);
            //        }
            //    }
            //}
        }


        private void OnUseNewObjEvent(IEvent ievent)
        {
            if (State != FrameState.Open)
            {
                return;
            }

            var _e = ievent as UseItemEvent;
            if (_e == null)
                return;

            var _item = _e.Item;
            if (_item == null)
                return;

            var _entryIndex = -1;
            for (var i = 0; i < 2; ++i)
            {
                if (BitFlag.GetLow(m_DataModel.UseMask, i))
                {
                    var _itemData = m_DataModel.Entrys[i].BagItemData;
                    if (_itemData.Index == _item.Index && _itemData.ItemId == _item.ItemId)
                    {
                        _entryIndex = i;
                        break;
                    }
                }
            }

            if (_entryIndex == -1)
                return;

            var _itemTotalCount = PlayerDataManager.Instance.GetItemTotalCount(_item.ItemId);
            if (_itemTotalCount.Count > 1)
            {
                if (_item.Count <= 1)
                {
                    var _bagItem = PlayerDataManager.Instance.GetBagItemByItemId(_item.BagId, _item.ItemId);
                    if (_bagItem != null)
                    {
                        m_DataModel.Entrys[_entryIndex].BagItemData = _bagItem;
                    }

                    //ClosePanel(entryIndex);
                    //return;
                }

                m_DataModel.Entrys[_entryIndex].Count = _itemTotalCount.Count - 1;

                ResetTimer();
            }
            else
            {
                ShutPanel(_entryIndex);
            }
        }

        //关闭界面
        private void OnCloseUIEvent(IEvent ievent)
        {
            ShutUI();
        }

        #endregion

        #region 其它

        //重置Timer
        private void ResetTimer()
        {
            var _time = Game.Instance.ServerTime.AddSeconds(m_RemainTime);
            if (m_Timer != null)
            {
                TimeManager.Instance.ChangeTime(m_Timer, _time);
            }
            else
            {
                m_Timer = TimeManager.Instance.CreateTrigger(_time, ShutUI);
            }
        }


        //检查并显示一个提示条
        private  bool CheckAndShowMsg(int entryId, int itemId, int bagIdx)
        {            
            var _args = PlayerDataManager.Instance.GetGainItemHintEntryArgs(itemId, bagIdx);
            if (_args == null)
            {
                return false;
            }
            
            m_DataModel.UseMask = BitFlag.IntSetFlag(m_DataModel.UseMask, entryId);

            var _entry = m_DataModel.Entrys[entryId];
            _entry.BagItemData = _args.ItemData;
            _entry.Count = _args.Count;
            _entry.FightValueOld = _args.FightValueOld;
            _entry.Index = _args.OldEquipIdx;
            _entry.FightValueAdd = _entry.BagItemData.FightValue - _entry.FightValueOld;

            var quality = Table.GetItemBase(_entry.BagItemData.ItemId).Quality;
            m_DataModel.IsShowTimes[entryId] = false;
            if (quality <= 3 && _entry.BagItemData.BagId == 0)
            {
                m_DataModel.IsShowTimes[entryId] = true;
                CountdownTime[entryId] = 9f;
                m_DataModel.CountdownTimes[entryId] = "(" + (int)CountdownTime[entryId] + GameUtils.GetDictionaryText(1045) + ")";
            }             
            return true;
       }

        //检查下，当前显示的entry里有没有 itemId 的物品，如果有，则刷新个数
        private bool CheckItemIdEntry(int itemId)
        {
            var _tbItem = Table.GetItemBase(itemId);
            //如果不是物品，返回false
            if (_tbItem.Type >= 10000 && _tbItem.Type <= 10099)
            {
                return false;
            }

            var _entrys = m_DataModel.Entrys;        
            for (int i = 0, imax = _entrys.Count; i < imax; i++)
            {
                if (BitFlag.GetLow(m_DataModel.UseMask, i))
                {
                    var _entry = _entrys[i];
                    if (_entry.BagItemData.ItemId == itemId)
                    {
                        //刷新count，由于获得了同样的物品，数量肯定发生变化了
                        _entry.Count = PlayerDataManager.Instance.GetItemTotalCount(itemId).Count;
                        return true;
                    }
                }
            }
            return false;
        }

        private void BagItemAlter(IEvent ievent)
        {
            if (State != FrameState.Open)
            {
                return;
            }

            var _e = ievent as UIEvent_BagChange;
            if (_e.HasType(eBagType.BaseItem))
            {
                for (var i = 0; i < 2; ++i)
                {
                    if (BitFlag.GetLow(m_DataModel.UseMask, i))
                    {
                        var _itemData = m_DataModel.Entrys[i].BagItemData;
                        if (_itemData.ItemId == -1 || _itemData.Count == 0)
                        {
                            ShutPanel(i);
                        }
                    }
                }                    
            }
        }

        //关闭一个提示条
        private void ShutPanel(int index)
        {
            ResetTimer();
            if (m_Caches.Count > 0)
            {
                for (int i = 0, imax = m_Caches.Count; i < imax; i++)
                {
                    var _cache = m_Caches[i];
                    if (CheckItemIdEntry(_cache.ItemId) || CheckAndShowMsg(index, _cache.ItemId, _cache.BagIdx))
                    {
                        m_Caches.RemoveRange(0, i + 1);
                        return;
                    }
                }
                m_Caches.Clear();
            }
            //缓存中没有符合条件的，则关闭该提示条
            m_DataModel.UseMask = BitFlag.IntSetFlag(m_DataModel.UseMask, index, false);
            if (m_DataModel.UseMask == 0)
            {
                ShutUI();
            }
        }

        //关闭整个UI
        private void ShutUI()
        {
            if (m_Timer != null)
            {
                TimeManager.Instance.DeleteTrigger(m_Timer);
                m_Timer = null;
            }
            m_DataModel.UseMask = 0;
            m_Caches.Clear();
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.GainItemHintUI));
        }

        #endregion
    }
}