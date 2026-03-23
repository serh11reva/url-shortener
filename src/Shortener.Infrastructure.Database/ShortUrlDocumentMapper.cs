using Shortener.Domain;
using Shortener.Infrastructure.Database.Documents;

namespace Shortener.Infrastructure.Database;

/// <summary>
/// Maps between <see cref="ShortUrl"/> domain entities and <see cref="ShortUrlDocument"/> persistence documents.
/// </summary>
internal static class ShortUrlDocumentMapper
{
    /// <summary>
    /// Maps a domain entity to a Cosmos DB document.
    /// </summary>
    internal static ShortUrlDocument ToDocument(ShortUrl entity)
    {
        return new ShortUrlDocument
        {
            Id = entity.ShortCode,
            Pk = entity.ShortCode,
            LongUrl = entity.LongUrl,
            LongUrlHash = entity.LongUrlHash,
            Alias = entity.Alias,
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt,
            LastAccessedAt = entity.LastAccessedAt,
            ClickCount = entity.ClickCount
        };
    }

    /// <summary>
    /// Maps a Cosmos DB document to a domain entity.
    /// </summary>
    internal static ShortUrl ToDomain(ShortUrlDocument doc)
    {
        return new ShortUrl(doc.Id, doc.LongUrl, doc.LongUrlHash, doc.Alias, doc.CreatedAt, doc.ExpiresAt, doc.LastAccessedAt, doc.ClickCount);
    }
}
