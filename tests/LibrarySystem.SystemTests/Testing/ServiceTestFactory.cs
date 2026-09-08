using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LibrarySystem.SystemTests.Testing;

public sealed class ServiceTestFactory(string connectionString) : WebApplicationFactory<IServiceMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" isn't Development, so Program.cs skips ApplyMigrations(). This avoids reseeding ~25k rows of Bogus data on every test.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
    }
}
