using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shortener.Application.Abstractions.Counter;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Infrastructure.Shared.Cache;
using Shortener.Infrastructure.Shared.Configuration;
using StackExchange.Redis;

namespace Shortener.Infrastructure.Database.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShortenerStorageOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosOptions>(configuration.GetSection(CosmosOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<ShortenerOptions>(configuration.GetSection(ShortenerOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddShortenerDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<Container>(sp =>
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
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    // Counter document already seeded from a previous run.
                }
            }

            return client.GetContainer(cosmosOpts.DatabaseId, cosmosOpts.ContainerId);
        });

        services.AddSingleton<IShortUrlRepository, CosmosShortUrlRepository>();
        services.AddSingleton<ICounterAllocator, CosmosCounterAllocator>();
        services.AddSingleton<IShortCodeCounter, ShortCodeCounterService>();
        services.AddSingleton<IShortUrlCache, RedisShortUrlCache>();
        services.AddSingleton<IShortUrlLifecyclePolicy, ShortUrlLifecyclePolicy>();

        return services;
    }

    public static IServiceCollection AddShortenerRedisConnection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = configuration.GetConnectionString("cache");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Redis connection string 'cache' is not configured.");
            }

            return ConnectionMultiplexer.Connect(connectionString);
        });

        return services;
    }

    public static IServiceCollection AddShortenerCosmosClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var connectionString = ResolveCosmosConnectionString(configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Cosmos connection string is not configured. Expected one of: " +
                    "'ConnectionStrings:cosmos', 'ConnectionStrings:cosmos-db', " +
                    "'Values:ConnectionStrings:cosmos', or env var forms like 'ConnectionStrings__cosmos'.");
            }

            var options = new CosmosClientOptions
            {
                UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            };

            if (env.IsDevelopment())
            {
                options.ConnectionMode = ConnectionMode.Gateway;
                options.LimitToEndpoint = true;
                options.HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    return new HttpClient(handler, disposeHandler: false);
                };
            }

            return new CosmosClient(connectionString, options);
        });

        return services;
    }

    private static string? ResolveCosmosConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("cosmos")
               ?? configuration.GetConnectionString("cosmos-db");
    }
}
