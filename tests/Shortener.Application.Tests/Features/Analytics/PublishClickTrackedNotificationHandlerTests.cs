using Microsoft.Extensions.Logging.Abstractions;
using Shortener.Application.Abstractions.Analytics;
using Shortener.Application.Features.Analytics;

namespace Shortener.Application.Tests.Features.Analytics;

public sealed class PublishClickTrackedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_PublishesClickToQueueStore()
    {
        var queueStore = new FakeQueueStore();
        var handler = new PublishClickTrackedNotificationHandler(
            queueStore,
            NullLogger<PublishClickTrackedNotificationHandler>.Instance);

        var at = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var clickId = Guid.Parse("b2222222-2222-2222-2222-222222222222");
        await handler.Handle(new ClickTrackedNotification("sc1", at, clickId), CancellationToken.None);

        Assert.Single(queueStore.Calls);
        Assert.Equal("clicks", queueStore.Calls[0].queueName);
        Assert.Equal("sc1", queueStore.Calls[0].eventPayload.ShortCode);
        Assert.Equal(at, queueStore.Calls[0].eventPayload.OccurredAtUtc);
        Assert.Equal(clickId, queueStore.Calls[0].eventPayload.ClickId);
        Assert.Equal(clickId.ToString("D"), queueStore.Calls[0].messageId);
    }

    private sealed class FakeQueueStore : IQueueStore
    {
        public List<(string queueName, ClickTrackedEvent eventPayload, string messageId)> Calls { get; } = [];

        public Task PublishAsync<T>(string queueName, T @event, CancellationToken cancellationToken = default) where T : class
            => PublishAsync(queueName, @event, string.Empty, cancellationToken);

        public Task PublishAsync<T>(string queueName, T @event, string messageId, CancellationToken cancellationToken = default) where T : class
        {
            Calls.Add((queueName, (ClickTrackedEvent)(object)@event, messageId));
            return Task.CompletedTask;
        }
    }
}
