using System;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using LitJson;

namespace Platfrom.TypeSDKHelper
{
    public class TypeSDKHelper
    {

        private static TypeSDKHelper sInstance = null;

        public static TypeSDKHelper Instance
        {
            get { return sInstance ?? (sInstance = new TypeSDKHelper()); }
        }

        public void InitSDK()
        {
            RegistCallBackFunctions();
            U3DTypeSDK.Instance.InitSDK();

            var platdata = U3DTypeSDK.Instance.GetPlatformData();
            var channelid = platdata.GetData(U3DTypeAttName.CHANNEL_ID);
            var appId = GameUtils.ReadConfig("Table/BundleWhiteList/AppConfig.txt");
            var appIds = Regex.Split(appId,"@_@");
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                JsonWriter writer = new JsonWriter(sb);
                writer.WriteObjectStart();
                writer.WritePropertyName("CHANNELID");
                writer.Write(channelid);
                writer.WritePropertyName("ANDROID");
                writer.Write(appIds[0]);
                writer.WritePropertyName("IOS");
                writer.Write(appIds[1]);
                writer.WriteObjectEnd();

                PlatformHelper.InitTalkingDataWithChannelId(sb.ToString());
            }
            catch (Exception e)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                JsonWriter writer = new JsonWriter(sb);
                writer.WriteObjectStart();
                writer.WritePropertyName("CHANNELID");
                writer.Write(channelid);
                writer.WritePropertyName("ANDROID");
                writer.Write("E457980A908047BA97A6B29123B1EAB1");
                writer.WritePropertyName("IOS");
                writer.Write("0125C38F3EF54CCCA242610203D9882F");
                writer.WriteObjectEnd();
                PlatformHelper.InitTalkingDataWithChannelId(sb.ToString());

