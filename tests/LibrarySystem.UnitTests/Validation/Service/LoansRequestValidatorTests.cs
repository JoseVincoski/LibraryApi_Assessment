using FluentValidation.TestHelper;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Service.Features.Loans;

namespace LibrarySystem.UnitTests.Validation.Service;

public class BorrowBookRequestValidatorTests
{
    private readonly BorrowBookRequestValidator _validator = new();

    [Theory]
    [InlineData("", "")]
    [InlineData("not-a-guid", "00000000-0000-0000-0000-000000000000")]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000001")]
    public void InvalidIds_HaveValidationErrors(string bookId, string readerId)
    {
        var result = _validator.TestValidate(new BorrowBookRequest { BookId = bookId, ReaderId = readerId });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidRequest_HasNoValidationErrors()
    {
        var request = new BorrowBookRequest { BookId = Guid.NewGuid().ToString(), ReaderId = Guid.NewGuid().ToString() };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class ReturnBookRequestValidatorTests
{
    private readonly ReturnBookRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void InvalidLoanId_HasValidationError(string loanId)
    {
        var result = _validator.TestValidate(new ReturnBookRequest { LoanId = loanId });

        result.ShouldHaveValidationErrorFor(x => x.LoanId);
    }

    [Fact]
    public void ValidLoanId_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new ReturnBookRequest { LoanId = Guid.NewGuid().ToString() });

        result.ShouldNotHaveAnyValidationErrors();
    }
}
