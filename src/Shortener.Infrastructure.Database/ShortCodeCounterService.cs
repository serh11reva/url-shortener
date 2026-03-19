using Shortener.Application.Abstractions.Counter;

namespace Shortener.Infrastructure.Database;

public sealed class ShortCodeCounterService : IShortCodeCounter
{
    private readonly ICounterAllocator _allocator;
    private long _current;
    private long _end;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int RangeSize = 1000;

    public ShortCodeCounterService(ICounterAllocator allocator)
    {
        _allocator = allocator;
        _current = 0;
        _end = 0;
    }

    public async Task<long> GetNextAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_current >= _end)
            {
                var start = await _allocator.GetNextRangeAsync(RangeSize, cancellationToken);
                _current = start;
                _end = start + RangeSize - 1;
            }

            return _current++;
        }
        finally
        {
            _lock.Release();
        }
    }
}
