using System.Net;
using System.Net.Http.Json;
using Api.Application.Account.Models;
using Api.Tests.Infrastructure;
using Libs.Domain;

namespace Api.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Account endpoints
///
/// Current setup: InMemoryIntegrationTest - Fast tests with in-memory database
/// Alternative: BaseIntegrationTest - Full infrastructure tests (requires docker-compose)
/// </summary>
[TestClass]
public sealed class AccountIntegrationTest : BaseIntegrationTest
{
    [TestMethod]
    public async Task Register_WithUniqueInformation_ReturnsSuccessAndToken()
    {
        // Arrange
        var registerModel = new RegisterModel
        {
            Email = "newuser@example.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "SecurePass123!",
            NormalizedTenantIdentifier = "unique-tenant-identifier",
            BusinessType = BusinessType.Bakery
        };

        // Act
        var response = await PostAsJsonAsync("/v1/account/register", registerModel);

        var text = response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var tokenModel = await response.Content.ReadFromJsonAsync<TokenModel>(JsonOptions);
        Assert.IsNotNull(tokenModel);
        Assert.IsFalse(string.IsNullOrEmpty(tokenModel.AccessToken));
        Assert.IsFalse(string.IsNullOrEmpty(tokenModel.RefreshToken));
    }

    [TestMethod]
    public async Task Register_WithExistingTenantIdentifier_ReturnsError()
    {
        // Arrange - First, create a user with a specific tenant
        var firstRegisterModel = new RegisterModel
        {
            Email = "firstuser@example.com",
            FirstName = "First",
            LastName = "User",
            Password = "SecurePass123!",
            NormalizedTenantIdentifier = "existing-tenant",
            BusinessType = BusinessType.Other
        };

        await PostAsJsonAsync("/v1/Account/Register", firstRegisterModel);

        // Now try to register another user with the same tenant identifier
        var secondRegisterModel = new RegisterModel
        {
            Email = "seconduser@example.com",
            FirstName = "Second",
            LastName = "User",
            Password = "SecurePass123!",
            NormalizedTenantIdentifier = "existing-tenant",
            BusinessType = BusinessType.Other
        };

        // Act
        var response = await PostAsJsonAsync("/v1/Account/Register", secondRegisterModel);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndToken()
    {
        // Arrange
        var loginModel = new LoginModel
        {
            Email = DemoUserEmail,
            Password = DemoUserPassword
        };

        // Act
        var response = await PostAsJsonAsync("/v1/account/login", loginModel);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var tokenModel = await response.Content.ReadFromJsonAsync<TokenModel>(JsonOptions);
        Assert.IsNotNull(tokenModel);
        Assert.IsFalse(string.IsNullOrEmpty(tokenModel.AccessToken));
        Assert.IsFalse(string.IsNullOrEmpty(tokenModel.RefreshToken));
    }

    [TestMethod]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginModel = new LoginModel
        {
            Email = DemoUserEmail,
            Password = "WrongPassword123!"
        };

        // Act
        var response = await PostAsJsonAsync("/v1/account/login", loginModel);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginModel = new LoginModel
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await PostAsJsonAsync("/v1/account/login", loginModel);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
