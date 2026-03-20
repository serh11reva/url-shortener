var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
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
    .WaitFor(cache)
    .WaitFor(database);

builder.AddAzureFunctionsProject<Projects.Shortener_Host_Functions>("functions")
    .WithHostStorage(storage)
    .WithReference(cache)
    .WithReference(database)
    .WaitFor(storage)
    .WaitFor(cache)
    .WaitFor(database);

builder.Build().Run();
