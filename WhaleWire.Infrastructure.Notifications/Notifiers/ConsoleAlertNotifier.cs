using Microsoft.Extensions.Logging;
using WhaleWire.Application.Alerts;

namespace WhaleWire.Infrastructure.Notifications.Notifiers;

public sealed class ConsoleAlertNotifier(ILogger<ConsoleAlertNotifier> logger) : IAlertNotifier
{
    public Task NotifyAsync(Alert alert, CancellationToken ct = default)
    {
        var truncatedAddress = alert.WalletAddress.Length > 10
            ? alert.WalletAddress[..10] + "..."
            : alert.WalletAddress;
        
        logger.LogWarning(
            "🐋 WHALE ALERT: {Asset} - {Wallet} {Direction} {Amount} | {Message}",
            alert.AssetId,
            truncatedAddress,
            alert.Direction,
            alert.Amount,
            alert.Message);
        
        return Task.CompletedTask;
    }
}
