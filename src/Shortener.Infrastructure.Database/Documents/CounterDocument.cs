using System.Text.Json.Serialization;

namespace Shortener.Infrastructure.Database.Documents;

internal sealed class CounterDocument
{
    public const string PartitionKeyValue = "counter";
    public const string DocumentId = "shortCode";

    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = PartitionKeyValue;

    [JsonPropertyName("last")]
    public long Last { get; set; }
}
