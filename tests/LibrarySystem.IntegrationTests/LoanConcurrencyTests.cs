using Grpc.Core;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.IntegrationTests.Testing;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Features.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibrarySystem.IntegrationTests;

// EF Core's InMemory provider can't reproduce either race below - both need
// a real Postgres engine enforcing a constraint at write time.
[Collection(PostgresCollection.Name)]
public class LoanConcurrencyTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BorrowBook_TwoConcurrentRequestsForTheSameBook_OnlyOneSucceeds()
    {
        Guid bookId, readerOneId, readerTwoId;
        await using (var setup = fixture.CreateDbContext())
        {
            var book = Book.Create(null, "Contested Book", "Author", 200).Value;
            var readerOne = Reader.Create(null, "Reader One", "one@example.com").Value;
            var readerTwo = Reader.Create(null, "Reader Two", "two@example.com").Value;
            setup.AddRange(book, readerOne, readerTwo);
            await setup.SaveChangesAsync();
            (bookId, readerOneId, readerTwoId) = (book.Id, readerOne.Id, readerTwo.Id);
        }

        await using var dbContextOne = fixture.CreateDbContext();
        await using var dbContextTwo = fixture.CreateDbContext();
        var serviceOne = new LoansService(dbContextOne, NullLogger<LoansService>.Instance);
        var serviceTwo = new LoansService(dbContextTwo, NullLogger<LoansService>.Instance);

        var requestOne = new BorrowBookRequest { BookId = bookId.ToString(), ReaderId = readerOneId.ToString() };
        var requestTwo = new BorrowBookRequest { BookId = bookId.ToString(), ReaderId = readerTwoId.ToString() };

        var taskOne = InvokeAsync(() => serviceOne.BorrowBook(requestOne, TestServerCallContext.Create()));
        var taskTwo = InvokeAsync(() => serviceTwo.BorrowBook(requestTwo, TestServerCallContext.Create()));
        var results = await Task.WhenAll(taskOne, taskTwo);

        var succeeded = results.Where(r => r.Response is not null).ToList();
        var failed = results.Where(r => r.Exception is not null).ToList();

        Assert.Single(succeeded);
        var failure = Assert.Single(failed);
        Assert.Equal(StatusCode.FailedPrecondition, failure.Exception!.StatusCode);

        await using var verifyContext = fixture.CreateDbContext();
        var activeLoanCount = await verifyContext.Set<Loan>()
            .CountAsync(l => l.BookId == bookId && l.ReturnedAtUtc == null);
        Assert.Equal(1, activeLoanCount);
    }

    [Fact]
    public async Task ReturnBook_TwoConcurrentReturnsOfTheSameLoan_OnlyOneSucceeds()
    {
        Guid loanId;
        await using (var setup = fixture.CreateDbContext())
        {
            var book = Book.Create(null, "Book", "Author", 200).Value;
            var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
            var loan = Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-1)).Value;
            setup.AddRange(book, reader, loan);
            await setup.SaveChangesAsync();
            loanId = loan.Id;
        }

        // Both contexts load the loan (and therefore its xmin) *before* either
        // one writes, exactly like two requests that both read, then race to write.
        await using var dbContextOne = fixture.CreateDbContext();
        await using var dbContextTwo = fixture.CreateDbContext();
        await dbContextOne.Set<Loan>().SingleAsync(l => l.Id == loanId);
        await dbContextTwo.Set<Loan>().SingleAsync(l => l.Id == loanId);

        var serviceOne = new LoansService(dbContextOne, NullLogger<LoansService>.Instance);
        var serviceTwo = new LoansService(dbContextTwo, NullLogger<LoansService>.Instance);
        var request = new ReturnBookRequest { LoanId = loanId.ToString() };

        var taskOne = InvokeAsync(() => serviceOne.ReturnBook(request, TestServerCallContext.Create()));
        var taskTwo = InvokeAsync(() => serviceTwo.ReturnBook(request, TestServerCallContext.Create()));
        var results = await Task.WhenAll(taskOne, taskTwo);

        var succeeded = results.Where(r => r.Response is not null).ToList();
        var failed = results.Where(r => r.Exception is not null).ToList();

        Assert.Single(succeeded);
        var failure = Assert.Single(failed);
        Assert.Equal(StatusCode.Aborted, failure.Exception!.StatusCode);

        await using var verifyContext = fixture.CreateDbContext();
        var persistedLoan = await verifyContext.Set<Loan>().SingleAsync(l => l.Id == loanId);
        Assert.NotNull(persistedLoan.ReturnedAtUtc);
    }

    private static async Task<(TResponse? Response, RpcException? Exception)> InvokeAsync<TResponse>(Func<Task<TResponse>> call)
        where TResponse : class
    {
        try
        {
            return (await call(), null);
        }
        catch (RpcException ex)
        {
            return (null, ex);
        }
    }
}
