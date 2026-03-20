using Shortener.Domain;
using Shortener.Infrastructure.Database;
using Shortener.Infrastructure.Database.Documents;

namespace Shortener.Infrastructure.Database.Tests;

public sealed class ShortUrlDocumentMapperTests
{
    [Fact]
    public void ToDocument_MapsAllProperties()
    {
        var entity = new ShortUrl(
            "abc12",
            "https://example.com/path",
            "hash64chars________________________________________________",
            "myAlias",
            new DateTime(2025, 3, 19, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 20, 10, 0, 0, DateTimeKind.Utc));

        var doc = ShortUrlDocumentMapper.ToDocument(entity);

        Assert.Equal("abc12", doc.Id);
        Assert.Equal("abc12", doc.Pk);
        Assert.Equal("https://example.com/path", doc.LongUrl);
        Assert.Equal("hash64chars________________________________________________", doc.LongUrlHash);
        Assert.Equal("myAlias", doc.Alias);
        Assert.Equal(new DateTime(2025, 3, 19, 10, 0, 0, DateTimeKind.Utc), doc.CreatedAt);
        Assert.Equal(new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc), doc.ExpiresAt);
        Assert.Equal(new DateTime(2025, 3, 20, 10, 0, 0, DateTimeKind.Utc), doc.LastAccessedAt);
    }

    [Fact]
    public void ToDocument_WithNullAlias_MapsAliasAsNull()
    {
        var entity = new ShortUrl("x", "https://x.com", "h", null, DateTime.UtcNow, null);

        var doc = ShortUrlDocumentMapper.ToDocument(entity);

        Assert.Null(doc.Alias);
        Assert.Null(doc.ExpiresAt);
    }

    [Fact]
    public void ToDomain_MapsAllProperties()
    {
        var doc = new ShortUrlDocument
        {
            Id = "xyz99",
            Pk = "xyz99",
            LongUrl = "https://example.org",
            LongUrlHash = "a".PadRight(64, 'a'),
            Alias = "custom",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastAccessedAt = new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var entity = ShortUrlDocumentMapper.ToDomain(doc);

        Assert.Equal("xyz99", entity.ShortCode);
        Assert.Equal("https://example.org", entity.LongUrl);
        Assert.Equal("a".PadRight(64, 'a'), entity.LongUrlHash);
        Assert.Equal("custom", entity.Alias);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.CreatedAt);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.ExpiresAt);
        Assert.Equal(new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc), entity.LastAccessedAt);
    }

    [Fact]
    public void ToDomain_WithNullAliasAndExpiresAt_MapsCorrectly()
    {
        var doc = new ShortUrlDocument
        {
            Id = "sc",
            Pk = "sc",
            LongUrl = "https://u.com",
            LongUrlHash = "h",
            Alias = null,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null
        };

        var entity = ShortUrlDocumentMapper.ToDomain(doc);

        Assert.Null(entity.Alias);
        Assert.Null(entity.ExpiresAt);
    }

    [Fact]
    public void RoundTrip_ToDocumentThenToDomain_PreservesData()
    {
        var original = new ShortUrl(
            "round",
            "https://round-trip.example.com",
            "roundHash__________________________________________________",
            "roundAlias",
            new DateTime(2023, 6, 15, 12, 30, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc));

        var doc = ShortUrlDocumentMapper.ToDocument(original);
        var roundTripped = ShortUrlDocumentMapper.ToDomain(doc);

        Assert.Equal(original.ShortCode, roundTripped.ShortCode);
        Assert.Equal(original.LongUrl, roundTripped.LongUrl);
        Assert.Equal(original.LongUrlHash, roundTripped.LongUrlHash);
        Assert.Equal(original.Alias, roundTripped.Alias);
        Assert.Equal(original.CreatedAt, roundTripped.CreatedAt);
        Assert.Equal(original.ExpiresAt, roundTripped.ExpiresAt);
        Assert.Equal(original.LastAccessedAt, roundTripped.LastAccessedAt);
    }
}
