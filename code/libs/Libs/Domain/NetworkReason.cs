using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<NetworkReason>))]
public enum NetworkReason
{
    [EnumMember(Value = "high_latency")]
    HighLatency = 1,          // High RTT

    [EnumMember(Value = "low_bandwidth")]
    LowBandwidth = 2,         // Low Downlink

    [EnumMember(Value = "network_congestion")]
    NetworkCongestion = 3,     // High "stalled" times in waterfall / browser queuing

    [EnumMember(Value = "unstable_cellular")]
    UnstableCellular = 4,

    [EnumMember(Value = "elevated_cdn_baseline")]
    ElevatedCdnBaseline = 5    // CDN control measurement shows elevated latency
}

