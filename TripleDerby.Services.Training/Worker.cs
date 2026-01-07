using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Services.Training;

public class Worker(ILogger<Worker> logger, IMessageConsumer consumer) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMessageConsumer _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting message consumer.");

        try
        {
            // start the consumer; allow it to observe the host cancellation token
            await _consumer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start message consumer. Host will stop.");
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
            _logger.LogInformation("Worker stopping message consumer.");

            try
            {
                await _consumer.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping message consumer.");
            }

            try
            {
                _consumer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while disposing message consumer.");
            }

            _logger.LogInformation("Worker stopped.");
        }
    }
}
