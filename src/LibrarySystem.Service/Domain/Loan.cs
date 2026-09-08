using LibrarySystem.Contracts.Common.Results;

namespace LibrarySystem.Service.Domain;

public sealed class Loan
{
    private Loan(Guid id, Guid bookId, Guid readerId, DateTimeOffset borrowedAtUtc)
    {
        Id = id;
        BookId = bookId;
        ReaderId = readerId;
        BorrowedAtUtc = borrowedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid ReaderId { get; private set; }
    public DateTimeOffset BorrowedAtUtc { get; private set; }
    public DateTimeOffset? ReturnedAtUtc { get; private set; }

    public static Result<Loan> Create(Guid? id, Guid bookId, Guid reader, DateTimeOffset borrowedAtUtc)
    {
        if (bookId == Guid.Empty)
        {
            return Result.Failure<Loan>(Error.Problem("Loan.EmptyBookId", "Book ID is required."));
        }

        if (reader == Guid.Empty)
        {
            return Result.Failure<Loan>(Error.Problem("Loan.EmptyReaderId", "Reader ID is required."));
        }

        return new Loan(id ?? Guid.NewGuid(), bookId, reader, borrowedAtUtc.ToUniversalTime());
    }

    public Result MarkAsReturned(DateTimeOffset returnedAtUtc)
    {
        if (ReturnedAtUtc is not null)
        {
            return Result.Failure(Error.Problem("Loan.AlreadyReturned", "This loan has already been returned."));
        }

        if (returnedAtUtc.ToUniversalTime() < BorrowedAtUtc)
        {
            return Result.Failure(Error.Problem("Loan.InvalidReturnDate", "Return date cannot be earlier than borrow date."));
        }

        ReturnedAtUtc = returnedAtUtc.ToUniversalTime();

        return Result.Success();
    }
}