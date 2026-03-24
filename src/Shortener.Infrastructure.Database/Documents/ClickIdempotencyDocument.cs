using System.Text.Json.Serialization;

namespace Shortener.Infrastructure.Database.Documents;

/// <summary>
/// Marker item in the urls container (same partition as the short URL) so click recording can be idempotent in a transactional batch.
/// </summary>
internal sealed class ClickIdempotencyDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "ClickIdempotency";

    /// <summary>
    /// Cosmos per-item TTL (seconds). Requires container default TTL enabled (e.g. -1).
    /// </summary>
    [JsonPropertyName("ttl")]
    public int? Ttl { get; set; }
}
