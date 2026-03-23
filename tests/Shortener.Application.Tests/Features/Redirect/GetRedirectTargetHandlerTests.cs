using MediatR;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Analytics;
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
        var publisher = new FakePublisher();
        var handler = new GetRedirectTargetHandler(
            repository,
            cache,
            new FakeLifecyclePolicy(),
            publisher);

        var result = await handler.Handle(new GetRedirectTargetQuery("abc123"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("https://example.com/cache-hit", result.LongUrl);
        Assert.Equal(0, repository.GetByShortCodeCallCount);
        Assert.Single(publisher.Notifications);
        Assert.Equal("abc123", ((ClickTrackedNotification)publisher.Notifications[0]).ShortCode);
    }

    [Fact]
    public async Task Handle_CacheMiss_RepositoryHit_ReturnsRedirect()
    {
        var repository = new FakeShortUrlRepository
        {
            GetByShortCodeResult = new ShortUrl("code01", "https://example.com/db", "hash", null, DateTime.UtcNow)
        };
        var cache = new FakeShortUrlCache();
        var publisher = new FakePublisher();
        var handler = new GetRedirectTargetHandler(
            repository,
            cache,
            new FakeLifecyclePolicy(),
            publisher);

        var result = await handler.Handle(new GetRedirectTargetQuery("code01"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("https://example.com/db", result.LongUrl);
        Assert.Empty(cache.SetCalls);
        Assert.Single(publisher.Notifications);
        Assert.Equal("code01", ((ClickTrackedNotification)publisher.Notifications[0]).ShortCode);
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
        var publisher = new FakePublisher();
        var handler = new GetRedirectTargetHandler(
            repository,
            cache,
            new FakeLifecyclePolicy(),
            publisher);

        var result = await handler.Handle(new GetRedirectTargetQuery("expired1"), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Empty(publisher.Notifications);
        Assert.Single(repository.RemovedCalls);
        Assert.Single(cache.RemovedCalls);
    }

    [Fact]
    public async Task Handle_InactiveLink_ReturnsNotFoundAndDeletes()
    {
        var repository = new FakeShortUrlRepository
        {
            GetByShortCodeResult = new ShortUrl(
                "inactive1",
                "https://example.com/inactive",
                "hash",
                null,
                DateTime.UtcNow.AddDays(-40),
                null,
                DateTime.UtcNow.AddDays(-31))
        };
        var cache = new FakeShortUrlCache();
        var publisher = new FakePublisher();
        var handler = new GetRedirectTargetHandler(
            repository,
            cache,
            new FakeLifecyclePolicy(),
            publisher);

        var result = await handler.Handle(new GetRedirectTargetQuery("inactive1"), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Empty(publisher.Notifications);
        Assert.Single(repository.RemovedCalls);
        Assert.Single(cache.RemovedCalls);
    }

    private sealed class FakePublisher : IPublisher
    {
        public List<object> Notifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeShortUrlRepository : IShortUrlRepository
    {
        public int GetByShortCodeCallCount { get; private set; }
        public ShortUrl? GetByShortCodeResult { get; set; }
        public List<string> RemovedCalls { get; } = [];

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
        {
            RemovedCalls.Add(shortCode);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(DateTime nowUtc, TimeSpan inactiveFor, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }

    private sealed class FakeLifecyclePolicy : IShortUrlLifecyclePolicy
    {
        public TimeSpan InactiveLinkThreshold => TimeSpan.FromDays(30);
    }

    private sealed class FakeShortUrlCache : IShortUrlCache
    {
        public CachedShortUrl? CachedResult { get; set; }
        public List<(string shortCode, CachedShortUrl value)> SetCalls { get; } = [];
        public List<string> RemovedCalls { get; } = [];

        public Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CachedResult);
        }

        public Task SetAsync(string shortCode, CachedShortUrl value, CancellationToken cancellationToken = default)
        {
            SetCalls.Add((shortCode, value));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
        {
            RemovedCalls.Add(shortCode);
            return Task.CompletedTask;
        }
    }
}
