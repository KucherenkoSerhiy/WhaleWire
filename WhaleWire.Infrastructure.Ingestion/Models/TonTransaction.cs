using System.Text.Json.Serialization;

namespace WhaleWire.Infrastructure.Ingestion.Models;

public sealed class TonTransaction
{
    [JsonPropertyName("lt")] public required long Lt { get; init; }

    [JsonPropertyName("hash")] public required string Hash { get; init; }

    [JsonPropertyName("utime")] public required long Utime { get; init; }

    public string? RawJson { get; set; }
}