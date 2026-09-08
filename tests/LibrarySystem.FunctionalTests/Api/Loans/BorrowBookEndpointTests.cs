using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.FunctionalTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.FunctionalTests.Api.Loans;

public class BorrowBookEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public BorrowBookEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BorrowBook_WithValidIds_ReturnsCreated()
    {
        var bookId = Guid.NewGuid();
        var readerId = Guid.NewGuid();
        var loanId = Guid.NewGuid();

        _factory.LoansClient.BorrowFault = null;
        _factory.LoansClient.BorrowHandler = request => new BorrowBookResponse
        {
            LoanId = loanId.ToString(),
            BookId = request.BookId,
            ReaderId = request.ReaderId,
            BorrowedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };

        var response = await _client.PostAsJsonAsync("api/loans", new { bookId, readerId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"api/loans/{loanId}", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<BorrowBookResponse>();
        Assert.Equal(loanId.ToString(), body!.LoanId);
        Assert.Equal(bookId.ToString(), _factory.LoansClient.LastBorrowRequest!.BookId);
        Assert.Equal(readerId.ToString(), _factory.LoansClient.LastBorrowRequest!.ReaderId);
    }

    [Fact]
    public async Task BorrowBook_WithEmptyBookId_ReturnsValidationProblem_WithoutCallingTheService()
    {
        _factory.LoansClient.BorrowHandler = _ => throw new InvalidOperationException("Should not be called.");

        var response = await _client.PostAsJsonAsync("api/loans", new { bookId = Guid.Empty, readerId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("BookId", problem!.Errors.Keys);
    }

    [Fact]
    public async Task BorrowBook_WithEmptyReaderId_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("api/loans", new { bookId = Guid.NewGuid(), readerId = Guid.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("ReaderId", problem!.Errors.Keys);
    }

    [Fact]
    public async Task BorrowBook_WhenBookAlreadyOnLoan_ReturnsConflict()
    {
        _factory.LoansClient.BorrowFault = new RpcException(new Status(StatusCode.FailedPrecondition, "This book is already borrowed by another reader."));

        var response = await _client.PostAsJsonAsync("api/loans", new { bookId = Guid.NewGuid(), readerId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("This book is already borrowed by another reader.", problem!.Detail);

        _factory.LoansClient.BorrowFault = null;
    }

    [Fact]
    public async Task BorrowBook_WhenBookOrReaderDoesNotExist_ReturnsNotFound()
    {
        _factory.LoansClient.BorrowFault = new RpcException(new Status(StatusCode.NotFound, "Book was not found."));

        var response = await _client.PostAsJsonAsync("api/loans", new { bookId = Guid.NewGuid(), readerId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        _factory.LoansClient.BorrowFault = null;
    }

    [Fact]
    public async Task BorrowBook_WithMalformedJsonBody_ReturnsBadRequest()
    {
        using var content = new StringContent("{ not valid json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/loans", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
