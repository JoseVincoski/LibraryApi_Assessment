using FluentValidation;
using Grpc.Core;
using LibrarySystem.Contracts.Common.Constraints;
using LibrarySystem.Contracts.Recommendations;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Service.Features.BorrowingPatterns;

public sealed class RecommendationRequestValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationRequestValidator()
    {
        RuleFor(x => x.BookId).Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid Book ID format.");
        RuleFor(x => x.MaxResults).InclusiveBetween(PagingConstraints.MinResults, PagingConstraints.MaxResults);
    }
}

public class RecommendationService : RecommendationsGrpc.RecommendationsGrpcBase
{
    private readonly LibraryDbContext _libraryContext;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(LibraryDbContext libraryContext, ILogger<RecommendationService> logger)
    {
        _libraryContext = libraryContext;
        _logger = logger;
    }

    public override async Task<RecommendationResponse> GetBookRecommendations(RecommendationRequest request, ServerCallContext context)
    {
        _logger.LogInformation("{Endpoint}: Computing affinity patterns for book {BookId}, max {MaxResults} results.",
            nameof(GetBookRecommendations), request.BookId, request.MaxResults);

        var targetBookId = Guid.Parse(request.BookId);

        var readerIds = _libraryContext.Set<Loan>()
            .Where(l => l.BookId == targetBookId)
            .Select(l => l.ReaderId);

        var recommendedTitles = await _libraryContext.Set<Loan>()
            .AsNoTracking()
            .Where(l => readerIds.Contains(l.ReaderId) && l.BookId != targetBookId)
            .Join(_libraryContext.Set<Book>(), l => l.BookId, b => b.Id, (l, b) => b.Title)
            .GroupBy(title => title)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(request.MaxResults)
            .ToListAsync(context.CancellationToken);

        var response = new RecommendationResponse();
        response.RecommendedBookTitles.AddRange(recommendedTitles);

        _logger.LogInformation("{Endpoint}: Found {RecommendationCount} recommendations for book {BookId}.",
            nameof(GetBookRecommendations), recommendedTitles.Count, targetBookId);

        return response;
    }
}
