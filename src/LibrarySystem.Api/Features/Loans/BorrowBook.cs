using Carter;
using FluentValidation;
using LibrarySystem.Api.Extensions;
using LoansContract = LibrarySystem.Contracts.Loans;

namespace LibrarySystem.Api.Features.Loans;

public sealed record BorrowBookRequest(Guid BookId, Guid ReaderId);

public sealed class BorrowBookRequestValidator : AbstractValidator<BorrowBookRequest>
{
    public BorrowBookRequestValidator()
    {
        RuleFor(x => x.BookId).NotEqual(Guid.Empty).WithMessage("BookId is required.");
        RuleFor(x => x.ReaderId).NotEqual(Guid.Empty).WithMessage("ReaderId is required.");
    }
}

public class BorrowBookEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/loans", async (
            BorrowBookRequest request,
            LoansContract.LoansGrpc.LoansGrpcClient client,
            CancellationToken cancellationToken) =>
        {
            var grpcRequest = new LoansContract.BorrowBookRequest
            {
                BookId = request.BookId.ToString(),
                ReaderId = request.ReaderId.ToString()
            };

            var grpcResponse = await client.BorrowBookAsync(
                grpcRequest,
                deadline: GrpcCallDefaults.Deadline,
                cancellationToken: cancellationToken);

            return Results.Created($"api/loans/{grpcResponse.LoanId}", grpcResponse);
        })
        .WithValidation<BorrowBookRequest>()
        .WithName("BorrowBook")
        .WithTags("Loans")
        .WithSummary("Borrow a book")
        .WithDescription("Registers a new loan for a reader. Fails with a conflict if the book is already on loan to another reader.");
    }
}
