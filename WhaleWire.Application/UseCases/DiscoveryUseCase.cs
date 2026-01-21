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
        var accounts = await topAccountsClient.GetTopAccountsByBalanceAsync(limit, ct);

        foreach (var account in accounts)
        {
            await monitoredAddressRepository.UpsertAddressAsync(
                chain: blockchainClient.Chain,
                address: account.Address,
                provider: blockchainClient.Provider,
                balance: account.Balance,
                ct: ct);
        }

        return accounts.Count;
    }
}

public interface ITopAccountsClient
{
    Task<IReadOnlyList<TopAccount>> GetTopAccountsByBalanceAsync(
        int limit,
        CancellationToken ct = default);
}

public sealed record TopAccount(string Address, long Balance);

public sealed class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    public bool Enabled { get; init; } = true;
    public int PollingIntervalMinutes { get; init; } = 60;
    public int TopAccountsLimit { get; init; } = 1000;
}
