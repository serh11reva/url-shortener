namespace Shortener.Application.Features.Analytics;

public record GetAnalyticsResult(long ClickCount, DateTimeOffset? LastAccessed);
