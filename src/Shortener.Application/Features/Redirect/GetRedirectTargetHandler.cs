using MediatR;

namespace Shortener.Application.Features.Redirect;

public class GetRedirectTargetHandler : IRequestHandler<GetRedirectTargetQuery, GetRedirectTargetResult>
{
    public Task<GetRedirectTargetResult> Handle(GetRedirectTargetQuery request, CancellationToken cancellationToken)
    {
        // Stub: return not found until Task 3.x implements Redis/Cosmos lookup
        return Task.FromResult(new GetRedirectTargetResult(string.Empty, false));
    }
}
