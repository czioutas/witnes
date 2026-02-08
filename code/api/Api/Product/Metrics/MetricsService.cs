using Api.Data;
using Api.Product.Metrics.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Api.Product.Metrics;

/// <summary>
/// Service interface for retrieving metrics data
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Gets all gold-layer speed metrics for the current tenant
    /// </summary>
    Task<List<SpeedMetricResponse>> GetAllMetricsAsync();

    /// <summary>
    /// Gets a specific speed metric by ID
    /// </summary>
    Task<SpeedMetricResponse?> GetMetricByIdAsync(Guid id);
}

/// <summary>
/// Service for retrieving metrics data from Gold layer
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ApplicationDbContextRead _contextRead;
    private readonly IMapper _mapper;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        ApplicationDbContextRead contextRead,
        IMapper mapper,
        ILogger<MetricsService> logger)
    {
        _contextRead = contextRead ?? throw new ArgumentNullException(nameof(contextRead));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<SpeedMetricResponse>> GetAllMetricsAsync()
    {
        _logger.LogInformation("Retrieving all Gold metrics");

        var metrics = await _contextRead.MetricsGold
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<SpeedMetricResponse>>(metrics);
    }

    /// <inheritdoc/>
    public async Task<SpeedMetricResponse?> GetMetricByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving Gold metric by Id={Id}", id);

        var metric = await _contextRead.MetricsGold
            .FirstOrDefaultAsync(m => m.Id == id);

        return metric != null ? _mapper.Map<SpeedMetricResponse>(metric) : null;
    }
}
