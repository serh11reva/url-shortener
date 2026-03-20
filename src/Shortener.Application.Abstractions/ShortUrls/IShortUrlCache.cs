namespace Shortener.Application.Abstractions.ShortUrls;

/// <summary>
/// Optional cache for short URL lookups (e.g. Redis). Used to prime cache after create.
/// </summary>
public interface IShortUrlCache
{
    Task<CachedShortUrl?> GetAsync(string shortCode, CancellationToken cancellationToken = default);

    Task SetAsync(
        string shortCode,
        CachedShortUrl value,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string shortCode, CancellationToken cancellationToken = default);
}
