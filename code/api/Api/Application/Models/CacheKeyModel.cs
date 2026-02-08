namespace Api.Application.Models;

public record CacheKeyModel
{
    public required string Key { get; set; }
    public required IReadOnlyList<string> Identifiers { get; set; }
}
