using MediatR;

namespace Shortener.Application.Features.CreateShortUrl;

public record CreateShortUrlCommand(string LongUrl, string? Alias = null) : IRequest<CreateShortUrlResult>;
