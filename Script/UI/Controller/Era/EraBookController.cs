#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataTable;
using EventSystem;
using ScorpionNetLib;
using UnityEngine;

#endregion

namespace ScriptController
{
    public class EraBookController : IControllerBase
    {
        private EraBookDataModel DataModel = new EraBookDataModel();
        private int showPage = 1;
        private int currentEraId;
        private readonly int[] titleIconId = { 201, 200, 202, 203 };
        private int currEnterFubenId = -1;
        private int currSelectEraId = -1;

        public EraBookController()
        {
            CleanUp();

            EventDispatcher.Instance.AddEventListener(Event_UpdateEraPage.EVENT_TYPE, OnUpdateEraPage);
            EventDispatcher.Instance.AddEventListener(Event_EraCellClick.EVENT_TYPE, OnClick_EraCell);
            EventDispatcher.Instance.AddEventListener(Event_CalalogClick.EVENT_TYPE, OnClick_EraCatalog);
            EventDispatcher.Instance.AddEventListener(Event_EraBookOperate.EVENT_TYPE, OnClick_EraBookOperator);
            EventDispatcher.Instance.AddEventListener(Event_UpdateMissionData.EVENT_TYPE, OnMissionUpdate);
            EventDispatcher.Instance.AddEventListener(ExDataInitEvent.EVENT_TYPE, OnExDataInitEvent);
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, OnFlagInit);
            EventDispatcher.Instance.AddEventListener(OnClick_EraGotoEvent.EVENT_TYPE, OnClick_EraGoto);
            EventDispatcher.Instance.AddEventListener(EraSelectCurrEra.EVENT_TYPE, EraSelectCurrEraCallBack);
            EventDispatcher.Instance.AddEventListener(GoToEraBookShiShiEvent.EVENT_TYPE, OnGoToEraBookShiShiEvent);
            EventDispatcher.Instance.AddEventListener(UpdateBranchMissionDataEvent.EVENT_TYPE, OnUpdateBranchMissionDataEvent);
            EventDispatcher.Instance.AddEventListener(EraBookFlyOverEvent.EVENT_TYPE, OnEraBookFly);
            EventDispatcher.Instance.AddEventListener(Event_EraBookNextPage.EVENT_TYPE, OnEvent_EraBookNextPage);
            
            //EventDispatcher.Instance.AddEventListener(FlagUpdateEvent.EVENT_TYPE, OnFlagUpgradeEvent);
            EventDispatcher.Instance.AddEventListener(Event_EraChangeCurrentPage.EVENT_TYPE, ievent =>
            {
                DataModel.SkillInfo.ShowTitleTips = false;
                var e = ievent as Event_EraChangeCurrentPage;
                if (e == null)
                {
                    return;
                }
                DataModel.CurrentPage = e.Page;

                var page = EraManager.Instance.UiPage2LogicPage(e.Page);
                var info = EraManager.Instance.GetPageInfo(page);
                if (info != null)
                {
                    DataModel.SelectBookMark = info.eraList[0].Record.Type;
                    if (info.Type == 0)
                    {                    
                        for (int i = 0; i < info.eraList.Count; i++)
                        {
                            var eraInfo = info.eraList[i];
                            EraBookCellDataModel animDataModel = DataModel.MayaList[i];
                            animDataModel.PlayAnim = false;                           
                            if (eraInfo.State == EraState.PlayAnim)
                            {                            
                                animDataModel.PlayAnim = true;
                                NetManager.Instance.StartCoroutine(PlayAnimEnd(animDataModel, eraInfo, 0.8f));                                
                            }
                        }

                        //while (enumorator.MoveNext())
                        //{
                        //    var eraInfo = enumorator.Current;
                        //    if (eraInfo.State == EraState.PlayAnim)
                        //    {
                        //        EraBookCellDataModel animDataModel;
                        //        if (count >= 3)
                        //        {
                        //            animDataModel = DataModel.RightList[count - 3];
                        //        }
                        //        else
                        //        {
                        //            animDataModel = DataModel.LeftList[count];
                        //        }
                        //        animDataModel.PlayAnim = true;
                        //        NetManager.Instance.StartCoroutine(PlayAnimEnd(animDataModel, eraInfo, 1.5f));
                        //    }
                        //    ++count;
                        //}
                    }
                    else
                    {
                        var eraInfo = info.eraList[0];
                        if (eraInfo.State == EraState.PlayAnimEnd || eraInfo.State == EraState.PlayAnim)
                        {
                            EventDispatcher.Instance.DispatchEvent(new Event_EraPlayAnim(0));
                            EraManager.Instance.PlayAnimEraId = -1;
                            NetManager.Instance.StartCoroutine(PlayContentAnimEnd(eraInfo, 0.8f));
                        }
                    }
                }
            });
        }

