using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Application.Account.Models;
using Api.Product.Location.Models;
using Api.Tests.Infrastructure;
using Libs.Domain;

namespace Api.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Location endpoints
/// Tests CRUD operations for locations with authentication
/// </summary>
[TestClass]
public sealed class LocationIntegrationTest : BaseIntegrationTest
{
    private string _accessToken = null!;

    [TestInitialize]
    public override void Initialize()
    {
        base.Initialize();

        // Authenticate before each test to get access token
        _accessToken = AuthenticateAsync().GetAwaiter().GetResult();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    private async Task<string> AuthenticateAsync()
    {
        var loginModel = new LoginModel
        {
            Email = DemoUserEmail,
            Password = DemoUserPassword
        };

        var response = await PostAsJsonAsync("/v1/account/login", loginModel);
        var tokenModel = await response.Content.ReadFromJsonAsync<TokenModel>(JsonOptions);

        return tokenModel!.AccessToken;
    }

    [TestMethod]
    public async Task LocationCrudFlow_FullLifecycle_Success()
    {
        // 1. Create
        var createRequest = new CreateLocationRequest
        {
            Name = "Test Location",
            HasSales = true,
            Address = new CreateAddressRequest
            {
                StreetLine1 = "123 Test Street",
                City = "Berlin",
                StateProvince = "Berlin",
                PostalCode = "10115",
                TerritoryCode = TerritoryCode.DE
            }
        };

        var createResponse = await PostAsJsonAsync("/v1/location", createRequest);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var createdLocation = await createResponse.Content.ReadFromJsonAsync<LocationModel>(JsonOptions);
        Assert.IsNotNull(createdLocation);
        Assert.AreEqual(createRequest.Name, createdLocation.Name);
        Assert.AreEqual(createRequest.HasSales, createdLocation.HasSales);
        Assert.IsNotNull(createdLocation.Address);
        Assert.AreEqual(createRequest.Address.City, createdLocation.Address.City);

        var locationId = createdLocation.Id;

        // 2. List all locations
        var listResponse = await Client.GetAsync("/v1/location");
        Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);

        var locations = await listResponse.Content.ReadFromJsonAsync<List<LocationModel>>(JsonOptions);
        Assert.IsNotNull(locations);
        Assert.IsNotEmpty(locations);
        Assert.IsTrue(locations.Any(l => l.Id == locationId));

        // 3. Get single location
        var getResponse = await Client.GetAsync($"/v1/location/{locationId}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedLocation = await getResponse.Content.ReadFromJsonAsync<LocationModel>(JsonOptions);
        Assert.IsNotNull(fetchedLocation);
        Assert.AreEqual(locationId, fetchedLocation.Id);
        Assert.AreEqual(createRequest.Name, fetchedLocation.Name);

        // 4. Update
        var updateRequest = new UpdateLocationRequest
        {
            Name = "Updated Location",
            HasSales = false,
            Address = new UpdateAddressRequest
            {
                StreetLine1 = "789 Updated Street",
                City = "Hamburg",
                PostalCode = "20095",
                TerritoryCode = TerritoryCode.DE
            }
        };

        var updateResponse = await Client.PutAsJsonAsync($"/v1/location/{locationId}", updateRequest, JsonOptions);
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedLocation = await updateResponse.Content.ReadFromJsonAsync<LocationModel>(JsonOptions);
        Assert.IsNotNull(updatedLocation);
        Assert.AreEqual(updateRequest.Name, updatedLocation.Name);
        Assert.AreEqual(updateRequest.HasSales, updatedLocation.HasSales);

        // 5. Delete
        var deleteResponse = await Client.DeleteAsync($"/v1/location/{locationId}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 6. Verify 404 after delete
        var notFoundResponse = await Client.GetAsync($"/v1/location/{locationId}");
        Assert.AreEqual(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

        // 7. Verify 404 for non-existent location
        var invalidId = Guid.NewGuid();
        var invalidResponse = await Client.GetAsync($"/v1/location/{invalidId}");
        Assert.AreEqual(HttpStatusCode.NotFound, invalidResponse.StatusCode);
    }

    [TestMethod]
    public async Task CreateLocation_WithoutAuthentication_Returns401()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        var request = new CreateLocationRequest
        {
            Name = "Unauthorized Location",
            HasSales = false,
            Address = new CreateAddressRequest
            {
                StreetLine1 = "123 Unauthorized Street",
                City = "Berlin",
                PostalCode = "10115",
                TerritoryCode = TerritoryCode.DE
            }
        };

        // Act
        var response = await PostAsJsonAsync("/v1/location", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
