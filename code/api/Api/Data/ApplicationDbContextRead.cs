using Api.Application.Tenancy.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

/// <summary>
/// Read-only database context for querying data from read replicas.
/// Uses AsNoTracking by default for optimal read performance.
/// </summary>
public class ApplicationDbContextRead : ApplicationDbContext
{
    public ApplicationDbContextRead(
        IRequestTenant requestTenant,
        DbContextOptions<ApplicationDbContextRead> options,
        TimeProvider timeProvider)
        : base(requestTenant, options, timeProvider)
    {
    }
}
