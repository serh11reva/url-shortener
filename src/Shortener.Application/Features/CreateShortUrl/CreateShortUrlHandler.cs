using MediatR;
using Shortener.Application.Abstractions.Counter;
using Shortener.Application.Abstractions.Exceptions;
using Shortener.Application.Abstractions.ShortUrls;
using Shortener.Application.Shared;
using Shortener.Domain;
using Sqids;

namespace Shortener.Application.Features.CreateShortUrl;

public sealed class CreateShortUrlHandler : IRequestHandler<CreateShortUrlCommand, CreateShortUrlResult>
{
    private static readonly SqidsEncoder<long> ShortCodeEncoder = new();
    private readonly IShortUrlRepository _repository;
    private readonly IShortCodeCounter _counter;

    public CreateShortUrlHandler(
        IShortUrlRepository repository,
        IShortCodeCounter counter)
    {
        _repository = repository;
        _counter = counter;
    }

    public async Task<CreateShortUrlResult> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        CreateShortUrlValidator.Validate(request.LongUrl, request.Alias, request.ExpiresAt);

        var longUrlHash = LongUrlHasher.ComputeHash(request.LongUrl);
        var existing = await _repository.FindExistingByLongUrlHashAndAliasAsync(
            longUrlHash,
            request.Alias,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateShortUrlResult(existing.ShortCode);
        }

        var shortCode = await CreateShortCode(request, cancellationToken);
        var entity = new ShortUrl(
            shortCode,
            request.LongUrl,
            longUrlHash,
            request.Alias,
            DateTime.UtcNow,
            request.ExpiresAt,
            null);

        await _repository.AddAsync(entity, cancellationToken);

        return new CreateShortUrlResult(shortCode);
    }

    private async Task<string> CreateShortCode(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        string shortCode;
        if (!string.IsNullOrEmpty(request.Alias))
        {
            var existingByAlias = await _repository.GetByAliasAsync(request.Alias, cancellationToken);
            if (existingByAlias is not null)
            {
                throw new AliasAlreadyExistsException(request.Alias);
            }

            shortCode = request.Alias!;
        }
        else
        {
            var value = await _counter.GetNextAsync(cancellationToken);
            shortCode = ShortCodeEncoder.Encode(value);
        }

        return shortCode;
    }
}
