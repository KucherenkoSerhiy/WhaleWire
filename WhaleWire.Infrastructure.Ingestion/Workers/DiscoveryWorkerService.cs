using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhaleWire.Application.UseCases;

namespace WhaleWire.Infrastructure.Ingestion.Workers;

public sealed class DiscoveryWorkerService(
    IServiceProvider serviceProvider,
    IOptions<DiscoveryOptions> options,
    ILogger<DiscoveryWorkerService> logger) : BackgroundService
{
    private readonly DiscoveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogWarning("Discovery disabled via configuration");
            return;
        }

        logger.LogInformation(
            "DiscoveryWorkerService started (refreshing every {Interval} minutes)",
            _options.PollingIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDiscoveryCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during ingestion cycle");
            }
            
            await Task.Delay(
                TimeSpan.FromMinutes(_options.PollingIntervalMinutes),
                stoppingToken);
        }

        logger.LogInformation("DiscoveryWorkerService stopped");
    }

    private async Task RunDiscoveryCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var discoveryUseCase = scope.ServiceProvider.GetRequiredService<IDiscoveryUseCase>();

            var count = await discoveryUseCase.ExecuteAsync(_options.TopAccountsLimit, ct);
            
            logger.LogInformation("Discovery completed: {Count} addresses updated", count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during discovery cycle");
        }
    }
}
