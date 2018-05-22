/********************************************************************************* 

                         Scorpion




  *FileName:ExchangeController

  *Version:1.0

  *Date:2017-06-06

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;

#endregion
namespace ScriptController
{
    public class CommutationFrameCtrler : IControllerBase
    {
        #region 成员变量
        ExchangeDataModel DataModel;
   
        private StoreRecord m_tbStoreGold;
        private StoreRecord m_tbStoreExp;

        public enum eExchangeType
        {
            Gold = 120000,
            Exp = 120001
        }
        #endregion

        #region 构造函数
        public CommutationFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(ExChangeInit_Event.EVENT_TYPE, OnExchangeDataInitEvent);
            EventDispatcher.Instance.AddEventListener(ExChange_Event.EVENT_TYPE, OnPurchasePressMsgBuyEvent);

        }
        #endregion

        #region 固有函数
        public FrameState State { get; set; }
        public void CleanUp()
        {
            DataModel = new ExchangeDataModel();
            m_tbStoreGold = Table.GetStore((int)eExchangeType.Gold);
            if (m_tbStoreGold == null)
                return;
            m_tbStoreExp = Table.GetStore((int) eExchangeType.Exp);
            if (m_tbStoreExp == null)
                return;
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

        public void Close()
        {
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {

        }



        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }
        #endregion
        #region 普通函数

        private IEnumerator ShopPurchaseCoroutine(int index, int count = 1)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreBuy(index, count, -1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (index == (int)eExchangeType.Gold)
                        {
                            DataModel.RemainGoldTimes -= 1;
                            var e = new PlayGoldOrExpEffectEvent(0);
                            EventDispatcher.Instance.DispatchEvent(e);
                            AlterGoldPrice();
                        }
                        else
                        {
                            DataModel.RemainExpTimes -= 1;
                            var e = new PlayGoldOrExpEffectEvent(1);
                            EventDispatcher.Instance.DispatchEvent(e);
                            AlterExpPrice();
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        Logger.Error("StoreBuy....StoreId= {0}...ErrorCode...{1}", index, _msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Error("StoreBuy............State..." + _msg.State);
                }
            }
        }
        private void AlterGoldPrice()
        {
            int times = DataModel.CurGoldTimes - DataModel.RemainGoldTimes;
            var _tbSkillUpGrading = Table.GetSkillUpgrading(m_tbStoreGold.WaveValue);
            if (null == _tbSkillUpGrading || 0 == _tbSkillUpGrading.Values.Count)
                return;
            if (times >= _tbSkillUpGrading.Values.Count)
                times = _tbSkillUpGrading.Values.Count - 1;
            DataModel.strGoldTimes = DataModel.RemainGoldTimes.ToString();//times.ToString() + "/" + DataModel.CurTimes.ToString();
            DataModel.GoldNeedDiamond = _tbSkillUpGrading.Values[times];
            DataModel.GoldNum = m_tbStoreGold.ItemCount;
        }
        private void AlterExpPrice()
        {
            int times = DataModel.CurExpTimes - DataModel.RemainExpTimes;
            var _tbSkillUpGrading = Table.GetSkillUpgrading(m_tbStoreExp.WaveValue);
            if (null == _tbSkillUpGrading || 0 == _tbSkillUpGrading.Values.Count)
                return;
            if (times >= _tbSkillUpGrading.Values.Count)
                times = _tbSkillUpGrading.Values.Count - 1;
            DataModel.strExpTimes = (DataModel.RemainExpTimes <= 0 ? 0 : DataModel.RemainExpTimes).ToString();//times.ToString() + "/" + DataModel.CurTimes.ToString();        
            DataModel.ExpNeedDiamond = _tbSkillUpGrading.Values[times];
            //DataModel.ExpNum = m_tbStoreExp.ItemCount;
            GetExpNumByLevel(m_tbStoreExp);
        }
        #endregion

        private void CheckGoldBuyLimit()
        {
            var _tbEx = Table.GetExdata(m_tbStoreGold.DayCount);
            if (_tbEx == null) return;

            int vipCount = 0;
            for (int i = 0; i < PlayerDataManager.Instance.TbVip.BuyItemId.Length; i++)
            {
                if (PlayerDataManager.Instance.TbVip.BuyItemId[i] == (int)eExchangeType.Gold && i < PlayerDataManager.Instance.TbVip.BuyItemCount.Length)
                {
                    vipCount = PlayerDataManager.Instance.TbVip.BuyItemCount[i];
                }
            }

            DataModel.CurGoldTimes = _tbEx.InitValue + vipCount;
            DataModel.RemainGoldTimes = PlayerDataManager.Instance.GetExData(m_tbStoreGold.DayCount) + vipCount;
        }

        private void CheckExpBuyLimit()
        {
            var _tbEx = Table.GetExdata(m_tbStoreExp.DayCount);
            if (_tbEx == null) return;

            int vipCount = 0;
            for (int i = 0; i < PlayerDataManager.Instance.TbVip.BuyItemId.Length; i++)
            {
                if (PlayerDataManager.Instance.TbVip.BuyItemId[i] == (int)eExchangeType.Exp && i < PlayerDataManager.Instance.TbVip.BuyItemCount.Length)
                {
                    vipCount = PlayerDataManager.Instance.TbVip.BuyItemCount[i];
                }
            }

            DataModel.CurExpTimes = _tbEx.InitValue + vipCount;
            DataModel.RemainExpTimes = PlayerDataManager.Instance.GetExData(m_tbStoreExp.DayCount) + vipCount;
            if (DataModel.RemainExpTimes <= 0) DataModel.RemainExpTimes = 0;        
        }

        #region 事件
        private void OnExchangeDataInitEvent(IEvent ievent)
        {
            CheckGoldBuyLimit();
            CheckExpBuyLimit();
            AlterGoldPrice();
            AlterExpPrice();
        }

        private void OnPurchasePressMsgBuyEvent(IEvent ievent)
        {
            var e = ievent as ExChange_Event;
            if (e == null)
                return;
            switch (e.Type)
            {
                case 0:
                {
                    Exchange((int)eExchangeType.Gold, m_tbStoreGold);
                    break;
                }
                case 1:
                {
                    Exchange((int)eExchangeType.Exp, m_tbStoreExp);
                    break;
                }
            }
        }
     
        private void Exchange(int index,StoreRecord storeRecord)
        {
            if (storeRecord == null)
                return;
            var _index = index;
            var _count = 1;

            //每日购买限制
            var _tbEx = Table.GetExdata(storeRecord.DayCount);
            if (_tbEx == null) return;

            int vipCount = 0;
            for (int i = 0; i < PlayerDataManager.Instance.TbVip.BuyItemId.Length; i++)
            {
                if (PlayerDataManager.Instance.TbVip.BuyItemId[i] == _index && i < PlayerDataManager.Instance.TbVip.BuyItemCount.Length)
                {
                    vipCount = PlayerDataManager.Instance.TbVip.BuyItemCount[i];
                }
            }
            var _dayCount = _tbEx.InitValue + vipCount;


            //当前剩余购买次数
            var _curCount = PlayerDataManager.Instance.GetExData(storeRecord.DayCount);
            var _times = _dayCount - _curCount;

            var _tbSkillUpGrading = Table.GetSkillUpgrading(storeRecord.WaveValue);
            if (null == _tbSkillUpGrading || 0 == _tbSkillUpGrading.Values.Count)
                return;
            if (index == (int)eExchangeType.Gold)
            {
                if (PlayerDataManager.Instance.GetRes(storeRecord.NeedType) < DataModel.GoldNeedDiamond)
                {
                    GameUtils.ShowHintTip(300401);
                    PlayerDataManager.Instance.ShowItemInfoGet(storeRecord.NeedType);
                    return;
                }                
            }
            else if (index == (int)eExchangeType.Exp)
            {
                if (PlayerDataManager.Instance.GetRes(storeRecord.NeedType) < DataModel.ExpNeedDiamond)
                {
                    GameUtils.ShowHintTip(100003320);
                    PlayerDataManager.Instance.ShowItemInfoGet(storeRecord.NeedType);
                    return;
                }             
            }
            NetManager.Instance.StartCoroutine(ShopPurchaseCoroutine(_index, _count));
        }


        private void GetExpNumByLevel(StoreRecord storeRecord)
        {
            int level =  PlayerDataManager.Instance.GetLevel();
            if (level <= 100)
            {
                DataModel.ExpNum = storeRecord.ItemCount;            
            }
            else
            {
                DataModel.ExpNum = storeRecord.ItemCount + ((level - 100) / 10) * storeRecord.BlessGrow;
            }
        }

        #endregion
 
  

  

  


    }
}
