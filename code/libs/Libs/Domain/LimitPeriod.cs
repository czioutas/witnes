using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<LimitPeriod>))]
public enum LimitPeriod
{
    [EnumMember(Value = "daily")]
    Daily,

    [EnumMember(Value = "weekly")]
    Weekly,

    [EnumMember(Value = "monthly")]
    Monthly
}
