using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Application.Account.Models;
using Api.Product.Accounting.Models;
using Api.Tests.Infrastructure;
using Libs.Domain;

namespace Api.Tests.IntegrationTests;

/// <summary>
/// Integration tests for AccountingPeriod endpoints
/// Tests period generation, retrieval, close, and reopen operations (admin only)
/// Tests run sequentially due to shared database state and period status dependencies
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AccountingPeriodIntegrationTest : BaseIntegrationTest
{
    private string _accessToken = null!;

    [TestInitialize]
    public override void Initialize()
    {
        base.Initialize();

        // Authenticate before each test to get access token
        // Demo user has AdminUserRole which is required for accounting period endpoints
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

    private async Task EnsureAccountingConfigExistsAsync()
    {
        // Check if config exists
        var getResponse = await Client.GetAsync("/v1/accounting-config");

        var text = getResponse.Content.ReadAsStringAsync();

        if (getResponse.StatusCode == HttpStatusCode.OK)
        {
            var existingConfig = await getResponse.Content.ReadFromJsonAsync<AccountingConfigModel>(JsonOptions);
            if (existingConfig != null)
            {
                return; // Config already exists
            }
        }

        // Create a config if it doesn't exist
        var request = new UpsertAccountingConfigRequest
        {
            FiscalYearStartMonth = 1, // January
            PeriodFrequency = PeriodFrequency.Monthly,
            BaseYear = 2024
        };

        await Client.PostAsJsonAsync("/v1/accounting-config", request, JsonOptions);
    }

    [TestMethod]
    public async Task Generate_WithAccountingConfig_ReturnsCreatedAndPeriods()
    {
        // Arrange - Ensure accounting config exists
        await EnsureAccountingConfigExistsAsync();

        // Act
        var response = await Client.PostAsync("/v1/accounting-period/generate", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var periods = await response.Content.ReadFromJsonAsync<List<AccountingPeriodModel>>(JsonOptions);
        Assert.IsNotNull(periods);
        Assert.IsNotEmpty(periods);

        // Verify period structure
        var firstPeriod = periods.First();
        Assert.IsFalse(string.IsNullOrEmpty(firstPeriod.PeriodName));
        Assert.IsTrue(firstPeriod.PeriodStart < firstPeriod.PeriodEnd);
    }

    [TestMethod]
    public async Task GetAll_AfterGeneration_ReturnsPeriods()
    {
        // Arrange - Ensure periods exist
        await EnsureAccountingConfigExistsAsync();
        await Client.PostAsync("/v1/accounting-period/generate", null);

        // Act
        var response = await Client.GetAsync("/v1/accounting-period");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var periods = await response.Content.ReadFromJsonAsync<List<AccountingPeriodModel>>(JsonOptions);
        Assert.IsNotNull(periods);
        Assert.IsNotEmpty(periods);
    }

    [TestMethod]
    public async Task Close_WithValidPeriodId_ReturnsClosedPeriod()
    {
        // Arrange - Ensure periods exist and get one
        await EnsureAccountingConfigExistsAsync();
        await Client.PostAsync("/v1/accounting-period/generate", null);

        var getAllResponse = await Client.GetAsync("/v1/accounting-period");
        var periods = await getAllResponse.Content.ReadFromJsonAsync<List<AccountingPeriodModel>>(JsonOptions);
        var periodToClose = periods!.First(p => p.Status == AccountingPeriodStatus.Open);

        // Act
        var response = await Client.PostAsync($"/v1/accounting-period/{periodToClose.Id}/close", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var closedPeriod = await response.Content.ReadFromJsonAsync<AccountingPeriodModel>(JsonOptions);
        Assert.IsNotNull(closedPeriod);
        Assert.AreEqual(AccountingPeriodStatus.Closed, closedPeriod.Status);
        Assert.IsNotNull(closedPeriod.ClosedAt);
        Assert.IsNotNull(closedPeriod.ClosedBy);
        Assert.IsFalse(string.IsNullOrEmpty(closedPeriod.ClosedByName));
    }

    [TestMethod]
    public async Task Close_WithInvalidPeriodId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/v1/accounting-period/{invalidId}/close", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Reopen_WithClosedPeriod_ReturnsReopenedPeriod()
    {
        // Arrange - Ensure periods exist, close one
        await EnsureAccountingConfigExistsAsync();
        await Client.PostAsync("/v1/accounting-period/generate", null);

        var getAllResponse = await Client.GetAsync("/v1/accounting-period");
        var periods = await getAllResponse.Content.ReadFromJsonAsync<List<AccountingPeriodModel>>(JsonOptions);
        var periodToReopen = periods!.First(p => p.Status == AccountingPeriodStatus.Open);

        // Close the period first
        await Client.PostAsync($"/v1/accounting-period/{periodToReopen.Id}/close", null);

        // Act - Reopen it
        var response = await Client.PostAsync($"/v1/accounting-period/{periodToReopen.Id}/reopen", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var reopenedPeriod = await response.Content.ReadFromJsonAsync<AccountingPeriodModel>(JsonOptions);
        Assert.IsNotNull(reopenedPeriod);
        Assert.AreEqual(AccountingPeriodStatus.Open, reopenedPeriod.Status);
        Assert.IsNull(reopenedPeriod.ClosedAt);
        Assert.IsNull(reopenedPeriod.ClosedBy);
    }

    [TestMethod]
    public async Task Reopen_WithInvalidPeriodId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/v1/accounting-period/{invalidId}/reopen", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Generate_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsync("/v1/accounting-period/generate", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync("/v1/accounting-period");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Close_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;
        var periodId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/v1/accounting-period/{periodId}/close", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Reopen_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authorization header
        Client.DefaultRequestHeaders.Authorization = null;
        var periodId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/v1/accounting-period/{periodId}/reopen", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CloseAndReopen_Workflow_MaintainsPeriodData()
    {
        // Arrange - Ensure periods exist
        await EnsureAccountingConfigExistsAsync();
        await Client.PostAsync("/v1/accounting-period/generate", null);

        var getAllResponse = await Client.GetAsync("/v1/accounting-period");
        var periods = await getAllResponse.Content.ReadFromJsonAsync<List<AccountingPeriodModel>>(JsonOptions);
        var originalPeriod = periods!.First(p => p.Status == AccountingPeriodStatus.Open);

        // Act - Close the period
        var closeResponse = await Client.PostAsync($"/v1/accounting-period/{originalPeriod.Id}/close", null);
        var closedPeriod = await closeResponse.Content.ReadFromJsonAsync<AccountingPeriodModel>(JsonOptions);

        // Act - Reopen the period
        var reopenResponse = await Client.PostAsync($"/v1/accounting-period/{originalPeriod.Id}/reopen", null);
        var reopenedPeriod = await reopenResponse.Content.ReadFromJsonAsync<AccountingPeriodModel>(JsonOptions);

        // Assert - Verify period data is maintained
        Assert.AreEqual(originalPeriod.Id, closedPeriod!.Id);
        Assert.AreEqual(originalPeriod.Id, reopenedPeriod!.Id);
        Assert.AreEqual(originalPeriod.PeriodName, reopenedPeriod.PeriodName);
        Assert.AreEqual(originalPeriod.PeriodStart, reopenedPeriod.PeriodStart);
        Assert.AreEqual(originalPeriod.PeriodEnd, reopenedPeriod.PeriodEnd);

        // Assert - Status changes correctly
        Assert.AreEqual(AccountingPeriodStatus.Closed, closedPeriod.Status);
        Assert.AreEqual(AccountingPeriodStatus.Open, reopenedPeriod.Status);
    }
}
