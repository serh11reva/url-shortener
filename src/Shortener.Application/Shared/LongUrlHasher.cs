using System.Security.Cryptography;
using System.Text;

namespace Shortener.Application.Shared;

/// <summary>
/// Normalizes a long URL and computes a fixed-size hash for storage and lookup.
/// Enables efficient index-friendly idempotency checks without querying by long strings.
/// </summary>
public static class LongUrlHasher
{
    /// <summary>
    /// Normalizes the URL (lowercase scheme/host, remove default ports, trim) then returns SHA256 hash as hex (64 chars).
    /// </summary>
    public static string ComputeHash(string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
        {
            throw new ArgumentException("Long URL must not be null or empty.", nameof(longUrl));
        }

        var normalized = UrlNormalizer.Normalize(longUrl);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
