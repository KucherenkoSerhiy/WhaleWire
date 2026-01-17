using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Application.Blockchain;

public interface IBlockchainClient
{
    string Chain { get; }
    string Provider { get; }

    Task<IReadOnlyList<BlockchainEvent>> GetEventsAsync(
        string address,
        Cursor? afterCursor,
        int limit,
        CancellationToken ct = default);
}