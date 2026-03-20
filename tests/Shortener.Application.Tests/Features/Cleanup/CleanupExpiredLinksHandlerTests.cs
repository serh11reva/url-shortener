using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Cleanup;
using Shortener.Domain;

namespace Shortener.Application.Tests.Features.Cleanup;

public sealed class CleanupExpiredLinksHandlerTests
{
    [Fact]
    public async Task Handle_RemovesCacheForEachDeletedLink()
    {
        var repository = new FakeShortUrlRepository
        {
            DeleteExpiredAndInactiveResult = ["a1", "b2", "c3"]
        };
        var cache = new FakeShortUrlCache();
        var handler = new CleanupExpiredLinksHandler(repository, cache, new FakeLifecyclePolicy());

        var result = await handler.Handle(new CleanupExpiredLinksCommand(), CancellationToken.None);

        Assert.Equal(3, result.RemovedCount);
        Assert.Equal(["a1", "b2", "c3"], cache.RemovedShortCodes);
    }

    private sealed class FakeShortUrlRepository : IShortUrlRepository
    {
        public IReadOnlyCollection<string> DeleteExpiredAndInactiveResult { get; set; } = Array.Empty<string>();

        public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task MarkAccessedAsync(string shortCode, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(DateTime nowUtc, TimeSpan inactiveFor, CancellationToken cancellationToken = default)
            => Task.FromResult(DeleteExpiredAndInactiveResult);
    }

    private sealed class FakeShortUrlCache : IShortUrlCache
    {
        public List<string> RemovedShortCodes { get; } = [];

        public Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<CachedShortUrl?>(null);

        public Task SetAsync(string shortCode, CachedShortUrl value, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            RemovedShortCodes.Add(shortCode);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLifecyclePolicy : IShortUrlLifecyclePolicy
    {
        public TimeSpan InactiveLinkThreshold => TimeSpan.FromDays(30);
    }
}
