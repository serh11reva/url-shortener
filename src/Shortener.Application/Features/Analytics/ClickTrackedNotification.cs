using MediatR;

namespace Shortener.Application.Features.Analytics;

public sealed record ClickTrackedNotification(string ShortCode, DateTimeOffset OccurredAtUtc) : INotification;
