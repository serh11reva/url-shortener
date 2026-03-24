using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shortener.Application.Features.Analytics;
using Shortener.Application.Features.CreateShortUrl;

namespace Shortener.IntegrationTests;

/// <summary>
/// Analytics over HTTP: redirect path publishes clicks; test queue applies <see cref="RecordClickCommand"/> like the Functions host.
/// Includes polling for stats to match production eventual-consistency expectations.
/// </summary>
[Collection("Integration")]
public sealed class AnalyticsIntegrationTests
{
    private static readonly TimeSpan StatsWait = TimeSpan.FromSeconds(15);

    private readonly ShortenerAppFixture _fixture;

    public AnalyticsIntegrationTests(ShortenerAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GET_stats_UnknownShortCode_Returns404()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/api/urls/does-not-exist-xyz/stats");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task E2E_CreateThenRedirects_ThenStats_ShowsClickCountAndLastAccessed()
    {
        var client = _fixture.Factory.CreateClient();
        var redirectClient = _fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var longUrl = $"https://example.com/analytics-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest(longUrl, null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateShortUrlResult>();
        Assert.NotNull(created?.ShortCode);
        var shortCode = created.ShortCode;

        const int redirectCount = 3;
        for (var i = 0; i < redirectCount; i++)
        {
            var redirectResponse = await redirectClient.GetAsync($"/{shortCode}");
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
            Assert.Equal(longUrl, redirectResponse.Headers.Location?.ToString());
        }

        var stats = await WaitForStatsAsync(client, shortCode, minClickCount: redirectCount, StatsWait);
        Assert.NotNull(stats);
        Assert.Equal(redirectCount, stats.ClickCount);
        Assert.NotNull(stats.LastAccessed);
    }

    [Fact]
    public async Task RecordClick_SameClickIdTwice_CountsOnce()
    {
        var client = _fixture.Factory.CreateClient();
        var longUrl = $"https://example.com/dedup-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest(longUrl, null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateShortUrlResult>();
        Assert.NotNull(created?.ShortCode);
        var shortCode = created.ShortCode;

        _ = _fixture.Factory.Server;

        var scopeFactory = _fixture.Factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var clickId = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        await mediator.Send(new RecordClickCommand(shortCode, at, clickId), CancellationToken.None);
        await mediator.Send(new RecordClickCommand(shortCode, at, clickId), CancellationToken.None);

        var stats = await WaitForStatsAsync(client, shortCode, minClickCount: 1, StatsWait);
        Assert.NotNull(stats);
        Assert.Equal(1L, stats.ClickCount);
    }

    [Fact]
    public async Task Stats_AfterCreate_BeforeClicks_ShowsZeroClicks()
    {
        var client = _fixture.Factory.CreateClient();
        var longUrl = $"https://example.com/preclick-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/urls", new CreateShortUrlRequest(longUrl, null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateShortUrlResult>();
        Assert.NotNull(created?.ShortCode);

        var statsResponse = await client.GetAsync($"/api/urls/{created.ShortCode}/stats");
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        var stats = await statsResponse.Content.ReadFromJsonAsync<GetAnalyticsResult>();
        Assert.NotNull(stats);
        Assert.Equal(0L, stats.ClickCount);
        Assert.Null(stats.LastAccessed);
    }

    private static async Task<GetAnalyticsResult?> WaitForStatsAsync(
        HttpClient client,
        string shortCode,
        long minClickCount,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/urls/{shortCode}/stats");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var stats = await response.Content.ReadFromJsonAsync<GetAnalyticsResult>();
                if (stats is not null && stats.ClickCount >= minClickCount)
                {
                    return stats;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return null;
    }
}
