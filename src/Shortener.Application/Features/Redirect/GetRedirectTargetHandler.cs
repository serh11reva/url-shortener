using MediatR;
using Microsoft.Extensions.Logging;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Analytics;

namespace Shortener.Application.Features.Redirect;

public class GetRedirectTargetHandler : IRequestHandler<GetRedirectTargetQuery, GetRedirectTargetResult>
{
    private readonly IShortUrlLifecyclePolicy _lifecyclePolicy;
    private readonly IShortUrlRepository _repository;
    private readonly IShortUrlCache _cache;
    private readonly IPublisher _publisher;
    private readonly ILogger<GetRedirectTargetHandler> _logger;

    public GetRedirectTargetHandler(
        IShortUrlRepository repository,
        IShortUrlCache cache,
        IShortUrlLifecyclePolicy lifecyclePolicy,
        IPublisher publisher,
        ILogger<GetRedirectTargetHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _lifecyclePolicy = lifecyclePolicy;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<GetRedirectTargetResult> Handle(GetRedirectTargetQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShortCode))
        {
            return new GetRedirectTargetResult(string.Empty, false);
        }

        var cached = await _cache.GetAsync(request.ShortCode, cancellationToken);
        if (cached is not null)
        {
            if (IsExpired(cached.ExpiresAt))
            {
                await _cache.RemoveAsync(request.ShortCode, cancellationToken);
                await _repository.RemoveByShortCodeAsync(request.ShortCode, cancellationToken);
                return new GetRedirectTargetResult(string.Empty, false);
            }

            await PublishClickAsync(request.ShortCode, cancellationToken);
            return new GetRedirectTargetResult(cached.LongUrl, true);
        }

        var shortUrl = await _repository.GetByShortCodeAsync(request.ShortCode, cancellationToken);
        if (shortUrl is null)
        {
            return new GetRedirectTargetResult(string.Empty, false);
        }

        if (IsExpired(shortUrl.ExpiresAt) || IsInactive(shortUrl, _lifecyclePolicy.InactiveLinkThreshold))
        {
            await _repository.RemoveByShortCodeAsync(request.ShortCode, cancellationToken);
            await _cache.RemoveAsync(request.ShortCode, cancellationToken);
            return new GetRedirectTargetResult(string.Empty, false);
        }

        await PublishClickAsync(request.ShortCode, cancellationToken);

        return new GetRedirectTargetResult(shortUrl.LongUrl, true);
    }

    private async Task PublishClickAsync(string shortCode, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.Publish(
                new ClickTrackedNotification(shortCode, DateTimeOffset.UtcNow, Guid.CreateVersion7()),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to publish click tracked notification for short code {ShortCode}", shortCode);
        }
    }

    private static bool IsExpired(DateTime? expiresAt)
    {
        return expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow;
    }

    private static bool IsInactive(Domain.ShortUrl shortUrl, TimeSpan inactiveThreshold)
    {
        var lastAccess = shortUrl.LastAccessedAt ?? shortUrl.CreatedAt;
        return lastAccess <= DateTime.UtcNow - inactiveThreshold;
    }
}
