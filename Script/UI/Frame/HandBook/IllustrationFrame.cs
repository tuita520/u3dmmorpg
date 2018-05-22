using ScriptManager;
using ScriptController;
using System;
#region using

using EventSystem;
using UnityEngine;
using DataTable;
using System.Collections.Generic;
#endregion

namespace GameUI
{
	public class IllustrationFrame : MonoBehaviour
	{
	    public GameObject AnimationBlocker;
	    public BindDataRoot Binding;
	    //private Vector3 lastPos;
	    //private float offset;
	    private bool isRemoveBind = true;

        public CreateFakeCharacter ModelRoot;
        private Transform ModelRootTransform;
        private Vector3 ModelRootOriPos;
	    //public UIToggle[] mToggle;
	    public GameObject tips;
	    private void Awake()
	    {
#if !UNITY_EDITOR
try
{
#endif

            if (ModelRoot != null)
            {
                ModelRootTransform = ModelRoot.transform;
            }
            if (ModelRootTransform != null)
            {
                ModelRootOriPos = ModelRootTransform.localPosition;
            }
	    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	    public void OnClickTips()
	    {
	        tips.SetActive(true);
	    }

	    public void OnReleaseTips()
	    {
	        tips.SetActive(false);
	    }
	    
	
	    public void OnBookInfoClose()
	    {
	        var e = new UIEvent_HandBookFrame_OnBookClick(null);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnButtonClose()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.HandBook, true));
	    }
	
	    private void OnUiCloseRemove(IEvent ievent)
	    {
	        var e = ievent as CloseUiBindRemove;
	        if (e.Config != UIConfig.HandBook)
	        {
	            return;
	        }
	        if (e.NeedRemove == 0)
	        {
	            isRemoveBind = false;
	        }
	        else
	        {
	            if (isRemoveBind == false)
	            {
	                DeleteBindListener();
	            }
	            isRemoveBind = true;
	        }
	    }
	
	    public void OnComposeButtonClick()
	    {
	        var e = new UIEvent_HandBookFrame_ComposeBookPieceFromBookInfo();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnComposeCardClick()
	    {
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_ComposeBookCardFromBookInfo());
	    }
	
	    private void OnDestroy()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        if (isRemoveBind == false)
	        {
	            DeleteBindListener();
	        }
	        isRemoveBind = true;
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
	        EventDispatcher.Instance.RemoveEventListener(UIEvent_HandBookFrame_ShowAnimationBlocker.EVENT_TYPE, SetAnimBlocker);
            EventDispatcher.Instance.RemoveEventListener(HandbookRefreshMonster_Event.EVENT_TYPE, CreateFakeObj);
	        if (isRemoveBind)
	        {
	            DeleteBindListener();
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
	
	        AnimationBlocker.SetActive(false);
	        EventDispatcher.Instance.AddEventListener(UIEvent_HandBookFrame_ShowAnimationBlocker.EVENT_TYPE, SetAnimBlocker);
	
            EventDispatcher.Instance.AddEventListener(HandbookRefreshMonster_Event.EVENT_TYPE, CreateFakeObj);
	        if (isRemoveBind)
	        {
	            EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnUiCloseRemove);
	
	            var controllerBase = UIManager.Instance.GetController(UIConfig.HandBook);
	            if (controllerBase == null)
	            {
	                return;
	            }
	            Binding.SetBindDataSource(controllerBase.GetDataModel(""));
	            Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);
                

                if (PlayerDataManager.Instance.mFightBook > 0)
	                InitModel(PlayerDataManager.Instance.mFightBook);
	            else
                {
                    BesideInstructionFrameCtrler control = controllerBase as BesideInstructionFrameCtrler;
                    if (control != null)
                    {
                        if ((control.GetDataModel("") as ClientDataModel.HandBookDataModel).HasBooks.Count > 0)
                        {
                            InitModel((control.GetDataModel("") as ClientDataModel.HandBookDataModel).HasBooks[0].BookId);
                        }
                    }
                }
            }
	        isRemoveBind = true;
	        
#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void OnGetBookClick()
	    {
	        var e = new UIEvent_HandBookFrame_OnGetBookClick();
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    private void DeleteBindListener()
	    {
	        Binding.RemoveBinding();
	        EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnUiCloseRemove);
	    }
	
	   
	
	    private void SetAnimBlocker(IEvent ievent)
	    {
	        var e = ievent as UIEvent_HandBookFrame_ShowAnimationBlocker;
	        AnimationBlocker.SetActive(e.bShow);
	    }
	
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }


	    private void InitModel(int dataId)
	    {

            if (ModelRoot != null)
            {
                ModelRoot.DestroyFakeCharacter();
            }
            if (-1 == dataId)
            {
                return;
            }
	        var tbBook = Table.GetHandBook(dataId);
	        if (tbBook == null)
	        {
	            return;
	        }

            var tableNpc = Table.GetCharacterBase(tbBook.NpcId);
            if (null == tableNpc)
            {
                return;
            }
            if (ModelRoot != null)
            {
                ModelRoot.Create(tbBook.NpcId, null, character =>
                {
                    character.SetScale(tableNpc.CameraMult / 10000f);
                    character.ObjTransform.localRotation = Quaternion.identity;
                    ModelRootTransform.localPosition = ModelRootOriPos + new Vector3(0, tableNpc.CameraHeight / 10000.0f, 0);
                    character.PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                });
            }

	    }
        private void CreateFakeObj(IEvent ievent)
        {
            int dataId = (ievent as HandbookRefreshMonster_Event)._id;

            InitModel(dataId);
        }

	    public void OnClickChangeFight()
	    {
	        //mToggle[1].Set(true);
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_HandBookFrame_OnFightClick(-1));
	    }
	}
}