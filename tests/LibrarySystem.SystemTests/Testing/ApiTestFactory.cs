using LibrarySystem.Contracts.Analytics;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Contracts.Recommendations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.SystemTests.Testing;

public sealed class ApiTestFactory(ServiceTestFactory serviceFactory) : WebApplicationFactory<IApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddGrpcClient<AnalyticsGrpc.AnalyticsGrpcClient>(o => o.Address = new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(() => serviceFactory.Server.CreateHandler());

            services.AddGrpcClient<ReaderMetricsGrpc.ReaderMetricsGrpcClient>(o => o.Address = new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(() => serviceFactory.Server.CreateHandler());

            services.AddGrpcClient<RecommendationsGrpc.RecommendationsGrpcClient>(o => o.Address = new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(() => serviceFactory.Server.CreateHandler());

            services.AddGrpcClient<LoansGrpc.LoansGrpcClient>(o => o.Address = new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(() => serviceFactory.Server.CreateHandler());
        });
    }
}
