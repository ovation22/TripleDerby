namespace TripleDerby.Services.Feeding;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Feeding worker starting (message consumer not yet implemented).");

        try
        {
            // Keep the background service alive until host shutdown
            // TODO: Replace with actual message consumer in later phase
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the host signals shutdown
        }
        finally
        {
            _logger.LogInformation("Feeding worker stopped.");
        }
    }
}
