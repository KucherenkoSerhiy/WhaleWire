using System.Numerics;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Persistence;

namespace WhaleWire.Application.UseCases;

public sealed class DiscoveryUseCase(
    ITopAccountsClient topAccountsClient,
    IMonitoredAddressRepository monitoredAddressRepository,
    IBlockchainClient blockchainClient) : IDiscoveryUseCase
{
    public async Task<int> ExecuteAsync(int limit, CancellationToken ct = default)
    {
        var assetHolders = await topAccountsClient.GetTopAccountsByAssetAsync(limit, ct);

        var totalAddresses = 0;
        foreach (var asset in assetHolders)
        {
            foreach (var holder in asset.Holders)
            {
                await monitoredAddressRepository.UpsertAddressAsync(
                    chain: blockchainClient.Chain,
                    address: holder.Address,
                    provider: blockchainClient.Provider,
                    assetId: asset.AssetIdentifier,
                    balance: holder.Balance.ToString(),
                    ct: ct);
                totalAddresses++;
            }
        }

        return totalAddresses;
    }
}

public interface ITopAccountsClient
{
    Task<IReadOnlyList<AssetTopHolders>> GetTopAccountsByAssetAsync(
        int limit,
        CancellationToken ct = default);
}

public sealed class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    public bool Enabled { get; init; } = true;
    public int PollingIntervalMinutes { get; init; } = 60;
    /// <summary>If > 0, overrides PollingIntervalMinutes for fast tests.</summary>
    public int PollingIntervalSeconds { get; init; }
    public int TopAccountsLimit { get; init; } = 100;
}
