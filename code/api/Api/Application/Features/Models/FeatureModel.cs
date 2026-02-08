using Libs.Domain;

namespace Api.Application.Features.Models;

public record FeatureModel
{
    public required FeatureKey Key { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsGlobal { get; init; }
    public bool IsEnabledByDefault { get; init; }
}
