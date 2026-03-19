using System.Net;
using Microsoft.Azure.Cosmos;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Domain;
using Shortener.Infrastructure.Database.Documents;

namespace Shortener.Infrastructure.Database;

/// <summary>
/// Cosmos DB repository for short URLs. The container must use partition key path /pk.
/// URL documents use pk = shortCode; counter document uses pk = "counter".
/// </summary>
public sealed class CosmosShortUrlRepository : IShortUrlRepository
{
    private readonly Container _container;

    public CosmosShortUrlRepository(Container container)
    {
        _container = container;
    }

    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<ShortUrlDocument>(
                shortCode,
                new PartitionKey(shortCode),
                cancellationToken: cancellationToken);
            return ShortUrlDocumentMapper.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        return await GetByShortCodeAsync(alias, cancellationToken);
    }

    public async Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
    {
        var q = @"
            SELECT * FROM c
            WHERE
                c.longUrlHash = @longUrlHash
                AND ((IS_NULL(@alias) AND (NOT IS_DEFINED(c.alias) OR IS_NULL(c.alias))) OR (NOT IS_NULL(@alias) AND c.alias = @alias))";
        var query = new QueryDefinition(q)
            .WithParameter("@longUrlHash", longUrlHash)
            .WithParameter("@alias", alias ?? (object?)DBNull.Value);

        using var iterator = _container.GetItemQueryIterator<ShortUrlDocument>(query);
        var response = await iterator.ReadNextAsync(cancellationToken);
        var first = response.FirstOrDefault();
        return first is null ? null : ShortUrlDocumentMapper.ToDomain(first);
    }

    public async Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
    {
        var doc = ShortUrlDocumentMapper.ToDocument(entity);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk), cancellationToken: cancellationToken);
    }
}
