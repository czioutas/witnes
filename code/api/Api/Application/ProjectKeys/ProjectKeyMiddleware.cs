using Api.Application.ProjectKeys.Services;
using Api.Application.Tenancy.Services;
using System.Linq;

namespace Api.Application.ProjectKeys;

public class ProjectKeyMiddleware : IMiddleware
{
    private readonly IProjectKeyService _projectKeyService;
    private readonly IRequestTenant _requestTenant;
    private readonly ILogger<ProjectKeyMiddleware> _logger;
    private readonly TimeProvider _timeProvider;

    public ProjectKeyMiddleware(
        IProjectKeyService projectKeyService,
        IRequestTenant requestTenant,
        ILogger<ProjectKeyMiddleware> logger,
        TimeProvider timeProvider)
    {
        _projectKeyService = projectKeyService ?? throw new ArgumentNullException(nameof(projectKeyService));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // 1. Extract project key from query parameter 'pk'
        if (!context.Request.Query.TryGetValue("pk", out var projectKeyQuery))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "MissingProjectKey",
                message = "Project key 'pk' query parameter is required"
            });
            return;
        }

        var projectKey = projectKeyQuery.ToString();

        if (string.IsNullOrWhiteSpace(projectKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "InvalidProjectKey",
                message = "Project key cannot be empty"
            });
            return;
        }

        // 2. Validate project key (uses FusionCache internally)
        var result = await _projectKeyService.GetByKeyAsync(projectKey);

        if (result.IsFailure)
        {
            _logger.LogWarning("Invalid project key attempted: {ProjectKey}", projectKey);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "InvalidProjectKey",
                message = "Project key is not valid"
            });
            return;
        }

        var projectKeyModel = result.GetValue;

        // 3. Check if active
        if (!projectKeyModel.IsActive)
        {
            _logger.LogWarning("Inactive project key used: {ProjectKey}", projectKey);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "InactiveProjectKey",
                message = "Project key has been deactivated"
            });
            return;
        }

        // 4. Validate origin
        var origin = context.Request.Headers.Origin.ToString();
        // Allow empty origins ONLY if in Development environment OR if specifically handled
#if DEBUG
        if (string.IsNullOrEmpty(origin))
        {
            _logger.LogInformation("Skipping Origin check for non-browser request in Debug mode.");
        }
        else
#endif
            if (string.IsNullOrEmpty(origin) || !projectKeyModel.Domain.Contains(origin, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid origin for project key {ProjectKey}: {Origin}",
                    projectKey, origin);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "InvalidOrigin",
                    message = "Origin is not allowed for this project key"
                });
                return;
            }

        // 5. Set tenant context (CRITICAL - same as MultiTenantServiceMiddleware)
        _requestTenant.SetTenantId(projectKeyModel.TenantId);

        await next(context);
    }
}
