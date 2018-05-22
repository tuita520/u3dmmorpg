using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ScriptManager;
using ScriptController;
using DataContract;
using DataTable;
using EventSystem;

public class NewStrongFrame : MonoBehaviour {

    public CreateFakeCharacter ModelRoot;
    public UIDragRotate DragRotate;
    public BindDataRoot Binding;
    private bool mHasBindRemoved = true;
    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (ModelRoot == null)
        {
            ModelRoot = transform.GetComponentInChildren<CreateFakeCharacter>();
        }

        if (DragRotate == null)
        {
            DragRotate = transform.GetComponentInChildren<UIDragRotate>();
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    private void OnEnable()
    {
#if !UNITY_EDITOR
	try
	{
#endif
        if (mHasBindRemoved)
        {
            EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUiBindRemove);
            EventDispatcher.Instance.AddEventListener(UIEvent_NewStrongOperation.EVENT_TYPE, OnOperation);
            EventDispatcher.Instance.AddEventListener(SuitShowModelEvent.EVENT_TYPE, OnShowModel);
            

            var controllerBase = UIManager.Instance.GetController(UIConfig.NewStrongUI);
            if (controllerBase == null)
            {
                return;
            }
            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
        }

        
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
        if (mHasBindRemoved)
        {
            RemoveBindEvent();
        }

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
    }

    public void OnClickChange()
    {
        EventDispatcher.Instance.DispatchEvent(new SuitShowChangeEvent());
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
	try
	{
#endif
        if (mHasBindRemoved == false)
        {
            RemoveBindEvent();
        }
        mHasBindRemoved = true;

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
    }

    private void RemoveBindEvent()
    {
        EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUiBindRemove);
        EventDispatcher.Instance.RemoveEventListener(UIEvent_NewStrongOperation.EVENT_TYPE, OnOperation);
        EventDispatcher.Instance.RemoveEventListener(SuitShowModelEvent.EVENT_TYPE, OnShowModel);
        Binding.RemoveBinding();
    }

    private void OnShowModel(IEvent ievent)
    {
       
    }
    private void OnOperation(IEvent ievent)
    {
        var evt = ievent as UIEvent_NewStrongOperation;
        if (null != evt)
        {
            switch (evt.operation)
            {
                case 2:
                {
                    var equipList = evt.Data as Dictionary<int, int>;
                    if (null != equipList)
                    {
                        NetManager.Instance.StartCoroutine(CreateModelCorountEnumerator(equipList));
                    }
                }
                    break;
            }
        }
    }

    public IEnumerator CreateModelCorountEnumerator(Dictionary<int, int> equipList)
    {
        yield return new WaitForEndOfFrame();
        CreateModel(equipList);
    }

    private void OnEvent_CloseUiBindRemove(IEvent ievent)
    {
        var e = ievent as CloseUiBindRemove;
        if (e.Config != UIConfig.StrongUI)
        {
            return;
        }
        if (e.NeedRemove == 0)
        {
            mHasBindRemoved = false;
        }
        else
        {
            if (mHasBindRemoved == false)
            {
                RemoveBindEvent();
            }
            mHasBindRemoved = true;
        }
    }

    private void CreateModel(Dictionary<int,int> equipList )
    {
        var player = ObjManager.Instance.MyPlayer;
        var controllerBase = UIManager.Instance.GetController(UIConfig.NewStrongUI);
        if (controllerBase == null)
        {
            return;
        }
        var ctrl = controllerBase;
        if (ctrl == null)
            return;

        ModelRoot.Create(player.GetDataId(), equipList, (character) =>
        {
            DragRotate.Target = character.transform;
            var roleId = PlayerDataManager.Instance.GetRoleId();
            int effectId = 0;
            if ((int)ctrl.CallFromOtherClass("mChange",null) == 1)
            {
                if (roleId == 0)
                    effectId = 404;
                else if (roleId == 1)
                    effectId = 402;
                else
                    effectId = 403;
            }
            else
            {
                if (roleId == 0)
                    effectId = 405;
                else if (roleId == 1)
                    effectId = 405;
                else
                    effectId = 405;                
            }

            var tableEffct = Table.GetEffect(effectId);
            EffectRef effectRef = new EffectRef();
            effectRef.TypeId = tableEffct.Id;
            effectRef.RefCount++;
            EffectManager.Instance.CreateEffect(tableEffct, character, null, (e, id) =>
            {
                var avatar = character.GetComponent<ActorAvatar>();
                e.gameObject.SetLayerRecursive(avatar.Layer);
                e.gameObject.SetRenderQueue(avatar.RenderQueue);
            });



        }, -1, true, 5, true, -1, -1);
    }

    public void OnCloseClick()
    {
        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.NewStrongUI));
    }

}