        private void OnEvent_EraBookNextPage(IEvent iEvent)
        {
            var ie = iEvent as Event_EraBookNextPage;
            if (ie != null)
            {
                var page = EraManager.Instance.UiPage2LogicPage(ie.Page);
                var info = EraManager.Instance.GetPageInfo(page);                 
                if (ie.Page == 12 || ie.Page == 1)
                {
                    var eraId = info.eraList[0].Record.Id;                    
                    SetSelectIndex(eraId);
                }
            }
        }

        private int SceneId = -1;
        private int MissionId = -1;
        private bool IsMissionToShiShi = false;
        private void OnGoToEraBookShiShiEvent(IEvent iEvent)
        {
            var ie = iEvent as GoToEraBookShiShiEvent;
            if (ie != null)
            {
                var tabFuben = Table.GetFuben(ie.FubenId);
                if (tabFuben != null)
                {
                    SceneId = tabFuben.SceneId;
                    MissionId = ie.MissionId;
                }
                IsMissionToShiShi = true;
                EraManager.Instance.GotoEraBookPage(ie.Info.Page +1 ,true);                
                SetSelectIndex(ie.Info.Record.Id);
            }
        }

        private bool isFinash = false;

        private IEnumerator PlayAnimEnd(EraBookCellDataModel data, EraInfo info, float time)
        {
            yield return new WaitForSeconds(time);
            info.State = EraState.PlayAnimEnd;
            info.State = EraState.Finish;
            data.State = (int)EraState.Finish;        
            var skillId = GetSkillIdByEraId(info.Record.Id);

            var skillInfo = GetSkillInfoBySkillId(skillId);
            if (skillInfo != null)
            {
                FillCatalogCell(data, skillInfo);
            }       
            if (tempEraId == info.Record.Id)
            {
                SetCurrEraCell(info.Record.Id);
            }
        }

        //填充cell skillInfo数据
        private void FillCatalogCell(EraBookCellDataModel data, EraBookSkillInfo info)
        {
            var eraInfo = EraManager.Instance.GetEraInfo(data.EraId);
            data.State = (int) eraInfo.State;
            data.HaveTakeAward = eraInfo.TakeAward;
            data.ChapterName = eraInfo.Order.ToString();

            var skilId = GetSkillIdByEraId(data.EraId);
            info.SkillId = skilId;

            info.State = (int)eraInfo.State;

            info.EraId = data.EraId;
            var strAtlas = GetAtlasBySkillId(skilId, data.EraId);
            if (string.IsNullOrEmpty(strAtlas))
            {
                Logger.Debug("Get Atlas is Null");
                return;
            }
            info.Atlas = strAtlas;
            if (eraInfo.State == EraState.NotStart || eraInfo.State == EraState.OnGoing || eraInfo.State == EraState.PlayAnim)
            {
                info.Atlas += "Grey";
            }
        }

        public void Close()
        {
            for (var i = 0; i < DataModel.MayaList.Count; ++i)
            {
                var data = DataModel.MayaList[i];
                data.PlayAnim = false;
            }
        }

        public void Tick()
        {
        }

        public void RefreshData(UIInitArguments args)
        {
            if (args != null && args.Args != null && args.Args.Count > 0)
            {
                showPage = args.Args[0];
            }

            DataModel.MaxPage = EraManager.Instance.TotalPage;
            DataModel.CurrentPage = 0;

            for (var type = 0; type < DataModel.BookMarkNotice.Count; ++type)
            {
                var notice = EraManager.Instance.RefreshEraNotice(type);
                DataModel.BookMarkNotice[type] = notice;
            }
        }

