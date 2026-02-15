using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<OverallSentiment>))]
public enum OverallSentiment
{
    [EnumMember(Value = "bad")]
    Bad = 0,
    [EnumMember(Value = "neutral")]
    Neutral = 1,
    [EnumMember(Value = "good")]
    Good = 2
}
