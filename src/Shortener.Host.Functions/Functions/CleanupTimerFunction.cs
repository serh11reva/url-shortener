using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Shortener.Host.Functions.Functions;

public sealed class CleanupTimerFunction(ILogger<CleanupTimerFunction> logger)
{
    [Function(nameof(CleanupTimerFunction))]
    public Task RunAsync([TimerTrigger("%CleanupSchedule%", RunOnStartup = false, UseMonitor = true)] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Cleanup timer triggered at {TriggeredAtUtc}. Next schedule at {NextScheduleUtc}.",
                DateTime.UtcNow,
                timerInfo.ScheduleStatus?.Next);

            // TODO: invoke expiration/inactive-link cleanup workflow here.
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cleanup timer execution failed.");
            throw;
        }
    }
}
