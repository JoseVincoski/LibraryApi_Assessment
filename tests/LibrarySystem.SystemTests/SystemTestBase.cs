using LibrarySystem.SystemTests.Testing;

namespace LibrarySystem.SystemTests;

[Collection(PostgresCollection.Name)]
public abstract class SystemTestBase(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private ServiceTestFactory _serviceFactory = null!;
    private ApiTestFactory _apiFactory = null!;

    protected PostgresContainerFixture Postgres { get; } = postgres;
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Postgres.ResetAsync();

        _serviceFactory = new ServiceTestFactory(Postgres.ConnectionString);
        _apiFactory = new ApiTestFactory(_serviceFactory);
        Client = _apiFactory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        _apiFactory.Dispose();
        _serviceFactory.Dispose();
        return Task.CompletedTask;
    }
}
