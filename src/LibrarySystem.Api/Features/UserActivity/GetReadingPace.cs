using Carter;
using LibrarySystem.Api.Extensions;
using LibrarySystem.Contracts.ReaderMetrics;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Features.UserActivity;

public class GetReadingPaceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/readers/{readerId:guid}/reading-pace", async (
            [FromRoute] Guid readerId,
            ReaderMetricsGrpc.ReaderMetricsGrpcClient client,
            CancellationToken cancellationToken) =>
        {
            var request = new ReadingPaceRequest { ReaderId = readerId.ToString() };

            var grpcResponse = await client.GetReadingPaceAsync(
                request,
                deadline: GrpcCallDefaults.Deadline,
                cancellationToken: cancellationToken);

            return Results.Ok(new { grpcResponse.ReaderName, grpcResponse.PagesPerDay });
        })
        .WithName("GetReadingPace")
        .WithTags("User Activity")
        .WithSummary("Estimate reading pace")
        .WithDescription("Calculates a reader's reading speed in pages per day by comparing book lengths with loan durations.");
    }
}
