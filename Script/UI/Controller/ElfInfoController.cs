/********************************************************************************* 

                         Scorpion



  *FileName:ElfMsgFrameCtrler

  *Version:1.0

  *Date:2017-06-013

  *Description:

**********************************************************************************/  
#region using

using System.Collections.Generic;
using System.ComponentModel;
using ClientDataModel;
using DataTable;
using ScriptManager;
using Shared;

#endregion

namespace ScriptController
{
    public class ElfMsgFrameCtrler : IControllerBase
    {
        #region 成员变量

        private ElfInfoDataModel DataModel;

        #endregion

        #region 构造函数

        public ElfMsgFrameCtrler()
        {
            CleanUp();
        }

        #endregion

        #region 固有函数

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as ElfInfoArguments;
            if (_args.DataModel == null)
            {
                if (_args.ItemId == -1)
                {
                    return;
                }

                var _item = new ElfItemDataModel();
                GameUtils.BuildShowElfExData(_item, _args.ItemId);
                _item.ItemId = _args.ItemId;

                DataModel.ItemData = _item;
            }
            else
            {
                DataModel.ItemData = _args.DataModel;
            }

            var _strDic230025 = GameUtils.GetDictionaryText(230025);
            var _strDic230033 = GameUtils.GetDictionaryText(230033);
            DataModel.ShowBtn = _args.ShowButton;
            var _fightAttr = new Dictionary<int, int>();
            var _tbItem = Table.GetItemBase(DataModel.ItemData.ItemId);
            var _tbElf = Table.GetElf(_tbItem.Exdata[0]);
            var _level = DataModel.ItemData.Exdata.Level;
            for (var i = 0; i < 6; i++)
            {
                var _id = _tbElf.ElfInitProp[i];
                var _value = _tbElf.ElfProp[i];
                DataModel.BaseAttr[i].Reset();
                if (_id != -1)
                {
                    var _valuelevel = _tbElf.GrowAddValue[i];
                    _value += _valuelevel * (_level - 1);

                    GameUtils.SetAttributeBase(DataModel.BaseAttr, i, _id, _value);
                    //value = GameUtils.EquipAttrValueRef(id, value);
                    _fightAttr.modifyValue(_id, _value);
                }
                else
                {
                    DataModel.BaseAttr[i].Reset();
                }
            }

            for (var i = 0; i < 6; i++)
            {
                var _attr = DataModel.BaseAttr[i];
                var _attrType = _attr.Type;
                if (_attrType != -1)
                {
                    var _str = "";
                    var _attrName = GameUtils.AttributeName(_attrType);
                    var _attrValue = GameUtils.AttributeValue(_attrType, _attr.Value);

                    if (_attr.ValueEx == 0)
                    {
                        _str = string.Format(_strDic230025, _attrName, _attrValue);
                    }
                    else
                    {
                        var _attrValueEx = GameUtils.AttributeValue(_attrType, _attr.ValueEx);
                        _str = string.Format(_strDic230033, _attrName, _attrValue, _attrValueEx);
                    }
                    DataModel.BaseAttrStr[i] = _str;
                }
                else
                {
                    DataModel.BaseAttrStr[i] = "";
                }
            }


            var starLevel = DataModel.ItemData.Exdata.StarLevel;
            var tbElf = _tbElf;
            for (var i = 0; i < tbElf.StarAttrId.Length; i++)
            {
                if (tbElf.StarAttrId[i] != -1)
                {
                    var id = tbElf.StarAttrId[i];
                    var value = tbElf.StarAttrValue[i];
                    if (i < starLevel)
                    {
                        GameUtils.SetAttributeBase(DataModel.InnateAttr, i, id, value);
                        _fightAttr.modifyValue(id, value);
                        DataModel.InnateExtra[i] = GameUtils.GetDictionaryText(100002140);
                        DataModel.InnateExtraColor[i] = "ADFF00";
                    }
                    else
                    {
                        GameUtils.SetAttributeBase(DataModel.InnateAttr, i, id, value);
                        DataModel.InnateExtra[i] = GameUtils.GetDictionaryText(100002135 + i); ;
                        DataModel.InnateExtraColor[i] = "888888";
                    }
                }
                else
                {
                    DataModel.InnateAttr[i].Reset();
                }
            }
            var tbItemBase = Table.GetItemBase(DataModel.ItemData.ItemId);
            if (null != tbItemBase)
            {
                DataModel.FightPoint = tbItemBase.Exdata[3];
            }
            DataModel.HaveBuff = false;
            for (var i = 0; i < DataModel.ElfBuffList.Count; ++i)
            {
                var buffId = DataModel.ItemData.Exdata[(int)ElfExdataDefine.BuffId1 + i * 2];
                DataModel.ElfBuffList[i].BuffId = buffId;
                DataModel.ElfBuffList[i].BuffLevel = DataModel.ItemData.Exdata[(int)ElfExdataDefine.BuffLevel1 + i * 2];
                if (buffId > 0)
                {
                    DataModel.HaveBuff = true;
                }
            }

            if (DataModel.ItemData.Exdata.Level > PlayerDataManager.Instance.GetLevel())
            {
                DataModel.ItemData.Exdata.Level = 1;
            }

            var _tbLevel = Table.GetLevelData(DataModel.ItemData.Exdata.Level);

            DataModel.SellCount = _tbElf.ResolveCoef[0] * _tbLevel.ElfResolveValue / 100 + _tbElf.ResolveCoef[1];

            var _elfController = UIManager.Instance.GetController(UIConfig.ElfUI);
            for (var i = 0; i < DataModel.SingleGroups.Count; i++)
            {
                var _groupId = _tbElf.BelongGroup[i];
                var _info = DataModel.SingleGroups[i];
                if (_groupId != -1)
                {
                    var _tbElfGroup = Table.GetElfGroup(_groupId);
                    var _param = new object[]
                    {
                        _info,
                        _tbElfGroup,
                        DataModel.ItemData.Index,
                        true
                    };
                    _elfController.CallFromOtherClass("SetGroupAttr", _param);
                }
                else
                {
                    _info.Reset();
                }
            }
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

        public void OnShow()
        {
            var tbItem = Table.GetItemBase(DataModel.ItemData.ItemId);
            if (tbItem.Type == 40000)
            {
                var tbElf = Table.GetElf(tbItem.Exdata[0]);
                if (tbElf == null)
                    return;

                var tbCharacter = Table.GetCharacterBase(tbElf.ElfModel);
                if (tbCharacter == null)
                    return;

                var tbCharModel = Table.GetCharModel(tbCharacter.CharModelID);
                if (tbCharModel == null)
                    return;

                var e = new EventSystem.ItemInfoMountModelDisplay_Event(Resource.Dir.Model + tbCharModel.ResPath, tbCharModel.AnimPath + "/Stand.anim", (float)tbCharModel.Scale * 2.5f);
                EventSystem.EventDispatcher.Instance.DispatchEvent(e);
            }
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public FrameState State { get; set; }
        public void CleanUp()
        {
            DataModel = new ElfInfoDataModel();
        }

        #endregion

    }
}