/********************************************************************************* 

                         Scorpion



  *FileName:FightAllianceFrameCtrler

  *Version:1.0

  *Date:2017-07-24

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using Shared;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class FightAllianceFrameCtrler : IControllerBase
    {

        #region 静态变量

        private readonly Dictionary<int, List<StoreRecord>> s_dicCacheDic = new Dictionary<int, List<StoreRecord>>();
        private readonly Dictionary<int, int> s_dicCacheDicLP = new Dictionary<int, int>();
        private readonly List<BattleUnionTeamSimpleDataModel> s_listCatchOhterUnion = new List<BattleUnionTeamSimpleDataModel>();
        private readonly Dictionary<int, int> s_dicOtherUnionDict = new Dictionary<int, int>(); //其他战盟id ，inde 字典
        private readonly int s_iTitleId = 5000;
        private readonly int s_iFuBenId = 9000;
        private readonly int donateItemsCapacity = 100;//可捐赠列表容量

        #endregion

        #region 成员变量

        private const int m_iINTERVEL_TIME = 5;
        private const int m_iREFLESH_COUNT = 6;
        private GuildRecord m_GuildRecord;
        private List<int> m_listBuffIdInit = new List<int> { 101, 201, 301, 401 }; //默认0级buff索引的下一级buffid
        private int m_BuffSelected; //buff选择id
        private ulong m_ulongCharacterID; //被操作玩家id
        private string m_strInputStr = string.Empty;
        private ulong m_ulongMemberId; //成员id
        //private GuildRecord m_GuildRecord;
        private List<DateTime> m_listRefleshTime = new List<DateTime>();
        // 0 战盟信息刷新  //1战盟捐献刷新    //2 申请列表  //3 简单信息  //4其他战盟  //5攻城战
        private RefleshType m_RefleshType = RefleshType.UnionData; //根据时间是否刷新战盟相关界面
        private int m_iOtherUnionSelectIndex = -1; //其他战盟选择index 
        private string m_strOnline = ""; //dictinory 字符串
        private UnionIDState m_UionState = UnionIDState.OtherJoin; //union加入状态
        private int m_iUnionLevelChanged; //战盟等级是否变化
        // public int LevelChange = 0; // 判断等级是否变化  
        private int m_UnionMaxLevel; //战盟最大可以达到的等级
        #region 攻城战

        //每次竞价钱数
        private int m_iaddPerCount = 10000;
        private int m_ilimitMin;
        private Coroutine m_buttonPress;
        private object m_attackCityStateTrigger;
        private object m_attackCityReadyTrigger;
        #endregion
        private ulong m_ulongModelGuid;

        private int curItemIndex;//战盟仓库当前展示ItemIndex
        private Dictionary<string, BattleUnionDepotClearUpDataModel> clearUpInfoDic = new Dictionary<string, BattleUnionDepotClearUpDataModel>();//战盟仓库清理信息

        #endregion

        #region 构造函数

        public FightAllianceFrameCtrler()
        {
            //按钮事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionBtnCreateUnion.EVENT_TYPE, OnButtonCreationAllianceEvent); //创建战盟
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionBtnPassApply.EVENT_TYPE, OnButtonClicApplyForEvent); //批量同意申请
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionCharacterClick.EVENT_TYPE, OnClicCharacterEvent); //人物点击返回事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionBtnDonation.EVENT_TYPE, OnButtonEndowmentEvent); // 捐赠按钮组
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionDonationItemClick.EVENT_TYPE, OnClicEndowmentPropEvent);
            //捐赠的物品点击
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionBuffUpShow.EVENT_TYPE, OnBuffIconEvent);
            //buff按钮点击事件，显示buff页面信息
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionBossClick.EVENT_TYPE, OnClicBossEvent); //bossList点击
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionOtherListClick.EVENT_TYPE, OnClicAllianceAnotherListEvent);
            //其他战盟列表点击事件
            //其他消息事件
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitialEvent); //初始化ExData
            EventDispatcher.Instance.AddEventListener(BattleUnionExdataUpdate.EVENT_TYPE, OnRenewalExDataEvent); //ExData变更相应
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionCommunication.EVENT_TYPE, OnCommunicateAskingEvent); //菜单点击事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionJoinReply.EVENT_TYPE, AllianceSyncMsgEvent); //菜单点击事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionGetCharacterID.EVENT_TYPE, OnSettingoperatedCharacterIDEvent);
            //操作列表请求战盟
            EventDispatcher.Instance.AddEventListener(UIEvent_BattleShopCellClick.EVENT_TYPE, OnClicBuyPropEvent); // 商品购买
            //    EventDispatcher.Instance.AddEventListener(BattleUnionExdataUpdate.EVENT_TYPE, UpdateExData);  // 同意玩家申请加入战盟
            EventDispatcher.Instance.AddEventListener(UIEvent_BattleBtnAutoAccept.EVENT_TYPE, OnButtonSetAutoAcceptedEvent); // 战盟自动申请成功
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionTabPageClick.EVENT_TYPE, OnClicTabPageEvent); // tab点击事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionTabPageClick2.EVENT_TYPE, OnClicTabPageTwoEvent); // tab点击事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionDonationItem.EVENT_TYPE, OnButtonEndowmentPropEvent); // 捐献物品点击事件
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionSyncDataChange.EVENT_TYPE, OnSynchronizeInfoChangeEvent); // 异步数据变化同步
            EventDispatcher.Instance.AddEventListener(UIEvent_UnionBattlePageCLick.EVENT_TYPE, OnClicAttackPageEvent); // tab点击事件

            EventDispatcher.Instance.AddEventListener(UIEvent_UnionOperation.EVENT_TYPE, OnAllianceOperateEvent); // 功能操作
            EventDispatcher.Instance.AddEventListener(BattleUnionCountChange.EVENT_TYPE, OnBingNumChangedEvent); // 竞价数量变化
            EventDispatcher.Instance.AddEventListener(BattleUnionSyncOccupantChange.EVENT_TYPE, OnSyncCastellanChangeEvent);
            // 城主信息变化同步
            EventDispatcher.Instance.AddEventListener(BattleUnionSyncChallengerDataChange.EVENT_TYPE,
                OnSyncDefierInfoChangedEvent); // 攻城方信息变化同步

            EventDispatcher.Instance.AddEventListener(DonateItemClickEvent.EVENT_TYPE, OnDonateItemClick);//仓库捐赠列表点击
            EventDispatcher.Instance.AddEventListener(DepotItemClickEvent.EVENT_TYPE, OnDepotItemClick);//仓库取出列表点击
            EventDispatcher.Instance.AddEventListener(BattleUnionDepot_Donate.EVENT_TYPE, OnDonateBtnClick);//仓库捐赠
            EventDispatcher.Instance.AddEventListener(BattleUnionDepot_TakeOut.EVENT_TYPE, OnTakeOutBtnClick);//仓库取出
            EventDispatcher.Instance.AddEventListener(BattleUnionDepot_Remove.EVENT_TYPE, OnRemoveBtnClick);//仓库单个清理
            EventDispatcher.Instance.AddEventListener(BattleUnionDepotOperation.EVENT_TYPE, DepotOperationEvent);//仓库操作
            EventDispatcher.Instance.AddEventListener(UIEvent_BagChange.EVENT_TYPE, OnBagChangeEvent);//背包变化事件
            EventDispatcher.Instance.AddEventListener(BattleUnionDepotCleanUpToggleEvent.EVENT_TYPE, OnDepotToggleChange);//仓库清理Toggle事件
            EventDispatcher.Instance.AddEventListener(BattleUnionOperationEvent.EVENT_TYPE, OnApplyOperationEvent);
            EventDispatcher.Instance.AddEventListener(PlayerExitAllianceMsgEvent.EVENT_TYPE, OnPlayerExitEvent);
        
            CleanUp();
        }

        #endregion

        #region 枚举函数

        //捐赠物品的状态
        private enum ItemState
        {
            Wait = 0, //等待刷新
            Normal = 1 //有任务
        }

        //战盟信息刷新类型
        private enum RefleshType
        {
            UnionData = 0, //战盟信息刷新 
            DonationData = 1, //战盟捐献刷新
            ApplyData = 2, //申请列表
            MemberDetailData = 3, //成员详细信息
            UnionDataSimple = 4, //战盟信息简单信息 
            OtherUnion = 5, //其他战盟
            AttackData = 6, //攻城战信息
            UnionDepot = 7 //战盟仓库
        }

        //Page页
        private enum TabPage
        {
            PageInfo = 0, //信息
            PageMember = 1, //信息
            PageBuild = 2, //建设
            PageLevel = 3, //升级
            PageShop = 4, //Boss
            PageCity = 5, //城市
            PageOther = 6, //其他战盟
            //   PageQuit = 6,        //退出
            PageDepot = 7 //战盟仓库
        }

        //战盟id变化时产生的原因
        private enum UnionIDState
        {
            CreateJoin = 0, //创建战盟
            OtherJoin = 1 //被邀请，被审批进入战盟
        }

        #endregion

        #region 属性函数

        private BattleUnionBossDataModel m_Boss { get; set; }
        private BattleUnionBuffDataModel m_Buff { get; set; }
        private BattleUnionBuildDataModel m_Build { get; set; }
        private BattleUnionInfoDataModel m_Info { get; set; }

        private Dictionary<ulong, CharacterBaseInfoDataModel> m_dicUnionMembers
        {
            get { return PlayerDataManager.Instance.mUnionMembers; }
            set { PlayerDataManager.Instance.mUnionMembers = value; }
        }

        private BattleUnionOtherUnionDataModel m_OtherUnion { get; set; }
        private BattleUnionShopDataModel m_Shop { get; set; }
        private BattleUnionAttackCityDataModel m_AttackCity { get; set; }
        private BattleUnionDataModel m_BattleData
        {
            get { return PlayerDataManager.Instance.BattleUnionDataModel; }
            set { PlayerDataManager.Instance.BattleUnionDataModel = value; }
        }

        private BattleUnionDepotDataModel m_DepotDataModel { get; set; }

        #endregion

        #region 固有函数

        public INotifyPropertyChanged GetDataModel(string name)
        {
            switch (name)
            {
                case "Info":
                {
                    return m_Info;
                }
                    break;
                case "Build":
                {
                    return m_Build;
                }
                    break;
                case "Buff":
                {
                    return m_Buff;
                }
                    break;
                case "Boss":
                {
                    return m_Boss;
                }
                    break;
                case "Shop":
                {
                    return m_Shop;
                }
                    break;
                case "OtherUnion":
                {
                    return m_OtherUnion;
                }
                    break;
                case "BattleData":
                {
                    return m_BattleData;
                }
                    break;
                case "AttackCity":
                {
                    return m_AttackCity;
                }
                    break;
                case "Depot":
                {
                    return m_DepotDataModel;
                }
                    break;
            }
            return null;
        }
        public void CleanUp()
        {
            if (m_BattleData != null)
            {
                m_BattleData.CheckBox.PropertyChanged -= OnToggleSet;
            }

            m_BattleData = new BattleUnionDataModel();
            m_Info = new BattleUnionInfoDataModel();
            m_Build = new BattleUnionBuildDataModel();
            m_Buff = new BattleUnionBuffDataModel();
            m_Boss = new BattleUnionBossDataModel();
            m_Shop = new BattleUnionShopDataModel();
            m_OtherUnion = new BattleUnionOtherUnionDataModel();
            m_AttackCity = new BattleUnionAttackCityDataModel();

            m_DepotDataModel = new BattleUnionDepotDataModel();

            var _tbClient = Table.GetClientConfig(241);
            var _money = int.Parse(_tbClient.Value) / 10000;
            m_Build.MaxDonation = int.Parse(Table.GetClientConfig(280).Value);
            m_BattleData.CreateUIStr1 = string.Format(GameUtils.GetDictionaryText(220967), _money);

            _tbClient = Table.GetClientConfig(242);
            m_BattleData.CreateUIStr2 = string.Format(GameUtils.GetDictionaryText(220968), _tbClient.Value);


            m_BattleData.CheckBox.PropertyChanged += OnToggleSet;

            m_strInputStr = GameUtils.GetDictionaryText(100000996);
            m_BattleData.CreateName = m_strInputStr;
            s_dicCacheDicLP.Clear();
            s_dicCacheDic.Clear();
            m_UnionMaxLevel = 0;
            Table.ForeachGuild(record =>
            {
                var _value = record.Id;
                if (!s_dicCacheDicLP.ContainsKey(record.StoreParam))
                {
                    s_dicCacheDicLP.Add(record.StoreParam, _value);
                }
                m_UnionMaxLevel++;
                return true;
            });
            if (m_UnionMaxLevel >= 1)
            {
                InitalStoreDic();
            }
            InitialAttempt();
        }
        public void OnChangeScene(int sceneId)
        {
            if (sceneId == 3)//主城（玛雅大陆）刷新
            {
                if (m_ulongModelGuid > 0)
                {
                    PlayerDataManager.Instance.ApplyPlayerInfo(m_ulongModelGuid, RenewalCharacterOne);
                }
            }
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            if (name == "HasUnion")
            {
                return HasAlliance();
            }
            else if (name == "MainUIInition")
            {
                MainUIInition();
            }
            else if (name == "CanRenewalInfo")
            {
                CanRenewalInfo((RefleshType)param[0]);
            }
            return null;
        }
        public FrameState State { get; set; }
        public void Close()
        {
            PlayerDataManager.Instance.CloseCharacterPopMenu();
            for (var i = 0; i < m_Build.DonationItem.Count; i++)
            {
                var _item = m_Build.DonationItem[i];
                if (_item.TimerCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(_item.TimerCoroutine);
                    _item.TimerCoroutine = null;
                }
            }
            //mCatchOhterUnion.Clear();
            //mOtherUnionDict.Clear();
            //mRefleshTime.Clear();
        }
        public void Tick()
        {
        }
        public void OnShow()
        {
            if (m_ulongModelGuid > 0)
            {
                PlayerDataManager.Instance.ApplyPlayerInfo(m_ulongModelGuid, RenewalCharacterTwo);
            }
        }
        public void RefreshData(UIInitArguments data)
        {
            var _tabId = 0;
            var _args = data as BattleUnionArguments;
            if (_args != null)
            {
                _tabId = _args.Tab;
            }
            m_listRefleshTime.Clear();
            for (var i = 0; i < m_iREFLESH_COUNT; i++)
            {
                m_listRefleshTime.Add(Game.Instance.ServerTime);
            }
            MainUIInition(_tabId);

            CanRenewalInfo(RefleshType.OtherUnion);
        }

        #endregion

        #region 逻辑函数

        #region 消息的处理

        private void BuildedDisplayCoinPage()
        {
            m_Build.MontyOrItemPage = 0;
        }
        private void BuildedDisplayPropPage()
        {
            m_Build.MontyOrItemPage = 1;
        }
        //设置战盟升级颜色显示
        private void SetFightMoneyStr()
        {
            if (m_GuildRecord.ConsumeUnionMoney <= 0)
            {
                m_BattleData.ContributionStr = string.Empty;
            }
            else
            {
                if (m_BattleData.MyUnion.Money >= m_GuildRecord.ConsumeUnionMoney)
                {
                    m_BattleData.ContributionStr = "[CFE5FF]" + m_GuildRecord.ConsumeUnionMoney + "[-]";
                }
                else
                {
                    m_BattleData.ContributionStr = "[FF0000]" + m_GuildRecord.ConsumeUnionMoney + "[-]";
                }
            }
        }
        //根据服务器地址获取战盟信息
        private void AcquireMineAllianceMsgByServersID(int isSimple, int listState)
        {
            NetManager.Instance.StartCoroutine(ApplyForMineAllianceMsgByServersIDCoroutine(PlayerDataManager.Instance.ServerId, isSimple,
                listState));
        }
        private IEnumerator ApplyForMineAllianceMsgByServersIDCoroutine(int ServerID, int isSimple, int listState)
        {
            using (new BlockingLayerHelper(0))
            {
                //0 详细，1简单
                var _msg = NetManager.Instance.ApplyAllianceDataByServerId(ServerID, isSimple);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SetApplyForAllianceRespond(_msg.Response, isSimple);
                        RenewalPage();
                        if (m_listRefleshTime.Count > 0)
                        {
                            m_listRefleshTime[0] = Game.Instance.ServerTime.AddSeconds(m_iINTERVEL_TIME);
                        }
                        DisplayMsg(listState);
                    }
                    else
                    {
                        // ServerID   Error_CharacterNoAlliance    Error_CharacterNoAlliance  Error_AllianceNotFind
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(e);
                }
            }
        }
        //设置战盟信息申请response返回的数据
        private void SetApplyForAllianceRespond(AllianceData data, int isSimple)
        {
            m_BattleData.MyUnion.UnionID = data.Id;
            m_BattleData.MyUnion.UnionName = data.Name;
            m_BattleData.MyUnion.ChiefID = data.Leader;
            m_BattleData.MyUnion.ServerID = data.ServerId;
            m_BattleData.MyUnion.Notice = data.Notice;
            m_BattleData.MyUnion.Force = data.FightPoint;
            m_BattleData.MyUnion.Level = data.Level;
            m_BattleData.MyUnion.Money = data.Money;
            var items = data.Depot.Items;
            var itemList = new List<BagItemDataModel>();
            for (int i = 0; i < items.Count; i++)
            {
                var bagItemData = new BagItemDataModel();
                bagItemData.ItemId = items[i].ItemId;
                bagItemData.Count = items[i].Count;
                bagItemData.Index = i;
                bagItemData.Exdata.InstallData(items[i].Exdata);
                itemList.Add(bagItemData);
            }
            m_DepotDataModel.DepotItems = new ObservableCollection<BagItemDataModel>(itemList);
            m_GuildRecord = Table.GetGuild(m_BattleData.MyUnion.Level);
            SetFightMoneyStr();
            m_BattleData.MyUnion.AutoAccept = data.AutoAgree;
            if (string.IsNullOrEmpty(data.Notice))
            {
                m_BattleData.MyUnion.Notice = GameUtils.GetDictionaryText(100000905);
            }
            m_Info.VarNotice = m_BattleData.MyUnion.Notice;
            if (isSimple == 1)
            {
                return;
            }

            var _count = 0;
            if (m_strOnline == "")
            {
                m_strOnline = GameUtils.GetDictionaryText(220953);
            }
            var _infoData = new List<CharacterBaseInfoDataModel>();
            m_dicUnionMembers.Clear();
            for (var i = 0; i < data.Members.Count; i++)
            {
                var _item = data.Members[i];
                var _baseData = new CharacterBaseInfoDataModel();
                _baseData.Index = i;
                _baseData.ID = _item.Guid;
                _baseData.Name = _item.Name;
                _baseData.Ladder = _item.Ladder;
                _baseData.RebornLadder = _item.RebornLadder;
                _baseData.Level = _item.Level;
                _baseData.CareerId = _item.TypeId;
                var _tbCharacterBase = Table.GetCharacterBase(_item.TypeId);
                if (_tbCharacterBase != null)
                {
                    _baseData.Career = _tbCharacterBase.Name;
                }
                _baseData.DonationCount = _item.MeritPoint;
                _baseData.Force = _item.FightPoint;
                _baseData.Online = _item.Online;
                if (_baseData.Online == 0)
                {
                    //baseData.Scene = "";
                    var _mLostTime = Game.Instance.ServerTime;
                    if (_item.LostTime != 0)
                    {
                        _mLostTime = Extension.FromServerBinary(_item.LostTime);
                    }
                    _baseData.LastTime = GameUtils.GetLastTimeDiffString(_mLostTime);
                }
                else
                {
                    _baseData.LastTime = m_strOnline;
                }
                var _tbSene = Table.GetScene(_item.SceneId);
                if (_tbSene == null)
                {
                    _baseData.Scene = "";
                }
                else
                {
                    _baseData.Scene = _tbSene.Name;
                }
                _infoData.Add(_baseData);
                if (data.Leader == _item.Guid)
                {
                    m_BattleData.MyUnion.ChiefName = _item.Name;
                }
            }
            //排序
            MemberListClassify(_infoData);
            for (var i = 0; i < _infoData.Count; i++)
            {
                if (m_dicUnionMembers.ContainsKey(_infoData[i].ID))
                {
                    int a = 0;
                    a ++;
                }
                m_dicUnionMembers.Add(_infoData[i].ID, _infoData[i]);
            }
            var _chacterid = PlayerDataManager.Instance.GetGuid();
            if (m_dicUnionMembers.ContainsKey(_chacterid))
            {
                m_BattleData.MyPorp = new CharacterBaseInfoDataModel(m_dicUnionMembers[_chacterid]);
            }
            SettingAccessed();
            m_BattleData.MyUnion.NowCount = m_dicUnionMembers.Count;
            m_BattleData.MyUnion.TotalCount = m_GuildRecord.MaxCount;
            // SetDonationItem(data.Missions);
        }
        //设置捐赠物品
        private void SetEndowmentProp(List<AllianceMissionDataOne> Missions)
        {
            var _count = 0;
            for (var i = 0; i < Missions.Count; i++)
            {
                var _ii = m_Build.DonationItem[i];
                _ii.TotalCount = Missions[i].MaxCount;
                _ii.TaskID = Missions[i].Id;
                var _tbMission = Table.GetGuildMission(Missions[i].Id);
                if (_tbMission == null)
                {
                    return;
                }
                _ii.ItemIDData.ItemId = _tbMission.ItemID;
                _ii.LeftCount = Missions[i].MaxCount - Missions[i].NowCount;
                _ii.State = Missions[i].State;
                _ii.NextTime = Missions[i].NextTime;
                if (_ii.State == (int)ItemState.Wait)
                {
                    SetEndowmentPropNull(i);
                }
                //if (Missions[i].Id == -1)
                //{
                //    SetDonationIitemNull(i);
                //}
            }
        }
        //被邀请加入后的应答 
        private void JoinAllianceRespond(string name1, int allianceId, string name2)
        {
            //自动加入邀请的战盟
            if (m_BattleData.CheckBox.CreateAutoAccept)
            {
                NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(3, allianceId));
            }
            else
            {
                //A邀请B加入战盟
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                    string.Format(GameUtils.GetDictionaryText(220900), name1, name2), "",
                    () => { NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(3, allianceId)); },
                    () => { NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(4, allianceId)); });
            }
        }
        //取得权限id 
        private int AcquireAccessed(ulong playerId)
        {
            var _accessId = -1;
            if (m_dicUnionMembers == null)
            {
                return _accessId;
            }
            if (m_dicUnionMembers.ContainsKey(playerId))
            {
                return m_dicUnionMembers[playerId].Ladder;
            }
            return _accessId;
        }
        //设置权限
        private void SettingAccessed()
        {
            var _vartbAccess = Table.GetGuildAccess(m_BattleData.MyPorp.Ladder);
            m_BattleData.Access.CanAddMember = _vartbAccess.CanAddMember;
            m_BattleData.Access.CanlevelBuff = _vartbAccess.CanLevelBuff;
            m_BattleData.Access.CanOperation = _vartbAccess.CanOperation;
            m_BattleData.Access.CanModifyNotice = _vartbAccess.CanModifyNotice;
        }
        // 判断是否成员可被操作 type  =  0 提升权限   1 降低权限 
        private bool IsMembersAccessed(int orgvalue, int willvalue)
        {
            var _varvalue = -1;
            if (orgvalue > willvalue)
            {
                if (willvalue < 0)
                {
                    var _e = new ShowUIHintBoard(220902);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return false;
                }
            }
            else if (orgvalue < willvalue)
            {
                if (willvalue > (int)battleAccess.Chief)
                {
                    return false;
                }
            }
            _varvalue = willvalue;
            var _vartbAccess = Table.GetGuildAccess(_varvalue);
            var _count = 0;
            {
                // foreach(var item in mUnionMembers)
                var _enumerator1 = (m_dicUnionMembers).GetEnumerator();
                while (_enumerator1.MoveNext())
                {
                    var _item = _enumerator1.Current;
                    {
                        if (_item.Value.Ladder == _varvalue)
                        {
                            _count++;
                        }
                    }
                }
            }
            //{0}最多{1}个
            if (_vartbAccess.MaxCount - _count <= 0)
            {
                var _ss = string.Format(GameUtils.GetDictionaryText(220914), _vartbAccess.Name, _count);
                var _e = new ShowUIHintBoard(_ss);
                EventDispatcher.Instance.DispatchEvent(_e);
                return false;
            }
            return true;
        }
        //type 0申请加入,1取消申请 ,2退出战盟,3同意邀请加入,4拒绝邀请加入
        private IEnumerator ApplyForOperateCoroution(int type, int value)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.AllianceOperation(type, value);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (type == 0)
                        {
                            var _index = s_dicOtherUnionDict[value];
                            s_listCatchOhterUnion[_index].IsApplyJoin = 1;
                            m_OtherUnion.OtherUnionList[m_iOtherUnionSelectIndex].IsApplyJoin = 1;
                            m_OtherUnion.JoinBtnText = GameUtils.GetDictionaryText(230300);
                            var _OtherUnionApplyUnionListCount0 = m_OtherUnion.ApplyUnionList.Count;
                            for (var i = 0; i < _OtherUnionApplyUnionListCount0; i++)
                            {
                                if (m_OtherUnion.ApplyUnionList[i] == 0)
                                {
                                    m_OtherUnion.ApplyUnionList[i] = value;
                                    break;
                                }
                            }
                            var _ee = new ShowUIHintBoard(220906);
                            EventDispatcher.Instance.DispatchEvent(_ee);

                            PlatformHelper.UMEvent("Union", "Apply");
                        }
                        else if (type == 1)
                        {
                            var _index = s_dicOtherUnionDict[value];
                            s_listCatchOhterUnion[_index].IsApplyJoin = 0;
                            m_OtherUnion.OtherUnionList[m_iOtherUnionSelectIndex].IsApplyJoin = 0;
                            m_OtherUnion.JoinBtnText = GameUtils.GetDictionaryText(230301);
                            var _OtherUnionApplyUnionListCount1 = m_OtherUnion.ApplyUnionList.Count;
                            for (var i = 0; i < _OtherUnionApplyUnionListCount1; i++)
                            {
                                if (m_OtherUnion.ApplyUnionList[i] == value)
                                {
                                    m_OtherUnion.ApplyUnionList[i] = 0;
                                    break;
                                }
                            }
                        }
                        else if (type == 2)
                        {
                            //var ee = new ShowUIHintBoard(220951);
                            //EventDispatcher.Instance.DispatchEvent(ee);
                            m_BattleData.CreateName = string.Empty;
                            InitialCreationInput();
                            m_BattleData.MyUnion.UnionID = 0;
                            m_iUnionLevelChanged = 0;
                            ButtonAnotherAlliance();
                        }
                    }
                    else
                    {
                        if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceApplyJoinOK) //自动申请，成功
                        {
                            m_BattleData.MyUnion.UnionID = value;
                            PlayerDataManager.Instance.SetExData((int)eExdataDefine.e282, value);
                            MainUIInition();
                        }
                        else if (_msg.ErrorCode == (int)ErrorCodes.Error_Alliance_Limit_Time)
                        {
                            GameUtils.ShowHintTip(100003301);
                            yield break;
                        }
                        else
                        {
                            UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        }
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }
        //type 0邀请加入,1同意申请 1,2拒绝申请
        private IEnumerator ApplyForOperateRoleCoroutine(int type, ulong value)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.AllianceOperationCharacter(type, value);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (type == 1)
                        {
                            //Logger.Info("申请加入成功");
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }
        //权限设置
        private IEnumerator SetPermissionCoroutine(int UnionID, ulong memberId, int access)
        {
            using (new BlockingLayerHelper(0))
            {
                if (memberId <= 0)
                {
                    yield break;
                }
                var _msg = NetManager.Instance.ChangeJurisdiction(UnionID, memberId, access);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (access == -1)
                        {
                            //踢出玩家
                            if (m_dicUnionMembers.ContainsKey(memberId))
                            {
                                m_dicUnionMembers.Remove(memberId);
                            }
                            var _e = new ShowUIHintBoard(220911);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else if (access == (int)battleAccess.Chief)
                        {
                            var _e = new ShowUIHintBoard(220912);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            m_BattleData.MyPorp.Ladder = (int)battleAccess.People0;
                            if (m_dicUnionMembers.ContainsKey(m_BattleData.MyPorp.ID))
                            {
                                m_dicUnionMembers[m_BattleData.MyPorp.ID].Ladder = 0;
                            }

                            if (m_dicUnionMembers.ContainsKey(memberId))
                            {
                                m_dicUnionMembers[memberId].Ladder = access;
                                m_BattleData.MyUnion.ChiefName = m_dicUnionMembers[memberId].Name;
                            }
                            SettingAccessed();
                        }
                        else
                        {
                            var _tbAccess = Table.GetGuildAccess(access);
                            var _str = string.Format(GameUtils.GetDictionaryText(220913), _tbAccess.Name);
                            var _e = new ShowUIHintBoard(_str);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            if (m_dicUnionMembers.ContainsKey(memberId))
                            {
                                m_dicUnionMembers[memberId].Ladder = access;
                            }
                        }
                        DisplayMsg(2);
                        //GetMyUnionInfoByServerId(0, Info.ListState);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }
        //显示下拉列表
        private void DisplayOperationUI(CharacterBaseInfoDataModel data)
        {
            CharacterBaseInfoDataModel varData = null;
            if (!m_dicUnionMembers.ContainsKey(data.ID))
            {
                m_ulongMemberId = 0;
                return;
            }
            m_ulongMemberId = data.ID;

            var _characterId = PlayerDataManager.Instance.GetGuid();
            var _member = m_dicUnionMembers[m_ulongMemberId];
            var _characterName = _member.Name;
            if (data.ID == _characterId)
            {
                return;
            }

            var _ladder = 0;

            switch (m_BattleData.MyPorp.Ladder)
            {
                case (int)battleAccess.People0:
                    _ladder = 11;
                    break;
                case (int)battleAccess.People1:
                    _ladder = 11;
                    break;
                case (int)battleAccess.AssistantChief:
                    _ladder = 12;
                    break;
                case (int)battleAccess.Chief:
                    _ladder = 13;
                    break;
            }
            PlayerDataManager.Instance.ShowCharacterPopMenu(m_ulongMemberId, _characterName, _ladder, _member.Level, _member.Ladder,
                _member.CareerId);
        }

        #endregion
        #region 基础接口

        private bool HasAlliance()
        {
            return m_BattleData.MyUnion.UnionID > 0;
        }

        #region OnToggleChange

        //对勾等的响应事件
        private void OnToggleSet(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "ShowOffLine")
            //{
            //    ShowInfo(0);
            //}
            //else
            if (e.PropertyName == "SelectAll")
            {
                {
                    // foreach(var item in Info.ShowList)
                    var _enumerator12 = (m_BattleData.MyUnion.ApplyList).GetEnumerator();
                    while (_enumerator12.MoveNext())
                    {
                        var _item = _enumerator12.Current;
                        {
                            if (m_BattleData.CheckBox.SelectAll)
                            {
                                _item.Selected = 1;
                            }
                            else
                            {
                                _item.Selected = 0;
                            }
                        }
                    }
                }
            }
            else if (e.PropertyName == "CreateAutoAccept")
            {
            }
            else if (e.PropertyName == "ShowAutoJoin")
            {
                m_OtherUnion.OtherUnionList.Clear();
                m_iOtherUnionSelectIndex = -1;
                if (m_BattleData.CheckBox.ShowAutoJoin)
                {
                    var _otherList1 = new List<BattleUnionTeamSimpleDataModel>();
                    for (var i = 0; i < s_listCatchOhterUnion.Count; i++)
                    {
                        var _item = s_listCatchOhterUnion[i];
                        if (_item.AutoAccept)
                        {
                            var _vardata = new BattleUnionTeamSimpleDataModel(_item);
                            _vardata.Selected = 0;
                            _otherList1.Add(_vardata);
                        }
                    }
                    _otherList1.Sort(OtherUnionListSort);
                    m_OtherUnion.OtherUnionList = new ObservableCollection<BattleUnionTeamSimpleDataModel>(_otherList1);
                }
                else
                {
                    var _otherList2 = new List<BattleUnionTeamSimpleDataModel>();
                    for (var i = 0; i < s_listCatchOhterUnion.Count; i++)
                    {
                        var _item = s_listCatchOhterUnion[i];
                        var _vardata = new BattleUnionTeamSimpleDataModel(_item);
                        _vardata.Selected = 0;
                        _otherList2.Add(_vardata);
                    }
                    _otherList2.Sort(OtherUnionListSort);
                    m_OtherUnion.OtherUnionList = new ObservableCollection<BattleUnionTeamSimpleDataModel>(_otherList2);
                }
                //申请加入
                m_OtherUnion.JoinBtnText = GameUtils.GetDictionaryText(230301);
            }
            else if (e.PropertyName == "ShowOffLineDetail")
            {
                DisplayMsg(2);
            }
        }

        #endregion

        #endregion
        #region Tab事件

        //更新page页面信息
        private void RenewalPage()
        {
            if (m_iUnionLevelChanged != m_BattleData.MyUnion.Level)
            {
                m_iUnionLevelChanged = m_BattleData.MyUnion.Level;
                RenewalAllianceLv();
                InitionBuff();
                InitalBufName();
                RenewalBuffPage(m_BuffSelected);
            }
            InitalStore();
            switch (m_BattleData.TabPage)
            {
                case (int)TabPage.PageInfo:
                {
                    IsAllianceLv();
                }
                    break;
                case (int)TabPage.PageBuild:
                {
                }
                    break;
                case (int)TabPage.PageLevel:
                {
                    m_listRefleshTime[3] = Game.Instance.ServerTime.AddSeconds(m_iINTERVEL_TIME);
                    RenewalBuffPage(m_BuffSelected);
                }
                    break;
                case (int)TabPage.PageCity:
                {
                }
                    break;
                case (int)TabPage.PageShop:
                {
                }
                    break;
            }
        }
        private void InitionBuff()
        {
            for (var i = 0; i < 4; i++)
            {
                m_Buff.BuffList[i].BuffID = PlayerDataManager.Instance.GetExData(550 + i);
            }
        }
        //退出战盟按钮
        private void TabQuitAlliance()
        {
            if (m_BattleData.MyPorp.Ladder == (int)battleAccess.Chief)
            {
                if (m_dicUnionMembers.Count > 1)
                {
                    ExitAllianceMessage();
                }
                else
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, GameUtils.GetDictionaryText(220961), "",
                        () => { NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(2, m_BattleData.MyUnion.UnionID)); }
                        );
                }
            }
            else
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, GameUtils.GetDictionaryText(220929), "",
                    () => { NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(2, m_BattleData.MyUnion.UnionID)); }
                    );
            }
        }
        //退出战盟按钮
        private void ExitAllianceMessage()
        {
            var _varLadder = -1;
            ulong _maxLadder = 0;
            {
                // foreach(var varitem in mUnionMembers)
                var _enumerator11 = (m_dicUnionMembers).GetEnumerator();
                while (_enumerator11.MoveNext())
                {
                    var _varitem = _enumerator11.Current;
                    {
                        var _item = _varitem.Value;
                        if (_item.Ladder == (int)battleAccess.Chief)
                        {
                            continue;
                        }
                        if (_item.Ladder > _varLadder)
                        {
                            _varLadder = _item.Ladder;
                            _maxLadder = _varitem.Key;
                        }
                        else if (_item.Ladder == _varLadder)
                        {
                            if (_item.DonationCount > m_dicUnionMembers[_maxLadder].DonationCount)
                            {
                                _varLadder = _item.Ladder;
                                _maxLadder = _varitem.Key;
                            }
                        }
                    }
                }
            }
            //转让
            var _str = string.Format(GameUtils.GetDictionaryText(220945), m_dicUnionMembers[_maxLadder].Name);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _str, "",
                () => { NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(2, m_BattleData.MyUnion.UnionID)); }
                );
        }

        #endregion
        #region 战盟初始化

        //战盟界面初始化
        private void MainUIInition(int tabId = 0)
        {
            if (tabId < 0)
            {
                tabId = 0;
            }
            m_BattleData.TabPage = tabId;
            m_BattleData.TabPage2 = 0;
            m_AttackCity.TabPage = -1;
            m_Build.NowCount = PlayerDataManager.Instance.GetExData(eExdataDefine.e285);
            m_BattleData.MyUnion.UnionID = PlayerDataManager.Instance.GetExData(eExdataDefine.e282);
            m_Build.TodayDonation = PlayerDataManager.Instance.GetExData(eExdataDefine.e284);
            m_listRefleshTime.Clear();
            for (var i = 0; i < m_iREFLESH_COUNT; i++)
            {
                m_listRefleshTime.Add(Game.Instance.ServerTime);
            }
            if (m_BattleData.MyUnion.UnionID > 0)
            {
                m_Build.MontyOrItemPage = 0;
                //BattleData.TabPage = (int)TabPage.PageInfo;
                m_Buff.ShowUpUI = 0;
                m_BattleData.ShowWitchUI = 0;
                if (tabId == 0)
                {
                    CanRenewalInfo(RefleshType.UnionData);
                }
                if (tabId == 0)
                {
                    CanRenewalInfo(RefleshType.UnionData);
                }
                else if (tabId == (int)TabPage.PageBuild)
                {
                    CanRenewalInfo(RefleshType.DonationData);
                }
                else if (tabId == (int)TabPage.PageCity)
                {
                    CanRenewalInfo(RefleshType.AttackData);
                }
            }
            else
            {
                if (m_BattleData.CreateName == string.Empty)
                {
                    m_BattleData.CreateName = m_strInputStr;
                }
                m_BattleData.ShowWitchUI = 1;
            }
            //InitBuff();
            m_OtherUnion.ApplyUnionList[0] = PlayerDataManager.Instance.GetExData(eExdataDefine.e286);
            m_OtherUnion.ApplyUnionList[1] = PlayerDataManager.Instance.GetExData(eExdataDefine.e287);
            m_OtherUnion.ApplyUnionList[2] = PlayerDataManager.Instance.GetExData(eExdataDefine.e288);
        }

        //升级等级后刷新界面信息
        private void RenewalAllianceLv()
        {
            m_Build.DonationIndex = m_BattleData.MyUnion.Level; //建设临时索引，主要用来设置建设页面的绑定值，
            SetFightMoneyStr();
            m_Build.TotalCount = m_GuildRecord.moneyCountLimit;
            m_BattleData.MyUnion.TotalCount = m_GuildRecord.MaxCount;
            m_Info.ReduceString = string.Format(GameUtils.GetDictionaryText(220972), m_GuildRecord.MaintainMoney); //维护资金
            m_GuildRecord = Table.GetGuild(m_BattleData.MyUnion.Level);
        }

        //初始化商店
        private void InitalStore()
        {
            var _list = new List<BattleUnionShopItemDataModel>();
            for (var i = 1; i <= m_UnionMaxLevel; i++)
            {
                var _tbGuild = Table.GetGuild(i);
                if (_tbGuild == null)
                {
                    return;
                }
                List<StoreRecord> _varlist;
                if (s_dicCacheDic.TryGetValue(_tbGuild.StoreParam, out _varlist))
                {
                    for (var j = 0; j < _varlist.Count; j++)
                    {
                        var _data = new BattleUnionShopItemDataModel();
                        _data.ShopID = _varlist[j].Id;
                        _data.ItemID = _varlist[j].ItemId;
                        _data.Zhangong = _varlist[j].NeedValue;
                        _data.ItemCount = _varlist[j].ItemCount;
                        _data.BuyLevel = s_dicCacheDicLP[_varlist[j].Type];
                        _data.ItemType = _varlist[j].NeedType;
                        _data.BuyCount = PlayerDataManager.Instance.GetExData(_varlist[j].DayCount);
                        if (_varlist[j].Type <= m_GuildRecord.StoreParam)
                        {
                            _data.CanBuy = 1;
                        }
                        else
                        {
                            _data.CanBuy = 0;
                        }
                        _list.Add(_data);
                    }
                }
            }
            m_Shop.ShopList = new ObservableCollection<BattleUnionShopItemDataModel>(_list);
        }

        //商店初始化字典
        private void InitalStoreDic()
        {
            s_dicCacheDic.Clear();
            var _tbminGuild = Table.GetGuild(1);
            var _tbmaxGuild = Table.GetGuild(m_UnionMaxLevel);
            if (_tbminGuild == null || _tbmaxGuild == null)
            {
                return;
            }
            Table.ForeachStore(record =>
            {
                var _key = record.Type;
                if (_key >= _tbminGuild.StoreParam && _key <= _tbmaxGuild.StoreParam)
                {
                    if (s_dicCacheDic.ContainsKey(_key))
                    {
                        s_dicCacheDic[_key].Add(record);
                    }
                    else
                    {
                        var _list = new List<StoreRecord>();
                        _list.Add(record);
                        s_dicCacheDic.Add(_key, _list);
                    }
                }
                return true;
            });
        }

        // 0 战盟信息刷新  //1战盟捐献刷新    //2 申请列表
        private void CanRenewalInfo(RefleshType type)
        {
            switch (type)
            {
                case RefleshType.UnionData:
                {
                    if (Game.Instance.ServerTime >= m_listRefleshTime[0])
                    {
                        AcquireMineAllianceMsgByServersID(0, 0);
                    }
                }
                    break;
                case RefleshType.DonationData:
                {
                    if (Game.Instance.ServerTime >= m_listRefleshTime[1])
                    {
                        NetManager.Instance.StartCoroutine(AcquireEndowmentMsgCoroutine(m_BattleData.MyUnion.UnionID));
                    }
                }
                    break;
                case RefleshType.UnionDataSimple:
                {
                    if (Game.Instance.ServerTime >= m_listRefleshTime[3])
                    {
                        AcquireMineAllianceMsgByServersID(1, 0);
                    }
                }
                    break;
                case RefleshType.ApplyData:
                {
                    if (Game.Instance.ServerTime >= m_listRefleshTime[2])
                    {
                        NetManager.Instance.StartCoroutine(ApplyUnionListCoroutine(m_BattleData.MyUnion.UnionID));
                    }
                    else
                    {
                        //没有玩家申请加入战盟
                        if (m_BattleData.MyUnion.ApplyList.Count == 0)
                        {
                            PlayerDataManager.Instance.NoticeData.BattleList = false;
                            var _e = new ShowUIHintBoard(220965);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            return;
                        }
                        m_BattleData.CheckBox.SelectAll = false;
                        //Info.JoinShow = 1;
                        DisplayMsg(1);
                    }
                }
                    break;
                case RefleshType.MemberDetailData:
                {
                    m_Info.ShowDetail = 1;
                    if (Game.Instance.ServerTime >= m_listRefleshTime[0])
                    {
                        AcquireMineAllianceMsgByServersID(0, 2);
                    }
                    else
                    {
                        var a = 3;
                        //Info.ShowDetail = 1;
                        DisplayMsg(2);
                    }
                }
                    break;
                case RefleshType.OtherUnion:
                {
                    if (Game.Instance.ServerTime >= m_listRefleshTime[4])
                    {
                        ButtonAnotherAlliance();
                    }
                }
                    break;
                case RefleshType.AttackData:
                {
                    if (Game.Instance.ServerTime >= m_listRefleshTime[5])
                    {
                        ApplyForUnionFightInfo();
                        ApplyForUnionFightDefierInfo();
                    }
                }
                    break;
                case RefleshType.UnionDepot:
                {
                    ApplyAllDepotInfo();
                }
                    break;
            }
        }

        //刷新tabPage
        private void RenewalTabPage(int mPage)
        {
            switch (mPage)
            {
                case 0: //信息                                                 
                {
                    CanRenewalInfo(RefleshType.UnionDataSimple);
                }
                    break;
                case 1: //成员
                {
                    CanRenewalInfo(RefleshType.MemberDetailData);
                }
                    break;
                case 2: //建设
                {
                    CanRenewalInfo(RefleshType.DonationData);
                }
                    break;
                case 3: //技能
                {
                    CanRenewalInfo(RefleshType.UnionDataSimple);
                }
                    break;
                case 4: //商店
                {
                    CanRenewalInfo(RefleshType.UnionDataSimple);
                }
                    break;
                case 5: //刷
                {
                    CanRenewalInfo(RefleshType.AttackData);
                }
                    break;
                case 6: //其他战盟
                {
                    CanRenewalInfo(RefleshType.OtherUnion);
                }
                    break;
                case 7: //战盟仓库
                {
                    CanRenewalInfo(RefleshType.UnionDepot);
                }
                    break;
            }
        }

        #endregion
        #region 战盟创建

        //判断战盟名称合法性
        private static bool InspectName(string input)
        {
            var _regex = new Regex(@"^[\u4E00-\u9FFFA-Za-z0-9]{1,14}$");
            if (!_regex.IsMatch(input))
            {
                return false;
            }
            var length = Regex.Replace(input, @"[\u4E00-\u9FFF]", "aa").Length;
            if (length > 14 || length <= 0)
            {
                return false;
            }
            return true;
        }

        private IEnumerator CreationAllimanceCoroutine(string name)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.CreateAlliance(name);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_BattleData.ShowWitchUI = 0;
                        m_BattleData.MyUnion.UnionID = _msg.Response;
                        m_UionState = UnionIDState.CreateJoin;
                        //GetMyUnionInfoById();
                        AcquireMineAllianceMsgByServersID(0, 0);

                        PlatformHelper.UMEvent("Union", "Create");
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //其他战盟
        private void ButtonAnotherAlliance()
        {
            m_iOtherUnionSelectIndex = -1;
            NetManager.Instance.StartCoroutine(AcquireAnotherAllianceCoroutine(PlayerDataManager.Instance.ServerId));
        }

        private IEnumerator AcquireAnotherAllianceCoroutine(int ServerID)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.GetServerAlliance(ServerID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_OtherUnion.OtherUnionList.Clear();
                        s_listCatchOhterUnion.Clear();
                        s_dicOtherUnionDict.Clear();
                        var _otherList = new List<BattleUnionTeamSimpleDataModel>();
                        for (var i = 0; i < _msg.Response.Alliances.Count; i++)
                        {
                            var _item = _msg.Response.Alliances[i];
                            var _unionTeam = new BattleUnionTeamSimpleDataModel();
                            _unionTeam.Index = i;
                            _unionTeam.UnionID = _item.Id;
                            _unionTeam.UnionName = _item.Name;
                            _unionTeam.ChiefName = _item.LeaderName;
                            _unionTeam.Level = _item.Level;
                            _unionTeam.NowCount = _item.NowCount;
                            _unionTeam.TotalCount = _item.MaxCount;
                            _unionTeam.Force = _item.FightPoint;
                            if (_item.AutoAgree == 1)
                            {
                                _unionTeam.AutoAccept = true;
                            }
                            else
                            {
                                _unionTeam.AutoAccept = false;
                            }
                            //是否已申请图标设置
                            for (var j = 0; j < m_OtherUnion.ApplyUnionList.Count; j++)
                            {
                                if (m_OtherUnion.ApplyUnionList[j] == 0)
                                {
                                    continue;
                                }
                                if (_unionTeam.UnionID == m_OtherUnion.ApplyUnionList[j])
                                {
                                    _unionTeam.IsApplyJoin = 1;
                                }
                            }

                            var _unionTeam2 = new BattleUnionTeamSimpleDataModel(_unionTeam);
                            s_dicOtherUnionDict.Add(_unionTeam.UnionID, i);
                            s_listCatchOhterUnion.Add(_unionTeam2);
                            //按照选中状态筛选其他战盟显示
                            if (m_BattleData.CheckBox.ShowAutoJoin)
                            {
                                if (_unionTeam.AutoAccept)
                                {
                                    _otherList.Add(_unionTeam); //OtherUnion.OtherUnionList.Add(unionTeam);
                                }
                            }
                            else
                            {
                                _otherList.Add(_unionTeam); //OtherUnion.OtherUnionList.Add(unionTeam);
                            }
                        }
                        _otherList.Sort(OtherUnionListSort);
                        int zhanLingIndex = -1;
                        BattleUnionTeamSimpleDataModel tempDataMadel = null;

                        //for (int i = 0; i < _otherList.Count; i++)
                        //{
                        //    if (m_AttackCity.CityUnionName == GameUtils.GetDictionaryText(270292))
                        //    {
                        //        break;
                        //    }                        
                        //    if (_otherList[i].UnionName == m_AttackCity.CityUnionName)
                        //    {
                        //        zhanLingIndex = i;
                        //        tempDataMadel = _otherList[i];
                        //        _otherList.RemoveAt(zhanLingIndex);
                        //        break;                            
                        //    }
                        //}

                        //if (zhanLingIndex!=-1 && tempDataMadel != null)
                        //{                          
                        //    _otherList.Insert(0, tempDataMadel);                                              
                        //}

                        m_OtherUnion.OtherUnionList = new ObservableCollection<BattleUnionTeamSimpleDataModel>(_otherList);
                        if (m_OtherUnion.OtherUnionList.Count > 0)
                        {
                            ChooseAnotherAlliance(0);
                        }
                        m_listRefleshTime[4] = Game.Instance.ServerTime.AddSeconds(m_iINTERVEL_TIME);
                        m_OtherUnion.JoinBtnText = GameUtils.GetDictionaryText(230301);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        private int OtherUnionListSort(BattleUnionTeamSimpleDataModel dataModel1, BattleUnionTeamSimpleDataModel dataModel2)
        {
            if (dataModel1.Level.CompareTo(dataModel2.Level) != 0)
            {
                return -(dataModel1.Level.CompareTo(dataModel2.Level));
            }        
            //else if (dataModel1.TotalCount.CompareTo(dataModel2.TotalCount) != 0)
            //{
            //    return -(dataModel1.TotalCount.CompareTo(dataModel2.TotalCount));
            //}
            else if (dataModel1.Force.CompareTo(dataModel2.Force) != 0)
            {
                return -(dataModel1.Force.CompareTo(dataModel2.Force));
            }
            else if (dataModel1.UnionID.CompareTo(dataModel2.UnionID) != 0)
            {
                return -(dataModel1.UnionID.CompareTo(dataModel2.UnionID));
            }
            else
            {
                return 1;
            }
        }

        private void EliminateCreationInput()
        {
            if (m_BattleData.CreateName == m_strInputStr)
            {
                m_BattleData.CreateName = string.Empty;
            }
        }

        private void InitialCreationInput()
        {
            if (String.IsNullOrEmpty(m_BattleData.CreateName))
            {
                m_BattleData.CreateName = m_strInputStr;
            }
        }

        #endregion
        #region 其他战盟

        //加入其他战盟
        private void ButtonApplyForJoinUnion()
        {
            //加入战盟等级不足
            var _tbClient = Table.GetClientConfig(243);
            var _varlevel = int.Parse(_tbClient.Value);
            if (_varlevel > PlayerDataManager.Instance.GetLevel())
            {
                var _str = string.Format(GameUtils.GetDictionaryText(220944), _varlevel);
                var _e = new ShowUIHintBoard(_str);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            //你已经加入战盟，不可加入新的战盟
            if (m_BattleData.MyUnion.UnionID > 0)
            {
                var _e = new ShowUIHintBoard(220918);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            //选择你想要加入的战盟
            if (m_iOtherUnionSelectIndex == -1)
            {
                var _e = new ShowUIHintBoard(220919);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _selectedItem = m_OtherUnion.OtherUnionList[m_iOtherUnionSelectIndex];
            //战盟已经满
            if (_selectedItem.NowCount == _selectedItem.TotalCount)
            {
                var _e = new ShowUIHintBoard(220905);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _count = 0;
            if (_selectedItem.IsApplyJoin == 0)
            {
                var _OtherUnionApplyUnionListCount4 = m_OtherUnion.ApplyUnionList.Count;
                for (var i = 0; i < _OtherUnionApplyUnionListCount4; i++)
                {
                    if (m_OtherUnion.ApplyUnionList[i] > 0)
                    {
                        _count++;
                    }
                }
                //您已达到战盟最大申请数量
                if (_count >= m_OtherUnion.ApplyUnionList.Count)
                {
                    var _e = new ShowUIHintBoard(220920);
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
                NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(0, _selectedItem.UnionID));
            }
            //取消加入此战盟的申请
            else if (_selectedItem.IsApplyJoin == 1)
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, GameUtils.GetDictionaryText(220921), "",
                    () => { NetManager.Instance.StartCoroutine(ApplyForOperateCoroution(1, _selectedItem.UnionID)); });
            }
        }

        //其他战盟页面返回
        private void ButtonAnotherGoBack()
        {
            if (m_BattleData.MyUnion.UnionID > 0)
            {
                m_BattleData.ShowWitchUI = 0;
            }
            else
            {
                m_BattleData.ShowWitchUI = 1;
                //EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.BattleUnionUI));
            }
        }

        //其他战盟选择
        private void ChooseAnotherAlliance(int index)
        {
            if (m_iOtherUnionSelectIndex != -1)
            {
                m_OtherUnion.OtherUnionList[m_iOtherUnionSelectIndex].Selected = 0;
            }
            m_iOtherUnionSelectIndex = index;
            if (m_OtherUnion.OtherUnionList[index].IsApplyJoin == 1)
            {
                //取消申请
                m_OtherUnion.JoinBtnText = GameUtils.GetDictionaryText(230300);
            }
            else
            {
                //申请加入
                m_OtherUnion.JoinBtnText = GameUtils.GetDictionaryText(230301);
            }
            m_OtherUnion.OtherUnionList[index].Selected = 1;
        }

        #endregion
        #region 战盟信息

        //战盟申请列表
        private void ButtonApplyForList()
        {
            //Info.JoinShow = Info.JoinShow == 0 ? 1 : 0;

            CanRenewalInfo(RefleshType.ApplyData);
        }

        //请求战盟申请列表
        private void ApplyForList()
        {
            NetManager.Instance.StartCoroutine(ApplyForListCoroutine(m_BattleData.MyUnion.UnionID));
        }

        //战盟申请列表
        private IEnumerator ApplyForListCoroutine(int BattleUnionId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceEnjoyList(BattleUnionId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        if (_msg.Response.Applys.Count > 0)
                        {
                            if (m_BattleData.Access.CanAddMember == 1)
                            {
                                PlayerDataManager.Instance.NoticeData.BattleList = true;
                            }
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        //战盟申请列表
        private IEnumerator ApplyUnionListCoroutine(int BattleUnionId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceEnjoyList(BattleUnionId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _count = 0;
                        m_BattleData.MyUnion.ApplyList.Clear();
                        var _list = new List<CharacterBaseInfoDataModel>();
                        {
                            // foreach(var item in msg.Response.Applys)
                            var _enumerator2 = (_msg.Response.Applys).GetEnumerator();
                            while (_enumerator2.MoveNext())
                            {
                                var _item = _enumerator2.Current;
                                {
                                    var _baseData = new CharacterBaseInfoDataModel();
                                    var _white = GameUtils.GetTableColor(0);
                                    _baseData.ColorOnLine = _white;
                                    _baseData.Selected = 0;
                                    _baseData.DonationCount = _item.MeritPoint;
                                    _baseData.Force = _item.FightPoint;
                                    _baseData.Online = _item.Online;
                                    if (_baseData.Online == 0)
                                    {
                                        //baseData.Scene = "";
                                        var _mLostTime = Game.Instance.ServerTime;
                                        if (_item.LostTime != 0)
                                        {
                                            _mLostTime = Extension.FromServerBinary(_item.LostTime);
                                        }
                                        _baseData.LastTime = GameUtils.GetLastTimeDiffString(_mLostTime);
                                    }
                                    else
                                    {
                                        _baseData.LastTime = m_strOnline;
                                    }
                                    var _tbSene = Table.GetScene(_item.SceneId);
                                    if (_tbSene == null)
                                    {
                                        _baseData.Scene = "";
                                    }
                                    else
                                    {
                                        _baseData.Scene = _tbSene.Name;
                                    }
                                    _baseData.Index = _count;
                                    _baseData.ID = _item.Guid;
                                    _baseData.Name = _item.Name;
                                    _baseData.Ladder = _item.Ladder;
                                    _baseData.Level = _item.Level;
                                    _baseData.CareerId = _item.TypeId;
                                    var _tbCharacterBase = Table.GetCharacterBase(_item.TypeId);
                                    if (_tbCharacterBase != null)
                                    {
                                        _baseData.Career = _tbCharacterBase.Name;
                                    }
                                    _list.Add(_baseData);
                                    _count++;
                                }
                            }
                            m_BattleData.MyUnion.ApplyList = new ObservableCollection<CharacterBaseInfoDataModel>(_list);
                        }
                        //没有玩家申请加入战盟
                        if (m_BattleData.MyUnion.ApplyList.Count != 0)
                        {
                            PlayerDataManager.Instance.NoticeData.BattleList = true;
                        }
                        else
                        {
                            //Info.JoinShow = 0;
                            //ShowInfo(0);
                            var _e = new ShowUIHintBoard(220965);
                            EventDispatcher.Instance.DispatchEvent(_e);
                            PlayerDataManager.Instance.NoticeData.BattleList = false;
                            yield break;
                        }
                        m_BattleData.CheckBox.SelectAll = false;
                        //Info.JoinShow = 1;
                        DisplayMsg(1);
                        m_listRefleshTime[2] = Game.Instance.ServerTime.AddSeconds(m_iINTERVEL_TIME);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //详细信息返回
        private void ButtonParticularReturn()
        {
            m_Info.ShowDetail = 1;
        }

        //添加成员按钮事件
        private void ButtonAdditionMember()
        {
            m_Info.ShowFindUI = 1;
        }

        //添加战盟成员按钮
        private void AdditionMemberOkay()
        {
            m_Info.FindName = m_Info.FindName.Trim();
            NetManager.Instance.StartCoroutine(AdditionMemberCoroutine(m_Info.FindName));
        }

        private IEnumerator AdditionMemberCoroutine(string name)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.AllianceOperationCharacterByName(0, name);
                //"已发送邀请玩家加入战盟的请求"
                m_Info.ShowFindUI = 0;

                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _ee = new ShowUIHintBoard(220974);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        //关闭添加成员页面
        private void CloseAdditionMeberUI()
        {
            m_Info.ShowFindUI = 0;
            m_Info.FindName = "";
        }

        // type = 0 批量申请    //type =1 批量拒绝
        private IEnumerator UnionAgreeMentApplyForListCoroutine(int BattleUnionId, int type, Uint64Array IDlist, List<int> indexList)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.AllianceAgreeApplyList(BattleUnionId, type, IDlist);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        for (var i = indexList.Count - 1; i >= 0; i--)
                        {
                            m_BattleData.MyUnion.ApplyList.RemoveAt(indexList[i]);
                        }
                        DisplayMsg(1);
                        if (type == 0)
                        {
                            //刷新成员信息
                            AcquireMineAllianceMsgByServersID(0, 1);
                        }
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_CharacterHaveAlliance)
                    {
                        for (var i = indexList.Count - 1; i >= 0; i--)
                        {
                            m_BattleData.MyUnion.ApplyList.RemoveAt(indexList[i]);
                        }
                        GameUtils.ShowHintTip(220973);
                        yield break;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        private void DisposeOfApplyForButton()
        {
            var _count = 0;
            var _idList = new Uint64Array();
            var _indexList = new List<int>();
            {
                // foreach(var item in Info.ShowList)
                var _enumerator4 = (m_BattleData.MyUnion.ApplyList).GetEnumerator();
                while (_enumerator4.MoveNext())
                {
                    var _item = _enumerator4.Current;
                    {
                        if (_item.Selected == 1)
                        {
                            //    if (count < BattleData.MyUnion.TotalCount - BattleData.MyUnion.NowCount)
                            //    {
                            _idList.Items.Add(m_BattleData.MyUnion.ApplyList[_count].ID);
                            _indexList.Add(_count);
                            //    }
                            //    else
                            //    {
                            //        break;
                            //    }
                            //}
                            _count++;
                        }
                    }
                }
            }
            if (_idList.Items.Count > 0)
            {
                NetManager.Instance.StartCoroutine(UnionAgreeMentApplyForListCoroutine(1, m_BattleData.MyUnion.UnionID, _idList, _indexList));
            }
        }

        // ShowInfo(0); //信息界面返回
        private void DataUIBackButton()
        {
            DisplayMsg(1);
        }

        //// ShowInfo(3);//成员信息
        //private void BtnMemberInfo()
        //{
        //    CanRefleshData(RefleshType.MemberDetailData);

        //    //UIEvent_UnionBtnMemberInfo e = ievent as UIEvent_UnionBtnMemberInfo;
        //    //Info.ShowList.Clear();
        //    //if (e.Index == 0)
        //    //{
        //    //    ShowInfo(0);
        //    //}
        //    //else if(e.Index==1)
        //    //{
        //    //    ShowInfo(1);
        //    //}
        //}
        private void MemberListClassify(List<CharacterBaseInfoDataModel> list)
        {
            // var varList = list;
            list.Sort((a, b) =>
            {
                if (a.Ladder < b.Ladder)
                {
                    return 1;
                }
                if (a.Ladder == b.Ladder)
                {
                    if (a.Level < b.Level)
                    {
                        return 1;
                    }
                    if (a.Level == b.Level)
                    {
                        return a.Index - b.Index;
                    }
                    return -1;
                }
                return -1;
            });

            // return array;
        }

        //0 基本信息，1 其他信息，2 成员详细信息
        private void DisplayMsg(int type)
        {
            var _white = GameUtils.GetTableColor(0);
            var _Gray = GameUtils.GetTableColor(96);

            #region

            //if (type == 0) //0 基本信息
            //{
            //    Info.ListState = 0;
            //    Info.JoinShow = 0;
            //    Info.ShowList.Clear();
            //    if (mUnionMembers != null)
            //    {
            //        if (BattleData.CheckBox.ShowOffLine)
            //        {
            //            ObservableCollection<CharacterBaseInfoDataModel> varList = new ObservableCollection<CharacterBaseInfoDataModel>();
            //            {
            //                // foreach(var item in mUnionMembers)
            //                var __enumerator5 = (mUnionMembers).GetEnumerator();
            //                while (__enumerator5.MoveNext())
            //                {
            //                    var item = __enumerator5.Current;
            //                    {
            //                        CharacterBaseInfoDataModel varitem = new CharacterBaseInfoDataModel(item.Value);
            //                        string str = "";
            //                        if (item.Value.Online == 1)
            //                        {
            //                            varitem.ColorOnLine = white;
            //                        }
            //                        else
            //                        {
            //                            varitem.ColorOnLine = Gray;
            //                        }
            //                        varitem.State = 0;
            //                        varList.Add(varitem);
            //                    }
            //                }
            //            }
            //            Info.ShowList = varList;
            //        }
            //        else
            //        {
            //            ObservableCollection<CharacterBaseInfoDataModel> varList = new ObservableCollection<CharacterBaseInfoDataModel>();
            //            {
            //                // foreach(var item in mUnionMembers)
            //                var __enumerator6 = (mUnionMembers).GetEnumerator();
            //                while (__enumerator6.MoveNext())
            //                {
            //                    var item = __enumerator6.Current;
            //                    {
            //                        if (item.Value.Online == 0)
            //                        {
            //                            continue;
            //                        }
            //                        CharacterBaseInfoDataModel varitem = new CharacterBaseInfoDataModel(item.Value);
            //                        varitem.ColorOnLine = white;
            //                        varitem.State = 0;
            //                        varList.Add(varitem);
            //                    }
            //                }
            //            }
            //            Info.ShowList = varList;
            //        }
            //    }
            //}
            //else

            #endregion

            if (type == 1) //1 申请信息
            {
                //Info.ListState = 2;
                //Info.JoinShow = 1;
                m_Info.ShowDetail = 0;
                //Info.ShowList.Clear();
                //if (BattleData.MyUnion.ApplyList != null)
                //{
                //    ObservableCollection<CharacterBaseInfoDataModel> varList = new ObservableCollection<CharacterBaseInfoDataModel>();
                //    {
                //        // foreach(var item in BattleData.MyUnion.ApplyList)
                //        var __enumerator7 = (BattleData.MyUnion.ApplyList).GetEnumerator();
                //        while (__enumerator7.MoveNext())
                //        {
                //            var item = __enumerator7.Current;
                //            {
                //                CharacterBaseInfoDataModel varitem = new CharacterBaseInfoDataModel(item);
                //                varitem.Selected = 0;
                //                varitem.ColorOnLine = white;
                //                varitem.State = 1;
                //                varList.Add(varitem);
                //            }
                //        }
                //    }
                //    Info.ShowList = varList;
                //}
            }
            else if (type == 2) //成员详细列表
            {
                m_Info.MembersDetail.Clear();
                m_Info.ShowDetail = 1;
                var DetailList = new ObservableCollection<CharacterBaseInfoDataModel>();
                {
                    // foreach(var item in mUnionMembers)
                    var _enumerator8 = (m_dicUnionMembers).GetEnumerator();
                    while (_enumerator8.MoveNext())
                    {
                        var _item = _enumerator8.Current;
                        {
                            _item.Value.ListShow = 1;
                            if (!m_BattleData.CheckBox.ShowOffLineDetail)
                            {
                                if (_item.Value.Online == 1)
                                {
                                    var _data = new CharacterBaseInfoDataModel(_item.Value);
                                    _data.ColorOnLine = _white;
                                    _item.Value.ListShow = 1;
                                    DetailList.Add(_data);
                                }
                            }
                            else
                            {
                                var _data = new CharacterBaseInfoDataModel(_item.Value);
                                if (_item.Value.Online == 1)
                                {
                                    _data.ColorOnLine = _white;
                                }
                                else
                                {
                                    _data.ColorOnLine = _Gray;
                                }
                                _item.Value.ListShow = 1;
                                DetailList.Add(_data);
                            }
                        }
                    }
                }
                m_Info.MembersDetail = DetailList;
            }
        }

        //修改公告
        private void AlterAfficheButton()
        {
            if (m_Info.VarNotice == string.Empty)
            {
                return;
            }
       
            if (GameUtils.CheckSensitiveName(m_Info.VarNotice))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 220990);
                return;
            }

            //公告内容没有改变
            if (m_Info.VarNotice == m_BattleData.MyUnion.Notice)
            {
                m_Info.IsNotice = 0;
                var _e = new ShowUIHintBoard(220922);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            //字数判断
            if (m_Info.VarNotice.Length > 120)
            {
                var _e = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(220962), 120));
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            NetManager.Instance.StartCoroutine(ChangedAllianceAfficheCoroutine(m_BattleData.MyUnion.UnionID, m_Info.VarNotice));
        }

        private IEnumerator ChangedAllianceAfficheCoroutine(int UnionID, string content)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ChangeAllianceNotice(UnionID, content);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_Info.IsNotice = 0;
                        var _e = new ShowUIHintBoard(220922);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //公告修改时，显示保存按钮
        private void AfficheSaveDisplay()
        {
            m_Info.IsNotice = 1;
        }

        //自动加入战盟checkbox
        private IEnumerator ChangedUnionAutoAddCoroutine(int BattleUnionId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _value = m_BattleData.MyUnion.AutoAccept == 0 ? 1 : 0;
                var _msg = NetManager.Instance.ChangeAllianceAutoJoin(BattleUnionId, _value);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_BattleData.MyUnion.AutoAccept = _value;
                        var _count = 0;
                        var _idList = new Uint64Array();
                        var _indexList = new List<int>();

                        for (var i = 0; i < m_BattleData.MyUnion.ApplyList.Count; i++)
                        {
                            var _item = m_BattleData.MyUnion.ApplyList[i];
                            _idList.Items.Add(_item.ID);
                            _indexList.Add(i);
                        }
                        if (_idList.Items.Count > 0)
                        {
                            NetManager.Instance.StartCoroutine(UnionAgreeMentApplyForListCoroutine(m_BattleData.MyUnion.UnionID, 0, _idList,
                                _indexList));
                        }
                        //自动加入战盟设置成功
                        var _e = new ShowUIHintBoard(220923);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        #endregion
        #region  战盟建设

        private IEnumerator EndowmentUnionPropCoroutine(int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.DonationAllianceItem(type);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_Build.NowCount++;
                        if (type == 0)
                        {
                            m_BattleData.MyUnion.Money += m_GuildRecord.LessUnionMoney;
                            m_BattleData.MyPorp.DonationCount += m_GuildRecord.LessGetGongji;
                            var ee = new PlayBattleUnionEffect(type);
                            EventDispatcher.Instance.DispatchEvent(ee);
                        }
                        else if (type == 1)
                        {
                            m_BattleData.MyUnion.Money += m_GuildRecord.MoreUnionMoney;
                            m_BattleData.MyPorp.DonationCount += m_GuildRecord.MoreGetGongji;
                            var ee = new PlayBattleUnionEffect(type);
                            EventDispatcher.Instance.DispatchEvent(ee);
                        }
                        else if (type == 2)
                        {
                            m_BattleData.MyUnion.Money += m_GuildRecord.DiaUnionMoney;
                            m_BattleData.MyPorp.DonationCount += m_GuildRecord.DiamondGetGongji;
                            var ee = new PlayBattleUnionEffect(type);
                            EventDispatcher.Instance.DispatchEvent(ee);
                        }
                        SetFightMoneyStr();
                        var _e = new ShowUIHintBoard(220924);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //捐赠物品
        private IEnumerator ButtonEndowmentPropCoroutine(int taskID, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.DonationAllianceItem(taskID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_Build.DonationItem[index].LeftCount = m_Build.DonationItem[index].TotalCount - _msg.Response;
                        var _tbGuildMission = Table.GetGuildMission(m_Build.DonationItem[index].TaskID);
                        if (_tbGuildMission == null)
                        {
                            yield break;
                        }
                        m_BattleData.MyUnion.Money += _tbGuildMission.GetMoney;
                        SetFightMoneyStr();
                        m_BattleData.MyPorp.DonationCount += _tbGuildMission.GetGongJi;
                        m_Build.TodayDonation += _tbGuildMission.GetGongJi;
                        if (m_Build.DonationItem[index].LeftCount == 0)
                        {
                            NetManager.Instance.StartCoroutine(AcquireEndowmentMsgCoroutine(m_BattleData.MyUnion.UnionID));
                            //Build.DonationItem[index].State = (int) ItemState.Wait;
                            //Build.DonationItem[index].NextTime = Game.Instance.ServerTime.ToBinary();
                            //SteDonationIitemNull(index);
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                        NetManager.Instance.StartCoroutine(AcquireEndowmentMsgCoroutine(m_BattleData.MyUnion.UnionID));
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //捐献时间刷新
        private void SetEndowmentPropNull(int index)
        {
            var _item = m_Build.DonationItem[index];
            _item.ItemIDData.ItemId = -1;

            var _vartime = Extension.FromServerBinary(_item.NextTime);
            if (_vartime >= Game.Instance.ServerTime)
            {
                if (_item.TimerCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(_item.TimerCoroutine);
                }
                _item.TimerCoroutine = NetManager.Instance.StartCoroutine(EndowmentTimeCoroutine(_vartime, index));
            }
        }

        //捐赠物品刷新
        private IEnumerator EndowmentTimeCoroutine(DateTime time, int index)
        {
            if (time < Game.Instance.ServerTime)
            {
                yield break;
            }
            var _str = GameUtils.GetDictionaryText(220977);
            while (time > Game.Instance.ServerTime)
            {
                yield return new WaitForSeconds(1.0f);

                m_Build.DonationItem[index].RefleshTime = GameUtils.GetTimeDiffString(time) + "\r\n" + _str;
            }
            yield return new WaitForSeconds(2f); //延迟2秒
            NetManager.Instance.StartCoroutine(AcquireEndowmentMsgCoroutine(m_BattleData.MyUnion.UnionID));
        }

        //请求战盟捐赠物品列表
        private IEnumerator AcquireEndowmentMsgCoroutine(int UnionID)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceMissionData(UnionID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SetEndowmentProp(_msg.Response.Missions);
                        m_listRefleshTime[1] = Game.Instance.ServerTime.AddSeconds(m_iINTERVEL_TIME);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //显示日志
        private void ButttonDisplayLog()
        {
            m_Build.ShowDonation = 1;
            NetManager.Instance.StartCoroutine(ApplyForUnionEndowmentListCoroutine(m_BattleData.MyUnion.UnionID));
        }

        private IEnumerator ApplyForUnionEndowmentListCoroutine(int BattleUnionId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceDonationList(BattleUnionId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_Build.LogList.Clear();
                        var _getstr = GameUtils.GetDictionaryText(220946);
                        var _mList = new List<BattleUnionDonationLogDataModel>();
                        {
                            // foreach(var data in msg.Response.Datas)
                            var _enumerator9 = (_msg.Response.Datas).GetEnumerator();
                            while (_enumerator9.MoveNext())
                            {
                                var _data = _enumerator9.Current;
                                {
                                    var _i = new BattleUnionDonationLogDataModel();
                                    var _str = Extension.FromServerBinary(_data.Time).ToString("yyyy年MM月dd日");
                                    var _tbBaseItem = Table.GetItemBase(_data.ItemId);
                                    //if (tbBaseItem != null)
                                    //{
                                    //  i.Label = string.Format(GameUtils.GetDictionaryText(220863), str , data.Name, data.Count, tbBaseItem.Name);
                                    //    Build.LogList.Add( i);
                                    //}
                                    //test
                                    if (_tbBaseItem != null)
                                    {
                                        if (_data.ItemId == 2 || _data.ItemId == 3)
                                        {
                                            _i.Label = _str + ": " + "[4FC012]" + _data.Name + "[-]" +
                                                       _getstr + "[4FC012]" + _data.Count + "[-]" + _tbBaseItem.Name;
                                        }
                                        else
                                        {
                                            _i.Label = _str + ": " + "[4FC012]" + _data.Name + "[-]" +
                                                       _getstr + "[4FC012]" + _tbBaseItem.Name + "[-]";
                                        }
                                        _mList.Insert(0, _i);
                                    }
                                    m_Build.LogList = new ObservableCollection<BattleUnionDonationLogDataModel>(_mList);
                                }
                            }
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //关闭日志
        private void ButtonClosedDisplayLog()
        {
            m_Build.ShowDonation = 0;
        }
   
        //显示帮助
        private void BuiltDisplayAssist()
        {
            m_Build.ShowHelp = m_Build.ShowHelp == 1 ? 0 : 1;
        }

        #endregion
        #region 战盟升级

        //buff页面显示
        private void ButtonAllianceBuffUpDisplay()
        {
            var _itemSelect = m_Buff.BuffList[m_BuffSelected];
            GuildBuffRecord _tbGuildBuff = null;
            if (_itemSelect.BuffID == 0 || _itemSelect.BuffID == -1)
            {
                _tbGuildBuff = Table.GetGuildBuff(m_listBuffIdInit[m_BuffSelected]);
            }
            else
            {
                var _tb = Table.GetGuildBuff(_itemSelect.BuffID);
                //已经是最高级
                if (_tb.NextLevel == -1)
                {
                    var _ee = new ShowUIHintBoard(220934);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
                _tbGuildBuff = Table.GetGuildBuff(_tb.NextLevel);
            }
            if (_tbGuildBuff == null)
            {
                return;
            }
            //等级不足
            if (PlayerDataManager.Instance.GetLevel() < _tbGuildBuff.LevelLimit)
            {
                var _ee = new ShowUIHintBoard(210110);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }

            if (m_BattleData.MyUnion.Level < _tbGuildBuff.NeedUnionLevel)
            {
                //升级buff需要战盟等级为{0}级
                var _e = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(220963), _tbGuildBuff.NeedUnionLevel));
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            //功绩不足
            if (_tbGuildBuff.UpConsumeGongji > PlayerDataManager.Instance.GetRes((int)eResourcesType.Contribution))
            {
                var _e = new ShowUIHintBoard(210111);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            //m_Buff.UpConsume = _tbGuildBuff.UpConsumeGongji.ToString();
            //m_Buff.ShowUpUI = 1;
            ButtonBuffLvUpSuccess();
        }

        //战盟是否可升级
        private void IsAllianceLv()
        {
            if (m_BattleData.MyUnion.Money >= m_GuildRecord.ConsumeUnionMoney)
            {
                m_Info.CanLevel = 1;
            }
            else
            {
                m_Info.CanLevel = 0;
            }
            if (m_GuildRecord.ConsumeUnionMoney <= 0)
            {
                m_Info.CanLevel = -1;
            }
            m_Buff.NeedDonation = m_GuildRecord.ConsumeUnionMoney;
        }

        private void InitalBufName()
        {
            var _count = m_Buff.BuffList.Count;
            for (var i = 0; i < _count; i++)
            {
                var _tbBuff = Table.GetBuff(500 + i);
                if (_tbBuff == null)
                {
                    return;
                }
                m_Buff.BuffList[i].Name = _tbBuff.Name;
            }
        }

        //刷新buff页面
        private void RenewalBuffPage(int index)
        {
            m_Buff.BuffList[m_BuffSelected].Selected = false;
            var _selectItem = m_Buff.BuffList[index];
            _selectItem.Selected = true;
            m_BuffSelected = index;
            m_Buff.ShowMinMaxStr = 0; //当前级别和下一级都显示。
            GuildBuffRecord tbGuildBuff = null;
            if (_selectItem.BuffID <= 0)
            {
                tbGuildBuff = Table.GetGuildBuff(m_listBuffIdInit[index]);
                m_Buff.BuffBtnStr = GameUtils.GetDictionaryText(270257);
                m_Buff.BuffEffect2 = tbGuildBuff.Desc;
                m_Buff.BuffNextLevel = tbGuildBuff.BuffLevel;
                m_Buff.BuffNextID = m_listBuffIdInit[index];
                m_Buff.ShowMinMaxStr = 1; //显示minstr
            }
            else
            {
                var _tb = Table.GetGuildBuff(_selectItem.BuffID);
                m_Buff.BuffEffect1 = _tb.Desc;
                m_Buff.BuffLevel = _tb.BuffLevel;
                if (_tb.NextLevel == -1)
                {
                    m_Buff.ShowMinMaxStr = 2; //显示Maxstr
                    return;
                }
                tbGuildBuff = Table.GetGuildBuff(_tb.NextLevel);
                m_Buff.BuffEffect2 = tbGuildBuff.Desc;
                m_Buff.BuffNextLevel = tbGuildBuff.BuffLevel;
                m_Buff.BuffNextID = tbGuildBuff.Id;
                m_Buff.BuffBtnStr = GameUtils.GetDictionaryText(270258);
            }


            var _tbBuff = Table.GetBuff(tbGuildBuff.BuffID);
            if (_tbBuff == null)
            {
                return;
            }
            _selectItem.Name = _tbBuff.Name;
            m_Buff.BuffName = _selectItem.Name;
            m_Buff.NeedGongji = tbGuildBuff.UpConsumeGongji;
            if (PlayerDataManager.Instance.GetLevel() >= tbGuildBuff.LevelLimit)
            {
                m_Buff.NeedCharacterLevel = "[4BE127]" + "Lv." + tbGuildBuff.LevelLimit + "[-]";
            }
            else
            {
                m_Buff.NeedCharacterLevel = "[FF0000]" + "Lv." + tbGuildBuff.LevelLimit + "[-]";
            }
            if (m_BattleData.MyUnion.Level >= tbGuildBuff.NeedUnionLevel)
            {
                m_Buff.NeedBattleLevel = "[4BE127]" + "Lv." + tbGuildBuff.NeedUnionLevel + "[-]";
            }
            else
            {
                m_Buff.NeedBattleLevel = "[FF0000]" + "Lv." + tbGuildBuff.NeedUnionLevel + "[-]";
            }
        }

        //buff升级按钮
        private void ButtonBuffLvUpSuccess()
        {
            NetManager.Instance.StartCoroutine(UpUnionBuffCoroutine(m_BattleData.MyUnion.UnionID, m_Buff.BuffNextID));
        }

        private IEnumerator UpUnionBuffCoroutine(int UnionID, int BuffID)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.UpgradeAllianceBuff(BuffID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_Buff.BuffList[m_BuffSelected].BuffID = BuffID;
                        var ee = new BattleUnionBuffUp(BuffID);
                        EventDispatcher.Instance.DispatchEvent(ee);
                        RenewalBuffPage(m_BuffSelected);
                        m_Buff.ShowUpUI = 0;
                        var _e = new ShowUIHintBoard(220975);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //战盟升级
        private void ButtonAllianceLvUp()
        {
            //战盟贡献度要大于消耗
            if (m_BattleData.MyUnion.Money < m_GuildRecord.ConsumeUnionMoney)
            {
                var _e = new ShowUIHintBoard(220930);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }

            //升级战盟等级需要消耗战盟贡献度{0}，确认升级么？
            var _str = string.Format(GameUtils.GetDictionaryText(220976), m_GuildRecord.ConsumeUnionMoney);
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _str, "",
                () => { NetManager.Instance.StartCoroutine(ButtonAllianceLvUpCoroutine(m_BattleData.MyUnion.UnionID)); }
                );
        }

        private IEnumerator ButtonAllianceLvUpCoroutine(int unionID)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.UpgradeAllianceLevel(unionID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new UIEvent_UnionAnim());

                        var _e = new ShowUIHintBoard(220932);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        //buff升级页面关闭
        private void ButtonLvUpBuffClose()
        {
            m_Buff.ShowUpUI = 0;
        }

        #endregion
        #region 商店

        private IEnumerator StoreBuyCoroutine(int ShopId, int count, int index)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.StoreBuy(ShopId, count, -1);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        //购买成功
                        var _tbShop = Table.GetStore(m_Shop.ShopList[index].ShopID);
                        if (_tbShop == null)
                        {
                            yield break;
                        }
                        m_Shop.ShopList[index].BuyCount --;
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        #endregion
        #region boss

        //boss获得奖励
        private void ButtonBossAcquireAward()
        {
        }

        #endregion
        #region 攻城战

        //加入城战
        private void AddAttempt()
        {
            var _mErrorCode = ExamineAddAttenpt();
            switch (_mErrorCode)
            {
                case 1:
                {
                    //"你没有参赛资格！"
                    var ee = new ShowUIHintBoard(270279);
                    EventDispatcher.Instance.DispatchEvent(ee);
                }
                    return;
                //无人竞标，本次城战取消s
                case 2:
                {
                    var ee = new ShowUIHintBoard(200005038);
                    EventDispatcher.Instance.DispatchEvent(ee);
                }
                    return;
                case 3:
                {
                    //"攻城战准备中！"
                    var ee = new ShowUIHintBoard(270280);
                    EventDispatcher.Instance.DispatchEvent(ee);
                }
                    return;
                case 4:
                {
                    //"等级小于100级不能参加攻城战！"
                    var ee = new ShowUIHintBoard(270281);
                    EventDispatcher.Instance.DispatchEvent(ee);
                }
                    return;
                case 5:
                {
                }
                    return;
            }
            NetManager.Instance.StartCoroutine(GoInUnionFightCoroutine());
        }

        //进入城战条件检查
        private int ExamineAddAttenpt()
        {
            if (m_BattleData.MyUnion.UnionID <= 0)
                return 1;
            var _battleCityDic = PlayerDataManager.Instance._battleCityDic;
            var _isCanJoin = false;
            var _unionId = m_BattleData.MyUnion.UnionID;
            foreach (var item in _battleCityDic)
            {
                if (item.Key == _unionId)
                {
                    _isCanJoin = true;
                    break;
                }
            }

            if (!_isCanJoin && m_AttackCity.CastellanId != m_BattleData.MyUnion.UnionID)
                return 1;

            _isCanJoin = false;
            for (var i = 0; i < m_AttackCity.AttackName.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_AttackCity.AttackName[i]))
                {
                    _isCanJoin = true;
                    break;
                }
            }
            if (!_isCanJoin)
            {
                return 2;
            }

            if (m_AttackCity.OpenState != (int)eAllianceWarState.WaitStart &&
                m_AttackCity.OpenState != (int)eAllianceWarState.Fight)
            {
                return 3;
            }

            if (PlayerDataManager.Instance.GetLevel() < Table.GetClientConfig(1153).Value.ToInt())
            {
                return 4;
            }

            if (SceneManager.Instance.CurrentSceneTypeId == s_iFuBenId)
            {
                return 5;
            }
            return 0;
        }

        //请求攻城战数据
        private IEnumerator GoInUnionFightCoroutine()
        {
            var _unionId = m_BattleData.MyUnion.UnionID;
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.EnterAllianceWar(_unionId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_FubenNotInOpenTime)
                    {
                        ApplyForUnionFightInfo();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        //攻城战初始化
        private void InitialAttempt()
        {
            m_iaddPerCount = Table.GetClientConfig(902).Value.ToInt();
            m_ilimitMin = Table.GetClientConfig(903).Value.ToInt();
            m_AttackCity.BindTips = string.Format(GameUtils.GetDictionaryText(270284), m_ilimitMin, m_iaddPerCount);
            m_AttackCity.TitleItem.Id = s_iTitleId;
            GameUtils.TitleAddAttr(m_AttackCity.TitleItem, Table.GetNameTitle(s_iTitleId));
            for (var i = 0; i < 4; i++)
            {
                m_AttackCity.EldersName[i] = string.Empty;
                if (i < 2)
                {
                    m_AttackCity.AttackName[i] = string.Empty;
                }
            }

            var _lists = new ReadonlyList<BattleUnionRewardItemDataModel>(4);
            for (var j = 0; j < 4; j++)
            {
                var _item = new BattleUnionRewardItemDataModel();
                var _tbGuildAccess = Table.GetGuildAccess(j);
                var _tbMail = Table.GetMail(_tbGuildAccess.MailId);
                for (var k = 0; k < 4; k++)
                {
                    var _tt = new ItemIconDataModel();
                    _tt.ItemId = _tbMail.ItemId[k];
                    _tt.Count = _tbMail.ItemCount[k];
                    _item.Items[k] = _tt;
                }
                _lists[3 - j] = _item;
            }
            m_AttackCity.RewardItems = _lists;
        }

        private void ApplyForUnionFightInfo()
        {
            NetManager.Instance.StartCoroutine(ApplyForUnionFightInfoCoroutine());
        }
    
        //请求攻城战数据
        private IEnumerator ApplyForUnionFightInfoCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceWarData(PlayerDataManager.Instance.ServerId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _nonStr = GameUtils.GetDictionaryText(100002286);
                        m_AttackCity.ViceCastellanName = _nonStr;
                        m_AttackCity.CastellanName = _nonStr;
                        for (var i = 0; i < m_AttackCity.EldersName.Count; i++)
                        {
                            m_AttackCity.EldersName[i] = _nonStr;
                        }
                        var _response = _msg.Response;
                        m_AttackCity.BiddingCountStr = string.Format(GameUtils.GetDictionaryText(270263), _response.SignUpCount);
                        m_AttackCity.BiddingCount = _response.SignUpCount;
                        var _memberCount = _response.Members.Count;
                        var _count = 0;
                        for (var i = 0; i < _memberCount; i++)
                        {
                            var _item = _response.Members[i];
                            if (_item.Ladder == 3)
                            {
                                m_AttackCity.CastellanName = _item.Name;
                                m_ulongModelGuid = _item.Guid;
                            }
                            else if (_item.Ladder == 2)
                            {
                                m_AttackCity.ViceCastellanName = _item.Name;
                            }
                            else
                            {
                                m_AttackCity.EldersName[_count] = _item.Name;
                                _count++;
                            }
                        }
                        if (m_listRefleshTime.Count > 0)
                        {
                            m_listRefleshTime[5] = Game.Instance.ServerTime.AddSeconds(m_iINTERVEL_TIME);
                        }
                        m_AttackCity.OpenState = _response.State;
                        RenewalStormACastleState(_response);
                        if (m_ulongModelGuid > 0)
                        {
                            PlayerDataManager.Instance.ApplyPlayerInfo(m_ulongModelGuid, RenewalCharacterOne);
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        //请求城主信息
        private void ApplyForUnionFightCastellanInfo()
        {
            NetManager.Instance.StartCoroutine(ApplyForUnionFightCastellanInfoCoroutine());
        }

        private void EliminateDefierInfo()
        {
            var _battleCityDic = PlayerDataManager.Instance._battleCityDic;
            var _removeList = new List<int>();
            foreach (var i in _battleCityDic)
            {
                if (i.Value.Type == 1)
                {
                    _removeList.Add(i.Key);
                }
            }
            for (var i = 0; i < _removeList.Count; i++)
            {
                _battleCityDic.Remove(_removeList[i]);
            }

            for (var i = 0; i < m_AttackCity.AttackName.Count; i++)
            {
                m_AttackCity.AttackName[i] = string.Empty;
            }
        }

        //请求城主信息
        private IEnumerator ApplyForUnionFightCastellanInfoCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceWarOccupantData(PlayerDataManager.Instance.ServerId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        SetCastellanInfo(_msg.Response);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void SetCastellanInfo(AllianceWarOccupantData data)
        {
            var _unionId = m_BattleData.MyUnion.UnionID;
            var _battleCityDic = PlayerDataManager.Instance._battleCityDic;
            var _response = data;
            m_AttackCity.CastellanId = _response.OccupantId;
            var _nonStr = GameUtils.GetDictionaryText(100002286);
            if (_response.OccupantId > 0)
            {
                m_AttackCity.CastellanIsExist = 1;
            }
            else
            {
                m_AttackCity.CastellanIsExist = 0;
            }
            if (string.IsNullOrEmpty(_response.OccupantName))
            {
                m_AttackCity.CityUnionName = _nonStr;
            }
            else
            {
                m_AttackCity.CityUnionName = _response.OccupantName;
            }

            var _removeList = new List<int>();
            foreach (var i in _battleCityDic)
            {
                if (i.Value.Type == 0)
                {
                    _removeList.Add(i.Key);
                }
            }
            for (var i = 0; i < _removeList.Count; i++)
            {
                _battleCityDic.Remove(_removeList[i]);
            }

            if (_battleCityDic.ContainsKey(_response.OccupantId))
            {
                _battleCityDic.Remove(_response.OccupantId);
            }

            _battleCityDic[_response.OccupantId] = new PlayerDataManager.BattleCityData
            {
                Type = 0,
                Name = _response.OccupantName
            };

            //如果是竞标阶段，城主不显示竞标功能。
            if (_response.OccupantId == _unionId)
            {
                if (m_AttackCity.OpenState == (int)eAllianceWarState.WaitBid)
                {
                    m_AttackCity.OpenState = -1;
                }
            }
        }

        private void ApplyForUnionFightDefierInfo()
        {
            NetManager.Instance.StartCoroutine(ApplyForUnionFightDefierInfoCoroutine());
        }

        //请求攻城战数据
        private IEnumerator ApplyForUnionFightDefierInfoCoroutine()
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceWarChallengerData(PlayerDataManager.Instance.ServerId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        DefierInfoChange(_msg.Response);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private void DefierInfoChange(AllianceWarChallengerData data)
        {
            var _battleCityDic = PlayerDataManager.Instance._battleCityDic;
            var _removeList = new List<int>();
            foreach (var i in _battleCityDic)
            {
                if (i.Value.Type == 1)
                {
                    _removeList.Add(i.Key);
                }
            }
            for (var i = 0; i < _removeList.Count; i++)
            {
                _battleCityDic.Remove(_removeList[i]);
            }

            for (var i = 0; i < m_AttackCity.AttackName.Count; ++i)
            {
                if (i < data.ChallengerId.Count)
                {
                    var challengerId = data.ChallengerId[i];
                    var challengerName = data.ChallengerName[i];
                    m_AttackCity.AttackName[i] = challengerName;
                    _battleCityDic[challengerId] = new PlayerDataManager.BattleCityData {Type = 1, Name = challengerName};
                }
                else
                {
                    m_AttackCity.AttackName[i] = string.Empty;
                }
            }
            m_AttackCity.ShowAttackLabel = data.ChallengerId.Count > 0;
        }

        private void RenewalCharacterOne(PlayerInfoMsg info)
        {
            PlayerDataManager.Instance.BattleUnionMaster = info;

            var _e = new BattleUnionRefreshModelView(info);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        private void RenewalCharacterTwo(PlayerInfoMsg info)
        {
            PlayerDataManager.Instance.BattleUnionMaster = info;

            var _e = new BattleUnionRefreshModelViewLogic(info);
            EventDispatcher.Instance.DispatchEvent(_e);
        }

        private void RenewalStormACastleState(AllianceWarData msg)
        {
            bool isCareSpecialDay = false;
            isCareSpecialDay = GameUtils.GetOpenServerAct () != -1;

            var _mState = (eAllianceWarState)msg.State;
            var baseTime = Extension.FromServerBinary(msg.OpenTime);
            switch (_mState)
            {
                case eAllianceWarState.WaitBid:
                case eAllianceWarState.Bid:
                {
                  
                    var str = GameUtils.GetDictionaryText(270251);
                    m_AttackCity.OpenDayStr =  string.Format(GameUtils.GetDictionaryText(270271)," ", baseTime.ToString(str));
                    m_AttackCity.NextAttackTime =
                        GameUtils.GetTimeDiffString(baseTime);
                    m_AttackCity.BiddingFinishTime = baseTime.Date;
                }
                    break;
                case eAllianceWarState.WaitEnter:
                case eAllianceWarState.WaitStart:
                {
                    var _tbFuben = Table.GetFuben(s_iFuBenId);
                    if (_tbFuben == null)
                    {
                        return;
                    }
                    var _time = _tbFuben.OpenTime[0] / 100 * 60 + _tbFuben.OpenTime[0] % 100;
                    var _startTime = Game.Instance.ServerTime.Date.AddMinutes(_time);
                    var _timeReady = Game.Instance.ServerTime.Date.AddMinutes(_time + _tbFuben.CanEnterTime);


                    if (Game.Instance.ServerTime < _startTime)
                    {
                        if (m_attackCityStateTrigger != null)
                        {
                            TimeManager.Instance.DeleteTrigger(m_attackCityStateTrigger);
                            m_attackCityStateTrigger = null;
                        }
                        m_attackCityStateTrigger = TimeManager.Instance.CreateTrigger(_startTime, () =>
                        {
                            if (m_attackCityStateTrigger != null)
                            {
                                TimeManager.Instance.DeleteTrigger(m_attackCityStateTrigger);
                                m_attackCityStateTrigger = null;
                            }
                            m_AttackCity.OpenState = (int)eAllianceWarState.WaitStart;
                            if (SceneManager.Instance.CurrentSceneTypeId != s_iFuBenId)
                            {
                                if (ExamineAddAttenpt() != 0)
                                {
                                    return;
                                }                                                                 
                                var myUnionName = PlayerDataManager.Instance.BattleUnionDataModel.MyUnion.UnionName;
                                if(string.IsNullOrEmpty(myUnionName)) return;                                
                                for(int i = 0; i < m_AttackCity.AttackName.Count; i++)
                                {
                                    if (myUnionName.Equals(m_AttackCity.AttackName[i]) || m_BattleData.MyUnion.UnionID == m_AttackCity.CastellanId)
                                    {
                                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 300921, "", () => { AddAttempt(); });
                                    }
                                }
                            }
                        });
                    }
                    else if (Game.Instance.ServerTime < _timeReady)
                    {
                        if (m_attackCityStateTrigger != null)
                        {
                            TimeManager.Instance.DeleteTrigger(m_attackCityReadyTrigger);
                            m_attackCityReadyTrigger = null;
                        }
                        m_attackCityReadyTrigger = TimeManager.Instance.CreateTrigger(_timeReady, () =>
                        {
                            if (m_attackCityReadyTrigger != null)
                            {
                                TimeManager.Instance.DeleteTrigger(m_attackCityReadyTrigger);
                                m_attackCityReadyTrigger = null;
                            }
                            m_AttackCity.OpenState = (int)eAllianceWarState.Fight;
                            if (SceneManager.Instance.CurrentSceneTypeId != s_iFuBenId)
                            {
                                if (ExamineAddAttenpt() != 0)
                                {
                                    return;
                                }
                                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 300921, "", () => { AddAttempt(); });
                            }
                        });
                    }
                }
                    break;
                case eAllianceWarState.Fight:
                {
                }
                    break;
            }
        }

        //竞价
        private void ButtonAddingBid()
        {
            var _bidMoney = m_AttackCity.BiddingMoney;
            //选择显示竞价或者增价界面
            if (_bidMoney <= 0)
            {
                var _money = m_ilimitMin + m_iaddPerCount + m_GuildRecord.MaintainMoney;
                if (m_BattleData.MyUnion.Money < _money)
                {
                    //"战盟资金小于{0}，无法参与竞价"
                    var _e = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(270282), _money));
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }

                m_AttackCity.BiddingState = 2;
                m_AttackCity.BiddingTextMoney = m_ilimitMin;
                SettingVarFightMoney();
            }
            else
            {
                var _money = m_GuildRecord.MaintainMoney + m_iaddPerCount;
                if (m_BattleData.MyUnion.Money < _money)
                {
                    //"战盟资金小于{0}，无法参与竞价"
                    var _e = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(270282), _money));
                    EventDispatcher.Instance.DispatchEvent(_e);
                    return;
                }
                m_AttackCity.BiddingState = 3;
                m_AttackCity.BiddingTextMoney = m_iaddPerCount;
                m_AttackCity.BiddingTotalMoney = m_AttackCity.BiddingMoney + m_iaddPerCount;
                SettingVarFightMoney();
            }
            SettingButtonState();
        }

        //关闭竞价窗口
        private void CloseFightBid()
        {
            var _bidMoney = m_AttackCity.BiddingMoney;
            if (_bidMoney <= 0)
            {
                m_AttackCity.BiddingState = 0;
                m_AttackCity.BiddingTextMoney = m_ilimitMin;
            }
            else
            {
                m_AttackCity.BiddingState = 1;
            }
        }

        ////确定竞价
        //private void BattleBiddingOk()
        //{
        //    AttackCity.BiddingState = 0;
        //}


        //竞价
        private void ButtonBidSub()
        {
            NetManager.Instance.StartCoroutine(ApplyForBiddingCoroutine(m_AttackCity.BiddingTextMoney, 1));
        }

        private IEnumerator ApplyForBiddingCoroutine(int price, int type)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BidAllianceWar(price);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var _oldMoney = m_AttackCity.BiddingMoney;
                        m_AttackCity.BiddingMoney = _msg.Response;
                        if (type == 0)
                        {
                            if (_msg.Response <= 0) //竞价
                            {
                                m_AttackCity.BiddingTextMoney = m_ilimitMin;
                                //lable 出价
                                m_AttackCity.BiddingState = 0;
                            }
                            else
                            {
                                m_AttackCity.BiddingTextMoney = _msg.Response;
                                //lable 增价
                                m_AttackCity.BiddingState = 1;
                            }
                        }
                        else
                        {
                            m_AttackCity.BiddingState = 1;
                            m_BattleData.MyUnion.Money -= price;
                            if (_oldMoney <= 0)
                            {
                                m_AttackCity.BiddingCountStr = string.Format(GameUtils.GetDictionaryText(270263),
                                    m_AttackCity.BiddingCount + 1);
                            }
                            //"您已成功竞价，金额为：{0}"
                            var _ee =
                                new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(270283),
                                    m_AttackCity.BiddingMoney));
                            EventDispatcher.Instance.DispatchEvent(_ee);
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
            }
        }

        private bool OnAdding(int type)
        {
            if (type == 0)
            {
                if (m_AttackCity.BiddingTextMoney + m_iaddPerCount + m_GuildRecord.MaintainMoney > m_BattleData.MyUnion.Money)
                {
                    SettingButtonState();
                    return false;
                }
                m_AttackCity.BiddingTextMoney += m_iaddPerCount;
                m_AttackCity.BiddingTotalMoney += m_iaddPerCount;
                SettingVarFightMoney();
                SettingButtonState();
                return true;
            }
            return false;
        }

        private bool OnDelete(int type)
        {
            var _limit = 0;
            if (m_AttackCity.BiddingState == 2)
            {
                _limit = m_ilimitMin;
            }
            else
            {
                _limit = m_iaddPerCount;
            }

            if (type == 0)
            {
                if (m_AttackCity.BiddingTextMoney - m_iaddPerCount < _limit)
                {
                    SettingButtonState();
                    return false;
                }
                m_AttackCity.BiddingTextMoney -= m_iaddPerCount;
                m_AttackCity.BiddingTotalMoney -= m_iaddPerCount;
                SettingVarFightMoney();
                SettingButtonState();
                return true;
            }
            return false;
        }

        private void SettingVarFightMoney()
        {
            m_AttackCity.VarBattleMoney = m_BattleData.MyUnion.Money - m_AttackCity.BiddingTextMoney;
            if (m_AttackCity.BiddingTextMoney + m_iaddPerCount + m_GuildRecord.MaintainMoney > m_BattleData.MyUnion.Money)
            {
                //红
                m_AttackCity.VarBattleMoneyColor = GameUtils.GetTableColor(10);
            }
            else
            {
                //白
                m_AttackCity.VarBattleMoneyColor = GameUtils.GetTableColor(0);
            }
        }

        private void SettingButtonState()
        {
            if (m_AttackCity.BiddingTextMoney + m_iaddPerCount + m_GuildRecord.MaintainMoney > m_BattleData.MyUnion.Money)
            {
                m_AttackCity.BtnAddIsGray = true;
            }
            else
            {
                m_AttackCity.BtnAddIsGray = false;
            }
            var limit = 0;
            if (m_AttackCity.BiddingState == 2)
            {
                limit = m_ilimitMin;
            }
            else
            {
                limit = m_iaddPerCount;
            }
            if (m_AttackCity.BiddingTextMoney - m_iaddPerCount < limit)
            {
                m_AttackCity.BtnDelIsGray = true;
            }
            else
            {
                m_AttackCity.BtnDelIsGray = false;
            }
        }

        private IEnumerator BtnAddingOnClicCoroutine(int type)
        {
            var _pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(_pressCd);
                if (OnAdding(type) == false)
                {
                    NetManager.Instance.StopCoroutine(m_buttonPress);
                    m_buttonPress = null;
                    yield break;
                }
                if (_pressCd > 0.01)
                {
                    _pressCd = _pressCd * 0.8f;
                }
            }
            yield break;
        }

        private IEnumerator BtnDeleteOnClicCoroutine(int type)
        {
            var _pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(_pressCd);
                if (OnDelete(type) == false)
                {
                    NetManager.Instance.StopCoroutine(m_buttonPress);
                    m_buttonPress = null;
                    yield break;
                }
                if (_pressCd > 0.01)
                {
                    _pressCd = _pressCd * 0.8f;
                }
            }
            yield break;
        }

        #endregion
        #region 战盟仓库逻辑

        /// <summary>
        /// 获取可捐献列表
        /// </summary>
        private void GetDepotDonateInfo()
        {
            m_DepotDataModel.CanDonateItems.Clear();
            var equipDic = PlayerDataManager.Instance.PlayerDataModel.Bags.Bags[0];//获取装备数据
            EquipBaseRecord tbEquipBase = null;
            ItemBaseRecord tbItemBase = null;
            var canDonateItems = new ObservableCollection<BagItemDataModel>();
            for (int i = 0; i < equipDic.Items.Count; i++)
            {
                if (equipDic.Items[i].ItemId == -1)
                    continue;
                tbEquipBase = Table.GetEquipBase(equipDic.Items[i].ItemId);
                if (null == tbEquipBase)
                    continue;
                tbItemBase = Table.GetItemBase(equipDic.Items[i].ItemId);
                if (null == tbItemBase)
                    continue;
                if (tbItemBase.Quality < 3 || tbItemBase.Quality > 4)//装备品质限3，4，即史诗和传说
                    continue;
                if (tbItemBase.DonatePrice == -1)//捐赠值为-1，不可捐赠
                    continue;
                if (tbEquipBase.Ladder < 3)//该装备的阶数是否>=3
                    continue;
                if (equipDic.Items[i].Exdata.Binding == 1)//该装备是否绑定
                    continue;
                PlayerDataManager.Instance.GetBagItemFightPoint(equipDic.Items[i]);
                PlayerDataManager.Instance.RefreshEquipBagStatus(equipDic.Items[i]);
                canDonateItems.Add(equipDic.Items[i]);
            }

            var tempItems = new ObservableCollection<BagItemDataModel>();
            for (int i = 0; i < donateItemsCapacity; i++)
            {
                if (i < canDonateItems.Count)
                {
                    tempItems.Add(canDonateItems[i]);
                }
                else
                {
                    var item = new BagItemDataModel();
                    item.ItemId = -1;
                    tempItems.Add(item);
                }
            }
            m_DepotDataModel.CanDonateItems = tempItems;
        }

        private void OnOpenClearUp()
        {
            RefreshClearCountInfo();
            GetClearUpData();
        }

        private void GetClearUpData()
        {
            clearUpInfoDic.Clear();
            var quality = 0;
            var ladder = 0;
            var clearPoints = 0;
            var dicIndex = 0;
            var depotItems = m_DepotDataModel.DepotItems;
            EquipBaseRecord tbEquipBase = null;
            ItemBaseRecord tbItemBase = null;
            var numStr = GameUtils.GetDictionaryText(100001426);//：{0}件
            for (int i = 0; i < depotItems.Count; i++)
            {
                if (depotItems[i].ItemId == -1)
                    continue;
                tbEquipBase = Table.GetEquipBase(depotItems[i].ItemId);
                if (null == tbEquipBase)
                    return;
                ladder = tbEquipBase.Ladder;
                tbItemBase = Table.GetItemBase(depotItems[i].ItemId);
                if (null == tbItemBase)
                    return;
                quality = tbItemBase.Quality;
                if (quality == 3)
                {
                    dicIndex = 100002260;
                }
                else
                {
                    dicIndex = 100002268;
                }
                clearPoints = tbItemBase.GuildPionts;
                var decStr = GameUtils.GetDictionaryText(dicIndex + ladder);//[D83BFC]装备描述[-]
                if (clearUpInfoDic.ContainsKey(decStr))
                {
                    clearUpInfoDic[decStr].Num += 1;
                    clearUpInfoDic[decStr].NumStr = string.Format(numStr, clearUpInfoDic[decStr].Num.ToString());
                    clearUpInfoDic[decStr].ClearPoints += clearPoints;
                }
                else
                {
                    var clearUpData = new BattleUnionDepotClearUpDataModel();
                    clearUpData.Num += 1;
                    clearUpData.NumStr = string.Format(numStr, clearUpData.Num.ToString());
                    clearUpData.Ladder = ladder;
                    clearUpData.Quality = quality;
                    clearUpData.DecStr = decStr;
                    clearUpData.ClearPoints = clearPoints;
                    clearUpInfoDic.Add(decStr, clearUpData);
                }
            }
            var tempClearUpDataList = clearUpInfoDic.Values.ToList();
            tempClearUpDataList.Sort(ClearUpDataSort);
            m_DepotDataModel.ClearUpList = new ObservableCollection<BattleUnionDepotClearUpDataModel>(tempClearUpDataList);
        }

        /// <summary>
        /// 清理数据排序
        /// </summary>
        private static int ClearUpDataSort(BattleUnionDepotClearUpDataModel a, BattleUnionDepotClearUpDataModel b)
        {
            if (a.Ladder > b.Ladder)
            {
                return 1;
            }
            else if (a.Ladder < b.Ladder)
            {
                return -1;
            }
            else
            {
                if (a.Quality > b.Quality)
                {
                    return 1;
                }
                else if (a.Quality < b.Quality)
                {
                    return -1;
                }
            }
            return 0;
        }

        /// <summary>
        /// 查看捐赠装备信息
        /// </summary>
        /// <param name="ievent"></param>
        private void OnDonateItemClick(IEvent ievent)
        {
            var e = ievent as DonateItemClickEvent;
            if (null == e)
            {
                return;
            }
            curItemIndex = e.ItemIndex;
            var item = GetCanDonateItem(curItemIndex);
            if (item == null)
            {
                return;
            }
            GameUtils.ShowItemDataTip(item, eEquipBtnShow.Donate);
        }

        /// <summary>
        /// 查看仓库装备信息
        /// </summary>
        /// <param name="ievent"></param>
        private void OnDepotItemClick(IEvent ievent)
        {
            var e = ievent as DepotItemClickEvent;
            if (null == e)
            {
                return;
            }
            var item = GetDepotItem(e.Index);
            if (item == null)
            {
                return;
            }
            if (m_BattleData.MyPorp.Ladder == 3 || m_BattleData.MyPorp.Ladder == 2)
            {
                GameUtils.ShowItemDataTip(item, eEquipBtnShow.ChiefOperate);
            }
            else
            {
                GameUtils.ShowItemDataTip(item, eEquipBtnShow.TakeOut);
            }
        }

        private BagItemDataModel GetDepotItem(int nIndex)
        {
            var depot = m_DepotDataModel.DepotItems;
            if (depot == null)
            {
                return null;
            }
            if (depot.Count > nIndex && nIndex >= 0)
            {
                return depot[nIndex];
            }
            return null;
        }

        private BagItemDataModel GetCanDonateItem(int nIndex)
        {
            var depot = m_DepotDataModel.CanDonateItems;
            if (depot == null)
            {
                return null;
            }
            if (depot.Count > nIndex && nIndex >= 0)
            {
                return depot[nIndex];
            }
            return null;
        }

        private void OnBagChangeEvent(IEvent ievent)
        {
            GetDepotDonateInfo();//获取可捐献列表
            RefreshDepotItemStatus();//刷新仓库物品状态
        }

        private void RefreshDepotItemStatus()
        {
            var depotItems = m_DepotDataModel.DepotItems;
            var tempList = new List<BagItemDataModel>();
            for (int i = 0; i < depotItems.Count; i++)
            {
                var tempItem = depotItems[i];
                if (tempItem.ItemId != -1)
                {
                    PlayerDataManager.Instance.GetBagItemFightPoint(tempItem);
                    PlayerDataManager.Instance.RefreshEquipBagStatus(tempItem);
                }
                tempList.Add(tempItem);
            }
            m_DepotDataModel.DepotItems = new ObservableCollection<BagItemDataModel>(tempList);
        }

        private int tempClearUpCount = 0;
        private void OnDepotToggleChange(IEvent ievent)
        {
            var e = ievent as BattleUnionDepotCleanUpToggleEvent;
            if (null == e)
            {
                return;
            }
            var index = e.Index;
            m_DepotDataModel.ClearUpList[index].IsCheck = !m_DepotDataModel.ClearUpList[index].IsCheck;
            var isCheck = m_DepotDataModel.ClearUpList[index].IsCheck;

            if (!isCheck)//未选中
            {
                tempClearUpCount += e.Num;
                if (tempClearUpCount > m_DepotDataModel.DepotNowCount)
                {
                    tempClearUpCount = m_DepotDataModel.DepotNowCount;
                }
                m_DepotDataModel.DepotCleanUpCount = tempClearUpCount + "/" + m_DepotDataModel.DepotMax;
            }
            else//选中
            {
                tempClearUpCount -= e.Num;
                if (tempClearUpCount < 0)
                {
                    tempClearUpCount = 0;
                }
                m_DepotDataModel.DepotCleanUpCount = tempClearUpCount + "/" + m_DepotDataModel.DepotMax;
            }
        }

        /// <summary>
        /// 清理战盟仓库
        /// </summary>
        private void OnDepotClearBtnClick()
        {
            ApplyDepotData();
            var clearCount = 0;
            var clearPoints = 0;
            var clearUpInfo = new ClearUpInfo();
            var clearItems = m_DepotDataModel.ClearUpList;
            for (int i = 0; i < clearItems.Count; i++)
            {
                if (clearItems[i].IsCheck)
                {
                    var info = new ClearUpInfoSingle();
                    info.Ladder = clearItems[i].Ladder;
                    info.Quality = clearItems[i].Quality;
                    clearUpInfo.Infos.Add(info);
                    clearCount += clearItems[i].Num;
                    clearPoints += clearItems[i].ClearPoints;
                }
            }
            //判断仓库中是否有装备 是否有选中
            if (m_DepotDataModel.DepotItems.Count <= 0 || clearUpInfo.Infos.Count == 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002260));//没有可供清理的装备
                return;
            }
            if (m_BattleData.MyPorp.Ladder != 3 && m_BattleData.MyPorp.Ladder != 2)
                return;
            //确认是否清理
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                string.Format(GameUtils.GetDictionaryText(100002294), clearCount, clearPoints),//已选中[ADFF00]{0}件[-]装备，获得[FFFF00]战盟资金[-]:{1}\n确定要清理吗？
                "",
                () => { NetManager.Instance.StartCoroutine(DepotClearUp(m_BattleData.MyUnion.UnionID, clearUpInfo, clearPoints)); });

        }

        //<summary>
        //战盟仓库清理
        //</summary>
        private IEnumerator DepotClearUp(int UnionID, ClearUpInfo info, int clearPoints)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BattleUnionDepotClearUp(UnionID, info);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(100002262), clearPoints)));//清理成功：战盟资金 x{0}
                        ApplyDepotLogList();
                        ApplyDepotData();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);//网络连接超时
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        /// <summary>
        /// 捐赠装备到战盟仓库
        /// </summary>
        private void OnDonateBtnClick(IEvent ievent)
        {
            var e = ievent as BattleUnionDepot_Donate;
            if (null == e)
            {
                return;
            }
            var bagIndex = e.BagIndex;
            //判断是否还在战盟
            if (PlayerDataManager.Instance.GetExData(eExdataDefine.e282) <= 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002244));//你已不是战盟成员
                return;
            }
            var tbGuild = Table.GetGuild(m_BattleData.MyUnion.Level);
            if (null == tbGuild)
                return;
            //判断战盟仓库是否满
            if (m_DepotDataModel.DepotNowCount >= tbGuild.DepotCapacity)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002245));//战盟仓库已满，无法捐赠
                return;
            }
            NetManager.Instance.StartCoroutine(DepotDonate(bagIndex));
        }

        /// <summary>
        /// 仓库捐赠
        /// </summary>
        /// <param name="EquipId"></param>
        /// <returns></returns>
        private IEnumerator DepotDonate(int bagIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BattleUnionDonateEquip(bagIndex);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        ApplyDepotLogList();//成功后请求仓库操作记录
                        ApplyDepotData();//成功后请求仓库数据
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotIsFull)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002245));//战盟仓库已满，无法捐赠  
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotItemChanged)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001498));//战盟仓库道具发生变化，请刷新后再进行捐赠
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotDonateWrongQuality)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001499));//战盟仓库只能捐赠紫色和橙色品质装备
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotDonateWrongLadder)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001500));//战盟仓库只能捐赠3阶及3阶以上装备 
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotDonateWrongBinding)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001501));//战盟仓库无法捐赠已绑定装备
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);//网络连接超时
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        private void OnRemoveBtnClick(IEvent ievent)
        {
            var e = ievent as BattleUnionDepot_Remove;
            if (null == e)
            {
                return;
            }
            var itemId = e.ItemId;
            var bagIndex = e.BagIndex;
            var tbItemBase = Table.GetItemBase(itemId);
            if (null == tbItemBase)
                return;
            var removePoints = tbItemBase.GuildPionts;
            if (m_BattleData.MyPorp.Ladder != 3 && m_BattleData.MyPorp.Ladder != 2)
                return;
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                string.Format(GameUtils.GetDictionaryText(100002293), removePoints), //清理后装备消失，获得[FFFF00]战盟资金[-]:{0}\n确定要清理吗？
                "",
                () => { NetManager.Instance.StartCoroutine(DepotRemoveItem(m_BattleData.MyUnion.UnionID, itemId, bagIndex, removePoints)); });
        }

        private IEnumerator DepotRemoveItem(int UnionID, int itemId, int bagIndex, int removePoints)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BattleUnionRemoveDepotItem(UnionID, itemId, bagIndex);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(100002262), removePoints)));//清理成功：战盟资金 x{0}
                        ApplyDepotLogList();//成功后请求仓库操作记录
                        ApplyDepotData();//成功后请求仓库数据
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotItemChanged)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002255));//该道具状态发生改变，请刷新后查看
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);//网络连接超时
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        /// <summary>
        /// 从战盟仓库中取出装备
        /// </summary>
        /// <param name="ievent"></param>
        private void OnTakeOutBtnClick(IEvent ievent)
        {
            var e = ievent as BattleUnionDepot_TakeOut;
            if (null == e)
            {
                return;
            }
            var itemId = e.ItemId;
            var bagIndex = e.BagIndex;
            var contribution = PlayerDataManager.Instance.PlayerDataModel.Bags.Resources.Contribution;
            var tbItembase = Table.GetItemBase(itemId);
            if (null == tbItembase)
                return;
            if (contribution < tbItembase.TakeoutPrice)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200002990));//贡献不足
                return;
            }
            var itemName = GameUtils.GetItemNameColorString(itemId);
            //判断是否还在战盟
            if (PlayerDataManager.Instance.GetExData(eExdataDefine.e282) <= 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002244));//你已不是战盟成员
                return;
            }
            //判断背包是否满
            if (PlayerDataManager.Instance.GetRemaindCapacity(eBagType.Equip) <= 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(302));
                return;
            }
            var tipStr = string.Format(GameUtils.GetDictionaryText(100002279), tbItembase.TakeoutPrice, itemName);
            //确认是否消耗贡献值取出
            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                tipStr,
                "",
                () => { NetManager.Instance.StartCoroutine(DepotTakeOut(itemId, bagIndex)); });
        }

        /// <summary>
        /// 仓库取出
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="bagIndex"></param>
        /// <returns></returns>
        private IEnumerator DepotTakeOut(int itemId, int bagIndex)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BattleUnionTakeOutEquip(itemId, bagIndex);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        ApplyDepotLogList();//成功后请求仓库操作记录
                        ApplyDepotData();//成功后请求仓库数据
                    }
                    else if (_msg.ErrorCode == (int)ErrorCodes.Error_AllianceDepotItemChanged)
                    {
                        EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100002255));//该道具状态发生改变，请刷新后查看
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);//网络连接超时
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        /// <summary>
        /// 整理战盟仓库
        /// </summary>
        private void OnDepotArrangeBtnClick()
        {
            NetManager.Instance.StartCoroutine(DepotArrange(m_BattleData.MyUnion.UnionID));
        }

        private IEnumerator DepotArrange(int UnionID)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.BattleUnionDepotArrange(UnionID);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        ApplyDepotLogList();//成功后请求仓库操作记录
                        ApplyDepotData();//成功后请求仓库数据
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);//网络连接超时
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        /// <summary>
        /// 打开捐献窗口
        /// </summary>
        private void OnDepotDonateBtnClick()
        {
            GetDepotDonateInfo();//获取可捐献列表
        }

        /// <summary>
        /// 战盟仓库操作
        /// </summary>
        private void DepotOperationEvent(IEvent ievent)
        {
            var e = ievent as BattleUnionDepotOperation;
            if (null == e)
            {
                return;
            }
            var type = e.Type;
            switch (type)
            {
                case 1://整理
                {
                    OnDepotArrangeBtnClick();
                }
                    break;
                case 2://清理
                {
                    OnDepotClearBtnClick();
                }
                    break;
                case 3://打开捐献窗口
                {
                    OnDepotDonateBtnClick();
                }
                    break;
                case 4://打开清理窗口
                {
                    OnOpenClearUp();
                }
                    break;
            }
        }

        /// <summary>
        /// 获取战盟仓库信息
        /// </summary>
        private void ApplyAllDepotInfo()
        {
            ApplyDepotLogList();
            ApplyDepotData();
            IsShowClearUp();
        }

        private void IsShowClearUp()
        {
            //盟主和副盟主显示清理按钮
            if (m_BattleData.MyPorp.Ladder == 3 || m_BattleData.MyPorp.Ladder == 2)
            {
                m_DepotDataModel.IsShowClearUp = true;
            }
            else
            {
                m_DepotDataModel.IsShowClearUp = false;
            }
        }

        /// <summary>
        /// 获取战盟仓库日志信息
        /// </summary>
        private void ApplyDepotLogList()
        {
            NetManager.Instance.StartCoroutine(ApplyDepotLogListCoroutine(m_BattleData.MyUnion.UnionID));
        }

        /// <summary>
        /// 请求战盟仓库日志信息
        /// </summary>
        private IEnumerator ApplyDepotLogListCoroutine(int BattleUnionId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceDepotLogList(BattleUnionId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_DepotDataModel.LogList.Clear();
                        var logData = _msg.Response.Datas;
                        var logList = new List<BattleUnionDepotLogDataModel>();
                        for (int i = 0; i < logData.Count; i++)
                        {
                            var data = logData[i];
                            var logDataModel = new BattleUnionDepotLogDataModel();
                            var timeStr = Extension.FromServerBinary(data.Time).ToString("HH:mm:ss");
                            var nameStr = data.Name;
                            var type = data.Type;
                            var itemId = data.ItemId;
                            if (itemId != -1)
                            {
                                var itemName = GameUtils.GetItemNameColorString(itemId);
                                var log = GetDepotOperationStr(type, timeStr, nameStr, itemName);
                                logDataModel.Log = log;
                                logList.Add(logDataModel);
                            }
                            else
                            {
                                var log = GetDepotOperationStr(type, timeStr, nameStr, "");
                                logDataModel.Log = log;
                                logList.Add(logDataModel);
                            }
                        }
                        m_DepotDataModel.LogList = new ObservableCollection<BattleUnionDepotLogDataModel>(logList);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        /// <summary>
        /// 获取战盟仓库装备信息
        /// </summary>
        private void ApplyDepotData()
        {
            NetManager.Instance.StartCoroutine(ApplyDepotDataCoroutine(m_BattleData.MyUnion.UnionID));
        }

        /// <summary>
        /// 请求战盟仓库装备信息
        /// </summary>
        private IEnumerator ApplyDepotDataCoroutine(int BattleUnionId)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ApplyAllianceDepotData(BattleUnionId);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        var items = _msg.Response.DepotData.Items;
                        var itemList = new List<BagItemDataModel>();
                        m_DepotDataModel.DepotNowCount = 0;
                        for (int i = 0; i < items.Count; i++)
                        {
                            var bagItemData = new BagItemDataModel();
                            bagItemData.ItemId = items[i].ItemId;
                            bagItemData.Count = items[i].Count;
                            bagItemData.Index = i;
                            bagItemData.Exdata.InstallData(items[i].Exdata);
                            if (bagItemData.ItemId != -1)
                            {
                                m_DepotDataModel.DepotNowCount++;
                                PlayerDataManager.Instance.GetBagItemFightPoint(bagItemData);
                                PlayerDataManager.Instance.RefreshEquipBagStatus(bagItemData);
                            }
                            itemList.Add(bagItemData);
                        }
                        m_DepotDataModel.DepotItems = new ObservableCollection<BagItemDataModel>(itemList);
                        RefreshDepotCountInfo();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }

        /// <summary>
        /// 刷新仓库数量信息
        /// </summary>
        private void RefreshDepotCountInfo()
        {
            m_DepotDataModel.DepotMax = m_DepotDataModel.DepotItems.Count;
            m_DepotDataModel.DepotCount = m_DepotDataModel.DepotNowCount + "/" + m_DepotDataModel.DepotMax;
        }

        /// <summary>
        /// 刷新清理窗口数量信息
        /// </summary>
        private void RefreshClearCountInfo()
        {
            tempClearUpCount = m_DepotDataModel.DepotNowCount;
            m_DepotDataModel.DepotMax = m_DepotDataModel.DepotItems.Count;
            m_DepotDataModel.DepotCleanUpCount = m_DepotDataModel.DepotNowCount + "/" + m_DepotDataModel.DepotMax;
        }

        /// <summary>
        /// 战盟仓库操作类型
        /// </summary>
        private enum AllianceDepotOperationType
        {
            Donate = 0, //捐赠
            Takeout = 1, //取出
            ClearUp = 2,  //清理
            Arrange = 3  //整理
        }

        /// <summary>
        /// 战盟仓库日志信息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="timeStr"></param>
        /// <param name="name"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        private string GetDepotOperationStr(int type, string timeStr, string name, string itemName)
        {
            var logStr = "";
            var dicStr = "";
            switch (type)
            {
                case (int)AllianceDepotOperationType.Arrange:
                {
                    dicStr = GameUtils.GetDictionaryText(100001420);
                    logStr = string.Format(dicStr, timeStr, name, "");
                }
                    break;
                case (int)AllianceDepotOperationType.ClearUp:
                {
                    dicStr = GameUtils.GetDictionaryText(100002252);
                    logStr = string.Format(dicStr, timeStr, "");
                }
                    break;
                case (int)AllianceDepotOperationType.Donate:
                {
                    dicStr = GameUtils.GetDictionaryText(100001418);
                    logStr = string.Format(dicStr, timeStr, name, itemName);
                }
                    break;
                case (int)AllianceDepotOperationType.Takeout:
                {
                    dicStr = GameUtils.GetDictionaryText(100001419);
                    logStr = string.Format(dicStr, timeStr, name, itemName);
                }
                    break;
            }
            return logStr;
        }

        #endregion 战盟仓库逻辑

        #endregion

        #region 事件函数

        #region 消息的处理

        //战盟操作事件
        private void OnAllianceOperateEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionOperation;
            switch (_e.Type)
            {
                //case 0:
                //    BtnOtherUnion();   //其他战盟
                //    break;
                case 1:
                    ButtonAnotherGoBack(); //其他战盟页面返回
                    break;
                case 2:
                    ButtonApplyForJoinUnion(); //加入其他战盟
                    break;
                case 3:
                    ButtonApplyForList(); //战盟申请列表
                    break;
                //case 4:
                //    BtnMemberInfo();  //成员信息
                //    break;
                case 5:
                    ButtonAdditionMember(); //添加成员按钮事件
                    break;
                case 6:
                    AdditionMemberOkay(); //添加战盟成员按钮
                    break;
                case 7:
                    CloseAdditionMeberUI(); //关闭添加成员页面
                    break;
                case 8:
                    DataUIBackButton(); //ShowInfo(0)
                    break;
                case 9:
                    ButtonParticularReturn(); //详细信息返回
                    break;
                case 10:
                    AlterAfficheButton(); //修改公告
                    break;
                case 11:
                    ButttonDisplayLog(); //显示日志
                    break;
                case 12:
                    ButtonClosedDisplayLog(); //关闭日志
                    break;
                //case 13:
                //    BtnDonationItem();//捐献物品
                //    break;
                //case 14:
                //    BtnUnionBuffUpShow();  //buff页面显示
                //    break;
                //case 15:
                //    BtnUnionBuffActive();  //buff激活
                //    break;
                case 16:
                    ButtonBuffLvUpSuccess(); //buff升级按钮
                    break;
                case 17:
                    ButtonAllianceLvUp(); //战盟升级
                    break;
                case 18:
                    ButtonLvUpBuffClose(); //buff升级页面关闭
                    break;
                case 19:
                    ButtonBossAcquireAward(); //boss获得奖励
                    break;
                case 20:
                    TabQuitAlliance(); //退出战盟按钮
                    break;
                case 21:
                    AfficheSaveDisplay(); //公告修改时，显示保存按钮
                    break;
                case 22:
                    BuiltDisplayAssist(); //显示帮助
                    break;
                case 23:
                    BuildedDisplayCoinPage(); //显示捐赠金币page
                    break;
                case 24:
                    BuildedDisplayPropPage(); //显示捐赠物品page
                    break;
                case 25:
                    ButtonAllianceBuffUpDisplay(); //显示buff升级提示
                    break;
                case 26:
                    ButtonAddingBid(); //竞价
                    break;
                case 27:
                    AddAttempt(); //加入城战
                    break;
                //case 28:
                //    BattleBiddingOk();  //确定竞价
                //    break;
                case 29:
                    CloseFightBid(); //关闭竞价窗口
                    break;
                case 30:
                    ButtonBidSub(); //竞价确定
                    break;
                case 31:
                    EliminateCreationInput(); //清除创建名称
                    break;
                case 32:
                    InitialCreationInput(); //初始化创建名称
                    break;
            }
        }
        //sc 战盟等级变化更新。
        private void OnSynchronizeInfoChangeEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionSyncDataChange;
            if (_e.Type == 0)
            {
                m_BattleData.MyUnion.Money = _e.param2;
                m_BattleData.MyUnion.Level = _e.param1;
                m_GuildRecord = Table.GetGuild(m_BattleData.MyUnion.Level);
                m_iUnionLevelChanged = m_BattleData.MyUnion.Level;
                InitalStore();
                InitionBuff();
                RenewalAllianceLv();
                IsAllianceLv();
                RenewalBuffPage(m_BuffSelected);
            }
        }
        //设置被操作的人物id 如被邀请
        private void OnSettingoperatedCharacterIDEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionGetCharacterID;
            m_ulongCharacterID = _e.CharacterID;
        }
        //战盟消息通知SC包
        private void AllianceSyncMsgEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionJoinReply;
            // Logger.Info(String.Format("Type ={0},id ={1},name1 ={2},name2 ={3}",e.Type,e.AllianceId,e.Name1,e.Name2));
            switch (_e.Type)
            {
                case 0: //邀请加入战盟
                {
                    JoinAllianceRespond(_e.Name1, _e.AllianceId, _e.Name2);
                }
                    break;
                case 1:
                {
                }
                    break;
                case 2: //你被拒绝加入战盟{0} Name1 = 拒绝你的名称   Name2 = 战盟名
                {
                    if (GameLogic.Instance == null)
                    {
                        return;
                    }
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok,
                        string.Format(GameUtils.GetDictionaryText(220955), _e.Name1, _e.Name2));
                }
                    break;
                case 3: //被同意邀请后反馈  Name1 = 同意你的名称   Name2 = 战盟名
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok,
                                                   string.Format(GameUtils.GetDictionaryText(220978), _e.Name1, _e.Name2),
                                                   "",
                                                   () => { CanRenewalInfo(RefleshType.MemberDetailData); });
                }
                    break;
                case 4: //被拒绝邀请后反馈   Name1 = 拒绝你的名称   Name2 = 战盟名
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok,
                        string.Format(GameUtils.GetDictionaryText(220979), _e.Name1, _e.Name2));
                }
                    break;
                case 5: //邀请超时后反馈 Name1 = 拒绝你的名称   Name2 = 战盟名
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok,
                        string.Format(GameUtils.GetDictionaryText(220979), _e.Name1, _e.Name2));
                }
                    break;
                case 6: //有成员申请
                {
                    if (_e.AllianceId == 0)
                    {
                        PlayerDataManager.Instance.NoticeData.BattleList = false;
                    }
                    else
                    {
                        PlayerDataManager.Instance.NoticeData.BattleList = true;
                    }
                }
                    break;
            }
        }
        //列表处理函数 3同意申请 4拒绝申请 13邀请入盟 14提升领袖 15提升权限 16降低权限 17请出战盟
        private void OnCommunicateAskingEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionCommunication;
            m_ulongCharacterID = _e.CharacterId;
            var _accessId = -1; //权限ID（值）
            switch (_e.ListIndex)
            {
                case 3: //同意申请   state=1;
                    NetManager.Instance.StartCoroutine(ApplyForOperateRoleCoroutine(1, m_ulongMemberId));
                    break;

                case 4: //拒绝申请  state=1;
                    NetManager.Instance.StartCoroutine(ApplyForOperateRoleCoroutine(2, m_ulongMemberId));
                    break;

                case 13: //邀请入盟  
                {
                    NetManager.Instance.StartCoroutine(ApplyForOperateRoleCoroutine(0, m_ulongCharacterID));
                }
                    break;
                case 14: //提升领袖   state=0
                {
                    //无权提升为领袖
                    if (m_BattleData.MyPorp.Ladder != (int)battleAccess.Chief)
                    {
                        return;
                    }
                    var _msgStr = GameUtils.GetDictionaryText(220981);
                    UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _msgStr, "",
                        () =>
                        {
                            NetManager.Instance.StartCoroutine(SetPermissionCoroutine(m_BattleData.MyUnion.UnionID, m_ulongMemberId,
                                (int)battleAccess.Chief));
                        });
                }
                    break;
                case 15: //提升权限   state=0
                {
                    _accessId = AcquireAccessed(m_ulongMemberId);
                    if (_accessId == -1)
                    {
                        return;
                    }
                    if (m_BattleData.MyPorp.Ladder > _accessId)
                    {
                        if (!IsMembersAccessed(_accessId, _accessId + 1))
                        {
                            return;
                        }
                        var _tbAccess = Table.GetGuildAccess(_accessId + 1);
                        var _msgStr = string.Format(GameUtils.GetDictionaryText(220980), _tbAccess.Name);
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _msgStr, "", () =>
                        {
                            NetManager.Instance.StartCoroutine(SetPermissionCoroutine(m_BattleData.MyUnion.UnionID, m_ulongMemberId,
                                _accessId + 1));
                        });
                    }
                    else
                    {
                        var _ee = new ShowUIHintBoard(220901);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                    }
                }
                    break;
                case 16: //降低权限   state=0
                {
                    _accessId = AcquireAccessed(m_ulongMemberId);
                    if (_accessId == -1)
                    {
                        return;
                    }
                    //玩家已经是最低权限了
                    if (_accessId == (int)battleAccess.People0)
                    {
                        var _ee = new ShowUIHintBoard(220902);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                        return;
                    }
                    //需要副帮主以上权限
                    if (m_BattleData.MyPorp.Ladder > _accessId && m_BattleData.MyPorp.Ladder >= (int)battleAccess.AssistantChief)
                    {
                        if (!IsMembersAccessed(_accessId, _accessId - 1))
                        {
                            return;
                        }
                        var _tbAccess = Table.GetGuildAccess(_accessId - 1);
                        var _msgStr = string.Format(GameUtils.GetDictionaryText(220980), _tbAccess.Name);
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _msgStr, "",
                            () =>
                            {
                                NetManager.Instance.StartCoroutine(SetPermissionCoroutine(m_BattleData.MyUnion.UnionID, m_ulongMemberId,
                                    _accessId - 1));
                            });
                    }
                    else
                    {
                        var _ee = new ShowUIHintBoard(220901);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                    }
                }
                    break;
                case 17: //请出战盟
                {
                    _accessId = AcquireAccessed(m_ulongMemberId);
                    //需要副帮主以上权限
                    if (m_BattleData.MyPorp.Ladder > _accessId && m_BattleData.MyPorp.Ladder >= (int)battleAccess.AssistantChief)
                    {
                        var _msgStr = GameUtils.GetDictionaryText(220982);
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, _msgStr, "",
                            () =>
                            {
                                NetManager.Instance.StartCoroutine(SetPermissionCoroutine(m_BattleData.MyUnion.UnionID, m_ulongMemberId,
                                    -1));
                            });
                    }
                    else
                    {
                        var _ee = new ShowUIHintBoard(220904);
                        EventDispatcher.Instance.DispatchEvent(_ee);
                    }
                }
                    break;
            }
        }
        //ExDataInit时请求玩家战盟信息
        private void OnExDataInitialEvent(IEvent ievent)
        {
            //TODO
            var _unionId = PlayerDataManager.Instance.GetExData(eExdataDefine.e282);
            PlayerDataManager.Instance.GetCharacterBaseData().GuildId = _unionId;
            m_BattleData.MyUnion.UnionID = _unionId;
            if (_unionId > 0)
            {
                AcquireMineAllianceMsgByServersID(0, 0);
                ApplyForList();
            }
            ApplyForUnionFightInfo();
            ApplyForUnionFightCastellanInfo();
            ApplyForUnionFightDefierInfo();
        }

        //更新玩家扩展数据，包括申请的战盟，玩家的战盟变化等
        private void OnRenewalExDataEvent(IEvent ievent)
        {
            var _e = ievent as BattleUnionExdataUpdate;
            if (_e.Type == eExdataDefine.e282)
            {
                m_BattleData.MyUnion.UnionID = _e.Value;
                {
                    if (_e.Value <= 0)
                    {
                        m_BattleData.ShowWitchUI = 1;
                        //清空battleData数据
                        {
                            var _ee = new ShowUIHintBoard(220951);
                            EventDispatcher.Instance.DispatchEvent(_ee);
                            //UIManager.Instance.ShowMessage(MessageBoxType.Ok, GameUtils.GetDictionaryText(220951), "", () =>
                            //{
                            //});
                        }
                    }
                    else
                    {
                        if (m_UionState == UnionIDState.OtherJoin)
                        {
                            var _ee = new ShowUIHintBoard(220964);
                            EventDispatcher.Instance.DispatchEvent(_ee);
                            MainUIInition();
                            ClearApplyFlag();
                            PlatformHelper.UMEvent("Union", "Enter");
                            //UIManager.Instance.ShowMessage(MessageBoxType.Ok, GameUtils.GetDictionaryText(220964), "", () =>
                            //{
                            //     MainUIInit();
                            //});
                        }
                        m_UionState = UnionIDState.OtherJoin;
                    }
                }
            }
            if (_e.Type == eExdataDefine.e286)
            {
                m_OtherUnion.ApplyUnionList[0] = _e.Value;
            }
            if (_e.Type == eExdataDefine.e287)
            {
                m_OtherUnion.ApplyUnionList[1] = _e.Value;
            }
            if (_e.Type == eExdataDefine.e288)
            {
                m_OtherUnion.ApplyUnionList[2] = _e.Value;
            }
        }
        private void ClearApplyFlag()
        {//加入战盟后，清除对所有战盟的申请标记
            for (int i = 0; i < m_OtherUnion.OtherUnionList.Count; i++)
            {
                var item = m_OtherUnion.OtherUnionList[i];
                item.IsApplyJoin = 0;
            }
        }

        // 人物点击，弹出加好友下拉框
        private void OnClicCharacterEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionCharacterClick;
            DisplayOperationUI(_e.Data);
        }

        #endregion
        #region Tab事件

        private void OnClicTabPageEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionTabPageClick;
            m_BattleData.TabPage = _e.Index;
            RenewalTabPage(_e.Index);
        }
        private void OnClicTabPageTwoEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionTabPageClick2;
            m_BattleData.TabPage2 = _e.Index;
            switch (_e.Index)
            {
                case 0:
                {
                }
                    break;
                case 1: //其他战盟
                {
                    CanRenewalInfo(RefleshType.OtherUnion);
                }
                    break;
            }
        }

        #endregion
        //创建战盟  
        private void OnButtonCreationAllianceEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionBtnCreateUnion;
            var _name = _e.Name.Trim();
            m_BattleData.CreateName = _name;
            //战盟不能为空
            if (_name == "")
            {
                var _ee = new ShowUIHintBoard(220949);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }

            if (!GameUtils.CheckName(_name))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 220950);
                return;
            }
            if (GameUtils.CheckSensitiveName(_name))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 220950);
                return;
            }
            if (GameUtils.ContainEmoji(_name))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 220950);
                return;
            }

            if (!GameUtils.CheckLanguageName(_name))
            {
                UIManager.Instance.ShowMessage(MessageBoxType.Ok, 220950);
                return;
            }

            var _tbClient = Table.GetClientConfig(241);
            var _needmoney = int.Parse(_tbClient.Value);
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes) < _needmoney)
            {
                //金币不足
                var _ee = new ShowUIHintBoard(210100);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            _tbClient = Table.GetClientConfig(242);
            if (PlayerDataManager.Instance.GetLevel() < int.Parse(_tbClient.Value))
            {
                //等级不足
                var _ee = new ShowUIHintBoard(210110);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }

            UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                string.Format(GameUtils.GetDictionaryText(220917), _needmoney / 10000, _name), "",
                () => { NetManager.Instance.StartCoroutine(CreationAllimanceCoroutine(m_BattleData.CreateName)); });
        }
        //其他战盟list点击事件
        private void OnClicAllianceAnotherListEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionOtherListClick;
            ChooseAnotherAlliance(_e.Index);
        }
        //通过申请
        private void OnButtonClicApplyForEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionBtnPassApply;
            if (null == _e)
            {
                return;
            }
            ApplyOperation(_e.Type);
        }
        private void OnApplyOperationEvent(IEvent ievent)
        {
            var e = ievent as BattleUnionOperationEvent;
            if (null == e)
            {
                return;
            }
            if (e.Type == 0)
            {   //一键通过
                if (m_BattleData.MyUnion.ApplyList.Count <= 0)
                {
                    return;
                }
                foreach (var item in m_BattleData.MyUnion.ApplyList)
                {
                    item.Selected = 1;
                }
                ApplyOperation(0);                
            }
            else if (e.Type == 1)
            {   //清除列表
                if (m_BattleData.MyUnion.ApplyList.Count <= 0)
                {
                    return;
                }
                foreach (var item in m_BattleData.MyUnion.ApplyList)
                {
                    item.Selected = 1;
                }
                ApplyOperation(1); 
                //var IdList = new Uint64Array();
                //for (int i = 0; i < m_BattleData.MyUnion.ApplyList.Count; i++)
                //{
                //    var id = m_BattleData.MyUnion.ApplyList[i].ID;
                //    IdList.Items.Add(id);
                //}
                //NetManager.Instance.StartCoroutine(ClearAllianceApplyListCoroutine(m_BattleData.MyUnion.UnionID, IdList));
            }
        }
        private IEnumerator ClearAllianceApplyListCoroutine(int BattleUnionId, Uint64Array IDlist)
        {
            using (new BlockingLayerHelper(0))
            {
                var _msg = NetManager.Instance.ClearAllianceApplyList(BattleUnionId, IDlist);
                yield return _msg.SendAndWaitUntilDone();
                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        m_BattleData.MyUnion.ApplyList.Clear();
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    var _e = new ShowUIHintBoard(220821);
                    EventDispatcher.Instance.DispatchEvent(_e);
                }
            }
        }
        
        private void ApplyOperation(int type)
        {
            var _count = 0;
            var _totalCount = 0;
            var _idList = new Uint64Array();
            var _indexList = new List<int>();
            //成员已满

            if (type == 0 && m_BattleData.MyUnion.TotalCount <= m_BattleData.MyUnion.NowCount)
            {
                var _ee = new ShowUIHintBoard(220940);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            {
                var _enumerator3 = (m_BattleData.MyUnion.ApplyList).GetEnumerator();
                while (_enumerator3.MoveNext())
                {
                    var _item = _enumerator3.Current;
                    {
                        if (_item.Selected == 1)
                        {
                            _idList.Items.Add(m_BattleData.MyUnion.ApplyList[_count].ID);
                            _indexList.Add(_count);
                            _totalCount++;
                        }
                        _count++;
                    }
                }
            }
            if (_idList.Items.Count > 0)
            {
                NetManager.Instance.StartCoroutine(UnionAgreeMentApplyForListCoroutine(m_BattleData.MyUnion.UnionID, type, _idList,
                    _indexList));
            }
        }
        #region  战盟建设

        //捐献金币
        private void OnButtonEndowmentEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionBtnDonation;
            if (m_Build.NowCount >= m_GuildRecord.moneyCountLimit)
            {
                var _ee = new ShowUIHintBoard(220935);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            if (_e.Index == 0)
            {
                //金币不足
                if (PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes) < m_GuildRecord.LessNeedCount)
                {
                    var _ee = new ShowUIHintBoard(210100);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
            }
            else if (_e.Index == 1)
            {
                //金币不足
                if (PlayerDataManager.Instance.GetRes((int)eResourcesType.GoldRes) < m_GuildRecord.MoreNeedCount)
                {
                    var _ee = new ShowUIHintBoard(210100);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
            }
            else if (_e.Index == 2)
            {
                //钻石不足
                if (PlayerDataManager.Instance.GetRes((int)eResourcesType.DiamondRes) < m_GuildRecord.DiaNeedCount)
                {
                    var _ee = new ShowUIHintBoard(210102);
                    EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = 0}));
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
            }
            NetManager.Instance.StartCoroutine(EndowmentUnionPropCoroutine(_e.Index));
        }
        //捐献物品点击
        private void OnClicEndowmentPropEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionDonationItemClick;
            var _selectedItemId = m_Build.DonationItem[_e.Index].ItemIDData.ItemId;
            if (_selectedItemId == -1)
            {
                return;
            }
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ItemInfoUI,
                new ItemInfoArguments
                {
                    ItemId = _selectedItemId,
                    ShowType = (int)eEquipBtnShow.Share
                }));
        }

        //捐献物品
        private void OnButtonEndowmentPropEvent(IEvent ievent)
        {
            var _ee = ievent as UIEvent_UnionDonationItem;
            var _select = _ee.Index;

            if (m_Build.DonationItem[_select].ItemIDData.ItemId == -1)
            {
                return;
            }
            //等待刷新
            if (m_Build.DonationItem[_select].State == (int)ItemState.Wait)
            {
                var _e = new ShowUIHintBoard(220938);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            //道具不足
            if (PlayerDataManager.Instance.GetItemCount(m_Build.DonationItem[_select].ItemIDData.ItemId) <= 0)
            {
                var _e = new ShowUIHintBoard(200000005);
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            var _tbMission = Table.GetGuildMission(m_Build.DonationItem[_select].TaskID);
            if (m_Build.TodayDonation + _tbMission.GetGongJi > m_Build.MaxDonation)
            {
                //每日捐赠道具获得的功绩不能超过{0}
                var _e = new ShowUIHintBoard(string.Format(GameUtils.GetDictionaryText(220915), m_Build.MaxDonation));
                EventDispatcher.Instance.DispatchEvent(_e);
                return;
            }
            NetManager.Instance.StartCoroutine(ButtonEndowmentPropCoroutine(m_Build.DonationItem[_select].TaskID, _select));
        }

        #endregion
        private void OnBuffIconEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionBuffUpShow;
            RenewalBuffPage(_e.Index);
        }
        //购买商品
        private void OnClicBuyPropEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_BattleShopCellClick;
            var _tbShop = Table.GetStore(m_Shop.ShopList[_e.Index].ShopID);
            if (_tbShop == null)
            {
                return;
            }
            //         var roleType = PlayerDataManager.Instance.GetRoleId();
            //         if (BitFlag.GetLow(tbShop.SeeCharacterID, roleType) == false)
            //         {
            //             return;
            //         }
            // 可购买次数小于单次股买数量
            if (m_Shop.ShopList[_e.Index].BuyCount <= 0)
            {
                var _ee = new ShowUIHintBoard(220939);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            //自身战功小于消耗
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.Contribution) < m_Shop.ShopList[_e.Index].Zhangong)
            {
                var _ee = new ShowUIHintBoard(210111);
                EventDispatcher.Instance.DispatchEvent(_ee);
                return;
            }
            NetManager.Instance.StartCoroutine(StoreBuyCoroutine(m_Shop.ShopList[_e.Index].ShopID,1, _e.Index));
        }
        private void OnPlayerExitEvent(IEvent ievent)
        {
            var e = ievent as PlayerExitAllianceMsgEvent;
            if (null == e)
            {
                return;
            }
            else if (null == e.ExitPlayerId)
            {
                return;
            }
            m_BattleData.MyUnion.ChiefID = e.LeaderId;
            m_BattleData.MyUnion.ChiefName = e.NewLeaderName;
            var MyId = PlayerDataManager.Instance.PlayerDataModel.CharacterBase.CharacterId;
            if (m_BattleData.MyUnion.ChiefID == MyId)
            {
                m_BattleData.MyPorp.Ladder = 3;
            }
            if (m_dicUnionMembers.ContainsKey(e.ExitPlayerId))
            {
                 m_dicUnionMembers.Remove(e.ExitPlayerId);
            }
            var HintStr = string.Empty;
            if (e.IsLeader)
            {
                HintStr = string.Format(GameUtils.GetDictionaryText(220992), e.ExitPlayerName, e.NewLeaderName);
            }
            else
            {
                HintStr = string.Format(GameUtils.GetDictionaryText(220987),e.ExitPlayerName);
            }
            if (!string.IsNullOrEmpty(HintStr))
            {
                GameUtils.ShowHintTip(HintStr);                
            }
        }
        
        private void OnClicBossEvent(IEvent ievent)
        {
        }
        //攻城战几个按钮点击打开相应page页
        private void OnClicAttackPageEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_UnionBattlePageCLick;
            //竞价按钮请求竞价金额
            if (_e.Index == 2)
            {
                if (m_BattleData.MyUnion.Level < 3)
                {
                    //"战盟等级至少需要3级才可报名！"
                    var _ee = new ShowUIHintBoard(270275);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
                if (m_AttackCity.OpenState != (int)eAllianceWarState.Bid)
                {
                    //"现在不是竞价时间段！"
                    var _ee = new ShowUIHintBoard(270276);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
                if (m_AttackCity.CastellanId == m_BattleData.MyUnion.UnionID)
                {
                    //"守城方不能报名！"
                    var _ee = new ShowUIHintBoard(270277);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }

                if (Table.GetGuildAccess(m_BattleData.MyPorp.Ladder).CanModifyAttackCity != 1)
                {
                    //"你没有权限报名！"
                    var _ee = new ShowUIHintBoard(270278);
                    EventDispatcher.Instance.DispatchEvent(_ee);
                    return;
                }
                NetManager.Instance.StartCoroutine(ApplyForBiddingCoroutine(0, 0));
            }
            m_AttackCity.TabPage = _e.Index;
        }
        private void OnSyncCastellanChangeEvent(IEvent ievent)
        {
            var _e = ievent as BattleUnionSyncOccupantChange;
            if (_e.Data.OccupantId > 0)
            {
                var _chat = new ChatMessageDataModel
                {
                    Type = (int)eChatChannel.SystemScroll,
                    CharId = 0,
                    Content = string.Format(GameUtils.GetDictionaryText(300932), _e.Data.OccupantName)
                };

                EventDispatcher.Instance.DispatchEvent(new Event_PushMessage(_chat));
            }
            SetCastellanInfo(_e.Data);
            EliminateDefierInfo();
            ApplyForUnionFightInfo();
        }
        private void OnBingNumChangedEvent(IEvent ievent)
        {
            var _e = ievent as BattleUnionCountChange;
            var type = _e.Type;
            if (_e.Index == 0)
            {
                OnAdding(type);
            }
            else if (_e.Index == 1)
            {
                OnDelete(type);
            }
            else if (_e.Index == 2) //2 Add Press
            {
                m_buttonPress = NetManager.Instance.StartCoroutine(BtnAddingOnClicCoroutine(type));
            }
            else if (_e.Index == 3) //3 Del Press
            {
                m_buttonPress = NetManager.Instance.StartCoroutine(BtnDeleteOnClicCoroutine(type));
            }
            else if (_e.Index == 4) //Add Release
            {
                if (m_buttonPress != null)
                {
                    NetManager.Instance.StopCoroutine(m_buttonPress);
                    m_buttonPress = null;
                }
            }
            else if (_e.Index == 5) //Del Release
            {
                if (m_buttonPress != null)
                {
                    NetManager.Instance.StopCoroutine(m_buttonPress);
                    m_buttonPress = null;
                }
            }
        }
        //设置自动申请加入
        private void OnButtonSetAutoAcceptedEvent(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ChangedUnionAutoAddCoroutine(m_BattleData.MyUnion.UnionID));
        }
        private void OnSyncDefierInfoChangedEvent(IEvent ievent)
        {
            var _e = ievent as BattleUnionSyncChallengerDataChange;
            DefierInfoChange(_e.Data);
        }

        #endregion 
        private int GetNextAllanceWarStartDate(DateTime _dateTime)
        {
            int day = 0;
            DayOfWeek targetDay;
            if (null != _dateTime)
            {
                targetDay = _dateTime.DayOfWeek;

                switch (targetDay)
                {
                    case DayOfWeek.Sunday:
                        day = 7;
                        break;
                    case DayOfWeek.Monday:
                    case DayOfWeek.Tuesday:
                    case DayOfWeek.Wednesday:
                    case DayOfWeek.Thursday:
                    case DayOfWeek.Friday:
                    case DayOfWeek.Saturday:
                        day = (int)targetDay;
                        break;
                }
            }

            return day;
        }
    }
}