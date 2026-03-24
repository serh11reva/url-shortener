using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Shortener.Application.Features.CreateShortUrl;
using Shortener.Host.Api.Endpoints;
using Shortener.Infrastructure.Database.DependencyInjection;
using Shortener.Infrastructure.ServiceBus.DependencyInjection;
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

builder.Services.AddShortenerStorageOptions(builder.Configuration);

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
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            return new HttpClient(handler, disposeHandler: false);
        };
    }
});

builder.Services.AddShortenerDataAccess();
builder.AddServiceBus();

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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "URL Shortener API");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapCreateShortUrl();
app.MapAliasAvailability();
app.MapRedirect();
app.MapShortUrlAnalytics();

app.Run();

public partial class Program
{
}
