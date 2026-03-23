using MediatR;
using Shortener.Application.Abstractions.ShortUrls;

namespace Shortener.Application.Features.Analytics;

public class GetAnalyticsHandler : IRequestHandler<GetAnalyticsQuery, GetAnalyticsResult?>
{
    private readonly IShortUrlRepository _repository;

    public GetAnalyticsHandler(IShortUrlRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAnalyticsResult?> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShortCode))
        {
            return null;
        }

        var shortUrl = await _repository.GetByShortCodeAsync(request.ShortCode, cancellationToken);
        if (shortUrl is null)
        {
            return null;
        }

        var lastAccessed = shortUrl.LastAccessedAt.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(shortUrl.LastAccessedAt.Value, DateTimeKind.Utc))
            : (DateTimeOffset?)null;

        return new GetAnalyticsResult(shortUrl.ClickCount, lastAccessed);
    }
}
