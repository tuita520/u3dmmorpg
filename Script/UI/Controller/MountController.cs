
#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class MountController : IControllerBase
    {
        private MountDataModel DataModel;
        private Coroutine m_TrainCoroutine;
        private Coroutine SkinCountDownCoroutine;
        private Dictionary<int, int> m_DicSkill2Mount = new Dictionary<int, int>();
        private Dictionary<int, DateTime> m_DicTimeLimitedSkins = new Dictionary<int, DateTime>();
        private List<int> FeedMaterialList = new List<int>();
        //计时器对象
        private object RefreshTimer;
        private delegate void func(int param=0);

        private int needItem { get; set; }
        private int needCount { get; set; }
        enum eAction
        {
            UI_SHOW = 10,
            UI_CLICK_RIDE = 13,
            UI_SELECT_MOUNT = 20,
            UI_MOUNT_UP = 21,
            UI_MOUNT_UP_AUTO = 22,
            UI_SKILL_SELECT = 30,
            UI_SKILL_UP = 31,
            UI_FEED = 40,
            UI_ADD_MOUNT_SKIN = 41,
        }

        private Dictionary<int, func> m_dicFun = new Dictionary<int, func>();
        public MountController()
        {
            CleanUp();
            //EventDispatcher.Instance.AddEventListener(AskMountRefreshModel_Event.EVENT_TYPE, OnModelRefresh);
            //EventDispatcher.Instance.AddEventListener(OnClickMountCell_Event.EVENT_TYPE, OnClickMount);
            //EventDispatcher.Instance.AddEventListener(OnClickMountUp_Event.EVENT_TYPE, OnClickMountUp);
            //EventDispatcher.Instance.AddEventListener(OnClickMountRide_Event.EVENT_TYPE, OnClickMountRide);
            //EventDispatcher.Instance.AddEventListener(OnMountSkill_Event.EVENT_TYPE, OnMountSkillEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnItemChange);
            EventDispatcher.Instance.AddEventListener(OnMountAction_Event.EVENT_TYPE, OnMountEvent);
            EventDispatcher.Instance.AddEventListener(MountMainTabEvent.EVENT_TYPE, OnMountMainTabEvent);
            EventDispatcher.Instance.AddEventListener(MountClickBtn_Event.EVENT_TYPE, OnClickTab);
            m_dicFun.Add((int)eAction.UI_SHOW,          OnModelRefresh);
            m_dicFun.Add((int)eAction.UI_SELECT_MOUNT,  OnSelectMount);
            m_dicFun.Add((int)eAction.UI_MOUNT_UP,      OnClickMountUp);
            m_dicFun.Add((int)eAction.UI_MOUNT_UP_AUTO, OnClickAutoUp);
            m_dicFun.Add((int)eAction.UI_CLICK_RIDE,    OnClickMountRide);
            m_dicFun.Add((int)eAction.UI_SKILL_SELECT,  OnSelectMountSkill);
            m_dicFun.Add((int)eAction.UI_SKILL_UP,      OnMountSkill);
            m_dicFun.Add((int)eAction.UI_FEED,          OnMountFeed);
            m_dicFun.Add((int)eAction.UI_ADD_MOUNT_SKIN,OnAddMountSkin);
        }

        public void CleanUp()
        {
            needItem = -1;
            needCount = -1;
            var list = new List<MountItemDataModel>();
            var skinList = new List<MountItemDataModel>();
            DataModel = new MountDataModel();
            FeedMaterialList.Clear();
            Table.ForeachMount(tb =>
            {
                if (tb.Level != 1)
                {
                    return true;
                }
                var item = new MountItemDataModel();
                item.MountId = tb.Id;
                item.Level = tb.Level;
                item.Step = tb.Step;
                item.ItemId = tb.ItemId;
                item.IsOpen = tb.IsOpen;
                item.IsPermanent = tb.IsPermanent;
                item.ValidityData = tb.ValidityData;
                item.NeedItemId = tb.NeedItem;
                item.NeedItemCount = tb.GetExp;//策划指定用此数据
                if (tb.Special > 0)
                {
                    item.strLimit = GameUtils.GetDictionaryText(274032);
                    var skinAddFight = new Dictionary<int, int>();
                    for (int i = 0; i < tb.Attr.Length && i < tb.Value.Length; i++)
                    {
                        if (tb.Attr[i] > 0)
                        {
                            skinAddFight.modifyValue(tb.Attr[i], tb.Value[i]);
                        }
                    }
                    item.SkinAddFightPoint = PlayerDataManager.Instance.GetElfAttrFightPoint(skinAddFight);
                    skinList.Add(item);
                }
                else
                {
                    item.strLimit = string.Format(GameUtils.GetDictionaryText(274000), tb.Step, tb.Level);
                    list.Add(item);
                }
                if (tb.SkillId > 0)
                {
                    m_DicSkill2Mount.Add(tb.SkillId, tb.Id);
                }
                return true;
            });
            list.Sort((a,b)=>
            {
                return a.Step>b.Step?1:-1;
            });
            for (int i = 0; i < list.Count; i++)
            {
                DataModel.MountBag.Add(list[i]);
            }

            skinList.Sort((a, b) =>
            {
                return a.Step > b.Step ? 1 : -1;
            });
            for (int i = 0; i < skinList.Count; i++)
            {
                DataModel.MountSkinBag.Add(skinList[i]);
            }

            Table.ForeachMountFeed(tb=> 
            {
                MountFeedItemDataModel item = new MountFeedItemDataModel();
                item.Item = new ItemIdDataModel();
                item.Item.ItemId = tb.Id;
                item.MaxCount = tb.MaxCount;
                item.NowCount = 0;

                FeedMaterialList.Add(tb.Id);
                DataModel.FeedItems.Add(item);
                return true;
            });

            Table.ForeachMountSkill(tb =>
            {
                MountSkillDataModel skill = new MountSkillDataModel();
                skill.SkillId = tb.Id;
                if (m_DicSkill2Mount.ContainsKey(tb.Id))
                {
                    skill.MountId = m_DicSkill2Mount[tb.Id];
                }
                DataModel.SkillList.Add(skill);
                return true;
            });
            DataModel.SkillList[0].IsSelect = true;
            DataModel.SelectSkill.CopyFrom(DataModel.SkillList[0]);  
        }

        public void RefreshData(UIInitArguments data)
        {
            if(data != null)
                DataModel.Tab = data.Tab;
        }

        private void OnClickTab(IEvent ievent)
        {
            MountClickBtn_Event e = ievent as MountClickBtn_Event;
            if (e == null)
                return;
            DataModel.Tab = e.Tab;
            if (1 == DataModel.Tab)
            {
                PlayerDataManager.Instance.NoticeData.MountSkillNotice = false;
            }
            else if (2 == DataModel.Tab)
            {
                PlayerDataManager.Instance.NoticeData.MountFeedNotice = false;
            }
        }
        #region 基类
        public INotifyPropertyChanged GetDataModel(string name)
        {

            return DataModel;
        }

        public void Close()
        {
        
        }

        public void OnShow()
        {
            UpdateMountExp(PlayerDataManager.Instance.mMountData.Id, PlayerDataManager.Instance.mMountData.Exp, true);
        }
        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "UpdateInfo")
            {
                UpdateInfo();
            }
            return null;
        }
        public FrameState State { get; set; }
        #endregion 基类 
        #region 事件

        private void OnMountEvent(IEvent eEvent)
        {
            OnMountAction_Event e = eEvent as OnMountAction_Event;
            if (e == null)
                return;
            if (m_dicFun.ContainsKey(e.type))
            {
                m_dicFun[e.type](e.param);
            }
        }

        private void OnMountMainTabEvent(IEvent iEvent)
        {
            var e = iEvent as MountMainTabEvent;
            if (e == null)
                return;
            var tab = e.Type;
            DataModel.MainTab = tab;
            var selectId = 0;
            if (DataModel.MainTab == 1)//皮肤
            {
                DataModel.MountSkinBag[0].IsSelect = true;
                selectId = DataModel.MountSkinBag[0].MountId;
            }
            else if (DataModel.MainTab == 0)
            {
                DataModel.MountBag[0].IsSelect = true;
                selectId = DataModel.MountBag[0].MountId;
            }
            EventDispatcher.Instance.DispatchEvent(new OnMountAction_Event(20, selectId));
        }

        private void OnItemChange(IEvent eEvent)
        {
            UIEvent_BagItemCountChange e = eEvent as UIEvent_BagItemCountChange;
            if (e != null)
            {
                if (e.ItemId == DataModel.NeedItem.ItemId)
                {
                    DataModel.NeedItem.Count += e.ChangeCount;
                    CheckRedNotic();
                }
                else if (FeedMaterialList.Contains(e.ItemId))
                {
                    CheckMountFeedNotice();
                }
                else if ((e.ItemId >= 22067 && e.ItemId <= 22079))
                {
                    CheckMountSkillNotice();
                }
                RefreshSkinItemInfo();
            }
        }
        private void CheckMountFeedNotice()
        {
            DataModel.FeedNoticeList.Clear();
            for (int i = 0; i < DataModel.FeedItems.Count; i++)
            {
                var item = DataModel.FeedItems[i];
                var count = PlayerDataManager.Instance.GetItemTotalCount(item.Item.ItemId).Count;
                if (item.NowCount >= item.MaxCount)
                {
                    item.FeedItemNotice = false;
                    DataModel.FeedNoticeList.Add(false);
                }
                else if (count >= 1 && IsShowRedNoticeByItemId(item.Item.ItemId))
                {
                    item.FeedItemNotice = true;
                    DataModel.FeedNoticeList.Add(true);
                }
                else
                {
                    DataModel.FeedNoticeList.Add(false);
                    item.FeedItemNotice = false;
                }
            }
            if (DataModel.FeedNoticeList.Contains(true))
            {
                PlayerDataManager.Instance.NoticeData.MountFeedNotice = true;
            }
            else
            {
                PlayerDataManager.Instance.NoticeData.MountFeedNotice = false;
            }
        }
        private void CheckMountSkillNotice()
        {
            DataModel.SkillNoticeList.Clear();
            for (int i = 0; i < DataModel.SkillList.Count; i++)
            {
                var item = DataModel.SkillList[i];
                var tbMountSkill = Table.GetMountSkill(item.SkillId);
                if (tbMountSkill == null)
                {
                    return;
                }
                int itemlevel = -1;
                if (null == PlayerDataManager.Instance.mMountData)
                {
                    return;
                }
                if (!PlayerDataManager.Instance.mMountData.Skills.TryGetValue(item.SkillId, out itemlevel))
                {
                    continue;
                }
                if (itemlevel >= tbMountSkill.MaxLevel || itemlevel < 0)
                {
                    item.SkillItemNotice = false;
                    continue;
                }
                var itemCount = PlayerDataManager.Instance.GetItemTotalCount(item.CostItem.ItemId).Count;
                if (itemCount >=item.CostItem.Count)
                {
                    item.SkillItemNotice = true;
                    DataModel.SkillNoticeList.Add(true);
                }
                else
                {
                    item.SkillItemNotice = false;
                    DataModel.SkillNoticeList.Add(false);
                }
            }
            if (DataModel.SkillNoticeList.Contains(true))
            {
                PlayerDataManager.Instance.NoticeData.MountSkillNotice = true;
                DataModel.SkillNotice = true;
            }
            else
            {
                DataModel.SkillNotice = false;
                PlayerDataManager.Instance.NoticeData.MountSkillNotice = false;
            }
        }

        private void OnModelRefresh(int param = 0)
        {
            SendModel();
        }
        private void OnSelectMount(int param = 0)
        {
            if (DataModel.SelectMount.MountId == param)
                return;
            foreach (var cell in DataModel.MountBag)
            {
                if (true == (cell.IsSelect = cell.MountId == param))
                {
                    DataModel.SelectMount.CopyFrom(cell);
                }
            }
            foreach (var cell in DataModel.MountSkinBag)
            {
                if (true == (cell.IsSelect = cell.MountId == param))
                {
                    DataModel.SelectMount.CopyFrom(cell);
                    RefreshSkinAttr();
                    GetCurSkinCountDownStr(DataModel.SelectMount.MountId);
                }
            }
            SendModel();
        }

        #region 坐骑皮肤相关

        /// <summary>
        /// 获取当前限时皮肤倒计时
        /// </summary>
        private void GetCurSkinCountDownStr(int mountId)
        {
            if (m_DicTimeLimitedSkins.ContainsKey(mountId))
            {
                var invalidDate = m_DicTimeLimitedSkins[mountId];
                DataModel.SkinCountDownStr = string.Format(GameUtils.GetDictionaryText(274079),
                    GameUtils.GetAllTimeDiffString(invalidDate, true));
            }
        }

        /// <summary>
        /// 限时皮肤计时
        /// </summary>
        private void RefreshAllTimeLimitedSkin()
        {
            //Debug.LogWarning(Game.Instance.ServerTime.ToString("yyyy/MM/dd HH:mm:ss"));
            if (m_DicTimeLimitedSkins.Count == 0)////没有限时坐骑皮肤的时候删除计时器
            {
                if (null != RefreshTimer)
                {
                    TimeManager.Instance.DeleteTrigger(RefreshTimer);
                    RefreshTimer = null;
                }
            }
            else
            {
                GetCurSkinCountDownStr(DataModel.SelectMount.MountId);
                foreach (var limitedSkin in m_DicTimeLimitedSkins)
                {
                    if (Game.Instance.ServerTime > limitedSkin.Value)//有限时坐骑皮肤到期，删除计时器
                    {
                        if (null != RefreshTimer)
                        {
                            TimeManager.Instance.DeleteTrigger(RefreshTimer);
                            RefreshTimer = null;
                        }
                        PlayerDataManager.Instance.ApplyMount();//请求坐骑信息，服务器会处理具体删除逻辑
                        //Debug.LogError("ApplyMount : " + Game.Instance.ServerTime.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 获取限时坐骑信息
        /// </summary>
        private void GetAllTimeLimitedSkinInfo()
        {
            m_DicTimeLimitedSkins.Clear();
            var skinItems = PlayerDataManager.Instance.mMountData.Special;
            if (skinItems.Count == 0)//没有坐骑皮肤直接返回
                return;
            foreach (var item in skinItems)
            {
                var mountId = item.Key;
                var tb = Table.GetMount(mountId);
                if (tb == null)
                    continue;
                if (tb.IsPermanent == 0)
                {
                    var invalidDate = DataTimeExtension.EpochStart.AddSeconds(item.Value);
                    m_DicTimeLimitedSkins.Add(mountId, invalidDate);
                }
            }
            if (m_DicTimeLimitedSkins.Count == 0)//如果没有限时坐骑皮肤不开启计时
                return;
            if (null != RefreshTimer)
            {
                TimeManager.Instance.DeleteTrigger(RefreshTimer);
                RefreshTimer = null;
            }
            RefreshTimer = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime,
                RefreshAllTimeLimitedSkin,
                1000);
        }

        /// <summary>
        /// 刷新坐骑皮肤属性
        /// </summary>
        private void RefreshSkinAttr()
        {
            var tb = Table.GetMount(DataModel.SelectMount.MountId);
            if (tb == null)
                return;
            DataModel.SkinAttrList.Clear();
            for (int i = 0; i < tb.Attr.Length && i < tb.Value.Length; i++)
            {
                if (tb.Attr[i] > 0)
                {
                    var att = new AttributeChangeDataModel();
                    att.Type = tb.Attr[i];
                    att.Value = tb.Value[i];
                    DataModel.SkinAttrList.Add(att);
                }
            }
        }

        /// <summary>
        /// 坐骑皮肤添加战力
        /// </summary>
        private void SkinAddFight()
        {
            Dictionary<int, int> skinAdd = new Dictionary<int, int>();
            var skinData = PlayerDataManager.Instance.mMountData.Special;
            foreach (var skin in skinData)
            {
                var skinMountId = skin.Key;
                var tbSkin = Table.GetMount(skinMountId);
                if (tbSkin != null)
                {
                    for (int i = 0; i < tbSkin.Attr.Length && i < tbSkin.Value.Length; i++)
                    {
                        if (tbSkin.Attr[i] > 0)
                        {
                            skinAdd.modifyValue(tbSkin.Attr[i], tbSkin.Value[i]);
                        }
                    }
                }
            }
            DataModel.SkinFightPoint = PlayerDataManager.Instance.GetElfAttrFightPoint(skinAdd);
        }

        private void OnAddMountSkin(int param = 0)
        {
            NetManager.Instance.StartCoroutine(AddMountSkinCoroutine());
        }

        private IEnumerator AddMountSkinCoroutine(float delay = 0.0f)
        {
            var msg = NetManager.Instance.AddMountSkin(DataModel.SelectMount.MountId);
            yield return msg.SendAndWaitUntilDone();
            if (msg.ErrorCode == (int)ErrorCodes.OK)
            {
                //TO BE ADDED
            }
            else
            {
                //TO BE ADDED
            }
        }

        /// <summary>
        /// 更新坐骑皮肤信息
        /// </summary>
        private void UpdataMountSkinInfo(bool RefreshModel = false)
        {
            DataModel.Step = PlayerDataManager.Instance.mMountData.Step;
            DataModel.Exp = PlayerDataManager.Instance.mMountData.Exp;
            DataModel.Level = PlayerDataManager.Instance.mMountData.Level;
            DataModel.RideId = PlayerDataManager.Instance.mMountData.Ride;
            var specialData = PlayerDataManager.Instance.mMountData.Special;
            var skinList = DataModel.MountSkinBag.ToList();
            skinList.Sort((a, b) =>
            {
                if (specialData.ContainsKey(a.MountId))
                {
                    if (specialData.ContainsKey(b.MountId))
                    {
                        if (a.MountId > b.MountId)
                        {
                            return -1;
                        }
                        else if (a.MountId < b.MountId)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    if (specialData.ContainsKey(b.MountId))
                    {
                        return 1;
                    }
                    else
                    {
                        if (a.MountId > b.MountId)
                        {
                            return 1;
                        }
                        else if (a.MountId < b.MountId)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            });
            DataModel.MountSkinBag.Clear();
            for (int i = 0; i < skinList.Count; i++)
            {
                DataModel.MountSkinBag.Add(skinList[i]);
            }

            RefreshSkinItemInfo();
            RefreshSkinAttr();
            SkinAddFight();
            GetAllTimeLimitedSkinInfo();
            if (RefreshModel)
            {
                SendModel();
                EventDispatcher.Instance.DispatchEvent(new MountEffect_Event(0));
            }
        }

        /// <summary>
        /// 刷新坐骑皮肤状态信息
        /// </summary>
        private void RefreshSkinItemInfo()
        {
            for (int i = 0; i < DataModel.MountSkinBag.Count; i++)
            {
                var skin = DataModel.MountSkinBag[i];
                skin.IsGrey = PlayerDataManager.Instance.mMountData.Special.ContainsKey(skin.MountId) ? 0 : 1;
                if (skin.IsGrey == 0)//已激活
                {
                    skin.State = 2;
                }
                else
                {
                    if (IsSkinCanActive(skin.MountId))
                    {
                        skin.State = 1;
                    }
                    else
                    {
                        skin.State = 0;
                    }
                }
                if (skin.MountId == DataModel.SelectMount.MountId)
                {
                    DataModel.SelectMount.CopyFrom(skin);
                }
            }
        }

        #endregion

        private void OnClickMountUp(int param = 0)
        {
            //var m_objTimerTrigger = TimeManager.Instance.CreateTrigger(Game.Instance.ServerTime.AddSeconds(2f), () =>
            //{
            //    int a = 0;
            //    a ++;
            //},3);
            NetManager.Instance.StartCoroutine(MountUpCoroutine());
        }

        private void OnClickAutoUp(int param = 0)
        {
            if (DataModel.IsAuto == param)
                return;
            DataModel.IsAuto = param;
            OnMountUpAuto();
        }
        private void OnClickMountRide(int param = 0)
        {

            NetManager.Instance.StartCoroutine(MountRideCoroutine());
        }
        private void OnSelectMountSkill(int param = 0)
        {
            foreach (var skill in DataModel.SkillList)
            {
                if (true == (skill.IsSelect = skill.SkillId == param))
                {
                    DataModel.SelectSkill.CopyFrom(skill);
                }
            }
        }
        private void OnMountSkill(int skillId)
        {
            skillId = DataModel.SelectSkill.SkillId;
            int lv = -1;
            if (PlayerDataManager.Instance.mMountData.Skills.TryGetValue(skillId, out lv) == false)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001251));
                return;
            }
            var tb = Table.GetMountSkill(skillId);
            if (tb == null)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000114));
                return;
            }
            if (lv >= tb.MaxLevel)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200002215));
                return;
            }
            var cost = Table.GetConsumArray(tb.CostList[lv]);
            if (cost == null)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200002216));
                return;
            }

            Dictionary<int, int> dic = new Dictionary<int, int>();
            dic.Add(cost.ItemId[0], cost.ItemCount[0]);
            if (!GameUtils.CheckEnoughItems(dic, true))
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(274046), GameUtils.MakeItemString(cost.ItemId[0]))));
                return;
            }
     
            NetManager.Instance.StartCoroutine(MountSkillCoroutine(DataModel.SelectSkill.SkillId));
        }
        private void OnMountFeed(int param = 0)
        {
            var tbFeed = Table.GetMountFeed(param);
            if (tbFeed == null)
                return;

            int cur = 0;
            PlayerDataManager.Instance.mMountData.Feeds.TryGetValue(param, out cur);
            if (cur >= tbFeed.MaxCount)
            {
                var tbItem = Table.GetItemBase(param);
                if (tbItem != null)
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(274048), tbItem.Name)));
                return;
            }
            var tb = Table.GetMount(tbFeed.UseLimit);
            if (tb == null)
                return;
            if (tb.Step*100 + tb.Level >
                PlayerDataManager.Instance.mMountData.Step*100 + PlayerDataManager.Instance.mMountData.Level)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(274031), tb.Name, tb.Step,tb.Level)));
                return;
            }
            NetManager.Instance.StartCoroutine(MountFeedCoroutine(param));
        }
        #endregion 事件
        #region 协议


        private IEnumerator MountRideCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.RideMount(DataModel.SelectMount.MountId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        PlayerDataManager.Instance.mMountData.Ride = DataModel.SelectMount.MountId;
                        UpdateMountInfo(true);
                        UpdataMountSkinInfo(true);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    }
                }

            }
            yield break;
        }

        private void OnBagItemCountChange(IEvent ievent)
        {
            var e = ievent as BagItemCountChangeEvent;
            if(e != null && e.ItemChanges.ContainsKey(needItem))
            {
                CheckRedNotic();
            }
        }
        private void OnMountUpAuto()
        {
            if (m_TrainCoroutine != null)
            {
                NetManager.Instance.StopCoroutine(m_TrainCoroutine);
            }
            if (DataModel.IsAuto == 1)
            {
                m_TrainCoroutine = NetManager.Instance.StartCoroutine(MountUpCoroutine());
            }
        }

        private IEnumerator MountUpCoroutine( float delay = 0.0f)
        {
            if (DataModel.IsMaxLevel == 1)
            {
                DataModel.IsAuto = 0;
                PlayerDataManager.Instance.NoticeData.MountTrainNotice = 0;
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(274003));
                yield break;
            }
            var tbMount = Table.GetMount(PlayerDataManager.Instance.mMountData.Id);
            if (tbMount == null)
            {
                yield break;
            }

            Dictionary<int, int> dic = new Dictionary<int, int>();
            dic.Add(tbMount.NeedItem,1);
            if (!GameUtils.CheckEnoughItems(dic, true))
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(210101));
                DataModel.IsAuto = 0;
                yield break;
            }            
       
            yield return new WaitForSeconds(delay);
            if (DataModel.IsAuto == 0 && delay > 0.001f)
            {
                yield break; 
            }

            var msg = NetManager.Instance.MountUp(0);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    var data = PlayerDataManager.Instance.mMountData;
                    var tb = Table.GetMount(data.Id);
                    if (tb != null)
                    {
                        UpdateMountExp(msg.Response.Items[0], msg.Response.Items[3]);
                        bool bChangeModel = data.Step < msg.Response.Items[1];
                        data.Id = msg.Response.Items[0];
                        data.Step = msg.Response.Items[1];
                        data.Level = msg.Response.Items[2];
                        data.Exp = msg.Response.Items[3];
                        data.Ride = msg.Response.Items[4];
                        UpdateMountInfo(bChangeModel);
                        var tbNext = Table.GetMount(data.Id);
                        if (tbNext.SkillId > 0 && false == data.Skills.ContainsKey(tbNext.SkillId))
                        {
                            data.Skills.Add(tbNext.SkillId, 0);
                            UpdateMountSkillInfo();
                        }
                    }
                }
                else
                {
                    if (msg.ErrorCode == (int)ErrorCodes.ItemNotEnough)
                    {
                        GameUtils.ShowQuickBuy(dic);
                    }
                    else if (msg.ErrorCode == (int)ErrorCodes.Error_Mount_MAX_LEVEL)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(274003));
                    }
                    else
                        UIManager.Instance.ShowNetError(msg.ErrorCode);
                    DataModel.IsAuto = 0;
                }
            }

        }
        private IEnumerator MountSkillCoroutine(int SkillId)
        {
            var msg = NetManager.Instance.MountSkill(SkillId);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    PlayerDataManager.Instance.mMountData.Skills.modifyValue(SkillId, 1);
                    UpdateMountSkillInfo();
                    EventDispatcher.Instance.DispatchEvent(new MountEffect_Event(1));
                }
                else
                {
                    UIManager.Instance.ShowNetError(msg.ErrorCode);
                }
            }
        }
        private IEnumerator MountFeedCoroutine(int ItemId)
        {
            //if (PlayerDataManager.Instance.GetItemCount(ItemId) <= 0)
            //{
            //    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200000005));
            //    yield break;   
            //}
            Dictionary<int,int> cost = new Dictionary<int, int>();
            cost.Add(ItemId,1);
            if (!GameUtils.CheckEnoughItems(cost, true))
            {
            
                string str = string.Format(GameUtils.GetDictionaryText(274045), GameUtils.MakeItemString(ItemId));
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(str));
                yield break;   
            }


            var msg = NetManager.Instance.MountFeed(ItemId);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {
                    var tb = Table.GetMountFeed(ItemId);
                    for (int i = 0; i < tb.Attr.Length; i++)
                    {
                        if (tb.Attr[i] > 0)
                            PlayerDataManager.Instance.mMountData.Attrs.modifyValue(tb.Attr[i], tb.Value[i]);
                    }
                    PlayerDataManager.Instance.mMountData.Feeds.modifyValue(ItemId, 1);
                    UpdateMountFeedInfo(ItemId);
                    EventDispatcher.Instance.DispatchEvent(new MountEffect_Event(2,ItemId));
                }
                else
                {
                    UIManager.Instance.ShowNetError(msg.ErrorCode);
                }
            }
        }
        #endregion 协议


        private bool IsShowRedNoticeByItemId(int param = 0)
        {
            var tbFeed = Table.GetMountFeed(param);
            if (tbFeed == null)
                return false;
             var tb = Table.GetMount(tbFeed.UseLimit);
            if (tb == null)
                return false;
            if (tb.Step*100 + tb.Level >
                PlayerDataManager.Instance.mMountData.Step*100 + PlayerDataManager.Instance.mMountData.Level)
            {
                return false;
            }
            return true;
        }

        private void SendModel()
        {
            EventDispatcher.Instance.DispatchEvent(new MountRefreshModel_Event(DataModel.SelectMount.ItemId));
        }

        private void UpdateInfo()
        {
            UpdateMountInfo();
            UpdateMountFeedInfo();
            UpdateMountSkillInfo();
            UpdateMountExp(PlayerDataManager.Instance.mMountData.Id, PlayerDataManager.Instance.mMountData.Exp,true);
            UpdataMountSkinInfo();
            CheckMountSkillNotice();
            CheckMountFeedNotice();
        }

        private void UpdateMountExp(int id,int exp,bool init = false)
        {
            var tbMount = Table.GetMount(id);
            if (tbMount == null)
                return;
            if (init == true)
            {
                DataModel.TrainSlider.MaxValues = new List<int> { tbMount.NeedExp };
                DataModel.TrainSlider.BeginValue = exp / (float)tbMount.NeedExp;
                DataModel.TrainSlider.TargetValue = DataModel.TrainSlider.BeginValue;
            }
            else
            {
                var _oldSliderValue = DataModel.TrainSlider.TargetValue;
                if (PlayerDataManager.Instance.mMountData.Id != id)
                {//升级
                    var tbOld = Table.GetMount(PlayerDataManager.Instance.mMountData.Id);
                    if (tbOld == null)
                        return;
                    List<int> _maxList = new List<int>();
                
                    while (tbOld.Id != tbMount.Id)
                    {
                        _maxList.Add(tbOld.NeedExp);
                        tbOld = Table.GetMount(tbOld.NextId);
                        if (tbOld == null || tbOld.IsOpen == 0 || tbOld.Special == 1)
                        {
                            break;
                        }
                    }
                    _maxList.Add(tbMount.NeedExp);
                

                    DataModel.TrainSlider.MaxValues = _maxList;
                    DataModel.TrainSlider.TargetValue = exp / (float)tbMount.NeedExp +
                                                        (_maxList.Count - 1);

                    ObjManager.Instance.MyPlayer.OnDisMount();
                }
                else
                {
                    DataModel.TrainSlider.MaxValues = new List<int>(tbMount.NeedExp);
                    DataModel.TrainSlider.TargetValue =(float)(exp / (float)tbMount.NeedExp);
                }
                if (DataModel.IsAuto == 1)
                {
                    var _dif = DataModel.TrainSlider.TargetValue -_oldSliderValue;
                    var _costTime = _dif + 0.25f;
                    m_TrainCoroutine = NetManager.Instance.StartCoroutine(MountUpCoroutine(_costTime));
                }
            }
        }

        private bool IsSkinCanActive(int skinId)
        {
            var tb = Table.GetMount(skinId);
            if (tb == null)
            {
                return false;
            }
            var itemCount = PlayerDataManager.Instance.GetItemCount(tb.NeedItem);
            if (itemCount >= tb.GetExp)
            {
                return true;
            }
            return false;
        }

        private void UpdateMountInfo(bool bSendModel = false)
        {
            bool bChangeSelect = DataModel.Step != PlayerDataManager.Instance.mMountData.Step;
            DataModel.Step = PlayerDataManager.Instance.mMountData.Step;
            DataModel.Exp = PlayerDataManager.Instance.mMountData.Exp;
            DataModel.Level = PlayerDataManager.Instance.mMountData.Level;
            DataModel.RideId = PlayerDataManager.Instance.mMountData.Ride;
            int selectIndex = 0;
            int topStep = 0;
            int topLevel = 0;
            for (int i = 0; i < DataModel.MountBag.Count; i++)
            {
                var m = DataModel.MountBag[i];
                m.IsGrey = PlayerDataManager.Instance.mMountData.Step >= m.Step ? 0 : 1;
                if(bChangeSelect == true)
                    m.IsSelect = false;

                if (m.IsGrey == 0 && bChangeSelect)
                {
                    if (m.MountId == DataModel.RideId)
                    {
                        selectIndex = i;
                        topStep = 100;
                    }
                    else if (topStep * 100 + topLevel < m.Step * 100 + m.Level)
                    {
                        selectIndex = i;
                        topStep = m.Step;
                        topLevel = m.Level;
                    }

                }
            }
            if (bChangeSelect)
            {
                DataModel.MountBag[selectIndex].IsSelect = true;
                DataModel.SelectMount.CopyFrom(DataModel.MountBag[selectIndex]);
            }

       
            DataModel.NeedItem = new ItemIdDataModel();

            var tb = Table.GetMount(PlayerDataManager.Instance.mMountData.Id);
            if (tb == null)
                return;
        
            DataModel.MaxExp = tb.NeedExp;
            {//属性浮动
                DataModel.AttrList.Clear();
                Dictionary<int, int> dicAttr = new Dictionary<int, int>();
                for (int i = 0; i < tb.Attr.Length && i < tb.Value.Length; i++)
                {
                    if (tb.Attr[i] > 0)
                    {
                        var att = new AttributeChangeDataModel();
                        att.Type = tb.Attr[i];
                        att.Value = tb.Value[i];
                        DataModel.AttrList.Add(att);
                        dicAttr.modifyValue(att.Type,att.Value);
                    }
                }
                DataModel.FightPoint = PlayerDataManager.Instance.GetElfAttrFightPoint(dicAttr);
                var tbNext = Table.GetMount(tb.NextId);
                if (tbNext != null&&tbNext.IsOpen == 1)
                {
                    dicAttr.Clear();
                    for (int i = 0; i < tbNext.Attr.Length && i < tbNext.Value.Length; i++)
                    {
                        if (tbNext.Attr[i] > 0 && tbNext.Value[i] > 0)
                        {
                            dicAttr.modifyValue(tbNext.Attr[i], tbNext.Value[i]);    
                        }
                    }
                    foreach (var attr in DataModel.AttrList)
                    {
                        IControllerBase control = UIManager.GetInstance().GetController(UIConfig.WingUI);
                        if (dicAttr.ContainsKey(attr.Type))
                        {
                            attr.Change = dicAttr[attr.Type] - attr.Value;
                            attr.Change = (int)control.CallFromOtherClass("AmendPropertiesValue", new[] { (object)attr.Type, (object)attr.Change });
                        }
                        attr.Value = (int)control.CallFromOtherClass("AmendPropertiesValue", new[] { (object)attr.Type, (object)attr.Change });
                    }
                }        
            }
            if(tb.NeedItem>0)
            {
                needItem = tb.NeedItem;
                needCount = (int)Math.Ceiling((double)(tb.NeedExp - DataModel.Exp) / (double)tb.GetExp);
                CheckRedNotic();
            }


            DataModel.NeedItem.ItemId = tb.NeedItem;
            DataModel.NeedItem.Count = PlayerDataManager.Instance.GetItemCount(tb.NeedItem);
            if (bSendModel)
            {
                SendModel();
                EventDispatcher.Instance.DispatchEvent(new MountEffect_Event(0));
            }


            {//init next string
                int maxLv = 0;
                int maxStep = 0;
                Table.ForeachMount(tbMount =>
                {
                    if (tbMount.Step > maxStep && tbMount.IsOpen == 1 && tbMount.Special == 0)
                        maxStep = tbMount.Step;
                    if (tbMount.Step != DataModel.Step)
                        return true;
                    if (tbMount.Level > maxLv)
                        maxLv = tbMount.Level;
                    return true;
                });
                if (maxStep > DataModel.Step)
                {//还可以进阶
                    int dis = maxLv - DataModel.Level + 1;
                    DataModel.strNextStep = string.Format(GameUtils.GetDictionaryText(274001), dis);
                }
                else
                {//以达到最大阶
                    DataModel.strNextStep = GameUtils.GetDictionaryText(274002);
                    DataModel.IsMaxLevel = maxLv == DataModel.Level ? 1 : 0;
                }

            }
        }

        private void CheckRedNotic()
        {
//        m_Cost2Count
            var count = PlayerDataManager.Instance.GetItemCount(needItem);
            PlayerDataManager.Instance.NoticeData.MountTrainNotice = count >= needCount ? 1 : 0;                  
            if (DataModel.IsMaxLevel == 1)
            {
                PlayerDataManager.Instance.NoticeData.MountTrainNotice = 0;
            }
        }
        private void UpdateMountFeedInfo(int id = 0)
        {
            var data = PlayerDataManager.Instance.mMountData;
            Dictionary<int,int> add = new Dictionary<int, int>();
            foreach (var item in DataModel.FeedItems)
            {
                var tb = Table.GetMountFeed(item.Item.ItemId);
                if (tb == null)
                    continue;

                item.MaxCount = tb.MaxCount;
                item.NeedItem = 1;
                int n = 0;
                item.AttrList.Clear();
                var tbMount = Table.GetMount(tb.UseLimit);
                if (tbMount == null)
                {
                    continue;
                }
                PlayerDataManager.Instance.mMountData.Feeds.TryGetValue(item.Item.ItemId, out n);
                if (data.Step*100 + data.Level >= tbMount.Step*100 + tbMount.Level)
                {
                    item.NowCount = n;
                    for (int i = 0; i < tb.Attr.Length && i < tb.Value.Length; i++)
                    {
                        if (tb.Attr[i] > 0)
                        {
                            var att = new StringDataModel();
                            att.str = string.Format("{0}: +{1}", ExpressionHelper.AttrName[tb.Attr[i]], tb.Value[i] * item.NowCount);
                            item.AttrList.Add(att);
                            add.modifyValue(tb.Attr[i], tb.Value[i]*item.NowCount);
                        }
                    }
                }
                else
                {
                    var att = new StringDataModel();
                    att.str = string.Format(GameUtils.GetDictionaryText(274031), tbMount.Name, tbMount.Step, tbMount.Level);
                    item.AttrList.Add(att);              
                }    
            }
            foreach (var att in DataModel.AttrList)
            {
                if (add.ContainsKey(att.Type))
                {
                    att.AddValue = add[att.Type];
                }
            }
            DataModel.AddFight = PlayerDataManager.Instance.GetElfAttrFightPoint(add);

        }

        private void UpdateMountSkillInfo()
        {
            var data = PlayerDataManager.Instance.mMountData;
            foreach (var item in DataModel.SkillList)
            {
                var tb = Table.GetMountSkill(item.SkillId);
                if (tb == null)
                    continue;
                if (data.Skills.ContainsKey(item.SkillId))
                {
                    item.BuffLevel = data.Skills[item.SkillId];
                    item.IsGrey = 0;
                }
                else
                {
                    item.IsGrey = 1;
                    item.BuffLevel = 0;                
                }

                if (item.BuffLevel < tb.CostList.Count)
                {
                    var tbCost = Table.GetConsumArray(tb.CostList[item.BuffLevel]);
                    if (tbCost != null)
                    {
                        item.CostItem.ItemId = tbCost.ItemId[0];
                        item.CostItem.Count = tbCost.ItemCount[0];
                    }
                }
                else
                {
                    item.CostItem.ItemId = 0;
                    item.CostItem.Count = 0;
                }

                if (item.IsSelect == true)
                {
                    DataModel.SelectSkill.CopyFrom(item);
                }
            }
        }
    }
}