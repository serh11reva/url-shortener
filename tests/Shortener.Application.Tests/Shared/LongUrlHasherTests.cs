using Shortener.Application.Shared;

namespace Shortener.Application.Tests.Shared;

public sealed class LongUrlHasherTests
{
    [Fact]
    public void ComputeHash_SameUrl_ReturnsSameHash()
    {
        var hash1 = LongUrlHasher.ComputeHash("https://example.com/path");
        var hash2 = LongUrlHasher.ComputeHash("https://example.com/path");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentCasingSchemeAndHost_ReturnsSameHash()
    {
        var hash1 = LongUrlHasher.ComputeHash("HTTPS://EXAMPLE.COM/path");
        var hash2 = LongUrlHasher.ComputeHash("https://example.com/path");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DefaultPortOmittedOrExplicit_ReturnsSameHash()
    {
        var hash1 = LongUrlHasher.ComputeHash("https://example.com:443/path");
        var hash2 = LongUrlHasher.ComputeHash("https://example.com/path");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_Returns64HexChars()
    {
        var hash = LongUrlHasher.ComputeHash("https://example.com");
        Assert.Equal(64, hash.Length);
        Assert.True(hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'));
    }

    [Fact]
    public void ComputeHash_DifferentPaths_ReturnsDifferentHash()
    {
        var hash1 = LongUrlHasher.ComputeHash("https://example.com/path1");
        var hash2 = LongUrlHasher.ComputeHash("https://example.com/path2");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_NullOrEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => LongUrlHasher.ComputeHash(null!));
        Assert.Throws<ArgumentException>(() => LongUrlHasher.ComputeHash(""));
        Assert.Throws<ArgumentException>(() => LongUrlHasher.ComputeHash("   "));
    }
}
