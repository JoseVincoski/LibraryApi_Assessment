using System.Runtime.CompilerServices;
using LibrarySystem.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.FunctionalTests.Testing;

internal static class InMemoryDbContextFactory
{
    public static LibraryDbContext Create([CallerMemberName] string? name = null)
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase($"{name}-{Guid.NewGuid()}")
            .Options;

        return new LibraryDbContext(options);
    }
}
