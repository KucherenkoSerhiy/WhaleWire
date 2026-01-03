using Microsoft.EntityFrameworkCore;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Repositories;

public sealed class LeaseRepository(WhaleWireDbContext db) : ILeaseRepository
{
    public async Task<bool> TryAcquireLeaseAsync(
        string leaseKey,
        string ownerId,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now + duration;

        var lease = await db.AddressLeases.FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, ct);

        if (lease is null)
        {
            // No lease exists, create one
            lease = new AddressLease
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

        // Lease exists - check if expired or owned by us
        if (lease.ExpiresAt <= now)
        {
            // Expired, take it over
            lease.OwnerId = ownerId;
            lease.ExpiresAt = expiresAt;
            await db.SaveChangesAsync(ct);
            return true;
        }

        if (lease.OwnerId == ownerId)
        {
            // Already ours, extend it
            lease.ExpiresAt = expiresAt;
            await db.SaveChangesAsync(ct);
            return true;
        }

        // Held by another owner
        return false;
    }

    public async Task<bool> RenewLeaseAsync(
        string leaseKey,
        string ownerId,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        var lease = await db.AddressLeases.FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, ct);

        if (lease is null || lease.OwnerId != ownerId)
            return false;

        lease.ExpiresAt = DateTime.UtcNow + duration;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReleaseLeaseAsync(
        string leaseKey,
        string ownerId,
        CancellationToken ct = default)
    {
        var lease = await db.AddressLeases.FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, ct);

        if (lease is null || lease.OwnerId != ownerId)
            return false;

        db.AddressLeases.Remove(lease);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true ||
        ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}
