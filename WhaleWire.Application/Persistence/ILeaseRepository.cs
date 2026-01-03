namespace WhaleWire.Application.Persistence;

public interface ILeaseRepository
{
    /// <summary>
    /// Attempts to acquire a lease for the given key.
    /// Returns true if the lease was acquired, false if it's held by another owner.
    /// </summary>
    Task<bool> TryAcquireLeaseAsync(
        string leaseKey,
        string ownerId,
        TimeSpan duration,
        CancellationToken ct = default);

    /// <summary>
    /// Renews an existing lease. Only succeeds if the current owner matches.
    /// Returns true if renewed, false if lease doesn't exist or is owned by someone else.
    /// </summary>
    Task<bool> RenewLeaseAsync(
        string leaseKey,
        string ownerId,
        TimeSpan duration,
        CancellationToken ct = default);

    /// <summary>
    /// Releases a lease. Only succeeds if the current owner matches.
    /// </summary>
    Task<bool> ReleaseLeaseAsync(
        string leaseKey,
        string ownerId,
        CancellationToken ct = default);
}
