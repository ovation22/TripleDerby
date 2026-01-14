var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sql = builder.AddSqlServer("sql", port: 59944)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("TripleDerby");

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
