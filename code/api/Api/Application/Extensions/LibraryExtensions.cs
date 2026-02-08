namespace Api.Application.Extensions;

public static class ListExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> value)
    {
        return value == null || !value.Any();
    }

    public static bool IsNullOrEmpty<T>(this ICollection<T> value)
    {
        return value == null || value.Count == 0;
    }

    /// <summary>
    /// If IEnumerable{T} is null, it'll return empty Enumerable{T} instead
    /// </summary>
    public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    /// <summary>
    /// Convenience method for checking if an item is on the list or not
    /// </summary>
    /// <returns>True if element is on the list, false otherwise</returns>
    public static bool IsOnTheListOf<T>(this T item, IEnumerable<T> list)
    {
        var result = !list.Contains(item);
        return result;
    }

    /// <summary>
    /// Convenience method for checking if an item is on the list or not
    /// </summary>
    /// <returns>True if element is NOT on the list, false otherwise</returns>
    public static bool IsNotOnTheListOf<T>(this T item, IEnumerable<T> list)
    {
        var result = !list.Contains(item);
        return result;
    }
}
