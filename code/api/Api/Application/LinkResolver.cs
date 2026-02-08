using System.Web;
using Api.Application.Settings;
using Microsoft.Extensions.Options;

namespace Api.Application;

public interface ILinkResolver
{
    public string GetApiUrl(string url, params (string key, string value)[] parameters);
    public string GetApplicationUrl(string url, params (string key, string value)[] parameters);
}


public class LinkResolver : ILinkResolver
{
    private readonly ApplicationSettings _applicationSettings;

    public LinkResolver(IOptions<ApplicationSettings> applicationSettings)
    {
        if (applicationSettings == null)
        {
            throw new ArgumentNullException(nameof(applicationSettings));
        }

        _applicationSettings = applicationSettings.Value;
    }

    public string GetApiUrl(string url, params (string key, string value)[] parameters)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        string baseUrl = _applicationSettings.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;
        url = url.TrimStart('/');

        var uriBuilder = new UriBuilder($"{baseUrl}/{url}");

        if (parameters != null && parameters.Length > 0)
        {
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var (key, value) in parameters)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    query[key] = value;
                }
            }
            uriBuilder.Query = query.ToString();
        }

        return uriBuilder.ToString();
    }

    public string GetApplicationUrl(string url, params (string key, string value)[] parameters)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        string baseUrl = _applicationSettings.ApplicationBaseUrl?.TrimEnd('/') ?? string.Empty;
        url = url.TrimStart('/');

        var uriBuilder = new UriBuilder($"{baseUrl}/{url}");

        if (parameters != null && parameters.Length > 0)
        {
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var (key, value) in parameters)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    query[key] = value;
                }
            }
            uriBuilder.Query = query.ToString();
        }

        return uriBuilder.ToString();
    }
}
