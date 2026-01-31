using System.Numerics;

namespace WhaleWire.Application.Blockchain;

public interface IAssetTopHoldersProvider
{
    Task<AssetTopHolders> GetTopHoldersAsync(int limit, CancellationToken ct = default);
}

public sealed record AssetTopHolders(
    string AssetIdentifier,
    string AssetType,
    IReadOnlyList<WalletHolder> Holders);

public sealed record WalletHolder(string Address, BigInteger Balance);
