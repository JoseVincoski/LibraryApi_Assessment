using Carter;
using LibrarySystem.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using LoansContract = LibrarySystem.Contracts.Loans;

namespace LibrarySystem.Api.Features.Loans;

public class ReturnBookEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/loans/{loanId:guid}/return", async (
            [FromRoute] Guid loanId,
            LoansContract.LoansGrpc.LoansGrpcClient client,
            CancellationToken cancellationToken) =>
        {
            var grpcRequest = new LoansContract.ReturnBookRequest { LoanId = loanId.ToString() };

            var grpcResponse = await client.ReturnBookAsync(
                grpcRequest,
                deadline: GrpcCallDefaults.Deadline,
                cancellationToken: cancellationToken);

            return Results.Ok(grpcResponse);
        })
        .WithName("ReturnBook")
        .WithTags("Loans")
        .WithSummary("Return a borrowed book")
        .WithDescription("Marks a loan as returned. Fails with a conflict if the loan was alreadyreturned or modified by another request in the meantime.");
    }
}
