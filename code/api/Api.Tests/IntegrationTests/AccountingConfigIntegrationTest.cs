using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Application.Account.Models;
using Api.Product.Accounting.Models;
using Api.Tests.Infrastructure;
using Libs.Domain;

namespace Api.Tests.IntegrationTests;

/// <summary>
/// Integration tests for AccountingConfig endpoints
/// Tests upsert and retrieval of accounting configuration (admin only)
/// </summary>
[TestClass]
public sealed class AccountingConfigIntegrationTest : BaseIntegrationTest
{
    private string _accessToken = null!;

    [TestInitialize]
    public override void Initialize()
    {
        base.Initialize();

        // Authenticate before each test to get access token
        // Demo user has AdminUserRole which is required for accounting config endpoints
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
    public async Task UpsertAccountingConfig_WithValidData_ReturnsConfig()
    {
        // Arrange
        var request = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 1, // January
            PeriodFrequency = PeriodFrequency.Monthly,
            BaseYear = 2024
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var config = await response.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
        Assert.IsNotNull(config);
        Assert.AreEqual(request.FiscalYearStartMonth, config.FiscalYearStartMonth);
        Assert.AreEqual(request.PeriodFrequency, config.PeriodFrequency);
        Assert.AreEqual(request.BaseYear, config.BaseYear);
    }

    [TestMethod]
    public async Task GetAccountingConfig_AfterUpsert_ReturnsConfig()
    {
        // Arrange - First create a config
        var upsertRequest = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 7, // July
            PeriodFrequency = PeriodFrequency.Quarterly,
            BaseYear = 2023
        };

        await Client.PostAsJsonAsync("/v1/accounting-config", upsertRequest, JsonOptions);

        // Act - Get the config
        var response = await Client.GetAsync("/v1/accounting-config");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var config = await response.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
        Assert.IsNotNull(config);
        Assert.AreEqual(upsertRequest.FiscalYearStartMonth, config.FiscalYearStartMonth);
        Assert.AreEqual(upsertRequest.PeriodFrequency, config.PeriodFrequency);
        Assert.AreEqual(upsertRequest.BaseYear, config.BaseYear);
    }

    [TestMethod]
    public async Task UpsertAccountingConfig_UpdatesExisting_ReturnsUpdatedConfig()
    {
        // Arrange - First create a config
        var initialRequest = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 1,
            PeriodFrequency = PeriodFrequency.Monthly,
            BaseYear = 2023
        };

        await Client.PostAsJsonAsync("/v1/accounting-config", initialRequest, JsonOptions);

        // Now update it
        var updateRequest = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 4, // April
            PeriodFrequency = PeriodFrequency.Yearly,
            BaseYear = 2024
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/accounting-config", updateRequest, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var updatedConfig = await response.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
        Assert.IsNotNull(updatedConfig);
        Assert.AreEqual(updateRequest.FiscalYearStartMonth, updatedConfig.FiscalYearStartMonth);
        Assert.AreEqual(updateRequest.PeriodFrequency, updatedConfig.PeriodFrequency);
        Assert.AreEqual(updateRequest.BaseYear, updatedConfig.BaseYear);

        // Verify it persisted
        var getResponse = await Client.GetAsync("/v1/accounting-config");
        var fetchedConfig = await getResponse.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
        Assert.AreEqual(updateRequest.FiscalYearStartMonth, fetchedConfig!.FiscalYearStartMonth);
    }

    [TestMethod]
    public async Task UpsertAccountingConfig_WithMinimalData_ReturnsConfig()
    {
        // Arrange - Only required field
        var request = new UpsertAccountingConfigRequest
        {
            PeriodFrequency = PeriodFrequency.Quarterly
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var config = await response.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
        Assert.IsNotNull(config);
        Assert.AreEqual(request.PeriodFrequency, config.PeriodFrequency);
        Assert.IsNull(config.FiscalYearStartMonth);
        Assert.IsNull(config.BaseYear);
    }

    [TestMethod]
    public async Task UpsertAccountingConfig_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange - Invalid month (must be 1-12)
        var request = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 13, // Invalid
            PeriodFrequency = PeriodFrequency.Monthly,
            BaseYear = 2024
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UpsertAccountingConfig_WithInvalidYear_ReturnsBadRequest()
    {
        // Arrange - Invalid year (must be 2000-2100)
        var request = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 1,
            PeriodFrequency = PeriodFrequency.Monthly,
            BaseYear = 1999 // Invalid
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAccountingConfig_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync("/v1/accounting-config");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpsertAccountingConfig_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        var request = new UpsertAccountingConfigRequest
        {
            PeriodFrequency = PeriodFrequency.Monthly
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpsertAccountingConfig_WithAllPeriodFrequencies_Succeeds()
    {
        // Test all enum values work correctly
        var frequencies = new[] { PeriodFrequency.Monthly, PeriodFrequency.Quarterly, PeriodFrequency.Yearly };

        foreach (var frequency in frequencies)
        {
            // Arrange
            var request = new UpsertAccountingConfigRequest
            {
                PeriodFrequency = frequency,
                BaseYear = 2024
            };

            // Act
            var response = await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Failed for {frequency}");

            var config = await response.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
            Assert.AreEqual(frequency, config!.PeriodFrequency);
        }
    }
}
