using Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Application.HealthChecks
{
    public class DbContextHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _dbContext;

        public DbContextHealthCheck(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
    }
}