                UnityEngine.Debug.LogError("talking data appid error..." + e);
            }

        }

        void RegistCallBackFunctions()
        {
            U3DTypeSDK.Instance.AddEventDelegate(TypeEventType.EVENT_INIT_FINISH, InitFinishResult);
            U3DTypeSDK.Instance.AddEventDelegate(TypeEventType.EVENT_LOGIN_SUCCESS, LoginResult);
            U3DTypeSDK.Instance.AddEventDelegate(TypeEventType.EVENT_PAY_RESULT, PayResult);
            U3DTypeSDK.Instance.AddEventDelegate(TypeEventType.EVENT_LOGOUT, LogoutResult);
            U3DTypeSDK.Instance.AddEventDelegate(TypeEventType.EVENT_RELOGIN, ReloginResult);
            U3DTypeSDK.Instance.AddEventDelegate(TypeEventType.Event_EXTRA_FUNCTION, Extrafunction);
        }

        public void Login()
        {
            U3DTypeSDK.Instance.Login();
        }

        public void Logout()
        {
            //晓丽说的，不用登出，直接调登陆就行，否则不自动登陆
           // U3DTypeSDK.Instance.Logout();
            Game.Instance.ChangeSceneToLogin();
        }

        public void ExitGame()
        {
            if (U3DTypeSDK.Instance.IsHasRequest("support_exit_window"))
            {
                U3DTypeSDK.Instance.ExitGame();
            }
            else
            {
                UIManager.Instance.ShowMessage(MessageBoxType.OkCancel, 300918, "", Application.Quit);
            }
        }

        public void ChangeAccount()
        {
            U3DTypeSDK.Instance.Logout();
        }
        public void PayItem(string json)
        {
            var jsonobj = JsonMapper.ToObject(json);
            var amount = (string)jsonobj["goodsPrice"];
            var roleid = (string)jsonobj["roleID"];
            var rolename = (string)jsonobj["roleName"];
            var oid = (string)jsonobj["oid"];
            var goodsName = (string)jsonobj["goodsName"];
            var level = (string)jsonobj["roleLevel"];
            var serverId = (string)jsonobj["serverId"];
            var serverName = (string)jsonobj["serverName"];
            var goodDesc = (string)jsonobj["goodsDesc"];
            var price = int.Parse(amount) * 100;

            var payData = new U3DTypeBaseData();
             var userData = U3DTypeSDK.Instance.GetUserData();
// 
            payData.SetData(U3DTypeAttName.USER_ID, userData.GetData(U3DTypeAttName.USER_ID));
            payData.SetData(U3DTypeAttName.USER_TOKEN, userData.GetData(U3DTypeAttName.USER_TOKEN));
           // payData.SetData(U3DTypeAttName.USER_ID, userIdforPay);
           // payData.SetData(U3DTypeAttName.USER_TOKEN, tokenforPay);
            //商品支付价格（单位：分）
            payData.SetData(U3DTypeAttName.REAL_PRICE, price.ToString());
            payData.SetData(U3DTypeAttName.ITEM_NAME, goodsName);
            payData.SetData(U3DTypeAttName.ITEM_COUNT,"1");
            payData.SetData(U3DTypeAttName.SERVER_ID, serverId);

            payData.SetData(U3DTypeAttName.SERVER_NAME, serverName);
            payData.SetData(U3DTypeAttName.ZONE_NAME, serverName);
            payData.SetData(U3DTypeAttName.BILL_NUMBER, oid);

            payData.SetData(U3DTypeAttName.EXTRA, "nouse");
            payData.SetData(U3DTypeAttName.ITEM_DESC, goodDesc);

            payData.SetData(U3DTypeAttName.ROLE_ID, roleid);
            payData.SetData(U3DTypeAttName.ROLE_NAME, rolename);

            U3DTypeSDK.Instance.PayItem(payData);
        }

        private string role_id;
        private string role_name;
        private string role_level;
        private string server_id;
        private string server_name;
        private string time_level = "-1";
        private string balance = "0";
        private string vip = "0";
        private string party_name = "无";
        private string time_create = "0";

        //新增
        private string user_token;
        public string userIdforPay;
        public string tokenforPay;
        private void SdkSubmitRoleInfo()
        {
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.USER_TOKEN, user_token);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.USER_ID, userIdforPay);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.VIP_LEVEL, vip);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.PARTY_NAME, party_name);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_ID, role_id);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_NAME, role_name);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_LEVEL, role_level);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_CREATE_TIME, time_create);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_LEVELUP_TIME, time_level);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.SERVER_ID, server_id);
            U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.SERVER_NAME, server_name);
        }

        public void UpdatePlayerInfo(string json)
        {
            var jsonObj = JsonMapper.ToObject(json);
            string type = (string)jsonObj["type"];
            switch (type)
            {
                case "enterGame":
                {
                    U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_TYPE, "enterGame");
                    role_id = (string)jsonObj["roleId"];
                    role_name = (string)jsonObj["roleName"];
                    server_id = (string)jsonObj["serverId"];
                    server_name = (string)jsonObj["serverName"];
                    int level = (int)jsonObj["roleLevel"];
                    role_level = level.ToString();
                    int vipLevel = (int)jsonObj["vipLevel"];
                    vip =vipLevel.ToString();
                    party_name = (string)jsonObj["partName"];
                    time_create = (string)jsonObj["createTime"];
                    if (party_name == "")
                    {
                        party_name = "无";
                    }
                    break;
                }
                case "createRole":
                {
                    U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_TYPE, "createRole");
                    role_id = (string)jsonObj["roleId"];
                    role_name = (string)jsonObj["roleName"];
                    server_id = (string)jsonObj["serverId"];
                    server_name = (string)jsonObj["serverName"];
                    role_level = "1";
                    time_level = "-1";
                    vip = "0";
                    time_create = (string)jsonObj["createTime"];
                    break;
                }
                case "levelUp":
                {
                    U3DTypeSDK.Instance.GetUserData().SetData(U3DTypeAttName.ROLE_TYPE, "levelUp");
                    int level = (int)jsonObj["roleLevel"];
                    role_level = level.ToString();
                    time_level = (string)jsonObj["time"];
                    break;
                }
            }

            SdkSubmitRoleInfo();
            U3DTypeSDK.Instance.UpdatePlayerInfo();
        }

        public void SetLocalPush(string key, string message, double timeInterval)
        {
            U3DTypeBaseData pushData = new U3DTypeBaseData();
            pushData.SetData(U3DTypeAttName.PUSH_ID, key);
            pushData.SetData(U3DTypeAttName.PUSH_TYPE, "0");
            pushData.SetData(U3DTypeAttName.PUSH_TYPE_DATA, "打开");
            pushData.SetData(U3DTypeAttName.PUSH_TITLE, "通知");
            pushData.SetData(U3DTypeAttName.PUSH_INFO, message);
            pushData.SetData(U3DTypeAttName.PUSH_REPEAT_INTERVAL, "kMONTH");
            var dateTime = DateTime.Now.AddSeconds(timeInterval);
            var date = dateTime.ToString("dd HH:mm");
            pushData.SetData(U3DTypeAttName.PUSH_ALERT_DATE, date);
            pushData.SetData(U3DTypeAttName.PUSH_NEED_NOTIFY, "0");
            pushData.SetData(U3DTypeAttName.PUSH_RECEIVE_TYPE, "type_001");
            pushData.SetData(U3DTypeAttName.PUSH_RECEIVE_INFO, "nouse");
            U3DTypeSDK.Instance.AddLocalPush(pushData);
        }

        public void DeleteLocalPush(string key)
        {
            U3DTypeSDK.Instance.RemoveLocalPush(key);
        }

        public void ClearAllPush()
        {
            U3DTypeSDK.Instance.RemoveAllLocalPush();
        }


        #region CallResult
        void InitFinishResult(U3DTypeEvent evt)
        {
            Debug.Log("receive u3d init finish");
            //current_ui_model = UI_LOGIN;
        }

        void LoginResult(U3DTypeEvent evt)
        {
            Debug.Log("LoginResult");

            U3DTypeBaseData data = evt.evtData;
            string userID = data.GetData(U3DTypeAttName.USER_ID);
            user_token = data.GetData(U3DTypeAttName.USER_TOKEN);

            var platdata = U3DTypeSDK.Instance.GetPlatformData();
            var channelid = platdata.GetData(U3DTypeAttName.CHANNEL_ID);
            var cpid = platdata.GetData(U3DTypeAttName.CP_ID);

            var tokens = user_token.Split('|');

            if (tokens.Length > 2)
            {
                Logger.Error("tokens split error!!! token=" + user_token);
                return;
            }

            //增加了注册类型
            tokenforPay = tokens[0].Trim();
            var loginType = tokens[1].Trim();
            //更换分隔符|为@_@，腾讯的userID中含有|
            var uid = string.Format("{0}@_@{1}@_@{2}", cpid, channelid, userID);
            LoginLogic.instance.StartCoroutine(LoginLogic.LoginByThirdCoroutine("typeSDK", GameSetting.Channel, uid, tokenforPay));

            //账号或者游客登陆 = 0 
            //账号注册 = 1
            //游客注册 = 2
            if (loginType == "1" || loginType == "2")
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                JsonWriter writer = new JsonWriter(sb);

                writer.WriteObjectStart();
                writer.WritePropertyName("type");
                writer.Write("onRegister");
                writer.WritePropertyName("playerId");
                writer.Write(userID);
                writer.WriteObjectEnd();

                PlatformHelper.OnRegister(sb.ToString());
            }
        }

        void ReloginResult(U3DTypeEvent evt)
        {
            Game.Instance.ChangeSceneToLoginAndAutoLogin(() =>
            {
                LoginResult(evt);
            });
        }

        void PayResult(U3DTypeEvent evt)
        {
            U3DTypeBaseData data = evt.evtData;

            //         datalock_pay_progress = 0;
            // 
            //         bool paySuccess = data.GetBool(U3DTypeAttName.PAY_RESULT);
            //         if (paySuccess)
            //         {
            //             StartRepeatRequestIntoAccount();
            //         }
        }
        void LogoutResult(U3DTypeEvent evt)
        {
            Game.Instance.ChangeSceneToLogin();
        }

        void Extrafunction(U3DTypeEvent evt)
        {
            Debug.Log("unityr receive 额外函数" + evt.evtData.DataToString());

            //         TypeSDKTool.AlartMessage.ShowMessage("额外函数", evt.evtData.DataToString());
            //         U3DTypeBaseData evtData = evt.evtData;
            //         string func_key = evtData.GetData(U3DTypeAttName.EXTERN_FUNCTION_KEY);
            //         string data_1 = evtData.GetData(U3DTypeAttName.EXTERN_FUNCTION_VALUE);
            //         //		string data_2 = evtData.GetData (U3DTypeAttName.EXTERN_FUNCTION_VALUE_2);
            //         switch (func_key)
            //         {
            //             case "http_get":
            //                 {
            //                     this.StartCoroutine(DataProxy.Ins.ServerLogic.HttpGet(data_1, ServerCBK_CommonShowMSG, null));
            //                 }
            //                 break;
            //             case "http_post":
            //                 {
            //                     DataProxy.Ins.ServerLogic.HttpPost(data_1, new Dictionary<string, object>(), ServerCBK_CommonShowMSG, null);
            //                     break;
            //                 }
            //         }
        }
        #endregion
    }


}
