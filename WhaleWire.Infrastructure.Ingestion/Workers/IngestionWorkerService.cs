using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;
using WhaleWire.Infrastructure.Ingestion.Configuration;

namespace WhaleWire.Infrastructure.Ingestion.Workers;

public sealed class IngestionWorkerService(
    IServiceProvider serviceProvider,
    IOptions<IngestionOptions> options,
    ILogger<IngestionWorkerService> logger) : BackgroundService
{
    private readonly IngestionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogWarning("Ingestion disabled via configuration");
            return;
        }

        logger.LogInformation(
            "IngestionWorkerService started (polling every {Interval}s, DB-driven)",
            _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await IngestAllAddressesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during ingestion cycle");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                stoppingToken);
        }

        logger.LogInformation("IngestionWorkerService stopped");
    }

    private async Task IngestAllAddressesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var ingestionCoordinator = scope.ServiceProvider.GetRequiredService<IIngestionCoordinatorUseCase>();

        var result = await ingestionCoordinator.ExecuteAsync(ct);

        if (result.TotalEventsPublished > 0)
        {
            logger.LogInformation(
                "Ingestion cycle: {Events} events from {Addresses} addresses",
                result.TotalEventsPublished, result.AddressesProcessed);
        }

        var failures = result.Results.Where(r => r.Error != null).ToList();
        if (failures.Count > 0)
        {
            logger.LogWarning(
                "{FailCount} addresses failed: {Addresses}",
                failures.Count,
                string.Join(", ", failures.Select(f => f.Address)));
        }
    }
}
