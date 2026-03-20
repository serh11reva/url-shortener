using MediatR;
using Shortener.Application.Abstractions.ShortUrls;

namespace Shortener.Application.Features.Redirect;

public class GetRedirectTargetHandler : IRequestHandler<GetRedirectTargetQuery, GetRedirectTargetResult>
{
    private readonly IShortUrlLifecyclePolicy _lifecyclePolicy;
    private readonly IShortUrlRepository _repository;
    private readonly IShortUrlCache _cache;

    public GetRedirectTargetHandler(
        IShortUrlRepository repository,
        IShortUrlCache cache,
        IShortUrlLifecyclePolicy lifecyclePolicy)
    {
        _repository = repository;
        _cache = cache;
        _lifecyclePolicy = lifecyclePolicy;
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

            await _repository.MarkAccessedAsync(request.ShortCode, DateTime.UtcNow, cancellationToken);
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

        var now = DateTime.UtcNow;
        await _repository.MarkAccessedAsync(request.ShortCode, now, cancellationToken);
        await _cache.SetAsync(
            shortUrl.ShortCode,
            new CachedShortUrl(shortUrl.LongUrl, shortUrl.ExpiresAt),
            cancellationToken);

        return new GetRedirectTargetResult(shortUrl.LongUrl, true);
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
