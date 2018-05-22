#region using

using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class ClimbingTowerRewardController : IControllerBase
    {
        public ClimbingTowerRewardController()
        {
            CleanUp();
        }

        private TowerResultDataModel DataModel;

        public void CleanUp()
        {
            DataModel = new TowerResultDataModel();
        }



        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
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
        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }
        private void RefreshResultData(IEvent ievent)
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var args = data as TowerRewardUIArguments;
            if (args == null)
            {
                return;
            }
            if (args.Result == (int) eDungeonCompleteType.Success)
            {
                var cur = PlayerDataManager.Instance.GetExData((int) eExdataDefine.e623);
                var max = PlayerDataManager.Instance.GetExData((int) eExdataDefine.e621);
                DataModel.AwardItems.Clear();
                DataModel.OnceRewards.Clear();
                var tbTower = Table.GetClimbingTower(cur);
                Table.ForeachClimbingTower(tb =>
                {
                    if (tb.FubenId == args.FubenId)
                    {
                        tbTower = tb;
                        return false;
                    }
                    return true;
                });
                var tbNext = Table.GetClimbingTower(tbTower.Id + 1);

                if (tbTower != null)
                {
                    for (int i = 0; i < tbTower.RewardList.Count&&i<tbTower.NumList.Count; i++)
                    {
                        var bagItemData = new ItemIdDataModel();
                        bagItemData.ItemId = tbTower.RewardList[i];
                        bagItemData.Count = tbTower.NumList[i];
                        DataModel.AwardItems.Add(bagItemData);
                    }
                    if (max == cur)
                    {
                        for (int i = 0; i < tbTower.OnceRewardList.Count && i < tbTower.OnceNumList.Count; i++)
                        {
                            var bagItemData = new ItemIdDataModel();
                            bagItemData.ItemId= tbTower.OnceRewardList[i];
                            bagItemData.Count = tbTower.OnceNumList[i];
                            DataModel.OnceRewards.Add(bagItemData);
                        }
                    }
                    DataModel.bWin = args.Result == (int) eDungeonCompleteType.Success;
                    DataModel.bFirst = (args.IsFirst == 1);
                    DataModel.bHasNext = DataModel.bWin && tbNext != null;
                    if (tbNext != null)
                    {
                        DataModel.strNext = string.Format(GameUtils.GetDictionaryText(100001220), tbNext.Id);
                    }
                }
            }
        }
    }
}