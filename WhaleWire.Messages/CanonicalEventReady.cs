namespace WhaleWire.Messages;

public sealed record CanonicalEventReady
{
    public required string EventId { get; init; }
    public required string Chain { get; init; }
    public required string Provider { get; init; }
    public required string Address { get; init; }
    public required long Lt { get; init; }
    public required string TxHash { get; init; }
    public required string RawJson { get; init; }
    public required DateTime OccurredAt { get; init; }
}