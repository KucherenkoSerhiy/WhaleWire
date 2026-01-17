using Microsoft.EntityFrameworkCore;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Repositories;

public sealed class LeaseRepository(
    WhaleWireDbContext db,
    TimeProvider timeProvider) : ILeaseRepository
{
    public async Task<bool> TryAcquireLeaseAsync(
        string leaseKey,
        string ownerId,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now + duration;

        var lease = await db.AddressLeases.FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, ct);

        if (lease is null)
        {
            return await AcquireNewLeaseAsync(leaseKey, ownerId, ct, expiresAt);
        }

        if (lease.IsHeldByAnotherOwner(now, ownerId))
            return false;

        lease.Renew(ownerId, expiresAt);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> AcquireNewLeaseAsync(string leaseKey, string ownerId, CancellationToken ct,
        DateTime expiresAt)
    {
        var lease = new AddressLease
        {
            LeaseKey = leaseKey,
            OwnerId = ownerId,
            ExpiresAt = expiresAt
        };
        db.AddressLeases.Add(lease);

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another process acquired the lease concurrently
            return false;
        }
    }

    public async Task<bool> RenewLeaseAsync(
        string leaseKey,
        string ownerId,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        var lease = await db.AddressLeases.FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, ct);
        if (lease is null || !lease.BelongsTo(ownerId))
            return false;

        var newExpirationTime = timeProvider.GetUtcNow().UtcDateTime + duration;
        lease.ExpiresAt = newExpirationTime;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReleaseLeaseAsync(
        string leaseKey,
        string ownerId,
        CancellationToken ct = default)
    {
        var lease = await db.AddressLeases.FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, ct);
        if (lease is null || !lease.BelongsTo(ownerId))
            return false;

        db.AddressLeases.Remove(lease);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true ||
        ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}