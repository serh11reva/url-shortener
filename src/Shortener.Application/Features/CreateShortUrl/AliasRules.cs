using System.Text.RegularExpressions;
using Shortener.Application.Abstractions.Exceptions;

namespace Shortener.Application.Features.CreateShortUrl;

public static partial class AliasRules
{
    public const int MaxAliasLength = 32;
    public const int MinAliasLength = 1;

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
    }

    [GeneratedRegex("^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex GetAliasFormatRegex();
}
