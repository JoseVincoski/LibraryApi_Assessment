using LibrarySystem.Contracts.Recommendations;
using LibrarySystem.FunctionalTests.Testing;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Features.BorrowingPatterns;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibrarySystem.FunctionalTests.Features.BorrowingPatterns;

public class RecommendationServiceTests
{
    [Fact]
    public async Task GetBookRecommendations_ReturnsBooksBorrowedByTheSameReaders_ExcludingTheTargetBook()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var target = Book.Create(null, "Target Book", "Author", 200).Value;
        var coBorrowedTwice = Book.Create(null, "Frequently Co-Borrowed", "Author", 200).Value;
        var coBorrowedOnce = Book.Create(null, "Rarely Co-Borrowed", "Author", 200).Value;
        var unrelated = Book.Create(null, "Unrelated Book", "Author", 200).Value;

        var readerA = Reader.Create(null, "Reader A", "a@example.com").Value;
        var readerB = Reader.Create(null, "Reader B", "b@example.com").Value;
        var readerC = Reader.Create(null, "Reader C", "c@example.com").Value;

        db.AddRange(target, coBorrowedTwice, coBorrowedOnce, unrelated, readerA, readerB, readerC);

        var now = DateTimeOffset.UtcNow;
        // Reader A and Reader B both borrowed the target book...
        db.Add(Loan.Create(null, target.Id, readerA.Id, now.AddDays(-10)).Value);
        db.Add(Loan.Create(null, target.Id, readerB.Id, now.AddDays(-9)).Value);
        // ...and both also borrowed "Frequently Co-Borrowed".
        db.Add(Loan.Create(null, coBorrowedTwice.Id, readerA.Id, now.AddDays(-8)).Value);
        db.Add(Loan.Create(null, coBorrowedTwice.Id, readerB.Id, now.AddDays(-7)).Value);
        // Only Reader A also borrowed "Rarely Co-Borrowed".
        db.Add(Loan.Create(null, coBorrowedOnce.Id, readerA.Id, now.AddDays(-6)).Value);
        // Reader C never touched the target book, so this shouldn't influence results.
        db.Add(Loan.Create(null, unrelated.Id, readerC.Id, now.AddDays(-5)).Value);

        await db.SaveChangesAsync();

        var service = new RecommendationService(db, NullLogger<RecommendationService>.Instance);
        var request = new RecommendationRequest { BookId = target.Id.ToString(), MaxResults = 10 };

        var response = await service.GetBookRecommendations(request, TestServerCallContext.Create());

        Assert.Equal(2, response.RecommendedBookTitles.Count);
        Assert.Equal(coBorrowedTwice.Title, response.RecommendedBookTitles[0]);
        Assert.Equal(coBorrowedOnce.Title, response.RecommendedBookTitles[1]);
        Assert.DoesNotContain(target.Title, response.RecommendedBookTitles);
        Assert.DoesNotContain(unrelated.Title, response.RecommendedBookTitles);
    }

    [Fact]
    public async Task GetBookRecommendations_ForABookNoOneHasBorrowed_ReturnsEmptyList()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var target = Book.Create(null, "Never Borrowed", "Author", 200).Value;
        db.Add(target);
        await db.SaveChangesAsync();

        var service = new RecommendationService(db, NullLogger<RecommendationService>.Instance);
        var request = new RecommendationRequest { BookId = target.Id.ToString(), MaxResults = 10 };

        var response = await service.GetBookRecommendations(request, TestServerCallContext.Create());

        Assert.Empty(response.RecommendedBookTitles);
    }

    [Fact]
    public async Task GetBookRecommendations_WhenNoOneElseBorrowedFromTheSameReaders_ReturnsEmptyList()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var target = Book.Create(null, "Target Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Solo Reader", "solo@example.com").Value;
        db.AddRange(target, reader);
        db.Add(Loan.Create(null, target.Id, reader.Id, DateTimeOffset.UtcNow).Value);
        await db.SaveChangesAsync();

        var service = new RecommendationService(db, NullLogger<RecommendationService>.Instance);
        var request = new RecommendationRequest { BookId = target.Id.ToString(), MaxResults = 10 };

        var response = await service.GetBookRecommendations(request, TestServerCallContext.Create());

        Assert.Empty(response.RecommendedBookTitles);
    }

    [Fact]
    public async Task GetBookRecommendations_RespectsMaxResults()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var target = Book.Create(null, "Target Book", "Author", 200).Value;
        var reader = Reader.Create(null, "Reader", "reader@example.com").Value;
        db.AddRange(target, reader);
        db.Add(Loan.Create(null, target.Id, reader.Id, DateTimeOffset.UtcNow.AddDays(-1)).Value);

        for (var i = 0; i < 5; i++)
        {
            var otherBook = Book.Create(null, $"Other Book {i}", "Author", 200).Value;
            db.Add(otherBook);
            db.Add(Loan.Create(null, otherBook.Id, reader.Id, DateTimeOffset.UtcNow).Value);
        }

        await db.SaveChangesAsync();

        var service = new RecommendationService(db, NullLogger<RecommendationService>.Instance);
        var request = new RecommendationRequest { BookId = target.Id.ToString(), MaxResults = 3 };

        var response = await service.GetBookRecommendations(request, TestServerCallContext.Create());

        Assert.Equal(3, response.RecommendedBookTitles.Count);
    }
}
