var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sqlServer = builder.AddSqlServer("sql", port: 59944)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var sql = sqlServer.AddDatabase("TripleDerby");

var rabbit = builder.AddRabbitMQ("messaging")
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// Azure Service Bus Emulator for Race microservice (Feature 011)
// Uses emulator for local dev, supports seamless production migration
// Runs as Docker container using existing SQL Server instance as backing store
var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator(configureContainer => configureContainer
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("SQL_SERVER", $"{sqlServer.Resource.Name}:1433")
        .WithEnvironment("MSSQL_SA_PASSWORD", "Password_01"))
    .WaitFor(sqlServer);

serviceBus.AddServiceBusQueue("race-requests");
serviceBus.AddServiceBusQueue("race-completions");

var apiService = builder.AddProject<Projects.TripleDerby_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)      // Breeding messages
    .WaitFor(rabbit)
    .WithReference(serviceBus)  // Race messages (NEW)
    .WaitFor(serviceBus);

builder.AddProject<Projects.TripleDerby_Web>("admin")
    .WithReference(apiService)
    .WaitFor(apiService);
    
builder.AddProject<Projects.TripleDerby_Services_Breeding>("breeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit);

// Racing microservice (Feature 011)
builder.AddProject<Projects.TripleDerby_Services_Racing>("racing")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

builder.Build().Run();
