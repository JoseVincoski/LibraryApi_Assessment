namespace LibrarySystem.Api.Extensions;

public static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        return configuration[key]
            ?? throw new InvalidOperationException($"The required configuration key '{key}' is missing in appsettings.");
    }
}