namespace Shortener.Infrastructure.Shared.Configuration;

public class ShortenerOptions
{
    public const string SectionName = "Shortener";

    /// <summary>
    /// Base URL for short links (e.g. https://short.example.com). Used to build full short URL in responses.
    /// </summary>
    public string? BaseUrl { get; set; }
    public TimeSpan InactiveLinkThreshold { get; set; } = TimeSpan.FromDays(30);
}
