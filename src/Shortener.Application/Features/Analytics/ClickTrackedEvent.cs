namespace Shortener.Application.Features.Analytics;

public sealed record ClickTrackedEvent(string ShortCode, DateTimeOffset OccurredAtUtc);
