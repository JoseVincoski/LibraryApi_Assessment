using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.FunctionalTests.Testing;

namespace LibrarySystem.FunctionalTests.Api.UserActivity;

public class GetMostActiveReadersEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public GetMostActiveReadersEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ReaderMetricsClient.ActiveReadersFault = null;
    }

    [Fact]
    public async Task NoQueryParameters_DefaultsToLast30DaysAndTop10()
    {
        _factory.ReaderMetricsClient.ActiveReadersHandler = _ => new ActiveReadersResponse();
        var before = DateTime.UtcNow;

        var response = await _client.GetAsync("api/readers/most-active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = _factory.ReaderMetricsClient.LastActiveReadersRequest!;
        Assert.Equal(10, request.MaxResults);

        var startDate = DateTimeOffset.Parse(request.StartDateUtc);
        var endDate = DateTimeOffset.Parse(request.EndDateUtc);
        Assert.True(endDate >= before);
        Assert.True((endDate - startDate).TotalDays is > 29.9 and < 30.1);
    }

    [Fact]
    public async Task ExplicitDatesAndMaxResults_AreForwardedAsIso8601()
    {
        _factory.ReaderMetricsClient.ActiveReadersHandler = _ => new ActiveReadersResponse();

        var response = await _client.GetAsync("api/readers/most-active?startDate=2026-01-01&endDate=2026-01-31&maxResults=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = _factory.ReaderMetricsClient.LastActiveReadersRequest!;
        Assert.Equal(25, request.MaxResults);
        Assert.Equal(new DateTime(2026, 1, 1), DateTimeOffset.Parse(request.StartDateUtc).DateTime);
        Assert.Equal(new DateTime(2026, 1, 31), DateTimeOffset.Parse(request.EndDateUtc).DateTime);
    }

    [Fact]
    public async Task ReturnsTheReadersFromTheService()
    {
        var expected = new ActiveReadersResponse();
        expected.Readers.Add(new UserDto { Id = Guid.NewGuid().ToString(), Name = "Alice", BorrowCount = 12 });
        _factory.ReaderMetricsClient.ActiveReadersHandler = _ => expected;

        var response = await _client.GetAsync("api/readers/most-active");

        var readers = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.Single(readers!);
        Assert.Equal("Alice", readers![0].Name);
    }

    [Fact]
    public async Task WhenStartDateIsAfterEndDate_ServiceRejectionBecomesBadRequest()
    {
        _factory.ReaderMetricsClient.ActiveReadersFault = new RpcException(new Status(StatusCode.InvalidArgument, "startDate must not be later than endDate."));

        var response = await _client.GetAsync("api/readers/most-active?startDate=2026-02-01&endDate=2026-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _factory.ReaderMetricsClient.ActiveReadersFault = null;
    }
}
