using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.CheckAliasAvailability;
using Shortener.Application.Features.CreateShortUrl;

namespace Shortener.IntegrationTests;

[Collection("Integration")]
public sealed class AliasAvailabilityIntegrationTests
{
    private readonly ShortenerAppFixture _fixture;

    public AliasAvailabilityIntegrationTests(ShortenerAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GET_availability_ExistingAlias_ReturnsAvailableFalse()
    {
        var alias = $"taken-{Guid.NewGuid():N}"[..32];
        var client = _fixture.Factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/urls",
            new CreateShortUrlRequest("https://example.com/alias-taken", alias));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var availabilityResponse = await client.GetAsync($"/api/aliases/{alias}/availability");

        Assert.Equal(HttpStatusCode.OK, availabilityResponse.StatusCode);
        var body = await availabilityResponse.Content.ReadFromJsonAsync<CheckAliasAvailabilityResult>();
        Assert.NotNull(body);
        Assert.False(body.Available);
    }

    [Fact]
    public async Task GET_availability_UnusedAlias_ReturnsAvailableTrue()
    {
        var alias = $"free-{Guid.NewGuid():N}"[..32];
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/aliases/{alias}/availability");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckAliasAvailabilityResult>();
        Assert.NotNull(body);
        Assert.True(body.Available);
    }

    [Fact]
    public async Task GET_availability_RedisKeyWithoutCosmos_ReturnsAvailableFalse()
    {
        var alias = $"cached-{Guid.NewGuid():N}"[..32];
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IShortUrlCache>();
            await cache.SetAsync(alias, new CachedShortUrl("https://example.com/orphan", null), CancellationToken.None);
        }

        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync($"/api/aliases/{alias}/availability");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CheckAliasAvailabilityResult>();
        Assert.NotNull(body);
        Assert.False(body.Available);
    }

    [Fact]
    public async Task GET_availability_InvalidAlias_Returns400()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/aliases/bad--bad/availability");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alias", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GET_availability_ReservedAlias_Returns400()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/aliases/api/availability");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("reserved", content, StringComparison.OrdinalIgnoreCase);
    }
}
