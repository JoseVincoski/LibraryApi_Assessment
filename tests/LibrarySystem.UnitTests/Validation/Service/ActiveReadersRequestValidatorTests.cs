using FluentValidation.TestHelper;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Service.Features.UserActivity;

namespace LibrarySystem.UnitTests.Validation.Service;

public class ActiveReadersRequestValidatorTests
{
    private readonly ActiveReadersRequestValidator _validator = new();

    [Fact]
    public void InvalidStartDate_HasValidationError()
    {
        var request = new ActiveReadersRequest
        {
            StartDateUtc = "not-a-date",
            EndDateUtc = DateTimeOffset.UtcNow.ToString("O"),
            MaxResults = 10
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.StartDateUtc);
    }

    [Fact]
    public void InvalidEndDate_HasValidationError()
    {
        var request = new ActiveReadersRequest
        {
            StartDateUtc = DateTimeOffset.UtcNow.ToString("O"),
            EndDateUtc = "not-a-date",
            MaxResults = 10
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.EndDateUtc);
    }

    [Fact]
    public void StartDateAfterEndDate_HasValidationError()
    {
        var request = new ActiveReadersRequest
        {
            StartDateUtc = DateTimeOffset.UtcNow.ToString("O"),
            EndDateUtc = DateTimeOffset.UtcNow.AddDays(-1).ToString("O"),
            MaxResults = 10
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DateRange");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void MaxResults_OutOfRange_HasValidationError(int maxResults)
    {
        var request = new ActiveReadersRequest
        {
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-30).ToString("O"),
            EndDateUtc = DateTimeOffset.UtcNow.ToString("O"),
            MaxResults = maxResults
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxResults);
    }

    [Fact]
    public void ValidRequest_HasNoValidationErrors()
    {
        var request = new ActiveReadersRequest
        {
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-30).ToString("O"),
            EndDateUtc = DateTimeOffset.UtcNow.ToString("O"),
            MaxResults = 10
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void StartDateEqualToEndDate_IsValid()
    {
        // Boundary: a zero-width window is unusual but not invalid. The rule only rejects startDate strictly after endDate.
        var sameInstant = DateTimeOffset.UtcNow.ToString("O");
        var request = new ActiveReadersRequest
        {
            StartDateUtc = sameInstant,
            EndDateUtc = sameInstant,
            MaxResults = 10
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
