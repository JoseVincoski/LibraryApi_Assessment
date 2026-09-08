namespace LibrarySystem.Api.Extensions;

public static class GrpcCallDefaults
{
    public static DateTime Deadline => DateTime.UtcNow.AddSeconds(5);
}
