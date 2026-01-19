using Microsoft.EntityFrameworkCore;
using Serilog;
using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Generators;
using TripleDerby.Core.Services;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Infrastructure.Data.Repositories;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.ServiceDefaults;
using TripleDerby.Services.Breeding;
using TripleDerby.Services.Breeding.Abstractions;
using TripleDerby.SharedKernel.Messages;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

var conn = builder.Configuration.GetConnectionString("TripleDerby");

builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

builder.Services.AddScoped<ITripleDerbyRepository, TripleDerbyRepository>();
builder.Services.AddScoped<IBreedingExecutor, BreedingExecutor>();
builder.Services.AddScoped<IBreedingRequestProcessor, BreedingRequestProcessor>();

// Register message bus (publishes and consumes via configured provider)
builder.Services.AddMessageBus(builder.Configuration);

// Register message consumer
builder.Services.AddSingleton<IMessageConsumer, GenericMessageConsumer<BreedingRequested, IBreedingRequestProcessor>>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ITimeManager, TimeManager>();
builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();
builder.Services.AddSingleton<IHorseNameGenerator, HorseNameGenerator>();
builder.Services.AddSingleton<ColorCache>();

builder.AddSqlServerClient(connectionName: "sql");
builder.AddRabbitMQClient(connectionName: "messaging");

try
{
    Log.Information("Starting Breeding worker host");
    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
