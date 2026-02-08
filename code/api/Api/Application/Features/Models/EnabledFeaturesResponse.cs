using Libs.Domain;

namespace Api.Application.Features.Models;

public record EnabledFeaturesResponse
{
    public required List<FeatureKey> Features { get; init; }
}
