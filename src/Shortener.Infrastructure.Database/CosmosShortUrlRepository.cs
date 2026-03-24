using System.Net;
using Microsoft.Azure.Cosmos;
using Shortener.Application.Abstractions.Exceptions;
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
        try
        {
            await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            // Same document id written concurrently (custom alias or idempotent duplicate create). Handler may reconcile via FindExisting.
            throw new AliasAlreadyExistsException(entity.ShortCode);
        }
    }

    public async Task RecordClickAsync(string shortCode, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.PatchItemAsync<ShortUrlDocument>(
                shortCode,
                new PartitionKey(shortCode),
                [
                    PatchOperation.Increment("/clickCount", 1L),
                    PatchOperation.Set("/lastAccessedAt", accessedAtUtc)
                ],
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Document removed (e.g. cleanup) before click was processed.
        }
    }

    public async Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<ShortUrlDocument>(
                shortCode,
                new PartitionKey(shortCode),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already removed.
        }
    }

    public async Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(
        DateTime nowUtc,
        TimeSpan inactiveFor,
        CancellationToken cancellationToken = default)
    {
        var inactiveCutoff = nowUtc - inactiveFor;
        const string queryText = """
            SELECT c.id, c.pk FROM c
            WHERE c.pk != "counter"
              AND (
                (IS_DEFINED(c.expiresAt) AND NOT IS_NULL(c.expiresAt) AND c.expiresAt <= @nowUtc)
                OR
                ((NOT IS_DEFINED(c.lastAccessedAt) OR IS_NULL(c.lastAccessedAt)) AND c.createdAt <= @inactiveCutoff)
                OR
                (IS_DEFINED(c.lastAccessedAt) AND NOT IS_NULL(c.lastAccessedAt) AND c.lastAccessedAt <= @inactiveCutoff)
              )
            """;
        var query = new QueryDefinition(queryText)
            .WithParameter("@nowUtc", nowUtc)
            .WithParameter("@inactiveCutoff", inactiveCutoff);

        var deleted = new List<string>();
        using var iterator = _container.GetItemQueryIterator<ShortCodeProjection>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in page)
            {
                try
                {
                    await _container.DeleteItemAsync<ShortUrlDocument>(
                        item.Id,
                        new PartitionKey(item.Pk),
                        cancellationToken: cancellationToken);
                    deleted.Add(item.Id);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Deleted concurrently.
                }
            }
        }

        return deleted;
    }

    private sealed class ShortCodeProjection
    {
        public string Id { get; init; } = string.Empty;
        public string Pk { get; init; } = string.Empty;
    }
}
