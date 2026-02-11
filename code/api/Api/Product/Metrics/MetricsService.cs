using Api.Data;
using Api.Product.Metrics.Models;
using Api.Product.MetricsProcessing.Gold;
using Api.Product.MetricsProcessing.Silver;
using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.Metrics;

/// <summary>
/// Service interface for retrieving metrics data
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Gets all gold-layer metrics for the current tenant
    /// </summary>
    Task<List<MetricResponse>> GetAllMetricsAsync();

    /// <summary>
    /// Gets a specific metric by ID
    /// </summary>
    Task<MetricResponse?> GetMetricByIdAsync(Guid id);

    Task<bool> RecalculateStageForTenantAsync(Guid tenantId, string stage);
}

/// <summary>
/// Service for retrieving metrics data from Gold layer
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ApplicationDbContextRead _contextRead;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        ApplicationDbContextRead contextRead,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        ILogger<MetricsService> logger)
    {
        _contextRead = contextRead ?? throw new ArgumentNullException(nameof(contextRead));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<MetricResponse>> GetAllMetricsAsync()
    {
        _logger.LogInformation("Retrieving all Gold metrics");

        var metrics = await _contextRead.MetricsGold
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<MetricResponse>>(metrics);
    }

    /// <inheritdoc/>
    public async Task<MetricResponse?> GetMetricByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving Gold metric by Id={Id}", id);

        var metric = await _contextRead.MetricsGold
            .FirstOrDefaultAsync(m => m.Id == id);

        return metric != null ? _mapper.Map<MetricResponse>(metric) : null;
    }

    public async Task<bool> RecalculateStageForTenantAsync(Guid tenantId, string stage)
    {
        switch (stage.ToLower())
        {
            case "silver":
                _logger.LogInformation("Publishing RecalculateSilverEvent for TenantId={TenantId}", tenantId);
                await _publishEndpoint.Publish(new RecalculateSilverEvent { TenantId = tenantId });
                return true;
            case "gold":
                _logger.LogInformation("Publishing RecalculateGoldEvent for TenantId={TenantId}", tenantId);
                await _publishEndpoint.Publish(new RecalculateGoldEvent { TenantId = tenantId });
                return true;
            default:
                return false;
        }
    }
}
