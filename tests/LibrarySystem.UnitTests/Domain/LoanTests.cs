using LibrarySystem.Service.Domain;

namespace LibrarySystem.UnitTests.Domain;

public class LoanTests
{
    private static readonly Guid BookId = Guid.NewGuid();
    private static readonly Guid ReaderId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndNormalizesToUtc()
    {
        var borrowedAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

        var result = Loan.Create(id: null, BookId, ReaderId, borrowedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(borrowedAt.ToUniversalTime(), result.Value.BorrowedAtUtc);
        Assert.Null(result.Value.ReturnedAtUtc);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void Create_WithExplicitId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var result = Loan.Create(id, BookId, ReaderId, DateTimeOffset.UtcNow);

        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public void Create_WithEmptyBookId_ReturnsFailure()
    {
        var result = Loan.Create(null, Guid.Empty, ReaderId, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.EmptyBookId", result.Error.Code);
    }

    [Fact]
    public void Create_WithEmptyReaderId_ReturnsFailure()
    {
        var result = Loan.Create(null, BookId, Guid.Empty, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.EmptyReaderId", result.Error.Code);
    }

    [Fact]
    public void MarkAsReturned_OnActiveLoan_ReturnsSuccessAndSetsReturnedDate()
    {
        var loan = Loan.Create(null, BookId, ReaderId, DateTimeOffset.UtcNow.AddDays(-3)).Value;
        var returnedAt = DateTimeOffset.UtcNow;

        var result = loan.MarkAsReturned(returnedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(returnedAt.ToUniversalTime(), loan.ReturnedAtUtc);
    }

    [Fact]
    public void MarkAsReturned_WhenAlreadyReturned_ReturnsFailure()
    {
        var loan = Loan.Create(null, BookId, ReaderId, DateTimeOffset.UtcNow.AddDays(-3)).Value;
        loan.MarkAsReturned(DateTimeOffset.UtcNow.AddDays(-1));

        var result = loan.MarkAsReturned(DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.AlreadyReturned", result.Error.Code);
    }

    [Fact]
    public void MarkAsReturned_BeforeBorrowDate_ReturnsFailure()
    {
        var borrowedAt = DateTimeOffset.UtcNow;
        var loan = Loan.Create(null, BookId, ReaderId, borrowedAt).Value;

        var result = loan.MarkAsReturned(borrowedAt.AddDays(-1));

        Assert.True(result.IsFailure);
        Assert.Equal("Loan.InvalidReturnDate", result.Error.Code);
    }

    [Fact]
    public void MarkAsReturned_AtExactlyTheBorrowInstant_ReturnsSuccess()
    {
        // Boundary case: same-day (same-instant) borrow-and-return is a valid even though it's unusual.
        var borrowedAt = DateTimeOffset.UtcNow;
        var loan = Loan.Create(null, BookId, ReaderId, borrowedAt).Value;

        var result = loan.MarkAsReturned(borrowedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(borrowedAt.ToUniversalTime(), loan.ReturnedAtUtc);
    }

    [Fact]
    public void MarkAsReturned_AfterAlreadyReturned_DoesNotOverwriteTheOriginalReturnDate()
    {
        var loan = Loan.Create(null, BookId, ReaderId, DateTimeOffset.UtcNow.AddDays(-5)).Value;
        var firstReturn = DateTimeOffset.UtcNow.AddDays(-2);
        loan.MarkAsReturned(firstReturn);

        loan.MarkAsReturned(DateTimeOffset.UtcNow);

        Assert.Equal(firstReturn.ToUniversalTime(), loan.ReturnedAtUtc);
    }
}
