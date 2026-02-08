using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Libs.Converters;

public class EnumTypeConverter<TEnum> : TypeConverter where TEnum : struct, Enum
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string stringValue)
        {
            // Try match EnumMemberAttribute.Value (e.g., "purchase_goods")
            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                if (attr != null && string.Equals(attr.Value, stringValue, StringComparison.OrdinalIgnoreCase))
                    return Enum.Parse(typeof(TEnum), field.Name);
            }

            // Fallback to normal Enum.Parse (e.g., "PurchaseGoods")
            if (Enum.TryParse<TEnum>(stringValue, true, out var result))
                return result;
        }

        throw new FormatException($"Invalid {typeof(TEnum).Name}: {value}");
    }
}
