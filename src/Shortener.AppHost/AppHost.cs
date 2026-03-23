var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var messaging = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));

messaging.AddServiceBusQueue("clicks");

var storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));
var cosmos = builder
    .AddAzureCosmosDB("cosmos-db")
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));
var database = cosmos.AddCosmosDatabase("cosmos", databaseName: "shortener");

var api = builder.AddProject<Projects.Shortener_Host_Api>("api")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(messaging)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(messaging);

var apiEndpoint = api.GetEndpoint("https");

builder.AddViteApp("client-web", "../Shortener.Client.Web")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_PROXY_TARGET", apiEndpoint)
    .WithEnvironment("VITE_SHORT_LINK_ORIGIN", apiEndpoint);

builder.AddAzureFunctionsProject<Projects.Shortener_Host_Functions>("functions")
    .WithHostStorage(storage)
    .WithReference(cache)
    .WithReference(database)
    .WithReference(messaging)
    .WaitFor(storage)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(messaging);

builder.Build().Run();
