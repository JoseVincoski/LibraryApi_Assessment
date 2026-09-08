using LibrarySystem.Contracts.Analytics;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Contracts.Recommendations;

namespace LibrarySystem.Api.Extensions;

public static class ClientExtensions
{
    public static IServiceCollection AddInfrastructureClients(this IServiceCollection services, string serviceUrl)
    {
        services.AddGrpcClient<AnalyticsGrpc.AnalyticsGrpcClient>(o => o.Address = new Uri(serviceUrl));
        services.AddGrpcClient<ReaderMetricsGrpc.ReaderMetricsGrpcClient>(o => o.Address = new Uri(serviceUrl));
        services.AddGrpcClient<RecommendationsGrpc.RecommendationsGrpcClient>(o => o.Address = new Uri(serviceUrl));
        services.AddGrpcClient<LoansGrpc.LoansGrpcClient>(o => o.Address = new Uri(serviceUrl));

        return services;
    }
}