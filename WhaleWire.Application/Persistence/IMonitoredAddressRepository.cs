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
        string assetId,
        string balance,
        CancellationToken ct = default);

    Task DeactivateAddressAsync(
        string chain,
        string address,
        string provider,
        string assetId,
        CancellationToken ct = default);

    /// <summary>Distinct wallet addresses with at least one active monitored row for the chain/provider.</summary>
    Task<int> CountActiveDistinctAddressesAsync(
        string chain,
        string provider,
        CancellationToken ct = default);
}
