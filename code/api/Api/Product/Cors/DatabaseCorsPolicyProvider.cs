using Api.Product.Cors.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Api.Product.Cors;

public class DatabaseCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly IAllowedOriginService _originService;

    public DatabaseCorsPolicyProvider(IAllowedOriginService originService)
    {
        _originService = originService;
    }

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origins = await _originService.GetAllowedOriginsAsync();

        var policy = new CorsPolicyBuilder()
            .WithOrigins([.. origins])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition")
            .Build();

        return policy;
    }
}
