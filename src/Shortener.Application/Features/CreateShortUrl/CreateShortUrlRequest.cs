namespace Shortener.Application.Features.CreateShortUrl;

public record CreateShortUrlRequest(string LongUrl, string? Alias = null, DateTime? ExpiresAt = null);
