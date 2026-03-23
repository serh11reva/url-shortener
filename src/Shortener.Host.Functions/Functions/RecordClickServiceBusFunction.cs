using System.Text.Json;
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
        [ServiceBusTrigger(QueueNames.Clicks, Connection = "messaging")] string messageBody,
        CancellationToken cancellationToken)
    {
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

        await _mediator.Send(new RecordClickCommand(dto.ShortCode, dto.OccurredAtUtc), cancellationToken);
    }

    private sealed record ClickEventDto(string ShortCode, DateTimeOffset OccurredAtUtc);
}
