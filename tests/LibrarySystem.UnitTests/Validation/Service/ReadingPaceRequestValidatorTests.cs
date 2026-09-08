using FluentValidation.TestHelper;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Service.Features.UserActivity;

namespace LibrarySystem.UnitTests.Validation.Service;

public class ReadingPaceRequestValidatorTests
{
    private readonly ReadingPaceRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void InvalidReaderId_HasValidationError(string readerId)
    {
        var result = _validator.TestValidate(new ReadingPaceRequest { ReaderId = readerId });

        result.ShouldHaveValidationErrorFor(x => x.ReaderId);
    }

    [Fact]
    public void ValidReaderId_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new ReadingPaceRequest { ReaderId = Guid.NewGuid().ToString() });

        result.ShouldNotHaveAnyValidationErrors();
    }
}
