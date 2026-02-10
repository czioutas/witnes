using Api.Application.ProjectKeys.Entities;
using Api.Application.ProjectKeys.Models;
using Api.Application.Services;
using Api.Application.Tenancy.Services;
using Api.Data;
using Libs.Result;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Application.ProjectKeys.Services;

public interface IProjectKeyService
{
    Task<Result<ProjectKeyModel>> CreateAsync(Guid tenantId, CreateProjectKeyRequest request);
    Task<Result<ProjectKeyModel>> GetByKeyAsync(string projectKey);
    Task<Result<List<ProjectKeyModel>>> GetAllForTenantAsync(Guid tenantId);
    Task<Result<ProjectKeyModel>> UpdateAsync(Guid tenantId, Guid projectKeyId, UpdateProjectKeyRequest request);
    Task<Result<bool>> DeleteAsync(Guid tenantId, Guid projectKeyId);
    Task<Result<bool>> UpdateLastUsedAsync(string projectKey);
}

public class ProjectKeyService : IProjectKeyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ApplicationDbContextRead _dbContextRead;
    private readonly IFusionCache _cache;
    private readonly TimeProvider _timeProvider;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private readonly IRequestTenant _requestTenant;

    public ProjectKeyService(
        IRequestTenant requestTenant,
        ApplicationDbContext dbContext,
        ApplicationDbContextRead dbContextRead,
        IFusionCache cache,
        TimeProvider timeProvider)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbContextRead = dbContextRead ?? throw new ArgumentNullException(nameof(dbContextRead));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<ProjectKeyModel>> CreateAsync(Guid tenantId, CreateProjectKeyRequest request)
    {
        var projectKey = GenerateProjectKey();

        var entity = new ProjectKeyEntity
        {
            TenantId = tenantId,
            ProjectKey = projectKey,
            Name = request.Name,
            Domain = DomainNormalizer.Normalize(request.Domain),
            IsActive = true
        };

        await _dbContext.ProjectKeys.AddAsync(entity);
        var result = await _dbContext.SaveChangesAsync();

        if (result != 1)
        {
            return Result<ProjectKeyModel>.BusinessError("ProjectKeyCreation", "Could not create project key");
        }

        // Cache the newly created key
        var cacheKey = GetCacheKey(projectKey);
        var model = MapToModel(entity);
        await _cache.SetAsync(cacheKey, model, new FusionCacheEntryOptions { Duration = CacheDuration });

        return new Result<ProjectKeyModel>(model);
    }

    public async Task<Result<ProjectKeyModel>> GetByKeyAsync(string projectKey)
    {
        var cacheKey = GetCacheKey(projectKey);

        var model = await _cache.GetOrSetAsync<ProjectKeyModel?>(
            cacheKey,
            async (ctx, ct) =>
            {
                var result = await UNSAFEGetProjectKeyFromDbAsync(projectKey);

                if (result == null)
                {
                    ctx.Options.Duration = TimeSpan.FromMinutes(15);
                }

                return result;
            },
            new FusionCacheEntryOptions { Duration = CacheDuration }
        );

        return model != null
            ? new Result<ProjectKeyModel>(model)
            : Result<ProjectKeyModel>.NotFound("ProjectKey", projectKey);
    }

    public async Task<Result<List<ProjectKeyModel>>> GetAllForTenantAsync(Guid tenantId)
    {
        var entities = await _dbContextRead.ProjectKeys
            .AsNoTracking()
            .Where(pk => pk.TenantId == tenantId)
            .OrderByDescending(pk => pk.CreatedAt)
            .ToListAsync();

        var models = entities.Select(MapToModel).ToList();
        return new Result<List<ProjectKeyModel>>(models);
    }

    public async Task<Result<ProjectKeyModel>> UpdateAsync(Guid tenantId, Guid projectKeyId, UpdateProjectKeyRequest request)
    {
        var entity = await _dbContext.ProjectKeys.Where(pk => pk.TenantId == tenantId && pk.Id == projectKeyId).FirstOrDefaultAsync();

        if (entity == null)
        {
            return Result<ProjectKeyModel>.NotFound("ProjectKey", projectKeyId.ToString());
        }

        // Update only provided fields
        if (request.Name != null)
        {
            entity.Name = request.Name;
        }

        if (request.Description != null)
        {
            entity.Description = request.Description;
        }

        if (request.Domain != null)
        {
            entity.Domain = DomainNormalizer.Normalize(request.Domain);
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        await _dbContext.SaveChangesAsync();

        // Invalidate cache
        var cacheKey = GetCacheKey(entity.ProjectKey);
        await _cache.RemoveAsync(cacheKey);

        var model = MapToModel(entity);
        return new Result<ProjectKeyModel>(model);
    }

    public async Task<Result<bool>> DeleteAsync(Guid tenantId, Guid projectKeyId)
    {
        var entity = await _dbContext.ProjectKeys.Where(pk => pk.TenantId == tenantId && pk.Id == projectKeyId).FirstOrDefaultAsync();

        if (entity == null)
        {
            return Result<bool>.NotFound("ProjectKey", projectKeyId.ToString());
        }

        // Invalidate cache before removing
        var cacheKey = GetCacheKey(entity.ProjectKey);
        await _cache.RemoveAsync(cacheKey);

        _dbContext.ProjectKeys.Remove(entity);
        await _dbContext.SaveChangesAsync();

        return new Result<bool>(true);
    }

    public async Task<Result<bool>> UpdateLastUsedAsync(string projectKey)
    {
        // Use direct database update to avoid triggering full entity tracking
        var entity = await _dbContext.ProjectKeys
            .FirstOrDefaultAsync(pk => pk.ProjectKey == projectKey);

        if (entity == null)
        {
            return Result<bool>.NotFound("ProjectKey", projectKey);
        }

        entity.LastUsedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync();

        // Update cached version with new LastUsedAt
        var cacheKey = GetCacheKey(projectKey);
        var model = MapToModel(entity);
        await _cache.SetAsync(cacheKey, model, new FusionCacheEntryOptions { Duration = CacheDuration });

        return new Result<bool>(true);
    }

    private async Task<ProjectKeyModel?> UNSAFEGetProjectKeyFromDbAsync(string projectKey)
    {
        var entity = await _dbContextRead.ProjectKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(pk => pk.ProjectKey == projectKey);

        return entity != null ? MapToModel(entity) : null;
    }

    private static string GenerateProjectKey()
    {
        // Generate cryptographically secure key in Stripe-style format
        return $"pk_{Guid.NewGuid():N}";
    }

    private static string GetCacheKey(string projectKey)
    {
        return CacheKeyGeneratorService.GenerateCacheKey<ProjectKeyEntity>("key", projectKey).Key;
    }

    private ProjectKeyModel MapToModel(ProjectKeyEntity entity)
    {
        return new ProjectKeyModel
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            ProjectKey = entity.ProjectKey,
            Name = entity.Name,
            Domain = entity.Domain,
            IsActive = entity.IsActive,
            LastUsedAt = entity.LastUsedAt,
            CreatedAt = entity.CreatedAt
        };
    }
}
