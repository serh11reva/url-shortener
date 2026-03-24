using MediatR;
using Shortener.Application.Features.Redirect;

namespace Shortener.Host.Api.Endpoints;

internal static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirect(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortCode}", async (string shortCode, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var query = new GetRedirectTargetQuery(shortCode);
                var result = await mediator.Send(query, cancellationToken);

                return result.Found
                    ? Results.Redirect(result.LongUrl, false)
                    : Results.NotFound();
            })
            .WithName("Redirect")
            .ExcludeFromDescription();

        return app;
    }
}
