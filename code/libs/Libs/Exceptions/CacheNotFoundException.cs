namespace Libs.Exceptions;

///<Summary>
/// Exception Type for handled cases.
///</Summary>
public class CacheNotFoundException : Exception
{
    ///<Summary>
    /// Parameter-less constructor
    ///</Summary>
    public CacheNotFoundException() : base("Cache Entry not found.") { }

    ///<Summary>
    /// Specify the cache that was not found
    ///</Summary>
    public CacheNotFoundException(string? cacheKey) : base($"Cache Entry: [{cacheKey}] not found.") { }

    public static void ThrowIfNull(object? o)
    {
        throw new CacheNotFoundException();
    }
}
