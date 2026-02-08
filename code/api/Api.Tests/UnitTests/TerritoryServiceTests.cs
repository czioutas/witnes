using Api.Data;
using Api.Product.Territories;
using Libs.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Api.Tests.UnitTests;

[TestClass]
public class TerritoryServiceTests
{
    private ApplicationDbContext _context = null!;
    private TerritoryService _service = null!;

    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var requestTenant = new Api.Application.Tenancy.Services.RequestTenant();
        requestTenant.SetTenantId(TestTenantId);

        _context = new ApplicationDbContext(
            requestTenant,
            new Mock<Api.Application.Business.Services.IRequestBusiness>().Object,
            options,
            TimeProvider.System);

        _service = new TerritoryService(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetTerritoryHierarchyAsync_FromCountry_ReturnsFullHierarchy()
    {
        // Arrange
        var world = new TerritoryEntity { Code = TerritoryCode.WORLD, Name = "World", ParentCode = null };
        var europe = new TerritoryEntity { Code = TerritoryCode.Europe, Name = "Europe", ParentCode = TerritoryCode.WORLD };
        var germany = new TerritoryEntity { Code = TerritoryCode.DE, Name = "Germany", ParentCode = TerritoryCode.Europe };

        _context.Territories.AddRange(world, europe, germany);
        await _context.SaveChangesAsync();

        var expectedHierarchy = new List<TerritoryCode> { TerritoryCode.DE, TerritoryCode.Europe, TerritoryCode.WORLD };

        // Act
        var result = await _service.GetTerritoryHierarchyAsync(TerritoryCode.DE);

        // Assert
        CollectionAssert.AreEquivalent(expectedHierarchy, result);
        Assert.AreEqual(expectedHierarchy[0], result[0]);
        Assert.AreEqual(expectedHierarchy[1], result[1]);
        Assert.AreEqual(expectedHierarchy[2], result[2]);
    }

    [TestMethod]
    public async Task GetTerritoryHierarchyAsync_FromContinent_ReturnsPartialHierarchy()
    {
        // Arrange
        var world = new TerritoryEntity { Code = TerritoryCode.WORLD, Name = "World", ParentCode = null };
        var europe = new TerritoryEntity { Code = TerritoryCode.Europe, Name = "Europe", ParentCode = TerritoryCode.WORLD };

        _context.Territories.AddRange(world, europe);
        await _context.SaveChangesAsync();

        var expectedHierarchy = new List<TerritoryCode> { TerritoryCode.Europe, TerritoryCode.WORLD };

        // Act
        var result = await _service.GetTerritoryHierarchyAsync(TerritoryCode.Europe);

        // Assert
        CollectionAssert.AreEquivalent(expectedHierarchy, result);
        Assert.AreEqual(expectedHierarchy[0], result[0]);
        Assert.AreEqual(expectedHierarchy[1], result[1]);
    }

    [TestMethod]
    public async Task GetTerritoryHierarchyAsync_FromWorld_ReturnsOnlyWorld()
    {
        // Arrange
        var world = new TerritoryEntity { Code = TerritoryCode.WORLD, Name = "World", ParentCode = null };

        _context.Territories.Add(world);
        await _context.SaveChangesAsync();

        var expectedHierarchy = new List<TerritoryCode> { TerritoryCode.WORLD };

        // Act
        var result = await _service.GetTerritoryHierarchyAsync(TerritoryCode.WORLD);

        // Assert
        CollectionAssert.AreEquivalent(expectedHierarchy, result);
        Assert.AreEqual(expectedHierarchy[0], result[0]);
    }

    [TestMethod]
    public async Task GetTerritoryHierarchyAsync_NonExistentTerritory_ReturnsPartialHierarchyUpToUnknown()
    {
        // Arrange - No territories in DB for the requested code or its parents
        // This effectively tests the loop termination when no territory is found.
        var world = new TerritoryEntity { Code = TerritoryCode.WORLD, Name = "World", ParentCode = null };
        _context.Territories.Add(world);
        await _context.SaveChangesAsync();

        var expectedHierarchy = new List<TerritoryCode> { (TerritoryCode)9999, TerritoryCode.WORLD }; // Assuming 9999 is a non-existent code

        // Act
        var result = await _service.GetTerritoryHierarchyAsync((TerritoryCode)9999); // Use a non-existent code

        // Assert
        Assert.HasCount(1, result); // Should only contain the non-existent code if no parent found
        Assert.AreEqual((TerritoryCode)9999, result[0]);
    }
}
