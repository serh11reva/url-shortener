using MediatR;
using Microsoft.Extensions.Logging;
using Shortener.Application.Abstractions.ShortUrls;

namespace Shortener.Application.Features.Analytics;

public sealed class RecordClickHandler : IRequestHandler<RecordClickCommand>
{
    private readonly IShortUrlRepository _repository;
    private readonly ILogger<RecordClickHandler> _logger;

    public RecordClickHandler(IShortUrlRepository repository, ILogger<RecordClickHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(RecordClickCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.RecordClickAsync(
                    request.ShortCode,
                    request.ClickId,
                    request.OccurredAtUtc.UtcDateTime,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record click for short code {ShortCode}", request.ShortCode);
            throw;
        }
    }
}
