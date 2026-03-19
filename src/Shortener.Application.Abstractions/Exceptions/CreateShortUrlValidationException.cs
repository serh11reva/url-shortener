namespace Shortener.Application.Abstractions.Exceptions;

public sealed class CreateShortUrlValidationException : Exception
{
    public CreateShortUrlValidationException(string message) : base(message)
    {
    }

    public CreateShortUrlValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
