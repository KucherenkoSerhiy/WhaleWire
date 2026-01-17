namespace WhaleWire.Application.Blockchain;

public interface IBlockchainClient
{
    string Chain {  get; }
    string Provider { get; }

    Task<IReadOnlyList<RawChainEvent>> GetEventsAsync(
        string address,
        Cursor? affectedCursor,
        int limit,
        CancellationToken token);
}