using ScriptManager;
using ClientDataModel;
using System;
#region using

using EventSystem;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

#endregion

namespace GameUI
{
	public class UIRewardFrame : MonoBehaviour
	{
		public RewardDataModel DataModel;
		public BindDataRoot Binding;
		private bool removeBind = true;
		public UIInput GiftCodeInput;


		private void OnEnable()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (removeBind)
			{
				var controller = UIManager.Instance.GetController(UIConfig.RewardFrame);
				DataModel = controller.GetDataModel("") as RewardDataModel;
				Binding.SetBindDataSource(DataModel);

				controller = UIManager.Instance.GetController(UIConfig.RechargeFrame);
				Binding.SetBindDataSource(controller.GetDataModel("RechargeDataModel"));


                controller = UIManager.Instance.GetController(UIConfig.NewOfflineExpFrame);
                if (controller != null)
                {
                    Binding.SetBindDataSource(controller.GetDataModel(""));
                }
                

				Binding.SetBindDataSource(PlayerDataManager.Instance.NoticeData);
				Binding.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);

              

				EventDispatcher.Instance.AddEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUIBindingRemove);
				EventDispatcher.Instance.AddEventListener(UIEvent_UpdateOnLineSeconds.EVENT_TYPE, OnEvent_UpdateOnLineSeconds);
			}
			removeBind = true;


            //Æ»¹û¹Ù·½ÓÀ¾ÃÆÁ±ÎµôÀñ°üÂë
		    var spid = PlatformHelper.GetSpid();
		    if (!string.IsNullOrEmpty(spid) && spid.Equals("46"))
		    {
		       var cdkey = transform.FindChild("MenuList/ScrollView/Grid/Cdkey");
		        if (cdkey != null)
		        {
		            cdkey.gameObject.SetActive(false);
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
			Binding.RemoveBinding();

			EventDispatcher.Instance.RemoveEventListener(EventSystem.UIEvent_UpdateOnLineSeconds.EVENT_TYPE,
				OnEvent_UpdateOnLineSeconds);
			EventDispatcher.Instance.RemoveEventListener(CloseUiBindRemove.EVENT_TYPE, OnCloseUIBindingRemove);
		}

		private void OnEvent_UpdateOnLineSeconds(IEvent ievent)
		{

		}

		private void OnCloseUIBindingRemove(IEvent ievent)
		{
			var e = ievent as CloseUiBindRemove;
			if (e.Config != UIConfig.RewardFrame)
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

		public void OnCloseClick()
		{
			EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.RewardFrame, false));
		}


		private void Update()
		{
#if !UNITY_EDITOR
try
{
#endif

			if (0 == Time.frameCount%30)
			{
				EventDispatcher.Instance.DispatchEvent(new UIEvent_GetOnLineSeconds());
			}


		
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

		public void OnCheckinToday()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.CheckinToday, -1));
		}


		public void OnReCheckinToday()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.ReCheckinToday,
				-1));
		}


		public void OnClaimContinuesLoginReward()
		{
			EventDispatcher.Instance.DispatchEvent(
				new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.ClaimContinuesLoginReward, -1));
		}


		public void OnCompensateGoldSeq()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.Compensate, 0));
		}

		public void OnCompensateDiaSeq()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.Compensate, 1));
		}

		public void OnCompensateOk()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.Compensate, 2));
		}

		public void OnCompensateCancel()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_CliamReward(EventSystem.UIEvent_CliamReward.Type.Compensate, 3));
		}

		public void OnClickGetMonthCard()
		{
			EventDispatcher.Instance.DispatchEvent(new GetMonthCardEvent());
		}

        public void OnClickGetWeekCardReward()
        {
            EventDispatcher.Instance.DispatchEvent(new GetWeekCardRewardEvent());
        }

		public void OnClickGotoRecharge()
		{
			EventDispatcher.Instance.DispatchEvent(new GotoRechargeEvent());
		}

		public void OnUseGiftCode()
		{
			var text = GiftCodeInput.text;
			EventDispatcher.Instance.DispatchEvent(new UIEvent_UseGiftCodeEvent(text));
		}

		public void OnUseOfflineItem()
		{
			EventDispatcher.Instance.DispatchEvent(new UIEvent_UseOfflineItemEvent());
		}

        public void OnGetofflineExp()
        {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_GetOfflineItemEvent());
        }
	}
}