using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<BackendReason>))]
public enum BackendReason
{
    [EnumMember(Value = "slow_initial_document")]
    SlowInitialDocument = 1, // High TTFB on HTML

    [EnumMember(Value = "slow_critical_api")]
    SlowCriticalApi = 2,      // High TTFB on Rank 1-5

    [EnumMember(Value = "server_processing_gap")]
    ServerProcessingGap = 3,   // High delta between TTFB and RTT

    [EnumMember(Value = "heavy_response_payload")]
    HeavyResponsePayload = 4
}
