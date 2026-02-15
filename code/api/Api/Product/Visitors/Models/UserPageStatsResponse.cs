namespace Api.Product.Visitors.Models;

public class UserPageStatsResponse
{
    public string UserId { get; set; } = null!;
    public string PagePath { get; set; } = null!;
    public string WindowType { get; set; } = null!;
    public int VisitCount { get; set; }
    public double AvgLoadTimeMs { get; set; }
    public double AvgDocTtfbMs { get; set; }
    public double AvgApiTtfbWorstMs { get; set; }
    public double AvgTransferSizeBytes { get; set; }
    public double AvgCdnLoadTimeMs { get; set; }
}
