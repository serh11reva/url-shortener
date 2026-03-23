var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var messaging = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator()
    .AddServiceBusQueue("clicks");
var storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator();
var cosmos = builder
    .AddAzureCosmosDB("cosmos-db")
    .RunAsEmulator();
var database = cosmos.AddCosmosDatabase("cosmos", databaseName: "shortener");

builder.AddProject<Projects.Shortener_Host_Api>("api")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(messaging)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(messaging);

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
