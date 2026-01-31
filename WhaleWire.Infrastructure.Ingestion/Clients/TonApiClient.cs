using System.Text.Json;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Domain;
using WhaleWire.Domain.Services;
using WhaleWire.Infrastructure.Ingestion.Configuration;
using WhaleWire.Infrastructure.Ingestion.Models;
using WhaleWire.Messages;

namespace WhaleWire.Infrastructure.Ingestion.Clients;

public sealed class TonApiClient(
    HttpClient httpClient,
    IOptions<TonApiOptions> options) : IBlockchainClient
{
    private readonly TonApiOptions _options = options.Value;

    public string Chain => "ton";
    public string Provider => "tonapi";

    public async Task<IReadOnlyList<BlockchainEvent>> GetEventsAsync(
        string address,
        Cursor? afterCursor,
        int limit,
        CancellationToken ct = default)
    {
        var tonTxs = await FetchFromApiAsync(address, afterCursor, limit, ct);
        
        return tonTxs.Select(tx => new BlockchainEvent
        {
            EventId = EventIdGenerator.Generate(Chain, address, tx.Lt, tx.Hash),
            Chain = Chain,
            Provider = Provider,
            Address = address,
            Cursor = new Cursor(tx.Lt, tx.Hash),
            OccurredAt = DateTimeOffset.FromUnixTimeSeconds(tx.Utime).UtcDateTime,
            RawJson = tx.RawJson ?? JsonSerializer.Serialize(tx)
        }).ToList();
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
        
        var sanitizedJson = SanitizeJson(json);
        foreach (var transaction in result.Transactions)
        {
            transaction.RawJson = sanitizedJson;
        }
        return result.Transactions;
    }

    private static string SanitizeJson(string json)
    {
        // Remove null bytes and control characters that Postgres can't handle
        return json
            .Replace("\\u0000", "")
            .Replace("\u0000", "")
            .Replace("\\u0001", "")
            .Replace("\u0001", "");
    }
}