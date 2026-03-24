using MediatR;
using Shortener.Application.Features.Analytics;
using Shortener.Application.Features.CreateShortUrl;

namespace Shortener.Host.Api.Endpoints;

internal static class ShortUrlEndpoints
{
    public static IEndpointRouteBuilder MapCreateShortUrl(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/urls", async (
                CreateShortUrlRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateShortUrlCommand(request.LongUrl, request.Alias, request.ExpiresAt);
                var result = await mediator.Send(command, cancellationToken);
                var response = new CreateShortUrlResult(result.ShortCode);

                return Results.Created($"/api/urls/{result.ShortCode}", response);
            })
            .WithName("CreateShortUrl");

        return app;
    }

    public static IEndpointRouteBuilder MapShortUrlAnalytics(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/urls/{shortCode}/stats", async (string shortCode, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var query = new GetAnalyticsQuery(shortCode);
                var result = await mediator.Send(query, cancellationToken);

                return result is not null ? Results.Ok(result) : Results.NotFound();
            })
            .WithName("GetAnalytics");

        return app;
    }
}
