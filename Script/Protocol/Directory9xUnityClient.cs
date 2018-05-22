using System;
using System.Collections;
using System.IO;
using ScorpionNetLib;
using DataContract;
using ProtoBuf;
using ServiceBase;

namespace ClientService
{

	public interface IDirectory9xServiceInterface : IAgentBase
    {
    }
    public static class Directory9xServiceInterfaceExtension
    {

        public static CheckVersionOutMessage CheckVersion(this IDirectory9xServiceInterface agent, string lang, string platform, string channel, string version)
        {
            return new CheckVersionOutMessage(agent, lang, platform, channel, version);
        }

        public static CheckVersion2OutMessage CheckVersion2(this IDirectory9xServiceInterface agent, string lang, string platform, string channel, string version)
        {
            return new CheckVersion2OutMessage(agent, lang, platform, channel, version);
        }

        public static CheckVersion3OutMessage CheckVersion3(this IDirectory9xServiceInterface agent, string lang, string platform, string channel, string version)
        {
            return new CheckVersion3OutMessage(agent, lang, platform, channel, version);
        }

        public static void Init(this IDirectory9xServiceInterface agent)
        {
            agent.AddPublishDataFunc(ServiceType.Directory, (p, list) =>
            {
                switch (p)
                {
                    default:
                        break;
                }

                return null;
            });


        agent.AddPublishMessageFunc(ServiceType.Directory, (evt) =>
            {
                switch (evt.Message.FuncId)
                {
                    default:
                        break;
                }
            });
        }
    }

    public class CheckVersionOutMessage : OutMessage
    {
        public CheckVersionOutMessage(IAgentBase sender, string lang, string platform, string channel, string version)
            : base(sender, ServiceType.Directory, 8000)
        {
            Request = new __RPC_Directory_CheckVersion_ARG_string_lang_string_platform_string_channel_string_version__();
            Request.Lang=lang;
            Request.Platform=platform;
            Request.Channel=channel;
            Request.Version=version;

        }

        public __RPC_Directory_CheckVersion_ARG_string_lang_string_platform_string_channel_string_version__ Request { get; private set; }

            private __RPC_Directory_CheckVersion_RET_VersionInfo__ mResponse;
            public VersionInfo Response { get { return mResponse.ReturnValue; } }

        protected override byte[] Serialize(MemoryStream s)
        {
            Serializer.Serialize(s, Request);
            return s.ToArray();
        }

        public override void SetResponse(uint error, byte[] data)
        {
            if (data != null)
            {
                var ms = new MemoryStream(data, false);
                mResponse = Serializer.Deserialize<__RPC_Directory_CheckVersion_RET_VersionInfo__>(ms);
            }
            State = MessageState.Reply;
            ErrorCode = (int) error;
        }
        public override bool HasReturnValue { get { return true; } }
    }

    public class CheckVersion2OutMessage : OutMessage
    {
        public CheckVersion2OutMessage(IAgentBase sender, string lang, string platform, string channel, string version)
            : base(sender, ServiceType.Directory, 8001)
        {
            Request = new __RPC_Directory_CheckVersion2_ARG_string_lang_string_platform_string_channel_string_version__();
            Request.Lang=lang;
            Request.Platform=platform;
            Request.Channel=channel;
            Request.Version=version;

        }

        public __RPC_Directory_CheckVersion2_ARG_string_lang_string_platform_string_channel_string_version__ Request { get; private set; }

            private __RPC_Directory_CheckVersion2_RET_VersionInfo__ mResponse;
            public VersionInfo Response { get { return mResponse.ReturnValue; } }

        protected override byte[] Serialize(MemoryStream s)
        {
            Serializer.Serialize(s, Request);
            return s.ToArray();
        }

        public override void SetResponse(uint error, byte[] data)
        {
            if (data != null)
            {
                var ms = new MemoryStream(data, false);
                mResponse = Serializer.Deserialize<__RPC_Directory_CheckVersion2_RET_VersionInfo__>(ms);
            }
            State = MessageState.Reply;
            ErrorCode = (int) error;
        }
        public override bool HasReturnValue { get { return true; } }
    }

    public class CheckVersion3OutMessage : OutMessage
    {
        public CheckVersion3OutMessage(IAgentBase sender, string lang, string platform, string channel, string version)
            : base(sender, ServiceType.Directory, 8002)
        {
            Request = new __RPC_Directory_CheckVersion3_ARG_string_lang_string_platform_string_channel_string_version__();
            Request.Lang=lang;
            Request.Platform=platform;
            Request.Channel=channel;
            Request.Version=version;

        }

        public __RPC_Directory_CheckVersion3_ARG_string_lang_string_platform_string_channel_string_version__ Request { get; private set; }

            private __RPC_Directory_CheckVersion3_RET_VersionInfo__ mResponse;
            public VersionInfo Response { get { return mResponse.ReturnValue; } }

        protected override byte[] Serialize(MemoryStream s)
        {
            Serializer.Serialize(s, Request);
            return s.ToArray();
        }

        public override void SetResponse(uint error, byte[] data)
        {
            if (data != null)
            {
                var ms = new MemoryStream(data, false);
                mResponse = Serializer.Deserialize<__RPC_Directory_CheckVersion3_RET_VersionInfo__>(ms);
            }
            State = MessageState.Reply;
            ErrorCode = (int) error;
        }
        public override bool HasReturnValue { get { return true; } }
    }

}
