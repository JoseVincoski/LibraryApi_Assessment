using FluentValidation.TestHelper;
using LibrarySystem.Api.Features.Loans;

namespace LibrarySystem.UnitTests.Validation.Api;

public class BorrowBookRequestValidatorTests
{
    private readonly BorrowBookRequestValidator _validator = new();

    [Fact]
    public void EmptyBookId_HasValidationError()
    {
        var request = new BorrowBookRequest(Guid.Empty, Guid.NewGuid());

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BookId);
    }

    [Fact]
    public void EmptyReaderId_HasValidationError()
    {
        var request = new BorrowBookRequest(Guid.NewGuid(), Guid.Empty);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ReaderId);
    }

    [Fact]
    public void ValidRequest_HasNoValidationErrors()
    {
        var request = new BorrowBookRequest(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
