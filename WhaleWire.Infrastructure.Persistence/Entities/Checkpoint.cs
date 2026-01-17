using WhaleWire.Infrastructure.Persistence.Exceptions;

namespace WhaleWire.Infrastructure.Persistence.Entities;

public sealed class Checkpoint
{
    public static Checkpoint Create(
        string chain,
        string address,
        string provider,
        long lastLt,
        string lastHash)
    {
        return new Checkpoint
        {
            Chain = chain,
            Address = address,
            Provider = provider,
            LastLt = lastLt,
            LastHash = lastHash,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public required string Chain { get; init; }
    public required string Address { get; init; }
    public required string Provider { get; init; }
    public required long LastLt { get; set; }
    public required string LastHash { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


    public void Update(long lastLt, string lastHash)
    {
        if (LastLt == lastLt && LastHash != lastHash)
        {
            throw new CheckpointConflictException(lastLt, LastHash, lastHash);
        }

        if (lastLt > LastLt)
        {
            LastLt = lastLt;
            LastHash = lastHash;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

