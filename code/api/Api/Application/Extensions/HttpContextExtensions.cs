using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Api.Application.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;

namespace Api.Application.Extensions;

public static class HttpContextExtensions
{
    public static Task WriteHealthReportResponse(HttpContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        var json = new JObject(
            new JProperty("status", result.Status.ToString()),
            new JProperty("results", new JObject(result.Entries.Select(pair =>
                new JProperty(pair.Key, new JObject(
                    new JProperty("status", pair.Value.Status.ToString()),
                    new JProperty("description", pair.Value.Description),
                    new JProperty("data", new JObject(pair.Value.Data
                        .Where(p => p.Value == null || p.Value.GetType().IsPrimitive || p.Value is string)
                        .Select(p => new JProperty(p.Key, p.Value))))))))));
        return httpContext.Response.WriteAsync(
            json.ToString((Newtonsoft.Json.Formatting)Formatting.Indented));
    }

    public static Task WritePrometheusHealthReport(HttpContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = MediaTypeNames.Text.Plain;
        var stringBuilder = new StringBuilder();
        foreach (var entry in result.Entries)
        {
            stringBuilder.Append(entry.Key.ToSnakeCase() + " " + (int)entry.Value.Status + "\n");
        }

        return httpContext.Response.WriteAsync(stringBuilder.ToString());
    }

    public static Guid GetUserId(this HttpContext httpContext)
    {
        if (!Guid.TryParse(GetClaimValue(httpContext, JwtRegisteredClaimNames.Sub), out Guid x))
        {
            return default;
        }

        return x;
    }

    public static Guid GetTenantId(this HttpContext httpContext)
    {
        if (!Guid.TryParse(GetClaimValue(httpContext, ClaimTypeConstants.TenantId), out Guid x))
        {
            return default;
        }

        return x;
    }

    public static Guid GetDefaultBusinessId(this HttpContext httpContext)
    {
        if (!Guid.TryParse(GetClaimValue(httpContext, ClaimTypeConstants.DefaultBusinessId), out Guid x))
        {
            return default;
        }

        return x;
    }

    public static string GetUserFirstName(this HttpContext httpContext)
    {
        var firstName = GetClaimValue(httpContext, ClaimTypeConstants.FirstName);

        if (string.IsNullOrEmpty(firstName))
        {
            throw new Exception("Token did not contain First Name.");
        }

        return firstName;
    }


    public static bool IsAdmin(this HttpContext context)
    {
        var roles = GetClaimValues(context, ClaimTypes.Role);

        return roles.Contains(AccountRoles.AdminUserRole.ToString());
    }

    private static List<string> GetClaimValues(HttpContext context, string ClaimType)
    {
        context.Request.Headers.TryGetValue("Authorization", out var accessToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            return [];
        }

        accessToken = accessToken.ToString().Replace("Bearer", "").Trim();

        if (string.IsNullOrEmpty(accessToken))
        {
            return [];
        }

        JwtSecurityToken jwt = new JwtSecurityToken(accessToken);

        return jwt.Claims.Where(c => c.Type == ClaimType).Select(c => c.Value).ToList();
    }

    private static string? GetClaimValue(HttpContext context, string ClaimType)
    {
        context.Request.Headers.TryGetValue("Authorization", out var accessToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        accessToken = accessToken.ToString().Replace("Bearer", "").Trim();

        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        JwtSecurityToken jwt = new JwtSecurityToken(accessToken);

        return jwt.Claims.First(c => c.Type == ClaimType).Value;
    }
}
