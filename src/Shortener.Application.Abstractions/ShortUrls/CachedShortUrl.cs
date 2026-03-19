namespace Shortener.Application.Abstractions.ShortUrls;

public sealed record CachedShortUrl(string LongUrl, DateTime? ExpiresAt);
