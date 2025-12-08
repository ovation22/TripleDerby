namespace TripleDerby.Services.Breeding;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqBreedingConsumer _consumer;

    public Worker(ILogger<Worker> logger, RabbitMqBreedingConsumer consumer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting RabbitMqBreedingConsumer.");

        try
        {
            // start the consumer; allow it to observe the host cancellation token
            await _consumer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMqBreedingConsumer. Host will stop.");
            throw; // fail fast so host doesn't run in a broken state
        }

        try
        {
            // keep the background service alive until host shutdown
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected when the host signals shutdown
        }
        finally
        {
            _logger.LogInformation("Worker stopping RabbitMqBreedingConsumer.");

            try
            {
                await _consumer.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping RabbitMqBreedingConsumer.");
            }

            try
            {
                _consumer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while disposing RabbitMqBreedingConsumer.");
            }

            _logger.LogInformation("Worker stopped.");
        }
    }
}
