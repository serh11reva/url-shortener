namespace Shortener.Infrastructure.Shared.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string InstanceName { get; set; } = "shortener:";
}
