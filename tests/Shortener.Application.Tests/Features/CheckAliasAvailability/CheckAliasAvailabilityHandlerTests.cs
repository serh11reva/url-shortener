using Shortener.Application.Abstractions.Exceptions;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.CheckAliasAvailability;
using Shortener.Domain;

namespace Shortener.Application.Tests.Features.CheckAliasAvailability;

public sealed class CheckAliasAvailabilityHandlerTests
{
    [Fact]
    public async Task Handle_InvalidAlias_ThrowsCreateShortUrlValidationException()
    {
        var handler = new CheckAliasAvailabilityHandler(new FakeCache(), new FakeRepository());

        await Assert.ThrowsAsync<CreateShortUrlValidationException>(() =>
            handler.Handle(new CheckAliasAvailabilityQuery("a--b"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CacheKeyExists_ReturnsUnavailableWithoutRepositoryLookup()
    {
        var repository = new FakeRepository();
        var cache = new FakeCache { ExistsResult = true };
        var handler = new CheckAliasAvailabilityHandler(cache, repository);

        var result = await handler.Handle(new CheckAliasAvailabilityQuery("valid-alias"), CancellationToken.None);

        Assert.False(result.Available);
        Assert.Equal(0, repository.GetByShortCodeCallCount);
        Assert.Equal("valid-alias", cache.ExistsAliasArgument);
    }

    [Fact]
    public async Task Handle_CacheMiss_RepositoryMiss_ReturnsAvailable()
    {
        var repository = new FakeRepository { GetByShortCodeResult = null };
        var cache = new FakeCache { ExistsResult = false };
        var handler = new CheckAliasAvailabilityHandler(cache, repository);

        var result = await handler.Handle(new CheckAliasAvailabilityQuery("free-code"), CancellationToken.None);

        Assert.True(result.Available);
        Assert.Equal(1, repository.GetByShortCodeCallCount);
    }

    [Fact]
    public async Task Handle_CacheMiss_RepositoryHit_ReturnsUnavailable()
    {
        var entity = new ShortUrl("taken1", "https://example.com/x", "h", null, DateTime.UtcNow);
        var repository = new FakeRepository { GetByShortCodeResult = entity };
        var cache = new FakeCache { ExistsResult = false };
        var handler = new CheckAliasAvailabilityHandler(cache, repository);

        var result = await handler.Handle(new CheckAliasAvailabilityQuery("taken1"), CancellationToken.None);

        Assert.False(result.Available);
        Assert.Equal(1, repository.GetByShortCodeCallCount);
    }

    private sealed class FakeCache : IShortUrlCache
    {
        public bool ExistsResult { get; set; }
        public string? ExistsAliasArgument { get; private set; }

        public Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<CachedShortUrl?>(null);

        public Task<bool> ExistsAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            ExistsAliasArgument = shortCode;
            return Task.FromResult(ExistsResult);
        }

        public Task SetAsync(string shortCode, CachedShortUrl value, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeRepository : IShortUrlRepository
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

        public Task RecordClickAsync(string shortCode, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(DateTime nowUtc, TimeSpan inactiveFor, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }
}
