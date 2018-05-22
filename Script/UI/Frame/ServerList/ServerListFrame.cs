using ScriptManager;
using System;
using System.Collections;

#region using

using System.ComponentModel;
using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class ServerListFrame : MonoBehaviour
	{
	    public BindDataRoot Binding;
	    private PropertyChangedEventHandler propChangeHandler;
	    public UISprite NoticeBackground;
	    public ServerListDataModel DataModel { get; set; }
	
	    public void OnBtnAnnouncementClick()
	    {
	        var e = new Event_ServerListButton(2);
	        EventDispatcher.Instance.DispatchEvent(e);
	        PlatformHelper.Event("Announcement");
	    }
	
	    public void OnBtnCancelLineUp()
	    {
	        Game.Instance.ExitToLogin();
	    }
	
	    //最近登录点击
	    public void OnBtnLastServerClick()
	    {
	        var e = new Event_ServerListButton(1);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnBtnQuit()
	    {
	        if (GameUtils.IsOurChannel())
	        {
	            Game.Instance.ExitToLogin();
	        }
	        else
	        {
	            PlatformHelper.ChangeAccount();
	        }
	    }
	
	    private void OnDestroy()
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
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        PlatformHelper.CLoseWebView();
	        DataModel.PropertyChanged -= propChangeHandler;
	        Binding.RemoveBinding();
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
	
	        var controllerBase = UIManager.Instance.GetController(UIConfig.ServerListUI);
	        if (controllerBase == null)
	        {
	            return;
	        }
	        DataModel = controllerBase.GetDataModel("") as ServerListDataModel;
	        Binding.SetBindDataSource(DataModel);
	        Binding.SetBindDataSource(PlayerDataManager.Instance.AccountDataModel);
	        propChangeHandler = OnPropertyChangeAnnounce;
	        DataModel.PropertyChanged += propChangeHandler;
	        LoginLogic.instance.InvisibleLoginFrame();

            //登录成功后就有pid了,可以正确加载公告地址
            PlatformHelper.UpdateUrl(UpdateHelper.AnnoucementURL);


#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	    UIManager.Instance.ShowMessage(MessageBoxType.Ok, ex.ToString());
	}
	#endif
	    }
	
	    //关闭界面
	    public void OnEnterGame()
	    {
	        var e = new Event_ServerListButton(0);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    private void OnPropertyChangeAnnounce(object sender, PropertyChangedEventArgs e)
	    {
	        if (e.PropertyName == "AnnouncementShow")
	        {
	            if (DataModel.AnnouncementShow)
	            {
                    AnnouncementHelper.ShowAnnouncement(UpdateHelper.AnnoucementURL, null);
	            }
	        }
	    }
	
	    public void OnServerListClick()
	    {
	        var e = new Event_ServerListButton(3);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }

	    public void OnPlayerListClick()
	    {
            var e = new Event_ServerListButton(4);
            EventDispatcher.Instance.DispatchEvent(e);
	    }

	    void Awake()
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

	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif

            GameUtils.RecordKeyPoint("SelectServer", 20);

            //没有公告地址不显示公告按钮
            if (string.IsNullOrEmpty(UpdateHelper.AnnoucementURL))
            {
                var ann = transform.FindChild("Announcement");
                if (null != ann)
                {
                    ann.gameObject.SetActive(false);
                }
            }

            //每天第一次显示公告
	        StartCoroutine(DelayShowAnnmouncement());

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
	    }

	    IEnumerator DelayShowAnnmouncement()
	    {
           // yield return new WaitForSeconds(0.1f);
            var _showAnn = PlayerPrefs.GetInt(GameSetting.ShowAnnouncementKey, -1);
            var _today = Game.Instance.ServerTime.Day;
            if (_today != _showAnn)
            {
                var e = new Event_ServerListButton(2);
                EventDispatcher.Instance.DispatchEvent(e);
                PlayerPrefs.SetInt(GameSetting.ShowAnnouncementKey, _today);
            }
            yield break;
	    }
	
	    private void Update()
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
	}
}