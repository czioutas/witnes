using Api.Data;
using Api.Product.Ingestion.Models;
using System.Text.Json;

namespace Api.Product.MetricsProcessing.Bronze;

public interface IBronzeService
{
    /// <summary>
    /// Stores raw speed metric in Bronze layer with full session context
    /// </summary>
    Task<MetricBronzeEntity> StoreAsync(IngestSpeedMetricRequestModel request, Guid tenantId, DateTime ingestedAt);
}

public class BronzeService : IBronzeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BronzeService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BronzeService(
        ApplicationDbContext context,
        ILogger<BronzeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<MetricBronzeEntity> StoreAsync(IngestSpeedMetricRequestModel request, Guid tenantId, DateTime ingestedAt)
    {
        // We extract the "First Glance" metadata to columns, 
        // and keep the "Deep Dive" data in the JSON payload.
        var entity = new MetricBronzeEntity
        {
            TenantId = tenantId,
            IngestedAt = ingestedAt,

            // Searchable Identity
            UserId = request.Session.UserId,
            SessionId = request.Session.SessionId,
            Url = request.Session.Url,
            EventType = request.Metadata.Event,

            // Store the dynamic performance block (Waterfall, Vitals, Jank) as JSONB
            // This prevents "Schema Fear" when adding new browser metrics later
            RawPerformanceData = JsonSerializer.Serialize(request, JsonOptions)
        };

        await _context.MetricsBronze.AddAsync(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bronze metric stored for Session {SessionId} at {Url}",
            entity.SessionId, entity.Url);

        return entity;
    }
}