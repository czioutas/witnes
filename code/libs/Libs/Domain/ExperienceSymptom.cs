using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<ExperienceSymptom>))]
public enum ExperienceSymptom
{
    [EnumMember(Value = "slow_visual_load")]
    SlowVisualLoad = 1,        // High Absolute LCP

    [EnumMember(Value = "unstable_layout")]
    UnstableLayout = 2,        // High CLS

    [EnumMember(Value = "stuttering_ui")]
    StutteringUI = 3,          // High Jank Count

    [EnumMember(Value = "delayed_functional_ready")]
    DelayedFunctionalReady = 4, // Page looked ready but wasn't (Dead Zone)

    [EnumMember(Value = "inifnite_waterfall")]
    InfiniteWaterfall = 5,      // SettledTime was null due to Timeout

    [EnumMember(Value = "rage_quit")]
    RageQuit = 6               // FinalizeReason was 'pagehide' before settling
}