using MediatR;

namespace Shortener.Application.Features.Redirect;

public record GetRedirectTargetQuery(string ShortCode) : IRequest<GetRedirectTargetResult>;
