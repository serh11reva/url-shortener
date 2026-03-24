using MediatR;

namespace Shortener.Application.Features.CheckAliasAvailability;

public record CheckAliasAvailabilityQuery(string Alias) : IRequest<CheckAliasAvailabilityResult>;
