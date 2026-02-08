namespace Api.Application.Extensions;

/// <summary>
/// Extensions methods for Enums
/// </summary>
public static class EnumExtensions
{
    public static TEnum Parse<TEnum>(this string value) where TEnum : struct, Enum
    {
        if (Enum.TryParse(value, true, out TEnum result))
        {
            return result;
        }
        else
        {
            // Default to the default value of the enum if parsing fails
            return default;
        }
    }

    /// <summary>
    /// Parses a string to an enum by matching against EnumMember attribute values
    /// </summary>
    public static bool TryParseEnumMember<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var enumType = typeof(TEnum);

        foreach (var field in enumType.GetFields())
        {
            if (field.IsSpecialName)
            {
                continue; // Skip the "value__" field
            }

            var attribute = System.Reflection.CustomAttributeExtensions.GetCustomAttribute(field, typeof(System.Runtime.Serialization.EnumMemberAttribute))
                as System.Runtime.Serialization.EnumMemberAttribute;

            // Check if EnumMember value matches (case-insensitive)
            if (attribute?.Value != null && string.Equals(attribute.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                result = (TEnum)field.GetValue(null)!;
                return true;
            }

            // Also check the field name itself (case-insensitive)
            if (string.Equals(field.Name, value, StringComparison.OrdinalIgnoreCase))
            {
                result = (TEnum)field.GetValue(null)!;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the name of the enum value as a string
    /// </summary>
    /// <param name="value"></param>
    /// <returns>The name of the enum value</returns>
    public static string GetName(this Enum value)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return Enum.GetName(value.GetType(), value);
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Convenience method. Returns true if "value" parameter is on the "oneOfValues" list.
    /// </summary>
    public static bool IsOneOf<T>(this T value, params T[] oneOfValues) where T : Enum
    {
        return oneOfValues.Any(x => x.Equals(value));
    }

    private static string ConvertToString(this Enum value, System.Globalization.CultureInfo cultureInfo)
    {
        if (value == null)
        {
            return "";
        }

        // if (value is System.Enum)
        // {
        var name = System.Enum.GetName(value.GetType(), value);
        if (name != null)
        {
            var field = System.Reflection.IntrospectionExtensions.GetTypeInfo(value.GetType()).GetDeclaredField(name);
            if (field != null)
            {
                var attribute = System.Reflection.CustomAttributeExtensions.GetCustomAttribute(field, typeof(System.Runtime.Serialization.EnumMemberAttribute))
                    as System.Runtime.Serialization.EnumMemberAttribute;
                if (attribute != null)
                {
                    return attribute.Value != null ? attribute.Value : name;
                }
            }

            var converted = System.Convert.ToString(System.Convert.ChangeType(value, System.Enum.GetUnderlyingType(value.GetType()), cultureInfo));
            return converted == null ? string.Empty : converted;
        }
        // }
        // else if (value is bool)
        // {
        //     return System.Convert.ToString((bool)value, cultureInfo).ToLowerInvariant();
        // }
        // else if (value is byte[])
        // {
        //     return System.Convert.ToBase64String((byte[])value);
        // }
        // else if (value.GetType().IsArray)
        // {
        //     var array = System.Linq.Enumerable.OfType<object>((System.Array)value);
        //     return string.Join(",", System.Linq.Enumerable.Select(array, o => ConvertToString(o, cultureInfo)));
        // }

        var result = System.Convert.ToString(value, cultureInfo);
        return result == null ? "" : result;
    }
}
