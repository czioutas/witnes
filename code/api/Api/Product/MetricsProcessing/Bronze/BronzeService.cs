using Api.Data;
using Api.Product.Ingestion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Product.MetricsProcessing.Bronze;

public interface IBronzeService
{
    /// <summary>
    /// Stores raw speed metric in Bronze layer with full session context
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
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
        // We extract the "First Glance" metadata to columns,
        // and keep the "Deep Dive" data in the JSON payload.
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

            // Store the dynamic blocks separately as JSONB
            // This makes it easier to debug and avoids a single gigantic JSON blob
            Metadata = JsonSerializer.Serialize(request.Metadata, JsonOptions),
            Session = JsonSerializer.Serialize(request.Session, JsonOptions),
            Performance = JsonSerializer.Serialize(request.Performance, JsonOptions),
            Network = request.Network != null ? JsonSerializer.Serialize(request.Network, JsonOptions) : null,
            Device = request.Device != null ? JsonSerializer.Serialize(request.Device, JsonOptions) : null
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