using MediatR;
using Shortener.Application.Abstractions.ShortUrls;

namespace Shortener.Application.Features.Cleanup;

public sealed class CleanupExpiredLinksHandler(
    IShortUrlRepository repository,
    IShortUrlCache cache,
    IShortUrlLifecyclePolicy lifecyclePolicy) : IRequestHandler<CleanupExpiredLinksCommand, CleanupExpiredLinksResult>
{
    public async Task<CleanupExpiredLinksResult> Handle(CleanupExpiredLinksCommand request, CancellationToken cancellationToken)
    {
        var removedShortCodes = await repository.DeleteExpiredAndInactiveAsync(
            DateTime.UtcNow,
            lifecyclePolicy.InactiveLinkThreshold,
            cancellationToken);

        foreach (var shortCode in removedShortCodes)
        {
            await cache.RemoveAsync(shortCode, cancellationToken);
        }

        return new CleanupExpiredLinksResult(removedShortCodes.Count);
    }
}
