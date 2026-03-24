using MediatR;
using Shortener.Application.Features.CheckAliasAvailability;

namespace Shortener.Host.Api.Endpoints;

internal static class AliasEndpoints
{
    public static IEndpointRouteBuilder MapAliasAvailability(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/aliases/{alias}/availability", async (string alias, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var query = new CheckAliasAvailabilityQuery(alias);
                var result = await mediator.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("CheckAliasAvailability");

        return app;
    }
}
