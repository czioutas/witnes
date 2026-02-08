namespace Api.Application.Extensions;

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(
            str.Select(
                (x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString()
            )
        ).ToLower();
    }

    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    /// <summary>
    /// Converts a string to a URL-friendly slug.
    /// Example: "Grandma's Sourdough Bread" -> "grandmas-sourdough-bread"
    /// </summary>
    public static string ToSlug(this string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to lowercase and replace spaces/apostrophes
        var normalized = name.ToLowerInvariant()
            .Trim()
            .Replace(" ", "-")
            .Replace("'", "");

        // Remove any characters that aren't letters, numbers, or hyphens
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9-]", "-");

        // Replace multiple consecutive hyphens with a single hyphen
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-");

        // Remove leading/trailing hyphens
        normalized = normalized.Trim('-');

        return normalized;
    }
}
