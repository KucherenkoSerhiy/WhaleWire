using WhaleWire.Application.UseCases;

namespace WhaleWire.Tests.Fakes;

/// <summary>
/// ITopAccountsClient that always throws. Used to verify discovery failure path.
/// </summary>
public sealed class FailingTopAccountsClient : ITopAccountsClient
{
    public Task<IReadOnlyList<Application.Blockchain.AssetTopHolders>> GetTopAccountsByAssetAsync(
        int limit,
        CancellationToken ct = default) =>
        throw new InvalidOperationException("Simulated TonCenter API failure");
}
