using LibrarySystem.IntegrationTests.Testing;
using LibrarySystem.Service.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibrarySystem.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class ReaderUniquenessTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TwoReaders_WithTheSameEmail_ViolatesTheUniqueIndex()
    {
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Add(Reader.Create(null, "First Reader", "duplicate@example.com").Value);
            await setup.SaveChangesAsync();
        }

        await using var context = fixture.CreateDbContext();
        context.Add(Reader.Create(null, "Second Reader", "duplicate@example.com").Value);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var pgException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pgException.SqlState);
    }

    [Fact]
    public async Task TwoReaders_WithDifferentEmails_BothPersist()
    {
        await using var context = fixture.CreateDbContext();
        context.AddRange(
            Reader.Create(null, "First Reader", "first@example.com").Value,
            Reader.Create(null, "Second Reader", "second@example.com").Value);

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Set<Reader>().CountAsync());
    }
}