        private void OnMissionUpdate(IEvent ievent)
        {
            var e = ievent as Event_UpdateMissionData;
            if (null == e) return;

            //变化的任务id，如果是-1说明是全部任务
            var missionId = e.Id;
            if (missionId == -1)
            {
            }
            else
            {
                var tbMission = Table.GetMissionBase(missionId);
                if (tbMission == null || tbMission.ViewType != (int)eMissionMainType.MainStoryLine)
                    return;
            }
            // 主线任务刷新时，更新
            EraManager.Instance.RefreshByMainMission();
        }

        private void OnEraBookFly(IEvent ievent)
        {
            NetManager.Instance.StartCoroutine(ClickMayaTip());
        }
        private IEnumerator ClickMayaTip()
        {
            var msg = NetManager.Instance.ClickMayaTip(0);
            yield return msg.SendAndWaitUntilDone();
        }
        private void OnUpdateBranchMissionDataEvent(IEvent iEvent)
        {
            var ie = iEvent as UpdateBranchMissionDataEvent;
            if (ie != null)
            {
                var missionId = ie.MissionId;
                SetEraStateByBranchMissionState(missionId,EraState.NotStart);
            }
        }



        private void SetEraStateByBranchMissionState(int missionId ,EraState state )
        {
            var info = EraManager.Instance.GetPageInfo(11);
            for (int i = 0; i < info.eraList.Count; i++)
            {
                var activeList = info.eraList[i].Record.ActiveParam;
                if (activeList.Count <= 0) continue;
                if (activeList[0] == missionId)
                {
                    EraManager.Instance.CurrShiShiEraId = info.eraList[i].Record.Id;
                    info.eraList[i].State = state;                   
                    var missState =  MissionManager.Instance.GetMissionState(missionId);
                    if (missState == eMissionState.Unfinished)
                    {
                        info.eraList[i].State = EraState.OnGoing;
                    }
                    var finish = PlayerDataManager.Instance.GetFlag(info.eraList[i].Record.FinishFlagId);
                    if (finish)
                    {
                        info.eraList[i].State = EraState.Finish;
                    }
                }
            }
        }


        private void OnFlagInit(IEvent ievent)
        {
            EraManager.Instance.ResetState();
        }

        private void OnExDataInitEvent(IEvent ievent)
        {
            EraManager.Instance.RefreshByMainMission();
        }

        private void OnFlagUpgradeEvent(IEvent ievent)
        {
            var e = ievent as FlagUpdateEvent;
            if (e != null)
            {
                EraManager.Instance.RefreshFlagId(e.Index);
            }
        }

        //刷新目录页
        private void RefreshCatalogPage(bool showTitle, List<int> eraIdList)
        {
            DataModel.ShowTitle = showTitle;
            //var leftStart = 0;
            if (DataModel.ShowTitle)//标题需要显示
            {
                var eraId = eraIdList[0];
                var tbMayaBase = Table.GetMayaBase(eraId);
                if (tbMayaBase != null)
                {
                    DataModel.TitleIconId = titleIconId[tbMayaBase.Type];
                }
                //leftStart = 1;//标题占用一个位置 所以左侧从1开始
            }

            #region  
            ////标题需要显示 leftList数据只需要填充2个数据

            //for (var i = leftStart; i < DataModel.LeftList.Count; ++i)
            //{
            //    var data = DataModel.LeftList[i];
            //    data.PlayAnim = false;
            //    if (infoIndex < eraIdList.Count)
            //    {
            //        data.EraId = eraIdList[infoIndex++];
            //        FillCatalogCell(data);
            //    }
            //    else
            //    {
            //        data.EraId = -1;
            //    }
            //}

            ////填充右侧数据
            //for (var i = 0; i < DataModel.RightList.Count; ++i)
            //{
            //    var data = DataModel.RightList[i];
            //    data.PlayAnim = false;
            //    if (infoIndex < eraIdList.Count)
            //    {
            //        data.EraId = eraIdList[infoIndex++];
            //        FillCatalogCell(data);
            //    }
            //    else
            //    {
            //        data.EraId = -1;
            //    }
            //}

            #endregion

            for (var i = 0; i < DataModel.MayaList.Count; i++)
            {
                var data = DataModel.MayaList[i];
                var info = DataModel.SkillInfoList[i];
                data.PlayAnim = false;
                if (i < eraIdList.Count)
                {
                    data.EraId = eraIdList[i];
                    FillCatalogCell(data, info);
                }
                else
                {
                    data.EraId = -1;
                    info.EraId = -1;
                }
            }
        }



