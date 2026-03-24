using Shortener.Domain;

namespace Shortener.Application.Abstractions.ShortUrls;

public interface IShortUrlRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default);

    Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one click for analytics. <paramref name="clickId"/> must be unique per redirect; duplicate ids are ignored (no second increment).
    /// </summary>
    Task RecordClickAsync(string shortCode, Guid clickId, DateTime accessedAtUtc, CancellationToken cancellationToken = default);

    Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(
        DateTime nowUtc,
        TimeSpan inactiveFor,
        CancellationToken cancellationToken = default);
}
