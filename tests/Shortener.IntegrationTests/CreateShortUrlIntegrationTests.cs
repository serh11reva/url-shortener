using System.Net;
using System.Net.Http.Json;
using Shortener.Application.Features.CreateShortUrl;

namespace Shortener.IntegrationTests;

[Collection("Integration")]
public sealed class CreateShortUrlIntegrationTests
{
    private readonly ShortenerAppFixture _fixture;

    public CreateShortUrlIntegrationTests(ShortenerAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task POST_api_urls_ValidRequest_Returns201AndShortUrl()
    {
        var client = _fixture.Factory.CreateClient();

        var request = new CreateShortUrlRequest("https://example.com/foo", null);
        var response = await client.PostAsJsonAsync("/api/urls", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateShortUrlResult>();
        Assert.NotNull(result);
        Assert.NotNull(result.ShortCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/api/urls/" + result.ShortCode, location);
    }

    [Fact]
    public async Task POST_api_urls_InvalidUrl_Returns400()
    {
        var client = _fixture.Factory.CreateClient();

        var request = new CreateShortUrlRequest("not-a-valid-url", null);
        var response = await client.PostAsJsonAsync("/api/urls", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("LongUrl", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task POST_api_urls_DuplicateAlias_Returns409()
    {
        var client = _fixture.Factory.CreateClient();

        var request1 = new CreateShortUrlRequest("https://example.com/first", "myAlias");
        var response1 = await client.PostAsJsonAsync("/api/urls", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var request2 = new CreateShortUrlRequest("https://example.com/second", "myAlias");
        var response2 = await client.PostAsJsonAsync("/api/urls", request2);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task POST_api_urls_ConcurrentDuplicateAlias_OneCreatedRestConflict()
    {
        var client = _fixture.Factory.CreateClient();
        const string alias = "concurrentDupAlias";
        var tasks = Enumerable.Range(0, 24).Select(i =>
            client.PostAsJsonAsync(
                "/api/urls",
                new CreateShortUrlRequest($"https://example.com/concurrent-{i}", alias)));
        var responses = await Task.WhenAll(tasks);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(
            responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task POST_api_urls_PastExpiration_Returns400()
    {
        var client = _fixture.Factory.CreateClient();

        var request = new CreateShortUrlRequest(
            "https://example.com/past",
            null,
            DateTime.UtcNow.AddMinutes(-5));
        var response = await client.PostAsJsonAsync("/api/urls", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ExpiresAt", content, StringComparison.OrdinalIgnoreCase);
    }
}
