namespace Api.Application.ProjectKeys;

public static class DomainNormalizer
{
    /// <summary>
    /// Normalizes a domain input by lowercasing, removing protocol, trailing slashes, and paths.
    /// Returns just the hostname (e.g., "www.example.com").
    /// </summary>
    public static string Normalize(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return string.Empty;

        var input = domain.Trim().ToLowerInvariant();

        // If it looks like a URL with a scheme, parse it
        if (input.Contains("://"))
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return uri.Host;
        }

        // Otherwise treat as a bare hostname — strip any path/query/fragment
        var slashIndex = input.IndexOf('/');
        if (slashIndex >= 0)
            input = input[..slashIndex];

        var questionIndex = input.IndexOf('?');
        if (questionIndex >= 0)
            input = input[..questionIndex];

        var hashIndex = input.IndexOf('#');
        if (hashIndex >= 0)
            input = input[..hashIndex];

        // Remove port if present
        var colonIndex = input.IndexOf(':');
        if (colonIndex >= 0)
            input = input[..colonIndex];

        return input;
    }

    /// <summary>
    /// Extracts and normalizes the hostname from an Origin header value.
    /// Origin header format: scheme "://" host [ ":" port ]
    /// </summary>
    public static string NormalizeOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return string.Empty;

        if (Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri))
            return uri.Host.ToLowerInvariant();

        // Fallback: treat as bare hostname
        return Normalize(origin);
    }
}
