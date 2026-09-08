using LibrarySystem.Contracts.Common.Results;

namespace LibrarySystem.Service.Domain;

public sealed class Book
{
    private Book(Guid id, string title, string author, int pageCount)
    {
        Id = id;
        Title = title;
        Author = author;
        PageCount = pageCount;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Author { get; private set; }
    public int PageCount { get; private set; }

    public static Result<Book> Create(Guid? id, string title, string author, int pageCount)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Book>(Error.Problem("Book.EmptyTitle", "Title is required."));
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            return Result.Failure<Book>(Error.Problem("Book.EmptyAuthor", "Author is required."));
        }

        if (pageCount <= 0)
        {
            return Result.Failure<Book>(Error.Problem("Book.InvalidPages", "Page count must be greater than zero."));
        }

        return new Book(id ?? Guid.NewGuid(), title.Trim(), author.Trim(), pageCount);
    }
}