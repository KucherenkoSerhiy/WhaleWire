namespace WhaleWire.Configuration;

public sealed class MetricsCollectorOptions
{
    public const string SectionName = "MetricsCollector";

    /// <summary>Interval between metric collection cycles in seconds. Default 30.</summary>
    public int IntervalSeconds { get; init; } = 30;

    /// <summary>Lag above this many seconds counts toward <c>whalewire_event_lag_stale_wallets</c>. Default 900 (15 minutes).</summary>
    public int StaleLagThresholdSeconds { get; init; } = 900;
}
