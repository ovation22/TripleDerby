using Microsoft.EntityFrameworkCore;
using Serilog;
using TripleDerby.Core.Abstractions.Data;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Services;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Infrastructure.Data.Repositories;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.ServiceDefaults;
using TripleDerby.Services.Racing;
using TripleDerby.Services.Racing.Abstractions;
using TripleDerby.Services.Racing.Calculators;
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

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITripleDerbyRepository, TripleDerbyRepository>();
builder.Services.AddScoped<IRaceRequestProcessor, RaceRequestProcessor>();

// Racing dependencies (same as API)
builder.Services.AddSingleton<ITimeManager, TimeManager>();
builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();
builder.Services.AddScoped<ISpeedModifierCalculator, SpeedModifierCalculator>();
builder.Services.AddScoped<IStaminaCalculator, StaminaCalculator>();
builder.Services.AddScoped<IRaceCommentaryGenerator, RaceCommentaryGenerator>();
builder.Services.AddScoped<IPurseCalculator, PurseCalculator>();
builder.Services.AddScoped<IOvertakingManager, OvertakingManager>();
builder.Services.AddScoped<IEventDetector, EventDetector>();
builder.Services.AddScoped<IStatProgressionCalculator, StatProgressionCalculator>();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IRaceRunService, RaceRunService>();
builder.Services.AddScoped<IRaceExecutor, RaceExecutor>();

// Messaging - Register message bus (publishes and consumes via configured provider)
builder.Services.AddMessageBus(builder.Configuration);

// Register message consumer
builder.Services.AddSingleton<IMessageConsumer, GenericMessageConsumer<RaceRequested, IRaceRequestProcessor>>();

builder.Services.AddHostedService<Worker>();

builder.AddSqlServerClient(connectionName: "sql");
builder.AddRabbitMQClient(connectionName: "messaging");

try
{
    Log.Information("Starting Racing worker host");
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

// Make the implicit Program class internal to avoid conflicts with other projects
internal partial class Program { }
