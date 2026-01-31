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
            .OrderByDescending(m => m.Balance)
            .Select(m => m.Address)
            .ToListAsync(ct);
    }

    public async Task UpsertAddressAsync(
        string chain,
        string address,
        string provider,
        string balance,
        CancellationToken ct = default)
    {
        var monitored = await GetMonitoredAddressAsync(chain, address, provider, ct);

        if (monitored is null)
        {
            monitored = new MonitoredAddress
            {
                Chain = chain,
                Address = address,
                Provider = provider,
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
        CancellationToken ct = default)
    {
        var monitored = await GetMonitoredAddressAsync(chain, address, provider, ct);

        if (monitored is not null)
        {
            monitored.IsActive = false;
            monitored.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<MonitoredAddress?> GetMonitoredAddressAsync(string chain, string address, string provider, CancellationToken ct)
    {
        return await db.MonitoredAddresses
            .FirstOrDefaultAsync(m =>
                m.Chain == chain &&
                m.Address == address &&
                m.Provider == provider, ct);
    }
}
