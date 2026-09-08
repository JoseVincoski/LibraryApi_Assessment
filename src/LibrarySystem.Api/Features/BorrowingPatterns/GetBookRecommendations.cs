using Carter;
using LibrarySystem.Api.Extensions;
using LibrarySystem.Contracts.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Features.BorrowingPatterns;

public class GetBookRecommendationsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/books/{bookId:guid}/borrowing-patterns", async (
            [FromRoute] Guid bookId,
            [FromQuery] int? maxResults,
            RecommendationsGrpc.RecommendationsGrpcClient client,
            CancellationToken cancellationToken) =>
        {
            var request = new RecommendationRequest { BookId = bookId.ToString(), MaxResults = maxResults ?? 3 };

            var grpcResponse = await client.GetBookRecommendationsAsync(
                request,
                deadline: GrpcCallDefaults.Deadline,
                cancellationToken: cancellationToken);

            return Results.Ok(grpcResponse.RecommendedBookTitles);
        })
        .WithName("GetBookRecommendations")
        .WithTags("Borrowing Patterns")
        .WithSummary("Get co-borrowing recommendations")
        .WithDescription("""
            Suggests related titles based on the borrowing patterns of other readers who read the same book.

            **Parameter (optional):** `maxResults` - default 3, how many titles to return (1-100).
         """);
    }
}
