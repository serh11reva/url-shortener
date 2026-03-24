using System.Text.Json;
using Shortener.Application.Abstractions.ShortUrls;
using StackExchange.Redis;

namespace Shortener.Infrastructure.Shared.Cache;

public sealed class RedisShortUrlCache : IShortUrlCache
{
    private readonly IConnectionMultiplexer _redis;
    private const string KeyPrefix = "short:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    public RedisShortUrlCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<CachedShortUrl?> GetAsync(string shortCodeOrAlias, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCodeOrAlias;
        var payload = await db.StringGetAsync(key);

        if (payload.IsNullOrEmpty)
        {
            return null;
        }

        var payloadString = payload.ToString();
        var entry = JsonSerializer.Deserialize<CachedShortUrlEntry>(payloadString);
        if (entry is null || string.IsNullOrWhiteSpace(entry.LongUrl))
        {
            return null;
        }

        // Sliding expiration: keep recently accessed links warm in cache.
        // Never extend beyond absolute URL expiration, when present.
        var slidingExpiry = ResolveExpiry(entry.ExpiresAt);
        if (slidingExpiry > TimeSpan.Zero)
        {
            await db.KeyExpireAsync(key, slidingExpiry);
        }

        return new CachedShortUrl(entry.LongUrl, entry.ExpiresAt);
    }

    public async Task<bool> ExistsAsync(string shortCodeOrAlias, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCodeOrAlias;
        return await db.KeyExistsAsync(key);
    }

    public async Task SetAsync(
        string shortCodeOrAlias,
        CachedShortUrl value,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCodeOrAlias;
        var expiry = ResolveExpiry(value.ExpiresAt);
        var entry = new CachedShortUrlEntry
        {
            LongUrl = value.LongUrl,
            ExpiresAt = value.ExpiresAt
        };
        var payload = JsonSerializer.Serialize(entry);
        await db.StringSetAsync(key, payload, expiry);
    }

    public async Task RemoveAsync(string shortCodeOrAlias, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCodeOrAlias;
        await db.KeyDeleteAsync(key);
    }

    private static TimeSpan ResolveExpiry(DateTime? expiresAt)
    {
        if (expiresAt is null)
        {
            return DefaultTtl;
        }

        var remaining = expiresAt.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(1);
        }

        return remaining < DefaultTtl ? remaining : DefaultTtl;
    }

    private sealed class CachedShortUrlEntry
    {
        public string LongUrl { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }
}
