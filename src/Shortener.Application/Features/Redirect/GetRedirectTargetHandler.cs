using MediatR;
using Shortener.Application.Abstractions.ShortUrls;

namespace Shortener.Application.Features.Redirect;

public class GetRedirectTargetHandler : IRequestHandler<GetRedirectTargetQuery, GetRedirectTargetResult>
{
    private readonly IShortUrlRepository _repository;
    private readonly IShortUrlCache _cache;

    public GetRedirectTargetHandler(IShortUrlRepository repository, IShortUrlCache cache)
    {
        _repository = repository;
        _cache = cache;
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
                return new GetRedirectTargetResult(string.Empty, false);
            }

            return new GetRedirectTargetResult(cached.LongUrl, true);
        }

        var shortUrl = await _repository.GetByShortCodeAsync(request.ShortCode, cancellationToken);
        if (shortUrl is null || IsExpired(shortUrl.ExpiresAt))
        {
            return new GetRedirectTargetResult(string.Empty, false);
        }

        await _cache.SetAsync(
            shortUrl.ShortCode,
            shortUrl.LongUrl,
            shortUrl.ExpiresAt,
            null,
            cancellationToken);

        return new GetRedirectTargetResult(shortUrl.LongUrl, true);
    }

    private static bool IsExpired(DateTime? expiresAt)
    {
        return expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow;
    }
}
