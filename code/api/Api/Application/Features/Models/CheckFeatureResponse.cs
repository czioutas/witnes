using Libs.Domain;

namespace Api.Application.Features.Models;

public record CheckFeatureResponse
{
    public required FeatureKey FeatureKey { get; init; }
    public required bool IsEnabled { get; init; }
}
