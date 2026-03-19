using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Redirect;
using Shortener.Domain;

namespace Shortener.Application.Tests.Features.Redirect;

public sealed class GetRedirectTargetHandlerTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsRedirectWithoutRepositoryLookup()
    {
        var repository = new FakeShortUrlRepository();
        var cache = new FakeShortUrlCache
        {
            CachedResult = new CachedShortUrl("https://example.com/cache-hit", null)
        };
        var handler = new GetRedirectTargetHandler(repository, cache);

        var result = await handler.Handle(new GetRedirectTargetQuery("abc123"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("https://example.com/cache-hit", result.LongUrl);
        Assert.Equal(0, repository.GetByShortCodeCallCount);
    }

    [Fact]
    public async Task Handle_CacheMiss_RepositoryHit_PrimesCacheAndReturnsRedirect()
    {
        var repository = new FakeShortUrlRepository
        {
            GetByShortCodeResult = new ShortUrl("code01", "https://example.com/db", "hash", null, DateTime.UtcNow)
        };
        var cache = new FakeShortUrlCache();
        var handler = new GetRedirectTargetHandler(repository, cache);

        var result = await handler.Handle(new GetRedirectTargetQuery("code01"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("https://example.com/db", result.LongUrl);
        Assert.Single(cache.SetCalls);
        Assert.Equal("code01", cache.SetCalls[0].shortCode);
    }

    [Fact]
    public async Task Handle_ExpiredLink_ReturnsNotFound()
    {
        var repository = new FakeShortUrlRepository
        {
            GetByShortCodeResult = new ShortUrl(
                "expired1",
                "https://example.com/expired",
                "hash",
                null,
                DateTime.UtcNow.AddDays(-31),
                DateTime.UtcNow.AddMinutes(-5))
        };
        var cache = new FakeShortUrlCache();
        var handler = new GetRedirectTargetHandler(repository, cache);

        var result = await handler.Handle(new GetRedirectTargetQuery("expired1"), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Empty(cache.SetCalls);
    }

    private sealed class FakeShortUrlRepository : IShortUrlRepository
    {
        public int GetByShortCodeCallCount { get; private set; }
        public ShortUrl? GetByShortCodeResult { get; set; }

        public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            GetByShortCodeCallCount++;
            return Task.FromResult(GetByShortCodeResult);
        }

        public Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeShortUrlCache : IShortUrlCache
    {
        public CachedShortUrl? CachedResult { get; set; }
        public List<(string shortCode, string longUrl, DateTime? expiresAt)> SetCalls { get; } = [];

        public Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CachedResult);
        }

        public Task SetAsync(string shortCode, string longUrl, DateTime? expiresAt, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            SetCalls.Add((shortCode, longUrl, expiresAt));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
