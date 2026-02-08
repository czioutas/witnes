namespace Api.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests using in-memory database
/// Fast, isolated tests that don't require external infrastructure
/// </summary>
[TestClass]
public abstract class InMemoryIntegrationTest
{
    protected HttpClient Client { get; private set; } = null!;
    protected InMemoryTestApplicationFactory Factory { get; private set; } = null!;

    [TestInitialize]
    public virtual void Initialize()
    {
        Factory = new InMemoryTestApplicationFactory();
        Client = Factory.CreateClient();
    }

    [TestCleanup]
    public virtual void Cleanup()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }
}
