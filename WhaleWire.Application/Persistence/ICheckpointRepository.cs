namespace WhaleWire.Application.Persistence;

public record CheckpointData(long LastLt, string LastHash, DateTime UpdatedAt);

public interface ICheckpointRepository
{
    /// <summary>
    /// Gets the checkpoint for a specific chain/address/provider combination.
    /// Returns null if no checkpoint exists.
    /// </summary>
    Task<CheckpointData?> GetCheckpointAsync(
        string chain,
        string address,
        string provider,
        CancellationToken ct = default);

    /// <summary>
    /// Updates checkpoint monotonically (only advances forward).
    /// Should only be called after events are successfully persisted.
    /// </summary>
    Task UpdateCheckpointMonotonicAsync(
        string chain,
        string address,
        string provider,
        long lastLt,
        string lastHash,
        CancellationToken ct = default);
}
