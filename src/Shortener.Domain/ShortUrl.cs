namespace Shortener.Domain;

public sealed class ShortUrl
{
    public string ShortCode { get; }
    public string LongUrl { get; }
    public string LongUrlHash { get; }
    public string? Alias { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ExpiresAt { get; }
    public DateTime? LastAccessedAt { get; }

    public ShortUrl(
        string shortCode,
        string longUrl,
        string longUrlHash,
        string? alias,
        DateTime createdAt,
        DateTime? expiresAt = null,
        DateTime? lastAccessedAt = null)
    {
        ShortCode = shortCode;
        LongUrl = longUrl;
        Alias = alias;
        LongUrlHash = longUrlHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        LastAccessedAt = lastAccessedAt;
    }
}
