using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LibrarySystem.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services, string healthCheckUrl)
    {
        services.AddHealthChecks()
            .AddUrlGroup(new Uri(healthCheckUrl), name: "Library.Service");

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
                    Info = report.Entries.Select(e => new
                    {
                        Key = e.Key,
                        Status = e.Value.Status.ToString(),
                        Duration = e.Value.Duration
                    })
                };
                await context.Response.WriteAsJsonAsync(response);
            }
        });
    }
}