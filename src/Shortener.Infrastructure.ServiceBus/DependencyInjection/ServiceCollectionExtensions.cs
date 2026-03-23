using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shortener.Application.Abstractions.Analytics;

namespace Shortener.Infrastructure.ServiceBus.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddServiceBus(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IQueueStore, ServiceBusQueueStore>();
        builder.AddAzureServiceBusClient("messaging");

        return builder;
    }
}
