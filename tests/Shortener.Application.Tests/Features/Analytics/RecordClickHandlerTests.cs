using Microsoft.Extensions.Logging.Abstractions;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Features.Analytics;
using Shortener.Domain;

namespace Shortener.Application.Tests.Features.Analytics;

public sealed class RecordClickHandlerTests
{
    [Fact]
    public async Task Handle_CallsRepositoryRecordClick()
    {
        var repository = new FakeRepository();
        var handler = new RecordClickHandler(repository, NullLogger<RecordClickHandler>.Instance);
        var at = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

        await handler.Handle(new RecordClickCommand("sc1", at), CancellationToken.None);

        Assert.Single(repository.Recorded);
        Assert.Equal("sc1", repository.Recorded[0].shortCode);
        Assert.Equal(at.UtcDateTime, repository.Recorded[0].accessedAt);
    }

    private sealed class FakeRepository : IShortUrlRepository
    {
        public List<(string shortCode, DateTime accessedAt)> Recorded { get; } = [];

        public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task<ShortUrl?> FindExistingByLongUrlHashAndAliasAsync(string longUrlHash, string? alias, CancellationToken cancellationToken = default)
            => Task.FromResult<ShortUrl?>(null);

        public Task AddAsync(ShortUrl entity, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RecordClickAsync(string shortCode, DateTime accessedAtUtc, CancellationToken cancellationToken = default)
        {
            Recorded.Add((shortCode, accessedAtUtc));
            return Task.CompletedTask;
        }

        public Task RemoveByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>> DeleteExpiredAndInactiveAsync(DateTime nowUtc, TimeSpan inactiveFor, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }
}
