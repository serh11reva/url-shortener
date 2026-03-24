using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Shortener.Application.Abstractions.Exceptions;

namespace Shortener.Application.Features.CreateShortUrl;

public static partial class AliasRules
{
    public const int MaxAliasLength = 32;
    public const int MinAliasLength = 1;

    /// <summary>
    /// Exact aliases reserved so they do not collide with API paths, health endpoints, OpenAPI/Swagger, or first-segment SPA routes when the short link is served on the same host.
    /// </summary>
    private static readonly FrozenSet<string> ReservedAliases = new[]
    {
        "api",
        "health",
        "alive",
        "openapi",
        "swagger",
        "metrics",
        "stats",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex AliasFormatRegex = GetAliasFormatRegex();

    public static void ValidateFormat(string alias)
    {
        if (alias.Length < MinAliasLength || alias.Length > MaxAliasLength)
        {
            throw new CreateShortUrlValidationException(
                $"Alias must be between {MinAliasLength} and {MaxAliasLength} characters.");
        }

        if (!AliasFormatRegex.IsMatch(alias))
        {
            throw new CreateShortUrlValidationException(
                "Alias must use letters, numbers, and single hyphens between segments (e.g. my-alias).");
        }

        if (ReservedAliases.Contains(alias))
        {
            throw new CreateShortUrlValidationException(
                "This alias is reserved and cannot be used.");
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex GetAliasFormatRegex();
}
