namespace Shortener.Application.Abstractions.ShortUrls;

public interface IShortUrlCache
{
    Task<CachedShortUrl?> GetAsync(string shortCodeOrAlias, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string shortCodeOrAlias, CancellationToken cancellationToken = default);

    Task SetAsync(
        string shortCodeOrAlias,
        CachedShortUrl value,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string shortCodeOrAlias, CancellationToken cancellationToken = default);
}
