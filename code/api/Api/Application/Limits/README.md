# Limits System

Resource usage tracking and enforcement system for multi-tenant platform limits.

## Architecture

### Core Components

- **LimitKey** (enum): Type-safe identifiers for limited resources (Locations, etc.)
- **LimitPeriod** (enum): Time period for limits (Daily, Weekly, Monthly)
- **TenantLimitEntity**: Per-tenant limit configuration with usage tracking
- **ILimitsService / LimitsService**: Core service interface and implementation for checking and enforcing limits
- **ResetMonthlyLimitsJob**: Coravel job that resets monthly limits on 1st of each month

### How It Works

1. **Tenant Creation**: Default limits are created when a new tenant is created (e.g., 3 locations monthly)
2. **Usage Tracking**: Services call `LimitsService.IncrementAsync()` before creating resources
3. **Limit Enforcement**: Returns `Result<T>` with error if limit would be exceeded
4. **Automatic Cleanup**: Coravel job resets `CurrentAmount` to 0 monthly at 00:01 UTC
5. **Rollback on Failure**: Services call `DecrementAsync()` if resource creation fails

## Usage

### Before Creating a Resource

```csharp
public async Task<Result<LocationModel>> CreateAsync(CreateLocationRequest request)
{
    // Check and increment limit
    var limitResult = await _limitsService.IncrementAsync(LimitKey.Locations);
    if (limitResult.IsFailure)
    {
        return limitResult.ToErrorResult<LocationModel>();
    }

    try
    {
        // Create resource...
        return Result<LocationModel>.Ok(model);
    }
    catch (Exception ex)
    {
        // Rollback limit on failure
        await _limitsService.DecrementAsync(LimitKey.Locations);
        throw;
    }
}
```

### After Deleting a Resource

```csharp
public async Task<Result<bool>> DeleteAsync(Guid id)
{
    // Delete resource...
    await _dbContext.SaveChangesAsync();

    // Decrement counter
    await _limitsService.DecrementAsync(LimitKey.Locations);

    return Result<bool>.Ok(true);
}
```

## Key Design Decisions

- **Enum-based**: Type-safe limit keys using `LimitKey` enum
- **Result pattern**: Returns `Result<T>` with user-friendly error messages from service
- **Tenant isolation**: Uses `IRequestTenant` for automatic multi-tenant filtering
- **Soft enforcement**: Allows slight over-limit in rare concurrent scenarios (acceptable tradeoff vs. locking)
- **No limits = unlimited**: Missing limit configuration allows operation but logs warning
- **Seeded defaults**: New tenants get default limits (3 locations monthly)
- **Coravel scheduling**: Monthly reset runs on 1st day at 00:01 UTC

## Database Schema

### tenant_limits table

| Column | Type | Description |
|--------|------|-------------|
| id | uuid | Primary key |
| tenant_id | uuid | Tenant identifier (with RLS) |
| limit_key | enum | Resource being limited |
| period | enum | Time period (default: Monthly) |
| max_amount | int | Maximum allowed usage |
| current_amount | int | Current usage counter |
| last_reset_at | timestamp | Last time counter was reset |

## Testing

- In-memory EF Core database
- Comprehensive test coverage:
  - Limit enforcement (within/over limit)
  - Increment/decrement operations
  - Multi-amount operations
  - No limit configured (warning logs)
  - Multi-tenant isolation
  - Edge cases (don't go below zero)

## Adding New Limit Types

1. Add new value to `LimitKey` enum in [Libs/Domain/LimitKey.cs](../../libs/Libs/Domain/LimitKey.cs)
2. Update `TenantService.CreateAsync()` to seed default limit for new tenants
3. Integrate `LimitsService` into the relevant service (before create, after delete)
4. Add tests for the new limit type

## Cron Schedule

- **Monthly Reset**: 1st day of month at 00:01 UTC (configured in [SchedulerExtensions.cs](../Extensions/SchedulerExtensions.cs))
