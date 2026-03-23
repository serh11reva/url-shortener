using MediatR;

namespace Shortener.Application.Features.Analytics;

public sealed record RecordClickCommand(string ShortCode, DateTimeOffset OccurredAtUtc) : IRequest;
