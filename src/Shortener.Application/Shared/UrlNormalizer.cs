namespace Shortener.Application.Shared;

/// <summary>
/// Normalizes URLs for consistent comparison and hashing: lowercase scheme and host,
/// remove default ports, trim, no fragment. Invalid URLs are returned trimmed only.
/// </summary>
public static class UrlNormalizer
{
    /// <summary>
    /// Normalizes URL for consistent hashing: lowercase scheme and host, remove default ports, trim, no fragment.
    /// If the input is not a valid absolute URI, returns the trimmed string unchanged.
    /// </summary>
    public static string Normalize(string longUrl)
    {
        var trimmed = longUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
        {
            return trimmed;
        }

        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();
        var port = uri.Port;
        var omitDefaultPort = (port == 80 && scheme == "http") || (port == 443 && scheme == "https");

        var path = uri.AbsolutePath;
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        var query = uri.Query;
        var portSegment = (!omitDefaultPort && port > 0) ? ":" + port : "";
        return scheme + "://" + host + portSegment + path + query;
    }
}
