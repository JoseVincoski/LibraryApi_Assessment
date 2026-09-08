using LibrarySystem.Contracts.Common.Results;

namespace LibrarySystem.UnitTests.Common;

public class ResultTests
{
    private static readonly Error SampleError = Error.Problem("Sample.Code", "Sample description.");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_CarriesTheGivenError()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void Constructing_SuccessWithAnError_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Result(true, SampleError));
    }

    [Fact]
    public void Constructing_FailureWithoutAnError_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Result(false, Error.None));
    }

    [Fact]
    public void GenericSuccess_ExposesTheValue()
    {
        var result = Result.Success("hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void GenericFailure_ThrowsWhenAccessingValue()
    {
        var result = Result.Failure<string>(SampleError);

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromNonNullValue_IsSuccess()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromNull_IsFailureWithNullValueError()
    {
        Result<string> result = (string?)null;

        Assert.True(result.IsFailure);
        Assert.Equal(Error.NullValue, result.Error);
    }
}
