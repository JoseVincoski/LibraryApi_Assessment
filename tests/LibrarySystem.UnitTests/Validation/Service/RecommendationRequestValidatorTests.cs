using FluentValidation.TestHelper;
using LibrarySystem.Contracts.Recommendations;
using LibrarySystem.Service.Features.BorrowingPatterns;

namespace LibrarySystem.UnitTests.Validation.Service;

public class RecommendationRequestValidatorTests
{
    private readonly RecommendationRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void InvalidBookId_HasValidationError(string bookId)
    {
        var request = new RecommendationRequest { BookId = bookId, MaxResults = 3 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BookId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void MaxResults_OutOfRange_HasValidationError(int maxResults)
    {
        var request = new RecommendationRequest { BookId = Guid.NewGuid().ToString(), MaxResults = maxResults };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxResults);
    }

    [Fact]
    public void ValidRequest_HasNoValidationErrors()
    {
        var request = new RecommendationRequest { BookId = Guid.NewGuid().ToString(), MaxResults = 3 };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
