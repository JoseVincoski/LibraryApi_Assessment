using LibrarySystem.Service.Domain;

namespace LibrarySystem.UnitTests.Domain;

public class ReaderTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndNormalizesEmail()
    {
        var result = Reader.Create(id: null, name: "  Alice Reviewer  ", email: "  Alice.Reviewer@Example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice Reviewer", result.Value.Name);
        Assert.Equal("alice.reviewer@example.com", result.Value.Email);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void Create_WithExplicitId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var result = Reader.Create(id, "Alice Reviewer", "alice@example.com");

        Assert.Equal(id, result.Value.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ReturnsFailure(string? name)
    {
        var result = Reader.Create(null, name!, "alice@example.com");

        Assert.True(result.IsFailure);
        Assert.Equal("Reader.EmptyName", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Create_WithInvalidEmail_ReturnsFailure(string? email)
    {
        var result = Reader.Create(null, "Alice Reviewer", email!);

        Assert.True(result.IsFailure);
        Assert.Equal("Reader.InvalidEmail", result.Error.Code);
    }
}
