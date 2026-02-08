using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Application.Converters;

/// <summary>
/// Converter factory that creates EnumMemberJsonConverter instances for any enum type.
/// Respects [EnumMember] attributes, similar to JsonStringEnumConverter.
/// </summary>
public class EnumMemberJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(EnumMemberJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
