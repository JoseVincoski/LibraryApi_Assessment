using Serilog;

namespace LibrarySystem.Service.Extensions;

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