using Api.Application.Pricing.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Seeders
{
    public static class PricingTiersSeeder
    {
        public static readonly Guid StarterTierId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid GrowthTierId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        public static void Seed(ModelBuilder modelBuilder)
        {
            var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<PricingTierEntity>().HasData(
                new PricingTierEntity(
                    "Starter",
                    "For small teams getting started with session diagnostics",
                    50_000,
                    29.00m,
                    2,
                    7)
                {
                    Id = StarterTierId,
                    CreatedAt = baseDate,
                    UpdatedAt = baseDate
                },
                new PricingTierEntity(
                    "Growth",
                    "For growing teams that need more capacity and longer retention",
                    100_000,
                    80.00m,
                    4,
                    14)
                {
                    Id = GrowthTierId,
                    CreatedAt = baseDate,
                    UpdatedAt = baseDate
                }
            );
        }
    }
}
