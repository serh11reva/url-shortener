using Shortener.Application.Abstractions.Counter;
using Shortener.Application.Abstractions.Exceptions;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.CreateShortUrl;
using Shortener.Domain;
using Sqids;

namespace Shortener.Application.Tests.Features.CreateShortUrl;

public sealed class CreateShortUrlHandlerTests
{
    private static readonly SqidsEncoder<long> ShortCodeEncoder = new();

    [Fact]
    public async Task Handle_ValidRequest_ReturnsShortCodeAndShortUrl()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(1);
        var cache = new FakeShortUrlCache();
        var handler = new CreateShortUrlHandler(repo, counter, cache);

        var result = await handler.Handle(
            new CreateShortUrlCommand("https://example.com/foo", null),
            CancellationToken.None);

        Assert.Equal(ShortCodeEncoder.Encode(1), result.ShortCode);
        Assert.Single(repo.Added);
        Assert.Equal(ShortCodeEncoder.Encode(1), repo.Added[0].ShortCode);
        Assert.Equal("https://example.com/foo", repo.Added[0].LongUrl);
        Assert.NotNull(repo.Added[0].LongUrlHash);
        Assert.Equal(64, repo.Added[0].LongUrlHash!.Length);
    }

    [Fact]
    public async Task Handle_WithAlias_UsesAliasAsShortCode()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(999);
        var cache = new FakeShortUrlCache();
        var handler = new CreateShortUrlHandler(repo, counter, cache);

        var result = await handler.Handle(
            new CreateShortUrlCommand("https://example.com", "myLink"),
            CancellationToken.None);

        Assert.Equal("myLink", result.ShortCode);
        Assert.Single(repo.Added);
        Assert.Equal("myLink", repo.Added[0].ShortCode);
        Assert.Equal("myLink", repo.Added[0].Alias);
        Assert.Equal(0, counter.CallCount);
    }

    [Fact]
    public async Task Handle_DuplicateAlias_ThrowsAliasAlreadyExistsException()
    {
        var repo = new FakeShortUrlRepository();
        repo.GetByAliasReturns = new ShortUrl("taken", "https://other.com", "somehash", "taken", DateTime.UtcNow);
        var counter = new FakeShortCodeCounter(1);
        var cache = new FakeShortUrlCache();
        var handler = new CreateShortUrlHandler(repo, counter, cache);

        var ex = await Assert.ThrowsAsync<AliasAlreadyExistsException>(() =>
            handler.Handle(
                new CreateShortUrlCommand("https://example.com", "taken"),
                CancellationToken.None));

        Assert.Equal("taken", ex.Alias);
        Assert.Empty(repo.Added);
    }

    [Fact]
    public async Task Handle_ExistingLongUrlAndAlias_ReturnsExistingShortUrl()
    {
        var existing = new ShortUrl("abc", "https://example.com", "somehash", null, DateTime.UtcNow, null);
        var repo = new FakeShortUrlRepository();
        repo.FindExistingByLongUrlHashAndAliasReturns = existing;
        var counter = new FakeShortCodeCounter(1);
        var cache = new FakeShortUrlCache();
        var handler = new CreateShortUrlHandler(repo, counter, cache);

        var result = await handler.Handle(
            new CreateShortUrlCommand("https://example.com", null),
            CancellationToken.None);

        Assert.Equal("abc", result.ShortCode);
        Assert.Empty(repo.Added);
        Assert.Equal(0, counter.CallCount);
    }

    [Fact]
    public async Task Handle_InvalidLongUrl_ThrowsValidationException()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(1);
        var cache = new FakeShortUrlCache();
        var handler = new CreateShortUrlHandler(repo, counter, cache);

        await Assert.ThrowsAsync<CreateShortUrlValidationException>(() =>
            handler.Handle(
                new CreateShortUrlCommand("not-a-url", null),
                CancellationToken.None));

        Assert.Empty(repo.Added);
    }

    [Fact]
    public async Task Handle_PrimesCache()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(42);
        var cache = new FakeShortUrlCache();
        var handler = new CreateShortUrlHandler(repo, counter, cache);

        await handler.Handle(
            new CreateShortUrlCommand("https://example.com", null),
            CancellationToken.None);

        Assert.Single(cache.SetCalls);
        Assert.Equal(ShortCodeEncoder.Encode(42), cache.SetCalls[0].shortCode);
        Assert.Equal("https://example.com", cache.SetCalls[0].longUrl);
    }

    private sealed class FakeShortUrlRepository : IShortUrlRepository
    {
        public List<ShortUrl> Added { get; } = [];
        public ShortUrl? GetByAliasReturns { get; set; }
        public ShortUrl? FindExistingByLongUrlHashAndAliasReturns { get; set; }

        public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
            => Task.FromResult(GetByAliasReturns);

        public Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
            => Task.FromResult(FindExistingByLongUrlHashAndAliasReturns);

        public Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
        {
            Added.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeShortCodeCounter : IShortCodeCounter
    {
        private long _value;
        public int CallCount { get; private set; }

        public FakeShortCodeCounter(long startValue)
        {
            _value = startValue;
        }

        public Task<long> GetNextAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_value++);
        }
    }

    private sealed class FakeShortUrlCache : IShortUrlCache
    {
        public List<(string shortCode, string longUrl)> SetCalls { get; } = [];

        public Task SetAsync(string shortCode, string longUrl, TimeSpan? ttl, CancellationToken cancellationToken = default)
        {
            SetCalls.Add((shortCode, longUrl));
            return Task.CompletedTask;
        }
    }
}
