using MediatR;

namespace Shortener.Application.Features.CreateShortUrl;

public record CreateShortUrlCommand(string LongUrl, string? Alias = null, DateTime? ExpiresAt = null) : IRequest<CreateShortUrlResult>;