        private string GetAtlasBySkillId(int skillId,int eraId)
        {
            string strAtlas = string.Empty;
            var tbSkill = Table.GetSkill(skillId);
            if (tbSkill == null)
            {
                var tbEra = Table.GetMayaBase(eraId);
                if (tbEra == null)
                {
                    Logger.Debug(" GetMayaBase is NULL ");
                    return strAtlas;
                }            
                var tIcon = Table.GetIcon(tbEra.IconId);
                if (tIcon != null)
                {
                    strAtlas = tIcon.Atlas;
                }
            }
            else
            {
                var tbIcon = Table.GetIcon(tbSkill.Icon);
                if (tbIcon != null)
                {
                    strAtlas = tbIcon.Atlas;
                }
            }
            return strAtlas;
        }


        //更新玛雅页数据
        private void OnUpdateEraPage(IEvent ievent)
        {
            var eraArgs = ievent as Event_UpdateEraPage;
            if (eraArgs == null)
                return;

            //分类 == 0
            if (eraArgs.Type == 0)
            {
                //刷新目录
                RefreshCatalogPage(eraArgs.ShowTitle, eraArgs.EraIdList);
                DataModel.CurrType = eraArgs.Page < 10 ? 0 : 1;
            }
            else
            {            
                var skillInfo = DataModel.SkillInfo;
                skillInfo.EraId = eraArgs.EraIdList[0];
                currentEraId = skillInfo.EraId;
                var eraInfo = EraManager.Instance.GetEraInfo(skillInfo.EraId);
                if (eraInfo == null)
                {
                    return;
                }
                skillInfo.State = (int)eraInfo.State;

                var tbMayaBase = eraInfo.Record;
                var finish = PlayerDataManager.Instance.GetFlag(tbMayaBase.FinishFlagId);

                skillInfo.Chapter = eraInfo.Order.ToString();
                skillInfo.Desc = GameUtils.GetDictionaryText(tbMayaBase.Desc);

                skillInfo.FromText = GameUtils.GetDictionaryText(tbMayaBase.GetDescId);
                skillInfo.ShowFightButton = false;
                skillInfo.SkillId = -1;
                var roleId = PlayerDataManager.Instance.GetRoleId();
                if (tbMayaBase.SkillIds.Count != 0 && tbMayaBase.SkillIds[0] != -1)
                {
                    skillInfo.SkillId = tbMayaBase.SkillIds[roleId];
                    skillInfo.SkillLv = 1;
                }
                else
                {
                    var dictId = 0;
                    if (roleId < eraInfo.Record.DisplayDescIds.Count)
                    {
                        dictId = eraInfo.Record.DisplayDescIds[roleId];
                    }
                    else if (eraInfo.Record.DisplayDescIds.Count > 0)
                    {
                        dictId = eraInfo.Record.DisplayDescIds[0];
                    }

                    DataModel.SkillInfo.FuncDesc = GameUtils.GetDictionaryText(dictId);
                }

                var inFuben = PlayerDataManager.Instance.IsInFubenScnen();
                if (eraInfo.State == EraState.OnGoing
                    && eraInfo.Record.FunBenId >= 0
                    && EraManager.Instance.IsRealMax()
                    && inFuben == false)
                {
                    skillInfo.ShowFightButton = true;
                }

                if (finish)
                {
                    skillInfo.ShowFightButton = false;
                }

                var index = 0;
                var awardId = eraInfo.Record.Award[roleId];
                var tbConsumArray = Table.GetConsumArray(awardId);
                if (tbConsumArray != null)
                {
                    for (var i = 0; i < tbConsumArray.ItemId.Count(); ++i)
                    {
                        var itemId = tbConsumArray.ItemId[i];
                        if (itemId >= 0)
                        {
                            if (index >= DataModel.SkillInfo.ItemList.Count)
                                break;
                            var itemCount = tbConsumArray.ItemCount[i];
                            DataModel.SkillInfo.ItemList[index].ItemId = itemId;
                            DataModel.SkillInfo.ItemList[index].Count = itemCount;
                            ++index;
                        }
                    }
                }

                for (var i = index; i < DataModel.SkillInfo.ItemList.Count; ++i)
                {
                    DataModel.SkillInfo.ItemList[i].ItemId = -1;
                }

                DataModel.SkillInfo.TitleItemData.Id = tbMayaBase.TitleId;
                if (tbMayaBase.TitleId >= 0)
                {
                    GameUtils.TitleAddAttr(DataModel.SkillInfo.TitleItemData, Table.GetNameTitle(tbMayaBase.TitleId));
                }
                DataModel.SkillInfo.HaveTakeAward = eraInfo.TakeAward;
            }
        }


