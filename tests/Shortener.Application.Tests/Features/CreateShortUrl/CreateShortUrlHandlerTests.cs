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
        var handler = new CreateShortUrlHandler(repo, counter);

        var result = await handler.Handle(
            new CreateShortUrlCommand("https://example.com/foo", null),
            CancellationToken.None);

        Assert.Equal(ShortCodeEncoder.Encode(1), result.ShortCode);
        Assert.Single(repo.Added);
        Assert.Equal(ShortCodeEncoder.Encode(1), repo.Added[0].ShortCode);
        Assert.Equal("https://example.com/foo", repo.Added[0].LongUrl);
        Assert.NotNull(repo.Added[0].LongUrlHash);
        Assert.Equal(64, repo.Added[0].LongUrlHash!.Length);
        Assert.Null(repo.Added[0].ExpiresAt);
    }

    [Fact]
    public async Task Handle_WithAlias_UsesAliasAsShortCode()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(999);
        var handler = new CreateShortUrlHandler(repo, counter);

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
        var handler = new CreateShortUrlHandler(repo, counter);

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
        var handler = new CreateShortUrlHandler(repo, counter);

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
        var handler = new CreateShortUrlHandler(repo, counter);

        await Assert.ThrowsAsync<CreateShortUrlValidationException>(() =>
            handler.Handle(
                new CreateShortUrlCommand("not-a-url", null),
                CancellationToken.None));

        Assert.Empty(repo.Added);
    }

    [Fact]
    public async Task Handle_DoesNotPrimeCache_WhenUsingReadThrough()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(42);
        var handler = new CreateShortUrlHandler(repo, counter);

        await handler.Handle(
            new CreateShortUrlCommand("https://example.com", null),
            CancellationToken.None);

        Assert.Single(repo.Added);
    }

    [Fact]
    public async Task Handle_WithExpiresAt_PersistsAndCachesExpiry()
    {
        var repo = new FakeShortUrlRepository();
        var counter = new FakeShortCodeCounter(7);
        var handler = new CreateShortUrlHandler(repo, counter);
        var expiresAt = DateTime.UtcNow.AddHours(2);

        await handler.Handle(
            new CreateShortUrlCommand("https://example.com/exp", null, expiresAt),
            CancellationToken.None);

        Assert.Single(repo.Added);
        Assert.Equal(expiresAt, repo.Added[0].ExpiresAt);
    }

    [Fact]
    public async Task Handle_AddAsyncConflict_ReconcilesSameLongUrlAndAlias_ReturnsExistingShortCode()
    {
        var existing = new ShortUrl("winner", "https://example.com/same", "hash1", "dup", DateTime.UtcNow, null);
        var repo = new FakeShortUrlRepository
        {
            ThrowAliasConflictOnAdd = true,
            FindExistingByLongUrlHashAndAliasFactory = callIndex => callIndex == 0 ? null : existing,
        };
        var counter = new FakeShortCodeCounter(1);
        var handler = new CreateShortUrlHandler(repo, counter);

        var result = await handler.Handle(
            new CreateShortUrlCommand("https://example.com/same", "dup"),
            CancellationToken.None);

        Assert.Equal("winner", result.ShortCode);
        Assert.Empty(repo.Added);
    }

    [Fact]
    public async Task Handle_AddAsyncConflict_NoMatchingRow_RethrowsAliasAlreadyExists()
    {
        var repo = new FakeShortUrlRepository
        {
            ThrowAliasConflictOnAdd = true,
            FindExistingByLongUrlHashAndAliasFactory = _ => null,
        };
        var counter = new FakeShortCodeCounter(1);
        var handler = new CreateShortUrlHandler(repo, counter);

        var ex = await Assert.ThrowsAsync<AliasAlreadyExistsException>(() =>
            handler.Handle(
                new CreateShortUrlCommand("https://example.com/other", "taken"),
                CancellationToken.None));

        Assert.Equal("taken", ex.Alias);
        Assert.Empty(repo.Added);
    }

    private sealed class FakeShortUrlRepository : IShortUrlRepository
    {
        public List<ShortUrl> Added { get; } = [];
        public ShortUrl? GetByAliasReturns { get; set; }
        public ShortUrl? FindExistingByLongUrlHashAndAliasReturns { get; set; }

        /// <summary>0-based call index; if null, <see cref="FindExistingByLongUrlHashAndAliasReturns"/> is used.</summary>
        public Func<int, ShortUrl?>? FindExistingByLongUrlHashAndAliasFactory { get; set; }

        public bool ThrowAliasConflictOnAdd { get; set; }

        private int _findExistingCallIndex;

        public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
            => Task.FromResult(GetByAliasReturns);

        public Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
        {
            if (FindExistingByLongUrlHashAndAliasFactory is not null)
            {
                var r = FindExistingByLongUrlHashAndAliasFactory(_findExistingCallIndex);
                _findExistingCallIndex++;
                return Task.FromResult(r);
            }

            return Task.FromResult(FindExistingByLongUrlHashAndAliasReturns);
        }

        public Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
        {
            if (ThrowAliasConflictOnAdd)
            {
                throw new AliasAlreadyExistsException(entity.ShortCode);
            }

            Added.Add(entity);
            return Task.CompletedTask;
        }

        public Task RecordClickAsync(string shortCode, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(DateTime nowUtc, TimeSpan inactiveFor, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
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

}
