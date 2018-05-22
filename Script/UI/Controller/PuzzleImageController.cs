/********************************************************************************* 

                         Scorpion



  *FileName:EnigmaIconFrameCtrler

  *Version:1.0

  *Date:2017-06-08

  *Description:

**********************************************************************************/
#region using

using System.ComponentModel;
using ClientDataModel;

#endregion

namespace ScriptController
{
    public class EnigmaIconFrameCtrler : IControllerBase
    {
        #region 构造函数
        public EnigmaIconFrameCtrler()
        {
            CleanUp();
            // EventDispatcher.Instance.AddEventListener(UIEvent_SkillFrame_SkillSelect.EVENT_TYPE, OnClicSkillItem);
        }
        #endregion

        #region 成员变量
        private PuzzleImageDataModel DataModel;
        #endregion

        #region 固有函数
        public void CleanUp()
        {
            DataModel = new PuzzleImageDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
            var _arg = data as PuzzleImageArguments;
            DataModel.StatueIndex = _arg.StatueIndex;
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
            return null;
        }

        public void OnShow()
        {
        }

        public FrameState State { get; set; }
        #endregion


    }
}