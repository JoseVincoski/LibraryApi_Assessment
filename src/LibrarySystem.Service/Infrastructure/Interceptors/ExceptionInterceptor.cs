using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LibrarySystem.Service.Infrastructure.Interceptors;

public sealed class ExceptionInterceptor(ILogger<ExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred during gRPC execution: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, "An internal error occurred. Check logs for details."));
        }
    }
}