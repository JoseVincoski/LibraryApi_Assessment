using LibrarySystem.Contracts.Analytics;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Contracts.Recommendations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LibrarySystem.FunctionalTests.Testing;

public sealed class ApiTestFactory : WebApplicationFactory<IApiMarker>
{
    public FakeAnalyticsGrpcClient AnalyticsClient { get; } = new();
    public FakeReaderMetricsGrpcClient ReaderMetricsClient { get; } = new();
    public FakeRecommendationsGrpcClient RecommendationsClient { get; } = new();
    public FakeLoansGrpcClient LoansClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AnalyticsGrpc.AnalyticsGrpcClient>();
            services.AddSingleton<AnalyticsGrpc.AnalyticsGrpcClient>(AnalyticsClient);

            services.RemoveAll<ReaderMetricsGrpc.ReaderMetricsGrpcClient>();
            services.AddSingleton<ReaderMetricsGrpc.ReaderMetricsGrpcClient>(ReaderMetricsClient);

            services.RemoveAll<RecommendationsGrpc.RecommendationsGrpcClient>();
            services.AddSingleton<RecommendationsGrpc.RecommendationsGrpcClient>(RecommendationsClient);

            services.RemoveAll<LoansGrpc.LoansGrpcClient>();
            services.AddSingleton<LoansGrpc.LoansGrpcClient>(LoansClient);
        });
    }
}
