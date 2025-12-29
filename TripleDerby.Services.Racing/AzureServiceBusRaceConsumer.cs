using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Azure Service Bus consumer for race requests.
/// Processes messages from the race-requests queue and publishes results to race-completions queue.
/// </summary>
public class AzureServiceBusRaceConsumer : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AzureServiceBusRaceConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureServiceBusRaceConsumer(
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AzureServiceBusRaceConsumer> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        var connectionString = configuration.GetConnectionString("servicebus")
            ?? throw new InvalidOperationException("Service Bus connection string not found");

        _client = new ServiceBusClient(connectionString);

        _processor = _client.CreateProcessor(
            "race-requests",
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 5, // Process 5 races concurrently
                PrefetchCount = 10
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Racing Service Bus consumer");
        await _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Racing Service Bus consumer");
        await _processor.StopProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = Encoding.UTF8.GetString(args.Message.Body);

        try
        {
            var request = JsonSerializer.Deserialize<RaceRequested>(messageBody, _jsonOptions);

            if (request == null)
            {
                _logger.LogWarning("Received null RaceRequested message");
                await args.DeadLetterMessageAsync(args.Message, "NullMessage", "Message deserialized to null");
                return;
            }

            _logger.LogInformation(
                "Processing race request: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
                request.RaceId, request.HorseId, request.CorrelationId);

            // Create a scope for this message processing to resolve scoped dependencies
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var requestProcessor = scope.ServiceProvider.GetRequiredService<IRaceRequestProcessor>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            // Process the race (delegates to RaceService)
            var result = await requestProcessor.ProcessAsync(request, args.CancellationToken);

            // Publish completion message
            var completion = new RaceCompleted
            {
                CorrelationId = request.CorrelationId,
                RaceRunId = result.RaceRunId,
                RaceId = request.RaceId,
                RaceName = result.RaceName,
                WinnerHorseId = result.HorseResults.First().HorseId,
                WinnerName = result.HorseResults.First().HorseName,
                WinnerTime = result.HorseResults.First().Time,
                FieldSize = result.HorseResults.Count,
                Result = result
            };

            await publisher.PublishAsync(
                completion,
                new MessagePublishOptions { Destination = "race-completions" },
                args.CancellationToken);

            // Complete the message (remove from queue)
            await args.CompleteMessageAsync(args.Message);

            _logger.LogInformation(
                "Race completed successfully: CorrelationId={CorrelationId}, Winner={Winner}",
                request.CorrelationId, completion.WinnerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing race request: {MessageId}",
                args.Message.MessageId);

            // Dead-letter the message after max retries
            if (args.Message.DeliveryCount >= 3)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "ProcessingFailed",
                    ex.Message);
            }
            else
            {
                // Abandon to retry
                await args.AbandonMessageAsync(args.Message);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus error: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
