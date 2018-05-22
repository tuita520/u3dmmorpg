/********************************************************************************* 

                         Scorpion



  *FileName:NewStrongCtrler

  *Version:1.0

  *Date:2017-09-01

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
using UnityEngine;

#endregion

namespace ScriptController
{
    public class NewStrongCtrler : IControllerBase
    {
        #region 构造函数
        public NewStrongCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_NewStrongOperation.EVENT_TYPE, OnOperation);
            EventDispatcher.Instance.AddEventListener(SuitShowChangeEvent.EVENT_TYPE, OnShowChange);
        
        }


        #endregion

        #region 成员变量
        private NewStrongDataModel DataModel;

        private int mChange
        {
            get { return DataModel.Tab; }
            set { DataModel.Tab = value; }
        }

        #endregion



        #region 固有函数
        public void CleanUp()
        {
            DataModel = new NewStrongDataModel();
            mChange = 2;//策划需求，要先打开套装界面
        }

        public void RefreshData(UIInitArguments data)
        {
            if (DataModel.RankSuits.Count == 0)
            {
                var rankType =  PlayerDataManager.Instance.GetRoleId();
                DataModel.FaceId = GameUtils.GetRebornCircleIconId(rankType, PlayerDataManager.Instance.ExtData[51]);
                var playerLevel = PlayerDataManager.Instance.GetLevel();
                RankSuitDataModel recommendSuit = null;
                Table.ForeachSuitShow((record) =>
                {
                    if (record.TypeId != rankType)
                    {
                        return true;
                    }

                    if (record.IsShow == 0)
                    {
                        return true;
                    }

                    var suit = CreateOneSuit(record);
                    if (suit != null)
                    {
                        DataModel.AllRankSuits.Add(suit);
                        if ((int) (record.Id/10000) == mChange)
                        {
                            DataModel.RankSuits.Add(suit);
                            if (playerLevel > record.RecommendLevel)
                            {
                                recommendSuit = suit;
                            }                        
                        }
                    }

                    return true;
                });
                if (null == recommendSuit)
                {
                    DataModel.CurrentSelectSuit = DataModel.RankSuits[0];
                }
                else
                {
                    DataModel.CurrentSelectSuit = recommendSuit;
                }
            
                DataModel.CurrentSelectSuit.IsSelected = true;
            }
            DataModel.TabIdx = 0;
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
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
            if (name == "mChange")
            {
                return mChange;
            }
            return null;
        }

        public void OnShow()
        {

        }


        public FrameState State { get; set; }
        #endregion

        private void OnShowChange(IEvent ievent)
        {
//        NetManager.Instance.StartCoroutine(onShowChangeEnumerator());
            mChange = mChange == 2 ? 1 : 2;
            DataModel.RankSuits.Clear();

            var rankType = PlayerDataManager.Instance.GetRoleId();
            for (int i = 0; i < DataModel.AllRankSuits.Count; i++)
            {
                if ((int)(DataModel.AllRankSuits[i].Id / 10000) == mChange)
                {
                    if (rankType != DataModel.AllRankSuits[i].TypeId)
                        continue;
                    DataModel.RankSuits.Add(DataModel.AllRankSuits[i]);
                    if (DataModel.AllRankSuits[i].Id % 10000 == DataModel.CurrentSelectSuit.Id % 10000)
                    {
                        SetCurrentSuit(DataModel.AllRankSuits[i]);
                    }
                }
            }
        }

        private IEnumerator onShowChangeEnumerator()
        {
            mChange = mChange == 2 ? 1 : 2;
            DataModel.RankSuits.Clear();

            yield return new WaitForSeconds(1);
            var rankType = PlayerDataManager.Instance.GetRoleId();
            for (int i = 0; i < DataModel.AllRankSuits.Count; i++)
            {
                if ((int)(DataModel.AllRankSuits[i].Id / 10000) == mChange)
                {
                    if (rankType != DataModel.AllRankSuits[i].TypeId)
                        continue;
                    DataModel.RankSuits.Add(DataModel.AllRankSuits[i]);
                    //if (DataModel.AllRankSuits[i].Id % 10000 == DataModel.CurrentSelectSuit.Id % 10000)
                    //{
                    //    SetCurrentSuit(DataModel.AllRankSuits[i]);
                    //}
                }
            }


        }
        private void OnOperation(IEvent ievent)
        {
            var evn = ievent as UIEvent_NewStrongOperation;

            if (evn == null) return;

            switch (evn.operation)
            {
                case 1:
                {
                    var dataModel = evn.Data as RankSuitDataModel;
                    SetCurrentSuit(dataModel);
                }
                    break;
                case 3:
                {
                    RefreshSuitModel();
                }
                    break;
            }
        }

        private RankSuitDataModel CreateOneSuit(SuitShowRecord record)
        {
            var rankSuit = new RankSuitDataModel();
            rankSuit.Id = record.Id;
            rankSuit.TypeId = record.TypeId;
            var names = record.SuitName.Split('|');
            if (names.Length > 1)
            {
                rankSuit.Name = names[0];
                rankSuit.SuitName = string.Format("{0}+{1}", names[1], record.EnchantLevel);
            }
            else
            {
                rankSuit.Name = record.SuitName;
            }
        
            rankSuit.EnchantLevel = record.EnchantLevel;
        
            var count = record.part.Length;
            var battlePoint = 0ul;
            for (var i = 0; i < count; i++)
            {
                var equip = new ItemIdDataModel();
                equip.ItemId = -1;
                equip.Count = 0;
                rankSuit.Equips[i] = equip;

                var partId = record.part[i];
                if (partId == -1)
                {
                    continue;
                }
                var tableItem = Table.GetItemBase(partId);
                if (tableItem == null)
                {
                    continue;
                }
                var itemId = tableItem.Exdata[0];
                if (itemId == -1)
                {
                    continue;
                }
                equip.ItemId = itemId;
                equip.Count = 1;

                var tbEquip = Table.GetEquipBase(itemId);
                if (null == tbEquip)
                {
                    continue;
                }
                battlePoint = battlePoint + (ulong)tbEquip.FIghtNumDesc;
            }
            rankSuit.BattlePoint = battlePoint;
            return rankSuit;
        }

        private void SetCurrentSuit(RankSuitDataModel dataModel)
        {
            if (null == dataModel) return;
            DataModel.CurrentSelectSuit.IsSelected = false;
            DataModel.CurrentSelectSuit = dataModel;
            DataModel.CurrentSelectSuit.IsSelected = true;
            RefreshSuitModel();
        }

        private static readonly List<int> id2part = new List<int>
        {
            17,
            18,
            12,
            7,
            11,
            15,
            16,
            14,
        };

        private void RefreshSuitModel()
        {
            var equipList = new Dictionary<int, int>();

            for (int i = 0; i < id2part.Count; i++)
            {
                var itemid = DataModel.CurrentSelectSuit.Equips[i].ItemId;
                if (itemid != -1)
                {
                    var tableItem = Table.GetItemBase(itemid);
                    if (tableItem != null && tableItem.Exdata[0] != -1)
                    {
                        var equipId = tableItem.Exdata[0];
                        equipList.Add(id2part[i], equipId*100 + DataModel.CurrentSelectSuit.EnchantLevel);
                    }
                }
            }

            EventDispatcher.Instance.DispatchEvent(new UIEvent_NewStrongOperation(2, equipList));
        }


    }
}