using Microsoft.EntityFrameworkCore;
using Serilog;
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

builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

builder.Services.AddScoped<ITripleDerbyRepository, TripleDerbyRepository>();
builder.Services.AddScoped<ITrainingExecutor, TrainingExecutor>();
builder.Services.AddScoped<ITrainingRequestProcessor, TrainingRequestProcessor>();
builder.Services.AddScoped<ITrainingCalculator, TrainingCalculator>();

// Register generic message consumer with RabbitMQ adapter
builder.Services.AddSingleton<IMessageBrokerAdapter, RabbitMqBrokerAdapter>();
builder.Services.AddSingleton<IMessageConsumer, GenericMessageConsumer<TrainingRequested, ITrainingRequestProcessor>>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<ITimeManager, TimeManager>();

builder.AddSqlServerClient(connectionName: "sql");
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
