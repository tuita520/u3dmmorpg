using System.ComponentModel;
using ClientDataModel;
using DataTable;
using EventSystem;
using UnityEngine;
using System;

namespace ScriptController
{
    public class BossHomeFrame : MonoBehaviour
    {
        public BossHomeDataModel DataModel;
        public BindDataRoot Binding;
        private bool removeBind = true;

        public UIDragRotate DrageRotate;
        public CreateFakeCharacter ModelRoot;
        private int UniqueResourceId;
        private static int sUniqueResourceId = 12345;
        public void CloseUI()
        {
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.BossHomeUI));
        }
        private void OnEnable()
        {
#if !UNITY_EDITOR
try
{
#endif
            if (removeBind)
            {
                var controller = UIManager.Instance.GetController(UIConfig.BossHomeUI);
                DataModel = controller.GetDataModel("") as BossHomeDataModel;
                Binding.SetBindDataSource(DataModel);
                DataModel.PropertyChanged += OnEventPropertyChanged;
                CreateCopyObj(DataModel.ModelId);
            }
            removeBind = true;

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }
        private void OnDisable()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (removeBind)
            {
                RemoveBindingEvent();
            }
            CreateCopyObj(-1);
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
try
{
#endif


            if (removeBind == false)
            {
                RemoveBindingEvent();
            }
            removeBind = true;


#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }
        private void RemoveBindingEvent()
        {
            EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUIBindingRemove);

            Binding.RemoveBinding();
            DataModel.PropertyChanged -= OnEventPropertyChanged;
        }

        private void OnEventPropertyChanged(object o, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "ModelId")
            {
                CreateCopyObj(DataModel.ModelId);
            }
        }

        public static int GetNextUniqueResourceId()
        {
            return sUniqueResourceId++;
        }

        private void CreateCopyObj(int dataId)
        {
            if (-1 == dataId)
            {
                ModelRoot.DestroyFakeCharacter();
                return;
            }

            var tbChar = Table.GetCharacterBase(dataId);


            ModelRoot.Create(dataId, null, character =>
            {
                character.transform.forward = Vector3.back;
                character.PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                character.transform.localScale = Vector3.one * tbChar.CameraMult * 0.0001f;
                DrageRotate.Target = character.transform;
            });


        }
        private void OnCloseUIBindingRemove(IEvent ievent)
        {
            var e = ievent as CloseUiBindRemove;
            if (e.Config != UIConfig.BossHomeUI)
            {
                return;
            }
            if (e.NeedRemove == 0)
            {
                removeBind = false;
            }
            else
            {
                if (removeBind == false)
                {
                    RemoveBindingEvent();
                }
                removeBind = true;
            }
        }
        public void EnterBossHome()
        {
            EventDispatcher.Instance.DispatchEvent(new UIBossHomeOperationClickEvent());
        }
    }
}
