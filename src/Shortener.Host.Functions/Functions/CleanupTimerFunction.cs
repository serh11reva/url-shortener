using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shortener.Application.Features.Cleanup;

namespace Shortener.Host.Functions.Functions;

public sealed class CleanupTimerFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<CleanupTimerFunction> _logger;

    public CleanupTimerFunction(
        IMediator mediator,
        ILogger<CleanupTimerFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function(nameof(CleanupTimerFunction))]
    public async Task RunAsync([TimerTrigger("%CleanupSchedule%", RunOnStartup = false, UseMonitor = true)] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            _logger.LogInformation(
                "Cleanup timer triggered at {TriggeredAtUtc}. Next schedule at {NextScheduleUtc}.",
                nowUtc,
                timerInfo.ScheduleStatus?.Next);

            var result = await _mediator.Send(new CleanupExpiredLinksCommand(), cancellationToken);

            _logger.LogInformation("Cleanup removed {RemovedCount} expired/inactive links.", result.RemovedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup timer execution failed.");
            throw;
        }
    }
}