        private IEnumerator PlayContentAnimEnd(EraInfo info, float time)
        {
            yield return new WaitForSeconds(time);
            info.State = EraState.Finish;
            DataModel.SkillInfo.State = (int)info.State;

            var notice = EraManager.Instance.RefreshEraNotice(info.Record.Type);
            DataModel.BookMarkNotice[info.Record.Type] = notice;
        }

        //Icon点击  获取当前数据
        private void OnClick_EraCell(IEvent ievent)
        {
            var args = ievent as Event_EraCellClick;
            if (args == null) return;
            SetSelectIndex(args.DataModel.EraId);
        }

        private int GetSkillIdByEraId(int eraId)
        {
            var currSkillId = 0;
            var mayaBaseTb = Table.GetMayaBase(eraId);

            if (mayaBaseTb == null)
            {
                Logger.Debug("GetMayaBase ID == null");
                return currSkillId;
            }
            var roleId = PlayerDataManager.Instance.GetRoleId();
            var skillList = mayaBaseTb.SkillIds;
            if (mayaBaseTb.SkillIds.Count > 0 && mayaBaseTb.SkillIds[0] >= 0)
            {
                currSkillId = skillList[roleId];            
            }
            else
            {
                currSkillId = -1;                     
            }
            return currSkillId;
        }

