using System.Text.Json;
using Api.Application.Account.Entities;
using Api.Application.Application.Entities;
using Api.Application.Features.Entities;
using Api.Application.Limits.Entities;
using Api.Application.Pricing.Entities;
using Api.Application.ProjectKeys.Entities;
using Api.Application.Tenancy.Entities;
using Api.Application.Tenancy.Services;
using Api.Application.Users;
using Api.Data.Seeders;
using Api.Product.DailySalt;
using Api.Product.MetricsProcessing.Bronze;
using Api.Product.MetricsProcessing.Gold;
using Api.Product.MetricsProcessing.Silver;
using Libs.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUserEntity, ApplicationRoleEntity, Guid>
{
    private readonly IRequestTenant _requestTenant;
    private readonly TimeProvider _dateTimeProvider;
    public Guid CurrentTenantId => _requestTenant.TenantId;

    public ApplicationDbContext(
        IRequestTenant requestTenant,
        DbContextOptions<ApplicationDbContext> options,
        TimeProvider dateTimeProvider) :
        base(options)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    // Protected constructor for derived contexts
    protected ApplicationDbContext(
        IRequestTenant requestTenant,
        DbContextOptions options,
        TimeProvider dateTimeProvider) :
        base(options)
    {
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public void SetTenantId(Guid tenantId)
    {
        _requestTenant.SetTenantId(tenantId);
    }


    // What is => Set<T>()? https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#dbcontext-and-dbset
    public virtual DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public virtual DbSet<TenantDetailsEntity> TenantDetails => Set<TenantDetailsEntity>();
    public virtual DbSet<UserInvitationEntity> UserInvitations => Set<UserInvitationEntity>();
    public virtual DbSet<PricingTierEntity> PricingTiers => Set<PricingTierEntity>();
    public virtual DbSet<TenantPricingTierEntity> TenantPricingTiers => Set<TenantPricingTierEntity>();
    public virtual DbSet<FeatureEntity> Features => Set<FeatureEntity>();
    public virtual DbSet<TenantFeatureEntity> TenantFeatures => Set<TenantFeatureEntity>();
    public virtual DbSet<TenantLimitEntity> TenantLimits => Set<TenantLimitEntity>();
    public virtual DbSet<ProjectKeyEntity> ProjectKeys => Set<ProjectKeyEntity>();

    // Daily Salt (for visitor hash ID generation)
    public virtual DbSet<DailySaltEntity> DailySalts => Set<DailySaltEntity>();

    // Witnes Metrics (Medallion Architecture)
    public virtual DbSet<MetricBronzeEntity> MetricsBronze => Set<MetricBronzeEntity>();
    public virtual DbSet<MetricSilverEntity> MetricsSilver => Set<MetricSilverEntity>();
    public virtual DbSet<MetricGoldEntity> MetricsGold => Set<MetricGoldEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureTenant(builder);
        ConfigureRLSForEntities(builder);

        // Configure PostgreSQL enums
        builder.HasPostgresEnum<FeatureKey>();
        builder.HasPostgresEnum<LimitKey>();
        builder.HasPostgresEnum<LimitPeriod>();

        // Seed data (order matters - roles and pricing must come first)
        PricingTiersSeeder.Seed(builder);
        FeaturesSeeder.Seed(builder);
        LimitsSeeder.Seed(builder);

        builder.Entity<MetricSilverEntity>(entity =>
        {
            // 1. Map to jsonb for PostgreSQL
            entity.Property(e => e.Waterfall)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<ProcessedResource>>(v, (JsonSerializerOptions)null!)
                        ?? new List<ProcessedResource>()
                );

            entity.Property(e => e.JankReports)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!)
                        ?? new List<string>()
                );

            // 2. Ensure the tenant_id FK and other constraints
            entity.ToTable("metrics_silver");
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // Check if the entity inherits from any AuditableBaseEntity variant
            var isAuditable = typeof(AuditableBaseEntity).IsAssignableFrom(entityType.ClrType) ||
                              typeof(AuditableBaseEntityLong).IsAssignableFrom(entityType.ClrType);

            if (isAuditable)
            {
                builder.Entity(entityType.ClrType).Property(nameof(AuditableBaseEntity.CreatedAt)).Metadata
                    .SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            }
        }
    }

    private static void ConfigureTenant(ModelBuilder builder)
    {
        builder.Entity<TenantEntity>().HasIndex(t => t.Identifier).IsUnique();
    }

    private void ConfigureRLSForEntities(ModelBuilder builder)
    {
        builder.Entity<UserInvitationEntity>(entity =>
        {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        builder.Entity<TenantPricingTierEntity>(entity =>
        {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        builder.Entity<TenantFeatureEntity>(entity =>
        {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        builder.Entity<TenantLimitEntity>(entity =>
        {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        builder.Entity<ProjectKeyEntity>(entity =>
        {
            entity.HasIndex(e => e.ProjectKey).IsUnique();
        });
    }



    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateStatuses();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateStatuses();
        return base.SaveChanges();
    }

    // private void DeleteDependent(EntityEntry entry)
    // {
    //     entry.CurrentValues.SetValues(new { DeletedAt = _dateTimeProvider.GetUtcNow(), Deleted = true });
    //     entry.State = EntityState.Modified;
    // }

    private void UpdateStatuses()
    {
        // Handle TenantAwareEntity (Guid IDs - only sets TenantId)
        foreach (var entry in ChangeTracker.Entries<TenantAwareEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.TenantId = entry.Entity.TenantId != Guid.Empty ? entry.Entity.TenantId : _requestTenant.TenantId;
                    break;
            }
        }

        // Handle TenantAwareEntityLong (long IDs - only sets TenantId)
        foreach (var entry in ChangeTracker.Entries<TenantAwareEntityLong>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.TenantId = entry.Entity.TenantId != Guid.Empty ? entry.Entity.TenantId : _requestTenant.TenantId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = _dateTimeProvider.GetUtcNow();
                    entry.Entity.UpdatedAt = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTimeProvider.GetUtcNow();
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableBaseEntityLong>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = _dateTimeProvider.GetUtcNow();
                    entry.Entity.UpdatedAt = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTimeProvider.GetUtcNow();
                    break;
            }
        }
    }
}
