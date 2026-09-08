using LibrarySystem.Service.Features.BorrowingPatterns;
using LibrarySystem.Service.Features.InventoryInsights;
using LibrarySystem.Service.Features.Loans;
using LibrarySystem.Service.Features.UserActivity;

namespace LibrarySystem.Service.Extensions;

public static class GrpcExtensions
{
    public static void MapGrpcServices(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AnalyticsService>();
        endpoints.MapGrpcService<ReaderMetricsService>();
        endpoints.MapGrpcService<RecommendationService>();
        endpoints.MapGrpcService<LoansService>();
    }
}