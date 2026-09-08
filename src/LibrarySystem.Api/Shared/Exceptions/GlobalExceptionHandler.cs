using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Shared.Exceptions;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Malformed request bodies (bad JSON, wrong content-type, etc.) surface
        // as BadHttpRequestException from the framework's own model binding,
        // already carrying the right 4xx status - that's client error, not a
        // server bug, so it shouldn't be flattened into a generic 500.
        if (exception is BadHttpRequestException badRequestException)
        {
            logger.LogInformation("Bad request: {Message}", badRequestException.Message);

            var badRequestProblem = new ProblemDetails
            {
                Status = badRequestException.StatusCode,
                Title = "Bad Request",
                Detail = "The request could not be understood by the server."
            };

            httpContext.Response.StatusCode = badRequestException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(badRequestProblem, cancellationToken);

            return true;
        }

        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server Error",
            Detail = "An unexpected error occurred on the server. Please try again later."
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}