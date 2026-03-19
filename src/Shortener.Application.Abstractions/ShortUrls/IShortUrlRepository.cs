using Shortener.Domain;

namespace Shortener.Application.Abstractions.ShortUrls;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default);

    Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default);
}
