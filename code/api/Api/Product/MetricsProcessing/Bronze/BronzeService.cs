using Api.Data;
using Api.Product.Ingestion.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.MetricsProcessing.Bronze;

public interface IBronzeService
{
    /// <summary>
    /// Stores raw metric in Bronze layer with full session context
    /// </summary>
    Task<MetricBronzeEntity> StoreAsync(IngestMetricRequestModel request, Guid tenantId, DateTimeOffset ingestedAt, string hashId);

    /// <summary>
    /// Deletes Bronze metrics older than the specified cutoff date for a tenant.
    /// </summary>
    Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate);
}

public class BronzeService : IBronzeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BronzeService> _logger;

    public BronzeService(
        ApplicationDbContext context,
        ILogger<BronzeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<MetricBronzeEntity> StoreAsync(IngestMetricRequestModel request, Guid tenantId, DateTimeOffset ingestedAt, string hashId)
    {
        var entity = new MetricBronzeEntity
        {
            TenantId = tenantId,
            IngestedAt = ingestedAt,

            // Searchable Identity
            UserId = request.Session.UserId,
            HashId = hashId,
            Url = request.Session.Url,
            EventType = request.Metadata.Event,
            PageRequestedAtByVisitor = request.Metadata.PageRequestedAtByVisitor,

            // JSONB columns — Npgsql serializes these automatically
            Metadata = request.Metadata,
            Session = request.Session,
            Performance = request.Performance,
            Network = request.Network,
            Device = request.Device
        };

        await _context.MetricsBronze.AddAsync(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bronze metric stored for HashId {HashId} at {Url}",
            entity.HashId, entity.Url);

        return entity;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTimeOffset cutoffDate)
    {
        return await _context.MetricsBronze
            .Where(m => m.PageRequestedAtByVisitor < cutoffDate)
            .ExecuteDeleteAsync();
    }
}