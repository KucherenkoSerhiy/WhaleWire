using WhaleWire.Messages;

namespace WhaleWire.Application.Alerts;

public interface IAlertEvaluator
{
    Task<IReadOnlyList<Alert>> EvaluateAsync(
        BlockchainEvent blockchainEvent, 
        CancellationToken ct = default);
}

public sealed record Alert(
    string AssetId,
    string WalletAddress,
    decimal Amount,
    string Direction,
    string Message);
