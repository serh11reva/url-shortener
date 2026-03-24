using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Analytics;
using Shortener.Domain;

namespace Shortener.Application.Tests.Features.Analytics;

public sealed class GetAnalyticsHandlerTests
{
    [Fact]
    public async Task Handle_EmptyShortCode_ReturnsNull()
    {
        var handler = new GetAnalyticsHandler(new FakeRepository(null));
        var result = await handler.Handle(new GetAnalyticsQuery(""), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhitespaceShortCode_ReturnsNull()
    {
        var handler = new GetAnalyticsHandler(new FakeRepository(null));
        var result = await handler.Handle(new GetAnalyticsQuery("   "), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShortUrlNotFound_ReturnsNull()
    {
        var handler = new GetAnalyticsHandler(new FakeRepository(null));
        var result = await handler.Handle(new GetAnalyticsQuery("missing"), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShortUrlFound_ReturnsClickCountAndLastAccessed()
    {
        var lastAccessed = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var entity = new ShortUrl("sc1", "https://x.test", "h", null, DateTime.UtcNow, null, lastAccessed, 42);
        var handler = new GetAnalyticsHandler(new FakeRepository(entity));

        var result = await handler.Handle(new GetAnalyticsQuery("sc1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(42L, result.ClickCount);
        Assert.Equal(new DateTimeOffset(lastAccessed), result.LastAccessed);
    }

    [Fact]
    public async Task Handle_ShortUrlFound_NoLastAccessed_ReturnsNullLastAccessed()
    {
        var entity = new ShortUrl("sc1", "https://x.test", "h", null, DateTime.UtcNow, null, null, 0);
        var handler = new GetAnalyticsHandler(new FakeRepository(entity));

        var result = await handler.Handle(new GetAnalyticsQuery("sc1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0L, result.ClickCount);
        Assert.Null(result.LastAccessed);
    }

    private sealed class FakeRepository : IShortUrlRepository
    {
        private readonly ShortUrl? _byCode;

        public FakeRepository(ShortUrl? byCode)
        {
            _byCode = byCode;
        }

        public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult(_byCode);

        public Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordClickAsync(string shortCode, Guid clickId, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(DateTime nowUtc, TimeSpan inactiveFor, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }
}
