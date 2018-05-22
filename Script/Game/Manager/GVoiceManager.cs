using System;
using System.Collections;
using System.Collections.Generic;
using ScriptManager;
using ClientService;
using DataTable;
using EventSystem;
using gcloud_voice;
using ScorpionNetLib;
using UnityEngine;

namespace ScriptManager
{
    public class GVoiceManager : Singleton<GVoiceManager>
    {
        public enum VoiceState
        {
            UnInited = 0,
            Inited,
            JoiningRoom,
            InRoom,
        }

        public enum RoomType
        {
            Silence = 0,//静音
            Anchor = 1,//主播
            Team,//组队
            League,//战盟
        }

        private readonly int TimeOutMillSeconds = 12000;

        private IGCloudVoice m_voiceengine = null;
        public bool Open { get; set; }
        public string AnchorRoom { get; set; }
        public List<string> AnchorName { get; set; }
        public List<string> AnchorBeginTime { get; set; }
        public List<string> AnchorEndTime { get; set; }
        public int GuildSpeekLevel { get; set; }
        public string OnlineAnchorName { get; set; }

        //public bool IsEnterAnchorRoom = false;

        public bool IsInRoom
        {
            get
            {
                return State == VoiceState.InRoom;
            }
        }

        private VoiceState State = VoiceState.UnInited;

        private string RoomName = "";

        private RoomType mRoomType;

        public RoomType CurrentRoomType
        {
            get
            {
                return mRoomType;
            }
        }

        public bool IsJoining
        {
            get 
            { 
                return State == VoiceState.JoiningRoom;
            }
        }

        public bool IsAnchor
        {
            get
            {
                if (null != AnchorName)
                {
                    return AnchorName.Contains(PlayerDataManager.Instance.GetName());
                }
                return false;
            }
        }

   
        public bool IsOnLine
        {
            get { return !string.IsNullOrEmpty(OnlineAnchorName); }        
        }

