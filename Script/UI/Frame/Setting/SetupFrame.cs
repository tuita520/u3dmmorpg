using DataTable;
using ClientService;
using System.Collections;
using ScriptManager;
using System;
#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class SetupFrame : MonoBehaviour
	{
	    public BindDataRoot Binding;
	    private bool deleteBind = true;
	    public UILabel QualityLabel;
	    public UIPopupList QualityList;
	    public UILabel ResolutionLabel;
	    public UIPopupList ResolutionList;
	    private SettingDataModel settingDataModel;
        UIInput modifyName_input;
	    public void ExitToLogin()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SettingUI));
	        Game.Instance.ExitToLogin();
	    }
	
	    public void ExitToSelectRole()
	    {
	        EventDispatcher.Instance.DispatchEvent(new SystemNoticeOperate(1));
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SettingUI));
	        Game.Instance.ExitToSelectRole();
	    }
	
	    public void ExitToServerList()
	    {
	        EventDispatcher.Instance.DispatchEvent(new SystemNoticeOperate(1));
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SettingUI));
	        Game.Instance.ExitToServerList();
	    }
	
	    public void OnBtnQuitGame()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SettingUI));
	        Application.Quit();
	    }

        public void OnBtnUserCenter()
        {
            //	        PlatformHelper.UserCenter();
            var tbScene = Table.GetScene(SceneManager.Instance.CurrentSceneTypeId);
            if (tbScene != null)
            {
                if (Scene.IsDungeon(tbScene))
                {
                    EventDispatcher.Instance.DispatchEvent(new DungeonBtnClick(100, eDungeonType.Invalid));
                }
                else
                {
                    if (GameLogic.Instance.Scene.SceneTypeId == 3)
                    {
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 291016, "", SendSolveStuck);
                    }
                    else
                        UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 291001, "", SendSolveStuck);
                }
            }
        }
        public void OnBtnShowModifyPlayerName()
        {
            var temp = transform.FindChild("List/System/ChargeNameBox");
            if (temp != null && !temp.gameObject.activeSelf)
            {
                if (modifyName_input == null)
                {
                    var temp_input = temp.FindChild("Name/NameInput");
                    if (temp_input != null)
                    {
                        modifyName_input = temp_input.GetComponent<UIInput>();
                    }
                }
                modifyName_input.value = PlayerDataManager.Instance.PlayerDataModel.CharacterBase.Name;
                temp.gameObject.SetActive(true);
            }
        }
        public void OnApplyModifyPlayerName()
        {
            var temp = transform.FindChild("List/System/ChargeNameBox");
            if (temp != null && temp.gameObject.activeSelf)
            {
                if (modifyName_input == null)
                {
                    var temp_input = temp.FindChild("Name/NameInput");
                    if (temp_input != null)
                    {
                        modifyName_input = temp_input.GetComponent<UIInput>();
                    }
                }
                string changeName = modifyName_input.value.Trim();
                if (!GameUtils.CheckName(changeName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 300900);
                    return;
                }
                if (GameUtils.CheckSensitiveName(changeName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 200004120);
                    return;
                }
                if (GameUtils.ContainEmoji(changeName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 725);
                    return;
                }

                if (!GameUtils.CheckLanguageName(changeName))
                {
                    UIManager.Instance.ShowMessage(MessageBoxType.Ok, 725);
                    return;
                }
                EventDispatcher.Instance.DispatchEvent(new SettingOperateModifyPlayerNameEvent(0, changeName));
               
            }
           
        }
        public void OnCloseModifyPlayer()
        {
            var temp = transform.FindChild("List/System/ChargeNameBox");
            if (temp != null && temp.gameObject.activeSelf)
            {
                if (modifyName_input == null)
                {
                    var temp_input = temp.FindChild("Name/NameInput");
                    if (temp_input != null)
                    {
                        modifyName_input = temp_input.GetComponent<UIInput>();
                        modifyName_input.value = "";
                    }
                }
                temp.gameObject.SetActive(false);
            }
        }
	    public void SendSolveStuck()
	    {
            GameControl.Executer.Stop();
            ObjManager.Instance.MyPlayer.LeaveAutoCombat();
            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SettingUI));
            NetManager.Instance.StartCoroutine(SendSolveStuckCoroutine());
        }

	    public IEnumerator SendSolveStuckCoroutine()
	    {
            var msg = NetManager.Instance.ApplySolveStuck(0);
            yield return msg.SendAndWaitUntilDone();
	    }

	    public void OnClickBtnClose()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SettingUI));
	    }
	
	    private void OnEvent_CloseUI(IEvent ievent)
	    {
	        var e = ievent as CloseUiBindRemove;
	        if (e.Config != UIConfig.SettingUI)
	        {
	            return;
	        }
	        if (e.NeedRemove == 0)
	        {
	            deleteBind = false;
	        }
	        else
	        {
	            if (deleteBind == false)
	            {
	                DeleteListener();
	            }
	            deleteBind = true;
	        }
	    }

        private void OnSettingUIModifyPlayerNameEvent(IEvent ievent)
        {
            var e = ievent as SettingUIModifyPlayerNameEvent;
            if(e==null)return;
            switch (e.Type)
            {
                case 0:
                    OnCloseModifyPlayer();
                    break;
                default:
                    break;
            }
        }
	    private void OnDestroy()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	        if (deleteBind == false)
	        {
	            DeleteListener();
	        }
	        deleteBind = true;
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
	        if (deleteBind)
	        {
	            DeleteListener();
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
	        if (deleteBind)
	        {
	            EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUI);
                EventDispatcher.Instance.AddEventListener(SettingUIModifyPlayerNameEvent.EVENT_TYPE, OnSettingUIModifyPlayerNameEvent);

	            var controllerBase = UIManager.Instance.GetController(UIConfig.SettingUI);
	            if (controllerBase == null)
	            {
	                return;
	            }
	            var source = controllerBase.GetDataModel("");
	            var dataModel = source as SettingDataModel;
	            settingDataModel = dataModel;
	            if (dataModel != null)
	            {
	                var quality = dataModel.SystemSetting.QualityToggle;
	                QualityLabel.text = QualityList.items[quality - 1];
	                // QualityList.value = QualityLabel.text;
	               // var resolution = dataModel.SystemSetting.Resolution;
	               // ResolutionLabel.text = ResolutionList.items[resolution - 1];
	                // ResolutionList.value = ResolutionLabel.text;
	            }
	            Binding.SetBindDataSource(source);
	            Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);
	        }
	        deleteBind = true;
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void QualityChange()
	    {
	        var v = QualityList.value;
	        QualityLabel.text = v;
	        var index = QualityList.items.IndexOf(v);
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_QualitySetting(index + 1));
	    }
	
	    private void DeleteListener()
	    {
	        Binding.RemoveBinding();
	        EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnEvent_CloseUI);
            EventDispatcher.Instance.RemoveEventListener(SettingUIModifyPlayerNameEvent.EVENT_TYPE, OnSettingUIModifyPlayerNameEvent);
	    }
	
	    public void ResoultionChange()
	    {
	        var v = ResolutionList.value;
	        ResolutionLabel.text = v;
	        var index = ResolutionList.items.IndexOf(v);
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_ResolutionSetting(index + 1));
	    }

	    public void OnEquipRecyleBtnClick()
	    {
	        EventDispatcher.Instance.DispatchEvent(new SettingShowMessageBoxEvent(0));
	    }
        /// <summary>
        /// 自动反击
        /// </summary>
        public void OnCounterattackBtnClick()
        {
            EventDispatcher.Instance.DispatchEvent(new SettingShowMessageBoxEvent(1));
        }

        public void OnSendQuestion()
        {
            EventDispatcher.Instance.DispatchEvent(new MailOperactionEvent(8));
        }
        public void OnQuestionResite()
        {
            EventDispatcher.Instance.DispatchEvent(new MailOperactionEvent(7));
        }

        #region 按钮相关
        public void OnSystemBtnClick()
        {
            settingDataModel.Tab = 0;
        }

        public void OnAutoFightBtnClick()
        {
            settingDataModel.Tab = 1;
        }

        public void OnMessageBtnClick()
        {
            settingDataModel.Tab = 2;
        }

        public void OnQuestionBtnClick()
        {
            settingDataModel.Tab = 3;
        }
        #endregion



	}
}