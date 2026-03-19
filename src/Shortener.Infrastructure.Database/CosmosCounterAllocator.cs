using System.Net;
using Microsoft.Azure.Cosmos;
using Shortener.Application.Abstractions.Counter;
using Shortener.Infrastructure.Database.Documents;

namespace Shortener.Infrastructure.Database;

public sealed class CosmosCounterAllocator : ICounterAllocator
{
    private readonly Container _container;
    private const int MaxRetries = 5;

    public CosmosCounterAllocator(Container container)
    {
        _container = container;
    }

    public async Task<long> GetNextRangeAsync(int rangeSize, CancellationToken cancellationToken = default)
    {
        if (rangeSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rangeSize), rangeSize, "Range size must be at least 1.");
        }

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await TryAllocateRangeAsync(rangeSize, cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict && attempt < MaxRetries - 1)
            {
                // Optimistic concurrency conflict; retry
            }
        }

        throw new InvalidOperationException($"Failed to allocate counter range after {MaxRetries} attempts.");
    }

    private async Task<long> TryAllocateRangeAsync(int rangeSize, CancellationToken cancellationToken)
    {
        ItemResponse<CounterDocument> readResponse;
        try
        {
            readResponse = await _container.ReadItemAsync<CounterDocument>(
                CounterDocument.DocumentId,
                new PartitionKey(CounterDocument.PartitionKeyValue),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // First run: create counter document with initial range
            var newDoc = new CounterDocument { Last = rangeSize };
            try
            {
                await _container.CreateItemAsync(
                    newDoc,
                    new PartitionKey(CounterDocument.PartitionKeyValue),
                    cancellationToken: cancellationToken);
                return 1;
            }
            catch (CosmosException createEx) when (createEx.StatusCode == HttpStatusCode.Conflict)
            {
                // Another instance created it; retry from start (next loop iteration in GetNextRangeAsync)
                throw;
            }
        }

        var doc = readResponse.Resource;
        var previousLast = doc.Last;
        var newLast = previousLast + rangeSize;

        doc.Last = newLast;
        await _container.ReplaceItemAsync(
            doc,
            CounterDocument.DocumentId,
            new PartitionKey(CounterDocument.PartitionKeyValue),
            new ItemRequestOptions { IfMatchEtag = readResponse.ETag },
            cancellationToken);

        return previousLast + 1;
    }
}
