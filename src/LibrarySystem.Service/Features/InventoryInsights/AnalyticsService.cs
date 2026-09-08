using FluentValidation;
using Grpc.Core;
using LibrarySystem.Contracts.Analytics;
using LibrarySystem.Contracts.Common.Constraints;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Service.Features.InventoryInsights;

public sealed class MostBorrowedRequestValidator : AbstractValidator<MostBorrowedRequest>
{
    public MostBorrowedRequestValidator()
    {
        RuleFor(x => x.DaysLookback).GreaterThan(0).WithMessage("Lookback days must be a positive integer.");
        RuleFor(x => x.MaxResults).InclusiveBetween(PagingConstraints.MinResults, PagingConstraints.MaxResults);
    }
}

public class AnalyticsService : AnalyticsGrpc.AnalyticsGrpcBase
{
    private readonly LibraryDbContext _libraryContext;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(LibraryDbContext libraryContext, ILogger<AnalyticsService> logger)
    {
        _libraryContext = libraryContext;
        _logger = logger;
    }

    public override async Task<MostBorrowedResponse> GetMostBorrowedBooks(MostBorrowedRequest request, ServerCallContext context)
    {
        _logger.LogInformation("{Endpoint}: Generating report with a lookback of {DaysLookback} days, max {MaxResults} results.",
            nameof(GetMostBorrowedBooks), request.DaysLookback, request.MaxResults);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-request.DaysLookback);

        var topBooks = await _libraryContext.Set<Loan>()
            .AsNoTracking()
            .Where(l => l.BorrowedAtUtc >= cutoffDate)
            .Join(_libraryContext.Set<Book>(),
                loan => loan.BookId,
                book => book.Id,
                (loan, book) => new { book.Id, book.Title })
            .GroupBy(x => new { x.Id, x.Title })
            .Select(g => new BookDto
            {
                Id = g.Key.Id.ToString(),
                Title = g.Key.Title,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(request.MaxResults)
            .ToListAsync(context.CancellationToken);

        var response = new MostBorrowedResponse();
        response.Books.AddRange(topBooks);

        _logger.LogInformation("{Endpoint}: Returning {BookCount} books.", nameof(GetMostBorrowedBooks), topBooks.Count);

        return response;
    }
}
