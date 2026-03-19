using StackExchange.Redis;
using Shortener.Application.Abstractions.ShortUrls;

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

    public async Task SetAsync(string shortCode, string longUrl, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + shortCode;
        var expiry = ttl ?? DefaultTtl;
        await db.StringSetAsync(key, longUrl, expiry);
    }
}
