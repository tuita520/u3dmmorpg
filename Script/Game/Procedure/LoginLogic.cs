#region using

using System;
using System.Collections;
using ScriptManager;
using ClientService;
using DataTable;
using EventSystem;
using Platfrom.TypeSDKHelper;
using ScorpionNetLib;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

public class LoginLogic : MonoBehaviour
{
	public static LoginLogic instance;
	public static LoginState State = LoginState.BeforeLogin;
	public static Action ThirdLoginAction;
	private Coroutine mLoginCoroutine;
	
	private BlendWeights oldBlendWeight = BlendWeights.TwoBones;
	public GameObject LoginWindowPerfab;
	public LoginWindow LoginView;

	public enum LoginState
	{
		BeforeLogin,
		ThirdLogin,
		LoginSuccess,
		InGaming
	}

	private void Awake()
	{
#if !UNITY_EDITOR
        try
        {
#endif

		instance = this;

		if (null == LoginView)
		{
			var go = GameObject.Instantiate(LoginWindowPerfab) as GameObject;

			go.transform.parent = UIManager.Instance.GetUIRoot(UIType.TYPE_BASE);
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = Vector3.one;
			go.transform.rotation = Quaternion.identity;
			
			LoginView = go.GetComponent<LoginWindow>();
		}

		Init();
		LoginView.Version.text = string.Format(GameUtils.GetDictionaryText(110000002), UpdateHelper.LocalGameVersion) + "." + UpdateHelper.Version;

		if (LoginView.PopList.items.Count > 0)
		{
			if (!string.IsNullOrEmpty(LoginView.PopList.items[0]))
			{
				LoginView.DefaultIdAddress = LoginView.PopList.items[0];
			}
		}

		RefreshIsbn();

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
	}

	private void RefreshIsbn()
	{
		if (null == LoginView.isbn1)
		{
			var label1 = LoginView.transform.FindChild("Banhao/LabelLeft1");
			if (null != label1)
			{
				LoginView.isbn1 = label1.GetComponent<UILabel>();
				if (null != LoginView.isbn1)
				{
					LoginView.isbn1.text = GameSetting.Instance.Isbn1;
					if (!label1.gameObject.activeSelf)
					{
						label1.gameObject.SetActive(true);
					}
				}
			}

			var label2 = LoginView.transform.FindChild("Banhao/LabelLeft2");
			if (null != label2)
			{
				LoginView.isbn2 = label2.GetComponent<UILabel>();
				if (null != LoginView.isbn2)
				{
					LoginView.isbn2.text = GameSetting.Instance.Isbn2;
					if (!label2.gameObject.activeSelf)
					{
						label2.gameObject.SetActive(true);
					}
				}
			}
		}
	}

	private void Start()
	{
#if !UNITY_EDITOR
try
{
#endif

#if !UNITY_EDITOR
	   var debug = PlayerPrefs.GetInt(GameSetting.LoginAssistantKey, 0);
        if(0 == debug)
        {
		    LoginView.IpRoot.SetActive(false);
        }
#endif

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
	}

	public void Init()
	{

		switch (State)
		{
			case LoginState.BeforeLogin:
				{
					ShowLogin();
				}
				break;
			case LoginState.ThirdLogin:
				{
					LoginView.LoginFrame.gameObject.SetActive(false);
					LoginView.ThirdLoginFrame.gameObject.SetActive(false);
					if (null != ThirdLoginAction)
					{
						ThirdLoginAction();
					}
				}
				break;
			case LoginState.LoginSuccess:
				{
					LoginView.LoginFrame.gameObject.SetActive(false);
					LoginView.ThirdLoginFrame.gameObject.SetActive(false);
				}
				break;
		}
	}

	public void InvisibleLoginFrame()
	{
		if (null != instance)
		{
			LoginView.LoginFrame.gameObject.SetActive(false);
			LoginView.ThirdLoginFrame.gameObject.SetActive(false);
		}
	}

	public static IEnumerator LoginByThirdCoroutine(string platform, string channel, string uid, string accessToken)
	{
		ThirdLoginAction = null;
		using (new BlockingLayerHelper(0))
		{
			NetManager.Instance.Stop();
			NetManager.Instance.ServerAddress = GameUtils.GetServerAddress();
			var result = new AsyncResult<int>();
			var connectToGate = NetManager.Instance.ConnectToGate(result);
			yield return connectToGate;
			if (0 == result.Result)
			{
				// 连接失败!
				UIManager.Instance.ShowMessage(MessageBoxType.Ok, 270111, "", PlatformHelper.UserLogout);
				yield break;
			}

			if (string.IsNullOrEmpty(uid) && string.IsNullOrEmpty(accessToken))
			{
				UIManager.Instance.ShowMessage(MessageBoxType.Ok, "uid and accessToken empty!");
				yield break;
			}

			var loginMsg = NetManager.Instance.PlayerLoginByThirdKey(platform, channel, uid, accessToken);
			yield return loginMsg.SendAndWaitUntilDone();

			if (loginMsg.State == MessageState.Reply)
			{
				if ((int)ErrorCodes.OK == loginMsg.ErrorCode)
				{
					NetManager.Instance.NeedReconnet = true;
					PlayerDataManager.Instance.LastLoginServerId = loginMsg.Response.LastServerId == 0
						? 1
						: loginMsg.Response.LastServerId;
					if (channel.Equals("BaiDu"))
					{
						PlayerDataManager.Instance.UidForPay = uid;
					}
					else
					{
						PlayerDataManager.Instance.UidForPay = loginMsg.Response.Uid;
						TypeSDKHelper.Instance.userIdforPay = loginMsg.Response.Uid;
					}
					NetManager.Instance.StartCoroutine(LoginSuccess());
				}
				else if (loginMsg.ErrorCode == (int)ErrorCodes.Error_PLayerLoginWait)
				{
					NetManager.Instance.NeedReconnet = false;
					PlayerDataManager.Instance.AccountDataModel.LineUpShow = true;
					var e = new Show_UI_Event(UIConfig.ServerListUI, null);
					EventDispatcher.Instance.DispatchEvent(e);
				}
				else
				{
					NetManager.Instance.NeedReconnet = false;
					UIManager.Instance.ShowNetError(loginMsg.ErrorCode);
				}
			}
			else
			{
				NetManager.Instance.NeedReconnet = false;
				Logger.Error("LoginByThirdCoroutine MessageState:{0}", loginMsg.State);
				GameUtils.ShowLoginTimeOutTip();
			}
		}
	}

	public static IEnumerator LoginCoroutine()
	{
		using (new BlockingLayerHelper(0))
		{
            //NetManager.Instance.Stop();
			var result = new AsyncResult<int>();
			var connectToGate = NetManager.Instance.ConnectToGate(result);
			yield return connectToGate;
			if (0 == result.Result)
			{
				// 连接失败!
				UIManager.Instance.ShowMessage(MessageBoxType.Ok, 270111);
				yield break;
			}

			if (string.IsNullOrEmpty(PlayerDataManager.Instance.UserName))
			{
				UIManager.Instance.ShowMessage(MessageBoxType.Ok, "user name empty!");
				yield break;
			}

			var loginMsg = NetManager.Instance.PlayerLoginByUserNamePassword(PlayerDataManager.Instance.UserName,
				PlayerDataManager.Instance.Password);
			yield return loginMsg.SendAndWaitUntilDone();

			if (loginMsg.State == MessageState.Reply)
			{
				if ((int)ErrorCodes.OK == loginMsg.ErrorCode)
				{
					NetManager.Instance.NeedReconnet = true;
					PlayerDataManager.Instance.LastLoginServerId = loginMsg.Response.LastServerId == 0
						? 1
						: loginMsg.Response.LastServerId;
					NetManager.Instance.StartCoroutine(LoginSuccess());
				}
				else if (loginMsg.ErrorCode == (int)ErrorCodes.Error_PLayerLoginWait)
				{
					NetManager.Instance.NeedReconnet = false;
					PlayerDataManager.Instance.AccountDataModel.LineUpShow = true;
					var e = new Show_UI_Event(UIConfig.ServerListUI, null);
					EventDispatcher.Instance.DispatchEvent(e);
				}
				else if ((int)ErrorCodes.PasswordIncorrect == loginMsg.ErrorCode)
				{
					var errorCode = loginMsg.ErrorCode;
					var dicId = errorCode + 200000000;
					var tbDic = Table.GetDictionary(dicId);
					var info = "";
					if (tbDic == null)
					{
						info = GameUtils.GetDictionaryText(200000001) + errorCode;
						Logger.Error(GameUtils.GetDictionaryText(200098), errorCode);
					}
					else
					{
						info = tbDic.Desc[GameUtils.LanguageIndex];
					}
					UIManager.Instance.ShowMessage(MessageBoxType.Ok, info);
				}
				else
				{
					NetManager.Instance.NeedReconnet = false;
					//Logger.Error("PlayerLoginByUserNamePassword ErrorCode" + loginMsg.ErrorCode);
					UIManager.Instance.ShowNetError(loginMsg.ErrorCode);
				}
			}
			else
			{
				NetManager.Instance.NeedReconnet = false;
				Logger.Error("PlayerLoginByUserNamePassword MessageState:{0}", loginMsg.State);
				GameUtils.ShowLoginTimeOutTip();
			}
		}
	}

	public static IEnumerator LoginSuccess()
	{
		using (new BlockingLayerHelper(1))
		{
			const int placeHolder = 0;
			var serverListMsg = NetManager.Instance.GetServerList(placeHolder);
			yield return serverListMsg.SendAndWaitUntilDone();

			if (serverListMsg.State == MessageState.Reply)
			{
				if (serverListMsg.ErrorCode == (int)ErrorCodes.OK)
				{
					var serverListData = serverListMsg.Response;
					//-----------------修改老数据导致 LastLoginServerId 数组越界的bug--------------------
                    //if (PlayerDataManager.Instance.LastLoginServerId > serverListData.Data.Count)
                    //{
                    //    PlayerDataManager.Instance.LastLoginServerId = 1;
                    //}
					//---------------------------------------------------------------------------------
					State = LoginState.LoginSuccess;
					var e = new Show_UI_Event(UIConfig.ServerListUI,
						new ServerListArguments { Data = serverListMsg.Response });
					EventDispatcher.Instance.DispatchEvent(e);
					Game.Instance.ServerInfoCached = true;
				}
				else
				{
					State = LoginState.BeforeLogin;
					Logger.Error("get server list error!");
					GameUtils.ShowLoginTimeOutTip();
				}
			}
			else
			{
				State = LoginState.BeforeLogin;
				GameUtils.ShowLoginTimeOutTip();
				Logger.Error("GetServerList MessageState:{0}", serverListMsg.State);
			}
		}
	}




	

	private void OnDestroy()
	{
#if !UNITY_EDITOR
try
{
#endif
// 		if (null != LoginView)
// 		{
// 			GameObject.Destroy(LoginView.gameObject);
// 		}
		
		instance = null;
		UIManager.Instance.ClearUI();
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

		QualitySettings.blendWeights = oldBlendWeight;
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
	    GameSetting.Instance.GameQualityLevel = -2;
		var var = PlayerPrefs.GetString("SelectServer", LoginView.DefaultIdAddress);
		LoginView.PopList.value = var;
		PlayerDataManager.Instance.AccountDataModel.Account = PlayerPrefs.GetString("Account",
			"Uborm" + Random.Range(1, 99999));
		PlayerDataManager.Instance.AccountDataModel.Pwd = PlayerPrefs.GetString("Password", "123");
		LoginView.IP.value = PlayerPrefs.GetString("IP", LoginView.DefaultIdAddress);
		

		var soundId = int.Parse(Table.GetClientConfig(998).Value);
		if (!SoundManager.Instance.IsBGMPlaying(soundId))
		{
			SoundManager.Instance.PlayBGMusic(soundId, 1, 1);
		}
		LoginView.OnServerIPChanged();
		oldBlendWeight = QualitySettings.blendWeights;
		QualitySettings.blendWeights = BlendWeights.FourBones;

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
	}

	

	public void OnLoginBtnClick()
	{
		NetManager.Instance.ServerAddress = GameUtils.GetServerAddress();

		if (GameUtils.IsOurChannel())
		{
#if UNITY_EDITOR
			NetManager.Instance.ServerAddress = LoginView.IP.value;
#else
			if(LoginView.IpRoot.gameObject.active)
			{
				NetManager.Instance.ServerAddress = LoginView.IP.value;
			}
#endif
			PlayerDataManager.Instance.UserName = LoginView.Account.value;
			PlayerDataManager.Instance.Password = LoginView.Password.value;
			if (mLoginCoroutine != null)
			{
				StopCoroutine(mLoginCoroutine);
			}
			mLoginCoroutine = StartCoroutine(LoginCoroutine());
		}
		else
		{
			PlatformHelper.UserLogin();
		}
	}


	private void ShowLogin()
	{
		if (GameUtils.IsOurChannel())
		{
			LoginView.LoginFrame.gameObject.SetActive(true);
			LoginView.ThirdLoginFrame.gameObject.SetActive(false);
		}
		else
		{
			LoginView.LoginFrame.gameObject.SetActive(false);
			LoginView.ThirdLoginFrame.gameObject.SetActive(true);
			LoginView.OnBtnThirdLogin();

		}
	}





	// Update is called once per frame
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