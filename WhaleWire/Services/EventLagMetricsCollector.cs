using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.Persistence;
using WhaleWire.Configuration;

namespace WhaleWire.Services;

/// <summary>
/// Periodically updates whalewire_event_lag_seconds from checkpoint timestamps.
/// </summary>
public sealed class EventLagMetricsCollector(
    IServiceProvider serviceProvider,
    IWhaleWireMetrics metrics,
    IOptions<MetricsCollectorOptions> options,
    ILogger<EventLagMetricsCollector> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        options.Value.IntervalSeconds is > 0 ? options.Value.IntervalSeconds : 30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Event lag metrics collection failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CollectAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var checkpointRepo = scope.ServiceProvider.GetRequiredService<ICheckpointRepository>();

        var timestamps = await checkpointRepo.GetCheckpointTimestampsAsync(ct);
        var now = DateTime.UtcNow;
        var thresholdSeconds = options.Value.StaleLagThresholdSeconds > 0
            ? options.Value.StaleLagThresholdSeconds
            : 900;

        foreach (var ts in timestamps)
        {
            var lagSeconds = (now - ts.UpdatedAt).TotalSeconds;
            metrics.RecordEventLag(ts.Chain, ts.Address, lagSeconds);
        }

        foreach (var group in timestamps.GroupBy(t => t.Chain))
        {
            var stale = group.Count(t => (now - t.UpdatedAt).TotalSeconds > thresholdSeconds);
            metrics.RecordStaleWalletLagCount(group.Key, stale);
        }
    }
}
