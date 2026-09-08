using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using LibrarySystem.Contracts.Analytics;
using LibrarySystem.FunctionalTests.Testing;

namespace LibrarySystem.FunctionalTests.Api.InventoryInsights;

public class GetMostBorrowedBooksEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public GetMostBorrowedBooksEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.AnalyticsClient.Fault = null;
    }

    [Fact]
    public async Task NoQueryParameters_UsesDocumentedDefaults()
    {
        _factory.AnalyticsClient.Handler = _ => new MostBorrowedResponse();

        var response = await _client.GetAsync("api/inventory/most-borrowed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(30, _factory.AnalyticsClient.LastRequest!.DaysLookback);
        Assert.Equal(10, _factory.AnalyticsClient.LastRequest!.MaxResults);
    }

    [Fact]
    public async Task ExplicitQueryParameters_AreForwardedAsIs()
    {
        _factory.AnalyticsClient.Handler = _ => new MostBorrowedResponse();

        var response = await _client.GetAsync("api/inventory/most-borrowed?daysLookback=7&maxResults=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(7, _factory.AnalyticsClient.LastRequest!.DaysLookback);
        Assert.Equal(5, _factory.AnalyticsClient.LastRequest!.MaxResults);
    }

    [Fact]
    public async Task ReturnsTheBooksFromTheService()
    {
        var expected = new MostBorrowedResponse();
        expected.Books.Add(new BookDto { Id = Guid.NewGuid().ToString(), Title = "Dune", BorrowCount = 42 });
        _factory.AnalyticsClient.Handler = _ => expected;

        var response = await _client.GetAsync("api/inventory/most-borrowed");

        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>();
        Assert.Single(books!);
        Assert.Equal("Dune", books![0].Title);
        Assert.Equal(42, books[0].BorrowCount);
    }

    [Fact]
    public async Task WhenMaxResultsIsOutOfRange_ServiceRejectionBecomesBadRequest()
    {
        _factory.AnalyticsClient.Fault = new RpcException(new Status(StatusCode.InvalidArgument, "MaxResults must be between 1 and 100."));

        var response = await _client.GetAsync("api/inventory/most-borrowed?maxResults=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _factory.AnalyticsClient.Fault = null;
    }
}
