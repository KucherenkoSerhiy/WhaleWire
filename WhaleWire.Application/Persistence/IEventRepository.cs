namespace WhaleWire.Application.Persistence;

public interface IEventRepository
{
    /// <summary>
    /// Inserts an event if it doesn't exist (idempotent by event_id).
    /// Returns true if the event was inserted, false if it already existed.
    /// </summary>
    Task<bool> UpsertEventIdempotentAsync(
        string eventId,
        string chain,
        string address,
        long lt,
        string txHash,
        DateTime blockTime,
        string rawJson,
        CancellationToken ct = default);
}
