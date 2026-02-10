using Api.Product.Ingestion.Models;

namespace Api.Product.Ingestion.Events;

/// <summary>
/// Event published when a speed metric is ingested
/// </summary>
public class MetricIngestedEvent
{
    public required IngestMetricRequestModel Event { get; set; }
    public Guid TenantId { get; set; }
    public DateTime IngestedAt { get; set; }
    public required string HashId { get; set; }
}
