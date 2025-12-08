using Microsoft.EntityFrameworkCore;
using Serilog;
using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Generators;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Infrastructure.Data.Repositories;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.ServiceDefaults;
using TripleDerby.Services.Breeding;

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
builder.Services.AddScoped<IBreedingRequestProcessor, BreedingRequestProcessor>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<RabbitMqBreedingConsumer>();
builder.Services.AddSingleton<ITimeManager, TimeManager>();
builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();
builder.Services.AddSingleton<IHorseNameGenerator, HorseNameGenerator>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

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
