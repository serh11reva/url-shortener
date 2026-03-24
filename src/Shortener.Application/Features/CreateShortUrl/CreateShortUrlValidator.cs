using Shortener.Application.Abstractions.Exceptions;

namespace Shortener.Application.Features.CreateShortUrl;

public static class CreateShortUrlValidator
{
    public const int MaxLongUrlLength = 2048;

    public static void Validate(string longUrl, string? alias, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
        {
            throw new CreateShortUrlValidationException("LongUrl is required.");
        }

        if (longUrl.Length > MaxLongUrlLength)
        {
            throw new CreateShortUrlValidationException(
                $"LongUrl must not exceed {MaxLongUrlLength} characters.");
        }

        if (!Uri.TryCreate(longUrl, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
        {
            throw new CreateShortUrlValidationException("LongUrl must be a valid absolute URL.");
        }

        if (uri.Scheme is not "http" and not "https")
        {
            throw new CreateShortUrlValidationException("LongUrl must use http or https scheme.");
        }

        if (alias is not null)
        {
            AliasRules.ValidateFormat(alias);
        }

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
        {
            throw new CreateShortUrlValidationException("ExpiresAt must be a future UTC timestamp.");
        }
    }
}
