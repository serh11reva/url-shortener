using Shortener.Application.Shared;

namespace Shortener.Application.Tests.Shared;

public sealed class UrlNormalizerTests
{
    [Fact]
    public void Normalize_LowercasesSchemeAndHost()
    {
        Assert.Equal("https://example.com/", UrlNormalizer.Normalize("HTTPS://EXAMPLE.COM/"));
        Assert.Equal("http://foo.bar/path", UrlNormalizer.Normalize("HTTP://FOO.BAR/path"));
    }

    [Fact]
    public void Normalize_OmitsDefaultHttpPort()
    {
        Assert.Equal("http://example.com/", UrlNormalizer.Normalize("http://example.com:80/"));
    }

    [Fact]
    public void Normalize_OmitsDefaultHttpsPort()
    {
        Assert.Equal("https://example.com/", UrlNormalizer.Normalize("https://example.com:443/"));
    }

    [Fact]
    public void Normalize_PreservesNonDefaultPort()
    {
        Assert.Equal("https://example.com:8443/", UrlNormalizer.Normalize("https://example.com:8443/"));
        Assert.Equal("http://example.com:8080/path", UrlNormalizer.Normalize("http://example.com:8080/path"));
    }

    [Fact]
    public void Normalize_EmptyPath_BecomesRootSlash()
    {
        Assert.Equal("https://example.com/", UrlNormalizer.Normalize("https://example.com"));
        Assert.Equal("https://example.com/", UrlNormalizer.Normalize("https://example.com/"));
    }

    [Fact]
    public void Normalize_PreservesPathAndQuery()
    {
        Assert.Equal("https://example.com/path?k=v", UrlNormalizer.Normalize("https://example.com/path?k=v"));
        Assert.Equal("https://example.com/a/b?x=1&y=2", UrlNormalizer.Normalize("https://example.com/a/b?x=1&y=2"));
    }

    [Fact]
    public void Normalize_TrimsWhitespace()
    {
        Assert.Equal("https://example.com/", UrlNormalizer.Normalize("  https://example.com/  "));
    }

    [Fact]
    public void Normalize_InvalidAbsoluteUrl_ReturnsTrimmedOnly()
    {
        const string invalid = "not-a-valid-url";
        Assert.Equal("not-a-valid-url", UrlNormalizer.Normalize(invalid));
        Assert.Equal("spaces", UrlNormalizer.Normalize("  spaces  "));
    }

    [Fact]
    public void Normalize_RelativeUrl_ReturnsTrimmedOnly()
    {
        Assert.Equal("/relative/path", UrlNormalizer.Normalize("/relative/path"));
    }

    [Fact]
    public void Normalize_SameUrlDifferentCasingAndPort_ProducesSameResult()
    {
        var a = UrlNormalizer.Normalize("HTTPS://EXAMPLE.COM:443/foo?q=1");
        var b = UrlNormalizer.Normalize("https://example.com/foo?q=1");
        Assert.Equal(a, b);
    }
}
