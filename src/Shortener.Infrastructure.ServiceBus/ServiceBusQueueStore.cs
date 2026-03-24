using System.Text.Json;
using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shortener.Application.Abstractions.Analytics;

namespace Shortener.Infrastructure.ServiceBus;

public sealed class ServiceBusQueueStore : IQueueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ServiceBusQueueStore> _logger;

    public ServiceBusQueueStore(
        ServiceBusClient serviceBusClient,
        ILogger<ServiceBusQueueStore> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    public Task PublishAsync<T>(string queueName, T @event, CancellationToken cancellationToken = default)
        where T : class
        => PublishCoreAsync(queueName, @event, messageId: null, cancellationToken);

    public Task PublishAsync<T>(string queueName, T @event, string messageId, CancellationToken cancellationToken = default)
        where T : class
        => PublishCoreAsync(queueName, @event, messageId, cancellationToken);

    private async Task PublishCoreAsync<T>(string queueName, T @event, string? messageId, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var sender = _senders.GetOrAdd(queueName, qn => _serviceBusClient.CreateSender(qn));
            var payload = JsonSerializer.Serialize(@event, JsonOptions);
            var message = new ServiceBusMessage(payload)
            {
                ContentType = "application/json",
            };
            if (!string.IsNullOrWhiteSpace(messageId))
            {
                message.MessageId = messageId;
            }

            await sender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
            throw;
        }
    }
}
