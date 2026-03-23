using MediatR;
using Microsoft.Extensions.Logging;
using Shortener.Application.Abstractions.Analytics;
using Shortener.Application.Shared;

namespace Shortener.Application.Features.Analytics;

public sealed class PublishClickTrackedNotificationHandler : INotificationHandler<ClickTrackedNotification>
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromMilliseconds(500);
    private readonly IQueueStore _queueStore;
    private readonly ILogger<PublishClickTrackedNotificationHandler> _logger;

    public PublishClickTrackedNotificationHandler(
        IQueueStore queueStore,
        ILogger<PublishClickTrackedNotificationHandler> logger)
    {
        _queueStore = queueStore;
        _logger = logger;
    }

    public async Task Handle(ClickTrackedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(PublishTimeout);

            await _queueStore.PublishAsync(
                    QueueNames.Clicks,
                    new ClickTrackedEvent(notification.ShortCode, notification.OccurredAtUtc),
                    timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Click analytics publish failed or timed out for short code {ShortCode}",
                notification.ShortCode);
        }
    }
}
