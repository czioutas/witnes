using Api.Data;

namespace Api.Product.MetricsProcessing.Gold;

/// <summary>
/// Service interface for Gold layer operations
/// </summary>
public interface IGoldService
{

}

/// <summary>
/// Service for Gold layer operations
/// </summary>
public class GoldService : IGoldService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GoldService> _logger;

    public GoldService(
        ApplicationDbContext context,
        ILogger<GoldService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
