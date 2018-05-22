/********************************************************************************* 

                         Scorpion



  *FileName:UIBeginnerGuidanceFrameCtrler

  *Version:1.0

  *Date:2017-07-12

  *Description:

**********************************************************************************/  


#region using

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
    public class UIBeginnerGuidanceFrameCtrler : IControllerBase
    {

        #region 静态变量

        private static bool s_bInit;
        private static Dictionary<int, GuidanceRecord> s_dicTableCache = new Dictionary<int, GuidanceRecord>();
        private static List<Vector2> s_listAnchorPosition = new List<Vector2>(5);

        #endregion

        #region 成员变量

        private GuideDataModel m_DataModel = new GuideDataModel();

        #endregion

        #region 构造函数

        public UIBeginnerGuidanceFrameCtrler()
        {
            if (0 == s_listAnchorPosition.Count)
            {
                var size = NGUITools.screenSize;
                var root = GameUtils.GetUiRoot();
                if (root != null)
                {
                    var s = (float)root.activeHeight / Screen.height;
                    var height = Mathf.CeilToInt(Screen.height * s);
                    var width = Mathf.CeilToInt(Screen.width * s);
                    size.x = width;
                    size.y = height;
                }
                s_listAnchorPosition.Add(Vector2.zero);
                s_listAnchorPosition.Add(new Vector2(-size.x / 2, size.y / 2));
                s_listAnchorPosition.Add(new Vector2(size.x / 2, size.y / 2));
                s_listAnchorPosition.Add(new Vector2(size.x / 2, -size.y / 2));
                s_listAnchorPosition.Add(new Vector2(-size.x / 2, -size.y / 2));
            }

            CleanUp();
            EventDispatcher.Instance.AddEventListener(UIEvent_UpdateGuideEvent.EVENT_TYPE, e => { UpgradeNewStep(); });

        }

        #endregion

        #region 固有函数

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
            return m_DataModel;
        }

        public void CleanUp()
        {
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

        public FrameState State { get; set; }

        #endregion

        #region 逻辑函数

        private void HollowLogoMoveToBagProp(eBagType bagType, int itemId)
        {
            const float _ItemIconWidth = 80;
            const int _ItemColumn = 5;
            var _bag = PlayerDataManager.Instance.GetBag((int)bagType);
            if (null == _bag)
            {
                Logger.Debug("PlayerDataManager.Instance.GetBag((int)bagType)");
                return;
            }

            var _idx = -1;
            for (var i = 0; i < _bag.Items.Count; i++)
            {
                var _item = _bag.Items[i];
                if (null == _item || itemId != _item.ItemId)
                {
                    continue;
                }

                _idx = i;
                break;
            }

            if (-1 == _idx)
            {
                Logger.Debug("Can't find item[{0}]", itemId);
                return;
            }
            var _offset = m_DataModel.PointerPos - m_DataModel.HollowPos;
            var _temp = m_DataModel.HollowPos;
            var _offsetX = _idx % _ItemColumn * _ItemIconWidth;
            float _offsetY = 5;
            _temp.x += _offsetX;
            //temp.y -= offsetY;
            m_DataModel.HollowPos = _temp;

            m_DataModel.PointerPos = m_DataModel.HollowPos + _offset;
        }

        private void OnGuidance11()
        {
            var _equipId = -1;
            var _type = PlayerDataManager.Instance.GetRoleId();
            if (0 == _type)
            {
                _equipId = 213101;
            }
            else if (1 == _type)
            {
                _equipId = 313101;
            }
            else if (2 == _type)
            {
                _equipId = 413101;
            }

            if (-1 == _equipId)
            {
                return;
            }

            HollowLogoMoveToBagProp(eBagType.Equip, _equipId);
        }

        private void OnGuidance201()
        {
            var _itemId = -1;
            var _type = PlayerDataManager.Instance.GetRoleId();
            if (0 == _type)
            {
                _itemId = 20093;
            }
            else if (1 == _type)
            {
                _itemId = 20192;
            }
            else if (2 == _type)
            {
                _itemId = 20290;
            }

            if (-1 == _itemId)
            {
                Logger.Error("OnGuilde201 can't find item");
                return;
            }

            HollowLogoMoveToBagProp(eBagType.BaseItem, _itemId);
        }


        private void OnGuidance71()
        {
            var _itemId = -1;
            var _type = PlayerDataManager.Instance.GetRoleId();
            if (0 == _type)
            {
                _itemId = 20091;
            }
            else if (1 == _type)
            {
                _itemId = 20191;
            }
            else if (2 == _type)
            {
                _itemId = 20291;
            }

            if (-1 == _itemId)
            {
                Logger.Error("OnGuilde71 can't find item");
                return;
            }

            HollowLogoMoveToBagProp(eBagType.BaseItem, _itemId);
        }

        private void OnGuidabceNewFunc(string btnName)
        {
            //var _p = MainButton.GetOriginPosition() + m_DataModel.HollowPos;
            //m_DataModel.HollowPos = _p;
            //m_DataModel.PointerPos = m_DataModel.HollowPos + m_DataModel.PointerPos;
        }

        private void OnGuidanceTalent()
        {
            var _itemId = -1;
            var _type = PlayerDataManager.Instance.GetRoleId();
            if (0 == _type)
            {
                _itemId = 20002;
            }
            else if (1 == _type)
            {
                _itemId = 20101;
            }
            else if (2 == _type)
            {
                _itemId = 20200;
            }

            if (-1 == _itemId)
            {
                return;
            }
            HollowLogoMoveToBagProp(eBagType.BaseItem, _itemId);
        }

        private void OnNextStepBefore(int id)
        {
            if (id == 811)
            {
                UIManager.Instance.OpenDefaultFrame();
            }
            else if (id == 791)
            {
                UIManager.Instance.OpenDefaultFrame();
            }
        }
        private void OnUpgradeNewStep(int id)
        {
            if (id == 11)
            {
                OnGuidance11();
            }
            else if (id == 71)
            {
                OnGuidance71();
            }
            else if (id == 201)
            {
                OnGuidance201();
            }

            else if (id == 791 || id == 811)
            {
                EventDispatcher.Instance.DispatchEvent(new RestoreMainUIMenu(true));
            }

        }

        private void UpgradeNewStep()
        {
            var _data = GuideManager.Instance.GetCurrentGuideData();
            if (null == _data)
            {
                return;
            }
            OnNextStepBefore(_data.Id);

            m_DataModel.Label = _data.Desc;
            m_DataModel.LabelWidth = _data.FontX;
            m_DataModel.LabelHeight = _data.FontY;
            m_DataModel.LabelPos = new Vector3(_data.PosX, _data.PoxY, 0);
            m_DataModel.ImageId = _data.Icon;
            m_DataModel.ImageIdPos = new Vector3(_data.IconX, _data.IconY, 0);
            m_DataModel.ClickAnyWhereToNext = 0 != _data.NextStep;
            var tb = Table.GetColorBase(_data.Color);
            if (tb == null)
            {
                m_DataModel.Col = MColor.white;
            }
            else
            {
                m_DataModel.Col = new Color(tb.Red / 255.0f, tb.Green / 255.0f, tb.Blue / 255.0f, _data.Transparency / 255.0f);
            }
            var orginalPoint = (_data.CenterPoint >= 0 && _data.CenterPoint < s_listAnchorPosition.Count)
                ? s_listAnchorPosition[_data.CenterPoint]
                : s_listAnchorPosition[0];

            m_DataModel.HollowSizeX = _data.SeeSizeX;
            m_DataModel.HollowSizeY = _data.SeeSizeY;
            m_DataModel.HollowPos = new Vector3(orginalPoint.x + _data.SeePosX, orginalPoint.y + _data.SeePosY, 0);
            //DataModel.HollowPos = new Vector3(data.SeePosX,data.SeePosY, 0);
            m_DataModel.ShowPointer = 0 != _data.IsShowPointer;
            m_DataModel.PointerPos = new Vector3(orginalPoint.x + _data.PointerX, orginalPoint.y + _data.PointerY, 0);
            m_DataModel.PointerAngel = _data.Rotation;
            m_DataModel.Skippable = 0 != _data.IsSkip;

            if (!string.IsNullOrEmpty(_data.BtnPath))
            {
                var idx = _data.BtnPath.IndexOf('/');
                var uiName = _data.BtnPath.Substring(0, idx);
                var objPath = _data.BtnPath.Substring(idx+1, _data.BtnPath.Length-idx-1);

                var ui = GameObject.Find(uiName);
                if (null == ui)
                {
                    Logger.Error("StepByStep Error[{0}]: Cannot find UI{1}", _data.Id, _data.BtnPath);
                }
                else
                {
                    var btn = ui.transform.Find(objPath);
                    if (null == btn)
                    {
                        Logger.Error("StepByStep Error[{0}]: Cannot find obj{1}", _data.Id, _data.BtnPath);
                    }
                    else
                    {
                        var widget = btn.GetComponent<UIWidget>();
                        if (null != widget)
                        {
                            m_DataModel.HollowPos = widget.transform.root.InverseTransformPoint(btn.position);
						
						
                        }
                        else
                        {
                            Logger.Error("StepByStep Error[{0}]: obj{1}  needs UIWidget", _data.Id, _data.BtnPath);
                        }
                    } 
                }
            }
	    
		

            OnUpgradeNewStep(_data.Id);

            Logger.Debug("UpdateNextStep id=[{0}], pos=[{1},{2}]", _data.Id, m_DataModel.HollowPos.x, m_DataModel.HollowPos.y);

            EventDispatcher.Instance.DispatchEvent(new UIEvent_NextGuideEvent(_data.DelayTime * 0.001f));
        }

        #endregion
           
    }
}