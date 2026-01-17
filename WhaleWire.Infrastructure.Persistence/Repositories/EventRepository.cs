using Microsoft.EntityFrameworkCore;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(
    WhaleWireDbContext db, TimeProvider timeProvider) : IEventRepository
{
    public async Task<bool> UpsertEventIdempotentAsync(
        string eventId,
        string chain,
        string address,
        long lt,
        string txHash,
        DateTime blockTime,
        string rawJson,
        CancellationToken ct = default)
    {
        var exists = await db.Events.AnyAsync(e => e.EventId == eventId, ct);
        if (exists)
            return false;

        var entity = new BlockchainEvent
        {
            EventId = eventId,
            Chain = chain,
            Address = address,
            Lt = lt,
            TxHash = txHash,
            BlockTime = blockTime,
            RawJson = rawJson,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Events.Add(entity);

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another process inserted the same event_id concurrently
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true ||
        ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}
