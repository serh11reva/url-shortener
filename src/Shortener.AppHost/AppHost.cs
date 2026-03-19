var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
var cosmos = builder
    .AddAzureCosmosDB("cosmos-db")
    .RunAsEmulator();
var database = cosmos.AddCosmosDatabase("cosmos", databaseName: "shortener");

builder.AddProject<Projects.Shortener_Host_Api>("api")
    .WithReference(cache)
    .WithReference(database)
    .WaitFor(cache)
    .WaitFor(database);

builder.Build().Run();
