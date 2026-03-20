using Shortener.Application.Abstractions.Exceptions;
using Shortener.Application.Features.CreateShortUrl;

namespace Shortener.Application.Tests.Features.CreateShortUrl;

public sealed class CreateShortUrlValidatorTests
{
    [Fact]
    public void Validate_ValidHttpUrl_DoesNotThrow()
    {
        CreateShortUrlValidator.Validate("http://example.com/path", null, null);
    }

    [Fact]
    public void Validate_ValidHttpsUrl_DoesNotThrow()
    {
        CreateShortUrlValidator.Validate("https://example.com", null, null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhiteSpaceLongUrl_ThrowsValidationException(string? longUrl)
    {
        var ex = Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate(longUrl!, null, null));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_UrlTooLong_ThrowsValidationException()
    {
        var longUrl = "https://example.com/" + new string('a', CreateShortUrlValidator.MaxLongUrlLength);
        var ex = Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate(longUrl, null, null));
        Assert.Contains("2048", ex.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    public void Validate_InvalidUrl_ThrowsValidationException(string longUrl)
    {
        Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate(longUrl, null, null));
    }

    [Fact]
    public void Validate_ValidAlias_DoesNotThrow()
    {
        CreateShortUrlValidator.Validate("https://example.com", "abc12", null);
    }

    [Fact]
    public void Validate_AliasTooLong_ThrowsValidationException()
    {
        var ex = Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate("https://example.com", "12345678", null));
        Assert.Contains("1", ex.Message);
        Assert.Contains("7", ex.Message);
    }

    [Fact]
    public void Validate_AliasWithInvalidCharacters_ThrowsValidationException()
    {
        var ex = Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate("https://example.com", "ab-cd", null));
        Assert.Contains("alphanumeric", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_AliasEmptyString_ThrowsValidationException()
    {
        Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate("https://example.com", "", null));
    }

    [Fact]
    public void Validate_ExpiresAtInPast_ThrowsValidationException()
    {
        var ex = Assert.Throws<CreateShortUrlValidationException>(() =>
            CreateShortUrlValidator.Validate("https://example.com", null, DateTime.UtcNow.AddMinutes(-1)));
        Assert.Contains("ExpiresAt", ex.Message);
    }
}
