using Microsoft.Extensions.Options;
using WhaleWire.Configuration;

namespace WhaleWire.Services;

public sealed class SchedulerService(
    ILogger<SchedulerService> logger,
    IOptions<SchedulerOptions> options)
    : BackgroundService
{
    private readonly SchedulerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SchedulerService is disabled by configuration");
            return;
        }

        logger.LogInformation(
            "SchedulerService started with polling interval of {PollingInterval} seconds",
            _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteScheduledWorkAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during scheduled work execution");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("SchedulerService stopped");
    }

    private Task ExecuteScheduledWorkAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Executing scheduled work cycle");
        // TODO: Implement actual scheduling logic
        return Task.CompletedTask;
    }
}

