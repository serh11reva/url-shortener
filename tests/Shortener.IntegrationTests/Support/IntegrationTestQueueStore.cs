using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shortener.Application.Abstractions.Analytics;
using Shortener.Application.Features.Analytics;
using Shortener.Application.Shared;

namespace Shortener.IntegrationTests.Support;

/// <summary>
/// Substitutes <see cref="IQueueStore"/> in integration tests so the API host does not send to Azure Service Bus.
/// For the clicks queue, applies the same work as the Azure Functions consumer (<see cref="RecordClickCommand"/>)
/// so stats integration tests can validate the full API path without Service Bus.
/// Lives under <c>tests/</c> only; production hosts use real Service Bus via Aspire or configured connection strings.
/// </summary>
public sealed class IntegrationTestQueueStore : IQueueStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IntegrationTestQueueStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task PublishAsync<T>(string queueName, T @event, CancellationToken cancellationToken = default)
        where T : class
    {
        if (queueName == QueueNames.Clicks && @event is ClickTrackedEvent click)
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator
                .Send(new RecordClickCommand(click.ShortCode, click.OccurredAtUtc), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
