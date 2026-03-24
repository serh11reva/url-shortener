using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
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
    private const int ClickIdempotencyTtlSeconds = 604800; // 7 days
    private readonly Container _container;
    private readonly ILogger<CosmosShortUrlRepository> _logger;

    public CosmosShortUrlRepository(Container container, ILogger<CosmosShortUrlRepository> logger)
    {
        _container = container;
        _logger = logger;
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

    public async Task RecordClickAsync(string shortCode, Guid clickId, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
    {
        var idempotencyId = FormattableString.Invariant($"idem-{clickId:D}");
        var idempotencyDoc = new ClickIdempotencyDocument
        {
            Id = idempotencyId,
            Pk = shortCode,
            Kind = "ClickIdempotency",
            Ttl = ClickIdempotencyTtlSeconds,
        };

        var batch = _container.CreateTransactionalBatch(new PartitionKey(shortCode));
        batch.CreateItem(idempotencyDoc);
        batch.PatchItem(
            shortCode,
            [
                PatchOperation.Increment("/clickCount", 1L),
                PatchOperation.Set("/lastAccessedAt", accessedAtUtc)
            ]);

        using var response = await batch.ExecuteAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        for (var i = 0; i < response.Count; i++)
        {
            var op = response[i];
            if (op.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogDebug(
                    "Skipping duplicate click id {ClickId} for short code {ShortCode} (transactional batch op {Index}).",
                    clickId,
                    shortCode,
                    i);
                return;
            }

            if (op.StatusCode == HttpStatusCode.NotFound)
            {
                // Short URL removed before the batch applied (batch rolled back).
                return;
            }
        }

        _logger.LogError(
            "Transactional batch failed for click {ClickId} on {ShortCode}: StatusCode={StatusCode}, ErrorMessage={ErrorMessage}",
            clickId,
            shortCode,
            response.StatusCode,
            response.ErrorMessage);

        throw new InvalidOperationException(
            $"Cosmos transactional batch failed for click on '{shortCode}': {response.StatusCode} {response.ErrorMessage}");
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
              AND NOT STARTSWITH(c.id, "idem-")
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
