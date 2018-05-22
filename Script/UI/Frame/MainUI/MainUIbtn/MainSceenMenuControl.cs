using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ScriptManager;
using DataTable;
using EventSystem;

public class MainSceenMenuControl : MonoBehaviour
{

	public GameObject OpenUpMenuBtn;
	public GameObject CloseUpMenuBtn;
	public GameObject OpenDownMenuBtn;
	public GameObject CloseDownMenuBtn;

	public TweenPosition SkillTween;
	public TweenPosition JoyStickTween;
	public TweenPosition DownMenuTween;
	public TweenPosition MiddleMenuTween;
    public TweenPosition MainUpUITween;//上部小地图左边的全部UI
    public TweenPosition FunctionUITween;//上部小地图下面的UI（有完成度的UI）
    public TweenPosition FubenCloseUITween;//退出副本一排UI（退出、鼓舞等）

	public MainSceenMneuAnimation Line1Ani;
	public MainSceenMneuAnimation Line2Ani;

	public NewFunctionControl NewFunctionCtrl;

    private float HideUITime = 0f;
    private float MaxHideUITime;
	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

       //数据表的菜单弹出后保留时间
        var reverseTime = Table.GetClientConfig(702);
        if (reverseTime == null)
        {
            return;
        }
        MaxHideUITime = float.Parse(reverseTime.Value);     
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	// Use this for initialization
	void Start () 
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

    private void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        PlayerDataManager.Instance.NoticeData.NewMainDownUI = NewFunctionCtrl.IsNoticeMainDown();

		EventDispatcher.Instance.AddEventListener(RestoreMainUIMenu.EVENT_TYPE, RestoreUI);
		EventDispatcher.Instance.AddEventListener(UIEvent_OpenNewFunctionEvent.EVENT_TYPE, OpenNewFunctionEvent);
		EventDispatcher.Instance.AddEventListener(UIEvent_ShowMainButton.EVENT_TYPE, ShowMainUiEvent);
		EventDispatcher.Instance.AddEventListener(MainDownUINoticeFlagRefrsh.EVENT_TYPE, UpdateMainDownNotice);
		EventDispatcher.Instance.AddEventListener(LoadSceneOverEvent.EVENT_TYPE, OnEvent_OnLoadSceneOver);

	    
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
	
	// Update is called once per frame
	void Update ()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (!GuideManager.Instance.IsGuiding())
        {
            if (CloseDownMenuBtn.activeSelf)
            {
                HideUITime += Time.deltaTime;
                if (HideUITime >= MaxHideUITime)
                {
                    CloseDownMenuBtn_Click();
                    HideUITime = 0;
                }
            }
        }		       	
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    void OnDestroy()
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

