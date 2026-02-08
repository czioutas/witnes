using Api.Application.Extensions;
using Api.Application.Models;

namespace Api.Application.Services;

internal static class TypeNameCache<T>
{
    // Cache the lowercase type name specific to each type T
    public static readonly string LowercaseName = typeof(T).GetFormattedTypeName();
}

public static class CacheKeyGeneratorService
{
    private const char InvalidChar = ':';
    private const char ReplacementChar = '_';

    private static string SanitizeIdentifier(string identifier)
    {
        return identifier?.Replace(InvalidChar, ReplacementChar);
    }

    public static CacheKeyModel GenerateCacheKey<T>(params string[] identifiers)
    {
        if (identifiers == null || identifiers.Length == 0)
            throw new ArgumentException("Identifiers cannot be null or empty", nameof(identifiers));

        // Sanitize identifiers and remove trailing null or empty strings
        var sanitizedIdentifiers = identifiers
            .Reverse()
            .SkipWhile(id => id == null || id == string.Empty)
            .Reverse()
            .Select(SanitizeIdentifier)
            .ToArray();

        // 1. Get cached lowercase type name
        string typeNameLower = typeof(T).GetFormattedTypeName();

        // 2. Calculate the exact final string length needed
        int totalLength = typeNameLower.Length;
        foreach (var id in sanitizedIdentifiers)
        {
            totalLength += 1 + (id?.Length ?? 0); // ':' + length
        }

        // 3. Use string.Create to build the string efficiently
        string key = string.Create(totalLength, (typeNameLower, sanitizedIdentifiers), (span, state) =>
        {
            var (tnl, ids) = state;
            int currentPosition = 0;

            // Copy lowercase type name
            tnl.AsSpan().CopyTo(span);
            currentPosition += tnl.Length;

            // Append identifiers using MemoryExtensions.ToLowerInvariant with hyphen separator
            foreach (var identifier in ids)
            {
                span[currentPosition++] = ':';
                if (identifier != null)
                {
                    ReadOnlySpan<char> sourceSpan = identifier.AsSpan();
                    Span<char> destinationSlice = span.Slice(currentPosition);
                    int written = MemoryExtensions.ToLowerInvariant(sourceSpan, destinationSlice);
                    currentPosition += written;
                }
                // If identifier is null, a ':' will be added.
            }
        });

        return new CacheKeyModel
        {
            Key = key,
            Identifiers = identifiers // Keep original identifiers for potential later use
        };
    }
}
