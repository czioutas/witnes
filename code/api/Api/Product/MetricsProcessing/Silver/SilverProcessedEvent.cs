namespace Api.Product.MetricsProcessing.Silver;

/// <summary>
/// Event published when Silver layer processing is complete
/// </summary>
public class SilverProcessedEvent
{
    public Guid SilverId { get; set; }
    public Guid BronzeId { get; set; }
    public Guid TenantId { get; set; }
}
