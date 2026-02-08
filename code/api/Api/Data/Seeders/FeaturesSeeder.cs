using Api.Application.Features.Entities;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Seeders;

public static class FeaturesSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var baseDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<FeatureEntity>().HasData(
            new FeatureEntity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = FeatureKey.Dropzone,
                Name = "DropZone",
                Description = "AI-powered document processing and activity extraction from PDFs",
                IsGlobal = false,
                IsEnabledByDefault = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            }
        );
    }
}
