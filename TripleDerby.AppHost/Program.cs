var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// ============================================================================
// DATABASE PROVIDER CONFIGURATION
// See docs/DATABASE_SWITCHING.md for switching between SQL Server and PostgreSQL
// ============================================================================

// SQL SERVER (Commented for local dev)
// var sql = builder.AddSqlServer("sql", port: 59944)
//     .WithDataVolume()
//     .WithLifetime(ContainerLifetime.Persistent)
//     .AddDatabase("TripleDerby");

// POSTGRESQL (Active for local dev)
var postgres = builder.AddPostgres("sql", port: 55432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();
var sql = postgres.AddDatabase("TripleDerby");

var rabbit = builder.AddRabbitMQ("messaging")
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var apiService = builder.AddProject<Projects.TripleDerby_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit);

builder.AddProject<Projects.TripleDerby_Web>("admin")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.TripleDerby_Services_Breeding>("breeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit);

builder.AddProject<Projects.TripleDerby_Services_Feeding>("feeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit);

builder.AddProject<Projects.TripleDerby_Services_Training>("training")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit);

builder.AddProject<Projects.TripleDerby_Services_Racing>("racing")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit);

builder.Build().Run();
