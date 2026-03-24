namespace Shortener.Application.Abstractions.Analytics;

public interface IQueueStore
{
    Task PublishAsync<T>(string queueName, T @event, CancellationToken cancellationToken = default) where T : class;

    Task PublishAsync<T>(string queueName, T @event, string messageId, CancellationToken cancellationToken = default) where T : class;
}
