using ScriptManager;
using DataTable;
using System;
#region using

using System.Collections.Generic;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class MainEntry : MonoBehaviour
	{

	    public void OnClickAchievement()
	    {
	        var e = new Show_UI_Event(UIConfig.AchievementFrame);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickActivity()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.RewardFrame));
	    }
        /// <summary>
        /// 如果有好友消息，会显示按钮，点击事件如下
        /// </summary>
	    public void OnClickFriendChat()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SNSFrameUI));
	    }

	    public void OnClickBattleUnion()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.BattleUnionUI));
	    }

        public void OnClickEndlessLand()
        {
            //var tbDy = Table.GetDynamicActivity(10);
            //GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ClimbingTowerUI));
        }

	    public void OnClickBtnRank()
	    {
            var e = new Show_UI_Event(UIConfig.RankUI, new RankArguments { });
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickCompose()
	    {
	        var e = new Show_UI_Event(UIConfig.ComposeUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickDepot()
	    {
	        var e = new Show_UI_Event(UIConfig.DepotUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickDiamondShop()
	    {
            var tab = 2;//默认钻石商城
            if (PlayerDataManager.Instance.GetRes((int)eResourcesType.VipLevel) > 0)
            {
                tab = 1;//VIP=>VIP商城
            }
            var e = new Show_UI_Event(UIConfig.RechargeFrame, new RechargeFrameArguments { Tab = tab });
            EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickDungeon()
	    {
	        var e = new Show_UI_Event(UIConfig.DungeonUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickElf()
	    {
	        PlayerDataManager.Instance.WeakNoticeData.ElfTotal = false;
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ElfUI));
	    }
	
	    public void OnClickEquip()
	    {
	        var e = new Show_UI_Event(UIConfig.EquipUI, new EquipUIArguments {Tab = 0});
	        EventDispatcher.Instance.DispatchEvent(e);
            PlayerDataManager.Instance.WeakNoticeData.AppendNotice = false;
	    }
	
	    public void OnClickHandBook()
	    {
	        var e = new Show_UI_Event(UIConfig.HandBook);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
        public void OnClickHandBookDown()
        {
            var e = new Show_UI_Event(UIConfig.HandBook, new HandBookArguments { Tab = 2});
            EventDispatcher.Instance.DispatchEvent(e);
        }
	
	    public void OnClickHuodong()
	    {
            var argList = new List<int>();
            argList.Add(-1);
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ActivityUI, new ActivityArguments
	        {
	            Tab = 0,
                Args = argList
	        }));
	    }
	
	    public void OnClickMail()
	    {
            FriendArguments arg = new FriendArguments();
	        arg.Tab = 1;
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SNSFrameUI,arg));
	    }
        public void OnClickAnswerQuestion()
        {
            var tbDy = Table.GetDynamicActivity(3);
            if (null != tbDy)
            {
                GameUtils.GotoUiTab(tbDy.UIID, tbDy.SufaceTab);                
            }
        }
	
	    public void OnClickMission()
	    {
	        var e = new Show_UI_Event(UIConfig.MissionList);
	        EventDispatcher.Instance.DispatchEvent(new Event_MissionList_TapIndex(1));
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickOffLine()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.OffLineExpFrame));
	    }
	
	    public void OnClickPack()
	    {
	        var e = new Show_UI_Event(UIConfig.CharacterUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	        PlayerDataManager.Instance.WeakNoticeData.BagTotal = false;
	    }
	
	    public void OnClickPet()
	    {
	        PlayerDataManager.Instance.NoticeData.CityLevel = false;
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.CityUI));
	    }
	
	    public void OnClickRank()
	    {
	        var e = new Show_UI_Event(UIConfig.RankUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickRecharge()
	    {
	        var e = new Show_UI_Event(UIConfig.RechargeActivityUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickSetting()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.SettingUI));
	    }
	
	    public void OnClickShop()
	    {
	        //Game.Instance.ExitSelectCharacter();
	    }
	
	    public void OnClickSkill()
	    {
	        PlayerDataManager.Instance.WeakNoticeData.SkillTotal = false;
	        var e = new Show_UI_Event(UIConfig.SkillFrameUI, new SkillFrameArguments());
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	//进入好友界面
	    public void OnClickSocial()
	    {
	        var e = new Show_UI_Event(UIConfig.FriendUI);
	        EventDispatcher.Instance.DispatchEvent(e);
           
	    }
  

	    public void OnClickSwith()
	    {
	        //mIsShow = !mIsShow;
	        //foreach (var btn in BtnList)
	        //{
	        //    btn.gameObject.SetActive(mIsShow);
	        //}
	    }
	
	    public void OnClickTeam()
	    {
	        var e = new Show_UI_Event(UIConfig.TeamFrame);
	        EventDispatcher.Instance.DispatchEvent(e);
	        var ee = new UIEvent_TeamFrame_NearTeam();
	        EventDispatcher.Instance.DispatchEvent(ee);
	    }

	    public void OnClickMount()
	    {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.MountUI));
	    }

		public void OnClick1V1()
		{
// 			var e = new Show_UI_Event(UIConfig.P1VP1Frame);
// 			EventDispatcher.Instance.DispatchEvent(e);

            var ee = new Show_UI_Event(UIConfig.AreanaUI, new ArenaArguments
            {
                BuildingData = CityManager.Instance.GetBuildingByAreaId(6),
                Tab = 0
            });
            EventDispatcher.Instance.DispatchEvent(ee);
		}
        //public void OnClickBattleField()
        //{
        //    var e = new Show_UI_Event(UIConfig.BattleUI);
        //    EventDispatcher.Instance.DispatchEvent(e);
        //}

		public void OnClickPVPIsland()
		{
			//GameUtils.GotoUiTab(60, 13);
			EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.AcientBattleFieldFrame));
		}

		public void OnClickWishTree()
		{
            var e = new Show_UI_Event(UIConfig.WishingUI, new WishingArguments { Tab = 0});
			EventDispatcher.Instance.DispatchEvent(e);
		}
        public void OnClickExChange()
        {
            //EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ExchangeUI));
            var e = new Show_UI_Event(UIConfig.WishingUI, new WishingArguments { Tab = 1 });
            EventDispatcher.Instance.DispatchEvent(e);
        }

		public void OnClicItemCompose()
		{
			var e = new Show_UI_Event(UIConfig.ComposeUI);
			EventDispatcher.Instance.DispatchEvent(e);
		}
        public void OnClickWing()
        {
            var e = new AttriFrameOperate(1);
            EventDispatcher.Instance.DispatchEvent(e);
            PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = false;
        }
		public void OnClicSmithy()
		{
			var list = CityManager.Instance.BuildingDataList;
			foreach (var buildingData in list)
			{
				var tb = Table.GetBuilding(buildingData.TypeId);
				if (null != tb)
				{
					if (BuildingType.BlacksmithShop == (BuildingType)tb.Type)
					{
						var ee = new Show_UI_Event(UIConfig.SmithyUI,
						new SmithyFrameArguments
						{
							BuildingData = buildingData
						});
						EventDispatcher.Instance.DispatchEvent(ee);
					}
				}
			}

		}

		public void OnClicSailingHarbor()
		{
			var list = CityManager.Instance.BuildingDataList;
			foreach (var buildingData in list)
			{
				var tb = Table.GetBuilding(buildingData.TypeId);
				if (null != tb)
				{
					if (BuildingType.BraveHarbor == (BuildingType)tb.Type)
					{
						var ee = new Show_UI_Event(UIConfig.SailingUI,
						new SailingArguments
						{
							BuildingData = buildingData
						});
						EventDispatcher.Instance.DispatchEvent(ee);
					}
				}
			}
			
		}

	    public void OnClick_Artifact()
	    {
            EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.ArtifactUi));
            EventDispatcher.Instance.DispatchEvent(new EnableFrameEvent(0));
	    }

	    public void OnClickOperationAcitivty()
		{
            var ee = new Show_UI_Event(UIConfig.OperationActivityFrame, new OperationActivityTypeUIArguments { Type = 0 });
			EventDispatcher.Instance.DispatchEvent(ee);
		}

        public void OnClickSuperVip()
        {
            var ee = new Show_UI_Event(UIConfig.SuperVipUI);
            EventDispatcher.Instance.DispatchEvent(ee);
        }

        public void OnClickNewOperationAcitivty()
        {
            var ee = new Show_UI_Event(UIConfig.OperationActivityFrame, new OperationActivityTypeUIArguments { Type = 1 });
            EventDispatcher.Instance.DispatchEvent(ee);
        }
	    public void OnClickEyeVisible()
	    {
	        EventDispatcher.Instance.DispatchEvent(new UIEvent_VisibleEyeClick(true));
	    }

	    public void OnClickEyeInvisible()
	    {
            EventDispatcher.Instance.DispatchEvent(new UIEvent_VisibleEyeClick(false));
	    }

        public void OnClickBtnReborn()
        {
            var e = new Show_UI_Event(UIConfig.RebornUi);
            EventDispatcher.Instance.DispatchEvent(e);
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
	
	    private enum BtnType
	    {
	        BtnPack = 0,
	        BtnEquip,
	        BtnSkill,
	        BtnPet,
	        BtnSetting,
	        BtnSocial,
	        BtnShop
	    }
	}
}