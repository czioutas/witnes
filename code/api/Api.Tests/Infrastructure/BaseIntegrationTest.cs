using System.Net.Http.Json;
using System.Text.Json;
using Api.Application.Converters;

namespace Api.Tests.Infrastructure;

/// <summary>
/// Base class for full integration tests using real infrastructure (PostgreSQL, Redis, etc.)
/// Requires docker-compose to be running with all services
/// Use this for end-to-end integration tests
/// </summary>
[TestClass]
public abstract class BaseIntegrationTest
{
    // Demo user credentials from Seeds.cs - email confirmed and ready to use
    protected const string DemoUserEmail = "mamaslittlebakery@witnes.io";
    protected const string DemoUserPassword = "greenactionsDemoAa1!";

    protected HttpClient Client { get; private set; } = null!;
    protected TestApplicationFactory Factory { get; private set; } = null!;
    protected JsonSerializerOptions JsonOptions { get; private set; } = null!;

    [TestInitialize]
    public virtual void Initialize()
    {
        // Configure JSON serialization to match API (snake_case + enum member values)
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };

        // Add enum converter that respects [EnumMember] attributes
        JsonOptions.Converters.Add(new EnumMemberJsonConverterFactory());

        Client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:7072")
        };
    }

    [TestCleanup]
    public virtual void Cleanup()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }

    // Helper method for POST requests with snake_case JSON
    protected Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        return Client.PostAsJsonAsync(requestUri, value, JsonOptions);
    }
}
