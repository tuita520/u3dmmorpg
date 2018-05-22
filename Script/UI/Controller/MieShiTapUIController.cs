using System.ComponentModel;

namespace ScriptController
{
    public class MieShiTapUIController : IControllerBase
    {
        public MieShiTapUIController()
        {
            CleanUp();
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
            return null;
        }

        public FrameState State { get; set; }
    }
}