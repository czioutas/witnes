using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<ConnectionReason>))]
public enum ConnectionReason
{
    [EnumMember(Value = "high_latency")]
    HighLatency,      // High RTT
    [EnumMember(Value = "low_bandwidth")]
    LowBandwidth,     // Small download pipe
    [EnumMember(Value = "network_congestion")]
    NetworkCongestion, // High Stall/Queue time
    [EnumMember(Value = "unstable_cellular")]
    UnstableCellular,  // 2g/3g
    [EnumMember(Value = "local_bottleneck")]
    LocalBottleneck    // Device/Browser throttling
}
