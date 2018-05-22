
/********************************************************************************* 

                         Scorpion




  *FileName:ItemInfoController

  *Version:1.0

  *Date:2017-06-15

  *Description:

**********************************************************************************/
#region using

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public class ItemMassageFrameCtrler : IControllerBase
    {


        #region 静态变量
        private  const int GET_PATH_COUNT = 5; //获取途径总个数 
        #endregion

        #region 成员变量
        private ItemInfoDataModel DataModel { get; set; }
        private readonly Dictionary<int, string> m_plantType = new Dictionary<int, string>();
        private Coroutine ButtonPress { get; set; }
        #endregion

        #region 构造函数
        public ItemMassageFrameCtrler()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_ItemInfoFrame_BtnAffirmClick.EVENT_TYPE, OnClickAffrimEvent);
            EventDispatcher.Instance.AddEventListener(ItemInfoOperate.EVENT_TYPE, OnItemMsgOperaEvent);
            EventDispatcher.Instance.AddEventListener(ItemInfoCountChange.EVENT_TYPE, OnItemMsgNumChangeEvent);
            EventDispatcher.Instance.AddEventListener(Event_ItemInfoClick.EVENT_TYPE, OnItemGetClickEvent);
            EventDispatcher.Instance.AddEventListener(UIEvent_BagItemCountChange.EVENT_TYPE, OnBagItemNumChangeEvent);
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            if (DataModel != null)
            {
                DataModel.PropertyChanged -= OnNumChange;
            }
            DataModel = new ItemInfoDataModel();

            DataModel.PropertyChanged += OnNumChange;
            m_plantType.Clear();
            m_plantType.Add(0, GameUtils.GetDictionaryText(240210));
            m_plantType.Add(1, GameUtils.GetDictionaryText(240211));
            m_plantType.Add(2, GameUtils.GetDictionaryText(240212));
            m_plantType.Add(3, GameUtils.GetDictionaryText(240214));
            m_plantType.Add(4, GameUtils.GetDictionaryText(240215));
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
            var tbItem = Table.GetItemBase(DataModel.ItemData.ItemId);
            if (tbItem == null)
            {
                return;
            }
            if (tbItem.Type == 15000)
            {
                var tbEquip = Table.GetEquipBase(DataModel.ItemData.ItemId);
                if (tbEquip == null)
                {
                    return;
                }
                var tbWeaponMount = Table.GetWeaponMount(DataModel.ItemData.ItemId);
                var path = tbWeaponMount.Path;
                var animationPath = tbEquip.AnimPath + "/Stand.anim";
                var e = new ItemInfoMountModelDisplay_Event(path, animationPath);
                EventDispatcher.Instance.DispatchEvent(e);
            }
            else if (tbItem.Type == 30000 || tbItem.Type == 35000)
            {
                var tbHandBook = Table.GetHandBook(tbItem.Exdata[0]);
                if (tbHandBook == null)
                    return;

                var tbCharacter = Table.GetCharacterBase(tbHandBook.NpcId);
                if (tbCharacter == null)
                    return;

                var tbCharModel = Table.GetCharModel(tbCharacter.CharModelID);
                if (tbCharModel == null)
                    return;

                var e = new ItemInfoMountModelDisplay_Event(Resource.Dir.Model + tbCharModel.ResPath, tbCharModel.AnimPath + "/Stand.anim", (float)tbCharModel.Scale * 1.50f);
                EventDispatcher.Instance.DispatchEvent(e);
            }
            else if (tbItem.Type == 10500)
            {
                var tbEquip = Table.GetEquipBase(DataModel.ItemData.ItemId);
                if (tbEquip == null)
                {
                    return;
                }
                var tbWeaponMount = Table.GetWeaponMount(DataModel.ItemData.ItemId);
                var path = tbWeaponMount.Path;
                var animationPath = tbEquip.AnimPath + "/Stand.anim";
                var e = new ItemInfoMountModelDisplay_Event(path, animationPath, 1.5f, -180);
                EventDispatcher.Instance.DispatchEvent(e);
            }
        }

        public void Close()
        {
            if (UIManager.GetInstance().GetController(UIConfig.ChestInfoUI).State == FrameState.Open)
            {
                DataModel.IsTips = 0;
            }
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as ItemInfoArguments;
            if (_args == null)
            {
                return;
            }

            var _showType = _args.ShowType;
            DataModel.IsShowRecycleMessage = false;

            if (_args.DataModel != null)
            {
                var _dataModel = _args.DataModel;
                DataModel.ItemData.Clone(_dataModel);
                DataModel.Tips = Table.GetItemBase(DataModel.ItemData.ItemId).Desc;
                if (DataModel.ItemData.Count != 0)
                {
                    DataModel.SellCount = _dataModel.Count;
                    DataModel.SellRate = (float)(DataModel.SellCount) / DataModel.ItemData.Count;
                    DataModel.UseCount = _dataModel.Count;
                    DataModel.UseRate = (float)(DataModel.UseCount) / DataModel.ItemData.Count;
                    DataModel.RecycleCount = _dataModel.Count;
                    DataModel.RecycleRate = (float)(DataModel.RecycleCount) / DataModel.ItemData.Count;
                }
                else
                {
                    //没有数量就单纯显示吧
                    DataModel.IsTips = 1;
                }

                if (_showType == (int)eEquipBtnShow.Share || _showType == (int)eEquipBtnShow.None)
                {
                    DataModel.IsTips = 1;
                }
                else
                {
                    DataModel.IsTips = 0;
                }
            }
            else
            {
                DataModel.ItemData.Reset();
                DataModel.ItemData.ItemId = _args.ItemId;
                DataModel.Tips = Table.GetItemBase(DataModel.ItemData.ItemId).Desc;

                DataModel.SellCount = 1;
                DataModel.UseCount = 1;
                DataModel.SellRate = 0.0f;
                DataModel.UseRate = 0.0f;
                DataModel.RecycleCount = 1;
                DataModel.RecycleRate = 0.0f;
                if (_showType == (int)eEquipBtnShow.Share || _showType == (int)eEquipBtnShow.None)
                {
                    DataModel.IsTips = 1;
                }
                else
                {
                    DataModel.IsTips = 0;
                }
            }

            var _tbItem = Table.GetItemBase(DataModel.ItemData.ItemId);
            if (_tbItem == null)
            {
                return;
            }

            var _type = Table.GetItemType(_tbItem.Type);
            if (_type == null)
            {
                return;
            }
            if (_tbItem.Sell > 0 || _tbItem.CallBackPrice > 0)
            {
                DataModel.ShowSellInfo = true;
            }
            else
            {
                DataModel.ShowSellInfo = false;
            }

            if (_tbItem.CanUse == 1)
            {
                DataModel.UseCount = 1;
            }

            DataModel.CallBackType = _tbItem.CallBackType;
            switch (_tbItem.Type)
            {
                case 21000: //技能书
                {
                    GameUtils.InitSkillBook(DataModel);
                }
                    break;
                case 26300: //藏宝图
                {
                    TreasuMapInit(DataModel);
                }
                    break;
                case 90000: //
                {
                    SeedMsgInit(DataModel);
                }
                    break;
                case 70000: //随从魂魄
                {
                    PetSoulInit(DataModel);
                }
                    break;
                case 26000: //随从蛋
                case 26100: //随从蛋
                {
                    PetEggInit(DataModel);
                }
                    break;
                case 22201: //祭拜消耗
                {
                    WorshipItemInfoInit(DataModel);
                }
                    break;
            }

            var _tbIype = Table.GetItemType(_tbItem.Type);

            //等级
            if (_tbItem.UseLevel > PlayerDataManager.Instance.GetLevel())
            {
                DataModel.LevelColor = MColor.red;
            }
            else
            {
                DataModel.LevelColor = MColor.green;
            }
            //职业
            var _role = _tbItem.OccupationLimit;

            if (_role != -1)
            {
                var _tbCharacter = Table.GetCharacterBase(_role);
                var _roleType = PlayerDataManager.Instance.GetRoleId();
                if (_tbCharacter != null)
                {
                    if (_roleType != _role)
                    {
                        DataModel.ProfessionColor = MColor.red;
                    }
                    else
                    {
                        DataModel.ProfessionColor = MColor.green;
                    }
                    DataModel.ProfessionLimit = _tbCharacter.Name;
                }
            }
            else
            {
                DataModel.ProfessionLimit = GameUtils.GetDictionaryText(220700);
                DataModel.ProfessionColor = MColor.green;
            }

            for (var i = 0; i < DataModel.ShowList.Count; i++)
            {
                DataModel.ShowList[i] = BitFlag.GetLow(_tbIype.Info, i);
            }
            //显示获取途径
            DataModel.IsShowGetPath = false;
            if (_tbItem.GetWay != -1)
            {
                DataModel.ShowList[13] = true;
                OnClickReceive();
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

        #region 事件
        private void OnItemMsgNumChangeEvent(IEvent ievent)
        {
            var _e = ievent as ItemInfoCountChange;
            //Type 2 使用 1 出售  3 回收
            //Index 0 Add Click 1 Del Click 2 Add Press 3 Del Press 4 Add Release 5 Del Release
            var _type = _e.Type;
            if (_type == 0)
            {
                if (_e.Index == 0)
                {
                    if (DataModel.UseCount < DataModel.ItemData.Count)
                    {
                        DataModel.UseCount++;
                    }
                }
                else if (_e.Index == 1)
                {
                    if (DataModel.UseCount > 0)
                    {
                        DataModel.UseCount--;
                    }
                }
            }
            else
            {
                if (_e.Index == 0)
                {
                    OnAdd(_type);
                }
                else if (_e.Index == 1)
                {
                    OnDelete(_type);
                }
                else if (_e.Index == 2) //2 Add Press
                {
                    ButtonPress = NetManager.Instance.StartCoroutine(BtnAddOnPress(_type));
                }
                else if (_e.Index == 3) //3 Del Press
                {
                    ButtonPress = NetManager.Instance.StartCoroutine(BtnDeleteOnPress(_type));
                }
                else if (_e.Index == 4) //Add Release
                {
                    if (ButtonPress != null)
                    {
                        NetManager.Instance.StopCoroutine(ButtonPress);
                        ButtonPress = null;
                    }
                }
                else if (_e.Index == 5) //Del Release
                {
                    if (ButtonPress != null)
                    {
                        NetManager.Instance.StopCoroutine(ButtonPress);
                        ButtonPress = null;
                    }
                }
            }
        }

        private  void OnItemMsgOperaEvent(IEvent ievent)
        {
            var _e = ievent as ItemInfoOperate;
            switch (_e.Type)
            {
                case 1:
                {
                    OnUsingItem();
                }
                    break;
                case 2:
                {
                    ClickRecycle();
                }
                    break;
                case 3:
                {
                    if ((DataModel.ItemData.ItemId >= 30000) && (DataModel.ItemData.ItemId < 40000))
                    {
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ItemInfoUI));
                        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.HandBook));
                    }
                }
                    break;
                case 4:
                {
                    OnClickReceive();
                }
                    break;
                case 5:
                {
                    ClickToClose();
                }
                    break;
            }
        }
        private void OnBagItemNumChangeEvent(IEvent ievent)
        {
            var _e = ievent as UIEvent_BagItemCountChange;
            var _myItem = DataModel.ItemData;
            if (_e.ItemId != _myItem.ItemId)
            {
                return;
            }
            DataModel.SellCount = _myItem.Count;
        }

        private void OnClickAffrimEvent(IEvent e)
        {
            var _ee = e as UIEvent_ItemInfoFrame_BtnAffirmClick;
            if (_ee.Type == 0)
            {
                NetManager.Instance.StartCoroutine(SellItemCorotion());
            }
            else if (_ee.Type == 1)
            {
                GameUtils.UseItem(DataModel);
            }
            else if (_ee.Type == 2)
            {
                GameUtils.RecycleConfirm(DataModel.ItemData.ItemId, DataModel.RecycleCount, () =>
                {
                    NetManager.Instance.StartCoroutine(RecyclItemCorotion());
                });
            }
            else if (_ee.Type == 3)
            {
                DataModel.IsShowRecycleMessage = false;
            }
        }
        private void OnItemGetClickEvent(IEvent ievent)
        {
            var _e = ievent as Event_ItemInfoClick;
            var _item = DataModel.GetPathList[_e.Index];
            var _tbItemGet = Table.GetItemGetInfo(_item.ItemGetId);
            if (_tbItemGet.IsShow == -1) //开启条件
            {
                if (_item.ItemGetId == 21)
                {//领地争夺有灭世入口
                    MainUIController MainCtr = UIManager.Instance.GetController(UIConfig.MainUI) as MainUIController;
                    if (1 != (MainCtr.GetDataModel("MainUI") as MainUIDataModel).MainActivity)
                    {
                        GameUtils.ShowHintTip(GameUtils.GetDictionaryText(270229));
                        return;
                    }
                }
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ItemInfoUI));
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ChestInfoUI));
                GameUtils.GotoUiTab(_tbItemGet.UIName, _tbItemGet.Param[0], _tbItemGet.Param[1], _tbItemGet.Param[2]);
            }
            else
            {
                var _dic = PlayerDataManager.Instance.CheckCondition(_tbItemGet.IsShow);
                if (_dic != 0)
                {
                    //不符合副本扫荡条件
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(_dic));
                    return;
                }
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ItemInfoUI));
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.ChestInfoUI));
                //从道具途径进入活动二级界面，关闭时不回活动一级界面，但是如果道具在活动界面时，关闭要显示一级界面
                if (UIManager.Instance.GetController(UIConfig.ActivityUI).State == FrameState.Open)
                {
                    GameUtils.GotoUiTab(_tbItemGet.UIName, _tbItemGet.Param[0], _tbItemGet.Param[1], _tbItemGet.Param[2]);
                }
                else
                {
                    GameUtils.GotoUiTab(_tbItemGet.UIName, _tbItemGet.Param[0], _tbItemGet.Param[1], 0);

                }
            }
            if (UIManager.Instance.GetController(UIConfig.QuickBuyUi).State == FrameState.Open)
            {
                var e = new Close_UI_Event(UIConfig.QuickBuyUi);
                EventDispatcher.Instance.DispatchEvent(e);
            }
        }
        #endregion






        private IEnumerator BtnAddOnPress(int type)
        {
            var _pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(_pressCd);
                if (OnAdd(type) == false)
                {
                    NetManager.Instance.StopCoroutine(ButtonPress);
                    ButtonPress = null;
                    yield break;
                }
                if (_pressCd > 0.01)
                {
                    _pressCd = _pressCd*0.8f;
                }
            }
            yield break;
        }

        private IEnumerator BtnDeleteOnPress(int type)
        {
            var _pressCd = 0.25f;
            while (true)
            {
                yield return new WaitForSeconds(_pressCd);
                if (OnDelete(type) == false)
                {
                    NetManager.Instance.StopCoroutine(ButtonPress);
                    ButtonPress = null;
                    yield break;
                }
                if (_pressCd > 0.01)
                {
                    _pressCd = _pressCd*0.8f;
                }
            }
            yield break;
        }

        //随从蛋提示
        private void PetEggInit(ItemInfoDataModel dataModel)
        {
            var _tbItemBase = Table.GetItemBase(dataModel.ItemData.ItemId);
            if (_tbItemBase != null)
            {
                var _petid = _tbItemBase.Exdata[0];
                var _tbPet = Table.GetPet(_petid);
                var _petTime = GameUtils.GetTimeDiffString(_tbItemBase.Exdata[1]*60, true);
                var _str = "";
                for (var i = 0; i < _tbPet.Ladder; i++)
                {
                    _str += GameUtils.StarIcon;
                }
                dataModel.Tips = string.Format(_tbItemBase.Desc, _tbPet.Name, _str, _petTime);
            }
        }

        //随从魂魄提示
        private void PetSoulInit(ItemInfoDataModel dataModel)
        {
            var tbItemBase = Table.GetItemBase(dataModel.ItemData.ItemId);
            if (tbItemBase != null)
            {
                var _count = 0;
                var _petid = tbItemBase.Exdata[2];
                var _tbPet = Table.GetPet(_petid);
                var _items = PlayerDataManager.Instance.EnumBagItem((int) eBagType.Pet);
                foreach (var _item in _items)
                {
                    if (null != _item && _petid == _item.ItemId)
                    {
                        _count = _item.Exdata[PetItemExtDataIdx.FragmentNum];
                        break;
                    }
                }

                if (_tbPet != null)
                {
                    dataModel.Tips = string.Format(tbItemBase.Desc, _tbPet.Name, tbItemBase.Exdata[1], _count);
                }
            }
        }

        private void SeedMsgInit(ItemInfoDataModel dataModel)
        {
            var _tbPlant = Table.GetPlant(dataModel.ItemData.ItemId);
            if (_tbPlant == null)
            {
                return;
            }
            DataModel.SeedLimit = _tbPlant.PlantLevel;
            var _level = (int) UIManager.Instance.GetController(UIConfig.FarmUI).CallFromOtherClass("GetBuildLevel", null);
            if (_tbPlant.PlantLevel > _level)
            {
                DataModel.SeedColor = MColor.white;
            }
            else
            {
                DataModel.SeedColor = GameUtils.GetTableColor(0);
            }
            var _type = _tbPlant.PlantType;
            if (m_plantType.ContainsKey(_type))
            {
                dataModel.SeedType = m_plantType[_type];
            }
            var _str1 = "";
            var _str2 = "";
            var _num1 = _tbPlant.MatureCycle/60;
            var _num2 = _tbPlant.MatureCycle%60;
            if (_num1 > 0)
            {
                _str1 = _num1 + GameUtils.GetDictionaryText(1040);
            }
            if (_num2 > 0)
            {
                _str2 = _num2 + GameUtils.GetDictionaryText(1041);
            }
            dataModel.SeedCircle = _str1 + _str2;
            if (_tbPlant.HarvestCount[0] == _tbPlant.HarvestCount[1])
            {
                dataModel.SeedCount = _tbPlant.HarvestCount[0].ToString();
            }
            else
            {
                dataModel.SeedCount = _tbPlant.HarvestCount[0] + "-" + _tbPlant.HarvestCount[1];
            }
        }

        private static void TreasuMapInit(ItemInfoDataModel dataModel)
        {
            var _exData = dataModel.ItemData.Exdata;
            var _sceneId = _exData[0];
            var _tbScene = Table.GetScene(_sceneId);
            if (_tbScene == null || 0 == _sceneId)
            {
//未初始化的藏宝图
                dataModel.Tips = GameUtils.GetDictionaryText(210500);
            }
            else
            {
                if (_tbScene == null)
                {
                    Logger.Error("InitTreasureMap(), tbScene == null!!!!");
                    return;
                }
                var _tip = Table.GetItemBase(dataModel.ItemData.ItemId).Desc;
                dataModel.Tips = string.Format(_tip, _tbScene.Name, _exData[1], _exData[2]);
            }
        }

        private static void WorshipItemInfoInit(ItemInfoDataModel dataModel)
        {
            var tbItemBase = Table.GetItemBase(dataModel.ItemData.ItemId);
            if (null == tbItemBase)
                return;
            var lodeId = tbItemBase.Exdata[0];
            var tbLode = Table.GetLode(lodeId);
            if (null == tbLode)
                return;
            var tbSceneNpc = Table.GetSceneNpc(tbLode.SceneNpcId);
            if (null == tbSceneNpc)
                return;
            var tbScene = Table.GetScene(tbSceneNpc.SceneID);
            var _tip = Table.GetItemBase(dataModel.ItemData.ItemId).Desc;
            dataModel.Tips = string.Format(_tip, tbScene.Name, tbSceneNpc.PosX, tbSceneNpc.PosZ);
        }


        private bool OnAdd(int type)
        {
            if (type == 1)
            {
                if (DataModel.SellCount < DataModel.ItemData.Count)
                {
                    DataModel.SellCount++;
                    DataModel.SellRate = (float) (DataModel.SellCount - 1)/(DataModel.ItemData.Count - 1);
                    return true;
                }
            }
            else if (type == 2)
            {
                if (DataModel.UseCount < DataModel.ItemData.Count)
                {
                    DataModel.UseCount++;
                    DataModel.UseRate = (float) (DataModel.UseCount - 1)/(DataModel.ItemData.Count - 1);
                    return true;
                }
            }
            else if (type == 3)
            {
                if (DataModel.RecycleCount < DataModel.ItemData.Count)
                {
                    DataModel.RecycleCount++;
                    DataModel.RecycleRate = (float) (DataModel.RecycleCount - 1)/(DataModel.ItemData.Count - 1);
                    return true;
                }
            }
            return false;
        }



        private void OnClickReceive()
        {
            var _tbItem = Table.GetItemBase(DataModel.ItemData.ItemId);
            if (_tbItem == null)
            {
                return;
            }
            if (_tbItem.GetWay != -1)
            {
                DataModel.IsShowGetPath = true;
                DataModel.GetPathList.Clear();
                var _list = new List<ItemGetPathDataModel>();
                int leng = (int)UIManager.GetInstance().GetController(UIConfig.ItemInfoGetUI).CallFromOtherClass("GetPathCount", null);
                for (var i = 0; i < leng; i++)
                {
                    var _isShow = BitFlag.GetLow(_tbItem.GetWay, i);
                    if (_isShow)
                    {
                        var _tbItemGetInfo = Table.GetItemGetInfo(i);
                        if (_tbItemGetInfo != null)
                        {
                            var _item = new ItemGetPathDataModel();
                            _item.ItemGetId = i;
                            _list.Add(_item);
                        }
                    }
                }
                DataModel.GetPathList = new ObservableCollection<ItemGetPathDataModel>(_list);
            }
        }

        private void ClickToClose()
        {
            DataModel.IsShowGetPath = false;
        }

        private void ClickRecycle()
        {
            DataModel.RecycleCount = DataModel.ItemData.Count;
            DataModel.RecycleRate = 1.0f;

            if (DataModel.ItemData.Count == 1)
            {
                GameUtils.RecycleConfirm(DataModel.ItemData.ItemId, DataModel.RecycleCount,() =>
                {
                    NetManager.Instance.StartCoroutine(RecyclItemCorotion());
                });
            }
            else
            {
                DataModel.IsShowRecycleMessage = true;
            }
        }

        private void OnNumChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SellRate")
            {
                DataModel.SellCount = (int) (Mathf.Round(DataModel.SellRate*(DataModel.ItemData.Count - 1)) + 1);
            }
            else if (e.PropertyName == "UseRate")
            {
                DataModel.UseCount = (int) (Mathf.Round(DataModel.UseRate*(DataModel.ItemData.Count - 1)) + 1);
            }
            else if (e.PropertyName == "RecycleRate")
            {
                DataModel.RecycleCount = (int) (Mathf.Round(DataModel.RecycleRate*(DataModel.ItemData.Count - 1)) + 1);
            }
        }

        private bool OnDelete(int type)
        {
            if (type == 1)
            {
                if (DataModel.SellCount > 1)
                {
                    DataModel.SellCount--;
                    DataModel.SellRate = (float) (DataModel.SellCount - 1)/(DataModel.ItemData.Count - 1);
                    return true;
                }
            }
            else if (type == 2)
            {
                if (DataModel.UseCount > 1)
                {
                    DataModel.UseCount--;
                    DataModel.UseRate = (float) (DataModel.UseCount - 1)/(DataModel.ItemData.Count - 1);
                    return true;
                }
            }
            else
            {
                if (DataModel.RecycleCount > 1)
                {
                    DataModel.RecycleCount--;
                    DataModel.RecycleRate = (float) (DataModel.RecycleCount - 1)/(DataModel.ItemData.Count - 1);
                    return true;
                }
            }
            return false;
        }



        private void OnUsingItem()
        {
            var _tbItem = Table.GetItemBase(DataModel.ItemData.ItemId);
            if (_tbItem.CanUse == 1 || DataModel.ItemData.Count == 1)
            {
//单个使用，或者物品数量就是1直接使用
                GameUtils.UseItem(DataModel);
            }
            else if (_tbItem.CanUse == 2)
            {
                var _e = new ItemInfoNotifyEvent(0);
                EventDispatcher.Instance.DispatchEvent(_e);
            }
        }

        private IEnumerator RecyclItemCorotion()
        {
            using (var _blockingLayer = new BlockingLayerHelper(0))
            {
                var _item = DataModel.ItemData;
                var _msg = NetManager.Instance.RecycleBagItem(_item.BagId, _item.ItemId, _item.Index, DataModel.RecycleCount);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        //回收成功
                        var _e1 = new ShowUIHintBoard(270110);
                        EventDispatcher.Instance.DispatchEvent(_e1);
                        var _e = new Close_UI_Event(UIConfig.ItemInfoUI);
                        EventDispatcher.Instance.DispatchEvent(_e);
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Info(string.Format("SellItemCorotion....State = {0} ErroeCode = {1}", _msg.State, _msg.ErrorCode));
                }
            }
        }

        private IEnumerator SellItemCorotion()
        {
            using (var _blockingLayer = new BlockingLayerHelper(0))
            {
                var _item = DataModel.ItemData;
                var _count = DataModel.SellCount;
                var _msg = NetManager.Instance.SellBagItem(_item.BagId, _item.ItemId, _item.Index, DataModel.SellCount);
                yield return _msg.SendAndWaitUntilDone();

                if (_msg.State == MessageState.Reply)
                {
                    if (_msg.ErrorCode == (int) ErrorCodes.OK)
                    {
                        var _e1 = new ShowUIHintBoard(223003);
                        EventDispatcher.Instance.DispatchEvent(_e1);

                        if (_item.Count == _count || _item.Count == 0)
                        {
//完全卖完数量才关闭窗口
                            _item.Count = 0;
                            var _e = new Close_UI_Event(UIConfig.ItemInfoUI);
                            EventDispatcher.Instance.DispatchEvent(_e);
                        }
                        else
                        {
                            _item.Count -= _count;
                        }
                    }
                    else
                    {
                        UIManager.Instance.ShowNetError(_msg.ErrorCode);
                    }
                }
                else
                {
                    Logger.Info(string.Format("SellItemCorotion....State = {0} ErroeCode = {1}", _msg.State, _msg.ErrorCode));
                }
            }
        }


    }
}