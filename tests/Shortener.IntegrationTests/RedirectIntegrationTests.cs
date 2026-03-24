using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Shortener.Application.Abstractions.ShortUrls;
using StackExchange.Redis;

namespace Shortener.IntegrationTests;

[Collection("Integration")]
public sealed class RedirectIntegrationTests
{
    private readonly ShortenerAppFixture _fixture;

    public RedirectIntegrationTests(ShortenerAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GET_shortCode_CacheHit_ReturnsRedirect()
    {
        var shortCode = $"hit{Guid.NewGuid():N}"[..10];
        var longUrl = "https://example.com/cache-hit";

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IShortUrlCache>();
            await cache.SetAsync(shortCode, new CachedShortUrl(longUrl, null), CancellationToken.None);
        }

        var client = _fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync($"/{shortCode}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(longUrl, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GET_shortCode_CacheMiss_LoadsFromCosmosAndRedirects()
    {
        var shortCode = $"miss{Guid.NewGuid():N}"[..11];
        var longUrl = "https://example.com/cache-miss";

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var container = services.GetRequiredService<Container>();
            var redis = services.GetRequiredService<IConnectionMultiplexer>();

            await container.CreateItemAsync(
                new
                {
                    id = shortCode,
                    pk = shortCode,
                    longUrl,
                    longUrlHash = "hash",
                    alias = (string?)null,
                    createdAt = DateTime.UtcNow,
                    expiresAt = (DateTime?)null
                },
                new PartitionKey(shortCode));

            await redis.GetDatabase().KeyDeleteAsync("short:" + shortCode);
        }

        var client = _fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync($"/{shortCode}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(longUrl, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GET_shortCode_ExpiredLink_Returns404()
    {
        var shortCode = $"exp{Guid.NewGuid():N}"[..10];

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var container = services.GetRequiredService<Container>();
            var redis = services.GetRequiredService<IConnectionMultiplexer>();

            await container.CreateItemAsync(
                new
                {
                    id = shortCode,
                    pk = shortCode,
                    longUrl = "https://example.com/expired",
                    longUrlHash = "hash",
                    alias = (string?)null,
                    createdAt = DateTime.UtcNow.AddDays(-40),
                    expiresAt = DateTime.UtcNow.AddMinutes(-1)
                },
                new PartitionKey(shortCode));

            await redis.GetDatabase().KeyDeleteAsync("short:" + shortCode);
        }

        var client = _fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync($"/{shortCode}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_shortCode_InactiveLink_Returns404()
    {
        var shortCode = $"ina{Guid.NewGuid():N}"[..10];

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var container = services.GetRequiredService<Container>();
            var redis = services.GetRequiredService<IConnectionMultiplexer>();

            await container.CreateItemAsync(
                new
                {
                    id = shortCode,
                    pk = shortCode,
                    longUrl = "https://example.com/inactive",
                    longUrlHash = "hash",
                    alias = (string?)null,
                    createdAt = DateTime.UtcNow.AddDays(-40),
                    expiresAt = (DateTime?)null,
                    lastAccessedAt = DateTime.UtcNow.AddDays(-31)
                },
                new PartitionKey(shortCode));

            await redis.GetDatabase().KeyDeleteAsync("short:" + shortCode);
        }

        var client = _fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync($"/{shortCode}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
