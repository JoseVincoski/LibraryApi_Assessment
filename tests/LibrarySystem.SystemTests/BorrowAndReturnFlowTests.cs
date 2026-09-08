using System.Net;
using System.Net.Http.Json;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Service.Domain;
using LibrarySystem.SystemTests.Testing;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.SystemTests;

public class BorrowAndReturnFlowTests(PostgresContainerFixture postgres) : SystemTestBase(postgres)
{
    [Fact]
    public async Task BorrowBook_EndToEnd_PersistsLoanInTheRealDatabase()
    {
        var (bookId, readerId) = await SeedBookAndReaderAsync();

        var response = await Client.PostAsJsonAsync("api/loans", new { bookId, readerId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BorrowBookResponse>();
        var loanId = Guid.Parse(body!.LoanId);

        await using var context = Postgres.CreateDbContext();
        var loan = await context.Set<Loan>().SingleAsync(l => l.Id == loanId);
        Assert.Equal(bookId, loan.BookId);
        Assert.Equal(readerId, loan.ReaderId);
        Assert.Null(loan.ReturnedAtUtc);
    }

    [Fact]
    public async Task BorrowBook_SameBookConcurrentlyRequestedTwiceOverHttp_SecondRequestConflicts()
    {
        var (bookId, readerOneId) = await SeedBookAndReaderAsync();
        var readerTwoId = await SeedReaderAsync();

        var first = await Client.PostAsJsonAsync("api/loans", new { bookId, readerId = readerOneId });
        var second = await Client.PostAsJsonAsync("api/loans", new { bookId, readerId = readerTwoId });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        await using var context = Postgres.CreateDbContext();
        var activeLoans = await context.Set<Loan>().CountAsync(l => l.BookId == bookId && l.ReturnedAtUtc == null);
        Assert.Equal(1, activeLoans);
    }

    [Fact]
    public async Task BorrowBook_UnknownBookId_ReturnsNotFound()
    {
        var readerId = await SeedReaderAsync();

        var response = await Client.PostAsJsonAsync("api/loans", new { bookId = Guid.NewGuid(), readerId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnBook_EndToEnd_MarksTheLoanAsReturnedInTheRealDatabase()
    {
        var (bookId, readerId) = await SeedBookAndReaderAsync();
        var loanId = await BorrowAsync(bookId, readerId);

        var response = await Client.PostAsync($"api/loans/{loanId}/return", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var context = Postgres.CreateDbContext();
        var loan = await context.Set<Loan>().SingleAsync(l => l.Id == loanId);
        Assert.NotNull(loan.ReturnedAtUtc);
    }

    [Fact]
    public async Task ReturnBook_AlreadyReturned_SecondRequestConflicts()
    {
        var (bookId, readerId) = await SeedBookAndReaderAsync();
        var loanId = await BorrowAsync(bookId, readerId);

        var firstReturn = await Client.PostAsync($"api/loans/{loanId}/return", content: null);
        var secondReturn = await Client.PostAsync($"api/loans/{loanId}/return", content: null);

        Assert.Equal(HttpStatusCode.OK, firstReturn.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondReturn.StatusCode);
    }

    [Fact]
    public async Task ReturnBook_UnknownLoanId_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"api/loans/{Guid.NewGuid()}/return", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BorrowThenReturnThenBorrowAgain_SameBookCanBeLentOutASecondTime()
    {
        var (bookId, readerId) = await SeedBookAndReaderAsync();
        var otherReaderId = await SeedReaderAsync();

        var firstLoanId = await BorrowAsync(bookId, readerId);
        var returnResponse = await Client.PostAsync($"api/loans/{firstLoanId}/return", content: null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var secondBorrowResponse = await Client.PostAsJsonAsync("api/loans", new { bookId, readerId = otherReaderId });

        Assert.Equal(HttpStatusCode.Created, secondBorrowResponse.StatusCode);
    }

    private async Task<Guid> BorrowAsync(Guid bookId, Guid readerId)
    {
        var response = await Client.PostAsJsonAsync("api/loans", new { bookId, readerId });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<BorrowBookResponse>();
        return Guid.Parse(body!.LoanId);
    }

    private async Task<(Guid BookId, Guid ReaderId)> SeedBookAndReaderAsync()
    {
        await using var context = Postgres.CreateDbContext();
        var book = Book.Create(null, "System Test Book", "Author", 200).Value;
        var reader = Reader.Create(null, "System Test Reader", $"{Guid.NewGuid()}@example.com").Value;
        context.AddRange(book, reader);
        await context.SaveChangesAsync();
        return (book.Id, reader.Id);
    }

    private async Task<Guid> SeedReaderAsync()
    {
        await using var context = Postgres.CreateDbContext();
        var reader = Reader.Create(null, "Extra Reader", $"{Guid.NewGuid()}@example.com").Value;
        context.Add(reader);
        await context.SaveChangesAsync();
        return reader.Id;
    }
}
