var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var cosmos = builder
    .AddAzureCosmosDB("cosmos-db")
    .RunAsEmulator(container =>
    {
        container.WithEnvironment("AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1");
    });
var database = cosmos.AddCosmosDatabase("cosmos");

builder.AddProject<Projects.Shortener_Host_Api>("api")
    .WithReference(cache)
    .WithReference(database);

builder.Build().Run();
