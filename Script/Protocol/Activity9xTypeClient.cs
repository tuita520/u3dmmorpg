//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: Activity9xTypeClient.proto
// Note: requires additional types generated from: CommonData.proto
// Note: requires additional types generated from: MessageData.proto
// Note: requires additional types generated from: ServerData.proto
namespace DataContract
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyActivityState_RET_Dict_int_int_Data__")]
  public partial class __RPC_Activity_ApplyActivityState_RET_Dict_int_int_Data__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyActivityState_RET_Dict_int_int_Data__() {}
    

    private DataContract.Dict_int_int_Data _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.Dict_int_int_Data ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyActivityState_ARG_int32_serverId__")]
  public partial class __RPC_Activity_ApplyActivityState_ARG_int32_serverId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyActivityState_ARG_int32_serverId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_NotifyActivityState_ARG_int32_activityId_int32_state__")]
  public partial class __RPC_Activity_NotifyActivityState_ARG_int32_activityId_int32_state__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_NotifyActivityState_ARG_int32_activityId_int32_state__() {}
    

    private int _ActivityId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ActivityId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ActivityId
    {
      get { return _ActivityId; }
      set { _ActivityId = value; }
    }

    private int _State = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"State", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int State
    {
      get { return _State; }
      set { _State = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyOrderSerial_RET_OrderSerialData__")]
  public partial class __RPC_Activity_ApplyOrderSerial_RET_OrderSerialData__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyOrderSerial_RET_OrderSerialData__() {}
    

    private DataContract.OrderSerialData _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.OrderSerialData ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyOrderSerial_ARG_ApplyOrderMessage_msg__")]
  public partial class __RPC_Activity_ApplyOrderSerial_ARG_ApplyOrderMessage_msg__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyOrderSerial_ARG_ApplyOrderMessage_msg__() {}
    

    private DataContract.ApplyOrderMessage _Msg = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"Msg", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.ApplyOrderMessage Msg
    {
      get { return _Msg; }
      set { _Msg = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_NotifyTableChange_ARG_int32_flag__")]
  public partial class __RPC_Activity_NotifyTableChange_ARG_int32_flag__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_NotifyTableChange_ARG_int32_flag__() {}
    

    private int _Flag = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"Flag", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int Flag
    {
      get { return _Flag; }
      set { _Flag = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyMieShiData_RET_CommonActivityData__")]
  public partial class __RPC_Activity_ApplyMieShiData_RET_CommonActivityData__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyMieShiData_RET_CommonActivityData__() {}
    

    private DataContract.CommonActivityData _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.CommonActivityData ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyMieShiData_ARG_int32_serverId__")]
  public partial class __RPC_Activity_ApplyMieShiData_ARG_int32_serverId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyMieShiData_ARG_int32_serverId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyMieshiHeroLogData_RET_MieshiHeroLogList__")]
  public partial class __RPC_Activity_ApplyMieshiHeroLogData_RET_MieshiHeroLogList__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyMieshiHeroLogData_RET_MieshiHeroLogList__() {}
    

    private DataContract.MieshiHeroLogList _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.MieshiHeroLogList ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyMieshiHeroLogData_ARG_int32_serverId__")]
  public partial class __RPC_Activity_ApplyMieshiHeroLogData_ARG_int32_serverId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyMieshiHeroLogData_ARG_int32_serverId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyBatteryData_RET_BatteryDatas__")]
  public partial class __RPC_Activity_ApplyBatteryData_RET_BatteryDatas__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyBatteryData_RET_BatteryDatas__() {}
    

    private DataContract.BatteryDatas _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.BatteryDatas ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyBatteryData_ARG_int32_serverId_int32_activityId__")]
  public partial class __RPC_Activity_ApplyBatteryData_ARG_int32_serverId_int32_activityId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyBatteryData_ARG_int32_serverId_int32_activityId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }

    private int _ActivityId = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"ActivityId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ActivityId
    {
      get { return _ActivityId; }
      set { _ActivityId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyContriRankingData_RET_ContriRankingData__")]
  public partial class __RPC_Activity_ApplyContriRankingData_RET_ContriRankingData__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyContriRankingData_RET_ContriRankingData__() {}
    

    private DataContract.ContriRankingData _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.ContriRankingData ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyContriRankingData_ARG_int32_serverId_int32_activityId__")]
  public partial class __RPC_Activity_ApplyContriRankingData_ARG_int32_serverId_int32_activityId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyContriRankingData_ARG_int32_serverId_int32_activityId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }

    private int _ActivityId = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"ActivityId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ActivityId
    {
      get { return _ActivityId; }
      set { _ActivityId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyPointRankingData_RET_PointRankingData__")]
  public partial class __RPC_Activity_ApplyPointRankingData_RET_PointRankingData__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyPointRankingData_RET_PointRankingData__() {}
    

    private DataContract.PointRankingData _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.PointRankingData ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyPointRankingData_ARG_int32_serverId_int32_activityId__")]
  public partial class __RPC_Activity_ApplyPointRankingData_ARG_int32_serverId_int32_activityId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyPointRankingData_ARG_int32_serverId_int32_activityId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }

    private int _ActivityId = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"ActivityId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ActivityId
    {
      get { return _ActivityId; }
      set { _ActivityId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_NotifyBatteryData_ARG_int32_activityId_ActivityBatteryOne_battery__")]
  public partial class __RPC_Activity_NotifyBatteryData_ARG_int32_activityId_ActivityBatteryOne_battery__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_NotifyBatteryData_ARG_int32_activityId_ActivityBatteryOne_battery__() {}
    

    private int _ActivityId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ActivityId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ActivityId
    {
      get { return _ActivityId; }
      set { _ActivityId = value; }
    }

    private DataContract.ActivityBatteryOne _Battery = null;
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"Battery", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.ActivityBatteryOne Battery
    {
      get { return _Battery; }
      set { _Battery = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_NotifyMieShiActivityState_ARG_int32_activityId_int32_state__")]
  public partial class __RPC_Activity_NotifyMieShiActivityState_ARG_int32_activityId_int32_state__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_NotifyMieShiActivityState_ARG_int32_activityId_int32_state__() {}
    

    private int _ActivityId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ActivityId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ActivityId
    {
      get { return _ActivityId; }
      set { _ActivityId = value; }
    }

    private int _State = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"State", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int State
    {
      get { return _State; }
      set { _State = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_NotifyMieShiActivityInfo_ARG_CommonActivityData_msg__")]
  public partial class __RPC_Activity_NotifyMieShiActivityInfo_ARG_CommonActivityData_msg__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_NotifyMieShiActivityInfo_ARG_CommonActivityData_msg__() {}
    

    private DataContract.CommonActivityData _Msg = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"Msg", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.CommonActivityData Msg
    {
      get { return _Msg; }
      set { _Msg = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_NotifyPlayerCanIn_ARG_int32_fubenId_int64_canInEndTime__")]
  public partial class __RPC_Activity_NotifyPlayerCanIn_ARG_int32_fubenId_int64_canInEndTime__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_NotifyPlayerCanIn_ARG_int32_fubenId_int64_canInEndTime__() {}
    

    private int _FubenId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"FubenId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int FubenId
    {
      get { return _FubenId; }
      set { _FubenId = value; }
    }

    private long _CanInEndTime = default(long);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"CanInEndTime", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(long))]
    public long CanInEndTime
    {
      get { return _CanInEndTime; }
      set { _CanInEndTime = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyAcientBattle_RET_Dict_int_int_Data__")]
  public partial class __RPC_Activity_ApplyAcientBattle_RET_Dict_int_int_Data__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyAcientBattle_RET_Dict_int_int_Data__() {}
    

    private DataContract.Dict_int_int_Data _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.Dict_int_int_Data ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyAcientBattle_ARG_int32_serverId__")]
  public partial class __RPC_Activity_ApplyAcientBattle_ARG_int32_serverId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyAcientBattle_ARG_int32_serverId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyPortraitData_RET_PlayerInfoMsg__")]
  public partial class __RPC_Activity_ApplyPortraitData_RET_PlayerInfoMsg__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyPortraitData_RET_PlayerInfoMsg__() {}
    

    private DataContract.PlayerInfoMsg _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.PlayerInfoMsg ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyPortraitData_ARG_int32_serverId__")]
  public partial class __RPC_Activity_ApplyPortraitData_ARG_int32_serverId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyPortraitData_ARG_int32_serverId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyBossHome_RET_Dict_int_int_Data__")]
  public partial class __RPC_Activity_ApplyBossHome_RET_Dict_int_int_Data__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyBossHome_RET_Dict_int_int_Data__() {}
    

    private DataContract.Dict_int_int_Data _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.Dict_int_int_Data ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyBossHome_ARG_int32_serverId__")]
  public partial class __RPC_Activity_ApplyBossHome_ARG_int32_serverId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyBossHome_ARG_int32_serverId__() {}
    

    private int _ServerId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ServerId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int ServerId
    {
      get { return _ServerId; }
      set { _ServerId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyChickenRankData_RET_ChickenRankData__")]
  public partial class __RPC_Activity_ApplyChickenRankData_RET_ChickenRankData__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyChickenRankData_RET_ChickenRankData__() {}
    

    private DataContract.ChickenRankData _ReturnValue = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ReturnValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public DataContract.ChickenRankData ReturnValue
    {
      get { return _ReturnValue; }
      set { _ReturnValue = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"__RPC_Activity_ApplyChickenRankData_ARG_uint64_characterId__")]
  public partial class __RPC_Activity_ApplyChickenRankData_ARG_uint64_characterId__ : global::ProtoBuf.IExtensible
  {
    public __RPC_Activity_ApplyChickenRankData_ARG_uint64_characterId__() {}
    

    private ulong _CharacterId = default(ulong);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"CharacterId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(ulong))]
    public ulong CharacterId
    {
      get { return _CharacterId; }
      set { _CharacterId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}