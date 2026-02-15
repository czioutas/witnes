using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<IndustryVerdict>))]
public enum IndustryVerdict
{
    [EnumMember(Value = "fast")]
    Fast = 1,     // LCP < 2500ms

    [EnumMember(Value = "normal")]
    Normal = 2,   // LCP 2500-4000ms

    [EnumMember(Value = "slow")]
    Slow = 3,     // LCP 4000-6000ms

    [EnumMember(Value = "critical")]
    Critical = 4  // LCP > 6000ms
}