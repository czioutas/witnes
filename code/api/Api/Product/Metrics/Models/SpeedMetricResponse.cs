namespace Api.Product.Metrics.Models;

/// <summary>
/// Response model for speed metric data
/// </summary>
public class SpeedMetricResponse
{
    public Guid Id { get; set; }
    public decimal Speed { get; set; }
    public bool IsGold { get; set; }
    public DateTime CreatedAt { get; set; }
}
