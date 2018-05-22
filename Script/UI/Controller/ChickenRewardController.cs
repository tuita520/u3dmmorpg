using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;

namespace ScriptController
{
    public class ChickenRewardController : IControllerBase
    {


        #region 成员变量

        private ChickenFightResultModel DataModel;

        #endregion

        #region 构造函数

        public ChickenRewardController()
        {
           // EventDispatcher.Instance.AddEventListener(UIEvent_DungeonReward.EVENT_TYPE, OnReceiveAwardEvent);
            CleanUp();
        }

        #endregion

        #region 固有函数

        public void CleanUp()
        {
            DataModel = new ChickenFightResultModel();
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
            var _args = data as ChickenRewardUIArgument;
            if (_args == null)
            {
                return;
            }
            DataModel.btnName = GameUtils.GetDictionaryText(100000029);
            DataModel.Rank = _args.Rank;
            DataModel.RankStr = string.Format(GameUtils.GetDictionaryText(470), _args.Rank);
            DataModel.FubenId = _args.FubenId;
            DataModel.BattleResult = 1;
            DataModel.Rewards.Clear();
            for (int i = 0; i < _args.Items.Count; i++)
            {
                ItemIdDataModel item = new ItemIdDataModel();
                item.ItemId = _args.Items[i].ItemId;
                item.Count = _args.Items[i].Count;
                DataModel.Rewards.Add(item);
            }
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

        #endregion
    }
}