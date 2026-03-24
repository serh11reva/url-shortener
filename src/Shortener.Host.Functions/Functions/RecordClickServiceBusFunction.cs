using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shortener.Application.Features.Analytics;
using Shortener.Application.Shared;

namespace Shortener.Host.Functions.Functions;

public sealed class RecordClickServiceBusFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMediator _mediator;
    private readonly ILogger<RecordClickServiceBusFunction> _logger;

    public RecordClickServiceBusFunction(IMediator mediator, ILogger<RecordClickServiceBusFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function(nameof(RecordClickServiceBusFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger(QueueNames.Clicks, Connection = "messaging")] ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var messageBody = message.Body.ToString();
        ClickEventDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ClickEventDto>(messageBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid click analytics message body");
            throw;
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.ShortCode))
        {
            _logger.LogError("Click analytics message missing shortCode");
            throw new InvalidOperationException("Click analytics message missing shortCode.");
        }

        Guid? resolvedId = dto.ClickId is { } cid && cid != Guid.Empty ? cid : null;
        resolvedId ??= Guid.TryParse(message.MessageId, out var parsedMessageId) && parsedMessageId != Guid.Empty
            ? parsedMessageId
            : null;
        if (resolvedId is null)
        {
            _logger.LogError("Click analytics message missing clickId and usable Service Bus MessageId.");
            throw new InvalidOperationException("Click analytics message missing click id.");
        }

        await _mediator.Send(new RecordClickCommand(dto.ShortCode, dto.OccurredAtUtc, resolvedId.Value), cancellationToken);
    }

    private sealed record ClickEventDto(Guid? ClickId, string ShortCode, DateTimeOffset OccurredAtUtc);
}
