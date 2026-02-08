namespace Api.Application.Converters;

using System.Text.Json;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return string.Concat(
            name.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + char.ToLowerInvariant(c)
                    : char.ToLowerInvariant(c).ToString()
            )
        );
    }
}
