namespace Shortener.Application.Abstractions.Exceptions;

public sealed class AliasAlreadyExistsException : Exception
{
    public AliasAlreadyExistsException(string alias) : base($"The alias '{alias}' is already in use.")
    {
        Alias = alias;
    }

    public string Alias { get; }
}
