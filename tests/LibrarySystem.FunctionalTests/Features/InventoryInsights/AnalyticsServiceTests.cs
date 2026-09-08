using LibrarySystem.Contracts.Analytics;
using LibrarySystem.FunctionalTests.Testing;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Features.InventoryInsights;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibrarySystem.FunctionalTests.Features.InventoryInsights;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task GetMostBorrowedBooks_RanksByBorrowCount_AndRespectsMaxResults()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var popular = Book.Create(null, "Popular Book", "Author A", 200).Value;
        var medium = Book.Create(null, "Medium Book", "Author B", 200).Value;
        var rare = Book.Create(null, "Rare Book", "Author C", 200).Value;
        var reader = Reader.Create(null, "Reader One", "reader@example.com").Value;
        db.AddRange(popular, medium, rare, reader);

        AddReturnedLoans(db, popular.Id, reader.Id, count: 3);
        AddReturnedLoans(db, medium.Id, reader.Id, count: 2);
        AddReturnedLoans(db, rare.Id, reader.Id, count: 1);
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db, NullLogger<AnalyticsService>.Instance);
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = 2 };

        var response = await service.GetMostBorrowedBooks(request, TestServerCallContext.Create());

        Assert.Equal(2, response.Books.Count);
        Assert.Equal(popular.Id.ToString(), response.Books[0].Id);
        Assert.Equal(3, response.Books[0].BorrowCount);
        Assert.Equal(medium.Id.ToString(), response.Books[1].Id);
    }

    [Fact]
    public async Task GetMostBorrowedBooks_WithNoLoansAtAll_ReturnsEmptyList()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var service = new AnalyticsService(db, NullLogger<AnalyticsService>.Instance);
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = 10 };

        var response = await service.GetMostBorrowedBooks(request, TestServerCallContext.Create());

        Assert.Empty(response.Books);
    }

    [Fact]
    public async Task GetMostBorrowedBooks_LoanJustInsideTheLookbackWindow_IsIncluded()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Boundary Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        db.AddRange(book, reader);

        db.Add(Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-29).AddHours(-23)).Value);
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db, NullLogger<AnalyticsService>.Instance);
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = 10 };

        var response = await service.GetMostBorrowedBooks(request, TestServerCallContext.Create());

        Assert.Single(response.Books);
    }

    [Fact]
    public async Task GetMostBorrowedBooks_ExcludesLoansOutsideLookbackWindow()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var book = Book.Create(null, "Old Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        db.AddRange(book, reader);

        var oldLoan = Loan.Create(null, book.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-60)).Value;
        db.Add(oldLoan);
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db, NullLogger<AnalyticsService>.Instance);
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = 10 };

        var response = await service.GetMostBorrowedBooks(request, TestServerCallContext.Create());

        Assert.Empty(response.Books);
    }

    private static void AddReturnedLoans(Service.Infrastructure.Persistence.LibraryDbContext db, Guid bookId, Guid readerId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var loan = Loan.Create(null, bookId, readerId, DateTimeOffset.UtcNow.AddDays(-1)).Value;
            loan.MarkAsReturned(DateTimeOffset.UtcNow);
            db.Add(loan);
        }
    }
}
