namespace Shortener.Application.Abstractions.Counter;

/// <summary>
/// Provides the next single counter value for short-code generation, consuming allocated ranges.
/// </summary>
public interface IShortCodeCounter
{
    Task<long> GetNextAsync(CancellationToken cancellationToken = default);
}
