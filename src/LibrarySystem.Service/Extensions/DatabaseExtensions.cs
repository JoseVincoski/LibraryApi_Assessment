using Microsoft.EntityFrameworkCore;
using LibrarySystem.Service.Infrastructure.Persistence;

namespace LibrarySystem.Service.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The database connection string is not configured.");

        services.AddDbContext<LibraryDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}