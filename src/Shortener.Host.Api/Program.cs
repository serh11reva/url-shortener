using System.Text.Json;
using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shortener.Application.Abstractions.Counter;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Analytics;
using Shortener.Application.Features.CreateShortUrl;
using Shortener.Application.Features.Redirect;
using Shortener.Infrastructure.Database;
using Shortener.Infrastructure.Shared.Cache;
using Shortener.Infrastructure.Shared.Configuration;
using Shortener.Infrastructure.Shared.Health;
using Shortener.Infrastructure.Shared.Infrastructure;
using Shortener.ServiceDefaults;
using StackExchange.Redis;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(configureHealthChecks: hcb =>
{
    hcb.AddRedis(
            sp => sp.GetRequiredService<IConnectionMultiplexer>(),
            name: "redis",
            tags: ["ready"])
        .AddCheck<CosmosDbHealthCheck>("cosmos", tags: ["ready"]);
});

builder.Services.Configure<CosmosOptions>(
    builder.Configuration.GetSection(CosmosOptions.SectionName));
builder.Services.Configure<RedisOptions>(
    builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<ShortenerOptions>(
    builder.Configuration.GetSection(ShortenerOptions.SectionName));

builder.AddRedisClient("cache");
builder.AddAzureCosmosClient("cosmos", configureClientOptions: options =>
{
    options.UseSystemTextJsonSerializerWithOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    if (builder.Environment.IsDevelopment())
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.LimitToEndpoint = true;
        options.HttpClientFactory = () =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            return new HttpClient(handler, disposeHandler: false);
        };
    }
});

builder.Services.AddSingleton<Container>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var cosmosOpts = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
    var env = sp.GetRequiredService<IHostEnvironment>();

    if (env.IsDevelopment())
    {
        var db = client.CreateDatabaseIfNotExistsAsync(cosmosOpts.DatabaseId)
            .GetAwaiter().GetResult();
        var containerResponse = db.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(cosmosOpts.ContainerId, "/pk"))
            .GetAwaiter().GetResult();

        try
        {
            containerResponse.Container.CreateItemAsync(
                    new { id = "shortCode", pk = "counter", last = 0L },
                    new PartitionKey("counter"))
                .GetAwaiter().GetResult();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Counter document already seeded from a previous run
        }
    }

    return client.GetContainer(cosmosOpts.DatabaseId, cosmosOpts.ContainerId);
});
builder.Services.AddSingleton<IShortUrlRepository, CosmosShortUrlRepository>();
builder.Services.AddSingleton<ICounterAllocator, CosmosCounterAllocator>();
builder.Services.AddSingleton<IShortCodeCounter, ShortCodeCounterService>();
builder.Services.AddSingleton<IShortUrlCache, RedisShortUrlCache>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 429,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please retry after the time specified in Retry-After."
        }, cancellationToken);
    };
});
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateShortUrlCommand>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Create short URL: POST /api/urls
app.MapPost("/api/urls", async (
    CreateShortUrlRequest request,
    IMediator mediator,
    IOptions<ShortenerOptions> options,
    CancellationToken cancellationToken) =>
{
    var command = new CreateShortUrlCommand(request.LongUrl, request.Alias);
    var result = await mediator.Send(command, cancellationToken);
    var baseUrl = options.Value.BaseUrl?.TrimEnd('/');
    var response = new CreateShortUrlResult(result.ShortCode);

    return Results.Created($"/api/urls/{result.ShortCode}", response);
})
.WithName("CreateShortUrl");

// Redirect: GET /{shortCode}
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

// Analytics: GET /api/urls/{shortCode}/stats
app.MapGet("/api/urls/{shortCode}/stats", async (string shortCode, IMediator mediator, CancellationToken cancellationToken) =>
{
    var query = new GetAnalyticsQuery(shortCode);
    var result = await mediator.Send(query, cancellationToken);

    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetAnalytics");

app.Run();

public partial class Program
{
}
