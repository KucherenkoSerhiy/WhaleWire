namespace WhaleWire.Configuration;

public sealed class MetricsCollectorOptions
{
    public const string SectionName = "MetricsCollector";

    /// <summary>Interval between metric collection cycles in seconds. Default 30.</summary>
    public int IntervalSeconds { get; init; } = 30;
}
