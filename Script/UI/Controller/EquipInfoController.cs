#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using DataTable;
using EventSystem;
using Shared;

#endregion

namespace ScriptController
{
    public class EquipInfoController : IControllerBase
    {
        private static int GET_PATH_COUNT; //获取途径总个数 

        public EquipInfoController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(Event_EquipInfoClick.EVENT_TYPE, OnEquipGetClickEvent);
        }



        private EquipInfoDataModel DataModel;
        private bool mIsInit = true;
        private string StrDic230004;
        private string StrDic230006;
        private string StrDic230025;
        private string StrDic230032;
        private string StrDic230033;
        private string StrDic230034;

        private void InitStr()
        {
            StrDic230004 = GameUtils.GetDictionaryText(230004);
            StrDic230006 = GameUtils.GetDictionaryText(230006);
            StrDic230034 = GameUtils.GetDictionaryText(230034);
            StrDic230033 = GameUtils.GetDictionaryText(230033);
            StrDic230032 = GameUtils.GetDictionaryText(230032);
            StrDic230025 = GameUtils.GetDictionaryText(230025);
            mIsInit = false;
        }

        public void CleanUp()
        {
            DataModel = new EquipInfoDataModel();
            mIsInit = true;
            GET_PATH_COUNT = 0;
            Table.ForeachItemGetInfo(
                record =>
                {
                    GET_PATH_COUNT++;
                    return true;
                }
                );
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            throw new NotImplementedException(name);
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
            var args = data as EquipInfoArguments;
            if (args == null)
            {
                return;
            }

            var type = args.Type;

            if (mIsInit)
            {
                InitStr();
            }
            var itemId = args.ItemId;
            //int itemId = 304001;
            var tbItem = Table.GetItemBase(itemId);
            if (tbItem == null)
            {
                return;
            }
            DataModel.BuffId = -1;
            var equipId = tbItem.Exdata[0];
            var tbEquip = Table.GetEquipBase(equipId);
            if (tbEquip != null)
            {
                DataModel.BuffLevel = tbEquip.AddBuffSkillLevel;

                if (tbEquip.BuffGroupId >= 0)
                {
                    var tbBuffGroup = Table.GetBuffGroup(tbEquip.BuffGroupId);
                    if (tbBuffGroup != null && tbBuffGroup.BuffID.Count == 1)
                    {
                        DataModel.BuffId = tbBuffGroup.BuffID[0];
                    }
                    else
                    {
                        DataModel.BuffId = 8999;
                    }
                }
            }
        
            if (tbEquip.FIghtNumDesc == -1)
            {
                DataModel.FightNum = "?????";
            }
            else
            {
                DataModel.FightNum = tbEquip.FIghtNumDesc.ToString();
            }
       
            if (tbEquip == null)
            {
                return;
            }
            DataModel.ItemId = itemId;
            DataModel.EquipId = equipId;
            DataModel.EnchanceLevel = 0;

            //套装展示特殊装备
            if (args.Exdata != null)
            {
                DataModel.EnchanceLevel = (int) args.Exdata;
                DataModel.IsExaddShow = true;
                if (tbItem.GetWay == -1)
                {
                    DataModel.IsShowGetPath = false;
                }
                else
                {
                    DataModel.IsShowGetPath = true;
                    DataModel.GetPathList.Clear();
                    var list = new List<ItemGetPathDataModel>();
                    for (var i = 0; i < GET_PATH_COUNT; i++)
                    {
                        var isShow = BitFlag.GetLow(tbItem.GetWay, i);
                        if (isShow)
                        {
                            var tbItemGetInfo = Table.GetItemGetInfo(i);
                            if (tbItemGetInfo != null)
                            {
                                var item = new ItemGetPathDataModel();
                                item.ItemGetId = i;
                                list.Add(item);
                            }
                        }
                    }
                    DataModel.GetPathList = new ObservableCollection<ItemGetPathDataModel>(list);
                }
            }

            DataModel.CanUseLevel = PlayerDataManager.Instance.GetLevel() < tbItem.UseLevel ? 1 : 0;
            //职业符合不？
            if (tbEquip.Occupation != -1)
            {
                DataModel.CanRole = PlayerDataManager.Instance.GetRoleId() == tbEquip.Occupation ? 0 : 1;
            }
            var strDic = GameUtils.GetDictionaryText(230004);

            DataModel.PhaseDesc = string.Format(strDic, GameUtils.NumEntoCh(tbEquip.Ladder));

            strDic = GameUtils.GetDictionaryText(230006);

            for (var i = 0; i != 2; ++i)
            {
                var attrId = tbEquip.NeedAttrId[i];
                if (attrId != -1)
                {
                    var attrValue = tbEquip.NeedAttrValue[i];
                    var selfAttrValue = PlayerDataManager.Instance.GetAttribute(attrId);
                    var needStr = string.Format(strDic, GameUtils.AttributeName(attrId), selfAttrValue, attrValue);

                    if (selfAttrValue < attrValue)
                    {
                        DataModel.NeedAttr[i] = string.Format("[FF0000]{0}[-]", needStr);
                    }
                    else
                    {
                        DataModel.NeedAttr[i] = string.Format("[00FF00]{0}[-]", needStr);
                    }
                }
                else
                {
                    DataModel.NeedAttr[i] = "";
                }
            }

            var enchanceLevel = DataModel.EnchanceLevel;

            for (var i = 0; i < 4; i++)
            {
                var nAttrId = tbEquip.BaseAttr[i];
                if (nAttrId != -1)
                {
                    var baseValue = tbEquip.BaseValue[i];
                    var changeValue = 0;
                    if (enchanceLevel > 0)
                    {
                        changeValue = GameUtils.GetBaseAttr(tbEquip, enchanceLevel, i, nAttrId) - baseValue;
                    }
                    GameUtils.SetAttribute(DataModel.BaseAttr, i, nAttrId, baseValue, changeValue);
                }
                else
                {
                    DataModel.BaseAttr[i].Reset();
                }
            }
            for (var i = 0; i < 4; i++)
            {
                var attrData = DataModel.BaseAttr[i];
                var nAttrId = attrData.Type;
                if (nAttrId != -1)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    var attrValue = GameUtils.AttributeValue(nAttrId, attrData.Value);

                    if (attrData.ValueEx != 0)
                    {
                        if (attrData.Change != 0 || attrData.ChangeEx != 0)
                        {
                            var attrValueEx = GameUtils.AttributeValue(nAttrId, attrData.ValueEx);
                            var attrChange = GameUtils.AttributeValue(nAttrId, attrData.Change);
                            var attrChangeEx = GameUtils.AttributeValue(nAttrId, attrData.ChangeEx);
                            //rDic = "{0}+:{1}[00ff00](+{2})[-]-{3}[00ff00](+{4})[-]";
                            strDic = StrDic230034;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrChange, attrValueEx,
                                attrChangeEx);
                        }
                        else
                        {
                            var attrValueEx = GameUtils.AttributeValue(nAttrId, attrData.ValueEx);
                            //strDic = "{0}+:{1}-{2}";
                            strDic = StrDic230033;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrValueEx);
                        }
                    }
                    else
                    {
                        if (attrData.Change != 0 || attrData.ChangeEx != 0)
                        {
                            var attrChange = GameUtils.AttributeValue(nAttrId, attrData.Change);
                            //strDic = "{0}+:{1}[00ff00](+{2})[-]";
                            strDic = StrDic230032;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue, attrChange);
                        }
                        else
                        {
                            //strDic = "{0}+:{1}";
                            strDic = StrDic230025;
                            DataModel.BaseAttrStr[i] = string.Format(strDic, attrName, attrValue);
                        }
                    }
                }
                else
                {
                    DataModel.BaseAttrStr[i] = "";
                }
            }

            strDic = StrDic230025;
            //strDic = "{0}+:{1}";
            for (var i = 0; i != 2; ++i)
            {
                var nAttrId = tbEquip.BaseFixedAttrId[i];
                if (nAttrId != -1)
                {
                    var attrName = GameUtils.AttributeName(nAttrId);
                    var attrValue = GameUtils.AttributeValue(nAttrId, tbEquip.BaseFixedAttrValue[i]);
                    DataModel.AddAttrStr[i] = string.Format(strDic, attrName, attrValue);
                }
                else
                {
                    DataModel.AddAttrStr[i] = "";
                }
            }

            //灵魂、卓越、字符串显示
            DataModel.StrExcellent = "";
            DataModel.StrSoul = "";
            var min = 0;
            var minbool = false;
            var max = 0;

            if (type == 1)
            {
                //取决于材料
                DataModel.StrAppend = GameUtils.GetDictionaryText(300836);
            }
            else
            {
                if (args.Exdata == null)
                {
                    //随机数值
                    if (tbEquip.JingLianDescId == -1)
                    {
                        DataModel.StrAppend = GameUtils.GetDictionaryText(300837);
                    }
                    else
                    {
                        DataModel.StrAppend = tbEquip.JingLianDescId.ToString();
                    }
                }
                else
                {
                    DataModel.StrAppend = string.Empty;
                }
            }


            if (type == 1)
            {
                //取决于材料装备
                DataModel.StrExcellent = GameUtils.GetDictionaryText(300838);
            }
            else
            {
                if (tbEquip.ExcellentAttrCount != -1)
                {
                    var tbEquipRalate = Table.GetEquipRelate(tbEquip.ExcellentAttrCount);
                    if (tbEquipRalate == null)
                    {
                        return;
                    }
                    for (var i = 0; i < tbEquipRalate.AttrCount.Length; i++)
                    {
                        if (tbEquipRalate.AttrCount[i] > 0)
                        {
                            max = i;
                            if (!minbool)
                            {
                                minbool = true;
                                min = i;
                            }
                        }
                    }
                    if (min != 0)
                    {
                        if (tbEquip.ZhuoYueDescId == -1)
                        {
                            if (min == max)
                            {
                                DataModel.StrExcellent = min + GameUtils.GetDictionaryText(300839); //"条随机属性";
                            }
                            else
                            {
                                DataModel.StrExcellent = string.Format("{0}-{1}" + GameUtils.GetDictionaryText(300839), min, max);
                            }
                            DataModel.ExcellentHeight = 16;
                        }
                        else
                        {
                            DataModel.StrExcellent = GameUtils.GetDictionaryText(tbEquip.ZhuoYueDescId); //"条随机属性";
                            string[] subts = DataModel.StrExcellent.Split('\n');
                            DataModel.ExcellentHeight = (subts.Length) * 16;
                        }
                    }
                }
            }


            if (type == 1)
            {
                DataModel.StrSoul = GameUtils.GetDictionaryText(300840);
            }
            else
            {
                if (tbEquip.RandomAttrCount != -1)
                {
                    var tbEquipRalate = Table.GetEquipRelate(tbEquip.RandomAttrCount);
                    if (tbEquipRalate == null)
                    {
                        return;
                    }
                    min = 0;
                    minbool = false;
                    max = 0;
                    for (var i = 0; i < tbEquipRalate.AttrCount.Length; i++)
                    {
                        if (tbEquipRalate.AttrCount[i] > 0)
                        {
                            max = i;
                            if (!minbool)
                            {
                                minbool = true;
                                min = i;
                            }
                        }
                    }
                    if (min != 0)
                    {
                        if (tbEquip.LingHunDescId == -1)
                        {
                            if (min == max)
                            {
                                DataModel.StrSoul = min + GameUtils.GetDictionaryText(300839); //"条随机属性";
                            }
                            else
                            {
                                DataModel.StrSoul = string.Format("{0}-{1}" + GameUtils.GetDictionaryText(300839), min, max);
                            }
                            DataModel.SouleHeight = 16;
                        }
                        else
                        {
                            DataModel.StrSoul = GameUtils.GetDictionaryText(tbEquip.LingHunDescId); //"条随机属性";
                            string[] subts = DataModel.StrExcellent.Split('\n');
                            DataModel.SouleHeight = (subts.Length) * 16;
                        }
                    }
                }
            }


            //套装相关
            for (var i = 0; i < 10; i++)
            {
                DataModel.TieCount[i] = 0;
            }
            DataModel.TieId = tbEquip.TieId;
            var nNowTieCount = 0;
            for (var i = 0; i != 4; ++i)
            {
                DataModel.TieAttrCount[i] = 0;
            }

            if (tbEquip.TieId == -1)
            {
                return;
            }
            var tbTie = Table.GetEquipTie(tbEquip.TieId);
            if (tbTie == null)
            {
                return;
            }

            PlayerDataManager.Instance.ForeachEquip(item =>
            {
                var ItemId = item.ItemId;
                if (ItemId == -1)
                {
                    return;
                }
                var tbTieItem = Table.GetItemBase(ItemId);
                if (tbTieItem == null)
                {
                    return;
                }
                var tbTieEquip = Table.GetEquipBase(tbTieItem.Exdata[0]);
                if (tbTieEquip == null)
                {
                    return;
                }
                if (tbEquip.TieId == tbTieEquip.TieId)
                {
                    DataModel.TieCount[tbTieEquip.TieIndex] = 1;
                    nNowTieCount++;
                }
            });

            DataModel.TieNowCount = nNowTieCount;
            for (var i = 0; i != 4; ++i)
            {
                if (nNowTieCount >= tbTie.NeedCount[i])
                {
                    DataModel.TieAttrCount[i] = 1;
                }
            }
        }

        private void OnEquipGetClickEvent(IEvent ievent)
        {
            var _e = ievent as Event_EquipInfoClick;
            var _item = DataModel.GetPathList[_e.Index];
            var _tbItemGet = Table.GetItemGetInfo(_item.ItemGetId);
            if (_tbItemGet.IsShow == -1) //开启条件
            {
                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipInfoUI));
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

                EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EquipInfoUI));
                GameUtils.GotoUiTab(_tbItemGet.UIName, _tbItemGet.Param[0], _tbItemGet.Param[1], _tbItemGet.Param[2]);
            }

        }


        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
    }
}