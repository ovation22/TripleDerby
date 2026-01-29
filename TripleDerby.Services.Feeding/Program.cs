using Microsoft.EntityFrameworkCore;
using Serilog;
using TripleDerby.Core.Abstractions.Data;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Infrastructure.Data.Repositories;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.ServiceDefaults;
using TripleDerby.Services.Feeding;
using TripleDerby.Services.Feeding.Abstractions;
using TripleDerby.Services.Feeding.Calculators;
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

// SQL SERVER (Commented for local dev)
// builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
//     options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

// POSTGRESQL (Active for local dev)
builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseNpgsql(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<TripleDerbyContext>());
builder.Services.AddScoped<ITransactionManager, TransactionManager>();
builder.Services.AddScoped<ITripleDerbyRepository, TripleDerbyRepository>();
builder.Services.AddScoped<IFeedingCalculator, FeedingCalculator>();
builder.Services.AddScoped<IFeedingExecutor, FeedingExecutor>();
builder.Services.AddScoped<IFeedingRequestProcessor, FeedingRequestProcessor>();

// Register message bus (publishes and consumes via configured provider)
builder.Services.AddMessageBus(builder.Configuration);

// Register message consumer
builder.Services.AddSingleton<IMessageConsumer, GenericMessageConsumer<FeedingRequested, IFeedingRequestProcessor>>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ITimeManager, TimeManager>();
builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();

// SQL SERVER (Commented for local dev)
// builder.AddSqlServerClient(connectionName: "sql");

// POSTGRESQL (Active for local dev)
builder.AddNpgsqlDataSource(connectionName: "sql");

builder.AddRabbitMQClient(connectionName: "messaging");

try
{
    Log.Information("Starting Feeding worker host");
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
