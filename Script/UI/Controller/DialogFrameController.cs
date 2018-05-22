/********************************************************************************* 

                         Scorpion



  *FileName:DiaJoumalFrameCtrler

  *Version:1.0

  *Date:2017-06-12

  *Description:

**********************************************************************************/  
#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using ScriptManager;
using ClientDataModel;
using EventSystem;

#endregion

namespace ScriptController
{
    public class DiaJoumalFrameCtrler : IControllerBase
    {
 
        #region ��Ա����

        private DialogueDataModel DataModel = new DialogueDataModel();
        private Action m_Callback;
        private List<DialogueData> m_listDialogue;
        private FrameState m_State;

        #endregion

        #region ���캯��

        public DiaJoumalFrameCtrler()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(Event_ShowDialogue.EVENT_TYPE, UpgradeDialogueMsg);
            EventDispatcher.Instance.AddEventListener(Event_ShowNextDialogue.EVENT_TYPE, evn => { RevealFollowingDialogue(); });
        }

        #endregion

        #region ���к���

        public void Close()
        {
            DataModel.ModelId = -2;
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments data)
        {
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void CleanUp()
        {
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

        public FrameState State
        {
            get { return m_State; }
            set { m_State = value; }
        }

        #endregion

        #region �߼�����

        private void RevealFollowingDialogue()
        {
            if (m_listDialogue.Count > 0)
            {
                DataModel.DialogContent = "    " + m_listDialogue[0].DialogContent;
                DataModel.ModelId = m_listDialogue[0].NpcDataId;
                DataModel.CharacterName = m_listDialogue[0].Name;
                DataModel.IsBlurBackground = m_listDialogue[0].IsBlurBackground;            
                m_listDialogue.RemoveAt(0);
                return;
            }

            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.DialogFrame));
            if (null != m_Callback)
            {
                m_Callback();
            }
        }
 
        #endregion       

        #region �¼�

        private void UpgradeDialogueMsg(IEvent ievent)
        {
            var _evn = ievent as Event_ShowDialogue;
            m_listDialogue = _evn.Dialogue;
            m_Callback = _evn.Callback;

            RevealFollowingDialogue();
        }

        #endregion
    }
}