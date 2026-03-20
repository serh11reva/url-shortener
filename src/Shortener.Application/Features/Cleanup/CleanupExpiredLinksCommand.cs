using MediatR;

namespace Shortener.Application.Features.Cleanup;

public sealed record CleanupExpiredLinksCommand : IRequest<CleanupExpiredLinksResult>;
