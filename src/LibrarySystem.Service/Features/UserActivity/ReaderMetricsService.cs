using FluentValidation;
using Grpc.Core;
using LibrarySystem.Contracts.Common.Constraints;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Service.Domain;
using LibrarySystem.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Service.Features.UserActivity;

public sealed class ActiveReadersRequestValidator : AbstractValidator<ActiveReadersRequest>
{
    public ActiveReadersRequestValidator()
    {
        RuleFor(x => x.StartDateUtc).Must(BeAValidDate).WithMessage("Invalid date format. Use ISO 8601.");
        RuleFor(x => x.EndDateUtc).Must(BeAValidDate).WithMessage("Invalid date format. Use ISO 8601.");
        RuleFor(x => x.MaxResults).InclusiveBetween(PagingConstraints.MinResults, PagingConstraints.MaxResults);

        RuleFor(x => x)
            .Must(x => DateTimeOffset.Parse(x.StartDateUtc) <= DateTimeOffset.Parse(x.EndDateUtc))
            .WithMessage("startDate must not be later than endDate.")
            .WithName("DateRange")
            .When(x => BeAValidDate(x.StartDateUtc) && BeAValidDate(x.EndDateUtc));
    }

    private static bool BeAValidDate(string value) => DateTimeOffset.TryParse(value, out _);
}

public sealed class ReadingPaceRequestValidator : AbstractValidator<ReadingPaceRequest>
{
    public ReadingPaceRequestValidator()
    {
        RuleFor(x => x.ReaderId).Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid Reader ID format.");
    }
}

public class ReaderMetricsService : ReaderMetricsGrpc.ReaderMetricsGrpcBase
{
    private readonly LibraryDbContext _libraryContext;
    private readonly ILogger<ReaderMetricsService> _logger;

    public ReaderMetricsService(LibraryDbContext libraryContext, ILogger<ReaderMetricsService> logger)
    {
        _libraryContext = libraryContext;
        _logger = logger;
    }

    public override async Task<ActiveReadersResponse> GetMostActiveReaders(ActiveReadersRequest request, ServerCallContext context)
    {
        _logger.LogInformation("{Endpoint}: Analyzing reader activity between {StartDate} and {EndDate}, max {MaxResults} results.",
            nameof(GetMostActiveReaders), request.StartDateUtc, request.EndDateUtc, request.MaxResults);

        var startDate = DateTimeOffset.Parse(request.StartDateUtc);
        var endDate = DateTimeOffset.Parse(request.EndDateUtc);

        var topReaders = await _libraryContext.Set<Loan>()
            .AsNoTracking()
            .Where(l => l.BorrowedAtUtc >= startDate && l.BorrowedAtUtc <= endDate)
            .Join(_libraryContext.Set<Reader>(),
                loan => loan.ReaderId,
                reader => reader.Id,
                (loan, reader) => new { reader.Id, reader.Name })
            .GroupBy(x => new { x.Id, x.Name })
            .Select(g => new UserDto
            {
                Id = g.Key.Id.ToString(),
                Name = g.Key.Name,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(request.MaxResults)
            .ToListAsync(context.CancellationToken);

        var response = new ActiveReadersResponse();
        response.Readers.AddRange(topReaders);

        _logger.LogInformation("{Endpoint}: Returning {ReaderCount} readers.", nameof(GetMostActiveReaders), topReaders.Count);

        return response;
    }

    public override async Task<ReadingPaceResponse> GetReadingPace(ReadingPaceRequest request, ServerCallContext context)
    {
        _logger.LogInformation("{Endpoint}: Calculating pace for reader {ReaderId}.", nameof(GetReadingPace), request.ReaderId);

        var readerId = Guid.Parse(request.ReaderId);

        var readerData = await _libraryContext.Set<Reader>()
            .AsNoTracking()
            .Where(m => m.Id == readerId)
            .Select(m => new {
                m.Name,
                Loans = _libraryContext.Set<Loan>()
                    .Where(l => l.ReaderId == m.Id && l.ReturnedAtUtc != null)
                    .Join(_libraryContext.Set<Book>(), l => l.BookId, b => b.Id, (l, b) => new {
                        b.PageCount,
                        Days = (l.ReturnedAtUtc!.Value - l.BorrowedAtUtc).TotalDays
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (readerData == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Reader with ID {readerId} not found."));
        }

        var totalPages = readerData.Loans.Sum(s => s.PageCount);
        var totalDays = readerData.Loans.Sum(s => Math.Max(s.Days, 1.0));
        var pagesPerDay = totalDays > 0 ? Math.Round(totalPages / totalDays, 2) : 0;

        _logger.LogInformation("{Endpoint}: Reader {ReaderId} reads {PagesPerDay} pages/day across {LoanCount} returned loans.",
            nameof(GetReadingPace), readerId, pagesPerDay, readerData.Loans.Count);

        return new ReadingPaceResponse
        {
            ReaderName = readerData.Name,
            PagesPerDay = pagesPerDay
        };
    }
}
