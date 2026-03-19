using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shortener.Infrastructure.Shared.Configuration;

namespace Shortener.Infrastructure.Shared.Health;

public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly Microsoft.Azure.Cosmos.CosmosClient _client;
    private readonly CosmosOptions _options;

    public CosmosDbHealthCheck(Microsoft.Azure.Cosmos.CosmosClient client, IOptions<CosmosOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _client.GetDatabase(_options.DatabaseId);
            await database.ReadAsync(requestOptions: null, cancellationToken);
            return HealthCheckResult.Healthy("Cosmos DB is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cosmos DB check failed.", ex);
        }
    }
}
