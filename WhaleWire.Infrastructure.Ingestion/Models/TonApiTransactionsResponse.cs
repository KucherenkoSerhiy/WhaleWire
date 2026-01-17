using System.Text.Json.Serialization;

namespace WhaleWire.Infrastructure.Ingestion.Models;

public sealed class TonApiTransactionsResponse
{
    [JsonPropertyName("transactions")] 
    public required IReadOnlyList<TonTransaction> Transactions { get; init; }
}