using Bogus;
using LibrarySystem.Contracts.Common;
using LibrarySystem.Service.Domain;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;
using System.Globalization;

namespace LibrarySystem.Service.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly Guid SampleBookId = Guid.Parse(SampleIds.SampleBookId);
    private static readonly Guid SampleReaderId = Guid.Parse(SampleIds.SampleReaderId);
    private static readonly Guid SampleLoanedBookId = Guid.Parse(SampleIds.SampleLoanedBookId);
    private static readonly Guid SampleActiveLoanId = Guid.Parse(SampleIds.SampleActiveLoanId);

    public static void SeedData(LibraryDbContext context, ILogger logger, SeedSettings seedSettings)
    {
        if (!IsDatabaseAlreadySeeded(context, logger))
        {
            logger.LogInformation("Starting deterministic data seeding process...");
            Randomizer.Seed = new Random(seedSettings.DeterministicSeed);

            var books = SeedBooks(context, logger, seedSettings);
            var readers = SeedReaders(context, logger, seedSettings);
            SeedLoans(context, logger, books, readers, seedSettings);

            context.SaveChanges();
            logger.LogInformation("All data persisted successfully.");
        }

        LogSampleData(context, logger);
    }

    private static bool IsDatabaseAlreadySeeded(LibraryDbContext context, ILogger logger)
    {
        bool hasBooks = context.Set<Book>().Any();
        bool hasReaders = context.Set<Reader>().Any();
        bool hasLoans = context.Set<Loan>().Any();

        if (hasBooks && hasReaders && hasLoans)
        {
            logger.LogInformation("Database already contains core domain data. Skipping seeding.");
            return true;
        }

        if (hasBooks || hasReaders || hasLoans)
        {
            logger.LogWarning("Partial or inconsistent data detected. Truncating tables for a clean modular seed...");

            context.Database.ExecuteSqlRaw("TRUNCATE TABLE loans, readers, books CASCADE;");
        }

        return false;
    }
    private static List<Book> SeedBooks(LibraryDbContext context, ILogger logger, SeedSettings seedSettings)
    {
        logger.LogInformation("Seeding Books...");

        var bookFaker = new Faker<Book>()
            .CustomInstantiator(f => Book.Create(
                id: f.Random.Guid(),
                title: GenerateSimpleTitle(f, seedSettings.TitlePatterns),
                author: f.Name.FullName(),
                pageCount: f.Random.Int(seedSettings.MinPageCount, seedSettings.MaxPageCount)).Value);

        var books = bookFaker.Generate(seedSettings.BooksCount - 2);

        var sampleBook = Book.Create(SampleBookId, "Sample Book", "Sample Author", pageCount: 320).Value;
        var loanedBook = Book.Create(SampleLoanedBookId, "Sample Loaned Book", "Sample Author", pageCount: 240).Value;
        books.Add(sampleBook);
        books.Add(loanedBook);

        context.Set<Book>().AddRange(books);

        logger.LogInformation("Successfully generated {Count} books.", books.Count);
        return books;
    }
    private static List<Reader> SeedReaders(LibraryDbContext context, ILogger logger, SeedSettings seedSettings)
    {
        logger.LogInformation("Seeding Readers...");

        var readerFaker = new Faker<Reader>()
            .CustomInstantiator(f => {
                var id = f.Random.Guid();
                var firstName = f.Name.FirstName();
                var lastName = f.Name.LastName();
                var fullName = $"{firstName} {lastName}";
                var email = f.Internet.Email(firstName, lastName, null, f.IndexGlobal.ToString()).ToLowerInvariant();

                return Reader.Create(id, fullName, email).Value;
            });

        var readers = readerFaker.Generate(seedSettings.ReadersCount - 1);

        // Seeded with a fixed ID so there's always a known, popular book to test against.
        var sampleReader = Reader.Create(SampleReaderId, "Sample Reader", "sample.reader@example.com").Value;
        readers.Add(sampleReader);

        context.Set<Reader>().AddRange(readers);

        logger.LogInformation("Successfully generated {Count} readers.", readers.Count);
        return readers;
    }
    private static void SeedLoans(LibraryDbContext context, ILogger logger, List<Book> books, List<Reader> readers, SeedSettings seedSettings)
    {
        logger.LogInformation("Seeding Loans and calculating borrowing patterns...");

        var faker = new Faker();
        var drafts = new List<(Guid Id, Guid BookId, Guid ReaderId, DateTimeOffset BorrowedAtUtc)>(seedSettings.LoansCount);

        for (int i = 0; i < seedSettings.LoansCount; i++)
        {
            var book = faker.PickRandom(books);
            var reader = faker.PickRandom(readers);
            var borrowDate = RandomPastDate(faker, seedSettings.MaxMonthsInPast);

            drafts.Add((faker.Random.Guid(), book.Id, reader.Id, borrowDate));
        }

        // Giving sample reader and sample book a rich history.
        foreach (var book in faker.Random.ListItems(books, Math.Min(50, books.Count)))
        {
            drafts.Add((faker.Random.Guid(), book.Id, SampleReaderId, RandomPastDate(faker, seedSettings.MaxMonthsInPast)));
        }

        foreach (var reader in faker.Random.ListItems(readers, Math.Min(50, readers.Count)))
        {
            drafts.Add((faker.Random.Guid(), SampleBookId, reader.Id, RandomPastDate(faker, seedSettings.MaxMonthsInPast)));
        }

        var latestLoanIdPerBook = drafts
            .GroupBy(d => d.BookId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.BorrowedAtUtc).First().Id);

        var loans = new List<Loan>(drafts.Count);

        foreach (var draft in drafts)
        {
            var loanResult = Loan.Create(draft.Id, draft.BookId, draft.ReaderId, draft.BorrowedAtUtc);
            if (loanResult.IsFailure)
            {
                continue;
            }

            var loan = loanResult.Value;
            var canStayActive = latestLoanIdPerBook[draft.BookId] == draft.Id;
            var shouldReturn = !canStayActive || faker.Random.Bool(seedSettings.ProbabilityOfReturn);

            if (shouldReturn)
            {
                var returnDate = draft.BorrowedAtUtc.AddDays(faker.Random.Int(seedSettings.MinDaysToReturn, seedSettings.MaxDaysToReturn));
                loan.MarkAsReturned(returnDate);
            }

            loans.Add(loan);
        }

        foreach (var loan in loans.Where(l =>
                     l.ReturnedAtUtc is null &&
                     (l.BookId == SampleBookId || l.BookId == SampleLoanedBookId)))
        {
            loan.MarkAsReturned(DateTimeOffset.UtcNow);
        }

        var activeLoan = Loan.Create(
            SampleActiveLoanId,
            SampleLoanedBookId,
            SampleReaderId,
            DateTimeOffset.UtcNow.AddDays(-7)).Value;
        loans.Add(activeLoan);

        context.Set<Loan>().AddRange(loans);
        logger.LogInformation("Successfully generated {Count} historical loans.", loans.Count);
    }
    private static DateTimeOffset RandomPastDate(Faker faker, int maxMonthsInPast)
    {
        var daysBack = faker.Random.Int(0, maxMonthsInPast * 30);
        return DateTimeOffset.UtcNow.AddDays(-daysBack);
    }

    private static string GenerateSimpleTitle(Faker f, List<string> patterns)
    {
        if (patterns == null || !patterns.Any())
            return f.Commerce.ProductName();

        var pattern = f.PickRandom(patterns);
        var title = string.Format(pattern, f.Random.Word(), f.Random.Word());

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLowerInvariant());
    }
    private static void LogSampleData(LibraryDbContext context, ILogger logger)
    {
        var readerLoanCount = context.Set<Loan>().Count(l => l.ReaderId == SampleReaderId);
        var bookLoanCount = context.Set<Loan>().Count(l => l.BookId == SampleBookId);

        logger.LogInformation(
            "Sample reader {SampleReaderId} has {ReaderLoanCount} loans, sample book {SampleBookId} has {BookLoanCount} loans.",
            SampleReaderId, readerLoanCount, SampleBookId, bookLoanCount);
    }
}