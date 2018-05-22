
#region using

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ScriptManager;
using ClientDataModel;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;

#endregion

namespace ScriptController
{
    public class SurveyController : IControllerBase
    {
        private SurveyDataModel DataModel;
        public SurveyController()
        {
            CleanUp();
            EventDispatcher.Instance.AddEventListener(FlagInitEvent.EVENT_TYPE, Init);
            EventDispatcher.Instance.AddEventListener(SurveySendResultEvent.EVENT_TYPE, OnSend);

            EventDispatcher.Instance.AddEventListener(SurveyCheckOptEvent.EVENT_TYPE, CheckOptEvent);


        }

        public void CleanUp()
        {
            DataModel = new SurveyDataModel();
        }

        public void RefreshData(UIInitArguments data)
        {
        
        }

        private void Init(IEvent ievent)
        {
            PlayerDataManager.Instance.NoticeData.bSurvey = false;
            Table.ForeachSurvey(tb =>
            {
                if (PlayerDataManager.Instance.GetFlag(tb.flagHad) == false && PlayerDataManager.Instance.GetFlag(tb.flagCan) == true)
                {
               
                    for (int i = 0; i < tb.questionList.Count; i++)
                    {
                        if (tb.questionList[i] > 0)
                        {
                            var tbCell = Table.GetSurveyList(tb.questionList[i]);
                            if (tbCell != null)
                            {
                                SurveyCell2DataModel cell = new SurveyCell2DataModel();
                                cell.str = (i+1).ToString()+"."+tbCell.title;
                                cell.key = tb.questionList[i];
                                cell.type = 0;
                                DataModel.SurveyList2.Add(cell);
                                for (int j = 0; j < tbCell.opt.Count(); j++)
                                {
                                    if(false == string.IsNullOrEmpty(tbCell.opt[j]))
                                    {
                                        SurveyCell2DataModel cell2 = new SurveyCell2DataModel();
                                        cell2.str = tbCell.opt[j];
                                        cell2.key = tbCell.Id;
                                        cell2.value = j+1;
                                        cell2.type = 1;
                                        cell2.bMul = tbCell.type != 1;
                                        DataModel.SurveyList2.Add(cell2);
                                    }

                                }
                            }
                        }

                    }
                    DataModel.reward.ItemId = tb.reward;
                    DataModel.id = tb.Id;
                    PlayerDataManager.Instance.NoticeData.bSurvey = true;
                    return false;
                }
                return true;
            });
        }

        #region 基类
        public INotifyPropertyChanged GetDataModel(string name)
        {

            return DataModel;
        }

        public void Close()
        {

        }

        public void OnShow()
        {

        }
        public void Tick()
        {
        }

        public void OnChangeScene(int sceneId)
        {
        }
        public object CallFromOtherClass(string name, object[] param)
        {
            return null;
        }
        public FrameState State { get; set; }
        #endregion 基类

        private void CheckOptEvent(IEvent ievent)
        {
            SurveyCheckOptEvent e = ievent as SurveyCheckOptEvent;
            if (e != null)
            {
                foreach (var cell in DataModel.SurveyList2)
                {
                    if (cell.key != e.id || cell.type != 1)
                        continue;
                    cell.bSelect = cell.value == e.value;
                }
            }
        }
        private void OnSend(IEvent ievent)
        {
            var tb = Table.GetSurvey(DataModel.id);
            if (tb == null)
                return;
            Dictionary<int,int> dic = new Dictionary<int, int>();
            for (int i = 0; i < tb.questionList.Count; i++)
            {
                dic.Add(tb.questionList[i],1);
            }
            Vec2Array vec = new Vec2Array();
            foreach (var cell in DataModel.SurveyList2)
            {
                if (cell.bSelect == false)
                    continue;
                if(dic.ContainsKey(cell.key))
                {
                    dic.Remove(cell.key);
                }
                var v = new Vector2Int32();
                v.x = cell.key;
                v.y = cell.value;
                vec.List.Add(v);
            }
            if (dic.Count > 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(100001403));
                return;
            }

            EventDispatcher.Instance.DispatchEvent(new Close_UI_Event(UIConfig.SurveyUI));
            NetManager.Instance.StartCoroutine(SendSurveyCoroutine(vec));
        }

        private IEnumerator SendSurveyCoroutine(Vec2Array data)
        {
            var msg = NetManager.Instance.SendSurvey(DataModel.id,data);
            yield return msg.SendAndWaitUntilDone();
            PlayerDataManager.Instance.NoticeData.bSurvey = false;
            yield break;        
        }

    }
}