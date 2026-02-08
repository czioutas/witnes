namespace Api.Application.Extensions;

public static class ObjectExtensions
{
    public static string GetFormattedTypeName(this Type type)
    {
        if (type.IsGenericType)
        {
            string genericTypeName = type.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));

            string[] typeArgs = type.GetGenericArguments()
                .Select(t => t.Name)
                .ToArray();

            return $"{genericTypeName}-{string.Join("-", typeArgs)}".ToLowerInvariant();
        }

        return type.Name.ToLowerInvariant();
    }
}
