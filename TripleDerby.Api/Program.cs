using OpenTelemetry;
using Serilog;
using Serilog.Context;
using TripleDerby.Api.Config;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Services;
using TripleDerby.Infrastructure.Caching;
using TripleDerby.Infrastructure.Data;
using TripleDerby.Infrastructure.Data.Repositories;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.Infrastructure.Utilities;
using TripleDerby.ServiceDefaults;

namespace TripleDerby.Api;

public class Program
{
    public static int Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        try {
            builder.Host.UseSerilog(Log.Logger);

            Log.Information("Starting TripleDerby.Api");

            builder.Services.Configure<RouteOptions>(options =>
            {
               options.LowercaseUrls = true;
            });

            // Add services to the container.
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddProblemDetails();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.AddSqlServerClient(connectionName: "sql");
            builder.AddRedisDistributedCache(connectionName: "cache");
            builder.AddRabbitMQClient(connectionName: "messaging");
            builder.AddAzureServiceBusClient(connectionName: "servicebus");

            builder.Services.AddCorsConfig();
            builder.Services.AddControllersConfig();
            builder.Services.AddDatabaseConfig(builder.Configuration);

            builder.Services.AddSingleton<ITimeManager, TimeManager>();
            builder.Services.AddSingleton<ICacheManager, CacheManager>();
            builder.Services.AddSingleton<IRandomGenerator, RandomGenerator>();
            builder.Services.AddScoped<ITripleDerbyRepository, TripleDerbyRepository>();

            builder.Services.AddCaching(builder.Configuration);

            // Note: Racing execution logic (calculators, commentary, purse) moved to Racing microservice in Feature 011
            // API only handles race request orchestration now

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRaceService, RaceService>();
            builder.Services.AddScoped<IRaceRunService, RaceRunService>(); // Feature 010
            builder.Services.AddScoped<IHorseService, HorseService>();
            builder.Services.AddScoped<IStatsService, StatsService>();
            builder.Services.AddScoped<IFeedingService, FeedingService>();
            builder.Services.AddScoped<IBreedingService, BreedingService>();
            builder.Services.AddScoped<ITrainingService, TrainingService>();

            // Keyed DI for dual-broker support (Feature 011)
            // Breeding uses RabbitMQ, Race uses Azure Service Bus
            builder.Services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");
            builder.Services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");

            var app = builder.Build();

            app.Use(async (ctx, next) =>
            {
                const string Header = "X-Correlation-ID";
                if (ctx.Request.Headers.TryGetValue(Header, out var values))
                {
                    var cid = values.ToString();
                    System.Diagnostics.Activity.Current?.SetTag("client.correlation_id", cid);
                    Baggage.Current = Baggage.Current.SetBaggage("client.correlation_id", cid);
                }

                var clientId = Baggage.Current.GetBaggage("client.correlation_id");
                using (LogContext.PushProperty("ClientCorrelationId", clientId ?? ""))
                {
                    await next();
                }
            });

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<TripleDerbyContext>();
                    db.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "An error occurred while migrating the database.");
                    throw;
                }
            }

            app.UseSerilogRequestLogging(x =>
            {
                x.GetLevel = (httpContext, elapsed, ex) =>
                httpContext.Request.Path.StartsWithSegments("/healthz")
                    ? Serilog.Events.LogEventLevel.Verbose
                    : ex is not null ? Serilog.Events.LogEventLevel.Error :
                    Serilog.Events.LogEventLevel.Information;
            });

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.MapControllers();

            app.Run();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.Information("Shutting down TripleDerby.Api");
            Log.CloseAndFlush();
        }
    }

    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}