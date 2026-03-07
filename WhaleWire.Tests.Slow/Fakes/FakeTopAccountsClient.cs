using WhaleWire.Application.UseCases;

namespace WhaleWire.Tests.Fakes;

/// <summary>
/// Fake ITopAccountsClient for integration tests. Returns configurable address count.
/// </summary>
public sealed class FakeTopAccountsClient : ITopAccountsClient
{
    private readonly IReadOnlyList<Application.Blockchain.AssetTopHolders> _assetHolders;

    public FakeTopAccountsClient(int addressCount = 3)
    {
        var holders = Enumerable.Range(0, addressCount)
            .Select(i => new Application.Blockchain.WalletHolder($"0:FAKE_ADDR_{i}", 1000))
            .ToList();
        _assetHolders = [new Application.Blockchain.AssetTopHolders("TON", "native", holders)];
    }

    public Task<IReadOnlyList<Application.Blockchain.AssetTopHolders>> GetTopAccountsByAssetAsync(
        int limit,
        CancellationToken ct = default) =>
        Task.FromResult(_assetHolders);
}
