using Api.Product.Metrics.Models;
using Api.Product.MetricsProcessing.Gold;
using AutoMapper;

namespace Api.Product.Metrics;

/// <summary>
/// AutoMapper profile for Metrics mappings
/// </summary>
public class MetricsMapper : Profile
{
    public MetricsMapper()
    {
        CreateMap<MetricGoldEntity, SpeedMetricResponse>();
    }
}
