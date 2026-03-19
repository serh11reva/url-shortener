namespace Shortener.Application.Abstractions.ShortUrls;

/// <summary>
/// Optional cache for short URL lookups (e.g. Redis). Used to prime cache after create.
/// </summary>
public interface IShortUrlCache
{
    Task SetAsync(string shortCode, string longUrl, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}
