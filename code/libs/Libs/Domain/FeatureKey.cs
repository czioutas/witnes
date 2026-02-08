using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<FeatureKey>))]
public enum FeatureKey
{
    [EnumMember(Value = "dropzone")]
    Dropzone
}
