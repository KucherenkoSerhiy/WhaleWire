using Microsoft.Extensions.Logging;
using WhaleWire.Application.Alerts;

namespace WhaleWire.Infrastructure.Notifications.Notifiers;

public sealed class ConsoleAlertNotifier(ILogger<ConsoleAlertNotifier> logger) : IAlertNotifier
{
    public Task NotifyAsync(Alert alert, CancellationToken ct = default)
    {
        logger.LogWarning(
            "🐋 WHALE ALERT: {Asset} - {Wallet} {Direction} {Amount} | {Message}",
            alert.AssetId,
            alert.WalletAddress[..10] + "...",
            alert.Direction,
            alert.Amount,
            alert.Message);
        
        return Task.CompletedTask;
    }
}
