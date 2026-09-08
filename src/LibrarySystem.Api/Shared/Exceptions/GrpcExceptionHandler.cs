using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Shared.Exceptions;

internal sealed class GrpcExceptionHandler(ILogger<GrpcExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RpcException rpcException)
        {
            return false;
        }

        var statusCode = MapStatusCode(rpcException.StatusCode);

        // Client errors (4xx) are expected traffic, not bugs - log them without a
        // stack trace so they don't drown out real failures in Seq. Anything that
        // maps to a 5xx is worth the full exception for troubleshooting.
        if (statusCode >= 500)
        {
            logger.LogWarning(rpcException, "gRPC call failed with {StatusCode}: {Detail}", rpcException.StatusCode, rpcException.Status.Detail);
        }
        else
        {
            logger.LogInformation("gRPC call failed with {StatusCode}: {Detail}", rpcException.StatusCode, rpcException.Status.Detail);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = rpcException.StatusCode.ToString(),
            Detail = rpcException.Status.Detail
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static int MapStatusCode(StatusCode statusCode) => statusCode switch
    {
        StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
        StatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
        StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
        StatusCode.NotFound => StatusCodes.Status404NotFound,
        StatusCode.AlreadyExists => StatusCodes.Status409Conflict,
        StatusCode.FailedPrecondition => StatusCodes.Status409Conflict,
        StatusCode.Aborted => StatusCodes.Status409Conflict,
        StatusCode.DeadlineExceeded => StatusCodes.Status504GatewayTimeout,
        StatusCode.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };
}
