using LibrarySystem.Contracts.Common.Results;

namespace LibrarySystem.UnitTests.Common;

public class ErrorTests
{
    [Fact]
    public void None_HasEmptyCodeAndDescription()
    {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Description);
        Assert.Equal(ErrorType.Failure, Error.None.Type);
    }

    [Theory]
    [InlineData("Book.InvalidPages", "Pages must be positive.")]
    public void Problem_ProducesProblemType(string code, string description)
    {
        var error = Error.Problem(code, description);

        Assert.Equal(code, error.Code);
        Assert.Equal(description, error.Description);
        Assert.Equal(ErrorType.Problem, error.Type);
    }

    [Fact]
    public void TwoErrorsWithSameValues_AreEqual()
    {
        var first = Error.Problem("Same.Code", "Same description.");
        var second = Error.Problem("Same.Code", "Same description.");

        Assert.Equal(first, second);
    }

    [Fact]
    public void TwoErrorsWithDifferentCodes_AreNotEqual()
    {
        var first = Error.Problem("Code.One", "Description.");
        var second = Error.Problem("Code.Two", "Description.");

        Assert.NotEqual(first, second);
    }
}
