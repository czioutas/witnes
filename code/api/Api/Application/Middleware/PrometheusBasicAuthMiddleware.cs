using System.Net.Http.Headers;
using System.Text;

namespace Api.Application.Middleware;

public class PrometheusBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _username;
    private readonly string _password;

    public PrometheusBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _username = configuration["PrometheusSettings:Username"] ?? "prometheus";
        _password = configuration["PrometheusSettings:Password"] ?? "";
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(_password))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Prometheus Metrics\"";
            return;
        }

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]!);
            if (authHeader.Scheme != "Basic" || authHeader.Parameter == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Prometheus Metrics\"";
                return;
            }

            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter)).Split(':', 2);
            var username = credentials[0];
            var password = credentials[1];

            if (username != _username || password != _password)
            {
                context.Response.StatusCode = 401;
                context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Prometheus Metrics\"";
                return;
            }
        }
        catch
        {
            context.Response.StatusCode = 401;
            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Prometheus Metrics\"";
            return;
        }

        await _next(context);
    }
}
