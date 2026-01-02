namespace WhaleWire.Infrastructure.Persistence.Entities;

public sealed class BlockchainEvent
{
    public long Id { get; init; }
    public required string EventId { get; init; }
    public required string Chain { get; init; }
    public required string Address { get; init; }
    public required long Lt { get; init; }
    public required string TxHash { get; init; }
    public required DateTime BlockTime { get; init; }
    public required string RawJson { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

