namespace Shortener.Application.Abstractions.Counter;

/// <summary>
/// Allocates ranges of counter values from persistent storage for short-code generation.
/// Each call returns the start of a new range; callers consume values sequentially.
/// </summary>
public interface ICounterAllocator
{
    /// <summary>
    /// Allocates a new range of size <paramref name="rangeSize"/> and returns the start value (inclusive).
    /// </summary>
    Task<long> GetNextRangeAsync(int rangeSize, CancellationToken cancellationToken = default);
}
