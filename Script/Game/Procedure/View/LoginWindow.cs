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

public class LoginWindow : MonoBehaviour
{
    public UIInput Account = null;
    public UIInput IP = null;
    public Transform LoginFrame;
    private Coroutine mLoginCoroutine;
    public UIInput Password = null;
    public Transform ThirdLoginFrame;
	public UIPopupList PopList;
	public GameObject IpRoot;
    public UILabel Version;
	public BindDataRoot root;
	public string DefaultIdAddress = "uborm.com.cn:18001";

	public UILabel isbn1 = null;
	public UILabel isbn2 = null;

	private void OnEnable()
	{
#if !UNITY_EDITOR
try
{
#endif

		root.SetBindDataSource(PlayerDataManager.Instance.AccountDataModel);
		LoginAssistant.CreateAssistant(transform);

	
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

		root.RemoveBinding();
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
	}

	public void OnIPChange()
	{
		if (-1 == PopList.items.IndexOf(IP.value))
		{
			PlayerPrefs.SetString("IP", IP.value);
			PlayerPrefs.Save();
		}

	}


	public void MoveFoucsToAccount()
	{
		StartCoroutine(MoveFoucsToAccountCoroutine());
	}

	private IEnumerator MoveFoucsToAccountCoroutine()
	{
		yield return new WaitForEndOfFrame();
		IP.RemoveFocus();
		UIInput.selection = Account;
		Account.selectionEnd = Account.value.Length;
		Account.SaveValue();
	}

	public void MoveFoucsToPassWord()
	{
		Account.RemoveFocus();
		UIInput.selection = Password;
		Password.selectionEnd = Password.value.Length;
		Password.SaveValue();
	}

	public void OnAccountChange()
	{
		PlayerPrefs.SetString("Account", Account.value);
	}

	public void OnLoginBtnClick()
	{
		LoginLogic.instance.OnLoginBtnClick();
	}

	public void OnPwdChange()
	{
		PlayerPrefs.SetString("Password", Password.value);
	}

	public void OnServerIPChanged()
	{
		var v = PopList.value;
		var index = PopList.items.IndexOf(v);
		if (index >= 0 && index < PopList.items.Count)
		{
			if (PopList.items.Count - 1 == index)
			{
				IP.value = PlayerPrefs.GetString("IP", DefaultIdAddress);
			}
			else
			{
				IP.value = PopList.items[index];
			}
		}
		else
		{
			IP.value = PopList.items[0];
		}
		PlayerPrefs.SetString("SelectServer", v);
		PlayerPrefs.Save();
	}

	private int TapCount = 0;
	public void ShowIpBtnClick()
	{
		TapCount++;
		if (0 == TapCount % 10)
		{
			IpRoot.SetActive(true);
		}
		else
		{
			IpRoot.SetActive(false);
		}
	}

	public void OnBtnThirdLogin()
	{
		PlatformHelper.UserLogin();
	}
}