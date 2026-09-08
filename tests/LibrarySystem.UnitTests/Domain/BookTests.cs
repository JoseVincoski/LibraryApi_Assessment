using LibrarySystem.Service.Domain;

namespace LibrarySystem.UnitTests.Domain;

public class BookTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndTrimsText()
    {
        var result = Book.Create(id: null, title: "  Dune  ", author: "  Frank Herbert  ", pageCount: 412);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dune", result.Value.Title);
        Assert.Equal("Frank Herbert", result.Value.Author);
        Assert.Equal(412, result.Value.PageCount);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void Create_WithExplicitId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var result = Book.Create(id, "Dune", "Frank Herbert", 412);

        Assert.Equal(id, result.Value.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_ReturnsFailure(string? title)
    {
        var result = Book.Create(null, title!, "Frank Herbert", 412);

        Assert.True(result.IsFailure);
        Assert.Equal("Book.EmptyTitle", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankAuthor_ReturnsFailure(string? author)
    {
        var result = Book.Create(null, "Dune", author!, 412);

        Assert.True(result.IsFailure);
        Assert.Equal("Book.EmptyAuthor", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePageCount_ReturnsFailure(int pageCount)
    {
        var result = Book.Create(null, "Dune", "Frank Herbert", pageCount);

        Assert.True(result.IsFailure);
        Assert.Equal("Book.InvalidPages", result.Error.Code);
    }
}
