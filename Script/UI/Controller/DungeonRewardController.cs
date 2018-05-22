/********************************************************************************* 

                         Scorpion



  *FileName:CachotAwardFrameCtrler

  *Version:1.0

  *Date:2017-06-03

  *Description:

**********************************************************************************/  


#region using

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class CachotAwardFrameCtrler : IControllerBase
    {


        #region 成员变量

        private DungeonRewardFrameDataModel DataModel;

        #endregion

        #region 构造函数

        public CachotAwardFrameCtrler()
        {
            EventDispatcher.Instance.AddEventListener(UIEvent_DungeonReward.EVENT_TYPE, OnReceiveAwardEvent);
            CleanUp();
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            DataModel = new DungeonRewardFrameDataModel();
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
            var _args = data as DungeonRewardArguments;
            if (_args == null)
            {
                return;
            }

            var _seconds = _args.Seconds;
            var _formatStr = GameUtils.GetDictionaryText(1052);
            DataModel.UseTime = string.Format(_formatStr, _seconds / 60, _seconds % 60);

            var _tbFuben = Table.GetFuben(_args.FubenId);
            if (_tbFuben == null)
            {
                Logger.Error("tbFuben == null in DungeonRewardController.RefreshData()");
                return;
            }

            var _enterCount = PlayerDataManager.Instance.GetExData(_tbFuben.TodayCountExdata);
            //var items = new List<ItemIdDataModel>();
            //for (int i = 0, imax = tbFuben.DisplayCount.Count; i < imax; ++i)
            //{
            //    var itemId = tbFuben.DisplayReward[i];
            //    if (itemId == -1)
            //    {
            //        break;
            //    }
            //    var itemCount = tbFuben.DisplayCount[i];
            //    itemCount = GameUtils.GetRewardCount(tbFuben, itemCount, 0, enterCount);
            //    var item = new ItemIdDataModel();
            //    item.ItemId = itemId;
            //    item.Count = itemCount;
            //    items.Add(item);
            //}
            //DataModel.Rewards = new ObservableCollection<ItemIdDataModel>(items);
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }

        #endregion

        #region 事件

        private void OnReceiveAwardEvent(IEvent ievent)
        {
            var _evt = ievent as UIEvent_DungeonReward;
            if (_evt == null)
                return;
            var _items = new List<ItemIdDataModel>();
            for (int i = 0; i < _evt.reward.Items.Count; i++)
            {
                var _item = new ItemIdDataModel();
                _item.ItemId = _evt.reward.Items[i].ItemId;
                _item.Count = _evt.reward.Items[i].Count;
                if (_item.Count != 0)
                {
                    _items.Add(_item);
                }  
            }
            DataModel.Rewards = new ObservableCollection<ItemIdDataModel>(_items);
        }

        #endregion
    }
}