using MediatR;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.CreateShortUrl;

namespace Shortener.Application.Features.CheckAliasAvailability;

public sealed class CheckAliasAvailabilityHandler
    : IRequestHandler<CheckAliasAvailabilityQuery, CheckAliasAvailabilityResult>
{
    private readonly IShortUrlCache _cache;
    private readonly IShortUrlRepository _repository;

    public CheckAliasAvailabilityHandler(IShortUrlCache cache, IShortUrlRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<CheckAliasAvailabilityResult> Handle(
        CheckAliasAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        AliasRules.ValidateFormat(request.Alias);

        if (await _cache.ExistsAsync(request.Alias, cancellationToken))
        {
            return new CheckAliasAvailabilityResult(Available: false);
        }

        var entity = await _repository.GetByShortCodeAsync(request.Alias, cancellationToken);
        return new CheckAliasAvailabilityResult(Available: entity is null);
    }
}
