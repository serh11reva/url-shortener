using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortener.Application.Abstractions.Analytics;
using Shortener.IntegrationTests.Support;
using StackExchange.Redis;
using Testcontainers.CosmosDb;
using Testcontainers.Redis;

namespace Shortener.IntegrationTests;

/// <summary>
/// xUnit fixture that starts Redis and Cosmos DB in Docker and provides a WebApplicationFactory
/// configured to use those containers. Cosmos database "shortener" and container "urls" (partition key /pk)
/// are created automatically.
/// </summary>
public sealed class ShortenerAppFixture : IAsyncLifetime, IDisposable
{
    private static readonly TimeSpan DbTimeout = TimeSpan.FromSeconds(10);
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.0").Build();
    private readonly CosmosDbContainer _cosmos = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest").Build();
    private WebApplicationFactory<Program>? _factory;

    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Fixture not initialized. Ensure IAsyncLifetime.InitializeAsync was called.");

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        await _cosmos.StartAsync();

        var cosmosConnectionString = UseHttpsEndpoint(_cosmos.GetConnectionString());
        await WaitForCosmosReady(cosmosConnectionString);
        await CreateCosmosDatabaseAndContainerAsync(cosmosConnectionString);

        var redisConnectionString = _redis.GetConnectionString();

        const string messagingPlaceholder =
            "Endpoint=sb://integration-tests.servicebus.windows.net/;SharedAccessKeyName=integration;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:cache"] = redisConnectionString,
                        ["ConnectionStrings:cosmos"] = cosmosConnectionString,
                        ["ConnectionStrings:messaging"] = messagingPlaceholder,
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    ReplaceService<CosmosClient>(services,
                        _ => CreateCosmosClient(cosmosConnectionString));

                    ReplaceService<IConnectionMultiplexer>(services,
                        _ => ConnectionMultiplexer.Connect(redisConnectionString));

                    ReplaceService<IQueueStore>(services,
                        sp => new IntegrationTestQueueStore(sp.GetRequiredService<IServiceScopeFactory>()));
                });
            });
    }

    private static void ReplaceService<T>(IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors)
            services.Remove(d);

        services.AddSingleton(factory);
    }

    private static string UseHttpsEndpoint(string connectionString)
    {
        return connectionString.Replace("AccountEndpoint=http://", "AccountEndpoint=https://", StringComparison.OrdinalIgnoreCase);
    }

    private static CosmosClient CreateCosmosClient(string connectionString)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            RequestTimeout = DbTimeout,
            HttpClientFactory = () => new HttpClient(handler, disposeHandler: false),
            LimitToEndpoint = true,
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
        };
        return new CosmosClient(connectionString, options);
    }

    private static async Task CreateCosmosDatabaseAndContainerAsync(string connectionString)
    {
        const string databaseId = "shortener";
        const string containerId = "urls";
        const string partitionKeyPath = "/pk";

        using var client = CreateCosmosClient(connectionString);

        // Emulator can take time to accept connections after container is "ready"; retry with backoff.
        var maxAttempts = 5;
        Exception? lastException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
                await database.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(containerId, partitionKeyPath));
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable || ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            {
                lastException = ex;
                if (attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5 * attempt));
                }
            }
        }

        throw new InvalidOperationException(
            "Cosmos DB emulator did not become ready in time. Ensure Docker has enough memory and the emulator image started successfully.",
            lastException);
    }

    private static async Task WaitForCosmosReady(string connectionString)
    {
        using var client = CreateCosmosClient(connectionString);

        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < DbTimeout)
        {
            try
            {
                await client.ReadAccountAsync(); // readiness probe
                return;
            }
            catch
            {
                await Task.Delay(2000);
            }
        }

        throw new TimeoutException("Cosmos DB emulator did not become ready in time.");
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _cosmos.DisposeAsync();
        await _redis.DisposeAsync();
    }

    public void Dispose()
    {
        _factory?.Dispose();
        _cosmos.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _redis.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
