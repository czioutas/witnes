using Api.Application.Pricing.Entities;
using Libs.Result;

namespace Api.Application.Pricing.Services.Interfaces;

public interface ITenantPricingService
{
    Task<Result<TenantPricingTierEntity>> SetTenantPricingAsync(Guid tenantId, Guid pricingTierId, DateOnly? startDate = null);
    Task<Result<TenantPricingTierEntity>> ChangeTenantPricingAsync(Guid tenantId, Guid newPricingTierId, DateOnly? startDate = null);
    Task<TenantPricingTierEntity> GetCurrentTenantPricingIgnoreFiltersAsync(Guid tenantId);
    Task<TenantPricingTierEntity?> GetTenantPricingAtDateAsync(Guid tenantId, DateOnly date);
    Task<List<TenantPricingTierEntity>> GetTenantPricingHistoryAsync(Guid tenantId);
    Task<Result<bool>> EndTenantPricingAsync(Guid tenantId, DateOnly? endDate = null);
    Task<PricingTierEntity?> GetFreeTierAsync();
}
