using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WhaleWire.Application.Blockchain;

namespace WhaleWire.Infrastructure.Ingestion.Providers;

public sealed class TonNativeTopHoldersProvider(
    HttpClient httpClient,
    ILogger<TonNativeTopHoldersProvider> logger) : IAssetTopHoldersProvider
{
    public async Task<AssetTopHolders> GetTopHoldersAsync(int limit, CancellationToken ct = default)
    {
        var url = $"/api/v3/topAccountsByBalance?limit={limit}";
        logger.LogDebug("Fetching top {Limit} TON accounts from {Url}", limit, httpClient.BaseAddress);
        
        var response = await httpClient.GetAsync(url, ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var accounts = JsonSerializer.Deserialize<AccountInfo[]>(json);

        var holders = accounts?
            .Select(a => new WalletHolder(
                a.Account,
                BigInteger.TryParse(a.Balance, out var bal) ? bal : BigInteger.Zero))
            .ToList() ?? [];

        return new AssetTopHolders("TON", "native", holders);
    }

    private sealed record AccountInfo(
        [property: JsonPropertyName("account")] string Account,
        [property: JsonPropertyName("balance")] string Balance);
}
