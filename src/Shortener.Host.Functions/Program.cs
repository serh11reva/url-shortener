using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

using Shortener.Application.Features.Cleanup;
using Shortener.Infrastructure.Database.DependencyInjection;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddShortenerStorageOptions(builder.Configuration);
builder.Services.AddShortenerRedisConnection(builder.Configuration);
builder.AddAzureCosmosClient("cosmos", configureClientOptions: options =>
{
    options.UseSystemTextJsonSerializerWithOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    if (builder.Environment.IsDevelopment())
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.LimitToEndpoint = true;
        options.HttpClientFactory = () =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            return new HttpClient(handler, disposeHandler: false);
        };
    }
});
builder.Services.AddShortenerDataAccess();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CleanupExpiredLinksCommand>());

var host = builder.Build();

host.Run();
