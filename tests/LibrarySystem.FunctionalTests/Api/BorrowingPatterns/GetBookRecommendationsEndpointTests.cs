using System.Net;
using System.Net.Http.Json;
using Grpc.Core;
using LibrarySystem.Contracts.Recommendations;
using LibrarySystem.FunctionalTests.Testing;

namespace LibrarySystem.FunctionalTests.Api.BorrowingPatterns;

public class GetBookRecommendationsEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public GetBookRecommendationsEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.RecommendationsClient.Fault = null;
    }

    [Fact]
    public async Task WithValidBookId_ReturnsTitlesFromTheService()
    {
        var bookId = Guid.NewGuid();
        var expected = new RecommendationResponse();
        expected.RecommendedBookTitles.Add("Dune");
        _factory.RecommendationsClient.Handler = _ => expected;

        var response = await _client.GetAsync($"api/books/{bookId}/borrowing-patterns");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var titles = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Equal(["Dune"], titles);
        Assert.Equal(bookId.ToString(), _factory.RecommendationsClient.LastRequest!.BookId);
        Assert.Equal(3, _factory.RecommendationsClient.LastRequest!.MaxResults);
    }

    [Fact]
    public async Task MaxResultsQueryParameter_OverridesTheDefaultOfThree()
    {
        _factory.RecommendationsClient.Handler = _ => new RecommendationResponse();

        var response = await _client.GetAsync($"api/books/{Guid.NewGuid()}/borrowing-patterns?maxResults=8");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, _factory.RecommendationsClient.LastRequest!.MaxResults);
    }

    [Fact]
    public async Task WithNonGuidRouteValue_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/books/not-a-guid/borrowing-patterns");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WhenBookDoesNotExist_PropagatesWhateverTheServiceReturns()
    {
        // GetBookRecommendations never throws NotFound for an unknown book (an
        // unknown ID just naturally produces zero co-borrowers) - this documents
        // that returning an empty list is the expected behavior, not an error.
        _factory.RecommendationsClient.Handler = _ => new RecommendationResponse();

        var response = await _client.GetAsync($"api/books/{Guid.NewGuid()}/borrowing-patterns");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var titles = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Empty(titles!);
    }
}
