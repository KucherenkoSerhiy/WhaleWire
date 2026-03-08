using WhaleWire.Domain;

namespace WhaleWire.Messages;

public sealed record BlockchainEvent
{
    /// <summary>Trace ID for log correlation across ingestion → publish → consume → handler. Set at publish time.</summary>
    public string? CorrelationId { get; init; }
    public required string EventId { get; init; }
    public required string Chain { get; init; }
    public required string Provider { get; init; }
    public required string Address { get; init; }
    public required Cursor Cursor { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required string RawJson { get; init; }
}