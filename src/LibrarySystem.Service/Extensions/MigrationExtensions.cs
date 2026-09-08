using LibrarySystem.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LibrarySystem.Service.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LibraryDbContext>>();
        var seedSettings = configuration.GetSection("SeedSettings").Get<SeedSettings>() ?? null;

        try
        {
            logger.LogInformation("Starting database migration process...");
            context.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");

            SeedData(context, logger, seedSettings);
        }
        catch (Exception ex) 
        {
            logger.LogCritical(ex, "A fatal error occurred while migrating the database: {Message}", ex.Message);
            throw;
        }
    }
    private static void SeedData(LibraryDbContext context, ILogger logger, SeedSettings? seedSettings)
    {
        if (seedSettings is null)
        {
            logger.LogWarning("No SeedSettings configuration section found - skipping database seeding.");
            return;
        }

        logger.LogInformation("Starting database seeding process...");
        DatabaseSeeder.SeedData(context, logger, seedSettings);
        logger.LogInformation("Seeding completed successfully.");
    }
}