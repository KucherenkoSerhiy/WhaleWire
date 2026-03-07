using WhaleWire.Application.Blockchain;
using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Tests.Fakes;

/// <summary>
/// Fake IBlockchainClient for integration tests. Returns fixed chain/provider.
/// </summary>
public sealed class FakeBlockchainClient : IBlockchainClient
{
    public string Chain => "ton";
    public string Provider => "tonapi";

    public Task<IReadOnlyList<BlockchainEvent>> GetEventsAsync(
        string address,
        Cursor? afterCursor,
        int limit,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BlockchainEvent>>([]);
}
