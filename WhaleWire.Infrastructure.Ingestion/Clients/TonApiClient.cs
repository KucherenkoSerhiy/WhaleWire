using System.Text.Json;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Infrastructure.Ingestion.Configuration;
using WhaleWire.Infrastructure.Ingestion.Models;

namespace WhaleWire.Infrastructure.Ingestion.Clients;

public sealed class TonApiClient(
    HttpClient httpClient,
    IOptions<TonApiOptions> options) : IBlockchainClient
{
    private readonly TonApiOptions _options = options.Value;

    public string Chain => "ton";
    public string Provider => "tonapi";

    public async Task<IReadOnlyList<RawChainEvent>> GetEventsAsync(
        string address,
        Cursor? afterCursor,
        int limit,
        CancellationToken token = default)
    {
        var tonTxs = await FetchFromApiAsync(address, afterCursor, limit, token);
        
        return tonTxs.Select(tx => new RawChainEvent(
            Chain: Chain,
            Provider: Provider,
            Address: address,
            Cursor: new Cursor(tx.Lt, tx.Hash),
            Hash: tx.Hash,
            OccurredAt: DateTimeOffset.FromUnixTimeSeconds(tx.Utime).UtcDateTime,
            RawJson: tx.RawJson ?? JsonSerializer.Serialize(tx)
        )).ToList();
    }
    
    private async Task<IReadOnlyList<TonTransaction>> FetchFromApiAsync(
        string address,
        Cursor? cursor,
        int limit,
        CancellationToken ct)
    {
        var url = $"/v2/blockchain/accounts/{address}/transactions?limit={limit}";
        
        if (cursor is not null)
        {
            url += $"&after_lt={cursor.Primary}";
        }

        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<TonApiTransactionsResponse>(json);

        if (result?.Transactions == null) return [];
        
        foreach (var transaction in result.Transactions)
        {
            transaction.RawJson = json;
        }
        return result.Transactions;
    }
}