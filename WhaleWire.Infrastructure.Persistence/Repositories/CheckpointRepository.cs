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

    public async Task UpdateCheckpointAsync(
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
            checkpoint = new Checkpoint
            {
                Chain = chain,
                Address = address,
                Provider = provider,
                LastLt = lastLt,
                LastHash = lastHash,
                UpdatedAt = DateTime.UtcNow
            };
            db.Checkpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.LastLt = lastLt;
            checkpoint.LastHash = lastHash;
            checkpoint.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
