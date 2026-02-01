using Microsoft.EntityFrameworkCore;
using WhaleWire.Application.Persistence;
using WhaleWire.Infrastructure.Persistence.Entities;

namespace WhaleWire.Infrastructure.Persistence.Repositories;

public sealed class MonitoredAddressRepository(WhaleWireDbContext db) : IMonitoredAddressRepository
{
    public async Task<IReadOnlyList<string>> GetActiveAddressesAsync(
        string chain,
        string provider,
        CancellationToken ct = default)
    {
        return await db.MonitoredAddresses
            .Where(m => m.Chain == chain && m.Provider == provider && m.IsActive)
            .GroupBy(m => m.Address)
            .Select(g => g.Key)
            .ToListAsync(ct);
    }

    public async Task UpsertAddressAsync(
        string chain,
        string address,
        string provider,
        string assetId,
        string balance,
        CancellationToken ct = default)
    {
        var monitored = await db.MonitoredAddresses
            .FirstOrDefaultAsync(m =>
                m.Chain == chain &&
                m.Address == address &&
                m.Provider == provider &&
                m.AssetId == assetId, ct);

        if (monitored is null)
        {
            monitored = new MonitoredAddress
            {
                Chain = chain,
                Address = address,
                Provider = provider,
                AssetId = assetId,
                Balance = balance,
                IsActive = true,
                DiscoveredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.MonitoredAddresses.Add(monitored);
        }
        else
        {
            monitored.Balance = balance;
            monitored.IsActive = true;
            monitored.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAddressAsync(
        string chain,
        string address,
        string provider,
        string assetId,
        CancellationToken ct = default)
    {
        var monitored = await db.MonitoredAddresses
            .FirstOrDefaultAsync(m =>
                m.Chain == chain &&
                m.Address == address &&
                m.Provider == provider &&
                m.AssetId == assetId, ct);

        if (monitored is not null)
        {
            monitored.IsActive = false;
            monitored.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
