using Microsoft.Extensions.Logging;
using WhaleWire.Application.Alerts;
using WhaleWire.Application.CorrelationId;
using WhaleWire.Application.Metrics;

namespace WhaleWire.Infrastructure.Notifications.Notifiers;

public sealed class ConsoleAlertNotifier(
    ILogger<ConsoleAlertNotifier> logger,
    ICorrelationIdAccessor correlationIdAccessor,
    IWhaleWireMetrics metrics) : IAlertNotifier
{
    public Task NotifyAsync(Alert alert, CancellationToken ct = default)
    {
        metrics.RecordAlertFired(alert.AssetId, alert.Direction);
        var truncatedAddress = alert.WalletAddress.Length > 10
            ? alert.WalletAddress[..10] + "..."
            : alert.WalletAddress;
        
        logger.LogWarning(
            "🐋 WHALE ALERT: {Asset} - {Wallet} {Direction} {Amount} | {Message}. CorrelationId: {CorrelationId}",
            alert.AssetId,
            truncatedAddress,
            alert.Direction,
            alert.Amount,
            alert.Message,
            correlationIdAccessor.CorrelationId);
        
        return Task.CompletedTask;
    }
}