        private EraBookSkillInfo GetSkillInfoBySkillId(int skillId)
        {        
            var enumerator = DataModel.SkillInfoList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var Id = enumerator.Current.SkillId;
                if (skillId == Id)
                {
                    return enumerator.Current;
                }
            }
            return null;
        }

        //根据状态设置按钮上的文字
        private void SetCurrSelectCellDataStateForBtnShow()
        {        
            switch (DataModel.CurrSelectCellDataModel.State)
            {
                case (int)EraState.Finish:
                case (int)EraState.PlayAnimEnd:
                    DataModel.BtnShowState = (int)EraState.Finish;
                    break;
                case (int)EraState.PlayAnim:
                    DataModel.BtnShowState = (int)EraState.OnGoing;
                    break;
                case (int)EraState.NotStart:
                    DataModel.BtnShowState = (int)EraState.NotStart;
                    break;
                case (int)EraState.OnGoing:
                    if (DataModel.CurrSelectCellDataModel.EraId == EraManager.Instance.CurrShiShiEraId)
                    {
                        DataModel.BtnShowState = (int)EraState.OnGoing;
                    }
                    else
                    {
                        DataModel.BtnShowState = EraManager.Instance.IsRealMax() ? (int)EraState.OnGoing : (int)EraState.NotStart;    
                    }
                    break;
            }
        }


        private void EraSelectCurrEraCallBack(IEvent iEvent)
        {
            if (iEvent != null)
            {
                var v = iEvent as EraSelectCurrEra;
                if (v == null) return;
                SetSelectIndex(v.EraId);
            }
        }


        int tempEraId = -1;


        private void SetSelectIndex(int eraId)
        {        
            tempEraId = eraId;
            DataModel.SelectIndex = tempEraId % 10;
            if (eraId / 10 > 0)
            {
                DataModel.SelectIndex = (eraId - 1) % 10;            
            }

            var mayaBaseTb = Table.GetMayaBase(eraId);
            if (mayaBaseTb == null) 
            {
                Logger.Debug("GetMayaBase ID == null");
                return;
            }
            currEnterFubenId = mayaBaseTb.FunBenId;
            currSelectEraId = eraId;
            SetSelectSkillInfo(eraId);
            SetCurrEraCell(eraId);
        }


        private void SetCurrEraCell(int eraId)
        {
            var info = EraManager.Instance.GetEraInfo(eraId);
            DataModel.CurrSelectCellDataModel.EraId = eraId;            
            DataModel.CurrSelectCellDataModel.State = (int)info.State;   
            SetCurrSelectCellDataStateForBtnShow();
        }

        //设置选中的skillInfo
        private void SetSelectSkillInfo(int eraId)
        {      
            var skillId = GetSkillIdByEraId(eraId);
            DataModel.CurrSelectSkillInfo.SkillId = skillId;

            var strAtlas = GetAtlasBySkillId(skillId, eraId);
            if(string.IsNullOrEmpty(strAtlas)) return;

            DataModel.CurrSelectSkillInfo.Atlas = strAtlas;
            DataModel.CurrSelectSkillInfo.EraId = eraId;

            var roleId = PlayerDataManager.Instance.GetRoleId();
            var tbMaya = Table.GetMayaBase(eraId);
            if (tbMaya == null) return;
            if (roleId < tbMaya.FubenDescIds.Count)
            {
                var dictId = tbMaya.FubenDescIds[roleId];
                DataModel.CurrSelectSkillInfo.FuncDesc = GameUtils.GetDictionaryText(dictId);            
            }        
        }

        private void OnClick_EraGoto(IEvent ievent)
        {       
            switch (DataModel.BtnShowState)
            {
                case (int)EraState.NotStart:
                case (int)EraState.Finish:
                    var page = EraManager.Instance.GetJumpToPage(DataModel.CurrSelectCellDataModel.EraId);
                    EventDispatcher.Instance.DispatchEvent(new Event_EraTurnPage(page));
                    break;
                case (int)EraState.OnGoing:
                    if (currEnterFubenId != -1)
                    {
                        var info = EraManager.Instance.GetEraInfo(currSelectEraId);
                        if (info != null)
                        {
                            var tabFuben = Table.GetFuben(currEnterFubenId);
                            if (tabFuben != null)
                            {
                                SceneId = tabFuben.SceneId;
                                MissionId = Table.GetMayaBase(currSelectEraId).ActiveParam[0];
                                if (SceneManager.Instance.CurrentSceneTypeId == SceneId)
                                {
                                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200005022));
                                    return;
                                }
                                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel,
                                GameUtils.GetDictionaryText(100001471),
                                GameUtils.GetDictionaryText(1503),
                                () =>
                                {
                                    MissionManager.Instance.ChangedSceneByMission(SceneId, MissionId);
                                    EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraBookUI));
                                });
                            }
                        }                          
                    }
                    break;
            }
        }

        //分类按钮
        private void OnClick_EraCatalog(IEvent ievent)
        {
            var args = ievent as Event_CalalogClick;
            if (args == null)
                return;
            DataModel.CurrType = args.Type;
            var page = EraManager.Instance.GetCatalogPage(args.Type);
            var eraInfos = EraManager.Instance.GetEraInfos(args.Type);
            var enumorator = eraInfos.GetEnumerator();
            while (enumorator.MoveNext())
            {
                var info = enumorator.Current;
                if (info != null && EraManager.Instance.ShowNotice(info))
                {
                    page = EraManager.Instance.LogicPage2UiPage(info.Page);
                    break;
                }
            }
            EventDispatcher.Instance.DispatchEvent(new Event_EraTurnPage(page));

            NetManager.Instance.StartCoroutine(DelayShow(eraInfos[0].Record.Id));
            DataModel.SelectBookMark = args.Type;
        }

        private IEnumerator DelayShow(int id)
        {
            yield return new WaitForSeconds(0.5f);
            SetSelectIndex(id);
        }

        private void OnClick_EraBookOperator(IEvent ievent)
        {
            var args = ievent as Event_EraBookOperate;
            if (args == null)
                return;

            switch (args.Type)
            {
                case 0:
                {
                    if (GotoEra(currentEraId))
                    {
                        EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.EraBookUI));
                    }
                }
                    break;
                case 1:
                {
                    NetManager.Instance.StartCoroutine(EraTakeAwardCoroutine(currentEraId));
                }
                    break;
                case 2:
                {
                    DataModel.SkillInfo.ShowTitleTips = true;
                }
                    break;
                case 3:
                {
                    DataModel.SkillInfo.ShowTitleTips = false;
                }
                    break;
            }
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void CleanUp()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }

        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }

        public void OnShow()
        {
            if (showPage > 0)
            {
                EventDispatcher.Instance.DispatchEvent(new Event_EraTurnPage(showPage, true));
                showPage = -1;
            }
        }

        private bool GotoEra(int eraId)
        {
            if (eraId < 0)
                return false;

            var tbMayaBase = Table.GetMayaBase(eraId);
            if (tbMayaBase == null)
                return false;

            if (tbMayaBase.FunBenId < 0)
                return false;

            if (1 != EraManager.Instance.GetCurrentEraState())
            {
                return false;
            }

            if (PlayerDataManager.Instance.IsInFubenScnen())
            {
                return false;
            }

            NetManager.Instance.StartCoroutine(GotoEraCoroutine(eraId));
            return true;
        }

        private IEnumerator GotoEraCoroutine(int eraId)
        {
            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.CSEnterEraById(eraId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                    }
                }
            }
        }

        //领取奖励
        private IEnumerator EraTakeAwardCoroutine(int eraId)
        {
            var info = EraManager.Instance.GetEraInfo(eraId);
            if (info == null)
            {
                yield break;
            }

            if (info.TakeAward)
            {
                yield break;
            }

            if (!PlayerDataManager.Instance.GetFlag(info.Record.FinishFlagId))
            {
                yield break;
            }

            using (new BlockingLayerHelper(0))
            {
                var msg = NetManager.Instance.EraTakeAward(eraId);
                yield return msg.SendAndWaitUntilDone();
                if (msg.State == MessageState.Reply)
                {
                    if (msg.ErrorCode == (int)ErrorCodes.OK)
                    {
                        info.TakeAward = true;

                        var enumorator = DataModel.MayaList.GetEnumerator();
                        while (enumorator.MoveNext())
                        {
                            var cellData = enumorator.Current;
                            if (cellData != null && cellData.EraId == eraId)
                            {
                                cellData.HaveTakeAward = info.TakeAward;
                            }
                        }

                        //var enumorator = DataModel.LeftList.GetEnumerator();
                        //while (enumorator.MoveNext())
                        //{
                        //    var cellData = enumorator.Current;
                        //    if (cellData != null && cellData.EraId == eraId)
                        //    {
                        //        cellData.HaveTakeAward = info.TakeAward;
                        //    }
                        //}
                        //enumorator = DataModel.RightList.GetEnumerator();
                        //while (enumorator.MoveNext())
                        //{
                        //    var cellData = enumorator.Current;
                        //    if (cellData != null && cellData.EraId == eraId)
                        //    {
                        //        cellData.HaveTakeAward = info.TakeAward;
                        //    }
                        //}

                        if (DataModel.SkillInfo.EraId == eraId)
                        {
                            DataModel.SkillInfo.HaveTakeAward = info.TakeAward;
                        }

                        var notice = EraManager.Instance.RefreshEraNotice(info.Record.Type);
                        DataModel.BookMarkNotice[info.Record.Type] = notice;

                        EventDispatcher.Instance.DispatchEvent(new Event_EraAddTurnPage(1));
                    }
                }
            }
        }


        public FrameState State { get; set; }
    }
}
