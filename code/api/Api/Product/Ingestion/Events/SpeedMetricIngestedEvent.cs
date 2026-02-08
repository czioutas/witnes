using Api.Product.Ingestion.Models;

namespace Api.Product.Ingestion.Events;

/// <summary>
/// Event published when a speed metric is ingested
/// </summary>
public class SpeedMetricIngestedEvent
{
    public required IngestSpeedMetricRequestModel Event { get; set; }
    public Guid TenantId { get; set; }
    public DateTime IngestedAt { get; set; }
}
