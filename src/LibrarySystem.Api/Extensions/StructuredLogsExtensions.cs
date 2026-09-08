using Carter;
using Serilog;

namespace LibrarySystem.Api.Extensions;

public static class StructuredLogsExtensions
{
    public static void AddStructuredLogs(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();
        builder.Host.UseSerilog();
    }
}