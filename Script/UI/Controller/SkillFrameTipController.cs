/********************************************************************************* 

                         Scorpion



  *FileName:SkillFrameTipCtrler

  *Version:1.0

  *Date:2017-07-13

  *Description:

**********************************************************************************/
#region using

using System;
using System.ComponentModel;
using ClientDataModel;
using DataTable;

#endregion

namespace ScriptController
{
    public class SkillFrameTipCtrler : IControllerBase
    {
        #region 构造函数
        public SkillFrameTipCtrler()
        {
            CleanUp();
        }
        #endregion

        #region 成员变量
        private int currentShowTalentId;
        private SkillDataModel DataModel;
        private int mEquipSkillDirtyMark;
        private bool skillChanged;
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new SkillDataModel();
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
            var _args = data as SkillTipFrameArguments;
            var _skillData = Table.GetSkill(_args.idSkill);
            DataModel.MieshiSkillItem.SkillId = _args.idSkill;
            DataModel.MieshiSkillItem.SkillName = _skillData.Name;
            DataModel.MieshiSkillItem.SkillNeedMp = _skillData.NeedMp;
            DataModel.MieshiSkillItem.SkillTargetConut = _skillData.TargetCount;
            DataModel.MieshiSkillItem.SkillCD = _skillData.Cd;
            DataModel.MieshiSkillItem.SkillDes = _skillData.Desc;
            if (_args.idNextSkill > 0)
            {
                DataModel.MieshiSkillItem.NextLevelSkillDes = Table.GetSkill(_args.idNextSkill).Desc;
            }
            else
            {
                DataModel.MieshiSkillItem.NextLevelSkillDes = "";
            }
            DataModel.MieshiSkillItem.SkillLv = String.Format(Table.GetDictionary(240302).Desc[0], _args.iLevel);
        }



        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public FrameState State { get; set; }
        #endregion

    }
}