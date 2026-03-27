using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WhaleWire.Messages;

namespace WhaleWire.Application.Alerts;

/// <summary>One JSON-serializable whale decision line for audit sinks (ADR 0002).</summary>
public sealed record WhaleDecisionRecord
{
    public required string Timestamp { get; init; }
    public string Category { get; init; } = "whale_decision";
    public required string Outcome { get; init; }
    public string? ReasonCode { get; init; }
    public required string Channel { get; init; }
    public string? EventId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Chain { get; init; }
    public string? Address { get; init; }
    public string? Provider { get; init; }
    public string? AssetId { get; init; }
    public string? Direction { get; init; }
    public decimal? Amount { get; init; }
    public string? Message { get; init; }

    public static WhaleDecisionRecord ForSent(Alert alert) => new()
    {
        Timestamp = UtcNowIso(),
        Outcome = "sent",
        Channel = "console",
        EventId = alert.EventId,
        CorrelationId = alert.CorrelationId,
        Chain = alert.Chain,
        Address = alert.Address ?? alert.WalletAddress,
        Provider = alert.Provider,
        AssetId = alert.AssetId,
        Direction = alert.Direction,
        Amount = alert.Amount,
        Message = alert.Message
    };

    public static WhaleDecisionRecord ForNoQualifyingTransfer(BlockchainEvent evt, string? correlationId) => new()
    {
        Timestamp = UtcNowIso(),
        Outcome = "suppressed",
        ReasonCode = WhaleDecisionReasonCodes.NoQualifyingTransfer,
        Channel = "none",
        EventId = evt.EventId,
        CorrelationId = correlationId ?? evt.CorrelationId,
        Chain = evt.Chain,
        Address = evt.Address,
        Provider = evt.Provider
    };

    public static WhaleDecisionRecord ForRawJsonParseError(BlockchainEvent evt) => new()
    {
        Timestamp = UtcNowIso(),
        Outcome = "failed",
        ReasonCode = WhaleDecisionReasonCodes.RawJsonParseError,
        Channel = "none",
        EventId = evt.EventId,
        CorrelationId = evt.CorrelationId,
        Chain = evt.Chain,
        Address = evt.Address,
        Provider = evt.Provider
    };

    private static string UtcNowIso() =>
        DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ToJsonLine() => JsonSerializer.Serialize(this, JsonOptions);
}
