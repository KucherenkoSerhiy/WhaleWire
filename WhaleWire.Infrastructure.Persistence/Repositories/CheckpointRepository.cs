using Microsoft.EntityFrameworkCore;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Repositories;

public sealed class CheckpointRepository(WhaleWireDbContext db) : ICheckpointRepository
{
    public async Task<CheckpointData?> GetCheckpointAsync(
        string chain,
        string address,
        string provider,
        CancellationToken ct = default)
    {
        var checkpoint = await db.Checkpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Chain == chain &&
                c.Address == address &&
                c.Provider == provider, ct);

        return checkpoint is null
            ? null
            : new CheckpointData(checkpoint.LastLt, checkpoint.LastHash, checkpoint.UpdatedAt);
    }

    public async Task UpdateCheckpointMonotonicAsync(
        string chain,
        string address,
        string provider,
        long lastLt,
        string lastHash,
        CancellationToken ct = default)
    {
        var checkpoint = await db.Checkpoints
            .FirstOrDefaultAsync(c =>
                c.Chain == chain &&
                c.Address == address &&
                c.Provider == provider, ct);

        if (checkpoint is null)
        {
            checkpoint = Checkpoint.Create(chain, address, provider, lastLt, lastHash);
            db.Checkpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.Update(lastLt, lastHash);
        }

        await db.SaveChangesAsync(ct);
    }
}