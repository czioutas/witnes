using Microsoft.EntityFrameworkCore;

namespace Api.Data.Seeders;

public static class LimitsSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Note: Tenant limits are seeded per tenant, not globally
        // This seeder provides a reference for default limits that should be
        // applied when creating new tenants. The actual seeding happens
        // in tenant creation logic or migration scripts.

        // Example: Default 3 locations limit for new tenants
        // modelBuilder.Entity<TenantLimitEntity>().HasData(
        //     new TenantLimitEntity
        //     {
        //         Id = Guid.NewGuid(),
        //         TenantId = <tenant-guid>,
        //         LimitKey = LimitKey.Locations,
        //         Period = LimitPeriod.Monthly,
        //         MaxAmount = 3,
        //         CurrentAmount = 0,
        //         CreatedAt = baseDate,
        //         UpdatedAt = baseDate
        //     }
        // );
    }
}
