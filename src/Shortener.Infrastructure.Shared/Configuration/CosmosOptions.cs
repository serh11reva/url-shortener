namespace Shortener.Infrastructure.Shared.Configuration;

public class CosmosOptions
{
    public const string SectionName = "Cosmos";

    public string DatabaseId { get; set; } = "shortener";

    public string ContainerId { get; set; } = "urls";
}
