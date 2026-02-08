namespace Api.Product.MetricsProcessing.Bronze;

/// <summary>
/// Event published when Bronze layer processing is complete
/// </summary>
public class BronzeProcessedEvent
{
    public Guid BronzeId { get; set; }
    public Guid TenantId { get; set; }
}
