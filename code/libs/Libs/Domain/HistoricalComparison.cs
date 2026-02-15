using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<HistoricalComparison>))]
public enum HistoricalComparison
{
    [EnumMember(Value = "insufficient_data")]
    InsufficientData = 3,

    [EnumMember(Value = "better")]
    Better = 2,

    [EnumMember(Value = "similar")]
    Similar = 1,

    [EnumMember(Value = "worse")]
    Worse = 0
}