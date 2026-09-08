using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.FunctionalTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.FunctionalTests.Api.Loans;

public class ReturnBookEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public ReturnBookEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReturnBook_WithValidLoanId_ReturnsOk()
    {
        var loanId = Guid.NewGuid();
        _factory.LoansClient.ReturnFault = null;
        _factory.LoansClient.ReturnHandler = request => new ReturnBookResponse
        {
            LoanId = request.LoanId,
            ReturnedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };

        var response = await _client.PostAsync($"api/loans/{loanId}/return", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReturnBookResponse>();
        Assert.Equal(loanId.ToString(), body!.LoanId);
        Assert.Equal(loanId.ToString(), _factory.LoansClient.LastReturnRequest!.LoanId);
    }

    [Fact]
    public async Task ReturnBook_WithNonGuidRouteValue_ReturnsNotFound()
    {
        var response = await _client.PostAsync("api/loans/not-a-guid/return", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnBook_WhenLoanAlreadyReturned_ReturnsConflict()
    {
        _factory.LoansClient.ReturnFault = new RpcException(new Status(StatusCode.FailedPrecondition, "This loan has already been returned."));

        var response = await _client.PostAsync($"api/loans/{Guid.NewGuid()}/return", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("This loan has already been returned.", problem!.Detail);

        _factory.LoansClient.ReturnFault = null;
    }

    [Fact]
    public async Task ReturnBook_WhenLoanWasModifiedConcurrently_ReturnsConflict()
    {
        _factory.LoansClient.ReturnFault = new RpcException(new Status(StatusCode.Aborted, "This loan was already updated by another request. Please retry."));

        var response = await _client.PostAsync($"api/loans/{Guid.NewGuid()}/return", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        _factory.LoansClient.ReturnFault = null;
    }

    [Fact]
    public async Task ReturnBook_WithUnknownLoanId_ReturnsNotFound()
    {
        _factory.LoansClient.ReturnFault = new RpcException(new Status(StatusCode.NotFound, "Loan was not found."));

        var response = await _client.PostAsync($"api/loans/{Guid.NewGuid()}/return", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        _factory.LoansClient.ReturnFault = null;
    }
}
