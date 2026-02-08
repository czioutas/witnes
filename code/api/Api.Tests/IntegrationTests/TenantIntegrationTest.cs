using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Application.Account.Models;
using Api.Application.Tenancy.Models;
using Api.Tests.Infrastructure;
using Libs.Domain;

namespace Api.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Tenant endpoints
/// Tests tenant information retrieval and update (admin only)
/// </summary>
[TestClass]
public sealed class TenantIntegrationTest : BaseIntegrationTest
{
    private string _accessToken = null!;

    [TestInitialize]
    public override void Initialize()
    {
        base.Initialize();

        // Authenticate before each test to get access token
        // Demo user has AdminUserRole which is required for tenant endpoints
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
    public async Task GetTenant_WithAuthentication_ReturnsCurrentTenant()
    {
        // Act
        var response = await Client.GetAsync("/v1/tenant");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var tenant = await response.Content.ReadFromJsonAsync<TenantModel>(JsonOptions);
        Assert.IsNotNull(tenant);
        Assert.IsFalse(string.IsNullOrEmpty(tenant.Identifier));
        // Demo user is in "Mama's Little Bakery" tenant
        Assert.AreEqual("Mama's Little Bakery", tenant.Identifier);
    }

    [TestMethod]
    public async Task GetTenant_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync("/v1/tenant");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTenant_WithValidData_ReturnsUpdatedTenant()
    {
        // Arrange
        var updateRequest = new UpdateTenantDetailsRequest
        {
            Identifier = "Mama's Little Bakery Updated",
            VatNumber = "DE123456789",
            CompanyRegistrationNumber = "HRB12345",
            BusinessType = BusinessType.Bakery,
            NumberOfEmployees = 25,
            StreetLine1 = "Hauptstraße 42",
            City = "Berlin",
            StateProvince = "Berlin",
            PostalCode = "10115",
            Country = "Germany"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/v1/tenant", updateRequest, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var updatedTenant = await response.Content.ReadFromJsonAsync<TenantModel>(JsonOptions);
        Assert.IsNotNull(updatedTenant);
        Assert.AreEqual(updateRequest.Identifier, updatedTenant.Identifier);
        Assert.AreEqual(updateRequest.VatNumber, updatedTenant.VatNumber);
        Assert.AreEqual(updateRequest.CompanyRegistrationNumber, updatedTenant.CompanyRegistrationNumber);
        Assert.AreEqual(updateRequest.BusinessType, updatedTenant.BusinessType);
        Assert.AreEqual(updateRequest.NumberOfEmployees, updatedTenant.NumberOfEmployees);
        Assert.AreEqual(updateRequest.City, updatedTenant.City);

        // Verify update persisted by fetching again
        var getResponse = await Client.GetAsync("/v1/tenant");
        var fetchedTenant = await getResponse.Content.ReadFromJsonAsync<TenantModel>(JsonOptions);
        Assert.AreEqual(updateRequest.Identifier, fetchedTenant!.Identifier);
    }

    [TestMethod]
    public async Task UpdateTenant_WithPartialData_ReturnsUpdatedTenant()
    {
        // Arrange - Only update a few fields
        var updateRequest = new UpdateTenantDetailsRequest
        {
            Identifier = "Mama's Little Bakery",
            VatNumber = "DE987654321",
            BusinessType = BusinessType.Bakery
        };

        // Act
        var response = await Client.PutAsJsonAsync("/v1/tenant", updateRequest, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var updatedTenant = await response.Content.ReadFromJsonAsync<TenantModel>(JsonOptions);
        Assert.IsNotNull(updatedTenant);
        Assert.AreEqual(updateRequest.Identifier, updatedTenant.Identifier);
        Assert.AreEqual(updateRequest.VatNumber, updatedTenant.VatNumber);
    }

    [TestMethod]
    public async Task UpdateTenant_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        var updateRequest = new UpdateTenantDetailsRequest
        {
            Identifier = "Unauthorized Update",
            BusinessType = BusinessType.Other
        };

        // Act
        var response = await Client.PutAsJsonAsync("/v1/tenant", updateRequest, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTenant_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange - Missing required field (Identifier)
        var invalidRequest = new
        {
            vat_number = "DE123456789",
            business_type = "bakery"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/v1/tenant", invalidRequest, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
