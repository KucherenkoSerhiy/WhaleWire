namespace WhaleWire.Infrastructure.Ingestion.Configuration;

public sealed class TonApiOptions
{
    public const string SectionName = "TonApi";

    public required string BaseUrl { get; init; }
    public string? ApiKey { get; init; }
    public int DefaultLimit { get; init; } = 100;
    public int DelayBetweenRequestsMs { get; init; } = 1000;
}
