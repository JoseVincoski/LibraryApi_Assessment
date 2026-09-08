using LibrarySystem.Service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LibrarySystem.Service.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<LibraryDbContext>();

        return services;
    }

    public static IEndpointConventionBuilder MapInfrastructureHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapHealthChecks("health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    Status = report.Status.ToString(),
                    Duration = report.TotalDuration,
                    Component = "Library.Service"
                };
                await context.Response.WriteAsJsonAsync(response);
            }
        });
    }
}