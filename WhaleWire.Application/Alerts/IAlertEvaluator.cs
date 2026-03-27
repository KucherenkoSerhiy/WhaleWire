using WhaleWire.Messages;

namespace WhaleWire.Application.Alerts;

public interface IAlertEvaluator
{
    Task<AlertEvaluationResult> EvaluateAsync(
        BlockchainEvent blockchainEvent,
        CancellationToken ct = default);
}

public sealed record Alert(
    string AssetId,
    string WalletAddress,
    decimal Amount,
    string Direction,
    string Message,
    string? EventId = null,
    string? Chain = null,
    string? Address = null,
    string? Provider = null,
    string? CorrelationId = null);
