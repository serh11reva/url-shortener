using Microsoft.Extensions.Options;
using Shortener.Application.Abstractions.ShortUrls;

namespace Shortener.Infrastructure.Shared.Configuration;

public sealed class ShortUrlLifecyclePolicy(IOptions<ShortenerOptions> options) : IShortUrlLifecyclePolicy
{
    public TimeSpan InactiveLinkThreshold => options.Value.InactiveLinkThreshold;
}
