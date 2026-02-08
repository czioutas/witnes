# Feature Toggle System

Multi-tenant feature flag system using Microsoft.FeatureManagement with database-backed configuration.

## Architecture

### Core Components

- **FeatureKey** (enum): Type-safe feature identifiers with `[EnumMember]` attributes for serialization
- **FeatureEntity**: Global feature definitions with default enablement state
- **TenantFeatureEntity**: Tenant-specific feature toggles with date-based activation (EnabledFrom/EnabledUntil)
- **DatabaseFeatureDefinitionProvider**: Bridges database to Microsoft.FeatureManagement framework

### How It Works

1. **Feature Definition**: Features are defined in `FeatureKey` enum and seeded into `features` table
2. **Tenant Assignment**: Features are enabled per-tenant in `tenant_features` table with optional date ranges
3. **Provider Logic**: `DatabaseFeatureDefinitionProvider` (singleton) accesses scoped services via `IHttpContextAccessor` to:
   - Query database for enabled features (global + tenant-specific)
   - Filter by tenant ID using `IgnoreQueryFilters()` with explicit WHERE clause
   - Check date ranges (EnabledFrom/EnabledUntil) against current date
   - Cache results for 15 minutes using FusionCache
4. **Framework Integration**: Returns `FeatureDefinition` with `AlwaysOn` filter for enabled features
5. **Usage**: Controllers inject `IFeatureManager` and call `await _featureManager.IsEnabledAsync(FeatureKey.DropZone.ToString())`

## Key Design Decisions

- **Enum-based**: Type safety and compile-time checking using `FeatureKey` enum
- **Pre-filtering**: Database queries handle all filtering logic (tenant, dates) before returning to framework
- **AlwaysOn pattern**: Simple enabled/disabled state rather than complex feature filters
- **Singleton + Scoped**: Provider uses `IHttpContextAccessor` to access current request's scoped services (IRequestTenant, ApplicationDbContext)
- **IgnoreQueryFilters**: Explicit tenant filtering since provider is singleton and can't rely on automatic query filters
- **Snake_case columns**: Database columns use snake_case naming via `[Column]` attributes

## Frontend Integration

- **FeatureContext**: React context provides `isFeatureEnabled(feature: string)` hook
- **Type-safe**: Generated TypeScript types from OpenAPI match backend FeatureKey enum

## Testing

- In-memory EF Core database with real FusionCache
- Mock `IHttpContextAccessor` with `DefaultHttpContext.RequestServices` pointing to test service provider
- Tests cover global features, tenant features, date-based activation, and edge cases
