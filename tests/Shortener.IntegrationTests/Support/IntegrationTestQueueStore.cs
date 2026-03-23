using Shortener.Application.Abstractions.Analytics;

namespace Shortener.IntegrationTests.Support;

/// <summary>
/// Substitutes <see cref="IQueueStore"/> in integration tests so the API host does not send to Azure Service Bus.
/// Lives under <c>tests/</c> only; production hosts use real Service Bus via Aspire or configured connection strings.
/// </summary>
public sealed class IntegrationTestQueueStore : IQueueStore
{
    public Task PublishAsync<T>(string queueName, T @event, CancellationToken cancellationToken = default)
        where T : class
    {
        _ = queueName;
        _ = @event;
        return Task.CompletedTask;
    }
}
