using MediatR;

namespace Shortener.Application.Features.CreateShortUrl;

public class CreateShortUrlHandler : IRequestHandler<CreateShortUrlCommand, CreateShortUrlResult>
{
    public Task<CreateShortUrlResult> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        // Stub: return placeholder until Task 2.x implements counter and persistence
        var shortCode = "stub1";
        var shortUrl = $"/{shortCode}";
        return Task.FromResult(new CreateShortUrlResult(shortCode, shortUrl));
    }
}