        public void Init(string appId, string appKey)
        {
            if (VoiceState.UnInited != State)
            {
                return;
            }
            if (m_voiceengine == null)
            {
                m_voiceengine = GCloudVoice.GetEngine();
                System.TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                string strTime = System.Convert.ToInt64(ts.TotalSeconds).ToString();

              
                m_voiceengine.SetAppInfo(appId, appKey, strTime);
                Debug.Log("GVoiceManager appId={0}" + appId + "\nappKey=" + appKey);
                m_voiceengine.Init();
            }

            m_voiceengine.OnJoinRoomComplete += (IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID) =>
            {
                Debug.Log("OnJoinRoomComplete ret=" + code + " roomName:" + roomName + " memberID:" + memberID);
                if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_JOINROOM_SUCC)
                {

                    State = VoiceState.InRoom;

                    RoomName = roomName;
                    if (IsAnchor)
                    {
                        m_voiceengine.OpenMic();
                    }

                    m_voiceengine.OpenSpeaker();

                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(301038));

                    SoundManager.Instance.VoicePlaying = true;

                }
                else
                {
                    State = VoiceState.Inited;
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(301038) + GameUtils.GetDictionaryText(1033)));
                }

            };
            //}
            m_voiceengine.OnQuitRoomComplete += (IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID) =>
            {
                Debug.Log("OnQuitRoomComplete ret=" + code + " roomName:" + roomName + " memberID:" + memberID);
                //UIManager.m_Instance.OnJoinRoomDone(code);

                if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_QUITROOM_SUCC)
                {
                    State = VoiceState.Inited;
                    //SoundManager.Instance.SetBgmPause(false);

                    SoundManager.Instance.VoicePlaying = false;

                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(301039));
                    RoomName = "";
                }
                else
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(301039) + GameUtils.GetDictionaryText(1033)));
                }
            };

            m_voiceengine.OnMemberVoice += (int[] members, int count) =>
            {
                //PrintLog ("OnMemberVoice");
                //s_logstr +="\r\ncount:"+count;
                // 			for (int i = 0; i < count && (i + 1) < members.Length; ++i)
                // 			{
                // 				Debug.Log("OnMemberVoice " + members[i]);
                // 				++i;
                // 			}
                //UIManager.m_Instance.UpdateMemberState(members, length, usingCount);
            };

            State = VoiceState.Inited;
            Debug.Log("GVoiceManager.Init----------------ok");
        }
        public void Init() 
        {
            var appId = "1615962565";
            var appKey = "781ac257727e97cfde686fd9f5b91892";
            try
            {
                appId = Table.GetClientConfig(1213).Value;
                appKey = Table.GetClientConfig(1214).Value;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            Init(appId,appKey);
        }
	
        // Update is called once per frame
        public void Update()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (VoiceState.UnInited == State)
            {
                return;
            }

            try
            {
                if (m_voiceengine != null)
                {
                    m_voiceengine.Poll();
                }

            }
            catch (Exception)
            {
			
            }

	
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
        }

        public bool JoinAnchorRoom()
        {
            if (VoiceState.Inited != State)
            {
                Debug.Log("Error:GVoiceManager.JoinAnchorRoom VoiceState.Inited = " + State);
                if (IsInRoom)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(301040)));
                }
                return false;
            }

            bool canSpeek = false;

            if (IsAnchor)
            {
                canSpeek = true;
                NetManager.Instance.StartCoroutine(AnchorEnterRoom());
            }
            Debug.Log("GVoiceManager.JoinAnchorRoom roomName=" + AnchorRoom + " isAnchor=" + canSpeek);

            m_voiceengine.SetMode(GCloudVoiceMode.RealTime);
            if ((int)GCloudVoiceErr.GCLOUD_VOICE_SUCC != m_voiceengine.JoinNationalRoom(AnchorRoom, canSpeek ? GCloudVoiceRole.ANCHOR : GCloudVoiceRole.AUDIENCE, TimeOutMillSeconds))
            {
                return false;
            }
            State = VoiceState.JoiningRoom;
            mRoomType = RoomType.Anchor;        
            return true;
        }


        /// <summary>
        /// 主播进入房间
        /// </summary>
        /// <returns></returns>
        public IEnumerator AnchorEnterRoom()
        {
            var msg = NetManager.Instance.NotifyAnchorEnterRoomChange(0);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                if (msg.ErrorCode == (int)ErrorCodes.OK)
                {                
                }
                else
                {
                    UIManager.Instance.ShowNetError(msg.ErrorCode); 
                }
            }
        }

        private bool AuchorIsInRoom = false;
        public IEnumerator GetAnchorIsInRoom()
        {
            var msg = NetManager.Instance.GetAnchorIsInRoom(0);
            yield return msg.SendAndWaitUntilDone();
            if (msg.State == MessageState.Reply)
            {
                AuchorIsInRoom = msg.Response == 1;
            }
        }


        public bool JoinTeamRoom()
        {
            if (VoiceState.Inited != State)
            {
                Debug.Log("Error:GVoiceManager.JoinTeamRoom VoiceState.Inited =" + State);
                if (IsInRoom)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(301040)));
                }
                return false;
            }

            var teamId = PlayerDataManager.Instance.TeamDataModel.TeamId;
            if (teamId <= 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200002408));
                return false;
            }

            var serverId = PlayerDataManager.Instance.ServerId;
            string roomName = string.Format("{0}_s{1}_t{2}", AnchorRoom,serverId, teamId);

            Debug.Log("GVoiceManager.JoinTeamRoom roomName=" + roomName);

            m_voiceengine.SetMode(GCloudVoiceMode.RealTime);

            if ((int)GCloudVoiceErr.GCLOUD_VOICE_SUCC != m_voiceengine.JoinTeamRoom(roomName, TimeOutMillSeconds))
            {
                return false;
            }
            RoomName = roomName;
            State = VoiceState.JoiningRoom;

            mRoomType = RoomType.Team;


            return true;
        }

        public bool JoinNationalRoom()
        {
            if (VoiceState.Inited != State)
            {
                Debug.Log("Error:GVoiceManager.JoinNationalRoom VoiceState.Inited = " + State);
                if (IsInRoom)
                {
                    EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(GameUtils.GetDictionaryText(301040)));
                }
                return false;
            }

            var unionId = PlayerDataManager.Instance.BattleUnionDataModel.MyUnion.UnionID;
            if (unionId <= 0)
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(200002974));
                return false;
            }

            bool canSpeek = false;
            if (PlayerDataManager.Instance.BattleUnionDataModel.MyUnion.Level>=GuildSpeekLevel)
            {
                canSpeek = true;	
            }

            var serverId = PlayerDataManager.Instance.ServerId;
            string roomName = string.Format("{0}_s{1}_g{2}", AnchorRoom,serverId, unionId);

            Debug.Log("GVoiceManager.JoinNationalRoom roomName=" + roomName + " isAnchor=" + canSpeek);

            m_voiceengine.SetMode(GCloudVoiceMode.RealTime);
            if ((int)GCloudVoiceErr.GCLOUD_VOICE_SUCC != m_voiceengine.JoinNationalRoom(roomName, canSpeek ? GCloudVoiceRole.ANCHOR : GCloudVoiceRole.AUDIENCE, TimeOutMillSeconds))
            {
                return false;
            }
		
            State = VoiceState.JoiningRoom;

            mRoomType = RoomType.League;


            return true;
        }

        public bool QuitRoom(bool force= false)
        {
            if (string.IsNullOrEmpty(RoomName))
            {
                return false;
            }

            if (force)
            {
                m_voiceengine.QuitRoom(RoomName, 10000);
                SoundManager.Instance.VoicePlaying = false;
                return true;
            }

            if (!IsInRoom)
            {
                Debug.Log("Error:GVoiceManager.QuitRoom !IsInRoom");
                return false;
            }	
            m_voiceengine.QuitRoom(RoomName, 10000);

            if (IsAnchor)
            {
                NetManager.Instance.StartCoroutine(AnchorExitRoom());
            }

            return true;
        }

        public IEnumerator AnchorExitRoom()
        {
            var msg = NetManager.Instance.AnchorExitRoom(0);
            yield return msg.SendAndWaitUntilDone();
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            if (VoiceState.UnInited == State)
            {
                return;
            }

            Debug.Log("Voice OnApplicationPause: " + pauseStatus);
            if (pauseStatus)
            {
                if (m_voiceengine == null)
                {
                    return;
                }
                m_voiceengine.Pause();
                //s_strLog += "\r\n pause:"+ret;
            }
            else
            {
                if (m_voiceengine == null)
                {
                    return;
                }
                m_voiceengine.Resume();
                //s_strLog += "\r\n resume:"+ret;
            }
        }
    }
}
