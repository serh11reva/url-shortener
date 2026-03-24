namespace Shortener.IntegrationTests;

/// <summary>
/// Shares one <see cref="ShortenerAppFixture"/> (Cosmos + Redis containers) across all integration tests
/// and prevents parallel runs against the same containers, which avoids timeouts and flaky analytics stats.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<ShortenerAppFixture>
{
}
