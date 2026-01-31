namespace WhaleWire.Infrastructure.Ingestion.Configuration;

public sealed class TonCenterOptions
{
    public const string SectionName = "TonCenter";

    public required string BaseUrl { get; init; }
    public string? ApiKey { get; init; }
    public IReadOnlyList<JettonConfig> Jettons { get; init; } = [];
    public int DelayBetweenRequestsMs { get; init; } = 1000;
}

public sealed class JettonConfig
{
    public required string Symbol { get; init; }
    public required string MasterAddress { get; init; }
}
