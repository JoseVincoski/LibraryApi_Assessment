using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.FunctionalTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.FunctionalTests.Api.UserActivity;

public class GetReadingPaceEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public GetReadingPaceEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ReaderMetricsClient.ReadingPaceFault = null;
    }

    [Fact]
    public async Task WithValidReaderId_ReturnsPaceFromTheService()
    {
        var readerId = Guid.NewGuid();
        _factory.ReaderMetricsClient.ReadingPaceHandler = _ => new ReadingPaceResponse { ReaderName = "Alice", PagesPerDay = 42.5 };

        var response = await _client.GetAsync($"api/readers/{readerId}/reading-pace");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PaceDto>();
        Assert.Equal("Alice", body!.ReaderName);
        Assert.Equal(42.5, body.PagesPerDay);
        Assert.Equal(readerId.ToString(), _factory.ReaderMetricsClient.LastReadingPaceRequest!.ReaderId);
    }

    [Fact]
    public async Task WithNonGuidRouteValue_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/readers/not-a-guid/reading-pace");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithUnknownReaderId_ReturnsNotFound()
    {
        _factory.ReaderMetricsClient.ReadingPaceFault = new RpcException(new Status(StatusCode.NotFound, "Reader was not found."));

        var response = await _client.GetAsync($"api/readers/{Guid.NewGuid()}/reading-pace");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Reader was not found.", problem!.Detail);

        _factory.ReaderMetricsClient.ReadingPaceFault = null;
    }

    private sealed record PaceDto(string ReaderName, double PagesPerDay);
}
