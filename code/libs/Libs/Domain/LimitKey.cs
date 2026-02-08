using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<LimitKey>))]
public enum LimitKey
{
    [EnumMember(Value = "locations")]
    Locations,

    [EnumMember(Value = "materials")]
    Materials
}
