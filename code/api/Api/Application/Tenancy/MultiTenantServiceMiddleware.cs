using Api.Application.Account.Services.Interfaces;
using Api.Application.Authentication;
using Api.Application.Tenancy.Services;

namespace Api.Application.Tenancy;

public class MultiTenantServiceMiddleware : IMiddleware
{
    private readonly IRequestTenant _requestTenant;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _dateTimeProvider;
    public MultiTenantServiceMiddleware(IRequestTenant requestTenant, ITokenService tokenService, TimeProvider dateTimeProvider)
    {
        _requestTenant = requestTenant;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        string? tenantId = TryGetTenantFromJwt(token);

        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = context.Request.Headers["tenantId"];
        }

        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out Guid parsedTenantId))
        {
            _requestTenant.SetTenantId(parsedTenantId);
        }

        await next(context);
    }

    private string? TryGetTenantFromJwt(string token)
    {
        // Get the token from the Authorization header
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var claimsPrincipal = _tokenService.GetPrincipalFromToken(token, _dateTimeProvider.GetUtcNow());

                if (claimsPrincipal is null)
                {
                    return null;
                }

                // Extract the user ID from the token
                var tenantId = claimsPrincipal.FindFirst(ClaimTypeConstants.TenantId)?.Value;

                return tenantId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        return null;
    }
}
