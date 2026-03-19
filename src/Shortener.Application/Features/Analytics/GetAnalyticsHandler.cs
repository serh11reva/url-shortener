using MediatR;

namespace Shortener.Application.Features.Analytics;

public class GetAnalyticsHandler : IRequestHandler<GetAnalyticsQuery, GetAnalyticsResult?>
{
    public Task<GetAnalyticsResult?> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        // Stub: return null (404) until Task 5.x implements analytics
        return Task.FromResult<GetAnalyticsResult?>(null);
    }
}
