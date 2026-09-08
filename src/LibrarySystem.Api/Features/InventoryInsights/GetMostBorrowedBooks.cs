using Carter;
using LibrarySystem.Api.Extensions;
using LibrarySystem.Contracts.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Features.InventoryInsights;

public class GetMostBorrowedBooksEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/inventory/most-borrowed", async (
            [FromQuery] int? daysLookback,
            [FromQuery] int? maxResults,
            AnalyticsGrpc.AnalyticsGrpcClient client,
            CancellationToken cancellationToken) =>
        {
            var request = new MostBorrowedRequest
            {
                DaysLookback = daysLookback ?? 30,
                MaxResults = maxResults ?? 10
            };

            var grpcResponse = await client.GetMostBorrowedBooksAsync(
                request,
                deadline: GrpcCallDefaults.Deadline,
                cancellationToken: cancellationToken);

            return Results.Ok(grpcResponse.Books);
        })
        .WithName("GetMostBorrowedBooks")
        .WithTags("Inventory Insights")
        .WithSummary("Get most borrowed books")
        .WithDescription("""
            Analyzes historical lending data to rank books by popularity within a specified day range.

            **Parameters (both optional):**
            - `daysLookback` - default 30 days, controls the popularity trend timeframe.
            - `maxResults` - default 10, how many books to return (1-100).
         """);
    }
}
