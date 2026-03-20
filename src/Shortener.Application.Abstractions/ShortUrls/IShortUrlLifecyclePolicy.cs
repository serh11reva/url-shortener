namespace Shortener.Application.Abstractions.ShortUrls;

public interface IShortUrlLifecyclePolicy
{
    TimeSpan InactiveLinkThreshold { get; }
}
