using System;
using System.Collections;
using ClientDataModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using ClientService;
using DataContract;
using DataTable;
using EventSystem;
using ScriptManager;
using UnityEngine;

namespace ScriptController
{
    public class SuperVIPController : IControllerBase
    {
        private SuperVipData DataModel;
        private SuperVIPData serverData;
        private Texture txture;//预加载储存图片

        private static string remoteUrl = "http://ww3.sinaimg.cn/large/80dfe250jw1dle1r2v4t9j.jpg";
        /// <summary>
        /// 用file存储读取资源时的地址
        /// </summary>
        private static string SaveResRootRrl = Application.dataPath + "/";
        /// <summary>
        /// 资源根目录  www方式加载必须要这么才能识别
        /// </summary>
        private static string rootRrl
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android)
                    return "jar:file://" + Application.dataPath + "!/assets/";
                else
                    return "file://" + Application.dataPath + "/";
            }
        }

        public SuperVIPController()
        {
            EventDispatcher.Instance.AddEventListener(PreDownLoadImageEvent.EVENT_TYPE, SelectServer);
            CleanUp();
        }

        void SelectServer(IEvent evt)
        {
            serverData = (evt as PreDownLoadImageEvent).DirData;
            if (serverData != null)
            {
                remoteUrl = serverData.HeadUrl;
            }
            else
            {
                SuperVipRecord record = null;
                Table.ForeachSuperVip(temp =>
                {
                    if (temp.ServerID == PlayerDataManager.Instance.ServerId)
                    {
                        record = temp;
                        return false;
                    }
                    return true;
                });
                if (record != null)
                {
                    remoteUrl = record.HeadUrl;
                }
            }
            RefreshDataModel();
            Game.Instance.StartCoroutine(StartLoad());
        }

        private IEnumerator StartLoad()
        {
            //  string[] urlStr = remoteUrl.Split('/');
            //  string imageName = urlStr[urlStr.Length - 1];
            //  string fileUrl = SaveResRootRrl + imageName;
            //  string wwwUrl = rootRrl + imageName;
            //  FileInfo f = new FileInfo(fileUrl);
            WWW www = null;
            //  if (f.Exists) //包含该图片
            //  {
            ///      www = new WWW(wwwUrl);
            //      yield return www;
            //      if (www.error == null)
            //      {
            //          txture = www.texture;
            //      }
            //      www.Dispose();
            //  }
            //  else
            //  {
            var sb = new StringBuilder(remoteUrl);
            sb.Append("?nocache=");
            sb.Append(Guid.NewGuid());
            remoteUrl = sb.ToString();
            www = new WWW(remoteUrl);
            yield return www;
            if (www.error == null)
            {
                txture = www.texture;
                EventDispatcher.Instance.DispatchEvent(new DownLoadImageEvent(txture));
                //  Texture2D newTexture = www.texture;
                //   byte[] pngData = newTexture.EncodeToPNG();
                //  File.WriteAllBytes(fileUrl, pngData);
            }
            www.Dispose();
            //    }
        }

        void RechargeSuccess(IEvent evt)
        {
            if (evt != null)
            {
                ExDataUpDataEvent evtObj = evt as ExDataUpDataEvent;
                if (evtObj.Key == (int)eExdataDefine.e753)
                {
                    RefreshDataModel();
                }
            }
        }

        void RefreshDataModel()
        {
            int dayNum = PlayerDataManager.Instance.GetExData(eExdataDefine.e653);
            int monthNum = PlayerDataManager.Instance.GetExData(eExdataDefine.e654);
            int isShow = PlayerDataManager.Instance.GetExData(eExdataDefine.e753);

            DataModel.DayRechargeNum = string.Format(Table.GetDictionary(274510).Desc[GameUtils.LanguageIndex], dayNum.ToString());
            DataModel.MonthRechargeNum = string.Format(Table.GetDictionary(274510).Desc[GameUtils.LanguageIndex], monthNum.ToString());
            if (serverData != null)
            {
                if (isShow == 1)
                {
                    DataModel.QQNum = serverData.QQ;
                    DataModel.ShotType = 1;
                }
                else
                {
                    DataModel.QQNum = "";
                    DataModel.ShotType = 0;
                }
            }
        }
        #region 固有函数

        public void RefreshData(UIInitArguments data)
        {
            PlayerDataManager.Instance.ApplySuperVipData(true);
        }


        public void CleanUp()
        {
            DataModel = new SuperVipData();
        }

        public INotifyPropertyChanged GetDataModel(string name)
        {
            return DataModel;
        }

        public void Close()
        {
        }

        public void OnShow()
        {
            EventDispatcher.Instance.AddEventListener(ExDataUpDataEvent.EVENT_TYPE, RechargeSuccess);
        }

        private void OnHide()
        {
            EventDispatcher.Instance.RemoveEventListener(ExDataUpDataEvent.EVENT_TYPE, RechargeSuccess);

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

        #endregion
    }
}