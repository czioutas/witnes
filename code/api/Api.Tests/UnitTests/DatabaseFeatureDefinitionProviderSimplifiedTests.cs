using Api.Data;
using Api.Application.Features.Entities;
using Api.Application.Features.Services;
using Api.Application.Tenancy.Services;
using Libs.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Tests.UnitTests;

/// <summary>
/// Simplified tests for DatabaseFeatureDefinitionProvider that focus on database logic
/// without complex cache mocking since GetOrSetAsync is an extension method.
/// </summary>
[TestClass]
public class DatabaseFeatureDefinitionProviderSimplifiedTests
{
    private ApplicationDbContext _context = null!;
    private IFusionCache _cache = null!;
    private Mock<TimeProvider> _mockTimeProvider = null!;
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor = null!;
    private IServiceProvider _serviceProvider = null!;
    private DatabaseFeatureDefinitionProvider _provider = null!;

    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DropzoneFeatureId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset TestDateTime = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var requestTenant = new RequestTenant();
        requestTenant.SetTenantId(TestTenantId);

        var requestBusiness = new Api.Application.Business.Services.RequestBusiness();
        requestBusiness.SetDefaultBusinessId(TestBusinessId);

        _mockTimeProvider = new Mock<TimeProvider>();
        _mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(TestDateTime);

        _context = new ApplicationDbContext(
            requestTenant,
            requestBusiness,
            options,
            _mockTimeProvider.Object);

        // Use real FusionCache with in-memory backing
        _cache = new FusionCache(new FusionCacheOptions());

        // Setup service provider
        var services = new ServiceCollection();
        services.AddScoped<ApplicationDbContext>(_ => _context);
        services.AddScoped<IRequestTenant>(_ => requestTenant);
        _serviceProvider = services.BuildServiceProvider();

        // Setup HttpContextAccessor to return a mock HttpContext with the service provider
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);

        _provider = new DatabaseFeatureDefinitionProvider(
            _mockHttpContextAccessor.Object,
            _cache,
            _mockTimeProvider.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            _context?.Database.EnsureDeleted();
        }
        catch (ObjectDisposedException)
        {
            // Context already disposed by service provider
        }
        _cache?.Dispose();
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenFeatureDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenFeatureIsGlobalAndEnabledByDefault_ReturnsFeatureDefinition()
    {
        // Arrange
        SeedGlobalFeature(isEnabledByDefault: true);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(nameof(FeatureKey.Dropzone), result.Name);
        Assert.AreEqual(1, result.EnabledFor.Count());
        Assert.AreEqual("AlwaysOn", result.EnabledFor.First().Name);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenFeatureIsGlobalButNotEnabledByDefault_ReturnsNull()
    {
        // Arrange
        SeedGlobalFeature(isEnabledByDefault: false);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenTenantFeatureIsEnabled_ReturnsFeatureDefinition()
    {
        // Arrange
        SeedTenantFeature(isEnabled: true, enabledFrom: null, enabledUntil: null);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(nameof(FeatureKey.Dropzone), result.Name);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenTenantFeatureIsDisabled_ReturnsNull()
    {
        // Arrange
        SeedTenantFeature(isEnabled: false, enabledFrom: null, enabledUntil: null);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenFeatureNotYetActive_ReturnsNull()
    {
        // Arrange - Feature starts in the future
        var futureDate = new DateOnly(2025, 7, 1);
        SeedTenantFeature(isEnabled: true, enabledFrom: futureDate, enabledUntil: null);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenFeatureExpired_ReturnsNull()
    {
        // Arrange - Feature ended in the past
        var pastDate = new DateOnly(2025, 6, 1);
        SeedTenantFeature(isEnabled: true, enabledFrom: null, enabledUntil: pastDate);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenFeatureWithinDateRange_ReturnsFeatureDefinition()
    {
        // Arrange - Current time: 2025-06-15, Feature: 2025-06-01 to 2025-06-30
        var startDate = new DateOnly(2025, 6, 1);
        var endDate = new DateOnly(2025, 6, 30);
        SeedTenantFeature(isEnabled: true, enabledFrom: startDate, enabledUntil: endDate);

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(nameof(FeatureKey.Dropzone));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(nameof(FeatureKey.Dropzone), result.Name);
    }

    [TestMethod]
    public async Task GetFeatureDefinitionAsync_WhenInvalidFeatureName_ReturnsNull()
    {
        // Arrange
        var invalidFeatureName = "InvalidFeatureName123";

        // Act
        var result = await _provider.GetFeatureDefinitionAsync(invalidFeatureName);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAllFeatureDefinitionsAsync_WhenNoFeaturesEnabled_ReturnsEmpty()
    {
        // Act
        var results = new List<FeatureDefinition>();
        await foreach (var feature in _provider.GetAllFeatureDefinitionsAsync())
        {
            results.Add(feature);
        }

        // Assert
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task GetAllFeatureDefinitionsAsync_WhenGlobalFeatureEnabled_ReturnsFeature()
    {
        // Arrange
        SeedGlobalFeature(isEnabledByDefault: true);

        // Act
        var results = new List<FeatureDefinition>();
        await foreach (var feature in _provider.GetAllFeatureDefinitionsAsync())
        {
            results.Add(feature);
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(nameof(FeatureKey.Dropzone), results[0].Name);
    }

    [TestMethod]
    public async Task GetAllFeatureDefinitionsAsync_WhenTenantFeatureEnabled_ReturnsFeature()
    {
        // Arrange
        SeedTenantFeature(isEnabled: true, enabledFrom: null, enabledUntil: null);

        // Act
        var results = new List<FeatureDefinition>();
        await foreach (var feature in _provider.GetAllFeatureDefinitionsAsync())
        {
            results.Add(feature);
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(nameof(FeatureKey.Dropzone), results[0].Name);
    }

    private void SeedGlobalFeature(bool isEnabledByDefault)
    {
        var feature = new FeatureEntity
        {
            Id = DropzoneFeatureId,
            Key = FeatureKey.Dropzone,
            Name = "DropZone",
            Description = "AI-powered document processing",
            IsGlobal = true,
            IsEnabledByDefault = isEnabledByDefault,
            CreatedAt = TestDateTime,
            UpdatedAt = TestDateTime
        };

        _context.Features.Add(feature);
        _context.SaveChanges();
    }

    private void SeedTenantFeature(bool isEnabled, DateOnly? enabledFrom, DateOnly? enabledUntil)
    {
        var feature = new FeatureEntity
        {
            Id = DropzoneFeatureId,
            Key = FeatureKey.Dropzone,
            Name = "DropZone",
            Description = "AI-powered document processing",
            IsGlobal = false,
            IsEnabledByDefault = false,
            CreatedAt = TestDateTime,
            UpdatedAt = TestDateTime
        };

        _context.Features.Add(feature);
        _context.SaveChanges();

        var tenantFeature = new TenantFeatureEntity
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            FeatureId = DropzoneFeatureId,
            IsEnabled = isEnabled,
            EnabledFrom = enabledFrom,
            EnabledUntil = enabledUntil,
            CreatedAt = TestDateTime,
            UpdatedAt = TestDateTime
        };

        _context.TenantFeatures.Add(tenantFeature);
        _context.SaveChanges();
    }
}
