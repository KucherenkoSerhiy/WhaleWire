using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;

namespace WhaleWire.Infrastructure.Ingestion.Workers;

public sealed class DiscoveryWorkerService(
    IServiceProvider serviceProvider,
    IOptions<DiscoveryOptions> options,
    IWhaleWireMetrics metrics,
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

        var interval = _options.PollingIntervalSeconds > 0
            ? TimeSpan.FromSeconds(_options.PollingIntervalSeconds)
            : TimeSpan.FromMinutes(_options.PollingIntervalMinutes);

        logger.LogInformation(
            "DiscoveryWorkerService started (refreshing every {Interval})",
            interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDiscoveryCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during discovery cycle");
            }

            await Task.Delay(interval, stoppingToken);
        }

        logger.LogInformation("DiscoveryWorkerService stopped");
    }

    private async Task RunDiscoveryCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var discoveryUseCase = scope.ServiceProvider.GetRequiredService<IDiscoveryUseCase>();
            var blockchainClient = scope.ServiceProvider.GetRequiredService<IBlockchainClient>();
            var monitoredRepo = scope.ServiceProvider.GetRequiredService<IMonitoredAddressRepository>();

            var upsertedFromProvider = await discoveryUseCase.ExecuteAsync(_options.TopAccountsLimit, ct);
            var totalMonitored = await monitoredRepo.CountActiveDistinctAddressesAsync(
                blockchainClient.Chain,
                blockchainClient.Provider,
                ct);

            metrics.RecordActiveMonitoredAddressCount(totalMonitored);
            metrics.RecordDiscoveryLastSuccessTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            logger.LogInformation(
                "Discovery completed: {Upserted} holder rows from provider; {TotalMonitored} distinct active monitored wallets",
                upsertedFromProvider,
                totalMonitored);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during discovery cycle");
        }
    }
}
