using Api.Application.Pricing.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Seeders
{
    public static class PricingTiersSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<PricingTierEntity>().HasData(
                new PricingTierEntity(
                    "Free",
                    "Free tier with limited requests",
                    1000,
                    0.00m,
                    0.001m)
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    CreatedAt = baseDate,
                    UpdatedAt = baseDate
                }
                // Commented out paid tiers for future use
                // new PricingTierEntity(
                //     "Small",
                //     "Small business tier with moderate usage",
                //     10000,
                //     29.99m,
                //     0.0005m)
                // {
                //     Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                //     CreatedAt = baseDate,
                //     UpdatedAt = baseDate
                // },
                // new PricingTierEntity(
                //     "Medium",
                //     "Medium business tier with high usage",
                //     100000,
                //     149.99m,
                //     0.0002m)
                // {
                //     Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                //     CreatedAt = baseDate,
                //     UpdatedAt = baseDate
                // }
            );
        }
    }
}
