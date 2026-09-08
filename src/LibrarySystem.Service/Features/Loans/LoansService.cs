using FluentValidation;
using Grpc.Core;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using LibrarySystem.Service.Infrastructure.Persistence;
using Npgsql;

namespace LibrarySystem.Service.Features.Loans;

public sealed class BorrowBookRequestValidator : AbstractValidator<BorrowBookRequest>
{
    public BorrowBookRequestValidator()
    {
        RuleFor(x => x.BookId).Must(BeAValidGuid).WithMessage("A valid Book ID is required.");
        RuleFor(x => x.ReaderId).Must(BeAValidGuid).WithMessage("A valid Reader ID is required.");
    }

    private static bool BeAValidGuid(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty;
}

public sealed class ReturnBookRequestValidator : AbstractValidator<ReturnBookRequest>
{
    public ReturnBookRequestValidator()
    {
        RuleFor(x => x.LoanId).Must(BeAValidGuid).WithMessage("A valid Loan ID is required.");
    }

    private static bool BeAValidGuid(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty;
}

public class LoansService : LoansGrpc.LoansGrpcBase
{
    private readonly LibraryDbContext _libraryContext;
    private readonly ILogger<LoansService> _logger;

    public LoansService(LibraryDbContext libraryContext, ILogger<LoansService> logger)
    {
        _libraryContext = libraryContext;
        _logger = logger;
    }

    public override async Task<BorrowBookResponse> BorrowBook(BorrowBookRequest request, ServerCallContext context)
    {
        var bookId = Guid.Parse(request.BookId);
        var readerId = Guid.Parse(request.ReaderId);

        _logger.LogInformation("{Endpoint}: Reader {ReaderId} attempting to borrow book {BookId}.", nameof(BorrowBook), readerId, bookId);

        var bookExists = await _libraryContext.Set<Book>().AsNoTracking().AnyAsync(b => b.Id == bookId, context.CancellationToken);
        if (!bookExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Book {bookId} was not found."));
        }

        var readerExists = await _libraryContext.Set<Reader>().AsNoTracking().AnyAsync(r => r.Id == readerId, context.CancellationToken);
        if (!readerExists)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Reader {readerId} was not found."));
        }

        var loanResult = Loan.Create(id: null, bookId, readerId, DateTimeOffset.UtcNow);
        if (loanResult.IsFailure)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, loanResult.Error.Description));
        }

        var loan = loanResult.Value;
        _libraryContext.Set<Loan>().Add(loan);

        try
        {
            await _libraryContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsActiveLoanConflict(ex))
        {
            _logger.LogWarning("Book {BookId} is already on loan, rejecting concurrent borrow attempt.", bookId);
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "This book is already borrowed by another reader."));
        }

        _logger.LogInformation("Reader {ReaderId} borrowed book {BookId} as loan {LoanId}.", readerId, bookId, loan.Id);

        return new BorrowBookResponse
        {
            LoanId = loan.Id.ToString(),
            BookId = loan.BookId.ToString(),
            ReaderId = loan.ReaderId.ToString(),
            BorrowedAtUtc = loan.BorrowedAtUtc.ToString("O")
        };
    }

    public override async Task<ReturnBookResponse> ReturnBook(ReturnBookRequest request, ServerCallContext context)
    {
        var loanId = Guid.Parse(request.LoanId);

        _logger.LogInformation("{Endpoint}: Attempting to return loan {LoanId}.", nameof(ReturnBook), loanId);

        var loan = await _libraryContext.Set<Loan>().FirstOrDefaultAsync(l => l.Id == loanId, context.CancellationToken);
        if (loan is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Loan {loanId} was not found."));
        }

        var returnResult = loan.MarkAsReturned(DateTimeOffset.UtcNow);
        if (returnResult.IsFailure)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, returnResult.Error.Description));
        }

        try
        {
            await _libraryContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Loan {LoanId} was modified by another request while returning it.", loanId);
            throw new RpcException(new Status(StatusCode.Aborted, "This loan was already updated by another request. Please retry."));
        }

        _logger.LogInformation("Loan {LoanId} marked as returned.", loanId);

        return new ReturnBookResponse
        {
            LoanId = loan.Id.ToString(),
            ReturnedAtUtc = loan.ReturnedAtUtc!.Value.ToString("O")
        };
    }

    private static bool IsActiveLoanConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx
        && pgEx.ConstraintName == LoanConfiguration.ActiveLoanPerBookIndexName;
}
