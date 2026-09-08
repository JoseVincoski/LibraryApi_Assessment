using Grpc.Core;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.FunctionalTests.Testing;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Features.Loans;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibrarySystem.FunctionalTests.Features.Loans;

public class LoansServiceTests
{
    // Note: the "book already on loan" (unique index) and "loan changed
    // concurrently" (xmin) conflict paths can't be exercised here - the
    // InMemory provider doesn't produce the same PostgresException/
    // DbUpdateConcurrencyException these services specifically catch. Those
    // are covered later by Testcontainers-backed integration tests.

    [Fact]
    public async Task BorrowBook_WithExistingBookAndReader_CreatesLoan()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        db.AddRange(book, reader);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var request = new BorrowBookRequest { BookId = book.Id.ToString(), ReaderId = reader.Id.ToString() };

        var response = await service.BorrowBook(request, TestServerCallContext.Create());

        Assert.Equal(book.Id.ToString(), response.BookId);
        Assert.Equal(reader.Id.ToString(), response.ReaderId);
        Assert.NotEmpty(response.LoanId);
        Assert.Single(db.Set<Loan>());
    }

    [Fact]
    public async Task BorrowBook_WithUnknownBook_ThrowsNotFound()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        db.Add(reader);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var request = new BorrowBookRequest { BookId = Guid.NewGuid().ToString(), ReaderId = reader.Id.ToString() };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.BorrowBook(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task BorrowBook_WithUnknownReader_ThrowsNotFound()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Book", "Author", 200).Value;
        db.Add(book);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var request = new BorrowBookRequest { BookId = book.Id.ToString(), ReaderId = Guid.NewGuid().ToString() };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.BorrowBook(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task ReturnBook_WithActiveLoan_MarksItReturned()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        var loan = Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-1)).Value;
        db.AddRange(book, reader, loan);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var request = new ReturnBookRequest { LoanId = loan.Id.ToString() };

        var response = await service.ReturnBook(request, TestServerCallContext.Create());

        Assert.Equal(loan.Id.ToString(), response.LoanId);
        Assert.NotEmpty(response.ReturnedAtUtc);
    }

    [Fact]
    public async Task ReturnBook_WithUnknownLoan_ThrowsNotFound()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var request = new ReturnBookRequest { LoanId = Guid.NewGuid().ToString() };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.ReturnBook(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task BorrowBook_GeneratesANewLoanIdOnEachCall()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Book", "Author", 200).Value;
        var readerOne = Reader.Create(null, "Reader One", "one@example.com").Value;
        var readerTwo = Reader.Create(null, "Reader Two", "two@example.com").Value;
        db.AddRange(book, readerOne, readerTwo);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);

        var first = await service.BorrowBook(
            new BorrowBookRequest { BookId = book.Id.ToString(), ReaderId = readerOne.Id.ToString() },
            TestServerCallContext.Create());

        // Return it before borrowing again, since a real book can only be on loan to one reader at a time.
        await service.ReturnBook(new ReturnBookRequest { LoanId = first.LoanId }, TestServerCallContext.Create());

        var second = await service.BorrowBook(
            new BorrowBookRequest { BookId = book.Id.ToString(), ReaderId = readerTwo.Id.ToString() },
            TestServerCallContext.Create());

        Assert.NotEqual(first.LoanId, second.LoanId);
    }

    [Fact]
    public async Task ReturnBook_SetsReturnedAtToOnOrAfterTheBorrowDate()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        var loan = Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-1)).Value;
        db.AddRange(book, reader, loan);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var response = await service.ReturnBook(new ReturnBookRequest { LoanId = loan.Id.ToString() }, TestServerCallContext.Create());

        Assert.True(DateTimeOffset.Parse(response.ReturnedAtUtc) >= loan.BorrowedAtUtc);
    }

    [Fact]
    public async Task ReturnBook_AlreadyReturned_ThrowsFailedPrecondition()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        var loan = Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-2)).Value;
        loan.MarkAsReturned(DateTimeOffset.UtcNow.AddDays(-1));
        db.AddRange(book, reader, loan);
        await db.SaveChangesAsync();

        var service = new LoansService(db, NullLogger<LoansService>.Instance);
        var request = new ReturnBookRequest { LoanId = loan.Id.ToString() };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.ReturnBook(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
    }
}
