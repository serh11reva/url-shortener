using System.Text.RegularExpressions;
using Shortener.Application.Abstractions.Exceptions;

namespace Shortener.Application.Features.CreateShortUrl;

public static partial class CreateShortUrlValidator
{
    public const int MaxLongUrlLength = 2048;
    public const int MaxAliasLength = 7;
    public const int MinAliasLength = 1;

    private static readonly Regex AliasRegex = GetAliasRegex();

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
            if (alias.Length < MinAliasLength || alias.Length > MaxAliasLength)
            {
                throw new CreateShortUrlValidationException(
                    $"Alias must be between {MinAliasLength} and {MaxAliasLength} characters.");
            }

            if (!AliasRegex.IsMatch(alias))
            {
                throw new CreateShortUrlValidationException(
                    "Alias must contain only alphanumeric characters (a-z, A-Z, 0-9).");
            }
        }

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
        {
            throw new CreateShortUrlValidationException("ExpiresAt must be a future UTC timestamp.");
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$", RegexOptions.Compiled)]
    private static partial Regex GetAliasRegex();
}
