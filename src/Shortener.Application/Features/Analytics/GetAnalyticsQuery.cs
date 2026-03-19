using MediatR;

namespace Shortener.Application.Features.Analytics;

public record GetAnalyticsQuery(string ShortCode) : IRequest<GetAnalyticsResult?>;
