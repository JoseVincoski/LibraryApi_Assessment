using FluentValidation.TestHelper;
using LibrarySystem.Contracts.Analytics;
using LibrarySystem.Service.Features.InventoryInsights;

namespace LibrarySystem.UnitTests.Validation.Service;

public class MostBorrowedRequestValidatorTests
{
    private readonly MostBorrowedRequestValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DaysLookback_NotPositive_HasValidationError(int daysLookback)
    {
        var request = new MostBorrowedRequest { DaysLookback = daysLookback, MaxResults = 10 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DaysLookback);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void MaxResults_OutOfRange_HasValidationError(int maxResults)
    {
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = maxResults };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxResults);
    }

    [Fact]
    public void ValidRequest_HasNoValidationErrors()
    {
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = 10 };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void MaxResults_AtInclusiveBoundaries_HasNoValidationError(int maxResults)
    {
        var request = new MostBorrowedRequest { DaysLookback = 30, MaxResults = maxResults };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.MaxResults);
    }

    [Fact]
    public void DaysLookback_AtMinimumValidValue_HasNoValidationError()
    {
        var request = new MostBorrowedRequest { DaysLookback = 1, MaxResults = 10 };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.DaysLookback);
    }
}