	void OnDisable()
	{
#if !UNITY_EDITOR
try
{
#endif

		EventDispatcher.Instance.RemoveEventListener(RestoreMainUIMenu.EVENT_TYPE, RestoreUI);
		EventDispatcher.Instance.RemoveEventListener(UIEvent_OpenNewFunctionEvent.EVENT_TYPE, OpenNewFunctionEvent);
		EventDispatcher.Instance.RemoveEventListener(UIEvent_ShowMainButton.EVENT_TYPE, ShowMainUiEvent);
		EventDispatcher.Instance.RemoveEventListener(MainDownUINoticeFlagRefrsh.EVENT_TYPE, UpdateMainDownNotice);
		EventDispatcher.Instance.RemoveEventListener(LoadSceneOverEvent.EVENT_TYPE, OnEvent_OnLoadSceneOver);
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	private void OnEvent_OnLoadSceneOver(IEvent ievent)
	{
		if (IsSceneType() == true)
		{
			MainUpUITween.gameObject.transform.localPosition = MainUpUITween.to;
			FunctionUITween.gameObject.transform.localPosition = FunctionUITween.from;
		    FubenCloseUITween.gameObject.transform.localPosition = FubenCloseUITween.to;
            FunctionUITween.PlayReverse();
		}
		else
		{
			MainUpUITween.gameObject.transform.localPosition = MainUpUITween.from;
			FunctionUITween.gameObject.transform.localPosition = FunctionUITween.to;
            FubenCloseUITween.gameObject.transform.localPosition = FubenCloseUITween.to;
        }
		var scene = GameLogic.Instance.Scene;
		if (null != scene)
		{
			if (null != scene.TableScene)
			{
				if (1 == scene.TableScene.IsShowMainUI)
				{
					ShowUpButton(false, true);
				}
				else
				{
					ShowUpButton(false, true);
				}
			}
		}
	}

	public void OpenUpMenuBtn_Click()
	{

		ShowUpButton(true);
		
	}

	public void CloseUpMenuBtn_Click()
	{
		if (GuideManager.Instance.IsGuiding())
		{
			return;
		}
		ShowUpButton(false);
	}

	public void OpenDownMenuBtn_Click()
	{
		ShowDownButton(true);

	}

	public void CloseDownMenuBtn_Click()
	{
		if (GuideManager.Instance.IsGuiding())
		{
			return;
		}
		ShowDownButton(false);
	}

	public void ShowUpButton(bool show,bool immediately = false)
	{
		OpenUpMenuBtn.SetActive(!show);
		CloseUpMenuBtn.SetActive(show);
        NewFunctionCtrl.RefreshBtnState();
		if (show)
		{
			Line1Ani.Open(immediately);
			Line2Ani.Open(immediately);
			
		}
		else
		{
			Line1Ani.Close(immediately);
			Line2Ani.Close(immediately);
		}
	}


	public void ShowDownButton(bool show, bool immediately = false)
	{
		ShowUpButton(show, immediately);

		OpenDownMenuBtn.SetActive(!show);
		CloseDownMenuBtn.SetActive(show);

		SkillTween.Play(show);
		JoyStickTween.Play(show);
		DownMenuTween.Play(show);
		MiddleMenuTween.Play(show);

        //if (GetSceneType() == (int)eSceneType.Fuben)
        //{
        //    Debug.LogError(GetSceneType());
        //    MainUpUITween.Play(!show);
        //    FubenCloseUITween.Play(!show);
        //    FunctionUITween.Play(show);
        //}

        if (IsSceneType() == true)
        {
            MainUpUITween.Play(!show);
            FubenCloseUITween.Play(!show);
            FunctionUITween.Play(show);
        }
	}

    //玩家移动后Mainui复位
    private void RestoreUI(IEvent e)
    {
	    var ev = e as RestoreMainUIMenu;
	    if (null == ev)
	    {
		    return;
	    }

	    if (ev.Force)
	    {
			ShowDownButton(false);
		    return;
	    }

		if (GuideManager.Instance.IsGuiding())
		{
			return;
		}    
        if (CloseDownMenuBtn.activeSelf)
        {
            CloseDownMenuBtn_Click();
            HideUITime = 0;
        }      
    }

	private void OpenNewFunctionEvent(IEvent e)
	{
		HideUITime = 0;
	}

    private void ShowMainUiEvent(IEvent e)
    {
        var args = e as UIEvent_ShowMainButton;
        if (args == null)
        {
            return;
        }

        if (!args.IsTop)
        {
            ShowDownButton(args.Open);
        }
        else
        {
            ShowUpButton(args.Open);
        }
    }

    private void UpdateMainDownNotice(IEvent e)
    {
        PlayerDataManager.Instance.NoticeData.NewMainDownUI = NewFunctionCtrl.IsNoticeMainDown();
    }
    //获取场景类型的ID
    //public int GetSceneType()
    //{
    //    var sceneType = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
    //    Debug.LogError(GameLogic.Instance.Scene.SceneTypeId);
    //    if (sceneType == null)
    //    {
    //        //空值
    //        return 5;
    //    }
    //    return sceneType.Type;
    //}
    public bool IsSceneType()
    {
        var sceneType = Table.GetScene(GameLogic.Instance.Scene.SceneTypeId);
        if (sceneType == null)
        {
            return false;
        }

        var fubenType = Table.GetFuben(sceneType.FubenId);
        if (fubenType == null)
        {
            return false;
        }
        if (sceneType.Type == (int) eSceneType.BossHome)
            return true;
        if (eDungeonAssistType.PhaseDungeon != (eDungeonAssistType)fubenType.AssistType && sceneType.Type == (int)eSceneType.Fuben)
        {
            return true;
        }      
        
        return false;
    }
}
