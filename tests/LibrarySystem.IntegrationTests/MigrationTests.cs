using LibrarySystem.IntegrationTests.Testing;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibrarySystem.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Migrate_CreatesAllExpectedTables()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var tables = await QueryStringColumnAsync(
            connection,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'");

        Assert.Contains("books", tables);
        Assert.Contains("readers", tables);
        Assert.Contains("loans", tables);
    }

    [Fact]
    public async Task Migrate_CreatesThePartialUniqueIndexOnActiveLoans()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var indexDefinitions = await QueryStringColumnAsync(
            connection,
            $"SELECT indexdef FROM pg_indexes WHERE indexname = '{LoanConfiguration.ActiveLoanPerBookIndexName}'");

        var indexDefinition = Assert.Single(indexDefinitions);
        Assert.Contains("UNIQUE", indexDefinition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", indexDefinition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"ReturnedAtUtc\" IS NULL", indexDefinition);
    }

    [Fact]
    public async Task Migrate_CreatesAUniqueIndexOnReaderEmail()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var indexDefinitions = await QueryStringColumnAsync(
            connection,
            "SELECT indexdef FROM pg_indexes WHERE tablename = 'readers' AND indexdef ILIKE '%Email%'");

        var indexDefinition = Assert.Single(indexDefinitions);
        Assert.Contains("UNIQUE", indexDefinition, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FreshlyMigratedDatabase_SupportsBasicCrudRoundTrip()
    {
        await using var context = fixture.CreateDbContext();

        var book = Book.Create(null, "Round Trip", "Author", 200).Value;
        context.Add(book);
        await context.SaveChangesAsync();

        await using var verifyContext = fixture.CreateDbContext();
        var persisted = await verifyContext.Set<Book>().SingleAsync(b => b.Id == book.Id);

        Assert.Equal("Round Trip", persisted.Title);
    }

    private static async Task<List<string>> QueryStringColumnAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
