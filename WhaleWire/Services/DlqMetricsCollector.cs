using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WhaleWire.Application.Metrics;
using WhaleWire.Configuration;

namespace WhaleWire.Services;

/// <summary>
/// Periodically updates whalewire_dlq_messages_total from RabbitMQ.
/// One message in DLQ = alert (strict).
/// </summary>
public sealed class DlqMetricsCollector(
    IConnection connection,
    IWhaleWireMetrics metrics,
    IOptions<MetricsCollectorOptions> options,
    ILogger<DlqMetricsCollector> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        options.Value.IntervalSeconds is > 0 ? options.Value.IntervalSeconds : 30);
    private const string BlockchainEventDlq = "whalewire.blockchainevent.queue.dlq";

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
                logger.LogWarning(ex, "DLQ metrics collection failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CollectAsync(CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync();
        var result = await channel.QueueDeclarePassiveAsync(BlockchainEventDlq, ct);
        metrics.RecordDlqMessageCount(BlockchainEventDlq, (int)result.MessageCount);
    }
}
