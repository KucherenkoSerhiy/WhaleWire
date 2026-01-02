namespace WhaleWire.Infrastructure.Persistence.Entities;

public sealed class Checkpoint
{
    public required string Chain { get; init; }
    public required string Address { get; init; }
    public required string Provider { get; init; }
    public required long LastLt { get; set; }
    public required string LastHash { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

