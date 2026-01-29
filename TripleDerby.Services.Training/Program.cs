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
using TripleDerby.Services.Training;
using TripleDerby.Services.Training.Abstractions;
using TripleDerby.Services.Training.Calculators;
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
builder.Services.AddScoped<ITrainingExecutor, TrainingExecutor>();
builder.Services.AddScoped<ITrainingRequestProcessor, TrainingRequestProcessor>();
builder.Services.AddScoped<ITrainingCalculator, TrainingCalculator>();

// Register message bus (publishes and consumes via configured provider)
builder.Services.AddMessageBus(builder.Configuration);

// Register message consumer
builder.Services.AddSingleton<IMessageConsumer, GenericMessageConsumer<TrainingRequested, ITrainingRequestProcessor>>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ITimeManager, TimeManager>();
builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();

// SQL SERVER (Commented for local dev)
// builder.AddSqlServerClient(connectionName: "sql");

// POSTGRESQL (Active for local dev)
// Connection string automatically provided by Aspire via .WithReference(sql)
// Manual DbContext configuration above

builder.AddRabbitMQClient(connectionName: "messaging");

try
{
    Log.Information("Starting Training worker host");
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
