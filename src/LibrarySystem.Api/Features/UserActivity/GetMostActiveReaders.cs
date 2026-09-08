using Carter;
using LibrarySystem.Api.Extensions;
using LibrarySystem.Contracts.ReaderMetrics;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Features.UserActivity;

public class GetMostActiveReadersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/readers/most-active", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? maxResults,
            ReaderMetricsGrpc.ReaderMetricsGrpcClient client,
            CancellationToken cancellationToken) =>
        {
            var request = new ActiveReadersRequest
            {
                StartDateUtc = (startDate ?? DateTime.UtcNow.AddDays(-30)).ToString("O"),
                EndDateUtc = (endDate ?? DateTime.UtcNow).ToString("O"),
                MaxResults = maxResults ?? 10
            };

            var grpcResponse = await client.GetMostActiveReadersAsync(
                request,
                deadline: GrpcCallDefaults.Deadline,
                cancellationToken: cancellationToken);

            return Results.Ok(grpcResponse.Readers);
        })
        .WithName("GetMostActiveReaders")
        .WithTags("User Activity")
        .WithSummary("Get most active readers")
        .WithDescription("""
            Identifies top readers by calculating the total volume of books borrowed within a specific time frame.

            **Parameters (all optional):**
            - `startDate` / `endDate` - ISO-8601 format (e.g., 2026-01-01), default to the last 30 days.
            - `maxResults` - default 10, how many readers to return (1-100).
         """);
    }
}
