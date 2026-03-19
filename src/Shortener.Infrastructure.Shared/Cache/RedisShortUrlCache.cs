using System.Text.Json;
using Shortener.Application.Abstractions.ShortUrls;
using StackExchange.Redis;

namespace Shortener.Infrastructure.Shared.Cache;

public sealed class RedisShortUrlCache : IShortUrlCache
{
    private readonly IConnectionMultiplexer _redis;
    private const string KeyPrefix = "short:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(12);

    public RedisShortUrlCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCode;
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

        return new CachedShortUrl(entry.LongUrl, entry.ExpiresAt);
    }

    public async Task SetAsync(
        string shortCode,
        string longUrl,
        DateTime? expiresAt,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCode;
        var expiry = ResolveExpiry(ttl, expiresAt);
        var entry = new CachedShortUrlEntry
        {
            LongUrl = longUrl,
            ExpiresAt = expiresAt
        };
        var payload = JsonSerializer.Serialize(entry);
        await db.StringSetAsync(key, payload, expiry);
    }

    public async Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCode;
        await db.KeyDeleteAsync(key);
    }

    private static TimeSpan ResolveExpiry(TimeSpan? ttl, DateTime? expiresAt)
    {
        if (ttl.HasValue)
        {
            return ttl.Value;
        }

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
