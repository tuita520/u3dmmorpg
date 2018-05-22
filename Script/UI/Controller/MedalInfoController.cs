
/********************************************************************************* 

                         Scorpion




  *FileName:MedalInfoController

  *Version:1.0

  *Date:2017-06-28

  *Description:

**********************************************************************************/
#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using ClientDataModel;
using DataTable;

#endregion

namespace ScriptController
{
    public class MedalMsgFrameCtrler : IControllerBase
    {


        #region 静态变量

        #endregion

        #region 成员变量
        private  Dictionary<int, string> m_medalType;
        private MedalInfoDataModel MedalInfoData { get; set; }
        #endregion

        #region 构造函数
        public MedalMsgFrameCtrler()
        {
            #region 勋章初始化

            m_medalType = new Dictionary<int, string>();
            m_medalType.Add(0, GameUtils.GetDictionaryText(230201));
            m_medalType.Add(1, GameUtils.GetDictionaryText(230202));
            m_medalType.Add(2, GameUtils.GetDictionaryText(230203));
            m_medalType.Add(3, GameUtils.GetDictionaryText(230204));
            m_medalType.Add(4, GameUtils.GetDictionaryText(230205));
            m_medalType.Add(5, GameUtils.GetDictionaryText(230206));
            m_medalType.Add(6, GameUtils.GetDictionaryText(230207));
            m_medalType.Add(7, GameUtils.GetDictionaryText(230208));
            m_medalType.Add(8, GameUtils.GetDictionaryText(230209));
            m_medalType.Add(9, GameUtils.GetDictionaryText(230210));
            m_medalType.Add(10, GameUtils.GetDictionaryText(230211));
            m_medalType.Add(11, GameUtils.GetDictionaryText(230212));
            m_medalType.Add(12, GameUtils.GetDictionaryText(230213));
            m_medalType.Add(13, GameUtils.GetDictionaryText(230214));

            #endregion

            // EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_SkillSelect.EVENT_TYPE, OnClicSkillItem);
            CleanUp();
        }
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            MedalInfoData = new MedalInfoDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            var _args = data as MedalInfoArguments;
            if (_args != null)
            {
                MedalInfoData = _args.MedalInfoData;
            }
            ItemMsgInit();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return MedalInfoData;
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

        public FrameState State { get; set; }
        #endregion

        #region 事件

        #endregion


   

        private bool ItemMsgInit()
        {
            var _canFind = false;
            var _medalId = MedalInfoData.ItemData.BaseItemId;

            MedalInfoData.ItemPropUI.Clear();

            var _itemBaseTable = Table.GetItemBase(_medalId);
            if (null == _itemBaseTable)
            {
                Logger.Error("Cant find ItemBaseTable !!");
            }

            if (_itemBaseTable.Sell != -1)
            {
                MedalInfoData.SaleMoney = _itemBaseTable.Sell.ToString();
            }
            else
            {
                //不可出售
                MedalInfoData.SaleMoney = GameUtils.GetDictionaryText(270115);
            }

            var _varType = -1;
            var _varValue = -1;
            var medalTable = Table.GetMedal(_itemBaseTable.Exdata[0]);
            MedalInfoData.MedalId = _itemBaseTable.Exdata[0];
            var medalTablePropValueLength0 = medalTable.PropValue.Length;
            for (var i = 0; i < medalTablePropValueLength0; i++)
            {
                _varType = medalTable.AddPropID[i];
                _varValue = medalTable.PropValue[i];
                if (_varValue != -1)
                {
                    var _MAttrUI = new MedalItemAttrDataModal();
                    var _tbProp = Table.GetSkillUpgrading(medalTable.PropValue[i]);
                    _MAttrUI.AttrName = ExpressionHelper.AttrName[_varType];
                    var _value = _tbProp.GetSkillUpgradingValue(MedalInfoData.ItemData.nLevel);
                    _MAttrUI.AttrValue = GameUtils.AttributeValue(_varType, _value);
                    MedalInfoData.ItemPropUI.Add(_MAttrUI);
                }
            }

            var _ss = "";
            MedalInfoData.ItemData.BaseItemId = _itemBaseTable.Exdata[0];
            m_medalType.TryGetValue(medalTable.MedalType, out _ss);
            MedalInfoData.MedalType = _ss;
            var _UpgradingTable = Table.GetSkillUpgrading(medalTable.LevelUpExp);
            MedalInfoData.MaxExp = _UpgradingTable.GetSkillUpgradingValue(MedalInfoData.ItemData.nLevel);
            MedalInfoData.TotalExp = MedalInfoData.ItemData.nExp + "/" + MedalInfoData.MaxExp;
            _canFind = true;
            if (!_canFind)
            {
                Logger.Error("Cant find ItemBaseTable Param !!");
                return false;
            }
            return true;
        }

  
    }
}