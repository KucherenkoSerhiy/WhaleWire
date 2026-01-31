using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WhaleWire.Application.Blockchain;

namespace WhaleWire.Infrastructure.Ingestion.Providers;

public sealed class JettonTopHoldersProvider(
    HttpClient httpClient,
    string jettonMasterAddress,
    string symbol,
    ILogger<JettonTopHoldersProvider> logger) : IAssetTopHoldersProvider
{
    public async Task<AssetTopHolders> GetTopHoldersAsync(int limit, CancellationToken ct = default)
    {
        var url = $"/api/v3/jetton/wallets" +
                  $"?jetton_address={jettonMasterAddress}" +
                  $"&sort=desc" +
                  $"&exclude_zero_balance=true" +
                  $"&limit={limit}";

        logger.LogDebug("Fetching top {Limit} {Symbol} holders", limit, symbol);
        
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<JettonWalletsResponse>(json);

        var holders = result?.JettonWallets?
            .Select(w => new WalletHolder(
                w.Owner,
                BigInteger.TryParse(w.Balance, out var bal) ? bal : BigInteger.Zero))
            .ToList() ?? [];

        return new AssetTopHolders(symbol, "jetton", holders);
    }

    private sealed record JettonWalletsResponse(
        [property: JsonPropertyName("jetton_wallets")] JettonWallet[]? JettonWallets);

    private sealed record JettonWallet(
        [property: JsonPropertyName("owner")] string Owner,
        [property: JsonPropertyName("balance")] string Balance);
}
