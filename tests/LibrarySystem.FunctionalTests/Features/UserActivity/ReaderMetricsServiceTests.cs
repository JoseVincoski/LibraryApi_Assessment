using Grpc.Core;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.FunctionalTests.Testing;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Features.UserActivity;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibrarySystem.FunctionalTests.Features.UserActivity;

public class ReaderMetricsServiceTests
{
    [Fact]
    public async Task GetMostActiveReaders_RanksByBorrowCount_WithinDateRange()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Some Book", "Author", 200).Value;
        var activeReader = Reader.Create(null, "Active Reader", "active@example.com").Value;
        var quietReader = Reader.Create(null, "Quiet Reader", "quiet@example.com").Value;
        db.AddRange(book, activeReader, quietReader);

        var now = DateTimeOffset.UtcNow;
        db.Add(Loan.Create(null, book.Id, activeReader.Id, now.AddDays(-5)).Value);
        db.Add(Loan.Create(null, book.Id, activeReader.Id, now.AddDays(-3)).Value);
        db.Add(Loan.Create(null, book.Id, quietReader.Id, now.AddDays(-2)).Value);
        // Outside the queried window entirely.
        db.Add(Loan.Create(null, book.Id, activeReader.Id, now.AddDays(-90)).Value);
        await db.SaveChangesAsync();

        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var request = new ActiveReadersRequest
        {
            StartDateUtc = now.AddDays(-30).ToString("O"),
            EndDateUtc = now.ToString("O"),
            MaxResults = 10
        };

        var response = await service.GetMostActiveReaders(request, TestServerCallContext.Create());

        Assert.Equal(2, response.Readers.Count);
        Assert.Equal(activeReader.Id.ToString(), response.Readers[0].Id);
        Assert.Equal(2, response.Readers[0].BorrowCount);
        Assert.Equal(quietReader.Id.ToString(), response.Readers[1].Id);
    }

    [Fact]
    public async Task GetReadingPace_ComputesPagesPerDay_FromReturnedLoansOnly()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        var book = Book.Create(null, "Book", "Author", 300).Value;
        db.AddRange(reader, book);

        // Returned after exactly 3 days -> 100 pages/day.
        var borrowedAt = DateTimeOffset.UtcNow.AddDays(-3);
        var loan = Loan.Create(null, book.Id, reader.Id, borrowedAt).Value;
        loan.MarkAsReturned(borrowedAt.AddDays(3));
        db.Add(loan);

        // Still active - must be excluded from the pace calculation.
        var otherBook = Book.Create(null, "Other Book", "Author", 500).Value;
        db.Add(otherBook);
        db.Add(Loan.Create(null, otherBook.Id, reader.Id, DateTimeOffset.UtcNow).Value);

        await db.SaveChangesAsync();

        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var request = new ReadingPaceRequest { ReaderId = reader.Id.ToString() };

        var response = await service.GetReadingPace(request, TestServerCallContext.Create());

        Assert.Equal(reader.Name, response.ReaderName);
        Assert.Equal(100, response.PagesPerDay);
    }

    [Fact]
    public async Task GetMostActiveReaders_WithNoLoansInRange_ReturnsEmptyList()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var now = DateTimeOffset.UtcNow;
        var request = new ActiveReadersRequest
        {
            StartDateUtc = now.AddDays(-30).ToString("O"),
            EndDateUtc = now.ToString("O"),
            MaxResults = 10
        };

        var response = await service.GetMostActiveReaders(request, TestServerCallContext.Create());

        Assert.Empty(response.Readers);
    }

    [Fact]
    public async Task GetReadingPace_ReaderWithNoReturnedLoans_ReturnsZeroPagesPerDay()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        var book = Book.Create(null, "Book", "Author", 300).Value;
        db.AddRange(reader, book);
        // Still-active loan only - shouldn't count toward the pace calculation.
        db.Add(Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow).Value);
        await db.SaveChangesAsync();

        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var response = await service.GetReadingPace(new ReadingPaceRequest { ReaderId = reader.Id.ToString() }, TestServerCallContext.Create());

        Assert.Equal(0, response.PagesPerDay);
    }

    [Fact]
    public async Task GetReadingPace_ReaderWithNoLoansAtAll_ReturnsZeroPagesPerDay()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var reader = Reader.Create(null, "Lonely Reader", "lonely@example.com").Value;
        db.Add(reader);
        await db.SaveChangesAsync();

        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var response = await service.GetReadingPace(new ReadingPaceRequest { ReaderId = reader.Id.ToString() }, TestServerCallContext.Create());

        Assert.Equal(reader.Name, response.ReaderName);
        Assert.Equal(0, response.PagesPerDay);
    }

    [Fact]
    public async Task GetReadingPace_ReturnedOnTheSameDayItWasBorrowed_DoesNotDivideByZero()
    {
        // Days is floored to a minimum of 1 in the service specifically to avoid a division by (near) zero for same-day returns.
        await using var db = InMemoryDbContextFactory.Create();

        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        var book = Book.Create(null, "Book", "Author", 100).Value;
        db.AddRange(reader, book);

        var borrowedAt = DateTimeOffset.UtcNow;
        var loan = Loan.Create(null, book.Id, reader.Id, borrowedAt).Value;
        loan.MarkAsReturned(borrowedAt.AddHours(2));
        db.Add(loan);
        await db.SaveChangesAsync();

        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var response = await service.GetReadingPace(new ReadingPaceRequest { ReaderId = reader.Id.ToString() }, TestServerCallContext.Create());

        Assert.Equal(100, response.PagesPerDay);
    }

    [Fact]
    public async Task GetReadingPace_UnknownReader_ThrowsNotFound()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var service = new ReaderMetricsService(db, NullLogger<ReaderMetricsService>.Instance);
        var request = new ReadingPaceRequest { ReaderId = Guid.NewGuid().ToString() };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.GetReadingPace(request, TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }
}
