namespace Shortener.Application.Features.Redirect;

public record GetRedirectTargetResult(string LongUrl, bool Found);
