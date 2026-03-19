using System.Text.Json.Serialization;

namespace Shortener.Infrastructure.Database.Documents;

internal sealed class ShortUrlDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = string.Empty;

    [JsonPropertyName("longUrl")]
    public string LongUrl { get; set; } = string.Empty;

    [JsonPropertyName("longUrlHash")]
    public string LongUrlHash { get; set; } = string.Empty;

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
