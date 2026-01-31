namespace WhaleWire.Application.Persistence;

public interface IMonitoredAddressRepository
{
    Task<IReadOnlyList<string>> GetActiveAddressesAsync(
        string chain,
        string provider,
        CancellationToken ct = default);

    Task UpsertAddressAsync(
        string chain,
        string address,
        string provider,
        string balance,
        CancellationToken ct = default);

    Task DeactivateAddressAsync(
        string chain,
        string address,
        string provider,
        CancellationToken ct = default);
}
