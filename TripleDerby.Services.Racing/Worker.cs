namespace TripleDerby.Services.Racing;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly AzureServiceBusRaceConsumer _consumer;

    public Worker(
        ILogger<Worker> logger,
        AzureServiceBusRaceConsumer consumer)
    {
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Racing Worker starting at: {time}", DateTimeOffset.Now);

        await _consumer.StartAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        finally
        {
            await _consumer.StopAsync();
        }
    }
}
