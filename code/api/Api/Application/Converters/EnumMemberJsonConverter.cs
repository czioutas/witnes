using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Application.Converters;

public class EnumMemberJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private readonly Dictionary<T, string> _enumToString = new();
    private readonly Dictionary<string, T> _stringToEnum = new();

    public EnumMemberJsonConverter()
    {
        var type = typeof(T);
        var values = Enum.GetValues<T>();

        foreach (var value in values)
        {
            var enumMember = type.GetMember(value.ToString())[0];
            var attr = enumMember.GetCustomAttribute<EnumMemberAttribute>();

            if (attr?.Value != null)
            {
                _enumToString[value] = attr.Value;
                _stringToEnum[attr.Value] = value;
            }
            else
            {
                var name = value.ToString();
                _enumToString[value] = name;
                _stringToEnum[name] = value;
            }
        }
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();

        if (stringValue == null)
        {
            throw new JsonException($"Cannot convert null to {typeof(T).Name}");
        }

        if (_stringToEnum.TryGetValue(stringValue, out var enumValue))
        {
            return enumValue;
        }

        throw new JsonException($"Unable to convert \"{stringValue}\" to enum {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_enumToString[value]);
    }
}
